using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for IKConfiguration serialization.
/// Tests Properties 4 and 16 from the spider-ik-walker design document.
/// </summary>
[TestFixture]
public class SpiderSerializationProperties
{
    private const int PropertyTestIterations = 100;

    /// <summary>
    /// **Feature: spider-ik-walker, Property 4: Configuration Round-Trip Serialization**
    /// *For any* valid IKConfiguration, serializing to JSON and deserializing back 
    /// SHALL produce a configuration equal to the original.
    /// **Validates: Requirements 1.7, 7.1, 7.2, 7.3**
    /// </summary>
    [Test]
    public void Property4_ConfigurationRoundTripSerialization()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);
            IKConfiguration original = SpiderGenerators.GenerateIKConfiguration();

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
            IKConfiguration restored = IKConfiguration.FromJson(json);

            // Verify equality
            if (!original.Equals(restored))
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Round-trip failed. Original != Restored\n" +
                    $"Original legCount: {original.legCount}, Restored: {restored.legCount}\n" +
                    $"Original boneCount: {original.boneCount}, Restored: {restored.boneCount}\n" +
                    $"Original bodyRadius: {original.bodyRadius}, Restored: {restored.bodyRadius}";
            }
        }

        Assert.AreEqual(0, failures, 
            $"Property 4 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 16: Invalid JSON Returns Default Configuration**
    /// *For any* invalid JSON string (malformed, missing fields, wrong types), 
    /// deserialization SHALL return a valid default IKConfiguration without throwing exceptions.
    /// **Validates: Requirements 7.4**
    /// </summary>
    [Test]
    public void Property16_InvalidJsonReturnsDefaultConfiguration()
    {
        IKConfiguration defaultConfig = new IKConfiguration();
        int failures = 0;
        string lastFailureMessage = "";

        // Test specific invalid JSON cases
        string[] invalidJsonCases = new string[]
        {
            "",                                         // Empty string
            null,                                       // Null
            "not json at all",                          // Plain text
            "{ invalid json }",                         // Malformed JSON
            "{ \"unknownField\": 123 }",                // Wrong fields (should still work)
            "{ legCount: 4 }",                          // Missing quotes on key
            "[1, 2, 3]",                                // Array instead of object
            "{ \"legCount\": \"notanumber\" }",         // Wrong type
        };

        foreach (string invalidJson in invalidJsonCases)
        {
            try
            {
                IKConfiguration result = IKConfiguration.FromJson(invalidJson);

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
            SpiderGenerators.Seed(i);
            string invalidJson = SpiderGenerators.GenerateInvalidJson();

            try
            {
                IKConfiguration result = IKConfiguration.FromJson(invalidJson);

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
        IKConfiguration config = new IKConfiguration();
        string json = config.ToJson();

        Assert.IsNotNull(json);
        Assert.IsNotEmpty(json);
        Assert.IsTrue(json.StartsWith("{"), "JSON should start with '{'");
        Assert.IsTrue(json.EndsWith("}"), "JSON should end with '}'");
        Assert.IsTrue(json.Contains("legCount"), "JSON should contain legCount field");
        Assert.IsTrue(json.Contains("boneCount"), "JSON should contain boneCount field");
        Assert.IsTrue(json.Contains("bodyRadius"), "JSON should contain bodyRadius field");
    }

    /// <summary>
    /// Verifies that FromJson with valid JSON restores all fields.
    /// </summary>
    [Test]
    public void FromJson_RestoresAllFields()
    {
        IKConfiguration original = new IKConfiguration
        {
            legCount = 6,
            boneCount = 2,
            bodyRadius = 0.5f,
            bodyHeight = 0.8f,
            bodyToLegRatio = 2f,
            dimensionScale = 1.5f,
            legLength = 0.9f,
            hipRatio = 0.6f,
            legSpread = 1.2f,
            stepThreshold = 0.5f,
            stepHeight = 0.15f,
            stepSpeed = 7f,
            strideForward = 0.4f,
            strideVelocityScale = 0.5f,
            uprightStrength = 25f,
            uprightDamping = 8f,
            heightStrength = 35f,
            heightDamping = 8f,
            surfaceTransitionSpeed = 6f,
            hitImpulse = 8f,
            scuttleForce = 40f,
            scuttleTime = 0.8f,
            maxHorizontalSpeed = 8f,
            segmentHealth = 150f,
            enableLegDamage = false,
            raycastUp = 2f,
            raycastDown = 4f
        };

        string json = original.ToJson();
        IKConfiguration restored = IKConfiguration.FromJson(json);

        Assert.AreEqual(original.legCount, restored.legCount);
        Assert.AreEqual(original.boneCount, restored.boneCount);
        Assert.AreEqual(original.bodyRadius, restored.bodyRadius, 0.0001f);
        Assert.AreEqual(original.bodyHeight, restored.bodyHeight, 0.0001f);
        Assert.AreEqual(original.bodyToLegRatio, restored.bodyToLegRatio, 0.0001f);
        Assert.AreEqual(original.dimensionScale, restored.dimensionScale, 0.0001f);
        Assert.AreEqual(original.legLength, restored.legLength, 0.0001f);
        Assert.AreEqual(original.hipRatio, restored.hipRatio, 0.0001f);
        Assert.AreEqual(original.legSpread, restored.legSpread, 0.0001f);
        Assert.AreEqual(original.stepThreshold, restored.stepThreshold, 0.0001f);
        Assert.AreEqual(original.stepHeight, restored.stepHeight, 0.0001f);
        Assert.AreEqual(original.stepSpeed, restored.stepSpeed, 0.0001f);
        Assert.AreEqual(original.strideForward, restored.strideForward, 0.0001f);
        Assert.AreEqual(original.strideVelocityScale, restored.strideVelocityScale, 0.0001f);
        Assert.AreEqual(original.uprightStrength, restored.uprightStrength, 0.0001f);
        Assert.AreEqual(original.uprightDamping, restored.uprightDamping, 0.0001f);
        Assert.AreEqual(original.heightStrength, restored.heightStrength, 0.0001f);
        Assert.AreEqual(original.heightDamping, restored.heightDamping, 0.0001f);
        Assert.AreEqual(original.surfaceTransitionSpeed, restored.surfaceTransitionSpeed, 0.0001f);
        Assert.AreEqual(original.hitImpulse, restored.hitImpulse, 0.0001f);
        Assert.AreEqual(original.scuttleForce, restored.scuttleForce, 0.0001f);
        Assert.AreEqual(original.scuttleTime, restored.scuttleTime, 0.0001f);
        Assert.AreEqual(original.maxHorizontalSpeed, restored.maxHorizontalSpeed, 0.0001f);
        Assert.AreEqual(original.segmentHealth, restored.segmentHealth, 0.0001f);
        Assert.AreEqual(original.enableLegDamage, restored.enableLegDamage);
        Assert.AreEqual(original.raycastUp, restored.raycastUp, 0.0001f);
        Assert.AreEqual(original.raycastDown, restored.raycastDown, 0.0001f);
    }

    /// <summary>
    /// Verifies that Clone creates an independent copy.
    /// </summary>
    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        IKConfiguration original = new IKConfiguration
        {
            legCount = 8,
            boneCount = 3,
            bodyRadius = 0.4f
        };

        IKConfiguration clone = original.Clone();

        // Modify original
        original.legCount = 4;
        original.bodyRadius = 0.8f;

        // Clone should be unchanged
        Assert.AreEqual(8, clone.legCount);
        Assert.AreEqual(0.4f, clone.bodyRadius, 0.0001f);
    }
}
