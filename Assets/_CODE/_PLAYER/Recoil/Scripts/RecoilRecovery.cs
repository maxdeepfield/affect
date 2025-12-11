using UnityEngine;

/// <summary>
/// Handles recoil recovery over time.
/// Simple, focused responsibility.
/// </summary>
public class RecoilRecovery
{
    private RecoilConfigurationSO _config;

    public RecoilRecovery(RecoilConfigurationSO config)
    {
        _config = config;
    }

    public void Update(float deltaTime, ref RecoilState state)
    {
        // Check if recovery needed
        if (state.accumulatedRecoil.sqrMagnitude < 0.0001f && state.weaponPositionOffset.sqrMagnitude < 0.0001f)
        {
            state.accumulatedRecoil = Vector2.zero;
            state.weaponPositionOffset = Vector3.zero;
            state.weaponRotationOffset = Quaternion.identity;
            state.shotCount = 0;
            return;
        }

        // Apply recovery
        float recoveryAmount = _config.recoverySpeed * deltaTime;

        // Recover accumulated recoil
        state.accumulatedRecoil = Vector2.MoveTowards(state.accumulatedRecoil, Vector2.zero, recoveryAmount);

        // Recover weapon position offset
        state.weaponPositionOffset = Vector3.MoveTowards(state.weaponPositionOffset, Vector3.zero, recoveryAmount * 0.01f);

        // Recover weapon rotation offset
        state.weaponRotationOffset = Quaternion.Slerp(state.weaponRotationOffset, Quaternion.identity, recoveryAmount * 10f * deltaTime);

        // Clamp to zero
        if (state.accumulatedRecoil.magnitude < 0.01f)
        {
            state.accumulatedRecoil = Vector2.zero;
            state.shotCount = 0;
        }

        if (state.weaponPositionOffset.magnitude < 0.0001f)
            state.weaponPositionOffset = Vector3.zero;

        state.timeSinceLastShot += deltaTime;
    }

    public void Reset()
    {
        // Reset is handled via state.Reset()
    }
}
