using UnityEngine;

/// <summary>
/// Spider controller - drives spider movement based on external forces.
/// The spider will walk automatically when the Rigidbody has velocity.
/// Use this to apply forces to the spider from your game logic.
/// </summary>
public class SpiderController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveForce = 50f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    
    private Rigidbody rb;
    private Vector3 moveDirection = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[SpiderController] Rigidbody not found!");
            enabled = false;
        }
    }

    void Update()
    {
        // Get input (doesn't conflict with FPS - just for spider movement)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate move direction
        moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        
        // Rotate spider to face movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        
        // Apply force in movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 force = moveDirection * moveForce;
            rb.AddForce(force, ForceMode.Force);
            
            // Clamp horizontal speed
            Vector3 velocity = rb.linearVelocity;
            float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            if (horizontalSpeed > maxSpeed)
            {
                Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z).normalized * maxSpeed;
                rb.linearVelocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
            }
        }
    }
    
    /// <summary>
    /// Apply external force to spider (for AI, knockback, etc.)
    /// </summary>
    public void ApplyForce(Vector3 force)
    {
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
