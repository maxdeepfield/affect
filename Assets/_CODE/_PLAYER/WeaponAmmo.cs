using System;
using UnityEngine;

/// <summary>
/// Lightweight ammo holder for weapons. Keeps magazine + reserve counts and exposes change events for UI.
/// </summary>
public class WeaponAmmo : MonoBehaviour
{
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int startingInMagazine = 30;
    [SerializeField] private int startingReserve = 90;
    [SerializeField] private bool infiniteReserve = true;
    [SerializeField] private bool infiniteMagazine;

    public int MagazineSize => magazineSize;
    public int CurrentMagazine { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool InfiniteReserve => infiniteReserve;
    public bool InfiniteMagazine => infiniteMagazine;
    public bool IsMagazineEmpty => !infiniteMagazine && CurrentMagazine <= 0;
    public bool CanReload => !infiniteMagazine && (infiniteReserve || ReserveAmmo > 0) && CurrentMagazine < magazineSize;

    public event Action<int, int, bool, bool> AmmoChanged;

    private void Awake()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        startingInMagazine = Mathf.Clamp(startingInMagazine, 0, magazineSize);

        CurrentMagazine = infiniteMagazine ? magazineSize : startingInMagazine;
        ReserveAmmo = Mathf.Max(0, startingReserve);

        NotifyAmmoChanged();
    }

    public bool TryConsumeRound()
    {
        if (infiniteMagazine)
        {
            NotifyAmmoChanged();
            return true;
        }

        if (CurrentMagazine <= 0) return false;

        CurrentMagazine--;
        NotifyAmmoChanged();
        return true;
    }

    public bool TryReload()
    {
        if (!CanReload) return false;

        if (infiniteReserve)
        {
            CurrentMagazine = magazineSize;
        }
        else
        {
            int needed = magazineSize - CurrentMagazine;
            int toLoad = Mathf.Min(needed, ReserveAmmo);
            ReserveAmmo -= toLoad;
            CurrentMagazine += toLoad;
        }

        NotifyAmmoChanged();
        return true;
    }

    public void AddReserve(int amount)
    {
        if (amount == 0 || infiniteReserve) return;

        ReserveAmmo = Mathf.Max(0, ReserveAmmo + amount);
        NotifyAmmoChanged();
    }

    public void SetInfiniteReserve(bool isInfinite)
    {
        infiniteReserve = isInfinite;
        NotifyAmmoChanged();
    }

    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(CurrentMagazine, ReserveAmmo, infiniteReserve, infiniteMagazine);
    }
}
