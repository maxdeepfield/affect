using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Simple HUD updater for health and ammo using standard UI Text components.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Data Sources")]
    [SerializeField] private Health health;
    [SerializeField] private WeaponAmmo weaponAmmo;

    [Header("UI Text Targets")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text ammoText;

    [Header("Formatting")]
    [SerializeField] private string healthFormat = "HP {0}/{1}";
    [SerializeField] private string ammoFormat = "Ammo {0}/{1}";
    [SerializeField] private string infiniteSymbol = "INF";

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (weaponAmmo == null)
            weaponAmmo = GetComponentInParent<WeaponAmmo>();
    }

    private void OnEnable()
    {
        RegisterListeners();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterListeners();
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

    private void RegisterListeners()
    {
        if (health != null)
            health.HealthChanged += OnHealthChanged;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged += OnAmmoChanged;
    }

    private void UnregisterListeners()
    {
        if (health != null)
            health.HealthChanged -= OnHealthChanged;

        if (weaponAmmo != null)
            weaponAmmo.AmmoChanged -= OnAmmoChanged;
    }

    private void RefreshAll()
    {
        if (health != null)
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);

        if (weaponAmmo != null)
            OnAmmoChanged(weaponAmmo.CurrentMagazine, weaponAmmo.ReserveAmmo, weaponAmmo.InfiniteReserve, weaponAmmo.InfiniteMagazine);
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
}
