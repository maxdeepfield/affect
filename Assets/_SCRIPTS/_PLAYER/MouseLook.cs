
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class MouseLook : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownRange = 80f;
    [SerializeField] private Transform cameraTransform;

    public float CurrentVerticalRotation => verticalRotation;
    public float recoilPitchOffset = 0f;

    private PlayerInputHandler inputHandler;
    private float verticalRotation = 0f;

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
        float horizontalRotation = mouseDelta.x * mouseSensitivity;
        float verticalInput = mouseDelta.y * mouseSensitivity;

        transform.Rotate(0f, horizontalRotation, 0f);

        verticalRotation -= verticalInput;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
