using UnityEngine;

/// <summary>
/// Module that generates smooth step trajectories with configurable arc height and easing.
/// Animates leg steps along parabolic arcs from planted position to target position.
/// Implements ISpiderModule for integration with SpiderIKSystem.
/// </summary>
public class StepAnimator : MonoBehaviour, ISpiderModule
{
    #region Configuration

    [Header("Step Animation")]
    [Tooltip("Maximum height of step arc above linear path")]
    [SerializeField] private float _stepHeight = 0.1f;

    [Tooltip("Steps per second (animation speed)")]
    [SerializeField] private float _stepSpeed = 5f;

    [Tooltip("Easing curve for step animation (0-1 input, 0-1 output)")]
    [SerializeField] private AnimationCurve _easingCurve;

    #endregion

    #region Properties

    public float StepHeight
    {
        get => _stepHeight;
        set => _stepHeight = Mathf.Max(0f, value);
    }

    public float StepSpeed
    {
        get => _stepSpeed;
        set => _stepSpeed = Mathf.Max(0.1f, value);
    }

    public AnimationCurve EasingCurve
    {
        get => _easingCurve;
        set => _easingCurve = value ?? CreateDefaultEasingCurve();
    }

    public bool IsEnabled { get; set; } = true;

    #endregion

    #region State

    private SpiderIKSystem _system;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Initialize default easing curve if not set
        if (_easingCurve == null || _easingCurve.length == 0)
        {
            _easingCurve = CreateDefaultEasingCurve();
        }
    }

    #endregion

    #region ISpiderModule Implementation

    public void Initialize(SpiderIKSystem system)
    {
        _system = system;

        // Ensure easing curve exists
        if (_easingCurve == null || _easingCurve.length == 0)
        {
            _easingCurve = CreateDefaultEasingCurve();
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (!IsEnabled || _system == null || _system.Legs == null) return;

        // Update all stepping legs
        foreach (var leg in _system.Legs)
        {
            if (leg != null && leg.isStepping)
            {
                UpdateStep(leg, deltaTime);
            }
        }
    }

    public void OnFixedUpdate(float fixedDeltaTime)
    {
        // Step animation is handled in Update for smooth visuals
    }

    public void Reset()
    {
        // Reset any stepping legs
        if (_system != null && _system.Legs != null)
        {
            foreach (var leg in _system.Legs)
            {
                if (leg != null)
                {
                    leg.isStepping = false;
                    leg.stepProgress = 0f;
                }
            }
        }
    }

    #endregion


    #region Public Methods

    /// <summary>
    /// Calculates the position along the step trajectory for a given progress value.
    /// Uses a parabolic arc with maximum height at the midpoint (progress = 0.5).
    /// </summary>
    /// <param name="start">Starting position (planted position)</param>
    /// <param name="end">Target position</param>
    /// <param name="progress">Step progress from 0 to 1</param>
    /// <returns>Position along the step arc</returns>
    public Vector3 CalculateStepPosition(Vector3 start, Vector3 end, float progress)
    {
        // Clamp progress to valid range
        progress = Mathf.Clamp01(progress);

        // Apply easing curve if available
        float easedProgress = _easingCurve != null && _easingCurve.length > 0
            ? _easingCurve.Evaluate(progress)
            : progress;

        // Linear interpolation for base position
        Vector3 linearPos = Vector3.Lerp(start, end, easedProgress);

        // Calculate parabolic arc height
        // Height = 4 * stepHeight * progress * (1 - progress)
        // This creates a parabola with maximum at progress = 0.5
        float arcHeight = 4f * _stepHeight * progress * (1f - progress);

        // Add arc height in the up direction (or surface normal if available)
        Vector3 upDirection = Vector3.up;
        if (_system != null)
        {
            // Use surface normal from terrain adapter if available
            var terrainAdapter = _system.GetComponent<TerrainAdapter>();
            if (terrainAdapter != null)
            {
                upDirection = terrainAdapter.CurrentSurfaceNormal;
            }
        }

        return linearPos + upDirection * arcHeight;
    }

    /// <summary>
    /// Updates the step animation for a leg, advancing progress and updating position.
    /// When progress reaches 1.0, the foot is placed exactly at the target position.
    /// </summary>
    /// <param name="leg">The leg data to update</param>
    /// <param name="deltaTime">Time elapsed since last update</param>
    public void UpdateStep(LegData leg, float deltaTime)
    {
        if (leg == null || !leg.isStepping) return;

        // Advance step progress
        leg.stepProgress += _stepSpeed * deltaTime;

        // Check if step is complete
        if (leg.stepProgress >= 1f)
        {
            // Ensure exact target position at completion
            leg.stepProgress = 1f;
            leg.currentTarget = leg.currentTarget; // Keep the target position
        }
        else
        {
            // Calculate current position along arc
            Vector3 startPos = leg.plantedPos;
            Vector3 endPos = leg.currentTarget;

            leg.currentTarget = CalculateStepPosition(startPos, endPos, leg.stepProgress);
        }
    }

    /// <summary>
    /// Initiates a step for the specified leg.
    /// </summary>
    /// <param name="leg">The leg to start stepping</param>
    /// <param name="targetPosition">The target position in local space</param>
    public void StartStep(LegData leg, Vector3 targetPosition)
    {
        if (leg == null) return;

        leg.isStepping = true;
        leg.stepProgress = 0f;
        leg.restTarget = targetPosition;
    }

    /// <summary>
    /// Gets the arc height at a specific progress value.
    /// Maximum height occurs at progress = 0.5.
    /// </summary>
    /// <param name="progress">Step progress from 0 to 1</param>
    /// <returns>Arc height at the given progress</returns>
    public float GetArcHeight(float progress)
    {
        progress = Mathf.Clamp01(progress);
        return 4f * _stepHeight * progress * (1f - progress);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a default smooth easing curve (ease-in-out).
    /// </summary>
    private static AnimationCurve CreateDefaultEasingCurve()
    {
        return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    #endregion

    #region Configuration Sync

    /// <summary>
    /// Updates animator parameters from an IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _stepHeight = config.stepHeight;
        _stepSpeed = config.stepSpeed;
    }

    #endregion
}
