using UnityEngine;

/// <summary>
/// Module that manages shootable leg segments, tracking damage per segment,
/// detaching damaged segments, and adjusting locomotion accordingly.
/// </summary>
public class LegDamageHandler : MonoBehaviour, ISpiderModule
{
    [Header("Damage Settings")]
    [Tooltip("Default health per segment")]
    [SerializeField] private float _segmentHealth = 100f;

    [Tooltip("Prefab to spawn when segment is detached")]
    [SerializeField] private GameObject _debrisPrefab;

    [Tooltip("Force applied to debris when spawned")]
    [SerializeField] private float _debrisForce = 5f;

    [Header("Debug")]
    [SerializeField] private bool _logDamageEvents = false;

    private SpiderIKSystem _system;
    private bool _isEnabled = true;

    #region Properties

    public float SegmentHealth
    {
        get => _segmentHealth;
        set => _segmentHealth = Mathf.Max(0f, value);
    }

    public GameObject DebrisPrefab
    {
        get => _debrisPrefab;
        set => _debrisPrefab = value;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    #endregion

    #region ISpiderModule Implementation

    public void Initialize(SpiderIKSystem system)
    {
        _system = system;

        if (_system?.Config != null)
        {
            _segmentHealth = _system.Config.segmentHealth;
        }
    }

    public void OnUpdate(float deltaTime) { }

    public void OnFixedUpdate(float fixedDeltaTime) { }

    public void Reset()
    {
        // Reset all leg segment health
        if (_system?.Legs != null)
        {
            foreach (var leg in _system.Legs)
            {
                if (leg != null)
                {
                    int boneCount = _system.Config?.boneCount ?? 3;
                    leg.InitializeSegments(boneCount, _segmentHealth);
                }
            }
        }
    }

    #endregion


    #region Damage System

    /// <summary>
    /// Applies damage to a specific leg segment.
    /// </summary>
    /// <param name="legIndex">Index of the leg</param>
    /// <param name="segmentIndex">Index of the segment within the leg</param>
    /// <param name="damage">Amount of damage to apply</param>
    public void ApplyDamage(int legIndex, int segmentIndex, float damage)
    {
        if (!_isEnabled) return;
        if (_system?.Legs == null) return;
        if (legIndex < 0 || legIndex >= _system.Legs.Length) return;

        var leg = _system.Legs[legIndex];
        if (leg == null) return;

        // Get current health before damage
        float previousHealth = leg.GetSegmentHealth(segmentIndex);
        
        // Apply damage through LegData
        leg.ApplyDamage(segmentIndex, damage);

        float newHealth = leg.GetSegmentHealth(segmentIndex);

        if (_logDamageEvents)
        {
            Debug.Log($"[LegDamageHandler] Leg {legIndex} Segment {segmentIndex}: {previousHealth} -> {newHealth} (damage: {damage})");
        }

        // Check if segment should be detached
        if (newHealth <= 0f && previousHealth > 0f)
        {
            DetachSegment(leg, segmentIndex);
        }
    }

    /// <summary>
    /// Detaches a segment and all child segments from the leg hierarchy.
    /// </summary>
    public void DetachSegment(LegData leg, int segmentIndex)
    {
        if (leg == null) return;
        if (leg.segmentActive == null || segmentIndex < 0 || segmentIndex >= leg.segmentActive.Length) return;

        // Deactivate this segment and all children
        for (int i = segmentIndex; i < leg.segmentActive.Length; i++)
        {
            leg.segmentActive[i] = false;
        }

        // Get the transform to detach based on segment index
        Transform segmentTransform = GetSegmentTransform(leg, segmentIndex);

        if (segmentTransform != null)
        {
            // Spawn debris if prefab is set
            SpawnDebris(segmentTransform.position, segmentTransform.rotation);

            // Disable the segment visually
            segmentTransform.gameObject.SetActive(false);
        }

        // Recalculate IK chain
        RecalculateLegChain(leg);

        if (_logDamageEvents)
        {
            Debug.Log($"[LegDamageHandler] Detached segment {segmentIndex} from leg {leg.legIndex}. Active bones: {leg.ActiveBoneCount}");
        }

        // Check if entire leg is destroyed
        if (leg.ActiveBoneCount == 0)
        {
            OnLegDestroyed(leg);
        }
    }

    /// <summary>
    /// Gets the transform for a specific segment index.
    /// </summary>
    private Transform GetSegmentTransform(LegData leg, int segmentIndex)
    {
        switch (segmentIndex)
        {
            case 0: return leg.hip;
            case 1: return leg.knee;
            case 2: return leg.foot;
            default: return null;
        }
    }

    /// <summary>
    /// Spawns debris at the specified position.
    /// </summary>
    private void SpawnDebris(Vector3 position, Quaternion rotation)
    {
        if (_debrisPrefab == null) return;

        GameObject debris = Instantiate(_debrisPrefab, position, rotation);
        
        // Add physics if not present
        Rigidbody rb = debris.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = debris.AddComponent<Rigidbody>();
        }

        // Apply random force
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Abs(randomDir.y); // Bias upward
        rb.AddForce(randomDir * _debrisForce, ForceMode.Impulse);
        rb.AddTorque(Random.onUnitSphere * _debrisForce * 0.5f, ForceMode.Impulse);
    }

    #endregion

    #region Chain Recalculation

    /// <summary>
    /// Recalculates the IK chain length for a leg after damage.
    /// </summary>
    public void RecalculateLegChain(LegData leg)
    {
        if (leg == null) return;

        // The CurrentChainLength property in LegData already calculates
        // based on active segments, so we just need to ensure the
        // segment active states are correct
        
        // Force recalculation by accessing the property
        float _ = leg.CurrentChainLength;
    }

    /// <summary>
    /// Called when an entire leg is destroyed.
    /// </summary>
    private void OnLegDestroyed(LegData leg)
    {
        if (_logDamageEvents)
        {
            Debug.Log($"[LegDamageHandler] Leg {leg.legIndex} completely destroyed!");
        }

        // Notify the system that a leg is destroyed
        // The GaitController will need to redistribute weight
        // This is handled through the ActiveBoneCount property
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets the total health remaining for a leg.
    /// </summary>
    public float GetLegTotalHealth(int legIndex)
    {
        if (_system?.Legs == null) return 0f;
        if (legIndex < 0 || legIndex >= _system.Legs.Length) return 0f;

        var leg = _system.Legs[legIndex];
        if (leg?.segmentHealth == null) return 0f;

        float total = 0f;
        for (int i = 0; i < leg.segmentHealth.Length; i++)
        {
            total += leg.segmentHealth[i];
        }
        return total;
    }

    /// <summary>
    /// Gets the number of active segments for a leg.
    /// </summary>
    public int GetActiveSegmentCount(int legIndex)
    {
        if (_system?.Legs == null) return 0;
        if (legIndex < 0 || legIndex >= _system.Legs.Length) return 0;

        return _system.Legs[legIndex]?.ActiveBoneCount ?? 0;
    }

    /// <summary>
    /// Checks if a specific segment is active.
    /// </summary>
    public bool IsSegmentActive(int legIndex, int segmentIndex)
    {
        if (_system?.Legs == null) return false;
        if (legIndex < 0 || legIndex >= _system.Legs.Length) return false;

        return _system.Legs[legIndex]?.IsSegmentActive(segmentIndex) ?? false;
    }

    /// <summary>
    /// Gets the health of a specific segment.
    /// </summary>
    public float GetSegmentHealth(int legIndex, int segmentIndex)
    {
        if (_system?.Legs == null) return 0f;
        if (legIndex < 0 || legIndex >= _system.Legs.Length) return 0f;

        return _system.Legs[legIndex]?.GetSegmentHealth(segmentIndex) ?? 0f;
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Applies configuration from IKConfiguration.
    /// </summary>
    public void ApplyConfiguration(IKConfiguration config)
    {
        if (config == null) return;

        _segmentHealth = config.segmentHealth;
        _isEnabled = config.enableLegDamage;
    }

    #endregion
}
