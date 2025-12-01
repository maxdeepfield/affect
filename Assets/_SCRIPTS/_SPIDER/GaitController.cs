using UnityEngine;

/// <summary>
/// Module that manages stepping patterns, diagonal pairing, and timing
/// to ensure stable locomotion for spider-like creatures.
/// Supports 1-8 legs with automatic diagonal group assignment.
/// </summary>
public class GaitController : MonoBehaviour, ISpiderModule
{
    [Header("Configuration")]
    [Tooltip("Distance from rest target to trigger a step")]
    [SerializeField] private float _stepThreshold = 0.4f;

    [Tooltip("Minimum time between steps for the same leg")]
    [SerializeField] private float _stepCooldown = 0.1f;

    [Header("State")]
    [Tooltip("Currently active diagonal group (0 or 1)")]
    [SerializeField] private int _activeStepGroup = 0;

    [Tooltip("Whether any leg is currently stepping")]
    [SerializeField] private bool _isAnyLegStepping = false;

    private SpiderIKSystem _system;
    private bool _isEnabled = true;
    private float _lastGroupSwitchTime = -999f;

    /// <summary>
    /// Gets or sets the step threshold distance.
    /// </summary>
    public float StepThreshold
    {
        get => _stepThreshold;
        set => _stepThreshold = Mathf.Max(0.01f, value);
    }

    /// <summary>
    /// Gets or sets the step cooldown time.
    /// </summary>
    public float StepCooldown
    {
        get => _stepCooldown;
        set => _stepCooldown = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Gets the currently active diagonal step group (0 or 1).
    /// </summary>
    public int ActiveStepGroup => _activeStepGroup;

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
            _stepThreshold = _system.Config.stepThreshold;
        }
    }

    /// <summary>
    /// Called every frame to update the module's state.
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        // Gait updates are typically called explicitly via UpdateGait
    }

    /// <summary>
    /// Called every fixed update for physics-related processing.
    /// </summary>
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        // Gait doesn't require fixed update
    }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        _activeStepGroup = 0;
        _isAnyLegStepping = false;
        _lastGroupSwitchTime = -999f;
    }

    /// <summary>
    /// Updates the gait state for all legs based on current positions and velocity.
    /// </summary>
    /// <param name="legs">Array of leg data</param>
    /// <param name="velocity">Current body velocity</param>
    public void UpdateGait(LegData[] legs, Vector3 velocity)
    {
        if (legs == null || legs.Length == 0) return;

        // Handle single leg (hop mode)
        if (legs.Length == 1)
        {
            UpdateSingleLegGait(legs[0], velocity);
            return;
        }

        // Check if any leg in the active group is still stepping
        _isAnyLegStepping = false;
        foreach (var leg in legs)
        {
            if (leg != null && leg.isStepping && leg.diagonalGroup == _activeStepGroup)
            {
                _isAnyLegStepping = true;
                break;
            }
        }

        // If no legs in active group are stepping, check if we should switch groups
        if (!_isAnyLegStepping)
        {
            // Check if any leg in the OTHER group needs to step
            int otherGroup = 1 - _activeStepGroup;
            bool otherGroupNeedsStep = false;

            foreach (var leg in legs)
            {
                if (leg != null && leg.diagonalGroup == otherGroup)
                {
                    Vector3 desiredTarget = CalculateDesiredTarget(leg, velocity);
                    if (ShouldLegStep(leg, desiredTarget))
                    {
                        otherGroupNeedsStep = true;
                        break;
                    }
                }
            }

            if (otherGroupNeedsStep)
            {
                _activeStepGroup = otherGroup;
                _lastGroupSwitchTime = Time.time;
            }
        }

        // Initiate steps for legs in the active group that need to step
        foreach (var leg in legs)
        {
            if (leg == null) continue;

            if (leg.diagonalGroup == _activeStepGroup && !leg.isStepping)
            {
                Vector3 desiredTarget = CalculateDesiredTarget(leg, velocity);
                if (ShouldLegStep(leg, desiredTarget))
                {
                    InitiateStep(leg, desiredTarget);
                }
            }
        }
    }


    /// <summary>
    /// Updates gait for single-leg hop mode.
    /// </summary>
    private void UpdateSingleLegGait(LegData leg, Vector3 velocity)
    {
        if (leg == null) return;

        if (!leg.isStepping)
        {
            Vector3 desiredTarget = CalculateDesiredTarget(leg, velocity);
            if (ShouldLegStep(leg, desiredTarget))
            {
                InitiateStep(leg, desiredTarget);
            }
        }
    }

    /// <summary>
    /// Calculates the desired target position for a leg based on velocity.
    /// </summary>
    private Vector3 CalculateDesiredTarget(LegData leg, Vector3 velocity)
    {
        if (leg == null || _system == null) return Vector3.zero;

        // Start with rest target in world space
        Vector3 worldRestTarget = leg.restTarget;
        if (_system.transform != null)
        {
            worldRestTarget = _system.transform.TransformPoint(leg.restTarget);
        }

        // Add velocity-based stride projection
        if (velocity.sqrMagnitude > 0.001f && _system.Config != null)
        {
            float strideScale = _system.Config.strideVelocityScale;
            Vector3 strideOffset = velocity.normalized * velocity.magnitude * strideScale;
            worldRestTarget += strideOffset;
        }
        else if (_system.Config != null)
        {
            // If no velocity, still project forward based on body orientation
            Vector3 strideOffset = _system.transform.forward * _system.Config.strideForward;
            worldRestTarget += strideOffset;
        }

        return worldRestTarget;
    }

    /// <summary>
    /// Determines if a leg should initiate a step.
    /// </summary>
    /// <param name="leg">The leg to check</param>
    /// <param name="desiredTarget">The desired target position</param>
    /// <returns>True if the leg should step</returns>
    public bool ShouldLegStep(LegData leg, Vector3 desiredTarget)
    {
        if (leg == null) return false;

        // Don't step if already stepping
        if (leg.isStepping) return false;

        // Check cooldown
        if (Time.time - leg.lastStepTime < _stepCooldown) return false;

        // Check distance from planted position to desired target
        float distance = Vector3.Distance(leg.plantedPos, desiredTarget);
        return distance > _stepThreshold;
    }

    /// <summary>
    /// Initiates a step for a leg.
    /// </summary>
    private void InitiateStep(LegData leg, Vector3 target)
    {
        if (leg == null) return;

        leg.isStepping = true;
        leg.stepProgress = 0f;
        leg.currentTarget = target;
    }

    /// <summary>
    /// Completes a step for a leg (called by StepAnimator when step finishes).
    /// </summary>
    public void CompleteStep(LegData leg)
    {
        if (leg == null) return;

        leg.isStepping = false;
        leg.stepProgress = 0f;
        leg.plantedPos = leg.currentTarget;
        leg.lastStepTime = Time.time;
    }


    /// <summary>
    /// Assigns a leg to a diagonal group based on its local position.
    /// Legs are grouped into two alternating sets for stable walking.
    /// </summary>
    /// <param name="legLocalPosition">Local position of the leg root relative to body</param>
    /// <param name="legCount">Total number of legs (1-8)</param>
    /// <returns>Diagonal group (0 or 1)</returns>
    public int AssignDiagonalGroup(Vector3 legLocalPosition, int legCount)
    {
        // Clamp leg count to valid range
        legCount = Mathf.Clamp(legCount, 1, 8);

        // Single leg always in group 0
        if (legCount == 1) return 0;

        // Determine quadrant based on local position
        // Front-right and back-left are group 0
        // Front-left and back-right are group 1
        bool isRight = legLocalPosition.x >= 0;
        bool isFront = legLocalPosition.z >= 0;

        // Diagonal pairing: opposite corners are in the same group
        if ((isRight && isFront) || (!isRight && !isFront))
        {
            return 0; // Front-right, Back-left
        }
        else
        {
            return 1; // Front-left, Back-right
        }
    }

    /// <summary>
    /// Assigns diagonal groups to all legs based on their positions.
    /// </summary>
    /// <param name="legs">Array of leg data to assign groups to</param>
    public void AssignAllDiagonalGroups(LegData[] legs)
    {
        if (legs == null || _system == null) return;

        int legCount = legs.Length;
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null) continue;

            Vector3 localPos = legs[i].restTarget;
            legs[i].diagonalGroup = AssignDiagonalGroup(localPos, legCount);
            legs[i].legIndex = i;
        }
    }

    /// <summary>
    /// Checks if any leg in a specific group is currently stepping.
    /// </summary>
    public bool IsGroupStepping(LegData[] legs, int group)
    {
        if (legs == null) return false;

        foreach (var leg in legs)
        {
            if (leg != null && leg.diagonalGroup == group && leg.isStepping)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the count of legs in each diagonal group.
    /// </summary>
    public (int group0, int group1) GetGroupCounts(LegData[] legs)
    {
        int group0 = 0;
        int group1 = 0;

        if (legs != null)
        {
            foreach (var leg in legs)
            {
                if (leg == null) continue;
                if (leg.diagonalGroup == 0) group0++;
                else group1++;
            }
        }

        return (group0, group1);
    }

    /// <summary>
    /// Sets the step threshold (for testing).
    /// </summary>
    public void SetStepThreshold(float threshold)
    {
        _stepThreshold = Mathf.Max(0.01f, threshold);
    }

    /// <summary>
    /// Forces the active step group (for testing).
    /// </summary>
    public void SetActiveStepGroup(int group)
    {
        _activeStepGroup = Mathf.Clamp(group, 0, 1);
    }

    /// <summary>
    /// Applies configuration from IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _stepThreshold = config.stepThreshold;
    }
}
