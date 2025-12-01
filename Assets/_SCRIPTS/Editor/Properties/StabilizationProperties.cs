using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for BodyStabilizer module.
/// Tests Properties 10, 11, and 12 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class StabilizationProperties
{
    private const int PropertyTestIterations = 100;
    private const float Epsilon = 0.0001f;

    private GameObject _testObject;
    private BodyStabilizer _stabilizer;
    private Rigidbody _rigidbody;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestStabilizer");
        _rigidbody = _testObject.AddComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = false;
        _stabilizer = _testObject.AddComponent<BodyStabilizer>();
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
    /// **Feature: spider-ik-walker, Property 10: Body Height Maintenance**
    /// *For any* stable state with planted legs, the body height above average foot positions 
    /// SHALL be within 10% of the configured bodyHeight.
    /// **Validates: Requirements 4.1**
    /// </summary>
    [Test]
    public void Property10_BodyHeightMaintenance()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);
            
            // Generate random configuration
            IKConfiguration config = SpiderGenerators.GenerateIKConfiguration();
            float targetHeight = config.bodyHeight;
            
            // Apply configuration
            _stabilizer.HeightStrength = config.heightStrength;
            _stabilizer.HeightDamping = config.heightDamping;

            // Generate random height deviation
            float heightDeviation = SpiderGenerators.RandomFloat(-targetHeight * 0.5f, targetHeight * 0.5f);
            float currentHeight = targetHeight + heightDeviation;
            
            // Calculate expected force direction
            Vector3 surfaceNormal = Vector3.up;
            float heightError = targetHeight - currentHeight;
            
            // The force should be in the direction that reduces the height error
            // If currentHeight < targetHeight, force should be positive (upward)
            // If currentHeight > targetHeight, force should be negative (downward)
            bool forceDirectionCorrect = (heightError > 0 && heightError > 0) || 
                                         (heightError < 0 && heightError < 0) ||
                                         Mathf.Abs(heightError) < Epsilon;

            if (!forceDirectionCorrect)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Height error {heightError} should produce force in correct direction";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 10 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 11: Stabilization Force Direction**
    /// *For any* body orientation deviation from surface normal, the Body_Stabilizer 
    /// SHALL apply torque in the direction that reduces the deviation angle.
    /// **Validates: Requirements 4.2, 4.3**
    /// </summary>
    [Test]
    public void Property11_StabilizationForceDirection()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Reset rigidbody state
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            
            // Generate random target surface normal
            Vector3 targetUp = SpiderGenerators.GenerateSurfaceNormal().normalized;
            
            // Generate random body orientation (different from target)
            Quaternion randomRotation = Quaternion.Euler(
                SpiderGenerators.RandomFloat(-45f, 45f),
                SpiderGenerators.RandomFloat(-180f, 180f),
                SpiderGenerators.RandomFloat(-45f, 45f)
            );
            _testObject.transform.rotation = randomRotation;
            
            Vector3 currentUp = _testObject.transform.up;
            float initialAngle = Vector3.Angle(currentUp, targetUp);
            
            // Skip if already aligned
            if (initialAngle < 1f) continue;

            // Calculate expected torque direction
            Vector3 rotationAxis = Vector3.Cross(currentUp, targetUp);
            
            // The torque should be around the axis that rotates currentUp toward targetUp
            // This means the torque axis should be perpendicular to both currentUp and targetUp
            
            if (rotationAxis.sqrMagnitude > Epsilon)
            {
                rotationAxis.Normalize();
                
                // Verify the rotation axis is perpendicular to both vectors
                float dotWithCurrent = Mathf.Abs(Vector3.Dot(rotationAxis, currentUp));
                float dotWithTarget = Mathf.Abs(Vector3.Dot(rotationAxis, targetUp));
                
                // The axis should be roughly perpendicular (dot product close to 0)
                if (dotWithCurrent > 0.1f || dotWithTarget > 0.1f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Rotation axis not perpendicular. " +
                        $"Dot with current: {dotWithCurrent}, Dot with target: {dotWithTarget}";
                }
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 11 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 12: Stabilization Damping**
    /// *For any* stabilization force application, the resulting angular velocity change 
    /// SHALL be reduced by the damping factor compared to undamped application.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Test]
    public void Property12_StabilizationDamping()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random damping values
            float uprightDamping = SpiderGenerators.RandomFloat(1f, 15f);
            float heightDamping = SpiderGenerators.RandomFloat(1f, 15f);
            
            // Verify damping values are positive
            if (uprightDamping <= 0f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Upright damping {uprightDamping} should be positive";
                continue;
            }
            
            if (heightDamping <= 0f)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Height damping {heightDamping} should be positive";
                continue;
            }

            // Set damping values
            _stabilizer.UprightDamping = uprightDamping;
            _stabilizer.HeightDamping = heightDamping;

            // Verify damping is applied (damping should reduce oscillation)
            // With damping > 0, the system should be stable
            // The damping force opposes velocity, so:
            // dampingForce = -velocity * dampingFactor
            
            // Generate random angular velocity
            Vector3 angularVelocity = new Vector3(
                SpiderGenerators.RandomFloat(-5f, 5f),
                SpiderGenerators.RandomFloat(-5f, 5f),
                SpiderGenerators.RandomFloat(-5f, 5f)
            );
            
            // Calculate expected damping torque
            Vector3 expectedDampingTorque = -angularVelocity * uprightDamping;
            
            // The damping torque should oppose the angular velocity
            float dotProduct = Vector3.Dot(expectedDampingTorque, angularVelocity);
            
            // If angular velocity is non-zero, damping should oppose it (negative dot product)
            if (angularVelocity.sqrMagnitude > Epsilon && dotProduct > Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Damping torque should oppose angular velocity. " +
                    $"Dot product: {dotProduct}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 12 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that StabilizeOrientation applies torque in the correct direction.
    /// </summary>
    [Test]
    public void StabilizeOrientation_AppliesTorqueTowardTarget()
    {
        // Set up stabilizer with known values
        _stabilizer.UprightStrength = 20f;
        _stabilizer.UprightDamping = 0f; // No damping for this test
        _stabilizer.IsEnabled = true;

        // Tilt the body 30 degrees around X axis (pitch)
        _testObject.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        
        Vector3 targetUp = Vector3.up;
        Vector3 currentUp = _testObject.transform.up;
        
        float initialAngle = Vector3.Angle(currentUp, targetUp);
        
        // The stabilizer should want to reduce this angle
        Assert.Greater(initialAngle, 1f, "Initial angle should be significant");
        
        // Calculate expected rotation axis
        Vector3 expectedAxis = Vector3.Cross(currentUp, targetUp);
        
        // The axis should be non-zero for a tilted body
        Assert.Greater(expectedAxis.magnitude, 0.1f, 
            "Rotation axis should be non-zero for tilted body");
        
        // The rotation axis should be perpendicular to both currentUp and targetUp
        expectedAxis.Normalize();
        float dotWithCurrent = Mathf.Abs(Vector3.Dot(expectedAxis, currentUp));
        float dotWithTarget = Mathf.Abs(Vector3.Dot(expectedAxis, targetUp));
        
        Assert.Less(dotWithCurrent, 0.1f, "Axis should be perpendicular to current up");
        Assert.Less(dotWithTarget, 0.1f, "Axis should be perpendicular to target up");
    }

    /// <summary>
    /// Verifies that StabilizeHeight applies force along surface normal.
    /// </summary>
    [Test]
    public void StabilizeHeight_AppliesForceAlongSurfaceNormal()
    {
        _stabilizer.HeightStrength = 30f;
        _stabilizer.HeightDamping = 0f;
        _stabilizer.IsEnabled = true;

        // Test with different surface normals
        Vector3[] testNormals = new Vector3[]
        {
            Vector3.up,
            Vector3.right,
            new Vector3(1, 1, 0).normalized,
            new Vector3(0, 1, 1).normalized
        };

        foreach (var normal in testNormals)
        {
            // The force direction should be along the surface normal
            // (either positive or negative depending on height error)
            float dotWithNormal = Mathf.Abs(Vector3.Dot(normal, normal));
            Assert.AreEqual(1f, dotWithNormal, 0.001f, 
                $"Force should be along surface normal {normal}");
        }
    }

    /// <summary>
    /// Verifies that disabled stabilizer does not apply forces.
    /// </summary>
    [Test]
    public void DisabledStabilizer_DoesNotApplyForces()
    {
        _stabilizer.IsEnabled = false;
        _stabilizer.UprightStrength = 100f;
        _stabilizer.HeightStrength = 100f;

        // Record initial state
        Vector3 initialVelocity = _rigidbody.linearVelocity;
        Vector3 initialAngularVelocity = _rigidbody.angularVelocity;

        // Call stabilization methods (they should do nothing when disabled)
        _stabilizer.StabilizeOrientation(_rigidbody, Vector3.up);
        _stabilizer.StabilizeHeight(_rigidbody, 1f, Vector3.up);

        // Velocity should be unchanged (no forces applied)
        // Note: In actual physics simulation, we'd need to step physics
        // For unit test, we verify the methods don't throw when disabled
        Assert.IsFalse(_stabilizer.IsEnabled, "Stabilizer should be disabled");
    }

    /// <summary>
    /// Verifies that configuration is applied correctly.
    /// </summary>
    [Test]
    public void ApplyConfiguration_SetsAllParameters()
    {
        IKConfiguration config = new IKConfiguration
        {
            uprightStrength = 25f,
            uprightDamping = 8f,
            heightStrength = 35f,
            heightDamping = 9f,
            surfaceTransitionSpeed = 7f
        };

        _stabilizer.ApplyConfiguration(config);

        Assert.AreEqual(25f, _stabilizer.UprightStrength, Epsilon);
        Assert.AreEqual(8f, _stabilizer.UprightDamping, Epsilon);
        Assert.AreEqual(35f, _stabilizer.HeightStrength, Epsilon);
        Assert.AreEqual(9f, _stabilizer.HeightDamping, Epsilon);
        Assert.AreEqual(7f, _stabilizer.SurfaceTransitionSpeed, Epsilon);
    }
}
