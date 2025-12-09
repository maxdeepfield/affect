using UnityEngine;

/// <summary>
/// Runtime state for each leg including joint references, target positions,
/// step progress, and diagonal group assignment.
/// </summary>
[System.Serializable]
public class LegData
{
    [Header("Joint References")]
    [Tooltip("Root transform of the leg hierarchy")]
    public Transform root;

    [Tooltip("Hip joint transform")]
    public Transform hip;

    [Tooltip("Knee joint transform (null for 1-2 bone legs)")]
    public Transform knee;

    [Tooltip("Foot/end effector transform")]
    public Transform foot;

    [Header("Segment Health")]
    [Tooltip("Health values for each segment")]
    public float[] segmentHealth;

    [Tooltip("Active state for each segment")]
    public bool[] segmentActive;

    [Header("IK State")]
    [Tooltip("Local space rest position")]
    public Vector3 restTarget;

    [Tooltip("World space current target")]
    public Vector3 currentTarget;

    [Tooltip("World space planted position")]
    public Vector3 plantedPos;

    [Header("Step State")]
    [Tooltip("Whether this leg is currently stepping")]
    public bool isStepping;

    [Tooltip("Progress through current step (0-1)")]
    [Range(0f, 1f)]
    public float stepProgress;

    [Tooltip("Time of last step completion")]
    public float lastStepTime;

    [Header("Group Assignment")]
    [Tooltip("Index of this leg in the legs array")]
    public int legIndex;

    [Tooltip("Diagonal group for alternating gait (0 or 1)")]
    public int diagonalGroup;

    /// <summary>
    /// Gets the current chain length based on active segments.
    /// </summary>
    public float CurrentChainLength
    {
        get
        {
            if (hip == null || foot == null) return 0f;

            float length = 0f;

            if (knee != null && IsSegmentActive(1))
            {
                // 3-bone: hip->knee + knee->foot
                length += Vector3.Distance(hip.position, knee.position);
                if (IsSegmentActive(2))
                {
                    length += Vector3.Distance(knee.position, foot.position);
                }
            }
            else if (IsSegmentActive(0))
            {
                // 2-bone or 1-bone: hip->foot
                length = Vector3.Distance(hip.position, foot.position);
            }

            return length;
        }
    }

    /// <summary>
    /// Gets the number of active bones in this leg.
    /// </summary>
    public int ActiveBoneCount
    {
        get
        {
            if (segmentActive == null || segmentActive.Length == 0)
            {
                // Default: count based on available transforms
                int count = 0;
                if (hip != null) count++;
                if (knee != null) count++;
                if (foot != null && count > 0) count = Mathf.Max(count, 1);
                return count;
            }

            int active = 0;
            for (int i = 0; i < segmentActive.Length; i++)
            {
                if (segmentActive[i]) active++;
            }
            return active;
        }
    }

    /// <summary>
    /// Checks if a specific segment is active.
    /// </summary>
    /// <param name="segmentIndex">Index of the segment to check</param>
    /// <returns>True if segment is active or no segment tracking exists</returns>
    public bool IsSegmentActive(int segmentIndex)
    {
        if (segmentActive == null || segmentIndex < 0 || segmentIndex >= segmentActive.Length)
        {
            return true; // Default to active if no tracking
        }
        return segmentActive[segmentIndex];
    }

    /// <summary>
    /// Initializes segment health arrays for the given bone count.
    /// </summary>
    /// <param name="boneCount">Number of bones (1-3)</param>
    /// <param name="healthPerSegment">Initial health for each segment</param>
    public void InitializeSegments(int boneCount, float healthPerSegment)
    {
        boneCount = Mathf.Clamp(boneCount, 1, 3);
        segmentHealth = new float[boneCount];
        segmentActive = new bool[boneCount];

        for (int i = 0; i < boneCount; i++)
        {
            segmentHealth[i] = healthPerSegment;
            segmentActive[i] = true;
        }
    }

    /// <summary>
    /// Resets the leg to its initial state.
    /// </summary>
    public void Reset()
    {
        isStepping = false;
        stepProgress = 0f;
        lastStepTime = -999f;
        currentTarget = plantedPos;
    }

    /// <summary>
    /// Applies damage to a specific segment.
    /// </summary>
    /// <param name="segmentIndex">Index of the segment to damage</param>
    /// <param name="damage">Amount of damage to apply</param>
    public void ApplyDamage(int segmentIndex, float damage)
    {
        if (segmentHealth == null || segmentIndex < 0 || segmentIndex >= segmentHealth.Length)
        {
            return;
        }

        segmentHealth[segmentIndex] = Mathf.Max(0f, segmentHealth[segmentIndex] - damage);

        // Deactivate segment if health reaches zero
        if (segmentHealth[segmentIndex] <= 0f && segmentActive != null && segmentIndex < segmentActive.Length)
        {
            segmentActive[segmentIndex] = false;
        }
    }

    /// <summary>
    /// Gets the health of a specific segment.
    /// </summary>
    /// <param name="segmentIndex">Index of the segment</param>
    /// <returns>Health value, or 0 if invalid index</returns>
    public float GetSegmentHealth(int segmentIndex)
    {
        if (segmentHealth == null || segmentIndex < 0 || segmentIndex >= segmentHealth.Length)
        {
            return 0f;
        }
        return segmentHealth[segmentIndex];
    }
}
