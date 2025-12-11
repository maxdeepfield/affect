# 🕷️ AFFECT - First-Person Horror Shooter Wiki

> **Unity 6000.3.0f1** | **URP Pipeline** | **New Input System**

A first-person horror shooter featuring procedural building generation, spider-like enemies with IK locomotion, and a sophisticated weapon/recoil system.

---

## 📁 Project Architecture

```
Assets/
├── _CODE/           # All C# scripts
│   ├── _PLAYER/     # Player systems (FPS, weapons, health, inventory)
│   │   ├── Audio/   # Sound systems (footsteps, weapons)
│   │   └── Recoil/  # Modular recoil system
│   ├── _SPIDER/     # Enemy AI and IK locomotion
│   ├── _UI/         # HUD, pause menu, graphics settings
│   └── Editor/      # Custom Unity editors
├── _LEVEL/          # Scene files
├── _PREFABS/        # Player, enemies, pickups, level prefabs
├── _MATERIALS/      # Materials and shaders
├── _SOUNDS/         # Audio clips (weapons, spider sounds)
└── _SETTINGS/       # Render pipeline settings
```

---

## 🎮 Core Systems

### Player Controller (`FPSController.cs`)
Wrapper component ensuring all player systems are attached:
- `PlayerInputHandler` - New Input System bindings
- `PlayerMovement` - CharacterController-based movement
- `MouseLook` - Camera rotation with recoil integration
- `WeaponController` - Shooting, aiming, effects

### Controls (New Input System)
| Action | Key |
|--------|-----|
| Move | WASD |
| Look | Mouse |
| Jump | Space |
| Shoot | Left Mouse |
| Aim (Toggle) | Right Mouse |
| Reload | R |
| Use/Interact | F |
| Sprint | Shift |
| Crouch | C / Ctrl |
| Pause | Escape |

---

## 🏃 Player Movement (`PlayerMovement.cs`)

**Features:**
- Walk/Run/Crouch speeds (5 / 8 / 2.5 m/s)
- Jump with gravity (-9.81)
- Crouch height adjustment with ceiling detection
- Footstep audio integration

**Key Properties:**
```csharp
moveSpeed = 5f
runSpeed = 8f
crouchSpeed = 2.5f
crouchHeight = 1.2f
jumpForce = 5f
```

---

## 🔫 Weapon System

### WeaponController.cs
Full-featured FPS weapon with:
- **Fire Modes:** Semi-automatic / Full-automatic
- **Hitscan Shooting:** Raycast-based with damage, impact force
- **Visual Effects:** Muzzle flash, shell ejection, bullet holes, impact particles
- **Aiming:** Toggle ADS with FOV zoom (60° → 30°), sensitivity reduction
- **Weapon Sway & Bobbing:** Movement-based weapon animation

**Key Settings:**
```csharp
fireRate = 0.5f
maxRange = 200f
damage = 20f
impactForce = 50f
normalFOV = 60f
aimFOV = 30f
```

### WeaponAmmo.cs
Magazine + reserve ammo system:
- Magazine size tracking
- Auto-reload on empty
- Infinite ammo options
- Event-driven UI updates

### Recoil System (Modular Architecture)

Located in `_CODE/_PLAYER/Recoil/`:

| Component | Purpose |
|-----------|---------|
| `RecoilSystem.cs` | Main orchestrator |
| `RecoilConfiguration.cs` | Serializable settings |
| `RecoilRandomizer.cs` | Perlin noise variation |
| `MouseTracker.cs` | Player compensation detection |
| `CameraShaker.cs` | Screen shake effects |
| `RecoilState.cs` | Runtime state tracking |
| `IRecoilModule.cs` | Module interface |

**Recoil Parameters:**
```csharp
baseVerticalKick = 2f      // 0.5-5° per shot
baseHorizontalKick = 0.5f  // ±2° spread
weaponKickbackDistance = 0.05f
recoverySpeed = 8f
shakeIntensity = 0.01f
```

---

## ❤️ Health & Pickups

### Health.cs
Universal health component for player and enemies:
- Damage/heal with events
- Death handling with object destruction
- `HealthChanged` event for UI binding

### Pickups
| Prefab | Script | Effect |
|--------|--------|--------|
| Medkit | `Medkit.cs` | Heals 50 HP |
| Ammo | `AmmoPickup.cs` | Adds 30 reserve ammo |

---

## 🔑 SINGLE UPGRADABLE KEYCARD SYSTEM

**Core Concept:** Player has ONE keycard that upgrades through progression. Not inventory items - a system object that transforms the world.

### PlayerInventory.cs
Singleton holding the single keycard:
```csharp
int KeycardLevel          // Current level (0 = none)
void UpgradeKeycard(int)  // Upgrade to new level
bool HasKeycard(int)      // Check if level >= required
```

### Keycard.cs (Pickup)
Legacy pickup that calls `UpgradeKeycard()` - use `KeycardUpgradeTerminal` instead.

### KeycardUpgradeTerminal.cs
Terminal at end of each building:
- Player uses terminal after completing building
- Upgrades keycard to next level
- Triggers `KeycardWorldSystem` to spawn next building
- One-time use with visual feedback

### KeycardWorldSystem.cs
**THE MAGIC:** When keycard upgrades, a NEW building descends from the sky!

```
Player completes Building 1
    ↓
Uses upgrade terminal
    ↓
Keycard: Level 0 → Level 1
    ↓
Player looks out window...
    ↓
Building 2 SLOWLY FALLS FROM SKY
    ↓
Assembles floor by floor
    ↓
Player has NEVER seen this building
```

**Features:**
- Building prefabs indexed by keycard level
- Animated descent from configurable height
- Floor-by-floor assembly with sound
- Events for cinematics/UI

### BuildingRules.cs
Defines how buildings change per level:

| Level | Floors | Enemies | Features |
|-------|--------|---------|----------|
| 0 | 1 | None | Tutorial, bright, safe |
| 1 | 2 | 1-2 spiders | Elevator introduced |
| 2 | 2-3 | 2-4 spiders | Locked doors, aggressive |
| 3 | 3-4 | 3-6 spiders | Complex layout |
| 4 | 4-5 | 4-8 spiders | Labyrinth, flickering lights |
| 5 | 5-6 | 5-10 + BOSS | Nightmare mode |
| N+ | Scales | Endless | Procedural difficulty |

### KeycardDoor.cs
Doors requiring minimum keycard level:
- Shows required level in prompt
- Visual indicator (red/green light)
- Unlocks automatically when keycard upgrades
- Events for unlock feedback

### KeycardDisplay.cs (UI)
Visual keycard display:
- Level number with color coding
- Punch animation on upgrade
- Glow pulse effect
- Color per level (gray→green→blue→purple→gold→red)

---

## 🏆 WEEKLY STAGE MODE (Separate Competitive Mode)

**NOT a gate for main progression!** Weekly Stage is a separate online competitive mode.

### Two Game Modes

| Mode | Description |
|------|-------------|
| **AFFECT/RUN** | Main roguelite, keycard progression, no time gates |
| **WEEKLY STAGE** | Fixed seed, same building for everyone, leaderboard |

### WeeklyStageSystem.cs
Manages the weekly competitive mode:

```csharp
// Everyone plays same seed this week
int CurrentSeed      // Deterministic from year + week
string WeeklyId      // "2025-W03"
int FixedKeycardLevel // Same for all players (fair competition)

// Hand-picked interesting seeds
int[] interestingSeeds = { 48291573, 73629184, ... }

// Scoring
Score = (Floors × 1000) + (Kills × 50) + (Secrets × 200)
      - (Minutes × 20) - (Damage × 5)
```

### WeeklyModifier
Optional weekly variety:
- Enemy health/damage/speed multipliers
- Extra spiders
- Darkness mode
- No minimap
- Extra floors

### WeeklyLeaderboard.cs
Online leaderboard service:
- Fetch/submit scores
- Local fallback when offline
- Pluggable backend (Steam, PlayFab, custom)

### WeeklyStageUI.cs
UI showing:
- Current week: `WEEKLY STAGE — 2025-W03`
- Seed: `SEED: 48291573`
- Countdown: `NEXT SEED IN 2d 14h`
- Score breakdown
- Leaderboard

### Player Experience

```
MAIN MENU
├── AFFECT/RUN → Normal progression, no limits
└── WEEKLY STAGE → Competitive mode
    ├── Same building for everyone
    ├── Fixed keycard level
    ├── Score = floors + kills - time - damage
    └── Online leaderboard
```

### Marketing Angle
> "Weekly seeded challenge mode with online leaderboards.
> Everyone plays the same building each week — compete on the same seed."

---

## 🕷️ Spider Enemy System

### SpiderEnemyController.cs
NavMeshAgent-based AI with state machine:

**States:**
1. **Idle** - Hold position
2. **Patrol** - Waypoint or wander movement
3. **Chase** - Pursue player
4. **Attack** - Shoot projectiles at player
5. **Retreat** - Find cover when low health

**Detection:**
- Detection radius: 14m
- Lose sight radius: 18m
- Field of view: 140°
- Line-of-sight raycasting

**Combat:**
- Attack range: 9m
- Shooting cooldown: 0.9s
- Projectile speed: 22 m/s
- Low health retreat at 35%

### AbsoluteSpiderFreakout.cs
Procedural IK locomotion system:

**Features:**
- 4-leg alternating gait (diagonal groups)
- Ground/wall surface detection
- Arc-based stepping animation
- Body height spring physics
- Shot impulse reactions

**Leg Hierarchy:**
```
Leg_XX/
├── Hip
│   └── Knee
│       └── Ankle
│           └── Foot
```

**Key Parameters:**
```csharp
maxLegLength = 1.1f
stepDistance = 0.55f
stepHeight = 0.18f
stepSpeed = 4.5f
bodyHeight = 0.6f
```

### LegData.cs
Per-leg runtime state:
- Joint references (hip, knee, ankle, foot)
- Segment health/damage tracking
- Step progress and timing
- Diagonal group assignment

### LegConnectorV3.cs
Visual connector between joints:
- Cylinder scaling between transforms
- Damage color visualization
- Runtime segment visibility

---

## 🏢 Procedural Generation

### BuildingGenerator.cs
Grid-based building layout generator:

**Layout Elements:**
- Main corridor (center)
- Cross corridor
- Halls at intersections
- Corridor branches
- Rooms adjacent to corridors

**Features:**
- Seed-based generation
- Multi-floor support with elevator
- Automatic door placement ensuring reachability
- Perimeter windows
- Wall/door/floor prefab instantiation

**Grid Cell Types:**
```csharp
enum CellType { Empty, Wall, Corridor, Room, Hall, Entrance, Door }
```

**Parameters:**
```csharp
buildingWidth = 20
buildingDepth = 20
floorsCount = 1
cellSize = 3f
mainCorridorWidth = 2
minRoomSize = 2
maxRoomSize = 5
```

### ProceduralTerrainGenerator.cs
Noise-based terrain with grass:

**Terrain:**
- Configurable size and resolution
- Multi-layer Perlin noise
- Height curve remapping
- Mesh collider generation

**Grass Foliage:**
- GPU instanced rendering
- Slope/height filtering
- Color variation
- Wind animation support

---

## 🛗 Elevator System

### ElevatorController.cs
Multi-floor elevator:
- Smooth vertical movement
- Dynamic button generation
- Floor height configuration
- Integration with building generator

### ElevatorButton.cs
Usable button for floor selection:
- Hooks into `Usable` system
- Per-floor prompt text

---

## 🎨 UI Systems

### PlayerHUD.cs
Main HUD displaying:
- Health (HP current/max)
- Ammo (magazine/reserve)
- Collected keycards
- Event-driven updates

### UsePromptUI.cs
Interaction prompt display:
- Shows current `Usable` object prompt
- "Press F" style interaction hints

### PauseMenuUI.cs
Pause menu with:
- Resume button
- Settings panel toggle
- Quit button

### GraphicsSettingsUI.cs
Settings panel:
- Quality presets (Low/Medium/High/Ultra)
- Resolution selection
- VSync toggle
- Target framerate slider
- Shadows toggle
- Master volume

### ReticleFeedback.cs
Hit marker system:
- Flash on hit
- Kill confirmation indicator

---

## ⚙️ Core Utilities

### PauseManager.cs
Singleton pause controller:
- ESC key toggle
- Time.timeScale freeze
- Cursor lock management

### Usable.cs
Interaction system:
- Raycast-based detection
- Distance checking
- UnityEvent callbacks
- Static current target tracking

### GraphicsSettings.cs
Persistent settings manager:
- PlayerPrefs serialization
- Quality level application
- Resolution/VSync/framerate control

### SoundManager.cs
Audio pooling system:
- Pre-allocated AudioSource pool
- Sound library with ID lookup
- 3D positioned playback
- Pitch/volume variation

---

## 🔊 Audio Systems

### FootstepSounds.cs
Movement audio:
- Velocity-based step timing
- Random clip selection (no repeats)
- Jump/land sounds
- Pitch variation

### WeaponSounds.cs
Weapon audio:
- Fire sounds with variation
- Reload sound
- Empty click
- Shell casing drops

---

## 🛠️ Editor Tools

### BuildingGeneratorEditor.cs
Custom inspector with "Generate" button for building creation.

### AbsoluteSpiderFreakoutGenerator.cs
Menu item (`GameObject > Spider IK > Create Absolute Spider Freakout`):
- Creates complete spider prefab
- Sets up rigidbody, collider, legs
- Configures IK system

### RecoilGenerators.cs
Test data generators for recoil system property testing.

---

## 📦 Prefabs

| Prefab | Description |
|--------|-------------|
| `_PLAYER.prefab` | Complete player setup |
| `_LEVEL.prefab` | Level container |
| `Absolute Spider Freakout.prefab` | Spider enemy |
| `Ammo.prefab` | Ammo pickup |
| `Medkit.prefab` | Health pickup |
| `Wall.prefab` | Building wall segment |
| `Window.prefab` | Building window |
| `Weapon.prefab` | Player weapon |
| `Bullet Shell.prefab` | Ejected casing |
| `Hole Decal.prefab` | Bullet hole |
| `Impact Particle.prefab` | Hit effect |
| `Muzzle Light.prefab` | Muzzle flash light |
| `Muzzle Particle.prefab` | Muzzle flash particles |

---

## 🎯 Quick Start

1. Open `Assets/_LEVEL/LEVEL_1.unity`
2. Ensure player prefab has all required components
3. Add `BuildingGenerator` to scene, configure, click "Generate"
4. Place spider enemies with patrol points
5. Add pickups (Ammo, Medkit, Keycard) with `Usable` components
6. Play!

---

## 🔧 Key Dependencies

- **Unity 6000.3.0f1** (Unity 6)
- **Universal Render Pipeline (URP)**
- **New Input System**
- **TextMeshPro**
- **NavMesh** (for spider AI)

---

## 📝 Code Conventions

- Singleton pattern: `Instance` property with null checks
- Events: C# events or UnityEvents for decoupling
- SerializeField for inspector exposure
- RequireComponent for dependencies
- ExecuteAlways for editor preview (terrain, spider IK)
