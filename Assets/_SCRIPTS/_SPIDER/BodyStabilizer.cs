using UnityEngine;

/// <summary>
/// Module that maintains body height and orientation relative to leg positions and terrain.
/// Applies corrective forces and torques to keep the spider body stable during locomotion.
/// Implements ISpiderModule for integration with SpiderIKSystem.
/// </summary>
public class BodyStabilizer : MonoBehaviour, ISpiderModule
{
    #region Configuration

    [Header("Orientation Stabilization")]
    [Tooltip("Torque strength for upright orientation")]
    [SerializeField] private float _uprightStrength = 20f;

    [Tooltip("Damping for upright torque to prevent oscillation")]
    [SerializeField] private float _uprightDamping = 6f;

    [Header("Height Stabilization")]
    [Tooltip("Force strength for height maintenance")]
    [SerializeField] private float _heightStrength = 30f;

    [Tooltip("Damping for height force to prevent bouncing")]
    [SerializeField] private float _heightDamping = 6f;

    [Header("Surface Transition")]
    [Tooltip("Rotation speed during surface changes (degrees/second)")]
    [SerializeField] private float _surfaceTransitionSpeed = 5f;

    #endregion

    #region Properties

    public float UprightStrength
    {
        get => _uprightStrength;
        set => _uprightStrength = Mathf.Max(0f, value);
    }

    public float UprightDamping
    {
        get => _uprightDamping;
        set => _uprightDamping = Mathf.Max(0f, value);
    }

    public float HeightStrength
    {
        get => _heightStrength;
        set => _heightStrength = Mathf.Max(0f, value);
    }

    public float HeightDamping
    {
        get => _heightDamping;
        set => _heightDamping = Mathf.Max(0f, value);
    }

    public float SurfaceTransitionSpeed
    {
        get => _surfaceTransitionSpeed;
        set => _surfaceTransitionSpeed = Mathf.Max(0f, value);
    }

    public bool IsEnabled { get; set; } = true;

    #endregion

    #region State

    private SpiderIKSystem _system;
    private Rigidbody _rigidbody;
    private Vector3 _targetSurfaceNormal = Vector3.up;
    private Vector3 _currentSurfaceNormal = Vector3.up;

    #endregion

    #region ISpiderModule Implementation

    public void Initialize(SpiderIKSystem system)
    {
        _system = system;
        _rigidbody = GetComponentInParent<Rigidbody>();

        if (_rigidbody == null)
        {
            Debug.LogWarning("[BodyStabilizer] No Rigidbody found. Physics-based stabilization will be disabled.");
            IsEnabled = false;
        }

        // Initialize surface normal
        _targetSurfaceNormal = Vector3.up;
        _currentSurfaceNormal = Vector3.up;
    }

    public void OnUpdate(float deltaTime)
    {
        // Surface normal interpolation happens in Update for smooth visual transitions
        if (!IsEnabled || _system == null) return;

        // Smoothly interpolate toward target surface normal
        _currentSurfaceNormal = Vector3.Slerp(
            _currentSurfaceNormal,
            _targetSurfaceNormal,
            _surfaceTransitionSpeed * deltaTime
        );
    }

    public void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!IsEnabled || _rigidbody == null || _system == null) return;

        // Calculate target height from average foot positions
        float targetHeight = CalculateTargetHeight();

        // Apply stabilization forces
        StabilizeOrientation(_rigidbody, _currentSurfaceNormal);
        StabilizeHeight(_rigidbody, targetHeight, _currentSurfaceNormal);
    }

    public void Reset()
    {
        _targetSurfaceNormal = Vector3.up;
        _currentSurfaceNormal = Vector3.up;
    }

    #endregion


    #region Public Methods

    /// <summary>
    /// Sets the target surface normal for orientation alignment.
    /// The body will gradually rotate to align with this normal.
    /// </summary>
    /// <param name="surfaceNormal">The target surface normal (should be normalized)</param>
    public void SetTargetSurfaceNormal(Vector3 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude > 0.001f)
        {
            _targetSurfaceNormal = surfaceNormal.normalized;
        }
    }

    /// <summary>
    /// Applies corrective torque to align the body's up vector with the target surface normal.
    /// Uses damping to prevent oscillation.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply torque to</param>
    /// <param name="targetUp">The target up direction (surface normal)</param>
    public void StabilizeOrientation(Rigidbody rb, Vector3 targetUp)
    {
        if (rb == null || _uprightStrength <= 0f) return;

        // Calculate the rotation needed to align body up with target up
        Vector3 currentUp = rb.transform.up;
        
        // Calculate the axis and angle of rotation needed
        Vector3 rotationAxis = Vector3.Cross(currentUp, targetUp);
        float rotationAngle = Vector3.Angle(currentUp, targetUp);

        // Skip if already aligned (avoid division by zero)
        if (rotationAngle < 0.01f) return;

        // Normalize the rotation axis
        if (rotationAxis.sqrMagnitude > 0.001f)
        {
            rotationAxis.Normalize();
        }
        else
        {
            // If vectors are parallel, use any perpendicular axis
            rotationAxis = Vector3.Cross(currentUp, Vector3.forward);
            if (rotationAxis.sqrMagnitude < 0.001f)
            {
                rotationAxis = Vector3.Cross(currentUp, Vector3.right);
            }
            rotationAxis.Normalize();
        }

        // Calculate corrective torque (proportional to angle)
        // Convert angle to radians for torque calculation
        float angleRadians = rotationAngle * Mathf.Deg2Rad;
        Vector3 correctiveTorque = rotationAxis * angleRadians * _uprightStrength;

        // Apply damping based on current angular velocity
        Vector3 dampingTorque = -rb.angularVelocity * _uprightDamping;

        // Apply combined torque
        rb.AddTorque(correctiveTorque + dampingTorque, ForceMode.Acceleration);
    }

    /// <summary>
    /// Applies force along the surface normal to maintain the configured body height
    /// above the average foot positions.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply force to</param>
    /// <param name="targetHeight">The target height above average foot positions</param>
    /// <param name="surfaceNormal">The surface normal direction for force application</param>
    public void StabilizeHeight(Rigidbody rb, float targetHeight, Vector3 surfaceNormal)
    {
        if (rb == null || _heightStrength <= 0f) return;

        // Calculate current height along surface normal
        float currentHeight = CalculateCurrentHeight(surfaceNormal);

        // Calculate height error
        float heightError = targetHeight - currentHeight;

        // Calculate corrective force along surface normal
        Vector3 correctiveForce = surfaceNormal * heightError * _heightStrength;

        // Calculate velocity along surface normal for damping
        float velocityAlongNormal = Vector3.Dot(rb.linearVelocity, surfaceNormal);
        Vector3 dampingForce = -surfaceNormal * velocityAlongNormal * _heightDamping;

        // Apply combined force
        rb.AddForce(correctiveForce + dampingForce, ForceMode.Acceleration);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Calculates the target height based on configuration and average foot positions.
    /// </summary>
    private float CalculateTargetHeight()
    {
        if (_system == null || _system.Config == null)
        {
            return 0.6f; // Default fallback
        }

        return _system.Config.bodyHeight;
    }

    /// <summary>
    /// Calculates the current height of the body above the average foot positions.
    /// </summary>
    private float CalculateCurrentHeight(Vector3 surfaceNormal)
    {
        if (_system == null || _system.Legs == null || _system.Legs.Length == 0)
        {
            return 0f;
        }

        // Calculate average foot position
        Vector3 averageFootPos = Vector3.zero;
        int plantedCount = 0;

        foreach (var leg in _system.Legs)
        {
            if (leg != null && !leg.isStepping)
            {
                averageFootPos += leg.plantedPos;
                plantedCount++;
            }
        }

        if (plantedCount == 0)
        {
            // No planted legs, use current targets
            foreach (var leg in _system.Legs)
            {
                if (leg != null)
                {
                    averageFootPos += leg.currentTarget;
                    plantedCount++;
                }
            }
        }

        if (plantedCount == 0) return 0f;

        averageFootPos /= plantedCount;

        // Calculate height as distance from body to average foot position along surface normal
        Vector3 bodyToFeet = transform.position - averageFootPos;
        return Vector3.Dot(bodyToFeet, surfaceNormal);
    }

    #endregion

    #region Configuration Sync

    /// <summary>
    /// Updates stabilizer parameters from an IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _uprightStrength = config.uprightStrength;
        _uprightDamping = config.uprightDamping;
        _heightStrength = config.heightStrength;
        _heightDamping = config.heightDamping;
        _surfaceTransitionSpeed = config.surfaceTransitionSpeed;
    }

    #endregion
}
