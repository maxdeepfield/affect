using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ProceduralTerrainGenerator))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate"))
        {
            ProceduralTerrainGenerator generator = (ProceduralTerrainGenerator)target;
            generator.Generate();
            EditorUtility.SetDirty(generator);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}
