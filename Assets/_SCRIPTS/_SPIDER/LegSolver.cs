using UnityEngine;

/// <summary>
/// Module that performs 1-3 bone IK calculations to position leg joints toward target positions.
/// Supports configurable bone counts: 1 (direct/hop), 2 (hip-foot), 3 (hip-knee-foot).
/// </summary>
public class LegSolver : MonoBehaviour, ISpiderModule
{
    [Header("Configuration")]
    [Tooltip("Number of bones per leg (1 = direct, 2 = hip-foot, 3 = hip-knee-foot)")]
    [Range(1, 3)]
    [SerializeField] private int _boneCount = 3;

    [Tooltip("Tolerance for target position accuracy (meters)")]
    [SerializeField] private float _positionTolerance = 0.01f;

    [Header("Segment Lengths")]
    [Tooltip("Upper leg segment length (hip to knee)")]
    [SerializeField] private float _upperLegLength = 0.3f;
    
    [Tooltip("Lower leg segment length (knee to foot)")]
    [SerializeField] private float _lowerLegLength = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool _drawDebugGizmos = false;

    private SpiderIKSystem _system;
    private bool _isEnabled = true;

    /// <summary>
    /// Gets or sets the number of bones per leg (1, 2, or 3).
    /// </summary>
    public int BoneCount
    {
        get => _boneCount;
        set => _boneCount = Mathf.Clamp(value, 1, 3);
    }

    /// <summary>
    /// Gets or sets the upper leg segment length.
    /// </summary>
    public float UpperLegLength
    {
        get => _upperLegLength;
        set => _upperLegLength = Mathf.Max(0.01f, value);
    }

    /// <summary>
    /// Gets or sets the lower leg segment length.
    /// </summary>
    public float LowerLegLength
    {
        get => _lowerLegLength;
        set => _lowerLegLength = Mathf.Max(0.01f, value);
    }


    /// <summary>
    /// Gets or sets whether this module is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// Initializes the module with a reference to the parent SpiderIKSystem.
    /// </summary>
    public void Initialize(SpiderIKSystem system)
    {
        _system = system;
        if (_system != null && _system.Config != null)
        {
            _boneCount = _system.Config.boneCount;
            float totalLength = _system.Config.legLength;
            float hipRatio = _system.Config.hipRatio;
            _upperLegLength = totalLength * hipRatio;
            _lowerLegLength = totalLength * (1f - hipRatio);
        }
    }

    /// <summary>
    /// Called every frame to update the module's state.
    /// </summary>
    public void OnUpdate(float deltaTime) { }

    /// <summary>
    /// Called every fixed update for physics-related processing.
    /// </summary>
    public void OnFixedUpdate(float fixedDeltaTime) { }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        if (_system != null && _system.Config != null)
        {
            _boneCount = _system.Config.boneCount;
        }
    }

    /// <summary>
    /// Sets segment lengths directly (useful for testing).
    /// </summary>
    public void SetSegmentLengths(float upper, float lower)
    {
        _upperLegLength = Mathf.Max(0.01f, upper);
        _lowerLegLength = Mathf.Max(0.01f, lower);
    }

    /// <summary>
    /// Solves IK for a leg, dispatching to the appropriate method based on bone count.
    /// </summary>
    public void SolveIK(LegData leg, Vector3 target, Vector3 bodyCenter)
    {
        if (leg == null || leg.foot == null) return;

        int activeBones = leg.ActiveBoneCount > 0 ? leg.ActiveBoneCount : _boneCount;

        switch (activeBones)
        {
            case 1:
                SolveOneBone(leg, target);
                break;
            case 2:
                SolveTwoBone(leg, target, bodyCenter);
                break;
            case 3:
            default:
                SolveThreeBone(leg, target, bodyCenter);
                break;
        }
    }

    /// <summary>
    /// One-bone IK: Direct positioning toward target (hop/jump style).
    /// </summary>
    public void SolveOneBone(LegData leg, Vector3 target)
    {
        if (leg == null || leg.foot == null) return;

        Transform pivot = leg.hip != null ? leg.hip : (leg.root != null ? leg.root : leg.foot.parent);
        if (pivot == null)
        {
            leg.foot.position = target;
            return;
        }

        float totalLength = _upperLegLength + _lowerLegLength;
        Vector3 toTarget = target - pivot.position;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget <= totalLength && distanceToTarget > 0.001f)
        {
            leg.foot.position = target;
        }
        else if (distanceToTarget > 0.001f)
        {
            // Extend fully toward target
            leg.foot.position = pivot.position + toTarget.normalized * totalLength;
        }

        // Orient foot
        Vector3 direction = (leg.foot.position - pivot.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            leg.foot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }


    /// <summary>
    /// Two-bone IK: Hip-foot chain with implicit knee direction.
    /// </summary>
    public void SolveTwoBone(LegData leg, Vector3 target, Vector3 bodyCenter)
    {
        if (leg == null || leg.hip == null || leg.foot == null) return;

        Vector3 hipPos = leg.hip.position;
        float totalLength = _upperLegLength + _lowerLegLength;
        
        Vector3 toTarget = target - hipPos;
        float distanceToTarget = toTarget.magnitude;

        // Calculate outward direction for knee bend
        Vector3 outwardDir = CalculateOutwardDirection(hipPos, bodyCenter, toTarget);

        Vector3 finalFootPos;
        Vector3 orientTarget;

        if (distanceToTarget < 0.001f)
        {
            // Target at hip - bend knee outward
            finalFootPos = hipPos + outwardDir * 0.01f;
            orientTarget = finalFootPos;
        }
        else if (distanceToTarget >= totalLength)
        {
            // Target out of reach - extend fully toward target
            finalFootPos = hipPos + toTarget.normalized * totalLength;
            orientTarget = finalFootPos;
        }
        else if (distanceToTarget < Mathf.Abs(_upperLegLength - _lowerLegLength) + 0.001f)
        {
            // Target too close - position at target, fold leg
            finalFootPos = target;
            orientTarget = target;
        }
        else
        {
            // Target within reach - solve with law of cosines
            finalFootPos = target;
            orientTarget = CalculateKneePosition(hipPos, target, _upperLegLength, _lowerLegLength, outwardDir);
        }

        // Orient hip first, then set foot position (foot is child of hip)
        OrientJoint(leg.hip, orientTarget, outwardDir);
        
        // Set foot position in world space after hip orientation
        leg.foot.position = finalFootPos;
    }

    /// <summary>
    /// Three-bone IK: Hip-knee-foot chain with explicit knee positioning.
    /// </summary>
    public void SolveThreeBone(LegData leg, Vector3 target, Vector3 bodyCenter)
    {
        if (leg == null || leg.hip == null || leg.foot == null) return;

        Vector3 hipPos = leg.hip.position;
        float totalLength = _upperLegLength + _lowerLegLength;
        
        Vector3 toTarget = target - hipPos;
        float distanceToTarget = toTarget.magnitude;

        // Calculate outward direction for knee bend
        Vector3 outwardDir = CalculateOutwardDirection(hipPos, bodyCenter, toTarget);

        Vector3 kneePos;
        Vector3 finalFootPos;

        if (distanceToTarget < 0.001f)
        {
            // Target at hip - bend knee outward
            kneePos = hipPos + outwardDir * _upperLegLength;
            finalFootPos = kneePos - Vector3.up * _lowerLegLength;
        }
        else if (distanceToTarget >= totalLength)
        {
            // Target out of reach - extend fully toward target
            Vector3 direction = toTarget.normalized;
            kneePos = hipPos + direction * _upperLegLength;
            finalFootPos = hipPos + direction * totalLength;
        }
        else if (distanceToTarget < Mathf.Abs(_upperLegLength - _lowerLegLength) + 0.001f)
        {
            // Target too close - fold leg with knee bent outward
            kneePos = hipPos + outwardDir * _upperLegLength * 0.7f + Vector3.down * _upperLegLength * 0.3f;
            finalFootPos = target;
        }
        else
        {
            // Target within reach - calculate knee position
            kneePos = CalculateKneePosition(hipPos, target, _upperLegLength, _lowerLegLength, outwardDir);
            finalFootPos = target;
        }

        // Orient joints first (parent to child order)
        OrientJoint(leg.hip, kneePos, outwardDir);
        
        // Position and orient knee if it exists
        if (leg.knee != null)
        {
            leg.knee.position = kneePos;
            OrientJoint(leg.knee, finalFootPos, outwardDir);
        }

        // Set foot position last (after all parent transforms are set)
        leg.foot.position = finalFootPos;
    }


    /// <summary>
    /// Calculates the knee position using the law of cosines.
    /// The knee is positioned on the outward side of the hip-foot line.
    /// </summary>
    private Vector3 CalculateKneePosition(Vector3 hipPos, Vector3 targetPos, 
        float upperLength, float lowerLength, Vector3 outwardDir)
    {
        Vector3 toTarget = targetPos - hipPos;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget < 0.001f)
        {
            return hipPos + outwardDir * upperLength;
        }

        // Law of cosines: find angle at hip
        // a = upper, b = distance to target, c = lower
        // cos(angle) = (a² + b² - c²) / (2ab)
        float a = upperLength;
        float b = distanceToTarget;
        float c = lowerLength;

        float cosAngle = (a * a + b * b - c * c) / (2f * a * b);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        float angle = Mathf.Acos(cosAngle);

        // Get the forward direction (hip to target)
        Vector3 forward = toTarget.normalized;
        
        // We need to rotate forward toward outwardDir by the calculated angle
        // The rotation axis should be perpendicular to both forward and outward
        // Cross(forward, outward) gives us an axis that rotates forward toward outward
        Vector3 rotationAxis = Vector3.Cross(forward, outwardDir);
        
        if (rotationAxis.sqrMagnitude < 0.001f)
        {
            // forward and outward are parallel or anti-parallel
            // Use a fallback axis perpendicular to forward
            rotationAxis = Vector3.Cross(forward, Vector3.up);
            if (rotationAxis.sqrMagnitude < 0.001f)
            {
                rotationAxis = Vector3.Cross(forward, Vector3.right);
            }
        }
        rotationAxis.Normalize();

        // Rotate forward by the angle around the rotation axis
        // This rotates the knee direction toward the outward side
        Vector3 kneeDirection = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis) * forward;

        return hipPos + kneeDirection * upperLength;
    }

    /// <summary>
    /// Calculates the outward direction for knee bend (away from body center).
    /// </summary>
    private Vector3 CalculateOutwardDirection(Vector3 hipPos, Vector3 bodyCenter, Vector3 toTarget)
    {
        // Direction from body center to hip (horizontal component)
        Vector3 hipOutward = hipPos - bodyCenter;
        hipOutward.y = 0;
        
        if (hipOutward.sqrMagnitude < 0.001f)
        {
            // Hip is directly above/below body center
            // Use perpendicular to target direction
            hipOutward = Vector3.Cross(toTarget, Vector3.up);
            if (hipOutward.sqrMagnitude < 0.001f)
            {
                hipOutward = Vector3.right;
            }
        }

        return hipOutward.normalized;
    }

    /// <summary>
    /// Orients a joint to look toward a target position.
    /// </summary>
    private void OrientJoint(Transform joint, Vector3 targetPos, Vector3 upHint)
    {
        if (joint == null) return;

        Vector3 direction = targetPos - joint.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            joint.rotation = Quaternion.LookRotation(direction.normalized, upHint);
        }
    }

    /// <summary>
    /// Gets the total leg chain length.
    /// </summary>
    public float GetTotalLegLength()
    {
        return _upperLegLength + _lowerLegLength;
    }

    /// <summary>
    /// Checks if a target is within reach of the leg.
    /// </summary>
    public bool IsTargetReachable(Vector3 hipPosition, Vector3 target)
    {
        float distance = Vector3.Distance(hipPosition, target);
        return distance <= (_upperLegLength + _lowerLegLength);
    }

    /// <summary>
    /// Applies configuration from IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _boneCount = Mathf.Clamp(config.boneCount, 1, 3);
        float totalLength = config.legLength;
        _upperLegLength = totalLength * config.hipRatio;
        _lowerLegLength = totalLength * (1f - config.hipRatio);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_drawDebugGizmos) return;
        // Debug visualization can be added here
    }
#endif
}
