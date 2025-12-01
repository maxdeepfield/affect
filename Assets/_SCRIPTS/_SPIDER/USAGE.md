# Spider IK Walker System - Usage Guide

A modular, procedural locomotion system for spider-like creatures in Unity with configurable legs (1-8), bone counts (1-3 per leg), and multi-surface support (ground, walls, ceilings).

## Quick Start

### Creating a Spider (New System)

1. **From Menu:**
   - Go to `GameObject > Spider IK > Create Spider`
   - This creates a fully configured spider with all modules

2. **Manual Setup:**
   - Create an empty GameObject
   - Add `SpiderIKSystem` component
   - Click "Add Required Components" in the inspector
   - Configure the `IKConfiguration` settings

### Using Presets

1. **Select your spider GameObject**
2. **In the Inspector, find the SpiderIKSystemEditor section**
3. **Click a preset button:**
   - **Spider Walker** - 4 legs, 3 bones, standard spider locomotion
   - **Animal Crouch** - 4 legs, 2 bones, low crouching movement
   - **Hopper** - 2 legs, 2 bones, jumping creature
   - **Octopod** - 8 legs, 3 bones, octopus-like movement

## Configuration

### IKConfiguration Properties

```csharp
// Leg Setup
legCount = 4;              // 1-8 legs
boneCount = 3;             // 1-3 bones per leg (1=direct, 2=hip-foot, 3=hip-knee-foot)

// Body Dimensions
bodyRadius = 0.3f;         // Capsule radius (meters)
bodyHeight = 0.6f;         // Capsule height (meters)
bodyToLegRatio = 1.5f;     // Leg root distance multiplier
dimensionScale = 1f;       // Global scale multiplier

// Leg Proportions
legLength = 0.6f;          // Total leg length (meters)
hipRatio = 0.5f;           // Upper leg portion (0-1)
legSpread = 0.8f;          // Horizontal spread multiplier

// Walking
stepThreshold = 0.4f;      // Distance to trigger step
stepHeight = 0.1f;         // Arc height during step
stepSpeed = 5f;            // Steps per second
strideForward = 0.3f;      // Base forward projection
strideVelocityScale = 0.4f; // Velocity-based stride multiplier

// Stabilization
uprightStrength = 20f;     // Torque strength for orientation
uprightDamping = 6f;       // Damping to prevent oscillation
heightStrength = 30f;      // Force strength for height maintenance
heightDamping = 6f;        // Damping to prevent bouncing

// Hit Reaction
hitImpulse = 6f;           // Impulse force on collision
scuttleForce = 30f;        // Scuttle acceleration force
scuttleTime = 0.6f;        // Duration of scuttle reaction
maxHorizontalSpeed = 6f;   // Maximum horizontal speed clamp

// Damage
segmentHealth = 100f;      // Health per leg segment
enableLegDamage = true;    // Enable leg damage system

// Ground Detection
groundLayers = -1;         // Layer mask for ground detection
raycastUp = 1.5f;          // Raycast start offset above position
raycastDown = 3f;          // Raycast distance downward
```

## Modules

The system is composed of modular components that can be enabled/disabled independently:

### LegSolver
Handles inverse kinematics for leg positioning. Supports 1, 2, or 3 bone configurations.

```csharp
legSolver.BoneCount = 3;  // Set bone count
legSolver.SolveIK(leg, targetPosition);
```

### GaitController
Manages stepping patterns and diagonal group alternation for smooth locomotion.

```csharp
gaitController.StepThreshold = 0.4f;
gaitController.UpdateGait(legs, velocity);
```

### TerrainAdapter
Detects surfaces and adjusts foot targets for ground, walls, and ceilings.

```csharp
terrainAdapter.GroundLayers = LayerMask.GetMask("Ground");
Vector3 footTarget = terrainAdapter.FindSurfacePosition(position, direction);
```

### BodyStabilizer
Maintains body height and orientation using physics forces.

```csharp
stabilizer.UprightStrength = 20f;
stabilizer.HeightStrength = 30f;
```

### StepAnimator
Generates smooth parabolic arc trajectories for stepping legs.

```csharp
stepAnimator.StepHeight = 0.1f;
stepAnimator.StepSpeed = 5f;
```

### HitReactor
Responds to collisions with impulse and scuttle reactions.

```csharp
hitReactor.HitImpulse = 6f;
hitReactor.ApplyScuttleReaction(awayDirection);
```

### LegDamageHandler
Tracks per-segment health and handles leg damage/destruction.

```csharp
damageHandler.ApplyDamage(legIndex, segmentIndex, damage);
bool isDamaged = damageHandler.IsSegmentDamaged(legIndex, segmentIndex);
```

## Scripting Examples

### Basic Setup

```csharp
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    private SpiderIKSystem spiderSystem;
    
    void Start()
    {
        spiderSystem = GetComponent<SpiderIKSystem>();
        
        // Configure spider
        var config = new IKConfiguration
        {
            legCount = 4,
            boneCount = 3,
            bodyHeight = 0.4f,
            legLength = 0.8f,
            stepThreshold = 0.4f
        };
        
        spiderSystem.Config = config;
        spiderSystem.RebuildLegData();
    }
}
```

### Applying Damage

```csharp
public class WeaponSystem : MonoBehaviour
{
    public void ShootLeg(SpiderIKSystem spider, int legIndex, int segmentIndex, float damage)
    {
        var damageHandler = spider.GetComponent<LegDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.ApplyDamage(legIndex, segmentIndex, damage);
        }
    }
}
```

### Querying Spider State

```csharp
public class SpiderAI : MonoBehaviour
{
    private SpiderIKSystem spider;
    private LegDamageHandler damageHandler;
    
    void Update()
    {
        // Check if spider is heavily damaged
        int functionalLegs = damageHandler.GetFunctionalLegCount();
        if (functionalLegs < 2)
        {
            // Spider is crippled, flee or die
            FleeOrDie();
        }
        
        // Check specific leg damage
        if (damageHandler.IsLegSignificantlyDamaged(0))
        {
            // Front-left leg is damaged
            AdjustMovement();
        }
    }
}
```

### Saving/Loading Presets

```csharp
public class PresetManager : MonoBehaviour
{
    public void SavePreset(SpiderIKSystem spider, string filename)
    {
        string json = spider.Config.ToJson();
        System.IO.File.WriteAllText(filename, json);
    }
    
    public void LoadPreset(SpiderIKSystem spider, string filename)
    {
        string json = System.IO.File.ReadAllText(filename);
        spider.Config = IKConfiguration.FromJson(json);
        spider.RebuildLegData();
    }
}
```

## Legacy System (v3.1)

The old `SPIDER_IK_LEGS_v3` system is still supported for backward compatibility.

### Migrating to New System

```csharp
var legacySpider = GetComponent<SPIDER_IK_LEGS_v3>();
legacySpider.MigrateToNewSystem();  // Converts to new system
```

### Using Legacy System

```csharp
var spider = GetComponent<SPIDER_IK_LEGS_v3>();
spider.useNewSystem = false;  // Use legacy code
spider.RebuildLegData();
```

## Editor Tools

### SpiderIKSystemEditor

Located in the Inspector when selecting a spider with SpiderIKSystem:

**Presets Section:**
- Quick preset buttons (Spider Walker, Animal Crouch, Hopper, Octopod)
- Save current configuration as preset
- Load preset from file

**Leg Hierarchy Tools:**
- Create Leg Hierarchy - Auto-generates leg structure
- Auto-Assign Leg Transforms - Finds and assigns leg transforms
- Rebuild Leg Data - Recalculates all leg data

**Physics Setup:**
- Setup Physics Shell - Configures Rigidbody and CapsuleCollider
- Add Required Components - Adds all ISpiderModule components

## Performance Tips

1. **Reduce Leg Count** - Use 4 legs instead of 8 for better performance
2. **Reduce Bone Count** - 2-bone legs are faster than 3-bone
3. **Disable Unused Modules** - Set `IsEnabled = false` on modules you don't need
4. **Optimize Raycasting** - Adjust `raycastDistance` and `groundLayers` appropriately
5. **Use Presets** - Pre-configured presets are optimized for different use cases

## Troubleshooting

### Spider Not Moving
- Check if `SpiderIKSystem` is enabled
- Verify `GaitController` is enabled
- Ensure `TerrainAdapter` can detect ground (check layer mask)
- Check if body velocity is being calculated

### Legs Clipping Through Ground
- Increase `raycastUp` and `raycastDown` values
- Verify ground layer mask is correct
- Check `bodyHeight` is appropriate for leg length

### Jerky Movement
- Increase `stepSpeed` for smoother transitions
- Adjust `uprightDamping` and `heightDamping` to reduce oscillation
- Check if frame rate is consistent

### Damage Not Working
- Verify `LegDamageHandler` is enabled
- Check `enableLegDamage` is true in configuration
- Ensure `segmentHealth` is > 0

## Architecture

```
SpiderIKSystem (Orchestrator)
├── LegSolver (IK solving)
├── GaitController (Step management)
├── TerrainAdapter (Surface detection)
├── BodyStabilizer (Physics stabilization)
├── StepAnimator (Step animation)
├── HitReactor (Collision response)
└── LegDamageHandler (Damage system)
```

Each module implements `ISpiderModule` and can be independently enabled/disabled.

## File Structure

```
Assets/_SCRIPTS/_SPIDER/
├── SpiderIKSystem.cs              # Main orchestrator
├── IKConfiguration.cs             # Configuration data
├── LegData.cs                      # Leg state data
├── SurfaceType.cs                  # Surface enum
├── LegSolver.cs                    # IK module
├── GaitController.cs               # Gait module
├── TerrainAdapter.cs               # Terrain module
├── BodyStabilizer.cs               # Stabilization module
├── StepAnimator.cs                 # Animation module
├── HitReactor.cs                   # Hit reaction module
├── LegDamageHandler.cs             # Damage module
├── SPIDER_IK_LEGS_v3.cs            # Legacy wrapper
├── Presets/
│   ├── SpiderWalker.json
│   ├── AnimalCrouch.json
│   ├── Hopper.json
│   └── Octopod.json
└── USAGE.md                        # This file

Assets/_SCRIPTS/Editor/
├── SpiderIKSystemEditor.cs         # Custom editor
├── SpiderGenerators.cs             # Test generators
└── Properties/
    ├── IKSolverProperties.cs
    ├── GaitProperties.cs
    ├── TerrainProperties.cs
    ├── StabilizationProperties.cs
    ├── StepAnimationProperties.cs
    ├── HitReactionProperties.cs
    ├── DamageProperties.cs
    └── SpiderSerializationProperties.cs
```

## Support

For issues or questions:
1. Check the design document: `.kiro/specs/spider-ik-walker/design.md`
2. Review requirements: `.kiro/specs/spider-ik-walker/requirements.md`
3. Run property tests to verify system correctness
4. Check console for debug logs (enable in LegDamageHandler)
