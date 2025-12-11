using UnityEngine;

/// <summary>
/// Handles weapon transform animation (position, rotation).
/// Applies recoil and reload offsets to the weapon.
/// </summary>
public class WeaponAnimator
{
    private Transform _weaponTransform;
    private RecoilConfigurationSO _config;

    // Reload state
    private bool _isReloading;
    private float _reloadTimer;
    private float _reloadDuration;
    private AnimationCurve _reloadCurve;
    private Vector3 _reloadTargetPosition;
    private Quaternion _reloadTargetRotation;

    // Original transform state
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    public WeaponAnimator(Transform weaponTransform, RecoilConfigurationSO config)
    {
        _weaponTransform = weaponTransform;
        _config = config;

        if (_weaponTransform != null)
        {
            _originalPosition = _weaponTransform.localPosition;
            _originalRotation = _weaponTransform.localRotation;
        }
    }

    public void Update(float deltaTime, RecoilState state)
    {
        if (_weaponTransform == null) return;

        // Update reload animation
        if (_isReloading)
        {
            _reloadTimer += deltaTime;
            float progress = Mathf.Clamp01(_reloadTimer / _reloadDuration);
            float eval = _reloadCurve != null ? _reloadCurve.Evaluate(progress) : progress;

            state.reloadPositionOffset = Vector3.Lerp(Vector3.zero, _reloadTargetPosition, eval);
            state.reloadRotationOffset = Quaternion.Slerp(Quaternion.identity, _reloadTargetRotation, eval);

            if (progress >= 1f)
                _isReloading = false;
        }
        else
        {
            state.reloadPositionOffset = Vector3.zero;
            state.reloadRotationOffset = Quaternion.identity;
        }

        // Apply combined transforms
        Vector3 targetPosition = _originalPosition + state.weaponPositionOffset + state.reloadPositionOffset;
        Quaternion targetRotation = _originalRotation * state.reloadRotationOffset * state.weaponRotationOffset;

        _weaponTransform.localPosition = Vector3.Lerp(_weaponTransform.localPosition, targetPosition, _config.swaySmoothness * deltaTime);
        _weaponTransform.localRotation = Quaternion.Slerp(_weaponTransform.localRotation, targetRotation, _config.recoverySpeed * deltaTime);
    }

    public void StartReload(Vector3 posOffset, Quaternion rotOffset, float duration, AnimationCurve curve = null)
    {
        _isReloading = true;
        _reloadTimer = 0f;
        _reloadDuration = Mathf.Max(0.01f, duration);
        _reloadCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        _reloadTargetPosition = posOffset;
        _reloadTargetRotation = rotOffset;
    }

    public void Reset()
    {
        _isReloading = false;
        _reloadTimer = 0f;
        if (_weaponTransform != null)
        {
            _weaponTransform.localPosition = _originalPosition;
            _weaponTransform.localRotation = _originalRotation;
        }
    }
}
