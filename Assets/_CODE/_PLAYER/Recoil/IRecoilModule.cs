using UnityEngine;

/// <summary>
/// Interface for modular recoil system components.
/// Modules can be enabled/disabled independently and the RecoilSystem
/// will continue functioning with remaining active modules.
/// </summary>
public interface IRecoilModule
{
    /// <summary>
    /// Gets or sets whether this module is enabled.
    /// Disabled modules are skipped during recoil processing.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes the module with a reference to the parent RecoilSystem.
    /// Called once when the RecoilSystem discovers and wires child modules.
    /// </summary>
    /// <param name="system">The parent RecoilSystem orchestrator</param>
    void Initialize(RecoilSystem system);

    /// <summary>
    /// Called when recoil is applied (a shot is fired).
    /// Modules can respond to the recoil event and update their internal state.
    /// </summary>
    /// <param name="recoilDelta">The recoil delta being applied (x = vertical, y = horizontal)</param>
    void OnRecoilApplied(Vector2 recoilDelta);

    /// <summary>
    /// Called every frame to update the module's state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update</param>
    void OnUpdate(float deltaTime);

    /// <summary>
    /// Resets the module to its initial state.
    /// Called when the recoil system is reset or the weapon is changed.
    /// </summary>
    void Reset();
}
