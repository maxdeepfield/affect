using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Door that requires a minimum keycard level to open.
/// Part of the single-keycard progression system.
/// </summary>
[RequireComponent(typeof(Usable))]
public class KeycardDoor : MonoBehaviour
{
    [Header("Access Requirements")]
    [SerializeField] private int requiredLevel = 1;

    [Header("Door Settings")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private bool staysOpen = true;

    [Header("Visuals")]
    [SerializeField] private Renderer doorRenderer;
    [SerializeField] private Light accessLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip unlockSound;

    [Header("Events")]
    public UnityEvent OnDoorOpened;
    public UnityEvent OnAccessDenied;
    public UnityEvent OnDoorUnlocked;

    private Usable usable;
    private AudioSource audioSource;
    private bool isOpen;
    private bool isUnlocked;
    private float currentAngle;
    private float targetAngle;
    private Quaternion closedRotation;

    private void Awake()
    {
        usable = GetComponent<Usable>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (doorPivot == null)
        {
            doorPivot = transform;
        }

        closedRotation = doorPivot.localRotation;
        currentAngle = 0f;
        targetAngle = 0f;

        UpdateAccessState();
    }

    private void Start()
    {
        if (usable != null)
        {
            UpdatePrompt();
            usable.ClearListeners();
            usable.AddListener(TryOpen);
        }

        // Subscribe to keycard changes
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.AddListener(OnKeycardChanged);
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.RemoveListener(OnKeycardChanged);
        }
    }

    private void Update()
    {
        // Animate door
        if (Mathf.Abs(currentAngle - targetAngle) > 0.1f)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * 60f * Time.deltaTime);
            doorPivot.localRotation = closedRotation * Quaternion.Euler(0f, currentAngle, 0f);
        }
    }

    /// <summary>
    /// Called when player tries to use the door.
    /// </summary>
    public void TryOpen()
    {
        if (isOpen && staysOpen) return;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[KeycardDoor] No PlayerInventory found!");
            return;
        }

        if (inventory.KeycardLevel >= requiredLevel)
        {
            Open();
        }
        else
        {
            AccessDenied();
        }
    }

    private void Open()
    {
        if (isOpen) return;

        isOpen = true;
        targetAngle = openAngle;

        PlaySound(openSound);
        OnDoorOpened.Invoke();

        if (usable != null)
        {
            usable.SetPrompt(staysOpen ? "Door Open" : "Close Door");
        }
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        targetAngle = 0f;

        UpdatePrompt();
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            TryOpen();
    }

    private void AccessDenied()
    {
        PlaySound(lockedSound);
        OnAccessDenied.Invoke();

        // Visual feedback - flash red
        if (accessLight != null)
        {
            StartCoroutine(FlashLight(lockedColor, 0.3f));
        }
    }

    private void OnKeycardChanged(int newLevel)
    {
        bool wasUnlocked = isUnlocked;
        UpdateAccessState();

        // Play unlock sound when door becomes accessible
        if (!wasUnlocked && isUnlocked)
        {
            PlaySound(unlockSound);
            OnDoorUnlocked.Invoke();

            // Visual feedback - flash green
            if (accessLight != null)
            {
                StartCoroutine(FlashLight(unlockedColor, 0.5f));
            }
        }

        UpdatePrompt();
    }

    private void UpdateAccessState()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        isUnlocked = inventory != null && inventory.KeycardLevel >= requiredLevel;

        // Update visual indicator
        if (accessLight != null)
        {
            accessLight.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (doorRenderer != null)
        {
            // Could tint the door material here
        }
    }

    private void UpdatePrompt()
    {
        if (usable == null) return;

        if (isOpen)
        {
            usable.SetPrompt(staysOpen ? "Door Open" : "Close Door");
        }
        else if (isUnlocked)
        {
            usable.SetPrompt("Open Door");
        }
        else
        {
            usable.SetPrompt($"Requires Keycard Level {requiredLevel}");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    private System.Collections.IEnumerator FlashLight(Color flashColor, float duration)
    {
        if (accessLight == null) yield break;

        Color originalColor = accessLight.color;
        float originalIntensity = accessLight.intensity;

        accessLight.color = flashColor;
        accessLight.intensity = originalIntensity * 2f;

        yield return new WaitForSeconds(duration);

        accessLight.color = isUnlocked ? unlockedColor : lockedColor;
        accessLight.intensity = originalIntensity;
    }

    private void OnValidate()
    {
        requiredLevel = Mathf.Max(1, requiredLevel);
    }

    private void OnDrawGizmosSelected()
    {
        // Show required level in scene view
        Gizmos.color = requiredLevel <= 2 ? Color.green : (requiredLevel <= 4 ? Color.yellow : Color.red);
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.5f, 0.5f, 0.1f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"LV.{requiredLevel}");
#endif
    }
}
