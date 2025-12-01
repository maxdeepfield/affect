using UnityEngine;

/// <summary>
/// Module that responds to physics impacts and impulses.
/// Applies forces to body and triggers leg reactions for evasive movement.
/// </summary>
public class HitReactor : MonoBehaviour, ISpiderModule
{
    [Header("Hit Response")]
    [Tooltip("Impulse force multiplier applied on collision")]
    [SerializeField] private float _hitImpulse = 6f;

    [Tooltip("Force applied during scuttle reaction")]
    [SerializeField] private float _scuttleForce = 30f;

    [Tooltip("Duration of scuttle reaction in seconds")]
    [SerializeField] private float _scuttleTime = 0.6f;

    [Tooltip("Maximum horizontal speed during reaction")]
    [SerializeField] private float _maxHorizontalSpeed = 6f;

    [Tooltip("Minimum relative velocity to trigger reaction")]
    [SerializeField] private float _minImpactVelocity = 0.1f;

    [Header("Step Speed Boost")]
    [Tooltip("Step speed multiplier during scuttle")]
    [SerializeField] private float _scuttleStepSpeedMultiplier = 1.5f;

    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;

    private SpiderIKSystem _system;
    private bool _isEnabled = true;
    private bool _isScuttling;
    private float _scuttleEndTime;
    private Vector3 _scuttleDirection;
    private float _originalStepSpeed;

    #region Properties

    public float HitImpulse
    {
        get => _hitImpulse;
        set => _hitImpulse = Mathf.Max(0f, value);
    }

    public float ScuttleForce
    {
        get => _scuttleForce;
        set => _scuttleForce = Mathf.Max(0f, value);
    }

    public float ScuttleTime
    {
        get => _scuttleTime;
        set => _scuttleTime = Mathf.Max(0f, value);
    }

    public float MaxHorizontalSpeed
    {
        get => _maxHorizontalSpeed;
        set => _maxHorizontalSpeed = Mathf.Max(0f, value);
    }

    public bool IsScuttling => _isScuttling;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    #endregion

    #region ISpiderModule Implementation

    public void Initialize(SpiderIKSystem system)
    {
        _system = system;

        if (_rigidbody == null)
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
        }

        if (_rigidbody == null)
        {
            Debug.LogWarning("[HitReactor] No Rigidbody found. Physics reactions will be disabled.");
        }

        if (_system?.Config != null)
        {
            ApplyConfiguration(_system.Config);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (!_isEnabled) return;

        // Check if scuttle has ended
        if (_isScuttling && Time.time >= _scuttleEndTime)
        {
            EndScuttle();
        }
    }

    public void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!_isEnabled || _rigidbody == null) return;

        // Apply continuous scuttle force
        if (_isScuttling)
        {
            ApplyContinuousScuttleForce(fixedDeltaTime);
        }

        // Clamp horizontal velocity
        ClampHorizontalVelocity();
    }

    public void Reset()
    {
        _isScuttling = false;
        _scuttleEndTime = 0f;
        _scuttleDirection = Vector3.zero;
    }

    #endregion


    #region Collision Handling

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isEnabled || _rigidbody == null) return;

        // Check if impact velocity is significant
        float relativeVelocity = collision.relativeVelocity.magnitude;
        if (relativeVelocity < _minImpactVelocity) return;

        // Calculate away direction from collision
        Vector3 awayDirection = CalculateAwayDirection(collision);

        // Apply impulse force
        ApplyImpulse(awayDirection, relativeVelocity);

        // Start scuttle reaction
        ApplyScuttleReaction(awayDirection);
    }

    /// <summary>
    /// Calculates the direction away from the collision point.
    /// </summary>
    private Vector3 CalculateAwayDirection(Collision collision)
    {
        if (collision.contactCount == 0)
        {
            return -collision.relativeVelocity.normalized;
        }

        // Average contact normal
        Vector3 avgNormal = Vector3.zero;
        foreach (ContactPoint contact in collision.contacts)
        {
            avgNormal += contact.normal;
        }
        avgNormal /= collision.contactCount;

        // Direction away from impact (opposite of contact normal from our perspective)
        return avgNormal.normalized;
    }

    /// <summary>
    /// Applies an impulse force to the rigidbody in the away direction.
    /// </summary>
    private void ApplyImpulse(Vector3 awayDirection, float relativeVelocity)
    {
        if (_rigidbody == null) return;

        // Scale impulse by relative velocity
        float impulseStrength = _hitImpulse * Mathf.Clamp01(relativeVelocity / 5f);
        Vector3 impulse = awayDirection * impulseStrength;

        _rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    #endregion

    #region Scuttle Reaction

    /// <summary>
    /// Initiates a scuttle reaction moving away from the impact direction.
    /// </summary>
    public void ApplyScuttleReaction(Vector3 awayDirection)
    {
        if (!_isEnabled) return;

        _isScuttling = true;
        _scuttleEndTime = Time.time + _scuttleTime;
        _scuttleDirection = awayDirection;

        // Flatten direction for horizontal movement
        _scuttleDirection.y = 0f;
        if (_scuttleDirection.sqrMagnitude > 0.001f)
        {
            _scuttleDirection.Normalize();
        }
        else
        {
            // Random horizontal direction if impact was vertical
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _scuttleDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        // Increase step speed during scuttle (if we have access to step animator)
        // This would be handled by the SpiderIKSystem coordinating with StepAnimator
    }

    /// <summary>
    /// Applies continuous force during scuttle.
    /// </summary>
    private void ApplyContinuousScuttleForce(float deltaTime)
    {
        if (_rigidbody == null || !_isScuttling) return;

        // Apply force in scuttle direction
        Vector3 force = _scuttleDirection * _scuttleForce;
        _rigidbody.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// Ends the scuttle reaction.
    /// </summary>
    private void EndScuttle()
    {
        _isScuttling = false;
        _scuttleDirection = Vector3.zero;
    }

    #endregion

    #region Velocity Clamping

    /// <summary>
    /// Clamps horizontal velocity to maximum speed.
    /// </summary>
    public void ClampHorizontalVelocity()
    {
        if (_rigidbody == null) return;

        Vector3 velocity = _rigidbody.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.magnitude > _maxHorizontalSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * _maxHorizontalSpeed;
            _rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }

    /// <summary>
    /// Gets the current horizontal speed.
    /// </summary>
    public float GetHorizontalSpeed()
    {
        if (_rigidbody == null) return 0f;

        Vector3 velocity = _rigidbody.linearVelocity;
        return new Vector3(velocity.x, 0f, velocity.z).magnitude;
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Applies configuration from IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _hitImpulse = config.hitImpulse;
        _scuttleForce = config.scuttleForce;
        _scuttleTime = config.scuttleTime;
        _maxHorizontalSpeed = config.maxHorizontalSpeed;
    }

    #endregion

    #region Public API for Testing

    /// <summary>
    /// Simulates a collision for testing purposes.
    /// </summary>
    public void SimulateImpact(Vector3 impactDirection, float relativeVelocity)
    {
        if (!_isEnabled || _rigidbody == null) return;
        if (relativeVelocity < _minImpactVelocity) return;

        Vector3 awayDirection = -impactDirection.normalized;
        ApplyImpulse(awayDirection, relativeVelocity);
        ApplyScuttleReaction(awayDirection);
    }

    /// <summary>
    /// Sets the rigidbody reference (useful for testing).
    /// </summary>
    public void SetRigidbody(Rigidbody rb)
    {
        _rigidbody = rb;
    }

    /// <summary>
    /// Gets the rigidbody reference.
    /// </summary>
    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    #endregion
}
