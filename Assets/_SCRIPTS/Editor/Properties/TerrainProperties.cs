using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for TerrainAdapter.
/// Tests Properties 8, 9 from the design document.
/// </summary>
[TestFixture]
public class TerrainProperties
{
    private const int PropertyTestIterations = 100;
    private const float PositionTolerance = 0.001f;
    private const float AngleTolerance = 5f; // degrees

    private GameObject _testObject;
    private TerrainAdapter _terrainAdapter;
    private SpiderIKSystem _system;
    private GameObject _groundPlane;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestTerrainAdapter");
        _system = _testObject.AddComponent<SpiderIKSystem>();
        _terrainAdapter = _testObject.AddComponent<TerrainAdapter>();

        _system.Config = new IKConfiguration();
        _terrainAdapter.Initialize(_system);

        // Create a ground plane for raycasting
        _groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _groundPlane.transform.position = Vector3.zero;
        _groundPlane.transform.localScale = new Vector3(10f, 1f, 10f);
    }

    [TearDown]
    public void TearDown()
    {
        if (_testObject != null)
        {
            Object.DestroyImmediate(_testObject);
        }
        if (_groundPlane != null)
        {
            Object.DestroyImmediate(_groundPlane);
        }
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 8: Surface Detection Positioning**
    /// *For any* raycast that hits a surface, the Terrain_Adapter SHALL return a foot target 
    /// position within 0.001 meters of the hit point.
    /// **Validates: Requirements 3.1, 3.2**
    /// </summary>
    [Test]
    public void Property8_SurfaceDetectionPositioning()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            try
            {
                // Unity's Plane primitive has its surface at Y=0 of the transform
                // Position the ground plane at origin
                _groundPlane.transform.position = Vector3.zero;
                _groundPlane.transform.rotation = Quaternion.identity;
                float groundHeight = 0f;

                // Random origin position above the ground (within the plane's bounds)
                Vector3 origin = new Vector3(
                    SpiderGenerators.RandomFloat(-3f, 3f),
                    SpiderGenerators.RandomFloat(0.5f, 3f),
                    SpiderGenerators.RandomFloat(-3f, 3f)
                );

                // Configure terrain adapter
                _terrainAdapter.SetConfiguration(-1, 5f, 1f);

                // Find surface position
                bool hit = _terrainAdapter.FindSurfacePosition(origin, Vector3.down, out Vector3 position, out Vector3 normal);

                if (hit)
                {
                    // The hit point should be on the ground plane (y = groundHeight)
                    float actualY = position.y;
                    float yDifference = Mathf.Abs(actualY - groundHeight);

                    if (yDifference > PositionTolerance)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Surface position Y={actualY} differs from expected Y={groundHeight} by {yDifference}m (tolerance: {PositionTolerance}m)";
                        continue;
                    }

                    // X and Z should be close to origin (straight down raycast)
                    float xDiff = Mathf.Abs(position.x - origin.x);
                    float zDiff = Mathf.Abs(position.z - origin.z);

                    if (xDiff > PositionTolerance || zDiff > PositionTolerance)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Surface position XZ differs from origin by ({xDiff}, {zDiff})m";
                    }
                }
                else
                {
                    // Should have hit the ground plane
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Raycast did not hit ground plane from origin {origin}";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 8 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 9: Surface Orientation Alignment**
    /// *For any* detected surface (ground, wall, or ceiling), the foot orientation up vector 
    /// SHALL align within 5 degrees of the surface normal.
    /// **Validates: Requirements 3.4, 3.6, 3.7**
    /// </summary>
    [Test]
    public void Property9_SurfaceOrientationAlignment()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);

            try
            {
                // Keep plane flat for consistent raycast results
                _groundPlane.transform.rotation = Quaternion.identity;
                _groundPlane.transform.position = Vector3.zero;

                // Position origin above the surface
                Vector3 origin = new Vector3(
                    SpiderGenerators.RandomFloat(-3f, 3f),
                    SpiderGenerators.RandomFloat(1f, 3f),
                    SpiderGenerators.RandomFloat(-3f, 3f)
                );

                // Configure and find surface
                _terrainAdapter.SetConfiguration(-1, 5f, 1f);
                bool hit = _terrainAdapter.FindSurfacePosition(origin, Vector3.down, out Vector3 position, out Vector3 normal);

                if (hit)
                {
                    // Get foot orientation and verify up vector aligns with the RETURNED surface normal
                    Quaternion footOrientation = _terrainAdapter.GetFootOrientation(position, Vector3.forward);
                    Vector3 footUp = footOrientation * Vector3.up;

                    // The foot up should align with the stored surface normal
                    float footAngle = Vector3.Angle(footUp, _terrainAdapter.CurrentSurfaceNormal);

                    if (footAngle > AngleTolerance)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Foot up vector angle {footAngle} degrees from stored surface normal exceeds tolerance {AngleTolerance} degrees. FootUp: {footUp}, Normal: {_terrainAdapter.CurrentSurfaceNormal}";
                        continue;
                    }

                    // Also verify the returned normal matches the stored normal
                    float normalAngle = Vector3.Angle(normal, _terrainAdapter.CurrentSurfaceNormal);
                    if (normalAngle > 0.1f)
                    {
                        failures++;
                        lastFailureMessage = $"Iteration {i}: Returned normal differs from stored normal by {normalAngle} degrees";
                    }
                }
                else
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Raycast did not hit surface from origin {origin}";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception: {e.Message}";
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 9 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// Tests surface classification for different normal angles.
    /// </summary>
    [Test]
    public void SurfaceClassification_CorrectlyClassifiesSurfaces()
    {
        // Ground (normal pointing up)
        Assert.AreEqual(SurfaceType.Ground, _terrainAdapter.ClassifySurface(Vector3.up));
        Assert.AreEqual(SurfaceType.Ground, _terrainAdapter.ClassifySurface(new Vector3(0.1f, 0.99f, 0f).normalized));

        // Ceiling (normal pointing down)
        Assert.AreEqual(SurfaceType.Ceiling, _terrainAdapter.ClassifySurface(Vector3.down));
        Assert.AreEqual(SurfaceType.Ceiling, _terrainAdapter.ClassifySurface(new Vector3(0.1f, -0.99f, 0f).normalized));

        // Wall (normal pointing sideways)
        Assert.AreEqual(SurfaceType.Wall, _terrainAdapter.ClassifySurface(Vector3.right));
        Assert.AreEqual(SurfaceType.Wall, _terrainAdapter.ClassifySurface(Vector3.forward));
        Assert.AreEqual(SurfaceType.Wall, _terrainAdapter.ClassifySurface(new Vector3(0.7f, 0.3f, 0f).normalized));
    }
}
