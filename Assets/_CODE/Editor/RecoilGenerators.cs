using UnityEngine;

/// <summary>
/// Custom generators for property-based testing of the recoil system.
/// Generates random but valid test data for property tests.
/// </summary>
public static class RecoilGenerators
{
    private static System.Random _random = new System.Random();

    /// <summary>
    /// Reseeds the random generator for reproducible tests.
    /// </summary>
    public static void Seed(int seed)
    {
        _random = new System.Random(seed);
    }

    /// <summary>
    /// Generates a random float within the specified range.
    /// </summary>
    public static float RandomFloat(float min, float max)
    {
        return (float)(_random.NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// Generates a random integer within the specified range.
    /// </summary>
    public static int RandomInt(int min, int max)
    {
        return _random.Next(min, max + 1);
    }

    /// <summary>
    /// Generates a random RecoilConfigurationSO with valid values.
    /// </summary>
    public static RecoilConfigurationSO GenerateRecoilConfiguration()
    {
        return ScriptableObject.CreateInstance<RecoilConfigurationSO>().InitializeWithRandomValues();
    }

    /// <summary>
    /// Extension method to initialize RecoilConfigurationSO with random values.
    /// </summary>
    public static RecoilConfigurationSO InitializeWithRandomValues(this RecoilConfigurationSO config)
    {
        // Vertical Recoil (0.5-5 range)
        config.baseVerticalKick = RandomFloat(0.5f, 5f);
        config.maxAccumulatedVertical = RandomFloat(5f, 30f);

        // Horizontal Recoil
        config.baseHorizontalKick = RandomFloat(-2f, 2f);
        config.horizontalSpread = RandomFloat(0.1f, 4f);

        // Weapon Transform
        config.weaponKickbackDistance = RandomFloat(0.01f, 0.2f);
        config.weaponRotationKick = RandomFloat(1f, 10f);

        // Recovery
        config.recoverySpeed = RandomFloat(1f, 20f);
        // Note: AnimationCurve is not serialized well by JsonUtility, using default

        // Randomizer
        config.verticalVariationMin = RandomFloat(0.5f, 0.95f);
        config.verticalVariationMax = RandomFloat(1.05f, 1.5f);
        config.noiseScale = RandomFloat(0.1f, 2f);

        // Mouse Tracking
        config.compensationMultiplier = RandomFloat(0.5f, 3f);
        config.maxCompensationRate = RandomFloat(1f, 5f);

        // Camera Shake
        config.shakeIntensity = RandomFloat(0.005f, 0.1f);
        config.shakeFrequency = RandomFloat(10f, 50f);
        config.pathFollowStrength = RandomFloat(0f, 1f);

        return config;
    }

    /// <summary>
    /// Generates a random Vector2 for mouse input simulation.
    /// </summary>
    public static Vector2 GenerateMouseInput()
    {
        return new Vector2(
            RandomFloat(-10f, 10f),
            RandomFloat(-10f, 10f)
        );
    }

    /// <summary>
    /// Generates a random Vector2 for recoil delta.
    /// </summary>
    public static Vector2 GenerateRecoilDelta()
    {
        return new Vector2(
            RandomFloat(0.5f, 5f),   // Vertical (always positive/upward)
            RandomFloat(-2f, 2f)     // Horizontal (can be either direction)
        );
    }

    /// <summary>
    /// Generates an invalid JSON string for testing error handling.
    /// </summary>
    public static string GenerateInvalidJson()
    {
        int type = RandomInt(0, 4);
        switch (type)
        {
            case 0: return "";                          // Empty string
            case 1: return null;                        // Null
            case 2: return "not json at all";           // Plain text
            case 3: return "{ invalid json }";          // Malformed JSON
            case 4: return "{ \"unknownField\": 123 }"; // Wrong fields
            default: return "{}";                       // Empty object
        }
    }
}
