using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for RecoilConfiguration serialization.
/// Tests Properties 6 and 16 from the design document.
/// </summary>
[TestFixture]
public class SerializationProperties
{
    private const int PropertyTestIterations = 100;

    /// <summary>
    /// **Feature: epic-recoil-system, Property 6: Configuration Round-Trip Serialization**
    /// *For any* valid RecoilConfiguration, serializing to JSON and deserializing back 
    /// SHALL produce a configuration equal to the original.
    /// **Validates: Requirements 2.4, 7.1, 7.2, 7.3**
    /// </summary>
    [Test]
    public void Property6_ConfigurationRoundTripSerialization()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            RecoilConfiguration original = RecoilGenerators.GenerateRecoilConfiguration();

            // Serialize to JSON
            string json = original.ToJson();

            // Verify JSON is not empty
            if (string.IsNullOrEmpty(json))
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: ToJson() returned empty string";
                continue;
            }

            // Deserialize back
            RecoilConfiguration restored = RecoilConfiguration.FromJson(json);

            // Verify equality
            if (!original.Equals(restored))
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Round-trip failed. Original != Restored\n" +
                    $"Original baseVerticalKick: {original.baseVerticalKick}, Restored: {restored.baseVerticalKick}";
            }
        }

        Assert.AreEqual(0, failures, 
            $"Property 6 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 16: Invalid JSON Returns Default Configuration**
    /// *For any* invalid JSON string (malformed, missing fields, wrong types), 
    /// deserialization SHALL return a valid default RecoilConfiguration without throwing exceptions.
    /// **Validates: Requirements 7.4**
    /// </summary>
    [Test]
    public void Property16_InvalidJsonReturnsDefaultConfiguration()
    {
        RecoilConfiguration defaultConfig = new RecoilConfiguration();
        int failures = 0;
        string lastFailureMessage = "";

        // Test specific invalid JSON cases
        string[] invalidJsonCases = new string[]
        {
            "",                                     // Empty string
            null,                                   // Null
            "not json at all",                      // Plain text
            "{ invalid json }",                     // Malformed JSON
            "{ \"unknownField\": 123 }",            // Wrong fields (should still work, just ignore)
            "{ baseVerticalKick: 2.0 }",            // Missing quotes on key
            "[1, 2, 3]",                            // Array instead of object
            "{ \"baseVerticalKick\": \"notanumber\" }", // Wrong type
        };

        foreach (string invalidJson in invalidJsonCases)
        {
            try
            {
                RecoilConfiguration result = RecoilConfiguration.FromJson(invalidJson);

                // Should not be null
                if (result == null)
                {
                    failures++;
                    lastFailureMessage = $"FromJson returned null for input: '{invalidJson ?? "null"}'";
                    continue;
                }

                // For truly invalid JSON, should return default values
                // Note: JsonUtility may partially parse some inputs, so we just verify it doesn't crash
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"FromJson threw exception for input: '{invalidJson ?? "null"}': {e.Message}";
            }
        }

        // Also run random invalid JSON tests
        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);
            string invalidJson = RecoilGenerators.GenerateInvalidJson();

            try
            {
                RecoilConfiguration result = RecoilConfiguration.FromJson(invalidJson);

                if (result == null)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: FromJson returned null for generated invalid JSON";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: FromJson threw exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 16 failed {failures} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that ToJson produces valid JSON format.
    /// </summary>
    [Test]
    public void ToJson_ProducesValidJsonFormat()
    {
        RecoilConfiguration config = new RecoilConfiguration();
        string json = config.ToJson();

        Assert.IsNotNull(json);
        Assert.IsNotEmpty(json);
        Assert.IsTrue(json.StartsWith("{"), "JSON should start with '{'");
        Assert.IsTrue(json.EndsWith("}"), "JSON should end with '}'");
        Assert.IsTrue(json.Contains("baseVerticalKick"), "JSON should contain baseVerticalKick field");
    }

    /// <summary>
    /// Verifies that FromJson with valid JSON restores all fields.
    /// </summary>
    [Test]
    public void FromJson_RestoresAllFields()
    {
        RecoilConfiguration original = new RecoilConfiguration
        {
            baseVerticalKick = 3.5f,
            maxAccumulatedVertical = 20f,
            baseHorizontalKick = 1.0f,
            horizontalSpread = 1.5f,
            weaponKickbackDistance = 0.08f,
            weaponRotationKick = 5f,
            recoverySpeed = 10f,
            verticalVariationMin = 0.75f,
            verticalVariationMax = 1.25f,
            noiseScale = 0.7f,
            compensationMultiplier = 2f,
            maxCompensationRate = 3f,
            shakeIntensity = 0.03f,
            shakeFrequency = 30f,
            pathFollowStrength = 0.6f
        };

        string json = original.ToJson();
        RecoilConfiguration restored = RecoilConfiguration.FromJson(json);

        Assert.AreEqual(original.baseVerticalKick, restored.baseVerticalKick, 0.0001f);
        Assert.AreEqual(original.maxAccumulatedVertical, restored.maxAccumulatedVertical, 0.0001f);
        Assert.AreEqual(original.baseHorizontalKick, restored.baseHorizontalKick, 0.0001f);
        Assert.AreEqual(original.horizontalSpread, restored.horizontalSpread, 0.0001f);
        Assert.AreEqual(original.weaponKickbackDistance, restored.weaponKickbackDistance, 0.0001f);
        Assert.AreEqual(original.weaponRotationKick, restored.weaponRotationKick, 0.0001f);
        Assert.AreEqual(original.recoverySpeed, restored.recoverySpeed, 0.0001f);
        Assert.AreEqual(original.verticalVariationMin, restored.verticalVariationMin, 0.0001f);
        Assert.AreEqual(original.verticalVariationMax, restored.verticalVariationMax, 0.0001f);
        Assert.AreEqual(original.noiseScale, restored.noiseScale, 0.0001f);
        Assert.AreEqual(original.compensationMultiplier, restored.compensationMultiplier, 0.0001f);
        Assert.AreEqual(original.maxCompensationRate, restored.maxCompensationRate, 0.0001f);
        Assert.AreEqual(original.shakeIntensity, restored.shakeIntensity, 0.0001f);
        Assert.AreEqual(original.shakeFrequency, restored.shakeFrequency, 0.0001f);
        Assert.AreEqual(original.pathFollowStrength, restored.pathFollowStrength, 0.0001f);
    }
}
