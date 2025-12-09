using UnityEngine;

/// <summary>
/// Handles player footstep sounds based on movement.
/// Attach to the player GameObject with CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootstepSounds : MonoBehaviour
{
    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.5f;
    [SerializeField] [Range(0f, 0.2f)] private float pitchVariation = 0.1f;

    [Header("Timing")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.35f;
    [SerializeField] private float minVelocityForSound = 0.5f;

    [Header("Jump/Land Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] [Range(0f, 1f)] private float jumpLandVolume = 0.6f;

    private CharacterController characterController;
    private AudioSource audioSource;
    private float stepTimer;
    private bool wasGrounded = true;
    private int lastFootstepIndex = -1;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        HandleFootsteps();
        HandleLanding();
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded) return;

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        if (speed < minVelocityForSound)
        {
            stepTimer = 0f;
            return;
        }

        float interval = walkStepInterval;
        
        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    private void HandleLanding()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && !wasGrounded)
        {
            PlayLandSound();
        }

        wasGrounded = isGrounded;
    }

    private void PlayFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Avoid repeating the same sound
        int index = Random.Range(0, footstepSounds.Length);
        if (footstepSounds.Length > 1 && index == lastFootstepIndex)
        {
            index = (index + 1) % footstepSounds.Length;
        }
        lastFootstepIndex = index;

        AudioClip clip = footstepSounds[index];
        if (clip == null) return;

        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(clip, transform.position, footstepVolume, pitch);
        }
        else
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    /// <summary>
    /// Call this when player jumps.
    /// </summary>
    public void PlayJumpSound()
    {
        if (jumpSound == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(jumpSound, transform.position, jumpLandVolume);
        }
        else
        {
            audioSource.PlayOneShot(jumpSound, jumpLandVolume);
        }
    }

    private void PlayLandSound()
    {
        if (landSound == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(landSound, transform.position, jumpLandVolume);
        }
        else
        {
            audioSource.PlayOneShot(landSound, jumpLandVolume);
        }
    }

    /// <summary>
    /// Set footstep clips at runtime.
    /// </summary>
    public void SetFootstepClips(AudioClip[] clips)
    {
        footstepSounds = clips;
    }
}
