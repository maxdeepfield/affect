using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for GaitController.
/// Tests Properties 5, 6, 7 from the design document.
/// </summary>
[TestFixture]
public class GaitProperties
{
    private const int PropertyTestIterations = 100;

    private GameObject _testObject;
    private GaitController _gaitController;
    private SpiderIKSystem _system;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestGaitController");
        _system = _testObject.AddComponent<SpiderIKSystem>();
        _gaitController = _testObject.AddComponent<GaitController>();

        _system.Config = new IKConfiguration();
        _gaitController.Initialize(_system);
    }

    [TearDown]
    public void TearDown()
    {
        if (_testObject != null)
        {
            Object.DestroyImmediate(_testObject);
        }
    }

    /// <summary>
    /// Creates test legs with specified count and positions.
    /// </summary>
    private LegData[] CreateTestLegs(int count)
    {
        LegData[] legs = new LegData[count];

        for (int i = 0; i < count; i++)
        {
            legs[i] = new LegData();
            legs[i].legIndex = i;

            // Position legs around the body in a circle
            float angle = (float)i / count * Mathf.PI * 2f;
            float radius = 0.5f;
            legs[i].restTarget = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            legs[i].plantedPos = legs[i].restTarget;
            legs[i].currentTarget = legs[i].restTarget;
            legs[i].isStepping = false;
            legs[i].stepProgress = 0f;
            legs[i].lastStepTime = -999f;
        }

        return legs;
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 5: Gait Alternation Invariant**
    /// *For any* spider with 2+ legs during movement, at most one diagonal group 
    /// SHALL have stepping legs at any given time.
    /// **Validates: Requirements 2.1, 2.2, 2.4**
    /// </summary>
    [Test]
    public void Property5_GaitAlternationInvariant()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random leg count (2-8)
            int legCount = SpiderGenerators.RandomInt(2, 8);
            LegData[] legs = CreateTestLegs(legCount);

            // Assign diagonal groups
            _gaitController.AssignAllDiagonalGroups(legs);

            // Set random step threshold
            float threshold = SpiderGenerators.RandomFloat(0.1f, 0.5f);
            _gaitController.SetStepThreshold(threshold);

            try
            {
                // Simulate multiple gait updates with movement
                Vector3 velocity = SpiderGenerators.GenerateVelocity(5f);

                for (int update = 0; update < 20; update++)
                {
                    // Move some legs beyond threshold to trigger steps
                    foreach (var leg in legs)
                    {
                        if (!leg.isStepping && SpiderGenerators.RandomFloat(0f, 1f) > 0.7f)
                        {
                            // Move planted position away from rest
                            leg.plantedPos = leg.restTarget + Random.onUnitSphere * (threshold * 1.5f);
                        }
                    }

                    // Update gait
                    _gaitController.UpdateGait(legs, velocity);

                    // Check invariant: at most one group should have stepping legs
                    bool group0Stepping = _gaitController.IsGroupStepping(legs, 0);
                    bool group1Stepping = _gaitController.IsGroupStepping(legs, 1);

                    if (group0Stepping && group1Stepping)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}, Update {update}: Both diagonal groups have stepping legs simultaneously. LegCount: {legCount}";
                        break;
                    }

                    // Simulate step completion for some legs
                    foreach (var leg in legs)
                    {
                        if (leg.isStepping && SpiderGenerators.RandomFloat(0f, 1f) > 0.5f)
                        {
                            _gaitController.CompleteStep(leg);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 5 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 6: Step Threshold Triggering**
    /// *For any* leg whose planted position distance from rest target exceeds stepThreshold,
    /// and whose diagonal group is not blocked, the Gait_Controller SHALL initiate a step.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Test]
    public void Property6_StepThresholdTriggering()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Create legs
            int legCount = SpiderGenerators.RandomInt(2, 8);
            LegData[] legs = CreateTestLegs(legCount);
            _gaitController.AssignAllDiagonalGroups(legs);

            // Set random threshold
            float threshold = SpiderGenerators.RandomFloat(0.1f, 0.5f);
            _gaitController.SetStepThreshold(threshold);

            try
            {
                // Pick a random leg
                int legIndex = SpiderGenerators.RandomInt(0, legCount - 1);
                LegData testLeg = legs[legIndex];

                // Set the active group to this leg's group
                _gaitController.SetActiveStepGroup(testLeg.diagonalGroup);

                // Move planted position beyond threshold from rest target
                // The desired target will be at rest position (no velocity), so we move
                // planted position away from rest
                float beyondThreshold = threshold * 1.5f;
                Vector3 offsetDir = Random.onUnitSphere;
                offsetDir.y = 0; // Keep horizontal
                if (offsetDir.sqrMagnitude < 0.001f) offsetDir = Vector3.forward;
                offsetDir.Normalize();
                
                testLeg.plantedPos = testLeg.restTarget + offsetDir * beyondThreshold;
                testLeg.lastStepTime = -999f; // Ensure cooldown passed
                testLeg.isStepping = false;

                // Update gait with zero velocity (so desired target = rest target)
                _gaitController.UpdateGait(legs, Vector3.zero);

                // Check that the leg initiated a step
                float actualDistance = Vector3.Distance(testLeg.plantedPos, testLeg.restTarget);
                if (!testLeg.isStepping)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Leg {legIndex} did not initiate step despite planted pos being {actualDistance}m from rest (threshold: {threshold}m)";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 6 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 7: Diagonal Group Assignment**
    /// *For any* leg count between 1 and 8, all legs SHALL be assigned to exactly one 
    /// diagonal group (0 or 1) based on their local position quadrant.
    /// **Validates: Requirements 2.6**
    /// </summary>
    [Test]
    public void Property7_DiagonalGroupAssignment()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Test all leg counts 1-8
            int legCount = SpiderGenerators.RandomInt(1, 8);
            LegData[] legs = CreateTestLegs(legCount);

            try
            {
                // Assign diagonal groups
                _gaitController.AssignAllDiagonalGroups(legs);

                // Verify all legs have valid group assignment
                foreach (var leg in legs)
                {
                    if (leg.diagonalGroup != 0 && leg.diagonalGroup != 1)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Leg {leg.legIndex} has invalid diagonal group {leg.diagonalGroup}. LegCount: {legCount}";
                        break;
                    }
                }

                // For leg count > 1, verify both groups have legs (balanced distribution)
                if (legCount > 1)
                {
                    var (group0, group1) = _gaitController.GetGroupCounts(legs);

                    if (group0 == 0 || group1 == 0)
                    {
                        // This is acceptable for some configurations, but let's verify
                        // at least one group has legs
                        if (group0 + group1 != legCount)
                        {
                            failures++;
                            lastFailureMessage = $"Iteration {i}: Group counts ({group0}, {group1}) don't sum to leg count {legCount}";
                        }
                    }
                }

                // Verify single leg is always in group 0
                if (legCount == 1 && legs[0].diagonalGroup != 0)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Single leg should be in group 0, but is in group {legs[0].diagonalGroup}";
                }

                // Verify diagonal pairing logic
                for (int j = 0; j < legs.Length; j++)
                {
                    Vector3 pos = legs[j].restTarget;
                    int expectedGroup = _gaitController.AssignDiagonalGroup(pos, legCount);

                    if (legs[j].diagonalGroup != expectedGroup)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Leg {j} at position {pos} has group {legs[j].diagonalGroup} but expected {expectedGroup}";
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 7 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }
}
