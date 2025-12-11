
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MouseLookInput { get; private set; }
    public Vector2 MovementInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool ShootInput { get; private set; }
    public bool ShootInputPressedThisFrame { get; private set; }
    public bool AimInput { get; private set; }
    public bool AimInputPressedThisFrame { get; private set; }
    public bool UseInput { get; private set; }
    public bool UseInputPressedThisFrame { get; private set; }
    public bool ReloadInput { get; private set; }
    public bool ReloadInputPressedThisFrame { get; private set; }
    public bool RunInput { get; private set; }
    public bool RunInputPressedThisFrame { get; private set; }
    public bool CrouchInput { get; private set; }
    public bool CrouchInputPressedThisFrame { get; private set; }

    [Header("Use Settings")]
    [SerializeField] private float useCheckDistance = 3f;
    [SerializeField] private Camera useCamera;

    private InputAction mouseLookAction;
    private InputAction movementAction;
    private InputAction jumpAction;
    private InputAction shootAction;
    private InputAction aimAction;
    private InputAction useAction;
    private InputAction reloadAction;
    private InputAction runAction;
    private InputAction crouchAction;

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
        aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        useAction = new InputAction("Use", InputActionType.Button, "<Keyboard>/f");
        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        runAction = new InputAction("Run", InputActionType.Button);
        runAction.AddBinding("<Keyboard>/leftShift");
        runAction.AddBinding("<Keyboard>/rightShift");
        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/c");
        crouchAction.AddBinding("<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/rightCtrl");

        mouseLookAction.Enable();
        movementAction.Enable();
        jumpAction.Enable();
        shootAction.Enable();
        aimAction.Enable();
        useAction.Enable();
        reloadAction.Enable();
        runAction.Enable();
        crouchAction.Enable();

        if (useCamera == null)
        {
            useCamera = Camera.main;
        }
    }

    void Update()
    {
        MouseLookInput = mouseLookAction.ReadValue<Vector2>();
        MovementInput = movementAction.ReadValue<Vector2>();
        JumpInput = jumpAction.WasPressedThisFrame();
        ShootInput = shootAction.IsPressed();
        ShootInputPressedThisFrame = shootAction.WasPressedThisFrame();
        AimInput = aimAction.IsPressed();
        AimInputPressedThisFrame = aimAction.WasPressedThisFrame();
        UseInput = useAction.IsPressed();
        UseInputPressedThisFrame = useAction.WasPressedThisFrame();
        ReloadInput = reloadAction.IsPressed();
        ReloadInputPressedThisFrame = reloadAction.WasPressedThisFrame();
        RunInput = runAction.IsPressed();
        RunInputPressedThisFrame = runAction.WasPressedThisFrame();
        CrouchInput = crouchAction.IsPressed();
        CrouchInputPressedThisFrame = crouchAction.WasPressedThisFrame();

        Camera cam = useCamera != null ? useCamera : Camera.main;
        if (cam != null)
        {
            Usable.CheckRaycast(cam, useCheckDistance);
        }

        if (UseInputPressedThisFrame)
        {
            Usable.TryUseCurrent();
        }
    }

    void OnDisable()
    {
        mouseLookAction?.Disable();
        movementAction?.Disable();
        jumpAction?.Disable();
        shootAction?.Disable();
        aimAction?.Disable();
        useAction?.Disable();
        reloadAction?.Disable();
        runAction?.Disable();
        crouchAction?.Disable();
    }
}
