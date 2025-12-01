using UnityEngine;

/// <summary>
/// Represents the current state of the recoil system.
/// Tracks accumulated recoil, shot count, and weapon transform offsets.
/// </summary>
[System.Serializable]
public struct RecoilState
{
    /// <summary>
    /// Current accumulated recoil offset (x = pitch/vertical, y = yaw/horizontal) in degrees.
    /// </summary>
    public Vector2 accumulatedRecoil;

    /// <summary>
    /// Direction of current recoil motion for path-following effects.
    /// </summary>
    public Vector2 currentPath;

    /// <summary>
    /// Number of shots fired in the current burst (resets after recovery).
    /// </summary>
    public int shotCount;

    /// <summary>
    /// Time elapsed since the last shot was fired, used for recovery timing.
    /// </summary>
    public float timeSinceLastShot;

    /// <summary>
    /// Current weapon position offset from recoil (local space).
    /// </summary>
    public Vector3 weaponPositionOffset;

    /// <summary>
    /// Current weapon rotation offset from recoil (local space).
    /// </summary>
    public Quaternion weaponRotationOffset;

    /// <summary>
    /// Creates a default/reset recoil state.
    /// </summary>
    public static RecoilState Default => new RecoilState
    {
        accumulatedRecoil = Vector2.zero,
        currentPath = Vector2.zero,
        shotCount = 0,
        timeSinceLastShot = 0f,
        weaponPositionOffset = Vector3.zero,
        weaponRotationOffset = Quaternion.identity
    };

    /// <summary>
    /// Resets the recoil state to default values.
    /// </summary>
    public void Reset()
    {
        accumulatedRecoil = Vector2.zero;
        currentPath = Vector2.zero;
        shotCount = 0;
        timeSinceLastShot = 0f;
        weaponPositionOffset = Vector3.zero;
        weaponRotationOffset = Quaternion.identity;
    }

    /// <summary>
    /// Gets the magnitude of the current accumulated recoil.
    /// </summary>
    public float RecoilMagnitude => accumulatedRecoil.magnitude;
}
