
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchCenterY = 0.6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Audio")]
    [SerializeField] private FootstepSounds footstepSounds;

    private CharacterController characterController;
    private PlayerInputHandler inputHandler;
    private float verticalVelocity = 0f;
    private bool isGrounded = false;
    private bool isCrouching = false;
    private float standHeight;
    private Vector3 standCenter;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        standHeight = characterController.height;
        standCenter = characterController.center;
        
        // Find FootstepSounds if not assigned
        if (footstepSounds == null)
        {
            footstepSounds = GetComponent<FootstepSounds>();
        }
    }

    void Update()
    {
        HandleCrouch();
        HandleMovement();
        HandleJumping();
    }

    private void HandleMovement()
    {
        Vector2 movementInput = inputHandler.MovementInput;
        float forwardMovement = movementInput.y;
        float strafeMovement = movementInput.x;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 movement = (forward * forwardMovement + right * strafeMovement).normalized;

        float speed = moveSpeed;
        if (isCrouching)
        {
            speed = crouchSpeed;
        }
        else if (inputHandler.RunInput)
        {
            speed = runSpeed;
        }

        movement *= speed;

        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = gravity * Time.deltaTime;
        }

        movement.y = verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
    }

    private void HandleJumping()
    {
        if (isGrounded && inputHandler.JumpInput)
        {
            verticalVelocity = jumpForce;
            
            // Play jump sound
            if (footstepSounds != null)
            {
                footstepSounds.PlayJumpSound();
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }

    private void UpdateGroundedStatus()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 0.1f))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void FixedUpdate()
    {
        UpdateGroundedStatus();
    }

    private void HandleCrouch()
    {
        if (characterController == null) return;

        bool wantsCrouch = inputHandler.CrouchInput;

        if (wantsCrouch)
        {
            ApplyCrouch(true);
        }
        else if (isCrouching && CanStandUp())
        {
            ApplyCrouch(false);
        }
    }

    private void ApplyCrouch(bool crouch)
    {
        if (crouch == isCrouching) return;

        if (crouch)
        {
            characterController.height = crouchHeight;
            characterController.center = new Vector3(standCenter.x, crouchCenterY, standCenter.z);
        }
        else
        {
            characterController.height = standHeight;
            characterController.center = standCenter;
        }

        isCrouching = crouch;
    }

    private bool CanStandUp()
    {
        if (characterController == null) return true;

        float radius = characterController.radius;
        float skin = characterController.skinWidth;
        Vector3 bottom = transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f - radius);
        float castDistance = standHeight - characterController.height;
        if (castDistance <= 0.01f) return true;

        if (Physics.SphereCast(bottom, radius - skin, Vector3.up, out RaycastHit hit, castDistance + 0.05f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider != characterController)
            {
                return false;
            }
        }

        return true;
    }
}
