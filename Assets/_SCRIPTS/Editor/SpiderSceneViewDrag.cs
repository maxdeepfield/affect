using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Allows dragging the spider in scene view to test walking without game mode.
/// </summary>
[InitializeOnLoad]
public class SpiderSceneViewDrag
{
    private static SpiderIKSystem _selectedSpider;
    private static Vector3 _dragStartPos;
    private static bool _isDragging;

    static SpiderSceneViewDrag()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        // Get the currently selected spider
        if (Selection.activeGameObject != null)
        {
            _selectedSpider = Selection.activeGameObject.GetComponent<SpiderIKSystem>();
        }
        else
        {
            _selectedSpider = null;
        }

        if (_selectedSpider == null) return;

        // Handle mouse events
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Start drag
            _dragStartPos = _selectedSpider.transform.position;
            _isDragging = true;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && _isDragging && e.button == 0)
        {
            // Calculate drag delta from mouse movement
            Vector3 mouseWorldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            
            // Project onto ground plane
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, _selectedSpider.transform.position.y);
            
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.origin + ray.direction * enter;
                
                // Move spider
                _selectedSpider.transform.position = targetPos;
                
                // Mark scene as dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            
            e.Use();
        }
        else if (e.type == EventType.MouseUp && _isDragging)
        {
            _isDragging = false;
            e.Use();
        }
    }
}
