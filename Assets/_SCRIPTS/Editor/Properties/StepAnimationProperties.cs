using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for StepAnimator module.
/// Tests Properties 13 and 14 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class StepAnimationProperties
{
    private const int PropertyTestIterations = 100;
    private const float Epsilon = 0.0001f;

    private GameObject _testObject;
    private StepAnimator _animator;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestStepAnimator");
        _animator = _testObject.AddComponent<StepAnimator>();
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
    /// **Feature: spider-ik-walker, Property 13: Step Arc Maximum at Midpoint**
    /// *For any* step animation, the vertical offset from the linear path SHALL be maximum 
    /// (equal to stepHeight ±5%) when stepProgress equals 0.5.
    /// **Validates: Requirements 5.1, 5.2**
    /// </summary>
    [Test]
    public void Property13_StepArcMaximumAtMidpoint()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random step height
            float stepHeight = SpiderGenerators.RandomFloat(0.05f, 0.5f);
            _animator.StepHeight = stepHeight;

            // Generate random start and end positions
            Vector3 start = SpiderGenerators.GeneratePosition(5f);
            Vector3 end = SpiderGenerators.GeneratePosition(5f);

            // Calculate position at midpoint (progress = 0.5)
            Vector3 midpointPos = _animator.CalculateStepPosition(start, end, 0.5f);

            // Calculate linear midpoint
            Vector3 linearMidpoint = Vector3.Lerp(start, end, 0.5f);

            // Calculate actual arc height at midpoint
            float actualArcHeight = Vector3.Distance(midpointPos, linearMidpoint);

            // The arc height at midpoint should equal stepHeight
            // Formula: 4 * stepHeight * 0.5 * (1 - 0.5) = 4 * stepHeight * 0.25 = stepHeight
            float expectedHeight = stepHeight;
            float tolerance = expectedHeight * 0.05f; // 5% tolerance

            if (Mathf.Abs(actualArcHeight - expectedHeight) > tolerance)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Arc height at midpoint {actualArcHeight} " +
                    $"differs from expected {expectedHeight} by more than 5%";
            }

            // Also verify that midpoint has maximum height by checking nearby points
            float heightAtMidpoint = _animator.GetArcHeight(0.5f);
            float heightBefore = _animator.GetArcHeight(0.4f);
            float heightAfter = _animator.GetArcHeight(0.6f);

            if (heightAtMidpoint < heightBefore - Epsilon || heightAtMidpoint < heightAfter - Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Midpoint height {heightAtMidpoint} is not maximum. " +
                    $"Before: {heightBefore}, After: {heightAfter}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 13 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 14: Step Completion Accuracy**
    /// *For any* step animation when stepProgress reaches 1.0, the foot position 
    /// SHALL be within 0.001 meters of the target position.
    /// **Validates: Requirements 5.3**
    /// </summary>
    [Test]
    public void Property14_StepCompletionAccuracy()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random step parameters
            float stepHeight = SpiderGenerators.RandomFloat(0.05f, 0.5f);
            _animator.StepHeight = stepHeight;

            // Generate random start and end positions
            Vector3 start = SpiderGenerators.GeneratePosition(5f);
            Vector3 end = SpiderGenerators.GeneratePosition(5f);

            // Calculate position at completion (progress = 1.0)
            Vector3 completionPos = _animator.CalculateStepPosition(start, end, 1f);

            // The position at completion should be exactly at the end position
            float distance = Vector3.Distance(completionPos, end);
            float tolerance = 0.001f; // 0.001 meters = 1mm

            if (distance > tolerance)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Completion position differs from target by {distance}m " +
                    $"(tolerance: {tolerance}m). Start: {start}, End: {end}, Completion: {completionPos}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 14 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that arc height is zero at start and end of step.
    /// </summary>
    [Test]
    public void ArcHeight_ZeroAtStartAndEnd()
    {
        _animator.StepHeight = 0.2f;

        float heightAtStart = _animator.GetArcHeight(0f);
        float heightAtEnd = _animator.GetArcHeight(1f);

        Assert.AreEqual(0f, heightAtStart, Epsilon, "Arc height should be zero at start");
        Assert.AreEqual(0f, heightAtEnd, Epsilon, "Arc height should be zero at end");
    }

    /// <summary>
    /// Verifies that step position at progress 0 equals start position.
    /// </summary>
    [Test]
    public void StepPosition_AtStartEqualsStartPosition()
    {
        Vector3 start = new Vector3(1f, 0f, 2f);
        Vector3 end = new Vector3(3f, 0f, 4f);

        Vector3 posAtStart = _animator.CalculateStepPosition(start, end, 0f);

        Assert.AreEqual(start.x, posAtStart.x, Epsilon);
        Assert.AreEqual(start.y, posAtStart.y, Epsilon);
        Assert.AreEqual(start.z, posAtStart.z, Epsilon);
    }

    /// <summary>
    /// Verifies that configuration is applied correctly.
    /// </summary>
    [Test]
    public void ApplyConfiguration_SetsAllParameters()
    {
        IKConfiguration config = new IKConfiguration
        {
            stepHeight = 0.15f,
            stepSpeed = 7f
        };

        _animator.ApplyConfiguration(config);

        Assert.AreEqual(0.15f, _animator.StepHeight, Epsilon);
        Assert.AreEqual(7f, _animator.StepSpeed, Epsilon);
    }

    /// <summary>
    /// Verifies that arc height scales with step height configuration.
    /// </summary>
    [Test]
    public void ArcHeight_ScalesWithStepHeight()
    {
        float[] stepHeights = { 0.1f, 0.2f, 0.3f, 0.5f };

        foreach (float stepHeight in stepHeights)
        {
            _animator.StepHeight = stepHeight;
            float arcAtMidpoint = _animator.GetArcHeight(0.5f);

            // At midpoint, arc height should equal step height
            Assert.AreEqual(stepHeight, arcAtMidpoint, Epsilon,
                $"Arc height at midpoint should equal step height {stepHeight}");
        }
    }

    /// <summary>
    /// Verifies that progress is clamped to valid range.
    /// </summary>
    [Test]
    public void CalculateStepPosition_ClampsProgress()
    {
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.right;
        _animator.StepHeight = 0.1f;

        // Test negative progress (should clamp to 0)
        Vector3 posNegative = _animator.CalculateStepPosition(start, end, -0.5f);
        Vector3 posZero = _animator.CalculateStepPosition(start, end, 0f);
        Assert.AreEqual(posZero, posNegative, "Negative progress should clamp to 0");

        // Test progress > 1 (should clamp to 1)
        Vector3 posOver = _animator.CalculateStepPosition(start, end, 1.5f);
        Vector3 posOne = _animator.CalculateStepPosition(start, end, 1f);
        Assert.AreEqual(posOne, posOver, "Progress > 1 should clamp to 1");
    }
}
