# Inventory & Keycards

## Core scripts
- `Assets/_CODE/_PLAYER/PlayerInventory.cs`: Tracks a single upgradable keycard level (int). Exposes `HasKeycard(requiredStage)`, `UpgradeKeycard(stage)`, `KeycardLevel`, `ClearInventory()`, and raises `OnKeycardLevelChanged` whenever the level increases or resets. A static `PlayerInventory.Instance` is set on `Awake()` for easy access. (Legacy `OnKeycardsChanged` still fires with a one-element set for compatibility.)
- `Assets/_CODE/_PLAYER/Keycard.cs`: Collectible component. Holds a `stage` value (the level to upgrade the one keycard to) and calls `PlayerInventory.UpgradeKeycard` on pickup. Uses `PlayerInventory.Instance`, falls back to a tagged player or a scene-wide search so pickups work even if the player is untagged.
- `Assets/_CODE/_UI/PlayerHUD.cs`: Listens to `OnKeycardLevelChanged` and formats a single line showing the current level (or “none”). If no inventory is assigned in the inspector it will try to grab `PlayerInventory.Instance` or find one in the scene at runtime.

## Pickup flow
1) Place a pickup object with `Keycard` + `Usable` and set the `stage` number the single card should upgrade to.  
2) The `Usable` event calls `Keycard.Collect()` (default prompt `F - keycard`).  
3) `Collect()` resolves the active `PlayerInventory` and upgrades the held card if this stage is higher.  
4) `OnKeycardLevelChanged` notifies listeners (HUD, doors, etc.) that the keycard level changed.

## Stage progression (design)
- Stage 1: dropped by the first boss outside; required to enter the first 1-floor building.
- Stage 2+: each building upgrades the same card to the next level (2-floor building upgrades to stage 3, etc.).
- Stage mapping: `stage N` -> grants access to the building with `N` floors/stages.
- Map idea: reveal all building markers when a run starts so players can plan routes even before unlocking them.

## Gating doors/elevators
Use the inventory check wherever an entrance needs a specific card (current level must be **>=** requirement):

```csharp
var inventory = PlayerInventory.Instance;
bool hasAccess = inventory != null && inventory.HasKeycard(requiredStage);

if (hasAccess)
{
    OpenDoor();
}
else
{
    ShowPrompt($"Need keycard stage {requiredStage}");
}
```

You can also subscribe to `OnKeycardLevelChanged` to refresh any lock UI:

```csharp
playerInventory.OnKeycardLevelChanged.AddListener(level => lockWidget.Refresh(level));
```

## UI notes
- Assign a TMP text element to `PlayerHUD.keycardText` to display the current level using `keycardFormat` (`Keycard L{0}` by default), or `noKeycardText` when level is 0.  
- If unassigned, the HUD still tracks inventory changes; only the on-screen display will be missing.

## Spawning/dropping cards
- To drop from an enemy/boss, spawn a prefab with `Keycard` + `Usable`; set `destroyOnCollect` as needed.  
- Because `Keycard` no longer depends on the player being tagged, drops remain collectible even if the player object uses a custom tag or prefab name.
