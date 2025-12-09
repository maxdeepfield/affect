using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class LegConnectorV3 : MonoBehaviour
{
    [Header("Joint References")]
    public Transform startJoint;
    public Transform endJoint;

    [Header("Visual Settings")]
    [Range(0.01f, 0.2f)] public float radius = 0.05f;
    public Color color = Color.gray;
    
    [Header("Damage Visualization")]
    [Tooltip("Color when segment is damaged")]
    public Color damagedColor = Color.red;
    [Tooltip("Reference to LegData for damage state (optional)")]
    public global::LegData legData;
    [Tooltip("Segment index in the leg (0=hip, 1=knee, 2=foot)")]
    public int segmentIndex = 0;

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

        // Check if segment is active (for damage system)
        if (legData != null && !legData.IsSegmentActive(segmentIndex))
        {
            // Hide this visual if segment is destroyed
            if (_renderer != null) _renderer.enabled = false;
            return;
        }
        
        if (_renderer != null) _renderer.enabled = true;

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

        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        if (_renderer == null) return;
        
        if (_material == null)
        {
#if UNITY_EDITOR
            _material = _renderer.sharedMaterial;
#else
            _material = _renderer.material;
#endif
        }
        
        if (_material == null) return;

        // Determine color based on damage state
        Color targetColor = color;
        if (legData != null)
        {
            float healthPercent = legData.GetSegmentHealth(segmentIndex) / 100f;
            if (healthPercent < 1f)
            {
                targetColor = Color.Lerp(damagedColor, color, healthPercent);
            }
        }
        
        //_material.color = targetColor;
    }

    /// <summary>
    /// Sets up this connector for a specific leg segment.
    /// </summary>
    /// <param name="start">Start joint transform</param>
    /// <param name="end">End joint transform</param>
    /// <param name="data">LegData for damage tracking (optional)</param>
    /// <param name="segment">Segment index (0=hip, 1=knee, 2=foot)</param>
    public void Setup(Transform start, Transform end, global::LegData data = null, int segment = 0)
    {
        startJoint = start;
        endJoint = end;
        legData = data;
        segmentIndex = segment;
    }

    void OnDrawGizmos()
    {
        if (startJoint != null && endJoint != null)
        {
            // Show damage state in gizmos
            if (legData != null && !legData.IsSegmentActive(segmentIndex))
            {
                Gizmos.color = Color.gray;
            }
            else
            {
                Gizmos.color = Color.magenta;
            }
            Gizmos.DrawLine(startJoint.position, endJoint.position);
        }
    }
}
