
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Audio")]
    [SerializeField] private FootstepSounds footstepSounds;

    private CharacterController characterController;
    private PlayerInputHandler inputHandler;
    private float verticalVelocity = 0f;
    private bool isGrounded = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        
        // Find FootstepSounds if not assigned
        if (footstepSounds == null)
        {
            footstepSounds = GetComponent<FootstepSounds>();
        }
    }

    void Update()
    {
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

        movement *= moveSpeed;

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
}
