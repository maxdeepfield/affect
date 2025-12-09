# AFFECT-2025-10-11 Repository Documentation

## Project Overview

**AFFECT-2025-10-11** is a Unity 6000.2.14f1 game project featuring procedural building generation and advanced spider IK (Inverse Kinematics) locomotion systems. It serves as a prototype for a Sci-Fi horror/cooperative game with destructible environments, AI navigation, and complex character mechanics.

## Key Features

- **Procedural Building Generation**: Optimized level generation with rooms, corridors, walls, doors, windows using A* pathfinding and spatial partitioning
- **Spider IK System v3.0**: Complete 4-leg IK system with physics-based movement, hit reactions, and body stabilization
- **Destructible Environments**: Physics-based destruction with debris, particles, and audio effects
- **Advanced Rendering**: Universal Render Pipeline (URP 17.2.0) with HDR, dynamic resolution, and post-processing
- **Input System**: Modern Unity Input System (1.16.0) with FPS controls
- **Audio System**: Comprehensive sound effects for spider mechanics and weapons
- **Weapon System**: FPS-style weapon with recoil, muzzle flash, shell ejection, and ballistics
- **Extensive Material Library**: Gridbox, iPoly3D, Scalable Grid, Yughues architectural/concrete/flooring/metal materials

## Project Structure

```
Assets/
├── _SCRIPTS/                       # Core C# gameplay code
│   ├── _LEVELGEN/                 # Procedural level generation
│   │   ├── OptimizedBuildingGenerator.cs
│   │   ├── FloorData.cs
│   │   ├── RoomNode.cs
│   │   ├── SpatialGrid.cs
│   │   ├── ThemeManager.cs
│   │   └── ObjectPoolManager.cs
│   ├── _SPIDER/                   # Spider IK and locomotion
│   │   ├── SPIDER_IK_LEGS_v3.cs
│   │   ├── LegSolver.cs
│   │   ├── GaitController.cs
│   │   ├── BodyStabilizer.cs
│   │   ├── HitReactor.cs
│   │   ├── LegDamageHandler.cs
│   │   ├── IKConfiguration.cs
│   │   └── AbsoluteSpiderFreakout.cs
│   ├── _PLAYER/                   # FPS player mechanics
│   │   ├── FPSController.cs
│   │   ├── PlayerInputHandler.cs
│   │   ├── PlayerMovement.cs
│   │   ├── MouseLook.cs
│   │   ├── WeaponController.cs
│   │   ├── BulletController.cs
│   │   ├── FlashLightController.cs
│   │   ├── Health.cs
│   │   ├── ReticleFeedback.cs
│   │   ├── Recoil/              # Weapon recoil system
│   │   │   ├── RecoilSystem.cs
│   │   │   ├── RecoilConfiguration.cs
│   │   │   ├── CameraShaker.cs
│   │   │   ├── RecoilRandomizer.cs
│   │   │   └── MouseTracker.cs
│   │   └── Audio/               # Audio systems
│   │       ├── SoundManager.cs
│   │       ├── WeaponSounds.cs
│   │       └── FootstepSounds.cs
│   ├── Editor/                    # Editor tools
│   │   ├── OptimizedBuildingGeneratorEditor.cs
│   │   ├── AbsoluteSpiderFreakoutGenerator.cs
│   │   └── VehicleGeneratorEditor.cs
│   └── VehicleGenerator.cs
│
├── _LEVEL/                         # Scene files
│   └── LEVEL_1.unity              # Main demo scene
│
├── _PREFABS/                      # Reusable prefabs
│   ├── _LEVEL.prefab
│   ├── _LEVEL 1.prefab
│   ├── _PLAYER.prefab
│   ├── _PLAYER 1.prefab
│   ├── Absolute Spider Freakout.prefab
│   ├── Weapon.prefab
│   ├── Muzzle Particle.prefab
│   ├── Impact Particle.prefab
│   ├── Muzzle Light.prefab
│   ├── Bullet Shell.prefab
│   └── Hole Decal.prefab
│
├── _MATERIALS/                    # Material assets
│   ├── Black.mat
│   ├── Hole Material.mat
│   └── NewHoleMaterial.mat
│
├── _PLUGINS/                      # Third-party asset packages
│   ├── Gridbox Prototype Materials/
│   ├── iPoly3D/
│   ├── Scalable Grid Prototype Materials/
│   ├── Thirdparty/
│   ├── YughuesFreeConcreteMaterials/
│   ├── YughuesFreeArchitecturalMaterials/
│   ├── YughuesFreeFlooringMaterials/
│   ├── YughuesFreeGroundMaterials/
│   ├── YughuesFreeMetalMaterials/
│   └── YughuesFreePavementsMaterials/
│
├── _SOUNDS/                       # Audio clips
│   ├── metallic_spider_robo_#1.mp3
│   ├── metallic_spider_robo_#2.mp3
│   ├── metallic_spider_robo.mp3
│   ├── rifle_silenced_shoot_#1.mp3
│   ├── rifle_silenced_shoot.mp3
│   └── New Audio Clip.wav
│
├── _SETTINGS/                     # URP and project settings
│   ├── DefaultVolumeProfile.asset
│   ├── InputSystem_Actions.inputactions
│   ├── Mobile_Renderer.asset
│   ├── PC_Renderer.asset
│   ├── UniversalRenderPipelineGlobalSettings.asset
│   └── New Lighting Settings.lighting
│
├── _ARCH/                         # Architecture prefabs
│   ├── _DOOR.prefab
│   ├── _FLOOR.prefab
│   ├── _WALL.prefab
│   └── _WINDOW.prefab
│
├── _DOCS/                         # Documentation
│   ├── BUILDING_V2_ARCHITECTURE.md
│   ├── DOOR_LOGIC_ANALYSIS.md
│   ├── README_BuildingGenerator.md
│   └── Various analysis documents
│
├── _Recovery/                     # Backup scenes
│   └── (Various .unity files)
│
└── .kiro/                         # Project specs
    └── specs/
        └── spider-ik-fix/
```

## Technology Stack

### Game Engine
- **Unity**: 6000.2.14f1
- **Scripting**: C# (.NET Standard 2.1)
- **Rendering**: Universal Render Pipeline (URP 17.2.0)

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| com.unity.render-pipelines.universal | 17.2.0 | Modern graphics rendering |
| com.unity.inputsystem | 1.16.0 | Input handling |
| com.unity.postprocessing | 3.5.1 | Post-processing effects |
| com.unity.probuilder | 6.0.8 | Level building tools |
| com.unity.timeline | 1.8.9 | Animation/cutscene sequencing |
| com.unity.test-framework | 1.6.0 | Unit testing |
| com.unity.ai.navigation | 2.0.9 | AI pathfinding |
| com.unity.ai.inference | 2.4.1 | Machine learning inference |
| com.unity.ai.generators | 1.0.0-pre.20 | AI-assisted generation |
| com.unity.collab-proxy | 2.10.2 | Version control integration |

### Build Platforms
- Standalone (Windows, Mac, Linux)
- Android (Vulkan)
- iOS (Metal)
- WebGL
- **Scripting Backend**: IL2CPP
- **Graphics API**: DirectX/OpenGL (Standalone), Vulkan (Android), Metal (iOS)

## Core Systems

### 1. Spider IK System (Assets/_SCRIPTS/_SPIDER/)
- **SPIDER_IK_LEGS_v3.cs**: Main 4-leg inverse kinematics solver
- **LegSolver.cs**: Individual leg IK calculations
- **GaitController.cs**: Leg stepping patterns and locomotion
- **BodyStabilizer.cs**: Physics-based body balance and stabilization
- **HitReactor.cs**: Damage response and animation feedback
- **LegDamageHandler.cs**: Leg health and destruction mechanics
- **IKConfiguration.cs**: IK solver parameters and constraints

### 2. Level Generation (Assets/_SCRIPTS/_LEVELGEN/)
- **OptimizedBuildingGenerator.cs**: Main procedural generation engine
- **RoomNode.cs**: Room data structures
- **SpatialGrid.cs**: Spatial partitioning for efficient queries
- **FloorData.cs**: Floor configuration and layout
- **ThemeManager.cs**: Visual themes and material selection
- **ObjectPoolManager.cs**: Object pooling for performance

### 3. Player Systems (Assets/_SCRIPTS/_PLAYER/)
- **FPSController.cs**: Main player controller
- **PlayerInputHandler.cs**: Input processing
- **PlayerMovement.cs**: Character movement mechanics
- **WeaponController.cs**: Weapon handling and firing
- **RecoilSystem.cs**: Weapon recoil simulation
- **BulletController.cs**: Projectile physics
- **Health.cs**: Player health management
- **SoundManager.cs**: Audio playback system

## Main Scene

- **LEVEL_1.unity**: Demonstration scene featuring:
  - Procedurally generated building
  - Playable spider creature
  - FPS player character
  - Destructible environment
  - Complete gameplay mechanics demo

## Build Configuration

### Project Settings
- **API Compatibility**: .NET Standard 2.1
- **Graphics**: HDR enabled, dynamic resolution support
- **Physics**: 3D Rigidbody with custom constraints
- **Input**: Modern Input System with customizable actions
- **Quality**: PC/Mobile renderer profiles

### Performance Optimizations
- Object pooling system
- Spatial grid partitioning
- Time-limited procedural generation
- Streaming/unloading of distant objects

## Development Tools

### Editor Extensions
- **OptimizedBuildingGeneratorEditor.cs**: Building generation UI
- **AbsoluteSpiderFreakoutGenerator.cs**: Spider creation tools
- **VehicleGeneratorEditor.cs**: Vehicle prefab generation

### Testing
- **Test Framework**: 1.6.0
- **Test Results**: Multiple XML files documenting test runs
  - TestResults_Spider*.xml
  - TestResults_IKSystem.xml
  - TestResults_Terrain.xml
  - etc.

### Version Control
- **System**: Plastic SCM (configured in .plastic/)
- **IDE Integration**: Rider (3.0.38), Visual Studio (2.0.25)

### Code Quality
- **Qodana**: Static analysis configuration (qodana.yaml)

## Documentation

### Internal Documentation
- `Assets/_DOCS/BUILDING_V2_ARCHITECTURE.md`: Building generation architecture
- `Assets/_DOCS/DOOR_LOGIC_ANALYSIS.md`: Door system analysis
- `Assets/_DOCS/README_BuildingGenerator.md`: Building generator guide
- `Assets/_DOCS/SCENE_VIEW_TESTING.md`: Scene testing procedures
- `Assets/.kiro/specs/spider-ik-fix/`: Spider IK improvement specifications

### External References
- README.md: Project overview
- LICENSE: Project licensing

## Getting Started

### Prerequisites
- Unity 2022+
- IL2CPP build tools (for target platform)
- 20+ GB disk space (includes Library, Packages, and material assets)

### Setup Steps
1. Clone or download the repository
2. Open in Unity Hub (version 6000.2.14f1 or compatible)
3. Allow package resolution via Packages/manifest.json
4. Open LEVEL_1.unity scene
5. Press Play in editor to test

### Build Instructions
1. File → Build Settings
2. Select target platform
3. Ensure LEVEL_1.unity is added to scenes
4. Configure quality settings (PC_Renderer or Mobile_Renderer)
5. Click Build and Run

## Notable Game Mechanics

### Spider Locomotion
- 4-leg IK system with procedural gait generation
- Physics-based body stabilization
- Damage system affecting leg functionality
- Hit reaction animations

### Destructible Environment
- Physics-based destruction particles
- Projectile impact decals
- Debris physics simulation
- Audio feedback on destruction

### Weapon System
- Recoil simulation with camera shake
- Muzzle flash particles
- Bullet shell ejection
- Hit impact feedback

### Level Generation
- Procedural room layout
- Automatic door/window placement
- Material theme system
- Performance-optimized object pooling

## File Statistics

- **C# Scripts**: 66+ files
- **Scenes**: 4 (1 main + 3 recovery)
- **Prefabs**: 50+ instances
- **Materials**: Extensive library with 8+ material packs
- **Audio Clips**: 6+ SFX files
- **Documentation**: 9+ MD files

## License

See LICENSE file for project licensing terms.

## Notes

- Project uses ExecuteAlways for editor-time IK solving
- Includes custom editor tools for rapid prototyping
- Supports multiple mission types: Exploration, Rescue, Extraction, Survival, Combat, Stealth
- Full debugging and profiling capabilities included
- Compatible with Unity Collaborate and Unity Connect
