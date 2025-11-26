# Requirements Document

## Introduction

This document specifies the requirements for fixing the spider IK (Inverse Kinematics) leg system to create a realistic, functional spider with proper leg anatomy, positioning, and walking behavior. The spider must be 2 meters tall with legs that extend downward from the body, have proper joint hierarchy (Hip→Knee→Foot), and walk naturally using diagonal gait patterns.

## Glossary

- **Spider Body**: The main central GameObject representing the spider's torso, positioned 1 meter above ground
- **Total Spider Height**: The combined height of Spider Body (1 meter) plus leg length (1 meter), totaling 2 meters
- **Leg Root**: The attachment point of a leg to the Spider Body
- **Hip Joint**: The first joint in the leg chain, attached to the Leg Root, controls leg direction
- **Knee Joint**: The middle joint in the leg chain, the primary bending point for the leg
- **Foot Joint**: The end effector of the leg chain, makes contact with the ground
- **IK System**: Inverse Kinematics solver that positions joints to reach target positions
- **Diagonal Gait**: Walking pattern where opposite diagonal legs move together (Front-Left with Back-Right, Front-Right with Back-Left)
- **Ground Contact**: The point where the Foot Joint touches the ground surface
- **Leg Segment**: The distance between two consecutive joints (Hip-to-Knee or Knee-to-Foot)
- **Bend Direction**: The direction in which the Knee Joint bends during IK solving

## Requirements

### Requirement 1

**User Story:** As a game developer, I want the spider to have a total height of 2 meters (1 meter body height + 1 meter leg length), so that the spider has proper scale and presence in the game world.

#### Acceptance Criteria

1. WHEN the spider is created THEN the Spider Body SHALL be positioned 1.0 meter (±0.1m) above the ground surface
2. WHEN measuring the Spider Body height THEN the system SHALL use ground raycasting to determine the ground level
3. WHEN measuring total spider height THEN the distance from ground to top of Spider Body SHALL be 2.0 meters (±0.1m)
4. WHEN legs are fully extended THEN each leg SHALL have a total length of approximately 1.0 meter from Leg Root to Foot Joint

### Requirement 2

**User Story:** As a game developer, I want spider legs to have correct anatomical structure with Hip above Knee above Foot, so that the IK system can solve leg positions naturally.

#### Acceptance Criteria

1. WHEN a leg is created THEN the system SHALL establish a hierarchy where Hip Joint is parent of Knee Joint and Knee Joint is parent of Foot Joint
2. WHEN positioning leg joints in rest pose THEN the Hip Joint SHALL be positioned above the Knee Joint along the negative Y axis
3. WHEN positioning leg joints in rest pose THEN the Knee Joint SHALL be positioned above the Foot Joint along the negative Y axis
4. WHEN measuring leg segments THEN the Hip-to-Knee segment SHALL have length of approximately 0.5 meters
5. WHEN measuring leg segments THEN the Knee-to-Foot segment SHALL have length of approximately 0.5 meters
6. WHEN measuring total leg length THEN the Hip-to-Foot distance SHALL be approximately 1.0 meter when fully extended

### Requirement 3

**User Story:** As a game developer, I want the knee joint to bend in a natural forward direction, so that the spider's leg movement looks realistic.

#### Acceptance Criteria

1. WHEN setting up the rest pose THEN the Knee Joint SHALL be offset forward (positive Z direction) by 0.1 to 0.3 meters from the straight Hip-Foot line
2. WHEN the IK System calculates bend direction THEN the system SHALL use the Knee Joint offset to determine the bending plane
3. WHEN the leg bends during IK solving THEN the Knee Joint SHALL move in the forward direction relative to the Spider Body

### Requirement 4

**User Story:** As a game developer, I want legs to be attached at appropriate positions on the spider body, so that the spider has balanced proportions.

#### Acceptance Criteria

1. WHEN creating a 4-legged spider THEN the system SHALL position Leg Roots at four locations around the Spider Body
2. WHEN positioning front legs THEN the Leg Roots SHALL be placed 0.4 to 0.7 meters forward from the Spider Body center
3. WHEN positioning back legs THEN the Leg Roots SHALL be placed 0.4 to 0.7 meters backward from the Spider Body center
4. WHEN positioning left and right legs THEN the Leg Roots SHALL be placed 0.4 to 0.7 meters to each side from the Spider Body center
5. WHEN positioning Leg Roots vertically THEN the system SHALL place them at the Spider Body center height (not above or below)

### Requirement 5

**User Story:** As a game developer, I want legs to reach the ground from the 2-meter high body, so that the spider stands properly on the surface.

#### Acceptance Criteria

1. WHEN calculating total leg length THEN the system SHALL ensure Hip-to-Knee length plus Knee-to-Foot length equals approximately 1.0 meter
2. WHEN the spider is at rest THEN all Foot Joints SHALL make Ground Contact with the surface
3. WHEN a Foot Joint is positioned THEN the system SHALL use raycasting to find the ground surface below the leg
4. WHEN no ground is detected THEN the system SHALL extend the leg to maximum reach distance
5. WHEN the Spider Body is 1 meter above ground THEN the legs SHALL be able to reach the ground surface

### Requirement 6

**User Story:** As a game developer, I want the IK system to solve leg positions correctly, so that legs bend naturally to reach target positions.

#### Acceptance Criteria

1. WHEN the IK System receives a target position THEN the system SHALL calculate Knee Joint position using the two-bone IK algorithm
2. WHEN calculating Knee Joint position THEN the system SHALL use the cached Bend Direction from the rest pose
3. WHEN the target is beyond maximum reach THEN the system SHALL clamp the target to the maximum reachable distance
4. WHEN the target is closer than minimum reach THEN the system SHALL clamp the target to the minimum reachable distance
5. WHEN IK solving completes THEN the Foot Joint SHALL be positioned at the target location

### Requirement 7

**User Story:** As a game developer, I want the spider to walk using a diagonal gait pattern, so that movement looks natural and stable.

#### Acceptance Criteria

1. WHEN the spider moves THEN the system SHALL coordinate leg stepping using Diagonal Gait pattern
2. WHEN Front-Left leg steps THEN the Back-Right leg SHALL step simultaneously or with minimal delay
3. WHEN Front-Right leg steps THEN the Back-Left leg SHALL step simultaneously or with minimal delay
4. WHEN one diagonal pair is stepping THEN the other diagonal pair SHALL remain planted for stability
5. WHEN calculating step timing THEN the system SHALL introduce a stagger delay of 0.2 to 0.5 seconds between diagonal pairs

### Requirement 8

**User Story:** As a game developer, I want legs to step smoothly with an arc motion, so that walking looks natural and not robotic.

#### Acceptance Criteria

1. WHEN a leg begins stepping THEN the system SHALL interpolate the Foot Joint position from current position to target position
2. WHEN interpolating step motion THEN the system SHALL add a vertical offset following a sine curve
3. WHEN calculating vertical offset THEN the maximum height SHALL be between 0.1 and 0.3 meters
4. WHEN a step completes THEN the Foot Joint SHALL be planted at the target Ground Contact position
5. WHEN a step is in progress THEN the step progress SHALL advance at a rate of 3 to 7 units per second

### Requirement 9

**User Story:** As a game developer, I want legs to step when the body moves beyond a threshold distance, so that the spider walks responsively.

#### Acceptance Criteria

1. WHEN the Spider Body moves THEN the system SHALL calculate the distance between current Foot Joint position and desired rest position
2. WHEN the distance exceeds the step threshold THEN the system SHALL initiate a step for that leg
3. WHEN setting step threshold THEN the value SHALL be between 0.4 and 1.0 meters
4. WHEN a leg is already stepping THEN the system SHALL NOT initiate a new step until the current step completes
5. WHEN the Spider Body is stationary THEN legs SHALL remain planted at their Ground Contact positions

### Requirement 10

**User Story:** As a game developer, I want to easily create and configure a spider in the Unity editor, so that I can quickly test and iterate on spider behavior.

#### Acceptance Criteria

1. WHEN the developer clicks "CREATE NEW SPIDER" button THEN the system SHALL generate a complete spider with body and 4 legs
2. WHEN generating a spider THEN the system SHALL automatically assign all Leg Root references to the spider component
3. WHEN generating a spider THEN the system SHALL create visual meshes for the body and leg segments
4. WHEN generating a spider THEN the system SHALL configure default walking parameters for Diagonal Gait
5. WHEN the spider is created THEN the system SHALL position the Spider Body and all Foot Joints using ground raycasting
