using UnityEngine;

/// <summary>
/// Custom generators for property-based testing of the Spider IK system.
/// Generates random but valid test data for property tests.
/// </summary>
public static class SpiderGenerators
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
    /// Generates a random IKConfiguration with valid values.
    /// </summary>
    public static IKConfiguration GenerateIKConfiguration()
    {
        return new IKConfiguration
        {
            // Leg Setup
            legCount = RandomInt(1, 8),
            boneCount = RandomInt(1, 3),

            // Body Dimensions
            bodyRadius = RandomFloat(0.1f, 1f),
            bodyHeight = RandomFloat(0.1f, 2f),
            bodyToLegRatio = RandomFloat(1f, 3f),
            dimensionScale = RandomFloat(0.5f, 2f),

            // Leg Proportions
            legLength = RandomFloat(0.1f, 2f),
            hipRatio = RandomFloat(0.2f, 0.8f),
            legSpread = RandomFloat(0.3f, 1.5f),

            // Walking
            stepThreshold = RandomFloat(0.1f, 1f),
            stepHeight = RandomFloat(0.05f, 0.3f),
            stepSpeed = RandomFloat(1f, 10f),
            strideForward = RandomFloat(0f, 1f),
            strideVelocityScale = RandomFloat(0f, 1f),

            // Stabilization
            uprightStrength = RandomFloat(5f, 50f),
            uprightDamping = RandomFloat(1f, 15f),
            heightStrength = RandomFloat(10f, 60f),
            heightDamping = RandomFloat(1f, 15f),
            surfaceTransitionSpeed = RandomFloat(1f, 10f),

            // Hit Reaction
            hitImpulse = RandomFloat(1f, 20f),
            scuttleForce = RandomFloat(10f, 100f),
            scuttleTime = RandomFloat(0.1f, 2f),
            maxHorizontalSpeed = RandomFloat(2f, 15f),

            // Damage
            segmentHealth = RandomFloat(50f, 200f),
            enableLegDamage = _random.Next(2) == 1,

            // Ground Detection
            groundLayers = -1, // All layers
            raycastUp = RandomFloat(0.5f, 3f),
            raycastDown = RandomFloat(1f, 5f)
        };
    }


    /// <summary>
    /// Generates a random Vector3 position within the specified range.
    /// </summary>
    public static Vector3 GeneratePosition(float maxDistance)
    {
        return new Vector3(
            RandomFloat(-maxDistance, maxDistance),
            RandomFloat(-maxDistance, maxDistance),
            RandomFloat(-maxDistance, maxDistance)
        );
    }

    /// <summary>
    /// Generates a random target position for IK testing.
    /// </summary>
    public static Vector3 GenerateTargetPosition(float legLength)
    {
        // Generate positions both within and beyond leg reach
        float distance = RandomFloat(0f, legLength * 1.5f);
        Vector3 direction = Random.onUnitSphere;
        return direction * distance;
    }

    /// <summary>
    /// Generates a random velocity vector.
    /// </summary>
    public static Vector3 GenerateVelocity(float maxSpeed)
    {
        return new Vector3(
            RandomFloat(-maxSpeed, maxSpeed),
            0f, // Typically horizontal velocity
            RandomFloat(-maxSpeed, maxSpeed)
        );
    }

    /// <summary>
    /// Generates an invalid JSON string for testing error handling.
    /// </summary>
    public static string GenerateInvalidJson()
    {
        int type = RandomInt(0, 5);
        switch (type)
        {
            case 0: return "";                              // Empty string
            case 1: return null;                            // Null
            case 2: return "not json at all";               // Plain text
            case 3: return "{ invalid json }";              // Malformed JSON
            case 4: return "{ \"unknownField\": 123 }";     // Wrong fields
            case 5: return "{ \"legCount\": \"notanumber\" }"; // Wrong type
            default: return "{}";                           // Empty object
        }
    }

    /// <summary>
    /// Generates a random LegData for testing.
    /// </summary>
    public static LegData GenerateLegData(int legIndex, int legCount)
    {
        LegData leg = new LegData
        {
            legIndex = legIndex,
            diagonalGroup = (legIndex % 2 == 0) ? 0 : 1,
            restTarget = GeneratePosition(1f),
            currentTarget = GeneratePosition(1f),
            plantedPos = GeneratePosition(1f),
            isStepping = _random.Next(2) == 1,
            stepProgress = RandomFloat(0f, 1f),
            lastStepTime = RandomFloat(-10f, 0f)
        };

        leg.InitializeSegments(RandomInt(1, 3), 100f);
        return leg;
    }

    /// <summary>
    /// Generates a random surface normal.
    /// </summary>
    public static Vector3 GenerateSurfaceNormal()
    {
        int type = RandomInt(0, 2);
        switch (type)
        {
            case 0: return Vector3.up;                      // Ground
            case 1: return Random.onUnitSphere;             // Random wall
            case 2: return Vector3.down;                    // Ceiling
            default: return Vector3.up;
        }
    }

    /// <summary>
    /// Generates a random target position relative to a hip position for IK testing.
    /// </summary>
    /// <param name="hipPosition">The hip position to generate target relative to</param>
    /// <param name="legLength">Total leg length for reach calculations</param>
    /// <param name="withinReach">If true, generates target within reach; if false, may be beyond reach</param>
    public static Vector3 GenerateIKTarget(Vector3 hipPosition, float legLength, bool withinReach)
    {
        float maxDistance = withinReach ? legLength * 0.95f : legLength * 1.5f;
        float minDistance = withinReach ? legLength * 0.1f : legLength * 1.05f;
        
        float distance = RandomFloat(minDistance, maxDistance);
        Vector3 direction = Random.onUnitSphere;
        
        return hipPosition + direction * distance;
    }

    /// <summary>
    /// Generates a random body center position for IK testing.
    /// </summary>
    public static Vector3 GenerateBodyCenter()
    {
        return new Vector3(
            RandomFloat(-5f, 5f),
            RandomFloat(0.5f, 3f),
            RandomFloat(-5f, 5f)
        );
    }

    /// <summary>
    /// Generates a random bone count (1, 2, or 3).
    /// </summary>
    public static int GenerateBoneCount()
    {
        return RandomInt(1, 3);
    }

    /// <summary>
    /// Generates random leg segment lengths.
    /// </summary>
    /// <param name="totalLength">Total leg length</param>
    /// <param name="hipRatio">Ratio of upper leg to total (0-1)</param>
    public static (float upper, float lower) GenerateSegmentLengths(float totalLength, float hipRatio)
    {
        float upper = totalLength * hipRatio;
        float lower = totalLength * (1f - hipRatio);
        return (upper, lower);
    }

    /// <summary>
    /// Generates a random normalized direction vector.
    /// </summary>
    public static Vector3 GenerateDirection()
    {
        // Generate random direction on unit sphere
        float theta = RandomFloat(0f, 2f * Mathf.PI);
        float phi = Mathf.Acos(RandomFloat(-1f, 1f));
        
        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Sin(phi) * Mathf.Sin(theta),
            Mathf.Cos(phi)
        ).normalized;
    }
}
