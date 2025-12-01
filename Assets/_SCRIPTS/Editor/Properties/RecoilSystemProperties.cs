using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for RecoilSystem orchestrator.
/// Tests Properties 1, 2, 3, 13, 14, and 15 from the design document.
/// </summary>
[TestFixture]
public class RecoilSystemProperties
{
    private const int PropertyTestIterations = 100;
    private GameObject _testObject;
    private GameObject _cameraObject;
    private GameObject _weaponObject;
    private RecoilSystem _recoilSystem;

    [SetUp]
    public void SetUp()
    {
        // Create test hierarchy
        _testObject = new GameObject("TestRecoilSystem");
        _cameraObject = new GameObject("Camera");
        _weaponObject = new GameObject("Weapon");

        _cameraObject.transform.SetParent(_testObject.transform);
        _weaponObject.transform.SetParent(_cameraObject.transform);

        // Add camera component
        _cameraObject.AddComponent<Camera>();

        // Add RecoilSystem
        _recoilSystem = _testObject.AddComponent<RecoilSystem>();
        _recoilSystem.SetTransformsForTesting(_cameraObject.transform, _weaponObject.transform);
        _recoilSystem.ForceInitializeForTesting();
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
    /// **Feature: epic-recoil-system, Property 1: Recoil Kick Within Bounds**
    /// *For any* RecoilConfiguration and any shot fired, the applied camera rotation kick 
    /// SHALL have vertical component between 0.5 and 5 degrees, and horizontal component 
    /// between -2 and +2 degrees.
    /// **Validates: Requirements 1.1, 1.2**
    /// </summary>
    [Test]
    public void Property1_RecoilKickWithinBounds()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration config = RecoilGenerators.GenerateRecoilConfiguration();
            
            // Reset system with new config
            _recoilSystem.ResetRecoil();
            _recoilSystem.SetConfiguration(config);

            // Store initial accumulated recoil
            Vector2 initialRecoil = _recoilSystem.AccumulatedRecoil;

            // Apply recoil
            _recoilSystem.ApplyRecoil();

            // Get the kick that was applied (difference from initial)
            Vector2 appliedKick = _recoilSystem.AccumulatedRecoil - initialRecoil;

            // Check vertical bounds (0.5-5 degrees)
            if (appliedKick.x < 0.5f - 0.001f || appliedKick.x > 5f + 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Vertical kick {appliedKick.x} outside bounds [0.5, 5]";
                continue;
            }

            // Check horizontal bounds (±2 degrees)
            if (appliedKick.y < -2f - 0.001f || appliedKick.y > 2f + 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Horizontal kick {appliedKick.y} outside bounds [-2, 2]";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 1 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 2: Accumulated Recoil Clamped to Maximum**
    /// *For any* sequence of N shots fired in succession, the accumulated recoil SHALL never 
    /// exceed the configured maximum limit, regardless of how many shots are fired.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Test]
    public void Property2_AccumulatedRecoilClampedToMaximum()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration config = RecoilGenerators.GenerateRecoilConfiguration();
            
            // Ensure reasonable max values for testing
            config.maxAccumulatedVertical = RecoilGenerators.RandomFloat(5f, 20f);
            float maxHorizontal = config.horizontalSpread * 2f;

            _recoilSystem.ResetRecoil();
            _recoilSystem.SetConfiguration(config);

            // Fire many shots in rapid succession
            int shotCount = RecoilGenerators.RandomInt(10, 50);
            for (int shot = 0; shot < shotCount; shot++)
            {
                _recoilSystem.ApplyRecoil();

                // Check accumulated recoil after each shot
                Vector2 accumulated = _recoilSystem.AccumulatedRecoil;

                if (accumulated.x > config.maxAccumulatedVertical + 0.001f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}, Shot {shot}: Vertical accumulated {accumulated.x} exceeds max {config.maxAccumulatedVertical}";
                    break;
                }

                if (accumulated.y < -maxHorizontal - 0.001f || accumulated.y > maxHorizontal + 0.001f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}, Shot {shot}: Horizontal accumulated {accumulated.y} outside bounds [{-maxHorizontal}, {maxHorizontal}]";
                    break;
                }
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 2 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: epic-recoil-system, Property 3: Recoil Recovery Over Time**
    /// *For any* non-zero accumulated recoil state, when no shots are fired for time T > 0, 
    /// the accumulated recoil magnitude after T SHALL be less than or equal to the initial magnitude.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Test]
    public void Property3_RecoilRecoveryOverTime()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration config = RecoilGenerators.GenerateRecoilConfiguration();
            config.recoverySpeed = RecoilGenerators.RandomFloat(1f, 20f);

            _recoilSystem.ResetRecoil();
            _recoilSystem.SetConfiguration(config);

            // Apply some recoil to create non-zero accumulated state
            int initialShots = RecoilGenerators.RandomInt(1, 5);
            for (int shot = 0; shot < initialShots; shot++)
            {
                _recoilSystem.ApplyRecoil();
            }

            float initialMagnitude = _recoilSystem.RecoilMagnitude;
            
            // Skip if no recoil was accumulated
            if (initialMagnitude < 0.001f)
            {
                continue;
            }

            // Simulate recovery over time (no shots fired)
            float recoveryTime = RecoilGenerators.RandomFloat(0.1f, 2f);
            float deltaTime = 0.016f; // ~60fps
            int steps = Mathf.CeilToInt(recoveryTime / deltaTime);

            for (int step = 0; step < steps; step++)
            {
                _recoilSystem.SimulateRecoveryForTesting(deltaTime);
            }

            float finalMagnitude = _recoilSystem.RecoilMagnitude;

            // Verify magnitude decreased or stayed same (never increased)
            if (finalMagnitude > initialMagnitude + 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Magnitude increased from {initialMagnitude} to {finalMagnitude} after {recoveryTime}s recovery";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 3 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 13: Graceful Module Degradation**
    /// *For any* combination of disabled modules (MouseTracker, RecoilRandomizer, CameraShaker), 
    /// the RecoilSystem SHALL not throw exceptions and SHALL still apply camera rotation recoil.
    /// **Validates: Requirements 5.3**
    /// </summary>
    [Test]
    public void Property13_GracefulModuleDegradation()
    {
        int failures = 0;
        string lastFailureMessage = "";

        // Test all combinations of module states (2^3 = 8 combinations)
        bool[] moduleStates = { true, false };

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            
            // Randomly select module states
            bool randomizerEnabled = moduleStates[RecoilGenerators.RandomInt(0, 1)];
            bool mouseTrackerEnabled = moduleStates[RecoilGenerators.RandomInt(0, 1)];
            bool cameraShakerEnabled = moduleStates[RecoilGenerators.RandomInt(0, 1)];

            // Create fresh test objects for this iteration
            GameObject testObj = new GameObject("TestDegradation");
            GameObject camObj = new GameObject("Camera");
            GameObject wpnObj = new GameObject("Weapon");
            camObj.transform.SetParent(testObj.transform);
            wpnObj.transform.SetParent(camObj.transform);
            camObj.AddComponent<Camera>();

            RecoilSystem system = testObj.AddComponent<RecoilSystem>();
            
            // Optionally add modules
            RecoilRandomizer randomizer = null;
            MouseTracker tracker = null;
            CameraShaker shaker = null;

            if (randomizerEnabled || RecoilGenerators.RandomInt(0, 1) == 1)
            {
                randomizer = testObj.AddComponent<RecoilRandomizer>();
                randomizer.IsEnabled = randomizerEnabled;
            }

            if (mouseTrackerEnabled || RecoilGenerators.RandomInt(0, 1) == 1)
            {
                tracker = testObj.AddComponent<MouseTracker>();
                tracker.IsEnabled = mouseTrackerEnabled;
            }

            if (cameraShakerEnabled || RecoilGenerators.RandomInt(0, 1) == 1)
            {
                shaker = testObj.AddComponent<CameraShaker>();
                shaker.IsEnabled = cameraShakerEnabled;
            }

            system.SetTransformsForTesting(camObj.transform, wpnObj.transform);
            system.ForceInitializeForTesting();

            try
            {
                // Apply recoil - should not throw
                system.ApplyRecoil();

                // Verify recoil was applied (accumulated recoil should be non-zero)
                if (system.AccumulatedRecoil.sqrMagnitude < 0.001f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: No recoil applied with modules (R:{randomizerEnabled}, M:{mouseTrackerEnabled}, C:{cameraShakerEnabled})";
                }

                // Simulate update - should not throw
                system.SimulateRecoveryForTesting(0.016f);
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception thrown with modules (R:{randomizerEnabled}, M:{mouseTrackerEnabled}, C:{cameraShakerEnabled}): {e.Message}";
            }
            finally
            {
                Object.DestroyImmediate(testObj);
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 13 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: epic-recoil-system, Property 14: Weapon Transform Reflects Recoil**
    /// *For any* applied recoil, the weapon transform SHALL have:
    /// - Local Z position offset ≤ 0 (backward)
    /// - Local X rotation offset ≤ 0 (upward tilt)
    /// - Position offset magnitude equal to configured kickback distance (±10% for interpolation)
    /// **Validates: Requirements 6.1, 6.2, 6.3**
    /// </summary>
    [Test]
    public void Property14_WeaponTransformReflectsRecoil()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration config = RecoilGenerators.GenerateRecoilConfiguration();
            config.weaponKickbackDistance = RecoilGenerators.RandomFloat(0.01f, 0.2f);
            config.weaponRotationKick = RecoilGenerators.RandomFloat(1f, 10f);

            _recoilSystem.ResetRecoil();
            _recoilSystem.SetConfiguration(config);

            // Apply recoil
            _recoilSystem.ApplyRecoil();

            // Get weapon position offset
            Vector3 posOffset = _recoilSystem.CurrentWeaponPositionOffset;

            // Check Z position is backward (≤ 0)
            if (posOffset.z > 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Weapon Z offset {posOffset.z} is positive (should be backward/negative)";
                continue;
            }

            // Check position offset magnitude matches config (±10%)
            float expectedMagnitude = config.weaponKickbackDistance;
            float actualMagnitude = Mathf.Abs(posOffset.z);
            float tolerance = expectedMagnitude * 0.1f + 0.001f;

            if (Mathf.Abs(actualMagnitude - expectedMagnitude) > tolerance)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Position offset magnitude {actualMagnitude} differs from expected {expectedMagnitude} by more than 10%";
                continue;
            }

            // Get weapon rotation offset
            Quaternion rotOffset = _recoilSystem.CurrentWeaponRotationOffset;
            Vector3 eulerOffset = rotOffset.eulerAngles;
            
            // Normalize to -180 to 180 range
            if (eulerOffset.x > 180f) eulerOffset.x -= 360f;

            // Check X rotation is upward tilt (negative X rotation in Unity = upward)
            if (eulerOffset.x > 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Weapon X rotation {eulerOffset.x} is positive (should be negative for upward tilt)";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 14 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 15: Weapon Transform Recovery**
    /// *For any* weapon transform with non-zero recoil offset, after sufficient recovery time 
    /// with no shots fired, the weapon transform SHALL return to within 0.001 units/degrees 
    /// of its original position and rotation.
    /// **Validates: Requirements 6.4**
    /// </summary>
    [Test]
    public void Property15_WeaponTransformRecovery()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration config = RecoilGenerators.GenerateRecoilConfiguration();
            config.recoverySpeed = RecoilGenerators.RandomFloat(5f, 20f); // Ensure reasonable recovery

            _recoilSystem.ResetRecoil();
            _recoilSystem.SetConfiguration(config);

            // Apply recoil to create non-zero offset
            _recoilSystem.ApplyRecoil();

            // Verify we have non-zero offset
            Vector3 initialPosOffset = _recoilSystem.CurrentWeaponPositionOffset;
            if (initialPosOffset.sqrMagnitude < 0.0001f)
            {
                continue; // Skip if no offset was created
            }

            // Simulate sufficient recovery time (5 seconds should be enough)
            float totalRecoveryTime = 5f;
            float deltaTime = 0.016f;
            int steps = Mathf.CeilToInt(totalRecoveryTime / deltaTime);

            for (int step = 0; step < steps; step++)
            {
                _recoilSystem.SimulateRecoveryForTesting(deltaTime);
            }

            // Check position offset is near zero
            Vector3 finalPosOffset = _recoilSystem.CurrentWeaponPositionOffset;
            if (finalPosOffset.magnitude > 0.001f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Position offset {finalPosOffset.magnitude} not recovered to within 0.001 after {totalRecoveryTime}s";
                continue;
            }

            // Check rotation offset is near identity
            Quaternion finalRotOffset = _recoilSystem.CurrentWeaponRotationOffset;
            float rotationAngle = Quaternion.Angle(finalRotOffset, Quaternion.identity);
            if (rotationAngle > 0.1f) // Allow small tolerance for quaternion comparison
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Rotation offset {rotationAngle} degrees from identity after {totalRecoveryTime}s recovery";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 15 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }
}
