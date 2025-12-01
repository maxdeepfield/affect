using UnityEngine;

/// <summary>
/// Handles weapon sound effects. Attach to the same GameObject as WeaponController.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeaponSounds : MonoBehaviour
{
    [Header("Fire Sounds")]
    [SerializeField] private AudioClip[] fireSounds;
    [SerializeField] [Range(0f, 1f)] private float fireVolume = 0.8f;
    [SerializeField] [Range(0f, 0.3f)] private float firePitchVariation = 0.05f;

    [Header("Reload Sounds")]
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] [Range(0f, 1f)] private float reloadVolume = 0.6f;

    [Header("Empty Click")]
    [SerializeField] private AudioClip emptyClickSound;
    [SerializeField] [Range(0f, 1f)] private float emptyClickVolume = 0.5f;

    [Header("Shell Casing")]
    [SerializeField] private AudioClip[] shellDropSounds;
    [SerializeField] [Range(0f, 1f)] private float shellVolume = 0.3f;

    private AudioSource audioSource;
    private Transform muzzlePosition;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Start()
    {
        // Try to find muzzle position from WeaponController
        WeaponController weapon = GetComponent<WeaponController>();
        if (weapon != null)
        {
            // Use reflection or serialize field to get muzzle position
            // For simplicity, we'll use the transform position
        }
    }

    /// <summary>
    /// Play weapon fire sound. Call this from WeaponController.Shoot()
    /// </summary>
    public void PlayFireSound()
    {
        if (fireSounds == null || fireSounds.Length == 0) return;

        AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
        if (clip == null) return;

        float pitch = 1f + Random.Range(-firePitchVariation, firePitchVariation);
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(clip, transform.position, fireVolume, pitch);
        }
        else
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, fireVolume);
        }
    }

    /// <summary>
    /// Play reload sound.
    /// </summary>
    public void PlayReloadSound()
    {
        if (reloadSound == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(reloadSound, transform.position, reloadVolume);
        }
        else
        {
            audioSource.PlayOneShot(reloadSound, reloadVolume);
        }
    }

    /// <summary>
    /// Play empty click sound when trying to fire with no ammo.
    /// </summary>
    public void PlayEmptyClick()
    {
        if (emptyClickSound == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(emptyClickSound, transform.position, emptyClickVolume);
        }
        else
        {
            audioSource.PlayOneShot(emptyClickSound, emptyClickVolume);
        }
    }

    /// <summary>
    /// Play shell casing drop sound. Call with delay after firing.
    /// </summary>
    public void PlayShellDrop(Vector3 position)
    {
        if (shellDropSounds == null || shellDropSounds.Length == 0) return;

        AudioClip clip = shellDropSounds[Random.Range(0, shellDropSounds.Length)];
        if (clip == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClip(clip, position, shellVolume, Random.Range(0.9f, 1.1f));
        }
    }
}
