using UnityEngine;

/// <summary>
/// Module that tracks player mouse input to detect compensation attempts
/// and adjusts recoil recovery accordingly.
/// </summary>
public class MouseTracker : MonoBehaviour, IRecoilModule
{
    [Header("Configuration")]
    [Tooltip("Multiplier for mouse compensation effectiveness")]
    [SerializeField]
    private float _compensationMultiplier = 1.5f;

    [Tooltip("Maximum rate at which compensation can accelerate recovery")]
    [SerializeField]
    private float _maxCompensationRate = 2f;

    private RecoilSystem _system;
    private PlayerInputHandler _inputHandler;
    private Vector2 _lastMouseDelta;
    private Vector2 _compensationDelta;
    private float _compensationEffectiveness;
    private Vector2 _currentRecoilDirection;

    /// <summary>
    /// Gets or sets the compensation multiplier.
    /// </summary>
    public float CompensationMultiplier
    {
        get => _compensationMultiplier;
        set => _compensationMultiplier = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Gets or sets the maximum compensation rate.
    /// </summary>
    public float MaxCompensationRate
    {
        get => _maxCompensationRate;
        set => _maxCompensationRate = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Gets the current compensation delta calculated from mouse input.
    /// This represents how much the player is compensating for recoil.
    /// </summary>
    public Vector2 CompensationDelta => _compensationDelta;

    /// <summary>
    /// Gets the current compensation effectiveness (0-maxCompensationRate).
    /// A value of 1.0 means base recovery rate, higher means faster recovery.
    /// </summary>
    public float CompensationEffectiveness => _compensationEffectiveness;

    #region IRecoilModule Implementation

    /// <summary>
    /// Gets or sets whether this module is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Initializes the module with a reference to the parent RecoilSystem.
    /// </summary>
    public void Initialize(RecoilSystem system)
    {
        _system = system;

        // Apply configuration from system if available
        if (_system != null && _system.Config != null)
        {
            _compensationMultiplier = _system.Config.compensationMultiplier;
            _maxCompensationRate = _system.Config.maxCompensationRate;
        }

        // Try to find PlayerInputHandler in parent hierarchy
        _inputHandler = GetComponentInParent<PlayerInputHandler>();
        if (_inputHandler == null)
        {
            Debug.LogWarning("[MouseTracker] PlayerInputHandler not found. Mouse compensation will be disabled.");
        }
    }


    /// <summary>
    /// Called when recoil is applied. Stores the recoil direction for compensation calculation.
    /// </summary>
    public void OnRecoilApplied(Vector2 recoilDelta)
    {
        // Store the recoil direction for compensation detection
        // Recoil is typically upward (positive x) and can be left/right (y)
        _currentRecoilDirection = recoilDelta.normalized;
    }

    /// <summary>
    /// Called every frame to update compensation based on mouse input.
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        if (!IsEnabled)
        {
            _compensationDelta = Vector2.zero;
            _compensationEffectiveness = 1f; // Base recovery rate
            return;
        }

        // Get mouse input
        Vector2 mouseInput = GetMouseInput();
        _lastMouseDelta = mouseInput;

        // Calculate compensation based on mouse movement opposite to recoil
        CalculateCompensation(mouseInput);
    }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        _lastMouseDelta = Vector2.zero;
        _compensationDelta = Vector2.zero;
        _compensationEffectiveness = 1f;
        _currentRecoilDirection = Vector2.zero;
    }

    #endregion

    /// <summary>
    /// Gets the current mouse input from PlayerInputHandler or returns zero if unavailable.
    /// </summary>
    private Vector2 GetMouseInput()
    {
        if (_inputHandler != null)
        {
            return _inputHandler.MouseLookInput;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// Sets mouse input directly for testing purposes.
    /// </summary>
    /// <param name="mouseInput">The mouse input to use</param>
    public void SetMouseInputForTesting(Vector2 mouseInput)
    {
        _lastMouseDelta = mouseInput;
        CalculateCompensation(mouseInput);
    }

    /// <summary>
    /// Sets the current recoil direction for testing purposes.
    /// </summary>
    /// <param name="recoilDirection">The recoil direction (normalized)</param>
    public void SetRecoilDirectionForTesting(Vector2 recoilDirection)
    {
        _currentRecoilDirection = recoilDirection.normalized;
    }

    /// <summary>
    /// Calculates compensation delta and effectiveness based on mouse input.
    /// </summary>
    /// <param name="mouseInput">Current frame's mouse input</param>
    private void CalculateCompensation(Vector2 mouseInput)
    {
        // If no mouse input, use base recovery rate
        if (mouseInput.sqrMagnitude < 0.0001f)
        {
            _compensationDelta = Vector2.zero;
            _compensationEffectiveness = 1f; // Base recovery rate
            return;
        }

        // Normalize mouse input for direction comparison
        Vector2 mouseDirection = mouseInput.normalized;

        // Recoil is typically upward (positive pitch), so compensation is downward (negative mouse Y)
        // Mouse Y is inverted: negative Y = moving mouse down = compensating for upward recoil
        // We need to check if mouse movement is opposite to recoil direction
        
        // For vertical: recoil goes up (positive), compensation is mouse down (negative Y in screen space)
        // For horizontal: recoil can go left/right, compensation is opposite direction
        
        // Calculate how much the mouse movement opposes the recoil
        // Mouse input: positive Y = mouse up, negative Y = mouse down
        // Recoil: positive X = upward pitch, positive Y = rightward yaw
        // To compensate upward recoil, player moves mouse down (negative mouse Y)
        
        // Map mouse input to recoil space:
        // Mouse Y (vertical movement) affects pitch (recoil X)
        // Mouse X (horizontal movement) affects yaw (recoil Y)
        Vector2 mouseInRecoilSpace = new Vector2(-mouseInput.y, -mouseInput.x);

        // Calculate dot product to see if mouse is moving opposite to recoil
        float oppositionFactor = 0f;
        if (_currentRecoilDirection.sqrMagnitude > 0.0001f)
        {
            // Positive dot product means mouse is moving in same direction as recoil (not compensating)
            // Negative dot product means mouse is moving opposite to recoil (compensating)
            float dot = Vector2.Dot(mouseInRecoilSpace.normalized, _currentRecoilDirection);
            
            // We want compensation when moving opposite to recoil (positive dot after negation)
            oppositionFactor = Mathf.Max(0f, dot);
        }

        // Calculate compensation delta - how much recoil should be reduced
        // Scale by mouse magnitude and compensation multiplier
        float mouseMagnitude = mouseInput.magnitude;
        float rawCompensation = oppositionFactor * mouseMagnitude * _compensationMultiplier * 0.01f;

        // Clamp compensation effectiveness to max rate
        _compensationEffectiveness = Mathf.Clamp(1f + rawCompensation, 1f, _maxCompensationRate);

        // Calculate the actual compensation delta (how much to reduce accumulated recoil)
        // This is proportional to the mouse movement in the opposite direction of recoil
        _compensationDelta = mouseInRecoilSpace * _compensationMultiplier * 0.01f;
        
        // Clamp the compensation delta magnitude
        float maxDeltaMagnitude = _maxCompensationRate * 0.1f;
        if (_compensationDelta.magnitude > maxDeltaMagnitude)
        {
            _compensationDelta = _compensationDelta.normalized * maxDeltaMagnitude;
        }
    }

    /// <summary>
    /// Gets the last mouse delta that was processed.
    /// </summary>
    public Vector2 LastMouseDelta => _lastMouseDelta;
}
