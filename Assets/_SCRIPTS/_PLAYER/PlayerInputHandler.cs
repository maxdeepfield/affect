
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MouseLookInput { get; private set; }
    public Vector2 MovementInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool ShootInput { get; private set; }
    public bool ShootInputPressedThisFrame { get; private set; }

    private InputAction mouseLookAction;
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction shootAction;

    void Awake()
    {
        mouseLookAction = new InputAction("MouseLook", InputActionType.Value, "<Mouse>/delta");
        movementAction = new InputAction("Movement", InputActionType.Value, expectedControlType: "Vector2");
        movementAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        shootAction = new InputAction("Shoot", InputActionType.Button, "<Mouse>/leftButton");

        mouseLookAction.Enable();
        movementAction.Enable();
        jumpAction.Enable();
        shootAction.Enable();
    }

    void Update()
    {
        MouseLookInput = mouseLookAction.ReadValue<Vector2>();
        MovementInput = movementAction.ReadValue<Vector2>();
        JumpInput = jumpAction.WasPressedThisFrame();
        ShootInput = shootAction.IsPressed();
        ShootInputPressedThisFrame = shootAction.WasPressedThisFrame();
    }

    void OnDisable()
    {
        mouseLookAction?.Disable();
        movementAction?.Disable();
        jumpAction?.Disable();
        shootAction?.Disable();
    }
}
