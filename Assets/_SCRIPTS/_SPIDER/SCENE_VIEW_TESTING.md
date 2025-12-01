# Scene View Testing Guide

## Dragging the Spider in Scene View

You can now test the spider's walking behavior directly in the scene view without entering game mode:

1. **Select the spider** in the hierarchy or scene
2. **Click and drag** the spider in the scene view
3. The spider will automatically walk as you drag it

The spider will:
- Detect movement from your dragging
- Calculate velocity based on position changes
- Trigger leg steps when legs move beyond the step threshold
- Animate legs with smooth arcs

## Adjusting Walking Behavior

If the legs are stepping too much or too little, adjust these settings in the SpiderIKSystem inspector:

- **Step Threshold** (0.4m default): Distance a leg must move before stepping. Increase to reduce stepping frequency.
- **Step Height** (0.1m default): Arc height during steps. Reduce if legs are moving too high.
- **Step Speed** (5 steps/sec default): How fast legs animate. Increase for faster steps.
- **Stride Forward** (0.3m default): Base forward projection when stationary.
- **Stride Velocity Scale** (0.4 default): How much velocity affects stride length.

## Game Mode Testing

In game mode, the spider responds to:
- **WASD keys** for movement (via SpiderController)
- **Physics forces** applied to the rigidbody
- **Automatic walking** based on rigidbody velocity

## Troubleshooting

**Legs not stepping:**
- Check that the spider has a "Legs" child object with leg hierarchy
- Verify step threshold isn't too high
- Ensure velocity is being calculated (check console for debug info)

**Legs stepping too much:**
- Increase step threshold
- Reduce stride forward value
- Check that legs are properly positioned around the body

**Legs moving upward excessively:**
- Reduce step height in StepAnimator
- Check that step speed isn't too fast
