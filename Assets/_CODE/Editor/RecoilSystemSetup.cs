using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Editor utility to set up the RecoilSystem on player prefabs.
/// Use via menu: Tools > Recoil System > Setup Player Recoil
/// </summary>
public class RecoilSystemSetup : EditorWindow
{
    private GameObject targetPlayer;
    
    [MenuItem("Tools/Recoil System/Setup Player Recoil")]
    public static void ShowWindow()
    {
        GetWindow<RecoilSystemSetup>("Recoil System Setup");
    }

    [MenuItem("Tools/Recoil System/Setup _PLAYER Prefab")]
    public static void SetupPlayerPrefab()
    {
        // Find the _PLAYER prefab in the Assets folder
        string[] guids = AssetDatabase.FindAssets("_PLAYER t:Prefab");
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Prefab Not Found", 
                "Could not find _PLAYER prefab in the project.", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Load Failed", 
                "Could not load _PLAYER prefab.", "OK");
            return;
        }

        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            SetupRecoilSystemOnPrefab(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[RecoilSystemSetup] Successfully set up RecoilSystem on _PLAYER prefab at: {prefabPath}");
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "RecoilSystem has been set up on _PLAYER prefab.\n\n" +
                "Components added:\n" +
                "- RecoilSystem (on player root)\n" +
                "- RecoilModules child object with:\n" +
                "  - RecoilRandomizer\n" +
                "  - MouseTracker\n" +
                "  - CameraShaker\n\n" +
                "Default configuration values have been applied.", 
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Recoil System Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool will add the RecoilSystem and its modules to a player GameObject.\n\n" +
            "Components added:\n" +
            "- RecoilSystem (main orchestrator)\n" +
            "- RecoilRandomizer (procedural variation)\n" +
            "- MouseTracker (compensation detection)\n" +
            "- CameraShaker (cinematic shake)",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        targetPlayer = (GameObject)EditorGUILayout.ObjectField(
            "Target Player", 
            targetPlayer, 
            typeof(GameObject), 
            true);
        
        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(targetPlayer == null);
        if (GUILayout.Button("Setup Recoil System", GUILayout.Height(30)))
        {
            SetupRecoilSystem(targetPlayer);
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Selected GameObject"))
        {
            if (Selection.activeGameObject != null)
            {
                SetupRecoilSystem(Selection.activeGameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", 
                    "Please select a GameObject in the hierarchy.", "OK");
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "Quick Setup: Use 'Tools > Recoil System > Setup _PLAYER Prefab' to automatically configure the _PLAYER prefab.",
            MessageType.Info);
        
        if (GUILayout.Button("Setup _PLAYER Prefab", GUILayout.Height(25)))
        {
            SetupPlayerPrefab();
        }
    }


    /// <summary>
    /// Sets up the RecoilSystem on the target GameObject (for scene objects with undo support).
    /// </summary>
    public static void SetupRecoilSystem(GameObject target)
    {
        if (target == null)
        {
            Debug.LogError("[RecoilSystemSetup] Target GameObject is null.");
            return;
        }

        Undo.SetCurrentGroupName("Setup Recoil System");
        int undoGroup = Undo.GetCurrentGroup();

        // Add RecoilSystem component if not present
        RecoilSystem recoilSystem = target.GetComponent<RecoilSystem>();
        if (recoilSystem == null)
        {
            recoilSystem = Undo.AddComponent<RecoilSystem>(target);
            Debug.Log("[RecoilSystemSetup] Added RecoilSystem component.");
        }

        // Create a child GameObject for recoil modules
        Transform modulesParent = target.transform.Find("RecoilModules");
        GameObject modulesObj;
        if (modulesParent == null)
        {
            modulesObj = new GameObject("RecoilModules");
            Undo.RegisterCreatedObjectUndo(modulesObj, "Create RecoilModules");
            modulesObj.transform.SetParent(target.transform);
            modulesObj.transform.localPosition = Vector3.zero;
            modulesObj.transform.localRotation = Quaternion.identity;
            Debug.Log("[RecoilSystemSetup] Created RecoilModules child object.");
        }
        else
        {
            modulesObj = modulesParent.gameObject;
        }

        // Add RecoilRandomizer module
        RecoilRandomizer randomizer = modulesObj.GetComponent<RecoilRandomizer>();
        if (randomizer == null)
        {
            randomizer = Undo.AddComponent<RecoilRandomizer>(modulesObj);
            Debug.Log("[RecoilSystemSetup] Added RecoilRandomizer module.");
        }

        // Add MouseTracker module
        MouseTracker mouseTracker = modulesObj.GetComponent<MouseTracker>();
        if (mouseTracker == null)
        {
            mouseTracker = Undo.AddComponent<MouseTracker>(modulesObj);
            Debug.Log("[RecoilSystemSetup] Added MouseTracker module.");
        }

        // Add CameraShaker module
        CameraShaker cameraShaker = modulesObj.GetComponent<CameraShaker>();
        if (cameraShaker == null)
        {
            cameraShaker = Undo.AddComponent<CameraShaker>(modulesObj);
            Debug.Log("[RecoilSystemSetup] Added CameraShaker module.");
        }

        // Find and wire camera transform
        Camera cam = target.GetComponentInChildren<Camera>();
        Transform cameraTransform = cam != null ? cam.transform : null;
        if (cameraTransform != null)
        {
            SetPrivateField(recoilSystem, "_cameraTransform", cameraTransform);
            Debug.Log($"[RecoilSystemSetup] Wired camera transform: {cam.name}");
        }
        else
        {
            Debug.LogWarning("[RecoilSystemSetup] No camera found in children. Please assign manually.");
        }

        // Find weapon transform (common naming conventions)
        Transform weaponTransform = FindWeaponTransform(target.transform);
        if (weaponTransform != null)
        {
            SetPrivateField(recoilSystem, "_weaponTransform", weaponTransform);
            Debug.Log($"[RecoilSystemSetup] Wired weapon transform: {weaponTransform.name}");
        }
        else
        {
            Debug.LogWarning("[RecoilSystemSetup] No weapon transform found. Please assign manually.");
        }

        // Find and wire MouseLook component
        MouseLook mouseLook = target.GetComponent<MouseLook>();
        if (mouseLook != null)
        {
            SetPrivateField(recoilSystem, "_mouseLook", mouseLook);
            Debug.Log("[RecoilSystemSetup] Wired MouseLook component.");
        }

        // Apply default configuration
        ApplyDefaultConfiguration(recoilSystem);

        // Wire WeaponController to RecoilSystem
        WeaponController weaponController = target.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            Undo.RecordObject(weaponController, "Wire RecoilSystem");
            weaponController.RecoilSystem = recoilSystem;
            Debug.Log("[RecoilSystemSetup] Wired WeaponController to RecoilSystem.");
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Mark prefab as dirty if editing a prefab
        if (PrefabUtility.IsPartOfPrefabAsset(target))
        {
            EditorUtility.SetDirty(target);
        }

        EditorUtility.DisplayDialog("Setup Complete", 
            "RecoilSystem has been set up on " + target.name + ".\n\n" +
            "Please verify the following in the Inspector:\n" +
            "- Camera Transform reference\n" +
            "- Weapon Transform reference\n" +
            "- RecoilConfiguration values", 
            "OK");
    }

    /// <summary>
    /// Sets up the RecoilSystem on a prefab (without undo support, for prefab editing).
    /// </summary>
    public static void SetupRecoilSystemOnPrefab(GameObject target)
    {
        if (target == null)
        {
            Debug.LogError("[RecoilSystemSetup] Target GameObject is null.");
            return;
        }

        // Add RecoilSystem component if not present
        RecoilSystem recoilSystem = target.GetComponent<RecoilSystem>();
        if (recoilSystem == null)
        {
            recoilSystem = target.AddComponent<RecoilSystem>();
            Debug.Log("[RecoilSystemSetup] Added RecoilSystem component.");
        }

        // Create a child GameObject for recoil modules
        Transform modulesParent = target.transform.Find("RecoilModules");
        GameObject modulesObj;
        if (modulesParent == null)
        {
            modulesObj = new GameObject("RecoilModules");
            modulesObj.transform.SetParent(target.transform);
            modulesObj.transform.localPosition = Vector3.zero;
            modulesObj.transform.localRotation = Quaternion.identity;
            modulesObj.transform.localScale = Vector3.one;
            Debug.Log("[RecoilSystemSetup] Created RecoilModules child object.");
        }
        else
        {
            modulesObj = modulesParent.gameObject;
        }

        // Add RecoilRandomizer module
        RecoilRandomizer randomizer = modulesObj.GetComponent<RecoilRandomizer>();
        if (randomizer == null)
        {
            randomizer = modulesObj.AddComponent<RecoilRandomizer>();
            Debug.Log("[RecoilSystemSetup] Added RecoilRandomizer module.");
        }

        // Add MouseTracker module
        MouseTracker mouseTracker = modulesObj.GetComponent<MouseTracker>();
        if (mouseTracker == null)
        {
            mouseTracker = modulesObj.AddComponent<MouseTracker>();
            Debug.Log("[RecoilSystemSetup] Added MouseTracker module.");
        }

        // Add CameraShaker module
        CameraShaker cameraShaker = modulesObj.GetComponent<CameraShaker>();
        if (cameraShaker == null)
        {
            cameraShaker = modulesObj.AddComponent<CameraShaker>();
            Debug.Log("[RecoilSystemSetup] Added CameraShaker module.");
        }

        // Find and wire camera transform
        Camera cam = target.GetComponentInChildren<Camera>();
        Transform cameraTransform = cam != null ? cam.transform : null;
        if (cameraTransform != null)
        {
            SetPrivateField(recoilSystem, "_cameraTransform", cameraTransform);
            Debug.Log($"[RecoilSystemSetup] Wired camera transform: {cam.name}");
        }
        else
        {
            Debug.LogWarning("[RecoilSystemSetup] No camera found in children. Camera transform must be assigned manually.");
        }

        // Find weapon transform (common naming conventions)
        Transform weaponTransform = FindWeaponTransform(target.transform);
        if (weaponTransform != null)
        {
            SetPrivateField(recoilSystem, "_weaponTransform", weaponTransform);
            Debug.Log($"[RecoilSystemSetup] Wired weapon transform: {weaponTransform.name}");
        }
        else
        {
            Debug.LogWarning("[RecoilSystemSetup] No weapon transform found. Weapon transform must be assigned manually.");
        }

        // Find and wire MouseLook component
        MouseLook mouseLook = target.GetComponent<MouseLook>();
        if (mouseLook != null)
        {
            SetPrivateField(recoilSystem, "_mouseLook", mouseLook);
            Debug.Log("[RecoilSystemSetup] Wired MouseLook component.");
        }

        // Apply default configuration
        ApplyDefaultConfiguration(recoilSystem);

        // Wire WeaponController to RecoilSystem
        WeaponController weaponController = target.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.RecoilSystem = recoilSystem;
            Debug.Log("[RecoilSystemSetup] Wired WeaponController to RecoilSystem.");
        }

        EditorUtility.SetDirty(target);
    }

    /// <summary>
    /// Applies default RecoilConfiguration values to the RecoilSystem.
    /// </summary>
    private static void ApplyDefaultConfiguration(RecoilSystem recoilSystem)
    {
        // Create default configuration with sensible values
        RecoilConfiguration config = new RecoilConfiguration
        {
            // Vertical Recoil
            baseVerticalKick = 2f,
            maxAccumulatedVertical = 15f,
            
            // Horizontal Recoil
            baseHorizontalKick = 0.5f,
            horizontalSpread = 2f,
            
            // Weapon Transform
            weaponKickbackDistance = 0.05f,
            weaponRotationKick = 3f,
            
            // Recovery
            recoverySpeed = 8f,
            recoveryCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            
            // Randomizer
            verticalVariationMin = 0.8f,
            verticalVariationMax = 1.2f,
            noiseScale = 0.5f,
            
            // Mouse Tracking
            compensationMultiplier = 1.5f,
            maxCompensationRate = 2f,
            
            // Camera Shake
            shakeIntensity = 0.02f,
            shakeFrequency = 25f,
            pathFollowStrength = 0.5f
        };

        SetPrivateField(recoilSystem, "_config", config);
        Debug.Log("[RecoilSystemSetup] Applied default RecoilConfiguration values.");
    }

    /// <summary>
    /// Sets a private serialized field using reflection.
    /// </summary>
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        
        System.Type type = target.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[RecoilSystemSetup] Could not find field '{fieldName}' on {type.Name}");
        }
    }

    /// <summary>
    /// Attempts to find the weapon transform using common naming conventions.
    /// </summary>
    private static Transform FindWeaponTransform(Transform root)
    {
        string[] weaponNames = { "Weapon", "weapon", "Gun", "gun", "Rifle", "rifle", "Pistol", "pistol" };
        
        foreach (string name in weaponNames)
        {
            Transform found = FindChildRecursive(root, name);
            if (found != null) return found;
        }
        
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    [MenuItem("Tools/Recoil System/Create Default Configuration")]
    public static void CreateDefaultConfiguration()
    {
        RecoilConfiguration config = new RecoilConfiguration();
        string json = config.ToJson();
        
        string path = EditorUtility.SaveFilePanel(
            "Save Recoil Configuration",
            "Assets/_SCRIPTS/_PLAYER/Recoil/Presets",
            "DefaultRecoilConfig",
            "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"[RecoilSystemSetup] Created default configuration at: {path}");
        }
    }
}
