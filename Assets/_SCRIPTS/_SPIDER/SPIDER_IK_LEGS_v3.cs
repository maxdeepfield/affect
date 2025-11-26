using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spider IK System v3.0 - COMPLETE REWRITE for proper spider locomotion
/// </summary>
[ExecuteAlways]
public class SPIDER_IK_LEGS_v3 : MonoBehaviour
{
    [Header("=== VERSION ===")]
    public string version = "v3.0 - COMPLETE SPIDER REWRITE";
    
    [Header("=== LEG SETUP ===")]
    public Transform[] legRoots;
    
    [Header("=== SPIDER PROPORTIONS ===")]
    [Range(0.1f, 1f)] public float bodyHeight = 0.3f;
    [Range(0.2f, 1f)] public float legLength = 0.6f;
    [Range(0.2f, 0.8f)] public float hipRatio = 0.5f; // Portion of total leg length assigned to hip -> knee
    [Range(0.5f, 2f)] public float legSpread = 0.8f;
    [Tooltip("Horizontal distance from body center to leg roots. Set to 0 to keep current offsets.")]
    [Range(0f, 2f)] public float hipOriginDistance = 0f;

    [Header("=== WALKING ===")]
    [Range(0.1f, 1f)] public float stepThreshold = 0.4f;
    [Range(0.05f, 0.3f)] public float stepHeight = 0.1f;
    [Range(1f, 10f)] public float stepSpeed = 5f;
    [Range(0f, 1.5f)] public float strideForward = 0.3f;
    [Range(0f, 1.5f)] public float strideVelocityScale = 0.4f;
    
    [Header("=== GROUND ===")]
    public LayerMask groundLayers = -1;
    
    internal LegData[] legs;
    private Vector3 lastBodyPos;
    private Vector3 bodyVelocity;
    private int activeStepGroup = -1;
    
    [System.Serializable]
    public class LegData
    {
        public Transform root, hip, knee, foot;
        public Vector3 restTarget, currentTarget, plantedPos;
        public bool isStepping;
        public float stepProgress;
        public float lastStepTime;
        public int legIndex;
        public int diagonalGroup;
    }
    
    void Start()
    {
        RebuildLegData();
    }

    void OnValidate()
    {
        hipRatio = Mathf.Clamp(hipRatio, 0.05f, 0.95f);
        hipOriginDistance = Mathf.Max(hipOriginDistance, 0f);
        RebuildLegData();
    }
    
    void Update()
    {
        float dt = Time.deltaTime;
        if (dt > 0.001f)
        {
            Vector3 velocity = (transform.position - lastBodyPos) / dt;
            bodyVelocity = Vector3.Lerp(bodyVelocity, velocity, dt * 5f);
        }
        lastBodyPos = transform.position;
    }
    
    public void RebuildLegData()
    {
        ApplyLegDimensions();
        InitLegs();
        lastBodyPos = transform.position;
    }
    
    void LateUpdate()
    {
        if (legs == null) return;
        
        if (activeStepGroup != -1 && !IsGroupStepping(activeStepGroup))
        {
            activeStepGroup = -1;
        }
        
        for (int i = 0; i < legs.Length; i++)
        {
            UpdateLeg(legs[i]);
        }
    }

    void ApplyLegDimensions()
    {
        if (legRoots == null || legRoots.Length == 0) return;

        float upperLen = legLength * hipRatio;
        float lowerLen = Mathf.Max(legLength - upperLen, 0.01f);
        float forwardOffset = legLength * 0.25f;
        float hipDistance = Mathf.Max(hipOriginDistance, 0f);

        for (int i = 0; i < legRoots.Length; i++)
        {
            Transform root = legRoots[i];
            if (root == null) continue;

            if (hipDistance > 0.0001f)
            {
                Vector3 local = root.localPosition;
                Vector2 horiz = new Vector2(local.x, local.z);
                if (horiz.sqrMagnitude > 0.0001f)
                {
                    Vector2 dir = horiz.normalized;
                    root.localPosition = new Vector3(dir.x * hipDistance, local.y, dir.y * hipDistance);
                }
            }

            Transform hip = FindChild(root, "Hip");
            Transform knee = hip != null ? FindChild(hip, "Knee") : null;
            Transform foot = knee != null ? FindChild(knee, "Foot") : null;

            if (knee != null)
            {
                knee.localPosition = new Vector3(0f, -upperLen, forwardOffset);
            }
            if (foot != null)
            {
                foot.localPosition = new Vector3(0f, -upperLen - lowerLen, 0f);
            }
        }
    }
    
    void InitLegs()
    {
        if (legRoots == null || legRoots.Length == 0) return;
        
        legs = new LegData[legRoots.Length];
        for (int i = 0; i < legRoots.Length; i++)
        {
            var leg = new LegData();
            leg.root = legRoots[i];
            leg.legIndex = i;
            leg.diagonalGroup = DetermineDiagonalGroup(leg.root);
            
            if (leg.root != null)
            {
                leg.hip = FindChild(leg.root, "Hip");
                if (leg.hip != null)
                {
                    leg.knee = FindChild(leg.hip, "Knee");
                    if (leg.knee != null)
                    {
                        leg.foot = FindChild(leg.knee, "Foot");
                    }
                }
                
                if (leg.foot != null)
                {
                    Vector3 groundPos = FindGround(leg.foot.position);
                    leg.restTarget = transform.InverseTransformPoint(groundPos);
                    leg.currentTarget = groundPos;
                    leg.plantedPos = groundPos;
                    leg.lastStepTime = -999f;
                }
            }
            
            legs[i] = leg;
        }
    }
    
    void UpdateLeg(LegData leg)
    {
        if (leg.hip == null || leg.knee == null || leg.foot == null) return;
        
        Vector3 desiredPos = transform.TransformPoint(leg.restTarget);
        
        // Push the target forward in the direction of motion (or facing) to avoid planted trailing feet
        Vector3 moveDir = bodyVelocity.sqrMagnitude > 0.0001f ? bodyVelocity.normalized : transform.forward;
        float strideMag = strideForward + bodyVelocity.magnitude * strideVelocityScale;
        desiredPos += moveDir * strideMag;
        
        desiredPos = FindGround(desiredPos);
        
        float dist = Vector3.Distance(leg.currentTarget, desiredPos);
        float speed = bodyVelocity.magnitude;
        
        bool groupBlocked = activeStepGroup != -1 && activeStepGroup != leg.diagonalGroup;
        bool shouldStep = !leg.isStepping && 
                         dist > stepThreshold && 
                         speed > 0.05f &&
                         (Time.time - leg.lastStepTime) > 0.2f &&
                         !groupBlocked;
        
        if (shouldStep)
        {
            leg.isStepping = true;
            leg.plantedPos = leg.currentTarget;
            leg.stepProgress = 0f;
            leg.lastStepTime = Time.time;
            
            if (activeStepGroup == -1)
            {
                activeStepGroup = leg.diagonalGroup;
            }
        }
        
        if (leg.isStepping)
        {
            leg.stepProgress += Time.deltaTime * stepSpeed;
            leg.stepProgress = Mathf.Clamp01(leg.stepProgress);
            
            Vector3 pos = Vector3.Lerp(leg.plantedPos, desiredPos, leg.stepProgress);
            pos += transform.up * Mathf.Sin(leg.stepProgress * Mathf.PI) * stepHeight;
            leg.currentTarget = pos;
            
            if (leg.stepProgress >= 1f)
            {
                leg.isStepping = false;
                leg.currentTarget = desiredPos;
                leg.plantedPos = desiredPos;
            }
        }
        else if (speed < 0.01f)
        {
            leg.currentTarget = leg.plantedPos;
        }
        
        SolveIK(leg);
    }
    
    void SolveIK(LegData leg)
    {
        Vector3 target = leg.currentTarget;
        Vector3 hipPos = leg.hip.position;
        
        float upperLen = Vector3.Distance(leg.hip.position, leg.knee.position);
        float lowerLen = Vector3.Distance(leg.knee.position, leg.foot.position);
        
        Vector3 toTarget = target - hipPos;
        float dist = toTarget.magnitude;
        dist = Mathf.Clamp(dist, 0.01f, (upperLen + lowerLen) * 0.99f);
        
        Vector3 dir = toTarget.normalized;
        
        // Calculate knee angle using law of cosines
        float cosAngle = (upperLen * upperLen + dist * dist - lowerLen * lowerLen) / (2f * upperLen * dist);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        float angle = Mathf.Acos(cosAngle);
        
        // Bend direction (outward from body)
        Vector3 legSide = Vector3.Cross(transform.up, dir).normalized;
        float sideSign = Mathf.Sign(transform.InverseTransformPoint(leg.root.position).x);
        Vector3 bendDir = legSide * sideSign;
        
        // Calculate knee position
        Vector3 kneePos = hipPos + dir * (upperLen * Mathf.Cos(angle)) + bendDir * (upperLen * Mathf.Sin(angle));
        
        leg.knee.position = kneePos;
        leg.foot.position = target;
        
        // Update rotations
        leg.hip.rotation = Quaternion.LookRotation(kneePos - hipPos, transform.up);
        leg.knee.rotation = Quaternion.LookRotation(target - kneePos, transform.up);
    }
    
    Vector3 FindGround(Vector3 pos)
    {
        Vector3 start = pos + Vector3.up * 2f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 4f, groundLayers))
        {
            return hit.point;
        }
        return pos;
    }
    
    Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    void OnDrawGizmos()
    {
        if (legs == null) return;
        
        foreach (var leg in legs)
        {
            if (leg.hip == null || leg.knee == null || leg.foot == null) continue;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leg.hip.position, leg.knee.position);
            Gizmos.DrawLine(leg.knee.position, leg.foot.position);
            
            Gizmos.color = leg.isStepping ? Color.red : Color.green;
            Gizmos.DrawSphere(leg.currentTarget, 0.03f);
        }
    }
    
    int DetermineDiagonalGroup(Transform root)
    {
        if (root == null) return 0;
        Vector3 local = transform.InverseTransformPoint(root.position);
        float product = local.x * local.z;
        return product >= 0f ? 0 : 1; // Group legs into diagonal pairs: (FL + BR) vs (FR + BL)
    }
    
    bool IsGroupStepping(int group)
    {
        if (legs == null) return false;
        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i];
            if (leg != null && leg.isStepping && leg.diagonalGroup == group)
            {
                return true;
            }
        }
        return false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SPIDER_IK_LEGS_v3))]
public class SpiderIKv3Editor : Editor
{
    struct SpiderPreset
    {
        public readonly string label;
        public readonly float bodyHeight;
        public readonly float legLength;
        public readonly float hipRatio;
        public readonly float legSpread;
        public readonly float stepThreshold;
        public readonly float stepHeight;
        public readonly float stepSpeed;
        public readonly float strideForward;
        public readonly float strideVelocityScale;

        public SpiderPreset(string label, float bodyHeight, float legLength, float hipRatio, float legSpread, float stepThreshold, float stepHeight, float stepSpeed, float strideForward, float strideVelocityScale)
        {
            this.label = label;
            this.bodyHeight = bodyHeight;
            this.legLength = legLength;
            this.hipRatio = hipRatio;
            this.legSpread = legSpread;
            this.stepThreshold = stepThreshold;
            this.stepHeight = stepHeight;
            this.stepSpeed = stepSpeed;
            this.strideForward = strideForward;
            this.strideVelocityScale = strideVelocityScale;
        }
    }

    static readonly SpiderPreset AnimalCrouchPreset = new SpiderPreset(
        "Animal Crouch",
        bodyHeight: 0.1f,
        legLength: 0.536f,
        hipRatio: 0.8f,
        legSpread: 0.5f,
        stepThreshold: 0.294f,
        stepHeight: 0.3f,
        stepSpeed: 8.29f,
        strideForward: 0.4f,
        strideVelocityScale: 0.6f
    );

    static readonly SpiderPreset SpiderWalkerPreset = new SpiderPreset(
        "Spider Walker",
        bodyHeight: 0.28f,
        legLength: 0.7f,
        hipRatio: 0.55f,
        legSpread: 0.9f,
        stepThreshold: 0.42f,
        stepHeight: 0.15f,
        stepSpeed: 6.5f,
        strideForward: 0.32f,
        strideVelocityScale: 0.45f
    );

    static readonly SpiderPreset WeirdWheeledPreset = new SpiderPreset(
        "Weird Wheeled Combo",
        bodyHeight: 0.2f,
        legLength: 0.5f,
        hipRatio: 0.6f,
        legSpread: 1.0f,
        stepThreshold: 0.35f,
        stepHeight: 0.12f,
        stepSpeed: 7.5f,
        strideForward: 0.45f,
        strideVelocityScale: 0.55f
    );

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        var spider = (SPIDER_IK_LEGS_v3)target;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Create", EditorStyles.boldLabel);
        if (GUILayout.Button("Create SPIDER (Walker)", GUILayout.Height(26)))
        {
            CreateSpider(spider, SpiderWalkerPreset, addWheels: false, clearExisting: true);
        }
        if (GUILayout.Button("Create ANIMAL (Crouch)", GUILayout.Height(26)))
        {
            CreateSpider(spider, AnimalCrouchPreset, addWheels: false, clearExisting: true);
        }
        if (GUILayout.Button("Create WEIRD WHEELED COMBO", GUILayout.Height(26)))
        {
            CreateSpider(spider, WeirdWheeledPreset, addWheels: true, clearExisting: true);
        }
        if (GUILayout.Button("Reset / Clear Children", GUILayout.Height(22)))
        {
            TotalReset(spider);
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Add/Refresh Physics Shell", GUILayout.Height(22)))
        {
            AddPhysicsShell(spider);
        }
        if (GUILayout.Button("Rebuild / Apply Current Settings", GUILayout.Height(22)))
        {
            spider.RebuildLegData();
            EditorUtility.SetDirty(spider);
        }

        DrawHitReaction(spider);
        
        EditorGUILayout.Space(10);
    }

    void CreateSpider(SPIDER_IK_LEGS_v3 spider, SpiderPreset preset, bool addWheels, bool clearExisting)
    {
        if (clearExisting)
        {
            TotalReset(spider);
        }

        ApplyPreset(spider, preset, rebuild: false);
        
        // Create body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.parent = spider.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.6f, 0.4f, 0.8f);
        DestroyImmediate(body.GetComponent<Collider>());
        
        // Create 4 legs
        float halfSpread = spider.legSpread * 0.5f;
        float hipDistance = spider.hipOriginDistance > 0.0001f ? spider.hipOriginDistance : halfSpread;
        spider.hipOriginDistance = hipDistance;
        Vector3[] positions = new Vector3[]
        {
            new Vector3(hipDistance, 0, hipDistance),    // FL
            new Vector3(-hipDistance, 0, hipDistance),   // FR
            new Vector3(hipDistance, 0, -hipDistance),   // BL
            new Vector3(-hipDistance, 0, -hipDistance)   // BR
        };
        
        string[] names = new string[] { "Leg_FL", "Leg_FR", "Leg_BL", "Leg_BR" };
        
        Transform[] roots = new Transform[4];
        float upperLen = spider.legLength * spider.hipRatio;
        float lowerLen = Mathf.Max(spider.legLength - upperLen, 0.01f);
        float forwardOffset = spider.legLength * 0.25f;
        
        for (int i = 0; i < 4; i++)
        {
            GameObject legRoot = new GameObject(names[i]);
            legRoot.transform.parent = spider.transform;
            legRoot.transform.localPosition = positions[i];
            roots[i] = legRoot.transform;
            
            // Hip
            GameObject hip = new GameObject("Hip");
            hip.transform.parent = legRoot.transform;
            hip.transform.localPosition = Vector3.zero;
            
            // Knee - angled down and out
            GameObject knee = new GameObject("Knee");
            knee.transform.parent = hip.transform;
            knee.transform.localPosition = new Vector3(0, -upperLen, forwardOffset);
            
            // Foot
            GameObject foot = new GameObject("Foot");
            foot.transform.parent = knee.transform;
            foot.transform.localPosition = new Vector3(0, -upperLen - lowerLen, 0);

            if (addWheels)
            {
                AddWheel(foot.transform, spider);
            }
            
            // Visual
            AddVisual(hip.transform, knee.transform, 0.04f);
            AddVisual(knee.transform, foot.transform, 0.03f);
        }
        
        spider.legRoots = roots;
        
        // Position on ground
        Vector3 start = spider.transform.position + Vector3.up * 2f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 5f, spider.groundLayers))
        {
            spider.transform.position = hit.point + Vector3.up * spider.bodyHeight;
        }
        else
        {
            spider.transform.position += Vector3.up * spider.bodyHeight;
        }
        
        spider.RebuildLegData();
        AddPhysicsShell(spider);

        EditorUtility.SetDirty(spider);
    }

    void TotalReset(SPIDER_IK_LEGS_v3 spider)
    {
        // Remove children
        while (spider.transform.childCount > 0)
        {
            DestroyImmediate(spider.transform.GetChild(0).gameObject);
        }

        // Reset leg roots reference
        spider.legRoots = null;

        // Remove visuals/physics components added by creator
        var rb = spider.GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);

        var collider = spider.GetComponent<Collider>();
        if (collider != null) DestroyImmediate(collider);

        var hit = spider.GetComponent<SpiderHitReaction>();
        if (hit != null) DestroyImmediate(hit);

        var stabilizer = spider.GetComponent<SpiderBodyStabilizer>();
        if (stabilizer != null) DestroyImmediate(stabilizer);

        spider.legs = null;
        EditorUtility.SetDirty(spider);
    }

    void AddVisual(Transform start, Transform end, float radius)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "Visual";
        cyl.transform.parent = start;
        DestroyImmediate(cyl.GetComponent<Collider>());
        
        var connector = cyl.AddComponent<LegConnectorV3>();
        connector.startJoint = start;
        connector.endJoint = end;
        connector.radius = radius;
    }

    void AddWheel(Transform parent, SPIDER_IK_LEGS_v3 spider)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "Wheel";
        wheel.transform.parent = parent;
        wheel.transform.localPosition = Vector3.zero;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // lay sideways
        float radius = Mathf.Max(0.12f, spider.legSpread * 0.15f);
        wheel.transform.localScale = new Vector3(radius, radius * 0.6f, radius);
        // keep collider for interaction
    }

    void AddPhysicsShell(SPIDER_IK_LEGS_v3 spider)
    {
        var rb = spider.GetComponent<Rigidbody>();
        if (rb == null) rb = spider.gameObject.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var capsule = spider.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = spider.gameObject.AddComponent<CapsuleCollider>();
        float radius = Mathf.Max(0.15f, spider.legSpread * 0.3f);
        float height = Mathf.Max(spider.bodyHeight * 2f, radius * 2f + 0.1f);
        capsule.direction = 1; // Y axis
        capsule.radius = radius;
        capsule.height = height;
        capsule.center = new Vector3(0f, spider.bodyHeight * 0.5f + radius * 0.5f, 0f);

        var hit = spider.GetComponent<SpiderHitReaction>();
        if (hit == null) hit = spider.gameObject.AddComponent<SpiderHitReaction>();
        hit.spider = spider;

        var stabilizer = spider.GetComponent<SpiderBodyStabilizer>();
        if (stabilizer == null) stabilizer = spider.gameObject.AddComponent<SpiderBodyStabilizer>();
        stabilizer.spider = spider;

        EditorUtility.SetDirty(spider);
        EditorUtility.SetDirty(rb);
        EditorUtility.SetDirty(capsule);
        EditorUtility.SetDirty(hit);
        EditorUtility.SetDirty(stabilizer);
    }

    void DrawHitReaction(SPIDER_IK_LEGS_v3 spider)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Hit Reaction", EditorStyles.boldLabel);
        var hit = spider.GetComponent<SpiderHitReaction>();
        if (hit == null)
        {
            EditorGUILayout.HelpBox("Physics shell is required to tweak hit reaction. Click Add/Refresh Physics Shell.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        float hitImpulse = EditorGUILayout.Slider("Hit Impulse", hit.hitImpulse, 0f, 40f);
        float scuttleForce = EditorGUILayout.Slider("Scuttle Force", hit.scuttleForce, 0f, 80f);
        float scuttleTime = EditorGUILayout.Slider("Scuttle Time", hit.scuttleTime, 0f, 2f);
        float maxHoriz = EditorGUILayout.Slider("Max Horizontal Speed", hit.maxHorizontalSpeed, 0f, 15f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(hit, "Edit Spider Hit Reaction");
            hit.hitImpulse = hitImpulse;
            hit.scuttleForce = scuttleForce;
            hit.scuttleTime = scuttleTime;
            hit.maxHorizontalSpeed = maxHoriz;
            EditorUtility.SetDirty(hit);
        }
    }

    void ApplyPreset(SPIDER_IK_LEGS_v3 spider, SpiderPreset preset, bool rebuild = true)
    {
        spider.bodyHeight = preset.bodyHeight;
        spider.legLength = preset.legLength;
        spider.hipRatio = preset.hipRatio;
        spider.legSpread = preset.legSpread;
        spider.hipOriginDistance = spider.legSpread * 0.5f;
        spider.stepThreshold = preset.stepThreshold;
        spider.stepHeight = preset.stepHeight;
        spider.stepSpeed = preset.stepSpeed;
        spider.strideForward = preset.strideForward;
        spider.strideVelocityScale = preset.strideVelocityScale;
        if (rebuild)
        {
            spider.RebuildLegData();
        }
        EditorUtility.SetDirty(spider);
    }
}
#endif

/// <summary>
/// Self-contained leg connector for v3 that stretches/rotates a cylinder between two joints.
/// </summary>
[ExecuteAlways]
public class LegConnectorV3 : MonoBehaviour
{
    [Header("Joint References")]
    public Transform startJoint;
    public Transform endJoint;

    [Header("Visual Settings")]
    [Range(0.01f, 0.2f)] public float radius = 0.05f;
    public Color color = Color.gray;

    private Renderer _renderer;
    private Material _material;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
#if UNITY_EDITOR
            _material = _renderer.sharedMaterial;
#else
            _material = _renderer.material;
#endif
        }
    }

    void LateUpdate()
    {
        if (startJoint == null || endJoint == null) return;

        Vector3 startPos = startJoint.position;
        Vector3 endPos = endJoint.position;
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;
        if (distance < 0.0001f) return;

        // Position and orient cylinder (Unity cylinders point up the Y axis)
        transform.position = (startPos + endPos) * 0.5f;
        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        transform.rotation = Quaternion.LookRotation(direction.normalized, up) * Quaternion.Euler(90f, 0f, 0f);
        transform.localScale = new Vector3(radius * 2f, distance * 0.5f, radius * 2f);

        if (_renderer != null)
        {
            if (_material == null)
            {
#if UNITY_EDITOR
                _material = _renderer.sharedMaterial;
#else
                _material = _renderer.material;
#endif
            }
            if (_material != null)
            {
                _material.color = color;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (startJoint != null && endJoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(startJoint.position, endJoint.position);
        }
    }
}

/// <summary>
/// Simple physics reaction: applies impulse and short scuttle when hit so the spider body moves away and keeps walking.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpiderHitReaction : MonoBehaviour
{
    public SPIDER_IK_LEGS_v3 spider;
    public float hitImpulse = 6f;
    public float scuttleForce = 30f;
    public float scuttleTime = 0.6f;
    public float maxHorizontalSpeed = 6f;
    
    private Rigidbody _rb;
    private Vector3 _scuttleDir;
    private float _scuttleTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (spider == null)
            spider = GetComponent<SPIDER_IK_LEGS_v3>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_rb == null) return;
        Vector3 away = ComputeAwayDirection(collision);
        _rb.AddForce(away * hitImpulse, ForceMode.Impulse);
        _scuttleDir = away;
        _scuttleTimer = scuttleTime;
    }

    void FixedUpdate()
    {
        if (_rb == null) return;

        if (_scuttleTimer > 0f)
        {
            _rb.AddForce(_scuttleDir * scuttleForce, ForceMode.Acceleration);
            _scuttleTimer -= Time.fixedDeltaTime;
        }

        LimitHorizontalSpeed();
    }

    Vector3 ComputeAwayDirection(Collision collision)
    {
        Vector3 away = Vector3.zero;
        foreach (var c in collision.contacts)
        {
            away += (transform.position - c.point);
        }

        if (away == Vector3.zero)
        {
            away = -collision.relativeVelocity;
        }

        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            away = spider != null ? -spider.transform.forward : transform.forward * -1f;
        }
        return away.normalized;
    }

    void LimitHorizontalSpeed()
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 horiz = new Vector3(v.x, 0f, v.z);
        float mag = horiz.magnitude;
        if (mag > maxHorizontalSpeed)
        {
            horiz = horiz.normalized * maxHorizontalSpeed;
            _rb.linearVelocity = new Vector3(horiz.x, v.y, horiz.z);
        }
    }
}

/// <summary>
/// Keeps the spider body upright and hovering at body height above ground while allowing physics impulses.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpiderBodyStabilizer : MonoBehaviour
{
    public SPIDER_IK_LEGS_v3 spider;
    [Header("Upright")]
    public float uprightStrength = 20f;
    public float uprightDamping = 6f;
    [Header("Height")]
    public float heightStrength = 30f;
    public float heightDamping = 6f;
    public float bodyHeightOffset = 0f;
    public float raycastUp = 1.5f;
    public float raycastDown = 3f;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (spider == null)
            spider = GetComponent<SPIDER_IK_LEGS_v3>();
        EnsureConstraints();
    }

    void OnEnable() => EnsureConstraints();
    void OnValidate() => EnsureConstraints();

    void EnsureConstraints()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void FixedUpdate()
    {
        if (_rb == null || spider == null) return;
        StabilizeUp();
        StabilizeHeight();
    }

    void StabilizeUp()
    {
        Vector3 currentUp = transform.up;
        Vector3 targetUp = Vector3.up;
        Vector3 torque = Vector3.Cross(currentUp, targetUp) * uprightStrength - _rb.angularVelocity * uprightDamping;
        _rb.AddTorque(torque, ForceMode.Acceleration);
    }

    void StabilizeHeight()
    {
        Vector3 origin = transform.position + Vector3.up * raycastUp;
        float targetY = transform.position.y;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastUp + raycastDown, spider.groundLayers))
        {
            targetY = hit.point.y + spider.bodyHeight + bodyHeightOffset;
        }

        float error = targetY - _rb.position.y;
        float lift = error * heightStrength - _rb.linearVelocity.y * heightDamping;
        _rb.AddForce(Vector3.up * lift, ForceMode.Acceleration);
    }
}
