using UnityEngine;

/// <summary>
/// Module that applies camera shake based on the recoil path trajectory
/// to create cinematic motion during weapon fire.
/// </summary>
public class CameraShaker : MonoBehaviour, IRecoilModule
{
    [Header("Configuration")]
    [Tooltip("Base intensity of camera shake in meters")]
    [SerializeField]
    private float _shakeIntensity = 0.01f;

    [Tooltip("How long the shake should last after a shot")]
    [SerializeField]
    private float _shakeDuration = 0.2f;

    [Tooltip("Frequency of shake oscillation in Hz")]
    [SerializeField]
    private float _shakeFrequency = 25f;

    [Tooltip("Curve that controls the shake intensity over its duration")]
    [SerializeField]
    private AnimationCurve _shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private RecoilSystem _system;
    private Vector3 _currentShakeOffset;
    private Quaternion _currentShakeRotation;
    private float _shakeTimer;
    private float _noiseOffsetX;
    private float _noiseOffsetY;
    private float _noiseOffsetZ;

    // Properties for editor script compatibility
    public float ShakeIntensity { get => _shakeIntensity; set => _shakeIntensity = value; }
    public float ShakeFrequency { get => _shakeFrequency; set => _shakeFrequency = value; }
    public float PathFollowStrength { get; set; }
    public float CurrentRecoilMagnitude { get; private set; }
    public Vector2 CurrentShakeDirection { get; private set; }

    /// <summary>
    /// Gets the current shake position offset to apply to the camera.
    /// </summary>
    public Vector3 CurrentShakeOffset => _currentShakeOffset;

    /// <summary>
    /// Gets the current shake rotation to apply to the camera.
    /// </summary>
    public Quaternion CurrentShakeRotation => _currentShakeRotation;

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

        if (_system != null && _system.Config != null)
        {
            _shakeIntensity = _system.Config.shakeIntensity;
            _shakeDuration = _system.Config.shakeDuration;
            _shakeFrequency = _system.Config.shakeFrequency;
        }

        _noiseOffsetX = Random.Range(0f, 1000f);
        _noiseOffsetY = Random.Range(0f, 1000f);
        _noiseOffsetZ = Random.Range(0f, 1000f);
    }

    /// <summary>
    /// Called when recoil is applied. Triggers the start of the shake timer.
    /// </summary>
    public void OnRecoilApplied(Vector2 recoilDelta)
    {
        if (IsEnabled)
        {
            _shakeTimer = _shakeDuration;
        }
    }

    /// <summary>
    /// Called every frame to update shake based on the timer.
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        if (!IsEnabled)
        {
            _currentShakeOffset = Vector3.zero;
            _currentShakeRotation = Quaternion.identity;
            return;
        }

        if (_shakeTimer > 0)
        {
            _shakeTimer -= deltaTime;
            CalculateShake();
        }
        else
        {
            _currentShakeOffset = Vector3.zero;
            _currentShakeRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        _shakeTimer = 0f;
        _currentShakeOffset = Vector3.zero;
        _currentShakeRotation = Quaternion.identity;
    }

    #endregion

    /// <summary>
    /// Calculates the current shake offset and rotation based on the shake timer.
    /// </summary>
    private void CalculateShake()
    {
        float normalizedTime = Mathf.Clamp01(_shakeTimer / _shakeDuration);
        float curveValue = _shakeCurve.Evaluate(1 - normalizedTime);
        float currentIntensity = _shakeIntensity * curveValue;

        float noiseTime = Time.time * _shakeFrequency;
        float noiseX = (Mathf.PerlinNoise(noiseTime + _noiseOffsetX, 0f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(0f, noiseTime + _noiseOffsetY) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(noiseTime + _noiseOffsetZ, noiseTime) - 0.5f) * 2f;

        Vector3 baseShake = new Vector3(noiseX, noiseY, noiseZ) * currentIntensity;
        _currentShakeOffset = baseShake;

        float rotationScale = 5f;
        float rotX = baseShake.y * rotationScale;
        float rotY = baseShake.x * rotationScale;
        float rotZ = baseShake.z * rotationScale * 0.5f;

        _currentShakeRotation = Quaternion.Euler(rotX, rotY, rotZ);
    }

    // Methods for editor script compatibility
    public void SetRecoilMagnitudeForTesting(float magnitude) { }
    public void SetRecoilPathForTesting(Vector2 path) { }
    public void AdvanceTimeForTesting(float time) { }
}
