using UnityEngine;

/// <summary>
/// Module that generates procedural random variations in recoil patterns.
/// Uses Perlin noise seeded by shot count and time for controlled randomness.
/// </summary>
public class RecoilRandomizer : MonoBehaviour, IRecoilModule
{
    [Header("Configuration")]
    [Tooltip("Horizontal spread range (+/-) in degrees")]
    [SerializeField]
    private float _horizontalSpread = 2f;

    [Tooltip("Minimum multiplier for vertical kick variation (0.8 = 80% of base)")]
    [SerializeField]
    private float _verticalVariationMin = 0.8f;

    [Tooltip("Maximum multiplier for vertical kick variation (1.2 = 120% of base)")]
    [SerializeField]
    private float _verticalVariationMax = 1.2f;

    [Tooltip("Scale factor for Perlin noise sampling")]
    [SerializeField]
    private float _noiseScale = 0.5f;

    private RecoilSystem _system;
    private int _shotCount;
    private float _noiseOffsetX;
    private float _noiseOffsetY;

    /// <summary>
    /// Gets or sets the horizontal spread range.
    /// </summary>
    public float HorizontalSpread
    {
        get => _horizontalSpread;
        set => _horizontalSpread = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Gets or sets the vertical variation range (min multiplier).
    /// </summary>
    public float VerticalVariationMin
    {
        get => _verticalVariationMin;
        set => _verticalVariationMin = Mathf.Clamp(value, 0.5f, 1f);
    }

    /// <summary>
    /// Gets or sets the vertical variation range (max multiplier).
    /// </summary>
    public float VerticalVariationMax
    {
        get => _verticalVariationMax;
        set => _verticalVariationMax = Mathf.Clamp(value, 1f, 1.5f);
    }


    /// <summary>
    /// Gets or sets the noise scale for Perlin noise sampling.
    /// </summary>
    public float NoiseScale
    {
        get => _noiseScale;
        set => _noiseScale = Mathf.Max(0.01f, value);
    }

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
            _horizontalSpread = _system.Config.horizontalSpread;
            _verticalVariationMin = _system.Config.verticalVariationMin;
            _verticalVariationMax = _system.Config.verticalVariationMax;
            _noiseScale = _system.Config.noiseScale;
        }

        // Initialize random noise offsets for variation
        _noiseOffsetX = Random.Range(0f, 1000f);
        _noiseOffsetY = Random.Range(0f, 1000f);
    }

    /// <summary>
    /// Called when recoil is applied. Increments shot counter for noise variation.
    /// </summary>
    public void OnRecoilApplied(Vector2 recoilDelta)
    {
        _shotCount++;
    }

    /// <summary>
    /// Called every frame. Not used by this module.
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        // RecoilRandomizer doesn't need per-frame updates
    }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        ResetShotCounter();
    }

    #endregion

    /// <summary>
    /// Generates a randomized recoil kick based on base values and Perlin noise.
    /// </summary>
    /// <param name="baseVertical">Base vertical kick in degrees</param>
    /// <param name="baseHorizontal">Base horizontal kick in degrees (center of spread)</param>
    /// <returns>Randomized recoil kick (x = vertical, y = horizontal)</returns>
    public Vector2 GenerateRecoilKick(float baseVertical, float baseHorizontal)
    {
        if (!IsEnabled)
        {
            return new Vector2(baseVertical, baseHorizontal);
        }

        // Use Perlin noise for smooth, controlled randomness
        // Seed with shot count and time for variation
        float noiseInputX = (_shotCount * _noiseScale) + _noiseOffsetX + (Time.time * 0.1f);
        float noiseInputY = (_shotCount * _noiseScale) + _noiseOffsetY + (Time.time * 0.1f);

        // Perlin noise returns 0-1, we need to map to our ranges
        float verticalNoise = Mathf.PerlinNoise(noiseInputX, 0f);
        float horizontalNoise = Mathf.PerlinNoise(0f, noiseInputY);

        // Calculate vertical variation (80-120% of base, clamped)
        // Map noise (0-1) to variation range (min-max)
        float verticalMultiplier = Mathf.Lerp(_verticalVariationMin, _verticalVariationMax, verticalNoise);
        float randomizedVertical = baseVertical * verticalMultiplier;

        // Calculate horizontal variation (within ±spread)
        // Map noise (0-1) to spread range (-spread to +spread)
        float horizontalOffset = Mathf.Lerp(-_horizontalSpread, _horizontalSpread, horizontalNoise);
        float randomizedHorizontal = Mathf.Clamp(
            baseHorizontal + horizontalOffset,
            -_horizontalSpread,
            _horizontalSpread
        );

        return new Vector2(randomizedVertical, randomizedHorizontal);
    }

    /// <summary>
    /// Resets the shot counter. Called when burst ends or weapon is reset.
    /// </summary>
    public void ResetShotCounter()
    {
        _shotCount = 0;
    }

    /// <summary>
    /// Gets the current shot count for this burst.
    /// </summary>
    public int ShotCount => _shotCount;
}
