using UnityEngine;

/// <summary>
/// Module that performs ground/wall/ceiling detection via raycasting
/// and adapts foot targets to surface normals for multi-surface locomotion.
/// </summary>
public class TerrainAdapter : MonoBehaviour, ISpiderModule
{
    [Header("Configuration")]
    [Tooltip("Layer mask for ground/surface detection")]
    [SerializeField] private LayerMask _groundLayers = -1;

    [Tooltip("Maximum raycast distance")]
    [SerializeField] private float _raycastDistance = 3f;

    [Tooltip("Raycast start offset above origin")]
    [SerializeField] private float _raycastUp = 1.5f;

    [Header("Wall Detection")]
    [Tooltip("Distance for lateral wall detection raycasts")]
    [SerializeField] private float _wallDetectionDistance = 1f;

    [Tooltip("Angle threshold for wall detection (degrees from vertical)")]
    [SerializeField] private float _wallAngleThreshold = 45f;

    [Header("State")]
    [SerializeField] private Vector3 _currentSurfaceNormal = Vector3.up;
    [SerializeField] private SurfaceType _currentSurface = SurfaceType.Ground;

    private SpiderIKSystem _system;
    private bool _isEnabled = true;

    /// <summary>
    /// Gets or sets the ground layer mask.
    /// </summary>
    public LayerMask GroundLayers
    {
        get => _groundLayers;
        set => _groundLayers = value;
    }

    /// <summary>
    /// Gets or sets the raycast distance.
    /// </summary>
    public float RaycastDistance
    {
        get => _raycastDistance;
        set => _raycastDistance = Mathf.Max(0.1f, value);
    }

    /// <summary>
    /// Gets the current surface normal.
    /// </summary>
    public Vector3 CurrentSurfaceNormal => _currentSurfaceNormal;

    /// <summary>
    /// Gets the current surface type.
    /// </summary>
    public SurfaceType CurrentSurface => _currentSurface;


    /// <summary>
    /// Gets or sets whether this module is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// Initializes the module with a reference to the parent SpiderIKSystem.
    /// </summary>
    public void Initialize(SpiderIKSystem system)
    {
        _system = system;
        if (_system != null && _system.Config != null)
        {
            _groundLayers = _system.Config.groundLayers;
            _raycastUp = _system.Config.raycastUp;
            _raycastDistance = _system.Config.raycastDown;
        }
    }

    /// <summary>
    /// Called every frame to update the module's state.
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        // Surface detection is typically called explicitly
    }

    /// <summary>
    /// Called every fixed update for physics-related processing.
    /// </summary>
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        // Update surface detection in fixed update for physics consistency
        if (_system != null)
        {
            UpdateSurfaceDetection(_system.transform.position, -_system.transform.up);
        }
    }

    /// <summary>
    /// Resets the module to its initial state.
    /// </summary>
    public void Reset()
    {
        _currentSurfaceNormal = Vector3.up;
        _currentSurface = SurfaceType.Ground;
    }

    /// <summary>
    /// Finds the surface position by raycasting from origin in the specified direction.
    /// </summary>
    /// <param name="origin">Starting position for raycast</param>
    /// <param name="direction">Direction to raycast (typically body's local down)</param>
    /// <returns>Surface hit point, or fallback position if no surface found</returns>
    public Vector3 FindSurfacePosition(Vector3 origin, Vector3 direction)
    {
        // Start raycast from above the origin
        Vector3 rayStart = origin - direction * _raycastUp;
        float totalDistance = _raycastUp + _raycastDistance;

        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, totalDistance, _groundLayers))
        {
            _currentSurfaceNormal = hit.normal;
            _currentSurface = ClassifySurface(hit.normal);
            return hit.point;
        }

        // No surface found - return fallback position
        return origin + direction * _raycastDistance * 0.5f;
    }

    /// <summary>
    /// Finds surface position and outputs the hit information.
    /// </summary>
    public bool FindSurfacePosition(Vector3 origin, Vector3 direction, out Vector3 position, out Vector3 normal)
    {
        Vector3 rayStart = origin - direction * _raycastUp;
        float totalDistance = _raycastUp + _raycastDistance;

        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, totalDistance, _groundLayers))
        {
            position = hit.point;
            normal = hit.normal;
            _currentSurfaceNormal = hit.normal;
            _currentSurface = ClassifySurface(hit.normal);
            return true;
        }

        position = origin + direction * _raycastDistance * 0.5f;
        normal = -direction;
        return false;
    }


    /// <summary>
    /// Updates surface detection for the body position.
    /// </summary>
    private void UpdateSurfaceDetection(Vector3 position, Vector3 downDirection)
    {
        FindSurfacePosition(position, downDirection, out _, out _);
    }

    /// <summary>
    /// Detects if there's a wall transition in the movement direction.
    /// </summary>
    /// <param name="position">Current position</param>
    /// <param name="moveDirection">Direction of movement</param>
    /// <returns>True if a wall is detected</returns>
    public bool DetectWallTransition(Vector3 position, Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.001f) return false;

        Vector3 lateralDir = moveDirection.normalized;
        
        // Raycast in movement direction to detect walls
        if (Physics.Raycast(position, lateralDir, out RaycastHit hit, _wallDetectionDistance, _groundLayers))
        {
            // Check if the surface is vertical enough to be a wall
            float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
            if (angleFromUp > _wallAngleThreshold && angleFromUp < (180f - _wallAngleThreshold))
            {
                _currentSurfaceNormal = hit.normal;
                _currentSurface = SurfaceType.Wall;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Classifies a surface based on its normal vector.
    /// </summary>
    /// <param name="normal">Surface normal</param>
    /// <returns>Surface type classification</returns>
    public SurfaceType ClassifySurface(Vector3 normal)
    {
        float angleFromUp = Vector3.Angle(normal, Vector3.up);

        if (angleFromUp < _wallAngleThreshold)
        {
            return SurfaceType.Ground;
        }
        else if (angleFromUp > (180f - _wallAngleThreshold))
        {
            return SurfaceType.Ceiling;
        }
        else
        {
            return SurfaceType.Wall;
        }
    }

    /// <summary>
    /// Gets the gravity direction based on current surface.
    /// </summary>
    /// <returns>Gravity direction vector</returns>
    public Vector3 GetGravityDirection()
    {
        switch (_currentSurface)
        {
            case SurfaceType.Ground:
                return Vector3.down;
            case SurfaceType.Ceiling:
                return Vector3.up; // Inverted for ceiling walking
            case SurfaceType.Wall:
                return -_currentSurfaceNormal; // Perpendicular to wall
            default:
                return Vector3.down;
        }
    }

    /// <summary>
    /// Calculates foot orientation based on surface normal.
    /// </summary>
    /// <param name="footPosition">Position of the foot</param>
    /// <param name="footForward">Forward direction of the foot</param>
    /// <returns>Rotation aligned to surface</returns>
    public Quaternion GetFootOrientation(Vector3 footPosition, Vector3 footForward)
    {
        // Align foot up with surface normal
        Vector3 up = _currentSurfaceNormal;
        Vector3 forward = Vector3.ProjectOnPlane(footForward, up).normalized;
        
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        }

        return Quaternion.LookRotation(forward, up);
    }

    /// <summary>
    /// Sets configuration values (for testing).
    /// </summary>
    public void SetConfiguration(LayerMask layers, float raycastDist, float raycastUpOffset)
    {
        _groundLayers = layers;
        _raycastDistance = raycastDist;
        _raycastUp = raycastUpOffset;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw surface normal
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, _currentSurfaceNormal);

        // Draw raycast direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, -transform.up * _raycastDistance);
    }
#endif
}
