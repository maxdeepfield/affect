using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simplified, modern recoil system using modular components.
/// Cleaner architecture: Controller pattern with service components.
/// </summary>
public class SimpleRecoilController : MonoBehaviour
{
    [SerializeField] private RecoilConfigurationSO _config;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _weaponTransform;

    // Core services
    private RecoilApplier _recoilApplier;
    private RecoilRecovery _recoilRecovery;
    private WeaponAnimator _weaponAnimator;

    // State
    private RecoilState _state = RecoilState.Default;

    public RecoilState State => _state;
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!IsInitialized) return;

        float dt = Time.deltaTime;

        // Update all services
        _recoilApplier.Update(dt);
        _recoilRecovery.Update(dt, ref _state);
        _weaponAnimator.Update(dt, _state);
    }

    public void Initialize()
    {
        if (IsInitialized) return;

        // Setup config
        if (_config == null)
            _config = ScriptableObject.CreateInstance<RecoilConfigurationSO>();

        // Auto-find references
        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) _cameraTransform = cam.transform;
        }

        if (_weaponTransform == null && _cameraTransform != null)
            _weaponTransform = _cameraTransform.Find("Weapon");

        // Initialize services
        _recoilApplier = new RecoilApplier(_config);
        _recoilRecovery = new RecoilRecovery(_config);
        _weaponAnimator = new WeaponAnimator(_weaponTransform, _config);

        _state = RecoilState.Default;
        IsInitialized = true;
    }

    public void ApplyRecoil(float multiplier = 1f)
    {
        if (!IsInitialized) Initialize();
        _recoilApplier.Apply(multiplier, ref _state);
    }

    public void StartReloadAnimation(Vector3 posOffset, Quaternion rotOffset, float duration)
    {
        _weaponAnimator.StartReload(posOffset, rotOffset, duration, _config.reloadAnimationCurve);
    }

    public void ResetRecoil()
    {
        _state.Reset();
        _recoilRecovery.Reset();
        _weaponAnimator.Reset();
    }
}
