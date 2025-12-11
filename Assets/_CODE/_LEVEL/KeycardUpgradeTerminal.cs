using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Terminal where player upgrades their keycard after completing a building.
/// Usually placed at the end/top of each building.
/// 
/// When used:
/// 1. Plays upgrade animation/sound
/// 2. Upgrades player's keycard
/// 3. Triggers world transformation (new building descends)
/// </summary>
[RequireComponent(typeof(Usable))]
public class KeycardUpgradeTerminal : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [Tooltip("What level this terminal upgrades the keycard TO")]
    [SerializeField] private int upgradeToLevel = 1;
    [SerializeField] private bool autoDetectLevel = true;
    [SerializeField] private bool oneTimeUse = true;

    [Header("Visuals")]
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject usedVisual;
    [SerializeField] private Light terminalLight;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color usedColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioClip deniedSound;

    [Header("Events")]
    public UnityEvent OnUpgradeSuccess;
    public UnityEvent OnUpgradeDenied;

    private Usable usable;
    private bool hasBeenUsed;
    private AudioSource audioSource;

    private void Awake()
    {
        usable = GetComponent<Usable>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Auto-detect level based on current keycard + 1
        if (autoDetectLevel && PlayerInventory.Instance != null)
        {
            upgradeToLevel = PlayerInventory.Instance.KeycardLevel + 1;
        }

        UpdateVisuals();
    }

    private void Start()
    {
        // Set up usable prompt
        if (usable != null)
        {
            usable.SetPrompt($"Upgrade Keycard to Level {upgradeToLevel}");
            usable.ClearListeners();
            usable.AddListener(TryUpgrade);
        }
    }

    /// <summary>
    /// Attempts to upgrade the player's keycard.
    /// </summary>
    public void TryUpgrade()
    {
        if (hasBeenUsed && oneTimeUse)
        {
            PlaySound(deniedSound);
            OnUpgradeDenied.Invoke();
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[KeycardUpgradeTerminal] No PlayerInventory found!");
            return;
        }

        // Check if player already has this level or higher
        if (inventory.KeycardLevel >= upgradeToLevel)
        {
            PlaySound(deniedSound);
            OnUpgradeDenied.Invoke();
            Debug.Log($"[KeycardUpgradeTerminal] Player already has level {inventory.KeycardLevel}, terminal offers {upgradeToLevel}");
            return;
        }

        // NO TIME GATING in main game - Weekly Stage is a separate mode!
        // SUCCESS - Upgrade the keycard!
        hasBeenUsed = true;
        inventory.UpgradeKeycard(upgradeToLevel);

        PlaySound(upgradeSound);
        UpdateVisuals();
        OnUpgradeSuccess.Invoke();

        Debug.Log($"[KeycardUpgradeTerminal] Keycard upgraded to level {upgradeToLevel}!");

        // Update prompt
        if (usable != null)
        {
            usable.SetPrompt("Terminal Used");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void UpdateVisuals()
    {
        bool isActive = !hasBeenUsed;

        if (activeVisual != null)
            activeVisual.SetActive(isActive);

        if (usedVisual != null)
            usedVisual.SetActive(!isActive);

        if (terminalLight != null)
            terminalLight.color = isActive ? activeColor : usedColor;
    }

    /// <summary>
    /// Resets the terminal (for testing or level restart).
    /// </summary>
    public void ResetTerminal()
    {
        hasBeenUsed = false;
        UpdateVisuals();

        if (usable != null)
        {
            usable.SetPrompt($"Upgrade Keycard to Level {upgradeToLevel}");
        }
    }

    private void OnValidate()
    {
        upgradeToLevel = Mathf.Max(1, upgradeToLevel);
    }
}
