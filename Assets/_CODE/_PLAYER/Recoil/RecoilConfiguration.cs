using UnityEngine;

/// <summary>
/// Serializable configuration for the Epic Recoil System.
/// Contains all parameters for camera recoil, weapon transform recoil,
/// randomization, mouse tracking, and camera shake.
/// </summary>
[System.Serializable]
public class RecoilConfiguration
{
    [Header("Vertical Recoil")]
    [Tooltip("Base upward rotation kick in degrees per shot (0.5-5 range enforced at runtime)")]
    [Range(0.5f, 5f)]
    public float baseVerticalKick = 2f;

    [Tooltip("Maximum accumulated vertical recoil in degrees before clamping")]
    [Min(0f)]
    public float maxAccumulatedVertical = 15f;

    [Header("Horizontal Recoil")]
    [Tooltip("Base horizontal rotation kick in degrees per shot")]
    [Range(-2f, 2f)]
    public float baseHorizontalKick = 0.5f;

    [Tooltip("Maximum horizontal spread range (+/-) in degrees")]
    [Min(0f)]
    public float horizontalSpread = 2f;

    [Header("Weapon Transform Recoil")]
    [Tooltip("Distance the weapon kicks backward along local Z-axis in meters")]
    [Min(0f)]
    public float weaponKickbackDistance = 0.05f;

    [Tooltip("Rotation kick applied to weapon around local X-axis in degrees")]
    [Min(0f)]
    public float weaponRotationKick = 3f;

    [Header("Recovery")]
    [Tooltip("Base speed at which recoil recovers (higher = faster recovery)")]
    [Min(0.1f)]
    public float recoverySpeed = 8f;

    [Tooltip("Animation curve defining recovery falloff over time (0-1 normalized)")]
    public AnimationCurve recoveryCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Weapon Sway")]
    [Tooltip("Smoothness of the weapon sway")]
    [Min(0.1f)]
    public float swaySmoothness = 4f;

    [Header("Randomizer Settings")]
    [Tooltip("Minimum multiplier for vertical kick variation (0.8 = 80% of base)")]
    [Range(0.5f, 1f)]
    public float verticalVariationMin = 0.8f;

    [Tooltip("Maximum multiplier for vertical kick variation (1.2 = 120% of base)")]
    [Range(1f, 1.5f)]
    public float verticalVariationMax = 1.2f;

    [Tooltip("Scale factor for Perlin noise sampling (affects pattern smoothness)")]
    [Min(0.01f)]
    public float noiseScale = 0.5f;

    [Header("Mouse Tracking")]
    [Tooltip("Multiplier for mouse compensation effectiveness")]
    [Min(0f)]
    public float compensationMultiplier = 1.5f;

    [Tooltip("Maximum rate at which compensation can accelerate recovery")]
    [Min(0f)]
    public float maxCompensationRate = 2f;

    [Header("Camera Shake")]
    [Tooltip("Base intensity of camera shake in meters")]
    [Min(0f)]
    public float shakeIntensity = 0.01f;

    [Tooltip("How long the shake should last after a shot")]
    [Min(0f)]
    public float shakeDuration = 0.2f;

    [Tooltip("Frequency of shake oscillation in Hz")]
    [Min(1f)]
    public float shakeFrequency = 25f;

    [Header("Reload Animation")]
    [Tooltip("Pitch rotation during reload (negative = rotate down/back)")]
    public float reloadRotationPitch = -15f;

    [Tooltip("Yaw rotation during reload (positive = rotate right)")]
    public float reloadRotationYaw = 25f;

    [Tooltip("Position offset during reload (x=right, y=up, z=forward)")]
    public Vector3 reloadPositionOffset = new Vector3(0.03f, -0.02f, 0.08f);

    [Tooltip("Animation curve for reload motion (0-1 normalized)")]
    public AnimationCurve reloadAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Deprecated, but kept for editor script compatibility
    public float pathFollowStrength = 0.5f;

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static RecoilConfiguration FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new RecoilConfiguration();
        }
        return JsonUtility.FromJson<RecoilConfiguration>(json) ?? new RecoilConfiguration();
    }
}
