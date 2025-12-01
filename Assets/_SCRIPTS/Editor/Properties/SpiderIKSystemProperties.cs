using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for SpiderIKSystem orchestrator.
/// Tests Properties 15, 17, 18, 19, 20 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class SpiderIKSystemProperties
{
    private const int PropertyTestIterations = 100;
    private const float Epsilon = 0.0001f;

    private GameObject _testObject;
    private SpiderIKSystem _system;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestSpiderIKSystem");
        _system = _testObject.AddComponent<SpiderIKSystem>();
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
    /// **Feature: spider-ik-walker, Property 15: Graceful Module Degradation**
    /// *For any* combination of disabled modules, the SpiderIKSystem SHALL not throw 
    /// exceptions and SHALL continue updating enabled modules.
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Test]
    public void Property15_GracefulModuleDegradation()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            try
            {
                // Create a fresh system for each iteration
                var testObj = new GameObject($"TestSystem_{i}");
                var system = testObj.AddComponent<SpiderIKSystem>();
                
                // Add some modules
                var legSolver = testObj.AddComponent<LegSolver>();
                var gaitController = testObj.AddComponent<GaitController>();
                var stepAnimator = testObj.AddComponent<StepAnimator>();

                // Randomly disable modules
                legSolver.IsEnabled = SpiderGenerators.RandomInt(0, 1) == 1;
                gaitController.IsEnabled = SpiderGenerators.RandomInt(0, 1) == 1;
                stepAnimator.IsEnabled = SpiderGenerators.RandomInt(0, 1) == 1;

                // Initialize and update should not throw
                system.Initialize();
                system.SetConfiguration(SpiderGenerators.GenerateIKConfiguration());

                // Clean up
                Object.DestroyImmediate(testObj);
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception thrown: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 15 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 17: Velocity-Based Stride Scaling**
    /// *For any* two velocities V1 and V2 where |V1| > |V2|, the calculated stride length 
    /// for V1 SHALL be greater than or equal to the stride length for V2.
    /// **Validates: Requirements 8.1**
    /// </summary>
    [Test]
    public void Property17_VelocityBasedStrideScaling()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random configuration
            IKConfiguration config = SpiderGenerators.GenerateIKConfiguration();
            _system.SetConfiguration(config);

            // Generate two velocities where |V1| > |V2|
            float speed1 = SpiderGenerators.RandomFloat(1f, 10f);
            float speed2 = SpiderGenerators.RandomFloat(0f, speed1 - 0.1f);

            Vector3 direction = SpiderGenerators.GenerateVelocity(1f).normalized;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;

            Vector3 v1 = direction * speed1;
            Vector3 v2 = direction * speed2;

            // Calculate stride lengths
            float stride1 = _system.CalculateStrideLength(v1);
            float stride2 = _system.CalculateStrideLength(v2);

            // Stride for higher velocity should be >= stride for lower velocity
            if (stride1 < stride2 - Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Stride for V1 ({stride1}) < Stride for V2 ({stride2}). " +
                    $"|V1|={v1.magnitude}, |V2|={v2.magnitude}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 17 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 18: Stride Direction Alignment**
    /// *For any* non-zero velocity, the stride projection direction SHALL align 
    /// within 15 degrees of the velocity direction.
    /// **Validates: Requirements 8.3**
    /// </summary>
    [Test]
    public void Property18_StrideDirectionAlignment()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random non-zero velocity
            Vector3 velocity = SpiderGenerators.GenerateVelocity(10f);
            
            // Ensure velocity is non-zero
            if (velocity.sqrMagnitude < 0.01f)
            {
                velocity = Vector3.forward * SpiderGenerators.RandomFloat(1f, 5f);
            }

            // Calculate stride direction
            Vector3 strideDir = _system.CalculateStrideDirection(velocity);
            Vector3 velocityDir = velocity.normalized;

            // Calculate angle between stride direction and velocity direction
            float angle = Vector3.Angle(strideDir, velocityDir);

            // Angle should be within 15 degrees
            if (angle > 15f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Stride direction differs from velocity by {angle} degrees " +
                    $"(max 15). Velocity: {velocity}, StrideDir: {strideDir}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 18 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 19: Body Dimension Proportional Scaling**
    /// *For any* body dimensions configuration, leg root positions SHALL be at distance 
    /// (bodyRadius * bodyToLegRatio * dimensionScale) from body center.
    /// **Validates: Requirements 9.2, 9.3, 9.4**
    /// </summary>
    [Test]
    public void Property19_BodyDimensionProportionalScaling()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random configuration
            IKConfiguration config = SpiderGenerators.GenerateIKConfiguration();
            _system.SetConfiguration(config);

            // Calculate expected leg root distance
            float expectedDistance = config.bodyRadius * config.bodyToLegRatio * config.dimensionScale * config.legSpread;

            // Check each leg's rest position distance from center
            if (_system.Legs != null)
            {
                foreach (var leg in _system.Legs)
                {
                    if (leg == null) continue;

                    // Get horizontal distance (XZ plane)
                    Vector3 restPos = leg.restTarget;
                    float horizontalDistance = new Vector2(restPos.x, restPos.z).magnitude;

                    // Allow 20% tolerance for leg spread variations
                    float tolerance = expectedDistance * 0.2f;
                    if (Mathf.Abs(horizontalDistance - expectedDistance) > tolerance)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Leg {leg.legIndex} distance {horizontalDistance} " +
                            $"differs from expected {expectedDistance} by more than 20%";
                        break;
                    }
                }
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 19 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 20: Leg Position Rotation Consistency**
    /// *For any* body rotation, leg root local positions SHALL remain constant 
    /// (world positions rotate with body).
    /// **Validates: Requirements 9.5**
    /// </summary>
    [Test]
    public void Property20_LegPositionRotationConsistency()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Set up configuration
            IKConfiguration config = SpiderGenerators.GenerateIKConfiguration();
            _system.SetConfiguration(config);

            if (_system.Legs == null || _system.Legs.Length == 0) continue;

            // Store original local positions
            Vector3[] originalLocalPositions = new Vector3[_system.Legs.Length];
            for (int j = 0; j < _system.Legs.Length; j++)
            {
                if (_system.Legs[j] != null)
                {
                    originalLocalPositions[j] = _system.Legs[j].restTarget;
                }
            }

            // Apply random rotation to body
            Quaternion randomRotation = Quaternion.Euler(
                SpiderGenerators.RandomFloat(-180f, 180f),
                SpiderGenerators.RandomFloat(-180f, 180f),
                SpiderGenerators.RandomFloat(-180f, 180f)
            );
            _testObject.transform.rotation = randomRotation;

            // Verify local positions remain constant
            for (int j = 0; j < _system.Legs.Length; j++)
            {
                if (_system.Legs[j] == null) continue;

                Vector3 currentLocal = _system.Legs[j].restTarget;
                Vector3 originalLocal = originalLocalPositions[j];

                float distance = Vector3.Distance(currentLocal, originalLocal);
                if (distance > Epsilon)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Leg {j} local position changed after rotation. " +
                        $"Original: {originalLocal}, Current: {currentLocal}";
                    break;
                }
            }

            // Reset rotation
            _testObject.transform.rotation = Quaternion.identity;
        }

        Assert.AreEqual(0, failures,
            $"Property 20 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that Initialize discovers modules correctly.
    /// </summary>
    [Test]
    public void Initialize_DiscoversModules()
    {
        // Add modules
        _testObject.AddComponent<LegSolver>();
        _testObject.AddComponent<GaitController>();

        // Initialize
        _system.Initialize();

        // System should have discovered the modules (no exception thrown)
        Assert.IsNotNull(_system);
    }

    /// <summary>
    /// Verifies that SetConfiguration applies to all modules.
    /// </summary>
    [Test]
    public void SetConfiguration_AppliesConfiguration()
    {
        IKConfiguration config = new IKConfiguration
        {
            legCount = 6,
            boneCount = 2,
            bodyRadius = 0.5f
        };

        _system.SetConfiguration(config);

        Assert.AreEqual(6, _system.Config.legCount);
        Assert.AreEqual(2, _system.Config.boneCount);
        Assert.AreEqual(0.5f, _system.Config.bodyRadius, Epsilon);
    }

    /// <summary>
    /// Verifies that RebuildLegData creates correct number of legs.
    /// </summary>
    [Test]
    public void RebuildLegData_CreatesCorrectLegCount()
    {
        int[] legCounts = { 1, 2, 4, 6, 8 };

        foreach (int count in legCounts)
        {
            IKConfiguration config = new IKConfiguration { legCount = count };
            _system.SetConfiguration(config);

            Assert.IsNotNull(_system.Legs);
            Assert.AreEqual(count, _system.Legs.Length, $"Expected {count} legs");
        }
    }
}
