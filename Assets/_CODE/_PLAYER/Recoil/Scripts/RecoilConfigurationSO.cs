using UnityEngine;

/// <summary>
/// ScriptableObject for recoil configuration to allow easy editing in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "RecoilConfiguration", menuName = "Game/Recoil Configuration")]
public class RecoilConfigurationSO : ScriptableObject, System.IEquatable<RecoilConfigurationSO>
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

    private const float FloatTolerance = 0.0001f;

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static RecoilConfigurationSO FromJson(string json)
    {
        var instance = ScriptableObject.CreateInstance<RecoilConfigurationSO>();

        if (string.IsNullOrEmpty(json))
        {
            return instance;
        }

        try
        {
            JsonUtility.FromJsonOverwrite(json, instance);
        }
        catch (System.Exception)
        {
            // Return default instance on any parse failure
        }

        return instance;
    }

    public bool Equals(RecoilConfigurationSO other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return AreFloatsEqual(baseVerticalKick, other.baseVerticalKick) &&
               AreFloatsEqual(maxAccumulatedVertical, other.maxAccumulatedVertical) &&
               AreFloatsEqual(baseHorizontalKick, other.baseHorizontalKick) &&
               AreFloatsEqual(horizontalSpread, other.horizontalSpread) &&
               AreFloatsEqual(weaponKickbackDistance, other.weaponKickbackDistance) &&
               AreFloatsEqual(weaponRotationKick, other.weaponRotationKick) &&
               AreFloatsEqual(recoverySpeed, other.recoverySpeed) &&
               AreCurvesEqual(recoveryCurve, other.recoveryCurve) &&
               AreFloatsEqual(swaySmoothness, other.swaySmoothness) &&
               AreFloatsEqual(verticalVariationMin, other.verticalVariationMin) &&
               AreFloatsEqual(verticalVariationMax, other.verticalVariationMax) &&
               AreFloatsEqual(noiseScale, other.noiseScale) &&
               AreFloatsEqual(compensationMultiplier, other.compensationMultiplier) &&
               AreFloatsEqual(maxCompensationRate, other.maxCompensationRate) &&
               AreFloatsEqual(shakeIntensity, other.shakeIntensity) &&
               AreFloatsEqual(shakeDuration, other.shakeDuration) &&
               AreFloatsEqual(shakeFrequency, other.shakeFrequency) &&
               AreFloatsEqual(reloadRotationPitch, other.reloadRotationPitch) &&
               AreFloatsEqual(reloadRotationYaw, other.reloadRotationYaw) &&
               AreVectorsEqual(reloadPositionOffset, other.reloadPositionOffset) &&
               AreCurvesEqual(reloadAnimationCurve, other.reloadAnimationCurve) &&
               AreFloatsEqual(pathFollowStrength, other.pathFollowStrength);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as RecoilConfigurationSO);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + baseVerticalKick.GetHashCode();
            hash = hash * 23 + maxAccumulatedVertical.GetHashCode();
            hash = hash * 23 + baseHorizontalKick.GetHashCode();
            hash = hash * 23 + horizontalSpread.GetHashCode();
            hash = hash * 23 + weaponKickbackDistance.GetHashCode();
            hash = hash * 23 + weaponRotationKick.GetHashCode();
            hash = hash * 23 + recoverySpeed.GetHashCode();
            hash = hash * 23 + swaySmoothness.GetHashCode();
            hash = hash * 23 + verticalVariationMin.GetHashCode();
            hash = hash * 23 + verticalVariationMax.GetHashCode();
            hash = hash * 23 + noiseScale.GetHashCode();
            hash = hash * 23 + compensationMultiplier.GetHashCode();
            hash = hash * 23 + maxCompensationRate.GetHashCode();
            hash = hash * 23 + shakeIntensity.GetHashCode();
            hash = hash * 23 + shakeDuration.GetHashCode();
            hash = hash * 23 + shakeFrequency.GetHashCode();
            hash = hash * 23 + reloadRotationPitch.GetHashCode();
            hash = hash * 23 + reloadRotationYaw.GetHashCode();
            hash = hash * 23 + reloadPositionOffset.GetHashCode();
            hash = hash * 23 + pathFollowStrength.GetHashCode();
            hash = hash * 23 + (recoveryCurve != null ? recoveryCurve.length.GetHashCode() : 0);
            hash = hash * 23 + (reloadAnimationCurve != null ? reloadAnimationCurve.length.GetHashCode() : 0);
            return hash;
        }
    }

    private bool AreFloatsEqual(float a, float b)
    {
        return Mathf.Abs(a - b) <= FloatTolerance;
    }

    private bool AreVectorsEqual(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= FloatTolerance * FloatTolerance;
    }

    private bool AreCurvesEqual(AnimationCurve a, AnimationCurve b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.length != b.length) return false;

        var keysA = a.keys;
        var keysB = b.keys;
        for (int i = 0; i < keysA.Length; i++)
        {
            if (!AreFloatsEqual(keysA[i].time, keysB[i].time) ||
                !AreFloatsEqual(keysA[i].value, keysB[i].value) ||
                !AreFloatsEqual(keysA[i].inTangent, keysB[i].inTangent) ||
                !AreFloatsEqual(keysA[i].outTangent, keysB[i].outTangent))
            {
                return false;
            }
        }

        return true;
    }
}
