# Pause Menu System Setup

## What It Does
- Press **ESC** to pause/unpause
- Game freezes completely (Time.timeScale = 0)
- Pause menu appears with Resume, Settings, and Quit options
- Press ESC again or click Resume to unpause

## Setup Steps

### Step 1: Add PauseManager to Your Scene
1. Create an empty GameObject
2. Name it `PauseManager`
3. Add the `PauseManager` component
4. Leave all fields empty (auto-initializes)

### Step 2: Create Pause Menu UI

In your Canvas, create this structure:

```
Canvas
└── PauseMenuPanel (Panel)
    ├── Title (Text - "PAUSED")
    ├── ResumeButton (Button)
    │   └── Text: "Resume"
    ├── SettingsButton (Button)
    │   └── Text: "Settings"
    └── QuitButton (Button)
        └── Text: "Quit"
```

#### Quick Setup:
1. Right-click Canvas → UI → Panel (this becomes PauseMenuPanel)
2. Set position/size to fill screen (or center it)
3. Right-click Panel → UI → Text → TextMeshPro (for title)
4. Right-click Panel → UI → Button → TextMeshPro (Repeat 3x for each button)

### Step 3: Assign Buttons to Script

1. Select **PauseMenuPanel**
2. Add the `PauseMenuUI` component to it
3. In Inspector, drag and drop:
   - ResumeButton → Resume Button slot
   - SettingsButton → Settings Button slot
   - QuitButton → Quit Button slot
   - (Optional) SettingsPanel → Settings Panel slot

### Step 4: Test!

1. Press Play
2. Press **ESC** → Menu appears, game freezes
3. Press **ESC** again → Resumes
4. Or click **Resume** button

## Optional: Link Graphics Settings Panel

If you want the Settings button to open your graphics settings:

1. In your Canvas, find your GraphicsSettingsPanel
2. In PauseMenuUI Inspector, assign it to the **Settings Panel** slot
3. Now clicking Settings toggles the graphics panel!

## Code Usage

If you need to pause/resume from code:

```csharp
// Pause
PauseManager.Instance.Pause();

// Resume
PauseManager.Instance.Resume();

// Check if paused
if (PauseManager.Instance.IsPaused)
{
    // Do something
}

// Toggle
PauseManager.Instance.TogglePause();
```

## Important Notes

⚠️ **Time.timeScale = 0 affects:**
- Physics (will freeze)
- Animations (will freeze)
- Coroutines using WaitForSeconds (will freeze)
- Particle systems (will freeze)

⚠️ **Time.timeScale = 0 DOES NOT affect:**
- UI animations (use `Canvas.renderMode = ScreenSpaceOverlay`)
- Update() that runs (but most things check timeScale)
- Real time operations

## Troubleshooting

**Menu doesn't appear?**
- Make sure PauseMenuPanel is a child of Canvas
- Check that PauseMenuUI is attached to the panel
- Verify buttons are assigned in Inspector

**Game doesn't freeze?**
- Check that Time.timeScale actually changes (debug log shows it)
- Some physics might need special handling

**UI buttons stuck/unresponsive?**
- EventSystem requires UI to be rendered on top
- Make sure PauseMenuPanel renders last (higher in hierarchy)
- Try: Canvas → Render Mode = Screen Space - Overlay

**ESC key not working?**
- Some games override input - check PlayerInputHandler
- ESC might be consumed by another system

---

**That's it!** Press ESC to pause, game freezes completely. 🎮
