using UnityEngine;

/// <summary>
/// Simple ammo pickup. Hook this to a Usable's OnUse event to refill the current weapon.
/// </summary>
public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int reserveAmount = 30;
    [SerializeField] private bool reloadAfterPickup = true;
    [SerializeField] private bool destroyOnUse = true;
    [Tooltip("Optional explicit target weapon ammo. If null, looks for the player's WeaponAmmo.")]
    [SerializeField] private WeaponAmmo targetOverride;

    /// <summary>
    /// Call this from Usable.OnUse (no parameters).
    /// </summary>
    public void GiveAmmo()
    {
        WeaponAmmo ammo = GetTarget();
        if (ammo == null) return;

        if (reserveAmount != 0)
        {
            ammo.AddReserve(reserveAmount);
        }

        if (reloadAfterPickup)
        {
            ammo.TryReload();
        }

        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
    }

    private WeaponAmmo GetTarget()
    {
        if (targetOverride != null)
            return targetOverride;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        // Try to find WeaponAmmo on the player or its children (weapon object).
        if (player.TryGetComponent(out WeaponAmmo ammoOnPlayer))
            return ammoOnPlayer;

        return player.GetComponentInChildren<WeaponAmmo>();
    }
}
