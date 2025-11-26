using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class GameCameraAlign
{
    static GameCameraAlign()
    {
        SceneView.duringSceneGui += view =>
        {
            if (Event.current.keyCode == KeyCode.F12 && Event.current.type == EventType.KeyDown)
            {
                if (Camera.main != null)
                {
                    view.LookAtDirect(Camera.main.transform.position, Camera.main.transform.rotation, 0.01f);
                    view.orthographic = Camera.main.orthographic;
                    view.size = Camera.main.orthographicSize;
                    view.fieldOfView = Camera.main.fieldOfView;
                    view.Repaint();
                }
            }
        };
    }
}