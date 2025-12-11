using UnityEngine;

/// <summary>
/// Handles smooth reload animations for weapons.
/// Applies rotation and position offsets during reload similar to recoil system,
/// creating cinematic weapon animation effects.
/// </summary>
public class ReloadAnimation : MonoBehaviour
{
    [Header("Reload Animation Settings")]
    [SerializeField]
    private float _reloadRotationPitch = -15f; // Rotate down/back during reload
    [SerializeField]
    private float _reloadRotationYaw = 25f; // Rotate to the side
    [SerializeField]
    private Vector3 _reloadPositionOffset = new Vector3(0.03f, -0.02f, 0.08f); // Pan up, back, right
    [SerializeField]
    private float _reloadDuration = 1.2f;
    [SerializeField]
    private AnimationCurve _reloadAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Component References")]
    [SerializeField]
    private Transform _weaponTransform;
    [SerializeField]
    private RecoilSystem _recoilSystem;

    private float _reloadTimer;
    private bool _isReloading;
    private Vector3 _originalWeaponPosition;
    private Quaternion _originalWeaponRotation;
    private Vector3 _currentReloadPositionOffset = Vector3.zero;
    private Quaternion _currentReloadRotationOffset = Quaternion.identity;

    /// <summary>
    /// Gets whether reload animation is currently playing.
    /// </summary>
    public bool IsReloading => _isReloading;

    /// <summary>
    /// Gets the current reload animation progress (0-1).
    /// </summary>
    public float ReloadProgress => Mathf.Clamp01(_reloadTimer / _reloadDuration);

    /// <summary>
    /// Gets the current position offset from reload animation.
    /// </summary>
    public Vector3 CurrentReloadPositionOffset => _currentReloadPositionOffset;

    /// <summary>
    /// Gets the current rotation offset from reload animation.
    /// </summary>
    public Quaternion CurrentReloadRotationOffset => _currentReloadRotationOffset;

    private void Start()
    {
        if (_weaponTransform == null)
        {
            // Try to find weapon transform
            Transform cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (cameraTransform != null)
            {
                _weaponTransform = cameraTransform.Find("Weapon");
            }
        }

        if (_weaponTransform != null)
        {
            _originalWeaponPosition = _weaponTransform.localPosition;
            _originalWeaponRotation = _weaponTransform.localRotation;
        }
        if (_recoilSystem == null)
        {
            _recoilSystem = GetComponent<RecoilSystem>();
            if (_recoilSystem == null)
            {
                _recoilSystem = GetComponentInChildren<RecoilSystem>();
            }
        }
    }

    private void Update()
    {
        if (!_isReloading) return;

        _reloadTimer += Time.deltaTime;
        UpdateReloadAnimation();

        if (_reloadTimer >= _reloadDuration)
        {
            EndReload();
        }
    }

    /// <summary>
    /// Starts the reload animation.
    /// </summary>
    /// <param name="duration">Duration of reload animation in seconds</param>
    public void StartReload(float duration = -1f)
    {
        if (_weaponTransform == null)
        {
            Debug.LogWarning("[ReloadAnimation] Weapon transform not assigned. Cannot start reload animation.");
            return;
        }

        _isReloading = true;
        _reloadTimer = 0f;
        if (duration > 0f)
        {
            _reloadDuration = duration;
        }

        _originalWeaponPosition = _weaponTransform.localPosition;
        _originalWeaponRotation = _weaponTransform.localRotation;

        // If a RecoilSystem is present, forward the request to it so transform updates are centralized
        if (_recoilSystem != null)
        {
            Quaternion rotOffset = Quaternion.Euler(_reloadRotationPitch, _reloadRotationYaw, 0f);
            _recoilSystem.StartReloadAnimation(_reloadPositionOffset, rotOffset, _reloadDuration, _reloadAnimationCurve);
            _isReloading = false; // don't do local update
            return;
        }
    }

    /// <summary>
    /// Ends the reload animation and returns weapon to original state.
    /// </summary>
    public void EndReload()
    {
        _isReloading = false;
        _reloadTimer = 0f;
        _currentReloadPositionOffset = Vector3.zero;
        _currentReloadRotationOffset = Quaternion.identity;
        if (_recoilSystem != null)
        {
            _recoilSystem.EndReloadAnimation();
        }
    }

    /// <summary>
    /// Updates the reload animation frame-by-frame.
    /// </summary>
    private void UpdateReloadAnimation()
    {
        if (_weaponTransform == null) return;

        float normalizedProgress = Mathf.Clamp01(_reloadTimer / _reloadDuration);
        float curveValue = _reloadAnimationCurve.Evaluate(normalizedProgress);

        // Apply animation with ease curve
        _currentReloadPositionOffset = _reloadPositionOffset * curveValue;
        _currentReloadRotationOffset = Quaternion.Euler(
            _reloadRotationPitch * curveValue,
            _reloadRotationYaw * curveValue,
            0f
        );

        // Apply to weapon transform
        _weaponTransform.localPosition = _originalWeaponPosition + _currentReloadPositionOffset;
        _weaponTransform.localRotation = _originalWeaponRotation * _currentReloadRotationOffset;
    }

    /// <summary>
    /// Manually set reload animation parameters.
    /// </summary>
    public void SetReloadParameters(float pitchRotation, float yawRotation, Vector3 positionOffset, float duration, AnimationCurve curve)
    {
        _reloadRotationPitch = pitchRotation;
        _reloadRotationYaw = yawRotation;
        _reloadPositionOffset = positionOffset;
        _reloadDuration = duration;
        _reloadAnimationCurve = curve;
    }
}
