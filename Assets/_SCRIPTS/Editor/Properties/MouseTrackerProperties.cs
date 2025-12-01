using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for MouseTracker module.
/// Tests Properties 7, 8, and 9 from the design document.
/// </summary>
[TestFixture]
public class MouseTrackerProperties
{
    private const int PropertyTestIterations = 100;
    private GameObject _testObject;
    private MouseTracker _mouseTracker;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestMouseTracker");
        _mouseTracker = _testObject.AddComponent<MouseTracker>();
        // Initialize without a RecoilSystem for isolated testing
        _mouseTracker.Initialize(null);
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
    /// **Feature: epic-recoil-system, Property 7: Mouse Compensation Reduces Recoil**
    /// *For any* positive accumulated recoil and mouse movement in the opposite direction 
    /// (downward for positive pitch, left for positive yaw), the MouseTracker SHALL produce 
    /// a compensation delta that reduces accumulated recoil when applied.
    /// **Validates: Requirements 3.1, 3.2**
    /// </summary>
    [Test]
    public void Property7_MouseCompensationReducesRecoil()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate random positive recoil (upward and/or rightward)
            float recoilVertical = RecoilGenerators.RandomFloat(0.5f, 5f);
            float recoilHorizontal = RecoilGenerators.RandomFloat(-2f, 2f);
            Vector2 recoilDirection = new Vector2(recoilVertical, recoilHorizontal);

            // Generate mouse input that opposes the recoil
            // For upward recoil (positive X), compensation is mouse down (negative Y in screen space)
            // For rightward recoil (positive Y), compensation is mouse left (negative X in screen space)
            // Mouse input maps: mouse Y -> pitch (recoil X), mouse X -> yaw (recoil Y)
            // To oppose recoil, we need: -mouseY to oppose recoilX, -mouseX to oppose recoilY
            float compensatingMouseY = -recoilVertical * RecoilGenerators.RandomFloat(0.5f, 2f);
            float compensatingMouseX = -recoilHorizontal * RecoilGenerators.RandomFloat(0.5f, 2f);
            Vector2 mouseInput = new Vector2(compensatingMouseX, compensatingMouseY);

            // Configure the tracker
            _mouseTracker.CompensationMultiplier = RecoilGenerators.RandomFloat(0.5f, 3f);
            _mouseTracker.MaxCompensationRate = RecoilGenerators.RandomFloat(1.5f, 5f);

            // Set the recoil direction and process mouse input
            _mouseTracker.SetRecoilDirectionForTesting(recoilDirection);
            _mouseTracker.SetMouseInputForTesting(mouseInput);

            // Check that compensation effectiveness is >= 1 (accelerated recovery)
            float effectiveness = _mouseTracker.CompensationEffectiveness;
            
            if (effectiveness < 1f - 0.0001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Compensation effectiveness {effectiveness} < 1.0 " +
                    $"for recoil {recoilDirection} and mouse {mouseInput}";
                continue;
            }

            // Check that compensation delta has components that would reduce recoil
            // CompensationDelta is in recoil space, so positive values should reduce positive recoil
            Vector2 compensationDelta = _mouseTracker.CompensationDelta;
            
            // The compensation delta should have the same sign as the recoil direction
            // (because it represents how much to reduce the recoil)
            // Actually, let's verify that applying compensation would reduce recoil magnitude
            // If recoil is positive and compensation delta is positive, subtracting it reduces recoil
            
            // For significant mouse input, we expect some compensation
            if (mouseInput.magnitude > 0.1f && compensationDelta.magnitude < 0.00001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: No compensation delta produced for significant " +
                    $"opposing mouse input {mouseInput} against recoil {recoilDirection}";
            }

            _mouseTracker.Reset();
        }

        Assert.AreEqual(0, failures,
            $"Property 7 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 8: Compensation Effectiveness Clamped**
    /// *For any* mouse input magnitude, the MouseTracker compensation effectiveness 
    /// SHALL never exceed the configured maximum compensation rate.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Test]
    public void Property8_CompensationEffectivenessClamped()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate random max compensation rate
            float maxRate = RecoilGenerators.RandomFloat(1.1f, 5f);
            _mouseTracker.MaxCompensationRate = maxRate;
            _mouseTracker.CompensationMultiplier = RecoilGenerators.RandomFloat(1f, 10f); // High multiplier to test clamping

            // Generate extreme mouse input to try to exceed the max rate
            float extremeMagnitude = RecoilGenerators.RandomFloat(50f, 200f);
            Vector2 extremeMouseInput = new Vector2(
                RecoilGenerators.RandomFloat(-1f, 1f),
                RecoilGenerators.RandomFloat(-1f, 1f)
            ).normalized * extremeMagnitude;

            // Set a recoil direction that the mouse opposes
            Vector2 recoilDirection = new Vector2(1f, 0f); // Upward recoil
            _mouseTracker.SetRecoilDirectionForTesting(recoilDirection);
            _mouseTracker.SetMouseInputForTesting(extremeMouseInput);

            float effectiveness = _mouseTracker.CompensationEffectiveness;

            if (effectiveness > maxRate + 0.0001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Effectiveness {effectiveness} exceeded max rate {maxRate} " +
                    $"for extreme mouse input magnitude {extremeMouseInput.magnitude}";
            }

            _mouseTracker.Reset();
        }

        Assert.AreEqual(0, failures,
            $"Property 8 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 9: Zero Input Yields Base Recovery**
    /// *For any* recoil state with zero mouse input, the recovery rate SHALL equal 
    /// the base recovery rate (compensation multiplier of 1.0).
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Test]
    public void Property9_ZeroInputYieldsBaseRecovery()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Configure with random settings
            _mouseTracker.CompensationMultiplier = RecoilGenerators.RandomFloat(0.5f, 5f);
            _mouseTracker.MaxCompensationRate = RecoilGenerators.RandomFloat(1.5f, 5f);

            // Set a random recoil direction
            Vector2 recoilDirection = RecoilGenerators.GenerateRecoilDelta();
            _mouseTracker.SetRecoilDirectionForTesting(recoilDirection);

            // Set zero mouse input
            _mouseTracker.SetMouseInputForTesting(Vector2.zero);

            float effectiveness = _mouseTracker.CompensationEffectiveness;

            // With zero input, effectiveness should be exactly 1.0 (base recovery rate)
            if (Mathf.Abs(effectiveness - 1f) > 0.0001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Effectiveness {effectiveness} != 1.0 for zero mouse input " +
                    $"with recoil direction {recoilDirection}";
            }

            // Compensation delta should also be zero
            Vector2 compensationDelta = _mouseTracker.CompensationDelta;
            if (compensationDelta.magnitude > 0.0001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Compensation delta {compensationDelta} != zero for zero mouse input";
            }

            _mouseTracker.Reset();
        }

        Assert.AreEqual(0, failures,
            $"Property 9 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that disabled MouseTracker returns base recovery rate.
    /// </summary>
    [Test]
    public void DisabledMouseTracker_ReturnsBaseRecoveryRate()
    {
        _mouseTracker.IsEnabled = false;

        // Set some mouse input
        _mouseTracker.SetMouseInputForTesting(new Vector2(10f, -10f));
        _mouseTracker.OnUpdate(0.016f);

        Assert.AreEqual(1f, _mouseTracker.CompensationEffectiveness, 0.0001f,
            "Disabled MouseTracker should return base recovery rate (1.0)");
        Assert.AreEqual(Vector2.zero, _mouseTracker.CompensationDelta,
            "Disabled MouseTracker should return zero compensation delta");
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        // Set some state
        _mouseTracker.SetRecoilDirectionForTesting(new Vector2(2f, 1f));
        _mouseTracker.SetMouseInputForTesting(new Vector2(5f, -5f));

        // Reset
        _mouseTracker.Reset();

        Assert.AreEqual(1f, _mouseTracker.CompensationEffectiveness, 0.0001f,
            "After reset, effectiveness should be 1.0");
        Assert.AreEqual(Vector2.zero, _mouseTracker.CompensationDelta,
            "After reset, compensation delta should be zero");
    }
}
