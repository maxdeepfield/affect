# Design Document

## Overview

This design document specifies the technical solution for fixing the spider IK leg system to create a realistic 2-meter tall spider (1m body + 1m legs) with proper anatomical structure, natural movement, and diagonal gait walking pattern. The solution addresses the current issues where legs extend outward instead of downward, and implements a robust two-bone IK solver with proper joint hierarchy.

## Architecture

The spider IK system consists of three main components:

1. **SPIDER_IK_LEGS Component**: Main controller attached to the spider body GameObject, manages all legs and coordinates walking behavior
2. **LegData Structure**: Internal data structure storing joint references, cached measurements, and stepping state for each leg
3. **Visual Mesh System**: Dynamic visual representation using cylinders and spheres that update to match joint positions

### Component Hierarchy

```
Spider (GameObject with SPIDER_IK_LEGS component)
├── Body_Mesh (visual representation)
├── Leg_Front_Left (Leg Root)
│   └── Hip
│       └── Knee
│           └── Foot
├── Leg_Front_Right (Leg Root)
│   └── Hip
│       └── Knee
│           └── Foot
├── Leg_Back_Left (Leg Root)
│   └── Hip
│       └── Knee
│           └── Foot
└── Leg_Back_Right (Leg Root)
    └── Hip
        └── Knee
            └── Foot
```

## Components and Interfaces

### SPIDER_IK_LEGS Component

**Public Interface:**
```csharp
public class SPIDER_IK_LEGS : MonoBehaviour
{
    // Configuration
    public Transform[] legRoots;
    public float stepThreshold = 0.6f;
    public float stepHeight = 0.15f;
    public float stepSpeed = 5f;
    public float stepStagger = 0.3f;
    public LayerMask groundLayers;
    public float raycastHeight = 1.5f;
    public float maxReach = 1.2f;
    public bool alignToGround = true;
    
    // Methods
    public void InitializeLegs();
    public void CacheLegRestPose(LegData leg);
}
```

**Key Responsibilities:**
- Initialize and manage all leg data structures
- Update leg IK solving each frame
- Coordinate diagonal gait stepping pattern
- Track body velocity for step prediction
- Provide editor tools for spider creation

### LegData Structure

```csharp
[System.Serializable]
public class LegData
{
    // Joint references
    public Transform root;
    public Transform hip;
    public Transform knee;
    public Transform foot;
    
    // Cached measurements
    public Vector3 restLocalTarget;
    public Vector3 bendNormal;
    public float upperLength;  // Hip to Knee
    public float lowerLength;  // Knee to Foot
    public float totalLength;
    
    // Stepping state
    public bool isStepping;
    public Vector3 currentTarget;
    public Vector3 stepStartPosition;
    public Vector3 stepTargetPosition;
    public Vector3 lastPlantedPosition;
    public float stepProgress;
    public float lastStepTime;
    public int legIndex;
}
```

### Visual Mesh Components

**LegConnector Component:**
```csharp
public class LegConnector : MonoBehaviour
{
    public Transform startJoint;
    public Transform endJoint;
    public float radius = 0.08f;
    public Color color = Color.gray;
    
    // Automatically updates cylinder position, rotation, and scale
    // to connect two joints dynamically
}
```

## Data Models

### Leg Positioning Model

**Body Position:**
- Height above ground: 1.0 meter
- Determined by raycasting downward from initial position

**Leg Root Positions (relative to body center):**
```
Front-Left:  (+0.5, 0.0, +0.8)
Front-Right: (-0.5, 0.0, +0.8)
Back-Left:   (+0.5, 0.0, -0.8)
Back-Right:  (-0.5, 0.0, -0.8)
```

**Joint Positions (local to Leg Root):**
```
Hip:  (0.0, 0.0, 0.0)
Knee: (0.0, -0.5, +0.15)  // 0.5m down, 0.15m forward for bend hint
Foot: (0.0, -1.0, +0.15)  // 1.0m down total
```

### IK Solving Model

**Two-Bone IK Algorithm:**

1. **Input:** Hip position, target position, upper length, lower length, bend normal
2. **Calculate distance** from hip to target
3. **Clamp distance** to valid range: `[|upperLength - lowerLength|, upperLength + lowerLength]`
4. **Apply law of cosines** to find knee angle:
   ```
   cosAngle = (upperLength² + distance² - lowerLength²) / (2 * upperLength * distance)
   ```
5. **Calculate bend direction** perpendicular to hip-target line using bend normal
6. **Position knee** using angle and bend direction
7. **Update joint rotations** to point toward next joint

### Stepping State Machine

**States:**
- **Planted**: Foot is on ground, maintaining contact
- **Stepping**: Foot is moving through arc to new position

**Transitions:**
- Planted → Stepping: When distance to desired position > stepThreshold AND body is moving AND not recently stepped
- Stepping → Planted: When stepProgress >= 1.0

**Step Arc Calculation:**
```
position = lerp(startPos, targetPos, progress)
height = sin(progress * π) * stepHeight
finalPosition = position + up * height
```

### Diagonal Gait Coordination

**Leg Groups:**
- Group A: Front-Left (index 0) + Back-Right (index 3)
- Group B: Front-Right (index 1) + Back-Left (index 2)

**Stepping Rules:**
- When Group A steps, Group B remains planted
- When Group B steps, Group A remains planted
- Stagger delay between groups: 0.3 seconds
- Small random variation (±0.06s) prevents perfect synchronization


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Leg length consistency

*For any* leg in the spider, the total length from Hip to Foot when fully extended should be approximately 1.0 meter (±0.1m)
**Validates: Requirements 1.4, 2.6**

### Property 2: Leg hierarchy correctness

*For any* leg in the spider, the Hip Joint must be the parent of the Knee Joint, and the Knee Joint must be the parent of the Foot Joint
**Validates: Requirements 2.1**

### Property 3: Downward leg orientation

*For any* leg in rest pose, the Hip Joint Y position must be greater than the Knee Joint Y position, and the Knee Joint Y position must be greater than the Foot Joint Y position
**Validates: Requirements 2.2, 2.3**

### Property 4: Leg segment length consistency

*For any* leg in the spider, the Hip-to-Knee distance should be approximately 0.5 meters (±0.05m) and the Knee-to-Foot distance should be approximately 0.5 meters (±0.05m)
**Validates: Requirements 2.4, 2.5**

### Property 5: Knee forward offset

*For any* leg in rest pose, the Knee Joint should be offset forward (positive Z in body space) by 0.1 to 0.3 meters from the straight Hip-Foot line
**Validates: Requirements 3.1**

### Property 6: Forward knee bending

*For any* leg and any valid IK target position, when IK solving completes, the Knee Joint should be positioned forward (positive Z in body space) relative to the Hip-Foot line
**Validates: Requirements 3.3**

### Property 7: Leg root positioning

*For any* leg root in the spider, it should be positioned 0.4 to 0.7 meters away from the Spider Body center in the horizontal plane (X and Z axes)
**Validates: Requirements 4.2, 4.3, 4.4**

### Property 8: Leg root height alignment

*For any* leg root in the spider, its Y position should equal the Spider Body Y position (±0.01m)
**Validates: Requirements 4.5**

### Property 9: Ground contact at rest

*For any* spider at rest, all Foot Joints should be in contact with the ground surface (within 0.05m of raycast hit point)
**Validates: Requirements 5.2**

### Property 10: Reachability from body height

*For any* spider with body positioned 1 meter above ground and legs of 1 meter length, all feet should be able to reach the ground surface
**Validates: Requirements 5.5**

### Property 11: IK target clamping (max reach)

*For any* leg and any target position beyond maximum reach distance, after IK solving, the Foot Joint should be positioned at maximum reach distance from the Hip Joint
**Validates: Requirements 6.3**

### Property 12: IK target clamping (min reach)

*For any* leg and any target position closer than minimum reach distance, after IK solving, the Foot Joint should be positioned at minimum reach distance from the Hip Joint
**Validates: Requirements 6.4**

### Property 13: IK accuracy

*For any* leg and any valid target position within reach, after IK solving, the Foot Joint should be positioned at the target location (within 0.01m tolerance)
**Validates: Requirements 6.5**

### Property 14: Diagonal pair coordination (FL-BR)

*For any* time when the Front-Left leg is stepping, the Back-Right leg should also be stepping or have stepped within the last 0.5 seconds
**Validates: Requirements 7.2**

### Property 15: Diagonal pair coordination (FR-BL)

*For any* time when the Front-Right leg is stepping, the Back-Left leg should also be stepping or have stepped within the last 0.5 seconds
**Validates: Requirements 7.3**

### Property 16: Diagonal pair stability

*For any* time when one diagonal pair (FL-BR or FR-BL) is stepping, at least one leg from the other diagonal pair should be planted
**Validates: Requirements 7.4**

### Property 17: Step arc motion

*For any* leg during a step, the foot Y position should increase from start to mid-step, then decrease from mid-step to end
**Validates: Requirements 8.2**

### Property 18: Step arc height bounds

*For any* leg during a step, the maximum vertical offset from the straight-line path should be between 0.1 and 0.3 meters
**Validates: Requirements 8.3**

### Property 19: Step completion accuracy

*For any* leg when step progress reaches 1.0, the Foot Joint should be positioned at the target Ground Contact position (within 0.05m)
**Validates: Requirements 8.4**

### Property 20: Step triggering

*For any* leg where the distance from current foot position to desired rest position exceeds the step threshold, and the body is moving, the leg should initiate a step
**Validates: Requirements 9.2**

### Property 21: Step non-interruption

*For any* leg that is currently stepping (isStepping = true), the system should not initiate a new step until the current step completes (isStepping = false)
**Validates: Requirements 9.4**

### Property 22: Stability when stationary

*For any* spider with body velocity near zero (< 0.05 m/s), all feet should remain at their planted positions (within 0.02m of last planted position)
**Validates: Requirements 9.5**

## Error Handling

### Invalid Joint References

**Error Condition:** Leg has null Hip, Knee, or Foot transform
**Handling:** Skip IK solving for that leg, log warning in editor, display red gizmo at leg root

### Ground Detection Failure

**Error Condition:** Raycast fails to detect ground below foot
**Handling:** 
- Use last known ground position
- If no previous position, extend leg to maximum reach downward
- Display yellow gizmo to indicate uncertain ground contact

### IK Solving Failure

**Error Condition:** NaN or Infinity values in IK calculations
**Handling:**
- Revert to rest pose for that leg
- Log error with leg index and target position
- Continue solving other legs

### Excessive Step Frequency

**Error Condition:** Leg attempts to step more than 10 times per second
**Handling:**
- Enforce minimum time between steps (0.1 seconds)
- Log warning about potential jitter or configuration issues

### Body Penetration

**Error Condition:** Body position is below ground level
**Handling:**
- Snap body upward to 1 meter above ground
- Reset all leg targets
- Log warning about terrain collision

## Testing Strategy

### Unit Testing

Unit tests will verify specific examples and edge cases:

**Spider Creation Tests:**
- Test that CREATE NEW SPIDER button generates exactly 4 legs
- Test that body is positioned 1 meter above ground after creation
- Test that all leg roots are assigned to spider component
- Test that visual meshes are created for body and legs

**Leg Structure Tests:**
- Test that a single leg has correct Hip→Knee→Foot hierarchy
- Test that leg segment lengths are 0.5m each
- Test that knee has forward offset in rest pose

**IK Edge Cases:**
- Test IK with target at maximum reach distance
- Test IK with target at minimum reach distance
- Test IK with target directly above hip
- Test IK with target directly below hip

**Ground Detection Edge Cases:**
- Test foot positioning when no ground is detected
- Test foot positioning on sloped terrain
- Test foot positioning with multiple ground layers

### Property-Based Testing

Property-based tests will verify universal properties across many random inputs using **Unity Test Framework with NUnit**. Each test will run a minimum of 100 iterations.

**Test Framework:** Unity Test Framework (NUnit) with custom property test helpers

**Property Test Structure:**
```csharp
[Test]
public void Property_LegLengthConsistency()
{
    for (int i = 0; i < 100; i++)
    {
        // Generate random spider configuration
        // Verify property holds
    }
}
```

**Key Property Tests:**

1. **Leg Length Consistency** - Generate spiders with random body positions, verify all legs are 1.0m ±0.1m
2. **Downward Orientation** - Generate spiders, verify Hip.Y > Knee.Y > Foot.Y for all legs
3. **Ground Contact** - Generate spiders on random terrain, verify all feet touch ground at rest
4. **IK Accuracy** - Generate random valid targets, verify feet reach targets within tolerance
5. **Diagonal Coordination** - Simulate movement, verify diagonal pairs step together
6. **Step Arc Motion** - Trigger random steps, verify foot follows arc with correct height
7. **Stability** - Keep body stationary, verify feet don't drift from planted positions

**Test Data Generation:**
- Random body positions (0.5m to 2m above ground)
- Random target positions (within and beyond reach)
- Random terrain heights and slopes
- Random movement velocities and directions

**Assertion Helpers:**
```csharp
AssertApproximately(float expected, float actual, float tolerance)
AssertVector3Approximately(Vector3 expected, Vector3 actual, float tolerance)
AssertLegHierarchy(LegData leg)
AssertGroundContact(Transform foot, LayerMask groundLayers)
```

### Integration Testing

Integration tests will verify end-to-end behavior:

- Create spider in scene, move body, verify legs walk with diagonal gait
- Create spider on uneven terrain, verify all feet maintain ground contact
- Create multiple spiders, verify each operates independently
- Test spider creation via editor button, verify complete setup

### Manual Testing Checklist

- [ ] Spider appears 2 meters tall (1m body + 1m legs)
- [ ] Legs extend downward from body, not outward
- [ ] Knees bend forward naturally
- [ ] All feet touch ground when stationary
- [ ] Diagonal gait pattern visible when moving body
- [ ] Smooth step arcs without jittering
- [ ] Legs don't penetrate ground or body
- [ ] Visual meshes update correctly with joint positions
- [ ] Editor CREATE NEW SPIDER button works
- [ ] Gizmos show leg state clearly in scene view
