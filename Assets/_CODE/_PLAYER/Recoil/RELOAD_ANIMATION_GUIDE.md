# Reload Animation System

## Overview

The **Reload Animation** system adds smooth, cinematic reload animations to your weapon similar to the existing recoil system. It applies rotational and positional transformations to the weapon during reload, creating a gorgeous visual effect.

## Features

✨ **Smooth Weapon Movement** - Combines rotation and panning animations during reload  
🎯 **Configurable Parameters** - Fully customizable rotation amounts, pan directions, and animation curves  
⚡ **Performance Optimized** - Lightweight implementation that works alongside the recoil system  
🔄 **Easy Integration** - Automatically detects and uses existing components  

## Architecture

### Components

#### 1. **ReloadAnimation.cs**
The core component that handles all reload animation logic:

```csharp
public class ReloadAnimation : MonoBehaviour
{
    // Configuration
    public float reloadRotationPitch = -15f;        // Rotate down during reload
    public float reloadRotationYaw = 25f;           // Rotate to the side
    public Vector3 reloadPositionOffset;             // Pan position
    public AnimationCurve reloadAnimationCurve;     // Animation easing
    
    // Methods
    public void StartReload(float duration);
    public void EndReload();
    public float ReloadProgress { get; }
}
```

**How it works:**
1. When reload starts, `StartReload()` is called with the reload duration
2. Each frame, the animation updates the weapon transform based on elapsed time
3. The animation curve controls the easing (smoothness) of the motion
4. When reload completes, `EndReload()` returns the weapon to its original position

#### 2. **RecoilConfiguration.cs** (Extended)
Added reload animation parameters to the existing configuration:

```csharp
[Header("Reload Animation")]
public float reloadRotationPitch = -15f;              // Pitch rotation (negative = down)
public float reloadRotationYaw = 25f;                 // Yaw rotation (positive = right)
public Vector3 reloadPositionOffset = new Vector3(...); // Position pan
public AnimationCurve reloadAnimationCurve;           // Easing curve
```

#### 3. **WeaponController.cs** (Modified)
Updated to integrate reload animation:

```csharp
// RecoilSystem is now the primary system responsible for applying reload animation offsets.
[SerializeField] private ReloadAnimation reloadAnimation; // optional compatibility wrapper

private IEnumerator ReloadRoutine()
{
    // Start animation with parameters from recoil config using the RecoilSystem API
    if (recoilSystem != null)
    {
        Vector3 posOffset = recoilSystem.Config.reloadPositionOffset;
        Quaternion rotOffset = Quaternion.Euler(recoilSystem.Config.reloadRotationPitch, recoilSystem.Config.reloadRotationYaw, 0f);
        AnimationCurve curve = recoilSystem.Config.reloadAnimationCurve;
        recoilSystem.StartReloadAnimation(posOffset, rotOffset, reloadDuration, curve);
    }
    else if (reloadAnimation != null)
    {
        // Backwards compatibility fallback - use local ReloadAnimation
        reloadAnimation.SetReloadParameters(-15f, 25f, new Vector3(0.03f, -0.02f, 0.08f), reloadDuration, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        reloadAnimation.StartReload(reloadDuration);
    }

    // Wait for reload to finish then end the animation
    if (reloadDuration > 0f)
        yield return new WaitForSeconds(reloadDuration);

    if (recoilSystem != null)
    {
        recoilSystem.EndReloadAnimation();
    }
    else if (reloadAnimation != null)
    {
        reloadAnimation.EndReload();
    }
}
```

## Default Values

The reload animation comes with sensible defaults designed to look great:

| Parameter | Default | Range | Effect |
|-----------|---------|-------|--------|
| **Rotation Pitch** | -15° | Any | Rotates weapon down/back during reload |
| **Rotation Yaw** | 25° | Any | Rotates weapon to the side |
| **Position Offset X** | 0.03m | Any | Pans weapon right |
| **Position Offset Y** | -0.02m | Any | Pans weapon down slightly |
| **Position Offset Z** | 0.08m | Any | Moves weapon forward |
| **Animation Curve** | EaseInOut | Any | Smooth start and end |

## Customization

### In Unity Inspector

1. Select your FPS Player object
2. Find the **RecoilSystem** component
3. Expand **Reload Animation** settings
4. Adjust parameters to taste:

```
🎮 Reload Animation
├─ Reload Rotation Pitch: -15    [Rotate down]
├─ Reload Rotation Yaw: 25       [Rotate right]
├─ Reload Position Offset: (0.03, -0.02, 0.08)
│  ├─ X: Pan left/right
│  ├─ Y: Pan up/down
│  └─ Z: Move forward/back
└─ Reload Animation Curve: [EaseInOut curve graph]
```

### Programmatically

```csharp
// Get the reload animation
ReloadAnimation reloadAnim = GetComponentInChildren<ReloadAnimation>();

// Set custom parameters
reloadAnim.SetReloadParameters(
    pitchRotation: -20f,
    yawRotation: 30f,
    positionOffset: new Vector3(0.05f, -0.03f, 0.1f),
    duration: 1.5f,
    curve: AnimationCurve.EaseInOut(0, 0, 1, 1)
);

// Start/stop manually if needed
reloadAnim.StartReload(1.2f);
reloadAnim.EndReload();
```

## Integration with Recoil System

The reload animation is **designed to work alongside** the recoil system:

- **During Shooting**: Recoil system controls weapon position/rotation with its recovery curve
- **During Reload**: Reload animation takes over and applies smooth animation
- **After Reload**: Weapon returns to normal, recoil system resumes

They don't conflict because:
- Reload animation is active only during `isReloading`
- Recoil recovery is skipped during reload
- Animation curves are independent

## Examples

### Aggressive Reload
```csharp
reloadAnim.SetReloadParameters(
    pitchRotation: -30f,    // Rotate more
    yawRotation: 45f,
    positionOffset: new Vector3(0.08f, -0.05f, 0.15f),
    duration: 1.2f,
    curve: AnimationCurve.EaseInOut(0, 0, 1, 1)
);
```

### Gentle/Tactical Reload
```csharp
reloadAnim.SetReloadParameters(
    pitchRotation: -5f,     // Subtle rotation
    yawRotation: 10f,
    positionOffset: new Vector3(0.01f, 0f, 0.03f),
    duration: 1.5f,
    curve: AnimationCurve.Linear(0, 0, 1, 1)
);
```

### Fast Reload
```csharp
reloadAnim.SetReloadParameters(
    pitchRotation: -20f,
    yawRotation: 35f,
    positionOffset: new Vector3(0.05f, -0.03f, 0.12f),
    duration: 0.8f,  // Faster
    curve: AnimationCurve.EaseInOut(0, 0, 1, 1)
);
```

## Physics & Stability

> ⚠️ **Note**: The reload animation uses **local space transforms only** - no physics interference!

The animation:
- ✅ Applies to `weaponTransform.localPosition` and `localRotation`
- ✅ Does NOT modify rigidbody physics
- ✅ Does NOT affect character movement
- ✅ Does NOT change aim or camera rotation
- ❌ Cannot cause physics issues because it's purely visual

## Troubleshooting

### Animation Not Playing
**Problem**: Reload animation doesn't appear

**Solution:**
1. Ensure **ReloadAnimation** component is attached to the same GameObject as **RecoilSystem**
2. Check **WeaponController** has `reloadAnimation` reference set
3. Verify weapon transform exists under camera as "Weapon"
4. Check console for warnings/errors

### Animation Looks Jerky
**Problem**: Animation has frame skips or stutters

**Solution:**
1. Ensure **AnimationCurve** is smooth (use Unity's preset curves)
2. Check frame rate isn't dropping (use Profiler)
3. Reduce complexity of other systems during reload

### Animation Too Subtle/Aggressive
**Problem**: Motion isn't visible or is too extreme

**Solution:**
1. Increase/decrease rotation and position values proportionally
2. Extend reload duration to see motion more clearly
3. Adjust animation curve for slower/faster progression

## Performance

- **CPU Cost**: Negligible (single transform update per frame)
- **Memory**: ~1KB per instance
- **GPU Cost**: None (CPU-side animation)

Perfect for optimization! 🚀

## Future Enhancements

Potential additions:
- [ ] Per-magazine-type animations (different speeds/motions)
- [ ] Reload cancel animation (abort mid-reload)
- [ ] Partial reload animation
- [ ] Weapon bob/idle animation between reloads
- [ ] Realistic magazine switch animation with multiple keyframes

---

**Created**: October 2025  
**System**: Reload Animation v1.0  
**Compatibility**: Unity 2021.3+, C# 9.0+
