# AFFECT-2025-10-11

## Description

This is a Unity project focused on procedural building generation and advanced spider IK (Inverse Kinematics) locomotion systems. It serves as a prototype or demo for a Sci-Fi horror or cooperative game featuring destructible environments, AI navigation, and complex character mechanics. The project combines procedural level generation with realistic spider-like creature movement and interaction systems.

## Features

- **Procedural Building Generation**: Optimized system for creating buildings with rooms, corridors, walls, doors, and windows using A* pathfinding and spatial partitioning
- **Spider IK System**: Complete rewrite (v3.0) of spider locomotion with 4-leg IK, physics-based movement, hit reactions, and body stabilization
- **Destructible Environments**: Physics-based destruction system with debris, particles, and audio effects
- **Advanced Rendering**: Universal Render Pipeline (URP) with post-processing, lighting, and volume profiles
- **Input System**: Modern Unity Input System with custom actions and UI integration
- **Audio System**: Multiple sound effects for spider mechanics and weapon systems
- **Weapon System**: FPS-style weapon with recoil, muzzle flash, shell ejection, and bullet physics
- **Material Library**: Extensive collection of prototype materials including architectural, concrete, flooring, metal, pavement, and ground textures

## Dependencies

### Unity Version
- Based on URP template (version 17.0.14), compatible with Unity 6000.2.12f1

### Core Packages
- Unity AI (generators, inference, navigation)
- Input System (1.14.2)
- Post Processing Stack V2
- ProBuilder (6.0.8)
- Universal Render Pipeline (17.2.0)
- Timeline (1.8.9)
- Test Framework (1.6.0)

### Asset Packages
- Various free material packs (Gridbox, iPoly3D, Scalable Grid, Yughues architectural materials)

## Installation and Build Instructions

### Prerequisites
- Unity 2022+ with URP support

### Setup
1. Clone or download the project.
2. Open the project in Unity Hub.
3. Ensure all packages resolve via `Packages/manifest.json`.

### Build Settings
- **Target Platforms**: Standalone, Android, iOS, WebGL
- **Graphics API**:
  - Vulkan (Android)
  - Metal (iOS)
  - DirectX/OpenGL (Standalone)
- **Scripting Backend**: IL2CPP (recommended for performance)
- **API Compatibility**: .NET Standard 2.1

### Build Process
1. Open the `LEVEL_1.unity` scene.
2. Navigate to File → Build Settings.
3. Select the desired target platform.
4. Click Build and Run.

## Project Structure

- **Assets/**: Main project assets
  - `_SCRIPTS`: Core mechanics (e.g., `SPIDER_IK_LEGS_v3.cs`, supporting classes)
  - `_MATERIALS`: Material assets and shaders
  - `_PREFABS`: Reusable objects (player, spider parts, building elements)
  - `_SOUNDS`: Audio clips for effects
  - `_SETTINGS`: URP assets, input actions, lighting
  - `_PLUGINS`: Third-party material packages
  - `_DOCS`: Documentation for building generator and architecture
- **Packages/**: Unity package configurations
- **ProjectSettings/**: Unity project settings
- **Library/**: Generated library files
- **Scenes**: `LEVEL_1.unity` (main demo scene with spider, player, and generated building)

## Additional Information

### Performance Optimizations
- Object pooling
- Spatial partitioning
- Time-limited generation

### Physics
- Rigidbody-based movement with custom stabilizers and collision detection

### Rendering
- HDR support
- Dynamic resolution
- Post-processing effects

### Input
- Mouse/keyboard FPS controls with customizable sensitivity

### Quality Settings
- Configurable for different platforms (PC/Mobile renderers)

### Version Control
- Plastic SCM integration

### CI/CD
- Qodana for code quality
- GitHub Actions setup

### Technical Notes
- Uses `ExecuteAlways` for editor-time IK solving
- Implements custom editor tools for spider creation and building generation
- Supports multiple mission types (Exploration, Rescue, Extraction, Survival, Combat, Stealth)
- Includes debug tools and profiling capabilities
- Compatible with Unity Collaborate and Unity Connect