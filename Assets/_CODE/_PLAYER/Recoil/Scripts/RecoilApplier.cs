using UnityEngine;

/// <summary>
/// Handles applying recoil to the camera and weapon.
/// Simple, focused responsibility.
/// </summary>
public class RecoilApplier
{
    private RecoilConfigurationSO _config;

    public RecoilApplier(RecoilConfigurationSO config)
    {
        _config = config;
    }

    public void Apply(float multiplier, ref RecoilState state)
    {
        // Generate base recoil
        float vertical = _config.baseVerticalKick * multiplier;
        float horizontal = Random.Range(-_config.baseHorizontalKick, _config.baseHorizontalKick) * multiplier;

        // Clamp to constraints
        vertical = Mathf.Clamp(vertical, 0.5f, 5f);
        horizontal = Mathf.Clamp(horizontal, -2f, 2f);

        // Accumulate
        state.accumulatedRecoil += new Vector2(vertical, horizontal);
        state.accumulatedRecoil.x = Mathf.Clamp(state.accumulatedRecoil.x, 0f, _config.maxAccumulatedVertical);
        state.accumulatedRecoil.y = Mathf.Clamp(state.accumulatedRecoil.y, -_config.horizontalSpread * 2f, _config.horizontalSpread * 2f);

        // Update state
        state.shotCount++;
        state.timeSinceLastShot = 0f;
        state.currentPath = new Vector2(vertical, horizontal).normalized;

        // Apply weapon recoil offsets
        state.weaponPositionOffset = new Vector3(0f, 0f, -_config.weaponKickbackDistance);
        state.weaponRotationOffset = Quaternion.Euler(-_config.weaponRotationKick, 0f, 0f);
    }

    public void Update(float deltaTime)
    {
        // Called each frame for any continuous updates
    }
}
