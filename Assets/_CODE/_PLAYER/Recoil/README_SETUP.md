# Recoil System Setup Guide

## Quick Setup for _PLAYER Prefab

To add the RecoilSystem to the _PLAYER prefab, use the Unity Editor menu:

**Tools > Recoil System > Setup _PLAYER Prefab**

This will automatically:
1. Add `RecoilSystem` component to the player root
2. Create a `RecoilModules` child GameObject with:
   - `RecoilRandomizer` (procedural variation)
   - `MouseTracker` (compensation detection)
   - `CameraShaker` (cinematic shake)
3. Wire camera and weapon transform references
4. Apply default configuration values
5. Connect `WeaponController` to the `RecoilSystem`

## Manual Setup

If you prefer manual setup or need to configure a different GameObject:

1. Open **Tools > Recoil System > Setup Player Recoil**
2. Drag your player GameObject into the "Target Player" field
3. Click "Setup Recoil System"

## Default Configuration Values

The setup applies these default values:

| Parameter | Value | Description |
|-----------|-------|-------------|
| Base Vertical Kick | 2° | Upward rotation per shot |
| Max Accumulated Vertical | 15° | Maximum vertical recoil |
| Base Horizontal Kick | 0.5° | Sideways rotation per shot |
| Horizontal Spread | 2° | Max horizontal spread |
| Weapon Kickback Distance | 0.05m | Backward weapon movement |
| Weapon Rotation Kick | 3° | Weapon tilt per shot |
| Recovery Speed | 8 | How fast recoil recovers |
| Compensation Multiplier | 1.5 | Mouse compensation strength |
| Max Compensation Rate | 2 | Max compensation speed |
| Shake Intensity | 0.02m | Camera shake amount |
| Shake Frequency | 25 Hz | Shake oscillation speed |
| Path Follow Strength | 0.5 | Shake follows recoil path |

## Requirements

The setup expects the player prefab to have:
- A `Camera` component in children (for camera recoil)
- A child named "Weapon" (or similar) for weapon transform recoil
- `MouseLook` component (optional, for recoil offset integration)
- `WeaponController` component (optional, for automatic wiring)

## After Setup

After running the setup, verify in the Inspector:
1. Camera Transform reference is assigned
2. Weapon Transform reference is assigned
3. RecoilConfiguration values are appropriate for your weapon

You can adjust all values in the Inspector at runtime to tune the feel.
