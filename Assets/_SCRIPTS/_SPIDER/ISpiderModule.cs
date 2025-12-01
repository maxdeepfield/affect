using UnityEngine;

/// <summary>
/// Interface for modular Spider IK system components.
/// Modules can be enabled/disabled independently and the SpiderIKSystem
/// will continue functioning with remaining active modules.
/// </summary>
public interface ISpiderModule
{
    /// <summary>
    /// Gets or sets whether this module is enabled.
    /// Disabled modules are skipped during IK processing.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes the module with a reference to the parent SpiderIKSystem.
    /// Called once when the SpiderIKSystem discovers and wires child modules.
    /// </summary>
    /// <param name="system">The parent SpiderIKSystem orchestrator</param>
    void Initialize(SpiderIKSystem system);

    /// <summary>
    /// Called every frame to update the module's state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update</param>
    void OnUpdate(float deltaTime);

    /// <summary>
    /// Called every fixed update for physics-related processing.
    /// </summary>
    /// <param name="fixedDeltaTime">Fixed time step</param>
    void OnFixedUpdate(float fixedDeltaTime);

    /// <summary>
    /// Resets the module to its initial state.
    /// Called when the IK system is reset or the spider configuration changes.
    /// </summary>
    void Reset();
}
