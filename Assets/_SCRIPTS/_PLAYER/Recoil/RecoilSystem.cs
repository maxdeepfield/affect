using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main orchestrator for the Epic Recoil System.
/// Coordinates all recoil modules and applies combined effects to weapon and camera.
/// </summary>
public class RecoilSystem : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Recoil configuration containing all parameters")]
    [SerializeField]
    private RecoilConfiguration _config = new RecoilConfiguration();

    [Header("Transform References")]
    [Tooltip("Camera transform for rotation recoil")]
    [SerializeField]
    private Transform _cameraTransform;

    [Tooltip("Weapon transform for position/rotation recoil")]
    [SerializeField]
    private Transform _weaponTransform;

    [Header("Component References")]
    [Tooltip("MouseLook component for external recoil offset")]
    [SerializeField]
    private MouseLook _mouseLook;

    [Tooltip("WeaponController for sway and bobbing")]
    [SerializeField]
    private WeaponController _weaponController;

    // Modules
    private List<IRecoilModule> _modules = new List<IRecoilModule>();
    private RecoilRandomizer _randomizer;
    private MouseTracker _mouseTracker;
    private CameraShaker _cameraShaker;

    // State
    private RecoilState _state;
    private Vector3 _originalWeaponPosition;
    private Quaternion _originalWeaponRotation;
    private bool _initialized;

    /// <summary>
    /// Gets or sets the recoil configuration.
    /// </summary>
    public RecoilConfiguration Config
    {
        get => _config;
        set
        {
            _config = value ?? new RecoilConfiguration();
            UpdateModuleConfigurations();
        }
    }

    /// <summary>
    /// Gets the current accumulated recoil (x = pitch/vertical, y = yaw/horizontal).
    /// </summary>
    public Vector2 AccumulatedRecoil => _state.accumulatedRecoil;

    /// <summary>
    /// Gets the current recoil path direction.
    /// </summary>
    public Vector2 CurrentRecoilPath => _state.currentPath;

    /// <summary>
    /// Gets the magnitude of current accumulated recoil.
    /// </summary>
    public float RecoilMagnitude => _state.RecoilMagnitude;

    /// <summary>
    /// Gets the current recoil state (read-only copy).
    /// </summary>
    public RecoilState State => _state;

    /// <summary>
    /// Gets the camera transform reference.
    /// </summary>
    public Transform CameraTransform => _cameraTransform;

    /// <summary>
    /// Gets the weapon transform reference.
    /// </summary>
    public Transform WeaponTransform => _weaponTransform;

    #region Unity Lifecycle

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!_initialized) return;

        float deltaTime = Time.deltaTime;

        // Update all modules
        UpdateModules(deltaTime);

        // Apply recovery
        ApplyRecovery(deltaTime);

        // Update weapon transform
        UpdateWeaponTransform(deltaTime);

        // Update time since last shot
        _state.timeSinceLastShot += deltaTime;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the recoil system and discovers child modules.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        _state = RecoilState.Default;

        // Auto-discover transform references if not set
        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                _cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogWarning("[RecoilSystem] Camera transform not found. Camera recoil will be disabled.");
            }
        }

        if (_weaponTransform == null && _cameraTransform != null)
        {
            _weaponTransform = _cameraTransform.Find("Weapon");
            if (_weaponTransform == null)
            {
                Debug.LogWarning("[RecoilSystem] Weapon transform not found. Weapon recoil will be disabled.");
            }
        }

        // Store original weapon transform
        if (_weaponTransform != null)
        {
            _originalWeaponPosition = _weaponTransform.localPosition;
            _originalWeaponRotation = _weaponTransform.localRotation;
        }

        // Find MouseLook component
        if (_mouseLook == null)
        {
            _mouseLook = GetComponentInParent<MouseLook>();
            if (_mouseLook == null)
            {
                _mouseLook = GetComponent<MouseLook>();
            }
        }

        // Find WeaponController component
        if (_weaponController == null)
        {
            _weaponController = GetComponentInParent<WeaponController>();
            if (_weaponController == null)
            {
                _weaponController = GetComponent<WeaponController>();
            }
        }

        // Discover and initialize modules
        DiscoverModules();
        InitializeModules();

        _initialized = true;
    }

    /// <summary>
    /// Discovers child IRecoilModule components.
    /// </summary>
    private void DiscoverModules()
    {
        _modules.Clear();

        // Get all IRecoilModule components in children
        IRecoilModule[] foundModules = GetComponentsInChildren<IRecoilModule>();
        foreach (var module in foundModules)
        {
            // Don't add self if this implements IRecoilModule
            if (module as MonoBehaviour != this)
            {
                _modules.Add(module);
            }
        }

        // Cache specific module references
        _randomizer = GetComponentInChildren<RecoilRandomizer>();
        _mouseTracker = GetComponentInChildren<MouseTracker>();
        _cameraShaker = GetComponentInChildren<CameraShaker>();
    }

    /// <summary>
    /// Initializes all discovered modules.
    /// </summary>
    private void InitializeModules()
    {
        foreach (var module in _modules)
        {
            try
            {
                module.Initialize(this);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RecoilSystem] Module initialization failed: {e.Message}. Module will be disabled.");
                module.IsEnabled = false;
            }
        }
    }

    /// <summary>
    /// Updates module configurations when config changes.
    /// </summary>
    private void UpdateModuleConfigurations()
    {
        // Re-initialize modules with new config
        foreach (var module in _modules)
        {
            try
            {
                module.Initialize(this);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RecoilSystem] Module re-initialization failed: {e.Message}");
            }
        }
    }

    #endregion

    #region Recoil Application

    /// <summary>
    /// Applies recoil from a shot with default multiplier.
    /// </summary>
    public void ApplyRecoil()
    {
        ApplyRecoil(1f);
    }

    /// <summary>
    /// Applies recoil from a shot with a multiplier.
    /// </summary>
    /// <param name="multiplier">Multiplier for recoil intensity</param>
    public void ApplyRecoil(float multiplier)
    {
        if (!_initialized)
        {
            Initialize();
        }

        // Get base recoil values from config
        float baseVertical = _config.baseVerticalKick;
        float baseHorizontal = _config.baseHorizontalKick;

        // Apply randomization if available
        Vector2 recoilKick;
        if (_randomizer != null && _randomizer.IsEnabled)
        {
            recoilKick = _randomizer.GenerateRecoilKick(baseVertical, baseHorizontal);
        }
        else
        {
            recoilKick = new Vector2(baseVertical, baseHorizontal);
        }

        // Apply multiplier
        recoilKick *= multiplier;

        // Clamp individual kick values per requirements
        // Vertical: 0.5-5 degrees, Horizontal: ±2 degrees
        recoilKick.x = Mathf.Clamp(recoilKick.x, 0.5f, 5f);
        recoilKick.y = Mathf.Clamp(recoilKick.y, -2f, 2f);

        // Store current path direction
        _state.currentPath = recoilKick.normalized;

        // Accumulate recoil with clamping
        // _state.accumulatedRecoil += recoilKick;
        // _state.accumulatedRecoil.x = Mathf.Clamp(_state.accumulatedRecoil.x, 0f, _config.maxAccumulatedVertical);
        // _state.accumulatedRecoil.y = Mathf.Clamp(_state.accumulatedRecoil.y, -_config.horizontalSpread * 2f, _config.horizontalSpread * 2f);

        // Update shot count and timing
        _state.shotCount++;
        _state.timeSinceLastShot = 0f;

        // Apply weapon transform recoil
        ApplyWeaponRecoil();

        // Notify all modules
        NotifyModulesRecoilApplied(recoilKick);

        // Apply camera rotation recoil
        ApplyCameraRecoil(recoilKick);
    }

    /// <summary>
    /// Notifies all modules that recoil was applied.
    /// </summary>
    private void NotifyModulesRecoilApplied(Vector2 recoilDelta)
    {
        foreach (var module in _modules)
        {
            if (module.IsEnabled)
            {
                try
                {
                    module.OnRecoilApplied(recoilDelta);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RecoilSystem] Module OnRecoilApplied failed: {e.Message}");
                }
            }
        }
    }

    #endregion

    #region Camera Recoil

    /// <summary>
    /// Applies camera rotation recoil.
    /// </summary>
    private void ApplyCameraRecoil(Vector2 recoilKick)
    {
        if (_mouseLook == null) return;

        // Apply recoil as pitch offset (vertical = pitch)
        // The MouseLook component will use this offset in its rotation calculation
        // _mouseLook.recoilPitchOffset = _state.accumulatedRecoil.x;
    }

    /// <summary>
    /// Gets the current camera recoil rotation to apply externally.
    /// </summary>
    /// <returns>Euler angles for camera recoil (pitch, yaw, 0)</returns>
    public Vector3 GetCameraRecoilRotation()
    {
        // Include camera shake if available
        Vector3 shakeRotation = Vector3.zero;
        if (_cameraShaker != null && _cameraShaker.IsEnabled)
        {
            Quaternion shakeQuat = _cameraShaker.CurrentShakeRotation;
            shakeRotation = shakeQuat.eulerAngles;
            // Normalize angles to -180 to 180 range
            if (shakeRotation.x > 180f) shakeRotation.x -= 360f;
            if (shakeRotation.y > 180f) shakeRotation.y -= 360f;
            if (shakeRotation.z > 180f) shakeRotation.z -= 360f;
        }

        return new Vector3(_state.accumulatedRecoil.x, _state.accumulatedRecoil.y, 0f) + shakeRotation;
    }

    #endregion

    #region Weapon Transform Recoil

    /// <summary>
    /// Applies weapon transform recoil (position kickback and rotation).
    /// </summary>
    private void ApplyWeaponRecoil()
    {
        if (_weaponTransform == null) return;

        // Position kickback along local -Z axis (backward)
        Vector3 positionKick = new Vector3(0f, 0f, -_config.weaponKickbackDistance);
        _state.weaponPositionOffset = positionKick;

        // Rotation kick around local X axis (upward tilt = negative X rotation)
        Quaternion rotationKick = Quaternion.Euler(-_config.weaponRotationKick, 0f, 0f);
        _state.weaponRotationOffset = rotationKick;
    }

    /// <summary>
    /// Updates weapon transform based on current recoil state.
    /// </summary>
    private void UpdateWeaponTransform(float deltaTime)
    {
        if (_weaponTransform == null) return;

        // Calculate sway and bobbing from WeaponController
        Vector3 swayPosition = Vector3.zero;
        Vector3 bobPosition = Vector3.zero;
        if (_weaponController != null)
        {
            swayPosition = _weaponController.HandleWeaponSway();
            bobPosition = _weaponController.HandleBobbing();
        }

        // Calculate target position and rotation
        Vector3 targetPosition = _originalWeaponPosition + _state.weaponPositionOffset + swayPosition + bobPosition;
        Quaternion targetRotation = _originalWeaponRotation * _state.weaponRotationOffset;

        // Add camera shake offset if available
        if (_cameraShaker != null && _cameraShaker.IsEnabled)
        {
            targetPosition += _cameraShaker.CurrentShakeOffset;
        }

        // Smoothly interpolate to target
        _weaponTransform.localPosition = Vector3.Lerp(_weaponTransform.localPosition, targetPosition, _config.swaySmoothness * deltaTime);
        _weaponTransform.localRotation = Quaternion.Slerp(_weaponTransform.localRotation, targetRotation, _config.recoverySpeed * deltaTime);
    }

    #endregion

    #region Recovery

    /// <summary>
    /// Applies recoil recovery over time.
    /// </summary>
    private void ApplyRecovery(float deltaTime)
    {
        // Skip recovery if no accumulated recoil
        if (_state.accumulatedRecoil.sqrMagnitude < 0.0001f && 
            _state.weaponPositionOffset.sqrMagnitude < 0.0001f)
        {
            _state.accumulatedRecoil = Vector2.zero;
            _state.weaponPositionOffset = Vector3.zero;
            _state.weaponRotationOffset = Quaternion.identity;
            _state.shotCount = 0;
            return;
        }

        // Calculate recovery rate
        float baseRecoveryRate = _config.recoverySpeed;
        float compensationMultiplier = 1f;

        // Apply mouse compensation if available
        if (_mouseTracker != null && _mouseTracker.IsEnabled)
        {
            compensationMultiplier = _mouseTracker.CompensationEffectiveness;
            
            // Apply compensation delta directly to accumulated recoil
            Vector2 compensation = _mouseTracker.CompensationDelta;
            // _state.accumulatedRecoil -= compensation;
        }

        // Calculate recovery amount using curve
        float normalizedTime = Mathf.Clamp01(_state.timeSinceLastShot * baseRecoveryRate * 0.1f);
        float curveValue = _config.recoveryCurve != null ? _config.recoveryCurve.Evaluate(normalizedTime) : 1f - normalizedTime;
        
        // Apply recovery decay
        float recoveryAmount = baseRecoveryRate * compensationMultiplier * deltaTime;
        
        // Decay accumulated recoil
        // _state.accumulatedRecoil = Vector2.MoveTowards(_state.accumulatedRecoil, Vector2.zero, recoveryAmount);

        // Decay weapon offsets
        _state.weaponPositionOffset = Vector3.MoveTowards(_state.weaponPositionOffset, Vector3.zero, recoveryAmount * 0.01f);
        
        // Decay weapon rotation offset
        float rotationRecovery = recoveryAmount * 10f;
        _state.weaponRotationOffset = Quaternion.Slerp(_state.weaponRotationOffset, Quaternion.identity, rotationRecovery * deltaTime);

        // Update camera recoil offset
        if (_mouseLook != null)
        {
            // _mouseLook.recoilPitchOffset = _state.accumulatedRecoil.x;
        }

        // Clamp to zero when very small
        if (_state.accumulatedRecoil.magnitude < 0.01f)
        {
            _state.accumulatedRecoil = Vector2.zero;
            _state.shotCount = 0;
        }

        if (_state.weaponPositionOffset.magnitude < 0.0001f)
        {
            _state.weaponPositionOffset = Vector3.zero;
        }

        // Update current path based on accumulated recoil direction
        if (_state.accumulatedRecoil.sqrMagnitude > 0.0001f)
        {
            _state.currentPath = _state.accumulatedRecoil.normalized;
        }
        else
        {
            _state.currentPath = Vector2.zero;
        }
    }

    /// <summary>
    /// Updates all modules each frame.
    /// </summary>
    private void UpdateModules(float deltaTime)
    {
        foreach (var module in _modules)
        {
            if (module.IsEnabled)
            {
                try
                {
                    module.OnUpdate(deltaTime);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RecoilSystem] Module OnUpdate failed: {e.Message}");
                }
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets a new recoil configuration.
    /// </summary>
    /// <param name="config">New configuration to apply</param>
    public void SetConfiguration(RecoilConfiguration config)
    {
        Config = config;
    }

    /// <summary>
    /// Resets the recoil system to its initial state.
    /// </summary>
    public void ResetRecoil()
    {
        _state.Reset();

        // Reset weapon transform
        if (_weaponTransform != null)
        {
            _weaponTransform.localPosition = _originalWeaponPosition;
            _weaponTransform.localRotation = _originalWeaponRotation;
        }

        // Reset camera recoil offset
        if (_mouseLook != null)
        {
            _mouseLook.recoilPitchOffset = 0f;
        }

        // Reset all modules
        foreach (var module in _modules)
        {
            try
            {
                module.Reset();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RecoilSystem] Module Reset failed: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the original weapon position before recoil.
    /// </summary>
    public Vector3 OriginalWeaponPosition => _originalWeaponPosition;

    /// <summary>
    /// Gets the original weapon rotation before recoil.
    /// </summary>
    public Quaternion OriginalWeaponRotation => _originalWeaponRotation;

    /// <summary>
    /// Gets whether the system is initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the list of discovered modules.
    /// </summary>
    public IReadOnlyList<IRecoilModule> Modules => _modules.AsReadOnly();

    /// <summary>
    /// Gets the RecoilRandomizer module if available.
    /// </summary>
    public RecoilRandomizer Randomizer => _randomizer;

    /// <summary>
    /// Gets the MouseTracker module if available.
    /// </summary>
    public MouseTracker MouseTracker => _mouseTracker;

    /// <summary>
    /// Gets the CameraShaker module if available.
    /// </summary>
    public CameraShaker CameraShaker => _cameraShaker;

    #endregion

    #region Testing Support

    /// <summary>
    /// Sets the accumulated recoil directly for testing purposes.
    /// </summary>
    /// <param name="recoil">The accumulated recoil to set</param>
    public void SetAccumulatedRecoilForTesting(Vector2 recoil)
    {
        _state.accumulatedRecoil = recoil;
        _state.currentPath = recoil.normalized;
    }

    /// <summary>
    /// Sets the weapon position offset directly for testing purposes.
    /// </summary>
    /// <param name="offset">The position offset to set</param>
    public void SetWeaponPositionOffsetForTesting(Vector3 offset)
    {
        _state.weaponPositionOffset = offset;
    }

    /// <summary>
    /// Sets the weapon rotation offset directly for testing purposes.
    /// </summary>
    /// <param name="offset">The rotation offset to set</param>
    public void SetWeaponRotationOffsetForTesting(Quaternion offset)
    {
        _state.weaponRotationOffset = offset;
    }

    /// <summary>
    /// Simulates recovery for testing purposes.
    /// </summary>
    /// <param name="deltaTime">Time to simulate</param>
    public void SimulateRecoveryForTesting(float deltaTime)
    {
        _state.timeSinceLastShot += deltaTime;
        ApplyRecovery(deltaTime);
    }

    /// <summary>
    /// Forces initialization for testing purposes.
    /// </summary>
    public void ForceInitializeForTesting()
    {
        _initialized = false;
        Initialize();
    }

    /// <summary>
    /// Sets transform references for testing purposes.
    /// </summary>
    public void SetTransformsForTesting(Transform camera, Transform weapon)
    {
        _cameraTransform = camera;
        _weaponTransform = weapon;
        
        if (_weaponTransform != null)
        {
            _originalWeaponPosition = _weaponTransform.localPosition;
            _originalWeaponRotation = _weaponTransform.localRotation;
        }
    }

    /// <summary>
    /// Gets the current weapon position offset.
    /// </summary>
    public Vector3 CurrentWeaponPositionOffset => _state.weaponPositionOffset;

    /// <summary>
    /// Gets the current weapon rotation offset.
    /// </summary>
    public Quaternion CurrentWeaponRotationOffset => _state.weaponRotationOffset;

    #endregion
}
