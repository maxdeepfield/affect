using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for CameraShaker module.
/// Tests Properties 10, 11, and 12 from the design document.
/// </summary>
[TestFixture]
public class CameraShakerProperties
{
    private const int PropertyTestIterations = 100;
    private GameObject _testObject;
    private CameraShaker _cameraShaker;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestCameraShaker");
        _cameraShaker = _testObject.AddComponent<CameraShaker>();
        // Initialize without a RecoilSystem for isolated testing
        _cameraShaker.Initialize(null);
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
    /// **Feature: epic-recoil-system, Property 10: Shake Proportional to Recoil Magnitude**
    /// *For any* two recoil states A and B where |A| > |B|, the CameraShaker intensity for A 
    /// SHALL be greater than or equal to the intensity for B. When recoil is zero, shake 
    /// intensity SHALL be zero.
    /// **Validates: Requirements 4.1, 4.4**
    /// </summary>
    [Test]
    public void Property10_ShakeProportionalToRecoilMagnitude()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate two different recoil magnitudes where A > B
            float magnitudeB = RecoilGenerators.RandomFloat(0.1f, 2f);
            float magnitudeA = magnitudeB + RecoilGenerators.RandomFloat(0.5f, 3f);

            // Configure the shaker with random but valid settings
            _cameraShaker.ShakeIntensity = RecoilGenerators.RandomFloat(0.01f, 0.1f);
            _cameraShaker.ShakeFrequency = RecoilGenerators.RandomFloat(10f, 50f);
            _cameraShaker.PathFollowStrength = RecoilGenerators.RandomFloat(0f, 1f);

            // Test with magnitude A (larger)
            _cameraShaker.Reset();
            _cameraShaker.SetRecoilMagnitudeForTesting(magnitudeA);
            _cameraShaker.SetRecoilPathForTesting(new Vector2(magnitudeA, 0f));
            _cameraShaker.OnUpdate(0.016f);
            float intensityA = _cameraShaker.CurrentShakeOffset.magnitude;

            // Test with magnitude B (smaller)
            _cameraShaker.Reset();
            _cameraShaker.SetRecoilMagnitudeForTesting(magnitudeB);
            _cameraShaker.SetRecoilPathForTesting(new Vector2(magnitudeB, 0f));
            _cameraShaker.OnUpdate(0.016f);
            float intensityB = _cameraShaker.CurrentShakeOffset.magnitude;

            // Intensity A should be >= Intensity B (with small tolerance for noise variation)
            // We use a ratio check since exact proportionality depends on noise
            if (intensityA < intensityB - 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Intensity for larger magnitude ({magnitudeA}) = {intensityA} " +
                    $"< intensity for smaller magnitude ({magnitudeB}) = {intensityB}";
            }

            // Test zero magnitude produces zero shake
            _cameraShaker.Reset();
            _cameraShaker.SetRecoilMagnitudeForTesting(0f);
            _cameraShaker.OnUpdate(0.016f);
            float zeroIntensity = _cameraShaker.CurrentShakeOffset.magnitude;

            if (zeroIntensity > 0.0001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Zero recoil magnitude produced non-zero shake intensity: {zeroIntensity}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 10 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: epic-recoil-system, Property 11: Shake Direction Follows Recoil Path**
    /// *For any* recoil path direction, after sufficient update time, the CameraShaker 
    /// direction component SHALL align within 45 degrees of the recoil path direction.
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Test]
    public void Property11_ShakeDirectionFollowsRecoilPath()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate a random recoil path direction
            float pathX = RecoilGenerators.RandomFloat(-2f, 5f); // Vertical (mostly upward)
            float pathY = RecoilGenerators.RandomFloat(-2f, 2f); // Horizontal
            Vector2 recoilPath = new Vector2(pathX, pathY);

            // Skip near-zero paths as direction is undefined
            if (recoilPath.magnitude < 0.1f)
            {
                continue;
            }

            // Configure the shaker with high path follow strength
            _cameraShaker.Reset();
            _cameraShaker.ShakeIntensity = RecoilGenerators.RandomFloat(0.02f, 0.1f);
            _cameraShaker.ShakeFrequency = RecoilGenerators.RandomFloat(10f, 50f);
            _cameraShaker.PathFollowStrength = RecoilGenerators.RandomFloat(0.5f, 1f); // High follow strength

            // Set the recoil path and magnitude
            _cameraShaker.SetRecoilMagnitudeForTesting(recoilPath.magnitude);
            _cameraShaker.SetRecoilPathForTesting(recoilPath);

            // Run several updates to allow interpolation to settle
            for (int frame = 0; frame < 30; frame++)
            {
                _cameraShaker.OnUpdate(0.016f);
            }

            // Get the current shake direction
            Vector2 shakeDir = _cameraShaker.CurrentShakeDirection;
            Vector2 pathDir = recoilPath.normalized;

            // Calculate angle between shake direction and recoil path
            float dotProduct = Vector2.Dot(shakeDir, pathDir);
            float angle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * Mathf.Rad2Deg;

            // Shake direction should be within 45 degrees of recoil path
            if (angle > 45f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Shake direction {shakeDir} is {angle:F1} degrees " +
                    $"from recoil path {pathDir} (expected <= 45 degrees)";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 11 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: epic-recoil-system, Property 12: Active Shake Produces Non-Zero Output**
    /// *For any* non-zero recoil state sampled at different times t1 and t2 
    /// (where t2 - t1 > 1/frequency), the CameraShaker output SHALL differ between samples, 
    /// demonstrating high-frequency variation.
    /// **Validates: Requirements 4.3**
    /// </summary>
    [Test]
    public void Property12_ActiveShakeProducesNonZeroOutput()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Configure the shaker
            float frequency = RecoilGenerators.RandomFloat(10f, 50f);
            _cameraShaker.Reset();
            _cameraShaker.ShakeIntensity = RecoilGenerators.RandomFloat(0.02f, 0.1f);
            _cameraShaker.ShakeFrequency = frequency;
            _cameraShaker.PathFollowStrength = RecoilGenerators.RandomFloat(0f, 1f);

            // Set a non-zero recoil magnitude
            float magnitude = RecoilGenerators.RandomFloat(1f, 5f);
            _cameraShaker.SetRecoilMagnitudeForTesting(magnitude);
            _cameraShaker.SetRecoilPathForTesting(new Vector2(magnitude, 0f));

            // Sample at time t1
            _cameraShaker.OnUpdate(0.016f);
            Vector3 shakeAtT1 = _cameraShaker.CurrentShakeOffset;
            Quaternion rotationAtT1 = _cameraShaker.CurrentShakeRotation;

            // Advance time by more than 1/frequency to ensure different noise sample
            float timeAdvance = (1f / frequency) * 2f; // Double the period
            _cameraShaker.AdvanceTimeForTesting(timeAdvance);

            // Sample at time t2
            _cameraShaker.OnUpdate(0.016f);
            Vector3 shakeAtT2 = _cameraShaker.CurrentShakeOffset;
            Quaternion rotationAtT2 = _cameraShaker.CurrentShakeRotation;

            // Check that shake values differ between samples
            float positionDifference = (shakeAtT2 - shakeAtT1).magnitude;
            float rotationDifference = Quaternion.Angle(rotationAtT1, rotationAtT2);

            // At least one of position or rotation should differ
            bool hasDifference = positionDifference > 0.0001f || rotationDifference > 0.01f;

            if (!hasDifference)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Shake did not vary between t1 and t2 " +
                    $"(position diff: {positionDifference}, rotation diff: {rotationDifference} degrees) " +
                    $"for magnitude {magnitude} and frequency {frequency}Hz";
            }

            // Also verify that active shake produces non-zero output
            if (shakeAtT1.magnitude < 0.00001f && shakeAtT2.magnitude < 0.00001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Active shake produced zero output for magnitude {magnitude}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 12 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// Verifies that disabled CameraShaker returns zero shake.
    /// </summary>
    [Test]
    public void DisabledCameraShaker_ReturnsZeroShake()
    {
        _cameraShaker.IsEnabled = false;

        // Set some recoil
        _cameraShaker.SetRecoilMagnitudeForTesting(3f);
        _cameraShaker.SetRecoilPathForTesting(new Vector2(3f, 1f));
        _cameraShaker.OnUpdate(0.016f);

        Assert.AreEqual(Vector3.zero, _cameraShaker.CurrentShakeOffset,
            "Disabled CameraShaker should return zero shake offset");
        Assert.AreEqual(Quaternion.identity, _cameraShaker.CurrentShakeRotation,
            "Disabled CameraShaker should return identity rotation");
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        // Set some state
        _cameraShaker.SetRecoilMagnitudeForTesting(3f);
        _cameraShaker.SetRecoilPathForTesting(new Vector2(3f, 1f));
        _cameraShaker.OnUpdate(0.016f);

        // Verify we have some shake
        Assert.Greater(_cameraShaker.CurrentShakeOffset.magnitude, 0f,
            "Should have shake before reset");

        // Reset
        _cameraShaker.Reset();

        Assert.AreEqual(Vector3.zero, _cameraShaker.CurrentShakeOffset,
            "After reset, shake offset should be zero");
        Assert.AreEqual(Quaternion.identity, _cameraShaker.CurrentShakeRotation,
            "After reset, shake rotation should be identity");
        Assert.AreEqual(0f, _cameraShaker.CurrentRecoilMagnitude, 0.0001f,
            "After reset, recoil magnitude should be zero");
    }

    /// <summary>
    /// Verifies that OnRecoilApplied updates the recoil magnitude and path.
    /// </summary>
    [Test]
    public void OnRecoilApplied_UpdatesRecoilState()
    {
        Vector2 recoilDelta = new Vector2(2.5f, 1.0f);
        _cameraShaker.OnRecoilApplied(recoilDelta);

        Assert.AreEqual(recoilDelta.magnitude, _cameraShaker.CurrentRecoilMagnitude, 0.0001f,
            "OnRecoilApplied should update recoil magnitude");
    }
}
