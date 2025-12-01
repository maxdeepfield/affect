using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Custom editor for SpiderIKSystem with preset buttons, leg hierarchy creation,
/// and physics shell setup tools.
/// </summary>
[CustomEditor(typeof(SpiderIKSystem))]
public class SpiderIKSystemEditor : Editor
{
    private SpiderIKSystem _system;
    private bool _showPresets = true;
    private bool _showHierarchyTools = true;
    private bool _showPhysicsTools = true;

    private static readonly string PresetsPath = "Assets/_SCRIPTS/_SPIDER/Presets";

    private void OnEnable()
    {
        _system = (SpiderIKSystem)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Preset Section
        _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true, EditorStyles.foldoutHeader);
        if (_showPresets)
        {
            DrawPresetSection();
        }

        EditorGUILayout.Space(5);

        // Hierarchy Tools Section
        _showHierarchyTools = EditorGUILayout.Foldout(_showHierarchyTools, "Leg Hierarchy Tools", true, EditorStyles.foldoutHeader);
        if (_showHierarchyTools)
        {
            DrawHierarchyToolsSection();
        }

        EditorGUILayout.Space(5);

        // Physics Tools Section
        _showPhysicsTools = EditorGUILayout.Foldout(_showPhysicsTools, "Physics Setup", true, EditorStyles.foldoutHeader);
        if (_showPhysicsTools)
        {
            DrawPhysicsToolsSection();
        }
    }

    #region Preset Section

    private void DrawPresetSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spider Walker"))
        {
            ApplyPreset("SpiderWalker");
        }
        if (GUILayout.Button("Animal Crouch"))
        {
            ApplyPreset("AnimalCrouch");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Hopper"))
        {
            ApplyPreset("Hopper");
        }
        if (GUILayout.Button("Octopod"))
        {
            ApplyPreset("Octopod");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Current as Preset"))
        {
            SaveCurrentAsPreset();
        }
        if (GUILayout.Button("Load from File"))
        {
            LoadPresetFromFile();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void ApplyPreset(string presetName)
    {
        string path = Path.Combine(PresetsPath, $"{presetName}.json");
        
        if (!File.Exists(path))
        {
            // Try to create default preset if it doesn't exist
            CreateDefaultPreset(presetName);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Preset Not Found", 
                    $"Preset '{presetName}' not found at {path}", "OK");
                return;
            }
        }

        string json = File.ReadAllText(path);
        IKConfiguration config = IKConfiguration.FromJson(json);

        Undo.RecordObject(_system, $"Apply {presetName} Preset");
        _system.Config = config;
        
        // Recreate leg hierarchy if leg count changed
        Transform legsContainer = _system.transform.Find("Legs");
        if (legsContainer != null && legsContainer.childCount != config.legCount)
        {
            // Remove old legs
            foreach (Transform child in legsContainer)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
            
            // Recreate with new count
            CreateLegHierarchyForConfig(config, legsContainer);
        }
        
        _system.RebuildLegData();
        EditorUtility.SetDirty(_system);

        Debug.Log($"[SpiderIKSystemEditor] Applied preset: {presetName}");
    }

    private void CreateLegHierarchyForConfig(IKConfiguration config, Transform legsContainer)
    {
        int legCount = config.legCount;
        int boneCount = config.boneCount;
        float legLength = config.legLength;
        float hipRatio = config.hipRatio;
        float bodyRadius = config.bodyRadius;
        float legSpread = config.legSpread;

        string[] legNames = { "FL", "FR", "BL", "BR", "ML", "MR", "FFL", "FFR" };
        
        for (int i = 0; i < legCount; i++)
        {
            string legName = i < legNames.Length ? $"Leg_{legNames[i]}" : $"Leg_{i}";
            
            // Calculate leg position in circle
            float angle = (360f / legCount) * i * Mathf.Deg2Rad;
            Vector3 legPos = new Vector3(
                Mathf.Sin(angle) * bodyRadius * legSpread,
                0f,
                Mathf.Cos(angle) * bodyRadius * legSpread
            );

            // Create leg root
            GameObject legRoot = new GameObject(legName);
            legRoot.transform.SetParent(legsContainer);
            legRoot.transform.localPosition = legPos;

            // Create bones
            Transform parent = legRoot.transform;
            float segmentLength = legLength / boneCount;

            // Hip
            GameObject hip = new GameObject("Hip");
            hip.transform.SetParent(parent);
            hip.transform.localPosition = Vector3.zero;
            parent = hip.transform;

            if (boneCount >= 2)
            {
                // Knee
                GameObject knee = new GameObject("Knee");
                knee.transform.SetParent(parent);
                knee.transform.localPosition = new Vector3(segmentLength * 0.5f, -segmentLength * 0.5f, 0f);
                parent = knee.transform;
            }

            if (boneCount >= 3)
            {
                // Foot
                GameObject foot = new GameObject("Foot");
                foot.transform.SetParent(parent);
                foot.transform.localPosition = new Vector3(0f, -segmentLength, 0f);
            }
            else if (boneCount == 2)
            {
                // Foot for 2-bone
                GameObject foot = new GameObject("Foot");
                foot.transform.SetParent(parent);
                foot.transform.localPosition = new Vector3(0f, -segmentLength, 0f);
            }
        }
    }

    private void SaveCurrentAsPreset()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Spider Preset",
            PresetsPath,
            "CustomSpider",
            "json");

        if (string.IsNullOrEmpty(path)) return;

        EnsurePresetsDirectoryExists();

        string json = _system.Config.ToJson();
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        Debug.Log($"[SpiderIKSystemEditor] Saved preset to: {path}");
    }

    private void LoadPresetFromFile()
    {
        string path = EditorUtility.OpenFilePanel(
            "Load Spider Preset",
            PresetsPath,
            "json");

        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        IKConfiguration config = IKConfiguration.FromJson(json);

        Undo.RecordObject(_system, "Load Preset");
        _system.Config = config;
        _system.RebuildLegData();
        EditorUtility.SetDirty(_system);

        Debug.Log($"[SpiderIKSystemEditor] Loaded preset from: {path}");
    }

    #endregion

    #region Hierarchy Tools Section

    private void DrawHierarchyToolsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Leg Hierarchy Creation", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Leg Hierarchy"))
        {
            CreateLegHierarchy();
        }

        if (GUILayout.Button("Auto-Assign Leg Transforms"))
        {
            AutoAssignLegTransforms();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Rebuild Leg Data"))
        {
            Undo.RecordObject(_system, "Rebuild Leg Data");
            _system.RebuildLegData();
            EditorUtility.SetDirty(_system);
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateLegHierarchy()
    {
        if (_system.Config == null)
        {
            EditorUtility.DisplayDialog("No Configuration", 
                "Please set a configuration first.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(_system.gameObject, "Create Leg Hierarchy");

        int legCount = _system.Config.legCount;
        int boneCount = _system.Config.boneCount;
        float bodyRadius = _system.Config.bodyRadius;
        float legLength = _system.Config.legLength;
        float legSpread = _system.Config.legSpread;

        // Create legs container
        Transform legsContainer = _system.transform.Find("Legs");
        if (legsContainer == null)
        {
            GameObject container = new GameObject("Legs");
            container.transform.SetParent(_system.transform);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            legsContainer = container.transform;
        }

        // Create each leg
        string[] legNames = { "FL", "FR", "BL", "BR", "ML", "MR", "FFL", "FFR" };
        
        for (int i = 0; i < legCount; i++)
        {
            string legName = i < legNames.Length ? $"Leg_{legNames[i]}" : $"Leg_{i}";
            
            // Calculate leg position
            float angle = (360f / legCount) * i - 45f; // Start at front-left
            float rad = angle * Mathf.Deg2Rad;
            Vector3 legPos = new Vector3(
                Mathf.Sin(rad) * bodyRadius * legSpread,
                0f,
                Mathf.Cos(rad) * bodyRadius * legSpread
            );

            // Create leg root
            GameObject legRoot = new GameObject(legName);
            legRoot.transform.SetParent(legsContainer);
            legRoot.transform.localPosition = legPos;
            legRoot.transform.localRotation = Quaternion.LookRotation(legPos.normalized, Vector3.up);

            // Create bones based on bone count
            Transform parent = legRoot.transform;
            float segmentLength = legLength / boneCount;

            // Hip
            GameObject hip = new GameObject("Hip");
            hip.transform.SetParent(parent);
            hip.transform.localPosition = Vector3.zero;
            parent = hip.transform;

            if (boneCount >= 2)
            {
                // Knee (for 2+ bones)
                GameObject knee = new GameObject("Knee");
                knee.transform.SetParent(parent);
                knee.transform.localPosition = new Vector3(segmentLength * 0.5f, -segmentLength * 0.5f, 0f);
                parent = knee.transform;
            }

            // Foot
            GameObject foot = new GameObject("Foot");
            foot.transform.SetParent(parent);
            foot.transform.localPosition = new Vector3(0f, -segmentLength, 0f);
        }

        _system.RebuildLegData();
        EditorUtility.SetDirty(_system);

        Debug.Log($"[SpiderIKSystemEditor] Created {legCount} legs with {boneCount} bones each");
    }

    private void AutoAssignLegTransforms()
    {
        Transform legsContainer = _system.transform.Find("Legs");
        if (legsContainer == null)
        {
            EditorUtility.DisplayDialog("No Legs Container", 
                "Create leg hierarchy first or add a 'Legs' child object.", "OK");
            return;
        }

        Undo.RecordObject(_system, "Auto-Assign Leg Transforms");
        _system.RebuildLegData();
        EditorUtility.SetDirty(_system);

        Debug.Log("[SpiderIKSystemEditor] Auto-assigned leg transforms");
    }

    #endregion

    #region Physics Tools Section

    private void DrawPhysicsToolsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Physics Shell Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup Physics Shell"))
        {
            SetupPhysicsShell();
        }

        if (GUILayout.Button("Add Required Components"))
        {
            AddRequiredComponents();
        }

        EditorGUILayout.EndVertical();
    }

    private void SetupPhysicsShell()
    {
        if (_system.Config == null)
        {
            EditorUtility.DisplayDialog("No Configuration", 
                "Please set a configuration first.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(_system.gameObject, "Setup Physics Shell");

        // Add or configure Rigidbody
        Rigidbody rb = _system.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = _system.gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 10f;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Add or configure CapsuleCollider
        CapsuleCollider col = _system.GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = _system.gameObject.AddComponent<CapsuleCollider>();
        }
        col.radius = _system.Config.bodyRadius;
        col.height = _system.Config.bodyHeight;
        col.center = Vector3.up * (_system.Config.bodyHeight * 0.5f);

        EditorUtility.SetDirty(_system);

        Debug.Log("[SpiderIKSystemEditor] Physics shell configured");
    }

    private void AddRequiredComponents()
    {
        Undo.RegisterFullObjectHierarchyUndo(_system.gameObject, "Add Required Components");

        // Add all ISpiderModule components if not present
        if (_system.GetComponent<LegSolver>() == null)
            _system.gameObject.AddComponent<LegSolver>();

        if (_system.GetComponent<GaitController>() == null)
            _system.gameObject.AddComponent<GaitController>();

        if (_system.GetComponent<TerrainAdapter>() == null)
            _system.gameObject.AddComponent<TerrainAdapter>();

        if (_system.GetComponent<BodyStabilizer>() == null)
            _system.gameObject.AddComponent<BodyStabilizer>();

        if (_system.GetComponent<StepAnimator>() == null)
            _system.gameObject.AddComponent<StepAnimator>();

        if (_system.GetComponent<HitReactor>() == null)
            _system.gameObject.AddComponent<HitReactor>();

        if (_system.GetComponent<LegDamageHandler>() == null)
            _system.gameObject.AddComponent<LegDamageHandler>();

        EditorUtility.SetDirty(_system);

        Debug.Log("[SpiderIKSystemEditor] Added all required components");
    }

    #endregion

    #region Preset Creation

    private void EnsurePresetsDirectoryExists()
    {
        if (!Directory.Exists(PresetsPath))
        {
            Directory.CreateDirectory(PresetsPath);
            AssetDatabase.Refresh();
        }
    }

    private void CreateDefaultPreset(string presetName)
    {
        EnsurePresetsDirectoryExists();

        IKConfiguration config = GetDefaultPresetConfig(presetName);
        if (config == null) return;

        string path = Path.Combine(PresetsPath, $"{presetName}.json");
        string json = config.ToJson();
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        Debug.Log($"[SpiderIKSystemEditor] Created default preset: {presetName}");
    }

    private IKConfiguration GetDefaultPresetConfig(string presetName)
    {
        switch (presetName)
        {
            case "SpiderWalker":
                return new IKConfiguration
                {
                    legCount = 4,
                    boneCount = 3,
                    bodyRadius = 0.3f,
                    bodyHeight = 0.4f,
                    legLength = 0.8f,
                    stepThreshold = 0.4f,
                    stepHeight = 0.15f,
                    stepSpeed = 6f
                };

            case "AnimalCrouch":
                return new IKConfiguration
                {
                    legCount = 4,
                    boneCount = 2,
                    bodyRadius = 0.4f,
                    bodyHeight = 0.3f,
                    legLength = 0.5f,
                    stepThreshold = 0.3f,
                    stepHeight = 0.08f,
                    stepSpeed = 8f
                };

            case "Hopper":
                return new IKConfiguration
                {
                    legCount = 2,
                    boneCount = 2,
                    bodyRadius = 0.25f,
                    bodyHeight = 0.5f,
                    legLength = 0.6f,
                    stepThreshold = 0.5f,
                    stepHeight = 0.3f,
                    stepSpeed = 4f
                };

            case "Octopod":
                return new IKConfiguration
                {
                    legCount = 8,
                    boneCount = 3,
                    bodyRadius = 0.5f,
                    bodyHeight = 0.3f,
                    legLength = 1.0f,
                    stepThreshold = 0.5f,
                    stepHeight = 0.12f,
                    stepSpeed = 5f
                };

            default:
                return null;
        }
    }

    #endregion

    #region Menu Items

    [MenuItem("GameObject/Spider IK/Create Spider", false, 10)]
    private static void CreateSpider(MenuCommand menuCommand)
    {
        GameObject spider = new GameObject("Spider");
        
        // Add SpiderIKSystem
        SpiderIKSystem system = spider.AddComponent<SpiderIKSystem>();
        
        // Set default config
        var config = new IKConfiguration
        {
            legCount = 4,
            boneCount = 3,
            bodyRadius = 0.3f,
            bodyHeight = 0.4f,
            legLength = 0.8f
        };
        system.Config = config;
        
        // Add all modules
        spider.AddComponent<LegSolver>();
        spider.AddComponent<GaitController>();
        spider.AddComponent<TerrainAdapter>();
        spider.AddComponent<BodyStabilizer>();
        spider.AddComponent<StepAnimator>();
        spider.AddComponent<HitReactor>();
        spider.AddComponent<LegDamageHandler>();

        // Add physics
        Rigidbody rb = spider.AddComponent<Rigidbody>();
        rb.mass = 10f;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;

        CapsuleCollider col = spider.AddComponent<CapsuleCollider>();
        col.radius = config.bodyRadius;
        col.height = config.bodyHeight;
        col.center = Vector3.up * (config.bodyHeight * 0.5f);

        // Create visual body
        GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bodyVisual.name = "Body";
        bodyVisual.transform.SetParent(spider.transform);
        bodyVisual.transform.localPosition = Vector3.zero;
        bodyVisual.transform.localScale = new Vector3(config.bodyRadius * 2f, config.bodyHeight * 0.7f, config.bodyRadius * 2.5f);
        DestroyImmediate(bodyVisual.GetComponent<Collider>());
        
        // Create legs container
        GameObject legsContainer = new GameObject("Legs");
        legsContainer.transform.SetParent(spider.transform);
        legsContainer.transform.localPosition = Vector3.zero;

        // Create 4 legs
        string[] legNames = { "Leg_FL", "Leg_FR", "Leg_BL", "Leg_BR" };
        float[] angles = { 45f, 135f, 225f, 315f };
        
        for (int i = 0; i < 4; i++)
        {
            float angle = angles[i] * Mathf.Deg2Rad;
            Vector3 legPos = new Vector3(
                Mathf.Cos(angle) * config.bodyRadius * 1.5f,
                0f,
                Mathf.Sin(angle) * config.bodyRadius * 1.5f
            );

            GameObject legRoot = new GameObject(legNames[i]);
            legRoot.transform.SetParent(legsContainer.transform);
            legRoot.transform.localPosition = legPos;

            // Hip
            GameObject hip = new GameObject("Hip");
            hip.transform.SetParent(legRoot.transform);
            hip.transform.localPosition = Vector3.zero;
            AddLegVisual(hip.transform, 0.05f);

            // Knee
            GameObject knee = new GameObject("Knee");
            knee.transform.SetParent(hip.transform);
            float upperLen = config.legLength * config.hipRatio;
            knee.transform.localPosition = new Vector3(0f, -upperLen * 0.5f, upperLen * 0.3f);
            AddLegVisual(knee.transform, 0.04f);

            // Foot
            GameObject foot = new GameObject("Foot");
            foot.transform.SetParent(knee.transform);
            float lowerLen = config.legLength - upperLen;
            foot.transform.localPosition = new Vector3(0f, -lowerLen, 0f);
            AddLegVisual(foot.transform, 0.03f);

            // Add leg connectors for visuals
            AddLegConnector(hip.transform, knee.transform, 0.04f);
            AddLegConnector(knee.transform, foot.transform, 0.03f);
        }

        // Rebuild leg data
        system.RebuildLegData();

        // Parent to context
        GameObjectUtility.SetParentAndAlign(spider, menuCommand.context as GameObject);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(spider, "Create Spider");
        
        Selection.activeObject = spider;
    }

    private static void AddLegVisual(Transform parent, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Joint";
        sphere.transform.SetParent(parent);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * radius * 2f;
        DestroyImmediate(sphere.GetComponent<Collider>());
    }

    private static void AddLegConnector(Transform start, Transform end, float radius)
    {
        GameObject connector = new GameObject("Connector");
        connector.transform.SetParent(start);
        connector.transform.localPosition = Vector3.zero;

        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "Visual";
        cyl.transform.SetParent(connector.transform);
        cyl.transform.localPosition = Vector3.zero;
        DestroyImmediate(cyl.GetComponent<Collider>());

        var legConnector = cyl.AddComponent<LegConnectorV3>();
        legConnector.startJoint = start;
        legConnector.endJoint = end;
        legConnector.radius = radius;
    }

    #endregion
}
