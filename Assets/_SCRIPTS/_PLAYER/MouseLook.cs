using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class MouseLook : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownRange = 80f;
    [SerializeField] private Transform cameraTransform;

    public float CurrentVerticalRotation => verticalRotation;
    
    /// <summary>
    /// External recoil offset applied to camera pitch (vertical rotation).
    /// Set by RecoilSystem to apply accumulated recoil.
    /// </summary>
    [HideInInspector]
    public float recoilPitchOffset = 0f;
    
    /// <summary>
    /// External recoil offset for camera yaw (horizontal rotation).
    /// Set by RecoilSystem to apply accumulated horizontal recoil.
    /// </summary>
    [HideInInspector]
    public float recoilYawOffset = 0f;

    /// <summary>
    /// Combined external recoil offset as Vector2 (x = pitch/vertical, y = yaw/horizontal).
    /// Provides a convenient way for RecoilSystem to set both offsets at once.
    /// </summary>
    public Vector2 ExternalRecoilOffset
    {
        get => new Vector2(recoilPitchOffset, recoilYawOffset);
        set
        {
            recoilPitchOffset = value.x;
            recoilYawOffset = value.y;
        }
    }

    private PlayerInputHandler inputHandler;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;
    private float accumulatedRecoilYaw = 0f;

    void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
    }


    void Update()
    {
        HandleMouseLook();
    }

    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        Vector2 mouseDelta = inputHandler.MouseLookInput;
        float horizontalInput = mouseDelta.x * mouseSensitivity;
        float verticalInput = mouseDelta.y * mouseSensitivity;

        // Apply horizontal rotation to player body
        // Include accumulated recoil yaw offset (additive per frame from recoil)
        horizontalRotation += horizontalInput + recoilYawOffset;
        transform.localRotation = Quaternion.Euler(0f, horizontalRotation, 0f);
        
        // Track accumulated yaw for recovery purposes
        accumulatedRecoilYaw += recoilYawOffset;
        
        // Reset yaw offset after applying (it's additive per frame from recoil)
        recoilYawOffset = 0f;

        // Apply vertical rotation to camera
        // Include recoil pitch offset (accumulated recoil from RecoilSystem)
        // Subtract recoil offset because positive X rotation = looking down in Unity,
        // but recoil should kick the camera UP (negative X rotation)
        verticalRotation -= verticalInput;
        float totalVertical = verticalRotation - recoilPitchOffset;
        totalVertical = Mathf.Clamp(totalVertical, -upDownRange, upDownRange);
        cameraTransform.localRotation = Quaternion.Euler(totalVertical, 0f, 0f);
    }

    /// <summary>
    /// Gets the current mouse delta input for use by other systems (e.g., MouseTracker).
    /// </summary>
    public Vector2 GetCurrentMouseDelta()
    {
        if (inputHandler == null) return Vector2.zero;
        return inputHandler.MouseLookInput;
    }

    /// <summary>
    /// Gets the camera transform reference.
    /// </summary>
    public Transform CameraTransform => cameraTransform;

    /// <summary>
    /// Gets the accumulated recoil yaw that has been applied.
    /// </summary>
    public float AccumulatedRecoilYaw => accumulatedRecoilYaw;

    /// <summary>
    /// Resets the accumulated recoil tracking.
    /// </summary>
    public void ResetAccumulatedRecoil()
    {
        accumulatedRecoilYaw = 0f;
        recoilPitchOffset = 0f;
        recoilYawOffset = 0f;
    }
}
