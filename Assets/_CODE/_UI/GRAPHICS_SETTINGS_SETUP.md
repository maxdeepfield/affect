# Graphics Settings System Setup Guide

## Overview
The graphics settings system allows players to control quality, resolution, framerate, shadows, and audio volume. Settings are automatically saved and restored on game restart.

## Components

### 1. GraphicsSettings.cs (Manager)
- Core system that manages all graphics configurations
- Singleton pattern (auto-creates if missing)
- Saves/loads settings from PlayerPrefs
- Auto-applies settings on startup

### 2. GraphicsSettingsUI.cs (UI Controller)
- Connects UI elements to the settings manager
- Handles all user inputs
- Real-time feedback for sliders

## Setup Steps

### Step 1: Add GraphicsSettings to Your Scene
1. Create an empty GameObject in your main scene
2. Name it `GraphicsSettingsManager`
3. Add the `GraphicsSettings` component to it
4. Mark it as DontDestroyOnLoad (optional, already handled in code)

**Or:** Let it auto-create - just have the UI trigger it!

### Step 2: Create UI Canvas (if you don't have one)
1. Right-click in Hierarchy → UI → Panel
2. This creates a Canvas with a Panel

### Step 3: Add UI Elements to Your Canvas
You need these UI elements as children of your Canvas:

```
Canvas
├── GraphicsSettingsPanel (Panel) ← Add GraphicsSettingsUI here
│   ├── QualityDropdown (Dropdown)
│   ├── ResolutionDropdown (Dropdown)
│   ├── VsyncToggle (Toggle)
│   ├── FramerateSlider (Slider)
│   ├── FramerateText (TextMeshProUGUI)
│   ├── ShadowsToggle (Toggle)
│   ├── VolumeSlider (Slider)
│   ├── VolumeText (TextMeshProUGUI)
│   ├── ApplyButton (Button)
│   └── CloseButton (Button)
```

### Step 4: Create UI Layout (Quick Template)

**In your Canvas:**

```
Right-click → UI → Panel → Text Input Field (this creates a Canvas if needed)
```

Then add these as children of the panel:

1. **Quality Dropdown**
   - Right-click Panel → UI → Dropdown
   - Name it `QualityDropdown`

2. **Resolution Dropdown**
   - Right-click Panel → UI → Dropdown
   - Name it `ResolutionDropdown`

3. **VSync Toggle**
   - Right-click Panel → UI → Toggle
   - Name it `VsyncToggle`

4. **Framerate Slider**
   - Right-click Panel → UI → Slider
   - Name it `FramerateSlider`
   - Drag a TextMeshPro text as sibling, name it `FramerateText`

5. **Shadows Toggle**
   - Right-click Panel → UI → Toggle
   - Name it `ShadowsToggle`

6. **Volume Slider**
   - Right-click Panel → UI → Slider
   - Name it `VolumeSlider`
   - Drag a TextMeshPro text as sibling, name it `VolumeText`

7. **Apply Button**
   - Right-click Panel → UI → Button
   - Name it `ApplyButton`
   - Add text child "Apply"

8. **Close Button**
   - Right-click Panel → UI → Button
   - Name it `CloseButton`
   - Add text child "Close"

### Step 5: Wire Up the UI Script

1. Select the `GraphicsSettingsPanel` (the Panel with GraphicsSettingsUI)
2. Add the `GraphicsSettingsUI` component
3. In the Inspector, drag the corresponding UI elements into each slot:
   - Quality Dropdown → `QualityDropdown`
   - Resolution Dropdown → `ResolutionDropdown`
   - VSync Toggle → `VsyncToggle`
   - Framerate Slider → `FramerateSlider`
   - Framerate Text → `FramerateText`
   - Shadows Toggle → `ShadowsToggle`
   - Volume Slider → `VolumeSlider`
   - Volume Text → `VolumeText`
   - Apply Button → `ApplyButton`
   - Close Button → `CloseButton`

### Step 6: Test

1. Press Play
2. Change settings
3. Click Apply (settings are saved to PlayerPrefs)
4. Restart game → settings should persist!

## Features

### Quality Levels
- **Low** - Minimal shadows, low quality
- **Medium** - Balanced performance/visuals
- **High** - Better visuals
- **Ultra** - Maximum quality (if GPU supports it)

### Resolution
- Auto-detects available resolutions
- Can switch fullscreen/windowed

### Framerate Control
- Range: 30-240 FPS
- Useful for power saving on laptops

### Shadows
- Toggle all shadows on/off
- Distance is tied to quality level

### Audio Volume
- Master volume slider (0-100%)
- Affects all audio listeners

## Accessing Settings in Code

```csharp
// Get the instance
GraphicsSettings gs = GraphicsSettings.Instance;

// Change quality
gs.SetQualityLevel(2); // High

// Change framerate
gs.SetTargetFramerate(60);

// Mute audio
gs.SetMasterVolume(0f);

// Save all changes
gs.SaveSettings();

// Get current values
int fps = gs.GetTargetFramerate();
float volume = gs.GetMasterVolume();
```

## Troubleshooting

**Settings not saving?**
- Make sure `ApplyButton` is wired and clicked
- Check PlayerPrefs in Project Settings → Player

**Dropdowns empty?**
- Ensure GraphicsSettings.Instance initialized first
- GraphicsSettingsUI needs to be in Start() after GraphicsSettings

**UI buttons not responding?**
- Check that Button.onClick has a listener
- Verify UI EventSystem exists in Canvas

**Framerate not changing?**
- Some platforms override Application.targetFrameRate
- Check if VSync is enabled (takes priority)

---

**Need help?** Debug output goes to Console - watch for `[Graphics]` logs!
