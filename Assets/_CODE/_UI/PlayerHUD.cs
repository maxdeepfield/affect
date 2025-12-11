using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Simple HUD updater for health, ammo, and keycard level using standard UI Text components.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [Header("Data Sources")]
    [SerializeField] private Health health;
    [SerializeField] private WeaponAmmo weaponAmmo;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI Text Targets")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text keycardText;

    [Header("Formatting")]
    [SerializeField] private string healthFormat = "HP {0}/{1}";
    [SerializeField] private string ammoFormat = "Ammo {0}/{1}";
    [SerializeField] private string keycardFormat = "Keycard L{0}";
    [SerializeField] private string noKeycardText = "Keycard: none";
    [SerializeField] private string infiniteSymbol = "INF";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerHUD instances found. Using the first one.");
        }
        else
        {
            Instance = this;
        }

        if (health == null)
            health = GetComponentInParent<Health>();

        if (weaponAmmo == null)
            weaponAmmo = GetComponentInParent<WeaponAmmo>();

        EnsurePlayerInventoryReference();
    }

    private void OnEnable()
    {
        EnsurePlayerInventoryReference();
        RegisterListeners();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetHealth(Health newHealth)
    {
        if (health == newHealth) return;

        if (health != null)
            health.HealthChanged -= OnHealthChanged;

        health = newHealth;

        if (health != null)
            health.HealthChanged += OnHealthChanged;

        if (health != null)
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    public void SetWeaponAmmo(WeaponAmmo newAmmo)
    {
        if (weaponAmmo == newAmmo) return;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged -= OnAmmoChanged;

        weaponAmmo = newAmmo;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged += OnAmmoChanged;

        if (weaponAmmo != null)
            OnAmmoChanged(weaponAmmo.CurrentMagazine, weaponAmmo.ReserveAmmo, weaponAmmo.InfiniteReserve, weaponAmmo.InfiniteMagazine);
    }

    public void SetPlayerInventory(PlayerInventory newInventory)
    {
        if (playerInventory == newInventory) return;

        if (playerInventory != null)
            playerInventory.OnKeycardLevelChanged.RemoveListener(OnKeycardLevelChanged);

        playerInventory = newInventory;

        if (playerInventory != null)
            playerInventory.OnKeycardLevelChanged.AddListener(OnKeycardLevelChanged);

        if (playerInventory != null)
            OnKeycardLevelChanged(playerInventory.KeycardLevel);
    }

    private void RegisterListeners()
    {
        if (health != null)
            health.HealthChanged += OnHealthChanged;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged += OnAmmoChanged;

        if (playerInventory != null)
            playerInventory.OnKeycardLevelChanged.AddListener(OnKeycardLevelChanged);
    }

    private void UnregisterListeners()
    {
        if (health != null)
            health.HealthChanged -= OnHealthChanged;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged -= OnAmmoChanged;

        if (playerInventory != null)
            playerInventory.OnKeycardLevelChanged.RemoveListener(OnKeycardLevelChanged);
    }

    private void RefreshAll()
    {
        if (health != null)
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);

        if (weaponAmmo != null)
            OnAmmoChanged(weaponAmmo.CurrentMagazine, weaponAmmo.ReserveAmmo, weaponAmmo.InfiniteReserve, weaponAmmo.InfiniteMagazine);

        if (playerInventory != null)
            OnKeycardLevelChanged(playerInventory.KeycardLevel);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthText == null) return;

        healthText.text = string.Format(healthFormat, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
    }

    private void OnAmmoChanged(int magazine, int reserve, bool infiniteReserve, bool infiniteMagazine)
    {
        if (ammoText == null) return;

        string magazineValue = infiniteMagazine ? infiniteSymbol : Mathf.Max(0, magazine).ToString();
        string reserveValue = infiniteReserve ? infiniteSymbol : Mathf.Max(0, reserve).ToString();

        ammoText.text = string.Format(ammoFormat, magazineValue, reserveValue);
    }

    private void OnKeycardLevelChanged(int level)
    {
        if (keycardText == null) return;

        if (level <= 0)
        {
            keycardText.text = noKeycardText;
            return;
        }

        keycardText.text = string.Format(keycardFormat, level);
    }

    private void EnsurePlayerInventoryReference()
    {
        if (playerInventory != null) return;

        playerInventory = GetComponentInParent<PlayerInventory>();
        if (playerInventory == null)
            playerInventory = PlayerInventory.Instance ?? FindObjectOfType<PlayerInventory>();
    }

    public TMP_Text KeycardText => keycardText;
}
