using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for RecoilRandomizer module.
/// Tests Properties 4 and 5 from the design document.
/// </summary>
[TestFixture]
public class RecoilRandomizerProperties
{
    private const int PropertyTestIterations = 100;
    private GameObject _testObject;
    private RecoilRandomizer _randomizer;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestRandomizer");
        _randomizer = _testObject.AddComponent<RecoilRandomizer>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_testObject != null)
        {
            Object.DestroyImmediate(_testObject);
        }
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 4: Randomizer Output Within Configured Bounds**
    /// *For any* base kick value and spread configuration, the RecoilRandomizer output SHALL have:
    /// - Vertical component between `baseVertical * 0.8` and `baseVertical * 1.2`
    /// - Horizontal component between `-horizontalSpread` and `+horizontalSpread`
    /// **Validates: Requirements 2.2, 2.3**
    /// </summary>
    [Test]
    public void Property4_RandomizerOutputWithinConfiguredBounds()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate random configuration values
            float baseVertical = RecoilGenerators.RandomFloat(0.5f, 5f);
            float baseHorizontal = RecoilGenerators.RandomFloat(-2f, 2f);
            float horizontalSpread = RecoilGenerators.RandomFloat(0.1f, 4f);
            float verticalMin = 0.8f;
            float verticalMax = 1.2f;


            // Configure the randomizer
            _randomizer.HorizontalSpread = horizontalSpread;
            _randomizer.VerticalVariationMin = verticalMin;
            _randomizer.VerticalVariationMax = verticalMax;

            // Generate multiple kicks to test bounds across different shot counts
            for (int shot = 0; shot < 10; shot++)
            {
                Vector2 kick = _randomizer.GenerateRecoilKick(baseVertical, baseHorizontal);

                // Check vertical bounds (80-120% of base)
                float expectedVerticalMin = baseVertical * verticalMin;
                float expectedVerticalMax = baseVertical * verticalMax;

                if (kick.x < expectedVerticalMin - 0.001f || kick.x > expectedVerticalMax + 0.001f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}, Shot {shot}: Vertical {kick.x} outside bounds [{expectedVerticalMin}, {expectedVerticalMax}] for base {baseVertical}";
                    break;
                }

                // Check horizontal bounds (within ±spread)
                if (kick.y < -horizontalSpread - 0.001f || kick.y > horizontalSpread + 0.001f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}, Shot {shot}: Horizontal {kick.y} outside bounds [{-horizontalSpread}, {horizontalSpread}]";
                    break;
                }

                // Simulate shot for next iteration
                _randomizer.OnRecoilApplied(kick);
            }

            // Reset for next test iteration
            _randomizer.Reset();
        }

        Assert.AreEqual(0, failures,
            $"Property 4 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: epic-recoil-system, Property 5: Randomizer Produces Variation**
    /// *For any* sequence of consecutive shots, the RecoilRandomizer SHALL produce 
    /// at least two distinct recoil values (not all identical), demonstrating procedural variation.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Test]
    public void Property5_RandomizerProducesVariation()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            RecoilGenerators.Seed(i);

            // Generate random configuration
            float baseVertical = RecoilGenerators.RandomFloat(0.5f, 5f);
            float baseHorizontal = RecoilGenerators.RandomFloat(-2f, 2f);
            float horizontalSpread = RecoilGenerators.RandomFloat(0.5f, 4f);

            _randomizer.HorizontalSpread = horizontalSpread;
            _randomizer.Reset();

            // Generate a sequence of kicks
            Vector2[] kicks = new Vector2[10];
            for (int shot = 0; shot < kicks.Length; shot++)
            {
                kicks[shot] = _randomizer.GenerateRecoilKick(baseVertical, baseHorizontal);
                _randomizer.OnRecoilApplied(kicks[shot]);
            }

            // Check that not all kicks are identical
            bool hasVariation = false;
            Vector2 firstKick = kicks[0];
            const float epsilon = 0.0001f;

            for (int j = 1; j < kicks.Length; j++)
            {
                if (Mathf.Abs(kicks[j].x - firstKick.x) > epsilon ||
                    Mathf.Abs(kicks[j].y - firstKick.y) > epsilon)
                {
                    hasVariation = true;
                    break;
                }
            }

            if (!hasVariation)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: All 10 kicks were identical ({firstKick}), no variation produced";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 5 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Verifies that disabled randomizer returns base values unchanged.
    /// </summary>
    [Test]
    public void DisabledRandomizer_ReturnsBaseValues()
    {
        _randomizer.IsEnabled = false;

        float baseVertical = 2.5f;
        float baseHorizontal = 0.5f;

        Vector2 kick = _randomizer.GenerateRecoilKick(baseVertical, baseHorizontal);

        Assert.AreEqual(baseVertical, kick.x, 0.0001f, "Disabled randomizer should return base vertical");
        Assert.AreEqual(baseHorizontal, kick.y, 0.0001f, "Disabled randomizer should return base horizontal");
    }

    /// <summary>
    /// Verifies that Reset clears the shot counter.
    /// </summary>
    [Test]
    public void Reset_ClearsShotCounter()
    {
        // Fire some shots
        for (int i = 0; i < 5; i++)
        {
            _randomizer.OnRecoilApplied(Vector2.one);
        }

        Assert.AreEqual(5, _randomizer.ShotCount, "Shot count should be 5 after 5 shots");

        _randomizer.Reset();

        Assert.AreEqual(0, _randomizer.ShotCount, "Shot count should be 0 after reset");
    }
}
