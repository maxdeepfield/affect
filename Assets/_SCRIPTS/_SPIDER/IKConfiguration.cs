using UnityEngine;

/// <summary>
/// Serializable configuration for the Spider IK Walker System.
/// Contains all parameters for leg setup, body dimensions, walking behavior,
/// stabilization, hit reaction, damage, and ground detection.
/// </summary>
[System.Serializable]
public class IKConfiguration
{
    [Header("Leg Setup")]
    [Tooltip("Number of legs (1-8)")]
    [Range(1, 8)]
    public int legCount = 4;

    [Tooltip("Number of bones per leg (1 = direct, 2 = hip-foot, 3 = hip-knee-foot)")]
    [Range(1, 3)]
    public int boneCount = 3;

    [Header("Body Dimensions")]
    [Tooltip("Capsule radius in meters")]
    [Min(0.01f)]
    public float bodyRadius = 0.3f;

    [Tooltip("Capsule height in meters")]
    [Min(0.01f)]
    public float bodyHeight = 0.6f;

    [Tooltip("Leg root distance = radius * ratio")]
    [Min(0.1f)]
    public float bodyToLegRatio = 1.5f;

    [Tooltip("Global scale multiplier")]
    [Min(0.01f)]
    public float dimensionScale = 1f;

    [Header("Leg Proportions")]
    [Tooltip("Total leg length in meters")]
    [Min(0.01f)]
    public float legLength = 0.6f;

    [Tooltip("Upper leg portion (0-1)")]
    [Range(0.1f, 0.9f)]
    public float hipRatio = 0.5f;

    [Tooltip("Horizontal spread multiplier")]
    [Min(0.1f)]
    public float legSpread = 0.8f;


    [Header("Walking")]
    [Tooltip("Distance to trigger step")]
    [Min(0.01f)]
    public float stepThreshold = 0.4f;

    [Tooltip("Arc height during step")]
    [Min(0f)]
    public float stepHeight = 0.1f;

    [Tooltip("Steps per second")]
    [Min(0.1f)]
    public float stepSpeed = 5f;

    [Tooltip("Base forward projection")]
    [Min(0f)]
    public float strideForward = 0.3f;

    [Tooltip("Velocity-based stride multiplier")]
    [Min(0f)]
    public float strideVelocityScale = 0.4f;

    [Header("Stabilization")]
    [Tooltip("Torque strength for upright orientation")]
    [Min(0f)]
    public float uprightStrength = 20f;

    [Tooltip("Damping for upright torque")]
    [Min(0f)]
    public float uprightDamping = 6f;

    [Tooltip("Force strength for height maintenance")]
    [Min(0f)]
    public float heightStrength = 30f;

    [Tooltip("Damping for height force")]
    [Min(0f)]
    public float heightDamping = 6f;

    [Tooltip("Rotation speed during surface changes")]
    [Min(0f)]
    public float surfaceTransitionSpeed = 5f;

    [Header("Hit Reaction")]
    [Tooltip("Impulse force on collision")]
    [Min(0f)]
    public float hitImpulse = 6f;

    [Tooltip("Scuttle acceleration force")]
    [Min(0f)]
    public float scuttleForce = 30f;

    [Tooltip("Duration of scuttle reaction")]
    [Min(0f)]
    public float scuttleTime = 0.6f;

    [Tooltip("Maximum horizontal speed clamp")]
    [Min(0f)]
    public float maxHorizontalSpeed = 6f;

    [Header("Damage")]
    [Tooltip("Health per leg segment")]
    [Min(0f)]
    public float segmentHealth = 100f;

    [Tooltip("Enable leg damage system")]
    public bool enableLegDamage = true;

    [Header("Ground Detection")]
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayers = -1;

    [Tooltip("Raycast start offset above position")]
    [Min(0f)]
    public float raycastUp = 1.5f;

    [Tooltip("Raycast distance downward")]
    [Min(0f)]
    public float raycastDown = 3f;


    /// <summary>
    /// Default constructor with sensible default values.
    /// </summary>
    public IKConfiguration()
    {
        // Leg Setup
        legCount = 4;
        boneCount = 3;

        // Body Dimensions
        bodyRadius = 0.3f;
        bodyHeight = 0.6f;
        bodyToLegRatio = 1.5f;
        dimensionScale = 1f;

        // Leg Proportions
        legLength = 0.6f;
        hipRatio = 0.5f;
        legSpread = 0.8f;

        // Walking
        stepThreshold = 0.4f;
        stepHeight = 0.1f;
        stepSpeed = 5f;
        strideForward = 0.3f;
        strideVelocityScale = 0.4f;

        // Stabilization
        uprightStrength = 20f;
        uprightDamping = 6f;
        heightStrength = 30f;
        heightDamping = 6f;
        surfaceTransitionSpeed = 5f;

        // Hit Reaction
        hitImpulse = 6f;
        scuttleForce = 30f;
        scuttleTime = 0.6f;
        maxHorizontalSpeed = 6f;

        // Damage
        segmentHealth = 100f;
        enableLegDamage = true;

        // Ground Detection
        groundLayers = -1;
        raycastUp = 1.5f;
        raycastDown = 3f;
    }

    /// <summary>
    /// Serializes this configuration to a JSON string.
    /// </summary>
    /// <returns>JSON representation of the configuration</returns>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    /// <summary>
    /// Deserializes an IKConfiguration from a JSON string.
    /// Returns a default configuration if the JSON is invalid.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized configuration or default if invalid</returns>
    public static IKConfiguration FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[IKConfiguration] Empty or null JSON provided, returning default configuration.");
            return new IKConfiguration();
        }

        try
        {
            IKConfiguration config = JsonUtility.FromJson<IKConfiguration>(json);
            if (config == null)
            {
                Debug.LogWarning("[IKConfiguration] JSON deserialization returned null, returning default configuration.");
                return new IKConfiguration();
            }
            return config;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[IKConfiguration] Failed to deserialize JSON: {e.Message}. Returning default configuration.");
            return new IKConfiguration();
        }
    }


    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    /// <returns>A new IKConfiguration with the same values</returns>
    public IKConfiguration Clone()
    {
        return new IKConfiguration
        {
            // Leg Setup
            legCount = this.legCount,
            boneCount = this.boneCount,

            // Body Dimensions
            bodyRadius = this.bodyRadius,
            bodyHeight = this.bodyHeight,
            bodyToLegRatio = this.bodyToLegRatio,
            dimensionScale = this.dimensionScale,

            // Leg Proportions
            legLength = this.legLength,
            hipRatio = this.hipRatio,
            legSpread = this.legSpread,

            // Walking
            stepThreshold = this.stepThreshold,
            stepHeight = this.stepHeight,
            stepSpeed = this.stepSpeed,
            strideForward = this.strideForward,
            strideVelocityScale = this.strideVelocityScale,

            // Stabilization
            uprightStrength = this.uprightStrength,
            uprightDamping = this.uprightDamping,
            heightStrength = this.heightStrength,
            heightDamping = this.heightDamping,
            surfaceTransitionSpeed = this.surfaceTransitionSpeed,

            // Hit Reaction
            hitImpulse = this.hitImpulse,
            scuttleForce = this.scuttleForce,
            scuttleTime = this.scuttleTime,
            maxHorizontalSpeed = this.maxHorizontalSpeed,

            // Damage
            segmentHealth = this.segmentHealth,
            enableLegDamage = this.enableLegDamage,

            // Ground Detection
            groundLayers = this.groundLayers,
            raycastUp = this.raycastUp,
            raycastDown = this.raycastDown
        };
    }

    /// <summary>
    /// Checks if this configuration equals another configuration.
    /// Used for verifying round-trip serialization.
    /// </summary>
    /// <param name="other">Configuration to compare against</param>
    /// <returns>True if all values are equal</returns>
    public bool Equals(IKConfiguration other)
    {
        if (other == null) return false;

        const float epsilon = 0.0001f;

        return legCount == other.legCount &&
               boneCount == other.boneCount &&
               Mathf.Abs(bodyRadius - other.bodyRadius) < epsilon &&
               Mathf.Abs(bodyHeight - other.bodyHeight) < epsilon &&
               Mathf.Abs(bodyToLegRatio - other.bodyToLegRatio) < epsilon &&
               Mathf.Abs(dimensionScale - other.dimensionScale) < epsilon &&
               Mathf.Abs(legLength - other.legLength) < epsilon &&
               Mathf.Abs(hipRatio - other.hipRatio) < epsilon &&
               Mathf.Abs(legSpread - other.legSpread) < epsilon &&
               Mathf.Abs(stepThreshold - other.stepThreshold) < epsilon &&
               Mathf.Abs(stepHeight - other.stepHeight) < epsilon &&
               Mathf.Abs(stepSpeed - other.stepSpeed) < epsilon &&
               Mathf.Abs(strideForward - other.strideForward) < epsilon &&
               Mathf.Abs(strideVelocityScale - other.strideVelocityScale) < epsilon &&
               Mathf.Abs(uprightStrength - other.uprightStrength) < epsilon &&
               Mathf.Abs(uprightDamping - other.uprightDamping) < epsilon &&
               Mathf.Abs(heightStrength - other.heightStrength) < epsilon &&
               Mathf.Abs(heightDamping - other.heightDamping) < epsilon &&
               Mathf.Abs(surfaceTransitionSpeed - other.surfaceTransitionSpeed) < epsilon &&
               Mathf.Abs(hitImpulse - other.hitImpulse) < epsilon &&
               Mathf.Abs(scuttleForce - other.scuttleForce) < epsilon &&
               Mathf.Abs(scuttleTime - other.scuttleTime) < epsilon &&
               Mathf.Abs(maxHorizontalSpeed - other.maxHorizontalSpeed) < epsilon &&
               Mathf.Abs(segmentHealth - other.segmentHealth) < epsilon &&
               enableLegDamage == other.enableLegDamage &&
               groundLayers == other.groundLayers &&
               Mathf.Abs(raycastUp - other.raycastUp) < epsilon &&
               Mathf.Abs(raycastDown - other.raycastDown) < epsilon;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as IKConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + legCount.GetHashCode();
            hash = hash * 31 + boneCount.GetHashCode();
            hash = hash * 31 + bodyRadius.GetHashCode();
            hash = hash * 31 + bodyHeight.GetHashCode();
            hash = hash * 31 + bodyToLegRatio.GetHashCode();
            hash = hash * 31 + dimensionScale.GetHashCode();
            hash = hash * 31 + legLength.GetHashCode();
            hash = hash * 31 + hipRatio.GetHashCode();
            hash = hash * 31 + legSpread.GetHashCode();
            hash = hash * 31 + stepThreshold.GetHashCode();
            hash = hash * 31 + stepHeight.GetHashCode();
            hash = hash * 31 + stepSpeed.GetHashCode();
            hash = hash * 31 + strideForward.GetHashCode();
            hash = hash * 31 + strideVelocityScale.GetHashCode();
            hash = hash * 31 + uprightStrength.GetHashCode();
            hash = hash * 31 + uprightDamping.GetHashCode();
            hash = hash * 31 + heightStrength.GetHashCode();
            hash = hash * 31 + heightDamping.GetHashCode();
            hash = hash * 31 + surfaceTransitionSpeed.GetHashCode();
            hash = hash * 31 + hitImpulse.GetHashCode();
            hash = hash * 31 + scuttleForce.GetHashCode();
            hash = hash * 31 + scuttleTime.GetHashCode();
            hash = hash * 31 + maxHorizontalSpeed.GetHashCode();
            hash = hash * 31 + segmentHealth.GetHashCode();
            hash = hash * 31 + enableLegDamage.GetHashCode();
            hash = hash * 31 + groundLayers.GetHashCode();
            hash = hash * 31 + raycastUp.GetHashCode();
            hash = hash * 31 + raycastDown.GetHashCode();
            return hash;
        }
    }
}
