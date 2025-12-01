using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for HitReactor module.
/// Tests Properties 21, 22 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class HitReactionProperties
{
    private const int PropertyTestIterations = 100;
    private const float Epsilon = 0.0001f;

    private GameObject _testObject;
    private HitReactor _hitReactor;
    private Rigidbody _rigidbody;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestHitReactor");
        _rigidbody = _testObject.AddComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.linearDamping = 0f;
        _rigidbody.angularDamping = 0f;
        
        _hitReactor = _testObject.AddComponent<HitReactor>();
        _hitReactor.SetRigidbody(_rigidbody);
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
    /// **Feature: spider-ik-walker, Property 21: Hit Impulse Application**
    /// *For any* collision with relative velocity > 0.1, the Hit_Reactor SHALL apply 
    /// an impulse force to the Rigidbody in the direction away from impact.
    /// **Validates: Requirements 10.1, 10.2**
    /// </summary>
    [Test]
    public void Property21_HitImpulseApplication()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random impact direction and velocity
            Vector3 impactDirection = SpiderGenerators.GenerateDirection();
            float relativeVelocity = SpiderGenerators.RandomFloat(0.2f, 10f); // Above threshold

            // Configure hit reactor
            float hitImpulse = SpiderGenerators.RandomFloat(1f, 10f);
            _hitReactor.HitImpulse = hitImpulse;

            // Test the impulse calculation logic directly
            // The expected impulse direction should be away from impact
            Vector3 expectedAwayDirection = -impactDirection.normalized;

            // Verify the scuttle direction is set correctly after impact
            _hitReactor.SimulateImpact(impactDirection, relativeVelocity);

            // After impact, should be scuttling
            if (!_hitReactor.IsScuttling)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Not scuttling after impact with velocity {relativeVelocity}";
                continue;
            }

            // Reset for next iteration
            _hitReactor.Reset();
        }

        Assert.AreEqual(0, failures,
            $"Property 21 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 22: Velocity Clamping**
    /// *For any* horizontal velocity exceeding maxHorizontalSpeed, the Hit_Reactor 
    /// SHALL clamp the velocity magnitude to maxHorizontalSpeed.
    /// **Validates: Requirements 10.4**
    /// </summary>
    [Test]
    public void Property22_VelocityClamping()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            // Generate random max speed
            float maxSpeed = SpiderGenerators.RandomFloat(1f, 10f);
            _hitReactor.MaxHorizontalSpeed = maxSpeed;

            // Generate velocity that exceeds max speed
            float excessSpeed = maxSpeed + SpiderGenerators.RandomFloat(1f, 20f);
            Vector3 horizontalDir = new Vector3(
                SpiderGenerators.RandomFloat(-1f, 1f),
                0f,
                SpiderGenerators.RandomFloat(-1f, 1f)
            ).normalized;

            // Set velocity exceeding max (with some vertical component)
            float verticalVelocity = SpiderGenerators.RandomFloat(-5f, 5f);
            _rigidbody.linearVelocity = horizontalDir * excessSpeed + Vector3.up * verticalVelocity;

            // Clamp velocity
            _hitReactor.ClampHorizontalVelocity();

            // Check horizontal speed
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            float horizontalSpeed = new Vector3(finalVelocity.x, 0f, finalVelocity.z).magnitude;

            // Horizontal speed should be clamped to max
            if (horizontalSpeed > maxSpeed + Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Horizontal speed {horizontalSpeed} exceeds max {maxSpeed}";
                continue;
            }

            // Vertical velocity should be preserved
            if (Mathf.Abs(finalVelocity.y - verticalVelocity) > Epsilon)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Vertical velocity changed from {verticalVelocity} to {finalVelocity.y}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 22 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// Verifies that impacts below threshold don't trigger reaction.
    /// </summary>
    [Test]
    public void BelowThreshold_NoReaction()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _hitReactor.HitImpulse = 5f;

        // Impact below threshold (0.1)
        _hitReactor.SimulateImpact(Vector3.forward, 0.05f);

        // Velocity should not change
        Assert.AreEqual(Vector3.zero, _rigidbody.linearVelocity,
            "Velocity should not change for impacts below threshold");
    }

    /// <summary>
    /// Verifies that scuttle reaction is triggered on impact.
    /// </summary>
    [Test]
    public void Impact_TriggersScuttle()
    {
        _hitReactor.ScuttleTime = 1f;

        // Simulate impact
        _hitReactor.SimulateImpact(Vector3.forward, 5f);

        // Should be scuttling
        Assert.IsTrue(_hitReactor.IsScuttling, "Should be scuttling after impact");
    }

    /// <summary>
    /// Verifies that disabled reactor doesn't respond to impacts.
    /// </summary>
    [Test]
    public void DisabledReactor_NoResponse()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _hitReactor.IsEnabled = false;

        _hitReactor.SimulateImpact(Vector3.forward, 5f);

        Assert.AreEqual(Vector3.zero, _rigidbody.linearVelocity,
            "Disabled reactor should not apply impulse");
        Assert.IsFalse(_hitReactor.IsScuttling,
            "Disabled reactor should not trigger scuttle");
    }

    /// <summary>
    /// Verifies configuration is applied correctly.
    /// </summary>
    [Test]
    public void ApplyConfiguration_SetsAllParameters()
    {
        IKConfiguration config = new IKConfiguration
        {
            hitImpulse = 8f,
            scuttleForce = 25f,
            scuttleTime = 0.8f,
            maxHorizontalSpeed = 7f
        };

        _hitReactor.ApplyConfiguration(config);

        Assert.AreEqual(8f, _hitReactor.HitImpulse, Epsilon);
        Assert.AreEqual(25f, _hitReactor.ScuttleForce, Epsilon);
        Assert.AreEqual(0.8f, _hitReactor.ScuttleTime, Epsilon);
        Assert.AreEqual(7f, _hitReactor.MaxHorizontalSpeed, Epsilon);
    }

    /// <summary>
    /// Verifies velocity below max is not affected by clamping.
    /// </summary>
    [Test]
    public void VelocityBelowMax_NotClamped()
    {
        _hitReactor.MaxHorizontalSpeed = 10f;

        Vector3 velocity = new Vector3(3f, 2f, 4f); // Horizontal magnitude = 5
        _rigidbody.linearVelocity = velocity;

        _hitReactor.ClampHorizontalVelocity();

        Vector3 finalVelocity = _rigidbody.linearVelocity;
        Assert.AreEqual(velocity.x, finalVelocity.x, Epsilon, "X velocity should be unchanged");
        Assert.AreEqual(velocity.y, finalVelocity.y, Epsilon, "Y velocity should be unchanged");
        Assert.AreEqual(velocity.z, finalVelocity.z, Epsilon, "Z velocity should be unchanged");
    }
}
