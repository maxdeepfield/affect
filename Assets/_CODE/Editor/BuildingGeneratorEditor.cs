using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate"))
        {
            BuildingGenerator generator = (BuildingGenerator)target;
            generator.GenerateBuilding();
            EditorUtility.SetDirty(generator.gameObject);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}
