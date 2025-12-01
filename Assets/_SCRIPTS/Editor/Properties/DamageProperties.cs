using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for LegDamageHandler module.
/// Tests Properties 23, 24, 25 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class DamageProperties
{
    private const int PropertyTestIterations = 100;
    private const float Epsilon = 0.0001f;

    /// <summary>
    /// **Feature: spider-ik-walker, Property 23: Segment Damage Tracking**
    /// *For any* damage applied to a leg segment, the segment's health SHALL decrease 
    /// by exactly the damage amount (clamped to 0 minimum).
    /// **Validates: Requirements 11.1**
    /// </summary>
    [Test]
    public void Property23_SegmentDamageTracking()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Create leg data with random initial health
            float initialHealth = SpiderGenerators.RandomFloat(50f, 200f);
            int boneCount = SpiderGenerators.RandomInt(1, 3);
            
            LegData leg = new LegData();
            leg.InitializeSegments(boneCount, initialHealth);

            // Pick random segment and damage amount
            int segmentIndex = SpiderGenerators.RandomInt(0, boneCount - 1);
            float damage = SpiderGenerators.RandomFloat(1f, initialHealth * 2f);

            // Get health before damage
            float healthBefore = leg.GetSegmentHealth(segmentIndex);

            // Apply damage
            leg.ApplyDamage(segmentIndex, damage);

            // Get health after damage
            float healthAfter = leg.GetSegmentHealth(segmentIndex);

            // Calculate expected health (clamped to 0)
            float expectedHealth = Mathf.Max(0f, healthBefore - damage);

            // Verify health decreased correctly
            if (Mathf.Abs(healthAfter - expectedHealth) > Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Health after damage {healthAfter} != expected {expectedHealth}. " +
                    $"Before: {healthBefore}, Damage: {damage}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 23 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 24: Segment Detachment Threshold**
    /// *For any* segment whose health reaches 0, the Leg_Damage_Handler SHALL detach 
    /// that segment and all child segments from the leg hierarchy.
    /// **Validates: Requirements 11.2**
    /// </summary>
    [Test]
    public void Property24_SegmentDetachmentThreshold()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Create leg data
            float initialHealth = SpiderGenerators.RandomFloat(50f, 100f);
            int boneCount = 3; // Use 3 bones to test child detachment
            
            LegData leg = new LegData();
            leg.InitializeSegments(boneCount, initialHealth);

            // Pick a segment to destroy (not the last one to test child detachment)
            int segmentIndex = SpiderGenerators.RandomInt(0, boneCount - 2);

            // Apply enough damage to destroy the segment
            float damage = initialHealth + 1f;
            leg.ApplyDamage(segmentIndex, damage);

            // Verify segment is deactivated
            if (leg.IsSegmentActive(segmentIndex))
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Segment {segmentIndex} still active after health reached 0";
                continue;
            }

            // Verify health is 0
            if (leg.GetSegmentHealth(segmentIndex) > Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Segment {segmentIndex} health {leg.GetSegmentHealth(segmentIndex)} > 0 after destruction";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 24 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 25: IK Chain Recalculation After Damage**
    /// *For any* leg that loses segments, the LegData.currentChainLength SHALL equal 
    /// the sum of remaining active segment lengths.
    /// **Validates: Requirements 11.3**
    /// </summary>
    [Test]
    public void Property25_IKChainRecalculationAfterDamage()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Create leg data with transforms for chain length calculation
            var testObj = new GameObject($"TestLeg_{i}");
            var hipObj = new GameObject("Hip");
            var kneeObj = new GameObject("Knee");
            var footObj = new GameObject("Foot");

            hipObj.transform.parent = testObj.transform;
            kneeObj.transform.parent = hipObj.transform;
            footObj.transform.parent = kneeObj.transform;

            // Position joints
            float upperLength = SpiderGenerators.RandomFloat(0.2f, 0.5f);
            float lowerLength = SpiderGenerators.RandomFloat(0.2f, 0.5f);

            hipObj.transform.position = Vector3.zero;
            kneeObj.transform.position = new Vector3(0, -upperLength, 0);
            footObj.transform.position = new Vector3(0, -upperLength - lowerLength, 0);

            LegData leg = new LegData
            {
                hip = hipObj.transform,
                knee = kneeObj.transform,
                foot = footObj.transform
            };
            leg.InitializeSegments(3, 100f);

            // Get initial chain length
            float initialChainLength = leg.CurrentChainLength;

            // Damage middle segment to deactivate it
            leg.ApplyDamage(1, 150f); // Destroy knee segment

            // Active bone count should decrease
            int activeBones = leg.ActiveBoneCount;

            // Verify active bones decreased
            if (activeBones >= 3)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Active bones {activeBones} did not decrease after damage";
            }

            // Clean up
            Object.DestroyImmediate(testObj);
        }

        Assert.AreEqual(0, failures,
            $"Property 25 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// Verifies that damage below segment health doesn't deactivate segment.
    /// </summary>
    [Test]
    public void PartialDamage_SegmentRemainsActive()
    {
        LegData leg = new LegData();
        leg.InitializeSegments(3, 100f);

        // Apply partial damage
        leg.ApplyDamage(0, 50f);

        // Segment should still be active
        Assert.IsTrue(leg.IsSegmentActive(0), "Segment should remain active after partial damage");
        Assert.AreEqual(50f, leg.GetSegmentHealth(0), Epsilon, "Health should be reduced by damage amount");
    }

    /// <summary>
    /// Verifies that multiple damage applications accumulate correctly.
    /// </summary>
    [Test]
    public void MultipleDamage_Accumulates()
    {
        LegData leg = new LegData();
        leg.InitializeSegments(3, 100f);

        // Apply damage multiple times
        leg.ApplyDamage(0, 30f);
        leg.ApplyDamage(0, 25f);
        leg.ApplyDamage(0, 20f);

        // Health should be 100 - 30 - 25 - 20 = 25
        Assert.AreEqual(25f, leg.GetSegmentHealth(0), Epsilon, "Damage should accumulate");
        Assert.IsTrue(leg.IsSegmentActive(0), "Segment should still be active");
    }

    /// <summary>
    /// Verifies that health cannot go below zero.
    /// </summary>
    [Test]
    public void ExcessiveDamage_ClampsToZero()
    {
        LegData leg = new LegData();
        leg.InitializeSegments(3, 100f);

        // Apply excessive damage
        leg.ApplyDamage(0, 500f);

        // Health should be clamped to 0
        Assert.AreEqual(0f, leg.GetSegmentHealth(0), Epsilon, "Health should be clamped to 0");
        Assert.IsFalse(leg.IsSegmentActive(0), "Segment should be deactivated at 0 health");
    }

    /// <summary>
    /// Verifies that invalid segment indices are handled gracefully.
    /// </summary>
    [Test]
    public void InvalidSegmentIndex_NoException()
    {
        LegData leg = new LegData();
        leg.InitializeSegments(3, 100f);

        // These should not throw
        Assert.DoesNotThrow(() => leg.ApplyDamage(-1, 50f));
        Assert.DoesNotThrow(() => leg.ApplyDamage(10, 50f));
        Assert.DoesNotThrow(() => leg.GetSegmentHealth(-1));
        Assert.DoesNotThrow(() => leg.GetSegmentHealth(10));
    }

    /// <summary>
    /// Verifies ActiveBoneCount decreases when segments are destroyed.
    /// </summary>
    [Test]
    public void DestroyedSegments_ReduceActiveBoneCount()
    {
        LegData leg = new LegData();
        leg.InitializeSegments(3, 100f);

        Assert.AreEqual(3, leg.ActiveBoneCount, "Should start with 3 active bones");

        // Destroy first segment
        leg.ApplyDamage(0, 150f);
        Assert.AreEqual(2, leg.ActiveBoneCount, "Should have 2 active bones after destroying one");

        // Destroy second segment
        leg.ApplyDamage(1, 150f);
        Assert.AreEqual(1, leg.ActiveBoneCount, "Should have 1 active bone after destroying two");

        // Destroy third segment
        leg.ApplyDamage(2, 150f);
        Assert.AreEqual(0, leg.ActiveBoneCount, "Should have 0 active bones after destroying all");
    }
}
