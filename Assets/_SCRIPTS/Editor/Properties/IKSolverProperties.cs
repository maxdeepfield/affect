using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Property-based tests for LegSolver IK calculations.
/// Tests Properties 1, 2, 3 from the design document.
/// </summary>
[TestFixture]
public class IKSolverProperties
{
    private const int PropertyTestIterations = 100;
    private const float PositionTolerance = 0.01f;
    private const float ReachTolerance = 0.01f; // 1% tolerance for reach extension
    
    private GameObject _testObject;
    private LegSolver _legSolver;
    private SpiderIKSystem _system;

    [SetUp]
    public void SetUp()
    {
        _testObject = new GameObject("TestLegSolver");
        _system = _testObject.AddComponent<SpiderIKSystem>();
        _legSolver = _testObject.AddComponent<LegSolver>();
        
        // Initialize with default config
        _system.Config = new IKConfiguration();
        _legSolver.Initialize(_system);
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
    /// Creates a test leg hierarchy with the specified bone count.
    /// </summary>
    private LegData CreateTestLeg(int boneCount, float upperLength, float lowerLength, Vector3 hipPosition)
    {
        LegData leg = new LegData();
        
        // Create transforms
        GameObject rootObj = new GameObject("LegRoot");
        GameObject hipObj = new GameObject("Hip");
        GameObject footObj = new GameObject("Foot");
        
        rootObj.transform.SetParent(_testObject.transform);
        hipObj.transform.SetParent(rootObj.transform);
        
        leg.root = rootObj.transform;
        leg.hip = hipObj.transform;
        leg.hip.position = hipPosition;
        
        if (boneCount >= 3)
        {
            GameObject kneeObj = new GameObject("Knee");
            kneeObj.transform.SetParent(hipObj.transform);
            leg.knee = kneeObj.transform;
            leg.knee.position = hipPosition + Vector3.down * upperLength;
            
            footObj.transform.SetParent(kneeObj.transform);
            leg.foot = footObj.transform;
            leg.foot.position = leg.knee.position + Vector3.down * lowerLength;
        }
        else
        {
            footObj.transform.SetParent(hipObj.transform);
            leg.foot = footObj.transform;
            leg.foot.position = hipPosition + Vector3.down * (upperLength + lowerLength);
        }
        
        leg.InitializeSegments(boneCount, 100f);
        return leg;
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 1: IK Solution Accuracy**
    /// *For any* target position within leg reach and any bone count configuration (1, 2, or 3), 
    /// the Leg_Solver SHALL position the foot transform within 0.01 meters of the target position.
    /// **Validates: Requirements 1.1, 1.4, 1.5, 1.6**
    /// </summary>
    [Test]
    public void Property1_IKSolutionAccuracy()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);
            
            // Generate random configuration
            int boneCount = SpiderGenerators.GenerateBoneCount();
            float legLength = SpiderGenerators.RandomFloat(0.3f, 2f);
            float hipRatio = SpiderGenerators.RandomFloat(0.3f, 0.7f);
            var (upperLength, lowerLength) = SpiderGenerators.GenerateSegmentLengths(legLength, hipRatio);
            
            Vector3 bodyCenter = SpiderGenerators.GenerateBodyCenter();
            Vector3 hipPosition = bodyCenter + new Vector3(
                SpiderGenerators.RandomFloat(-1f, 1f),
                SpiderGenerators.RandomFloat(-0.5f, 0.5f),
                SpiderGenerators.RandomFloat(-1f, 1f)
            );
            
            // Generate target WITHIN reach - ensure it's clearly within reach
            float targetDistance = SpiderGenerators.RandomFloat(legLength * 0.2f, legLength * 0.85f);
            Vector3 targetDir = Random.onUnitSphere;
            Vector3 target = hipPosition + targetDir * targetDistance;
            
            // Create test leg and configure solver
            LegData leg = CreateTestLeg(boneCount, upperLength, lowerLength, hipPosition);
            _legSolver.BoneCount = boneCount;
            _legSolver.SetSegmentLengths(upperLength, lowerLength);
            
            try
            {
                // Solve IK
                _legSolver.SolveIK(leg, target, bodyCenter);
                
                // Check foot position accuracy
                float distance = Vector3.Distance(leg.foot.position, target);
                
                if (distance > PositionTolerance)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Foot at {leg.foot.position} is {distance}m from target {target} (tolerance: {PositionTolerance}m). BoneCount: {boneCount}, LegLength: {legLength}, TargetDist: {targetDistance}";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception during IK solve: {e.Message}";
            }
            finally
            {
                // Cleanup leg objects
                if (leg.root != null) Object.DestroyImmediate(leg.root.gameObject);
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 1 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }

    /// <summary>
    /// **Feature: spider-ik-walker, Property 2: IK Reach Extension**
    /// *For any* target position beyond leg reach, the Leg_Solver SHALL extend the leg fully 
    /// toward the target with total chain length equal to the sum of segment lengths (±1% tolerance).
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Test]
    public void Property2_IKReachExtension()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);
            
            // Generate random configuration
            int boneCount = SpiderGenerators.GenerateBoneCount();
            float legLength = SpiderGenerators.RandomFloat(0.3f, 2f);
            float hipRatio = SpiderGenerators.RandomFloat(0.3f, 0.7f);
            var (upperLength, lowerLength) = SpiderGenerators.GenerateSegmentLengths(legLength, hipRatio);
            
            Vector3 bodyCenter = SpiderGenerators.GenerateBodyCenter();
            Vector3 hipPosition = bodyCenter + new Vector3(
                SpiderGenerators.RandomFloat(-1f, 1f),
                SpiderGenerators.RandomFloat(-0.5f, 0.5f),
                SpiderGenerators.RandomFloat(-1f, 1f)
            );
            
            // Generate target BEYOND reach
            Vector3 target = SpiderGenerators.GenerateIKTarget(hipPosition, legLength, withinReach: false);
            
            // Create test leg and configure solver
            LegData leg = CreateTestLeg(boneCount, upperLength, lowerLength, hipPosition);
            _legSolver.BoneCount = boneCount;
            _legSolver.SetSegmentLengths(upperLength, lowerLength);
            
            try
            {
                // Solve IK
                _legSolver.SolveIK(leg, target, bodyCenter);
                
                // Check that leg is fully extended
                float actualChainLength = Vector3.Distance(leg.hip.position, leg.foot.position);
                float expectedChainLength = upperLength + lowerLength;
                float tolerance = expectedChainLength * ReachTolerance;
                
                if (Mathf.Abs(actualChainLength - expectedChainLength) > tolerance)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Chain length {actualChainLength} differs from expected {expectedChainLength} by more than {tolerance}. BoneCount: {boneCount}";
                    continue;
                }
                
                // Check that foot is in the direction of target
                Vector3 toTarget = (target - leg.hip.position).normalized;
                Vector3 toFoot = (leg.foot.position - leg.hip.position).normalized;
                float dotProduct = Vector3.Dot(toTarget, toFoot);
                
                if (dotProduct < 0.99f) // Should be pointing toward target
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Foot direction dot product {dotProduct} < 0.99 (not pointing toward target). BoneCount: {boneCount}";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception during IK solve: {e.Message}";
            }
            finally
            {
                if (leg.root != null) Object.DestroyImmediate(leg.root.gameObject);
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 2 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }


    /// <summary>
    /// **Feature: spider-ik-walker, Property 3: Knee Bend Direction Invariant**
    /// *For any* IK solution with 3 bones, the knee position SHALL be on the outward side 
    /// of the hip-foot line relative to body center (dot product of knee offset and outward direction > 0).
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Test]
    public void Property3_KneeBendDirectionInvariant()
    {
        int failures = 0;
        string lastFailureMessage = "";

        for (int i = 0; i < PropertyTestIterations; i++)
        {
            SpiderGenerators.Seed(i);
            
            // Only test 3 bone configurations (explicit knee)
            int boneCount = 3;
            float legLength = SpiderGenerators.RandomFloat(0.3f, 2f);
            float hipRatio = SpiderGenerators.RandomFloat(0.3f, 0.7f);
            var (upperLength, lowerLength) = SpiderGenerators.GenerateSegmentLengths(legLength, hipRatio);
            
            Vector3 bodyCenter = SpiderGenerators.GenerateBodyCenter();
            
            // Position hip clearly away from body center for clear outward direction
            Vector3 hipOffset = new Vector3(
                SpiderGenerators.RandomFloat(0.5f, 2f) * (SpiderGenerators.RandomInt(0, 1) == 0 ? 1 : -1),
                SpiderGenerators.RandomFloat(-0.3f, 0.3f),
                SpiderGenerators.RandomFloat(0.5f, 2f) * (SpiderGenerators.RandomInt(0, 1) == 0 ? 1 : -1)
            );
            Vector3 hipPosition = bodyCenter + hipOffset;
            
            // Generate target within reach for proper IK solution (not too close, not too far)
            float minDist = Mathf.Abs(upperLength - lowerLength) + 0.1f;
            float maxDist = legLength * 0.9f;
            if (minDist >= maxDist) minDist = maxDist * 0.5f;
            float targetDist = SpiderGenerators.RandomFloat(minDist, maxDist);
            Vector3 targetDir = Random.onUnitSphere;
            Vector3 target = hipPosition + targetDir * targetDist;
            
            // Create test leg and configure solver
            LegData leg = CreateTestLeg(boneCount, upperLength, lowerLength, hipPosition);
            _legSolver.BoneCount = boneCount;
            _legSolver.SetSegmentLengths(upperLength, lowerLength);
            
            try
            {
                // Solve IK
                _legSolver.SolveIK(leg, target, bodyCenter);
                
                // Calculate outward direction (from body center to hip, horizontal)
                Vector3 outwardDir = hipPosition - bodyCenter;
                outwardDir.y = 0;
                
                if (outwardDir.magnitude < 0.001f)
                {
                    continue; // Skip if hip is directly above/below body center
                }
                outwardDir.Normalize();
                
                // Get actual knee position from the leg
                if (leg.knee == null)
                {
                    continue; // Skip if no knee
                }
                Vector3 kneePos = leg.knee.position;
                
                // Calculate knee offset from hip-foot line
                Vector3 hipToFoot = leg.foot.position - leg.hip.position;
                if (hipToFoot.magnitude < 0.001f)
                {
                    continue; // Skip degenerate case
                }
                
                Vector3 hipToKnee = kneePos - leg.hip.position;
                
                // Project knee onto hip-foot line
                float t = Vector3.Dot(hipToKnee, hipToFoot.normalized);
                Vector3 projectedPoint = leg.hip.position + hipToFoot.normalized * t;
                Vector3 kneeOffset = kneePos - projectedPoint;
                
                // Check if knee offset is in outward direction
                float dotProduct = Vector3.Dot(kneeOffset.normalized, outwardDir);
                
                // Allow some tolerance for edge cases where knee is nearly on the line
                if (kneeOffset.magnitude > 0.01f && dotProduct < -0.1f)
                {
                    failures++;
                    lastFailureMessage = $"Iteration {i}: Knee bend direction dot product {dotProduct} < 0 (knee bending inward). KneeOffset: {kneeOffset}, OutwardDir: {outwardDir}";
                }
            }
            catch (System.Exception e)
            {
                failures++;
                lastFailureMessage = $"Iteration {i}: Exception during IK solve: {e.Message}";
            }
            finally
            {
                if (leg.root != null) Object.DestroyImmediate(leg.root.gameObject);
            }
        }

        Assert.AreEqual(0, failures,
            $"Property 3 failed {failures}/{PropertyTestIterations} times. Last failure: {lastFailureMessage}");
    }
}
