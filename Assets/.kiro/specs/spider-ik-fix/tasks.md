# Implementation Plan

- [x] 1. Fix leg positioning and proportions in CreateNewSpider method


  - Update leg root positions to be at body center height (not below)
  - Set leg root horizontal positions to 0.5m from center (X axis) and 0.8m (Z axis)
  - Update DEFAULT_UPPER_LEN and DEFAULT_LOWER_LEN to 0.5m each for 1m total leg length
  - Update DEFAULT_BODY_CLEARANCE to 1.0m for proper body height
  - Ensure knee forward offset (DEFAULT_KNEE_FORWARD) is 0.15m
  - _Requirements: 1.1, 1.4, 2.4, 2.5, 4.2, 4.3, 4.4, 4.5_



- [ ] 2. Fix leg chain creation to ensure downward orientation
  - Modify CreateLegChain to position Hip at (0, 0, 0) relative to leg root
  - Position Knee at (0, -0.5, 0) initially (straight down from Hip)
  - Position Foot at (0, -0.5, 0) relative to Knee (straight down)
  - Apply knee forward offset AFTER initial positioning: Knee at (0, -0.5, 0.15)


  - Ensure all local rotations are identity (0, 0, 0)
  - _Requirements: 2.1, 2.2, 2.3, 3.1_

- [ ] 3. Fix IK solver bend direction calculation
  - Update SolveLegIK to correctly calculate bend direction from cached bendNormal
  - Ensure bend direction is perpendicular to hip-target vector


  - Use cross product: bendDirection = Cross(bendNormal, toTarget).normalized
  - Add fallback bend direction if cross product is near zero
  - Verify knee bends forward (positive Z in body space)
  - _Requirements: 3.2, 3.3, 6.1, 6.2_



- [ ] 4. Implement proper IK target clamping
  - Calculate minimum reach as |upperLength - lowerLength| + 0.01
  - Calculate maximum reach as upperLength + lowerLength - 0.02 (slight softening)
  - Clamp target distance to [minReach, maxReach] range
  - Update maxReach public parameter default to 1.2m
  - _Requirements: 6.3, 6.4_



- [ ] 5. Fix ground detection and body positioning
  - Update SnapBodyAndFeetToGround to position body 1.0m above ground (not 0.3m)
  - Ensure raycast starts from body position + 2m up
  - Raycast down 5m to find ground


  - Position body at ground hit point + 1.0m up
  - Update all foot positions to ground contact points
  - _Requirements: 1.1, 1.2, 5.2, 5.3, 5.5_

- [x] 6. Improve step triggering logic


  - Update step threshold default to 0.6m
  - Add check for minimum body velocity (> 0.05 m/s) before allowing steps
  - Enforce minimum time between steps (0.25 seconds) using lastStepTime
  - Calculate desired foot position based on body-relative rest pose
  - _Requirements: 9.1, 9.2, 9.3, 9.4_



- [ ] 7. Implement diagonal gait coordination
  - Update StartStep to assign diagonal pair groups: (0,3) and (1,2)
  - Set stagger delay to 0.3 seconds between diagonal pairs
  - Add small random variation (±0.06s) to prevent perfect sync


  - Ensure diagonal pairs step together by checking leg indices
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 8. Fix step arc calculation
  - Update UpdateStep to use sine curve for height: sin(progress * π) * stepHeight


  - Set default stepHeight to 0.15m
  - Ensure arc peaks at progress = 0.5
  - Verify foot returns to ground level at progress = 1.0
  - _Requirements: 8.1, 8.2, 8.3_

- [x] 9. Implement step completion and planting


  - Update UpdateStep to set isStepping = false when progress >= 1.0
  - Set currentTarget to stepTargetPosition on completion
  - Update lastPlantedPosition to final target position
  - Record lastStepTime to prevent immediate re-stepping
  - _Requirements: 8.4_



- [ ] 10. Add stationary stability logic
  - Calculate body velocity using exponential smoothing
  - When velocity < 0.05 m/s, maintain planted foot positions
  - Use MaintainGroundContact to keep feet aligned to ground
  - Prevent step initiation when body is stationary


  - _Requirements: 9.5_

- [ ] 11. Update visual mesh system
  - Verify LegConnector component updates cylinder position/rotation/scale correctly
  - Ensure cylinders connect Hip-Knee and Knee-Foot dynamically
  - Update cylinder radius: 0.08m for upper leg, 0.06m for lower leg



  - Add knee joint sphere (0.12m radius) for better visualization
  - Set appropriate colors: gray for legs, light blue for joints, dark for feet
  - _Requirements: 10.3_

- [ ] 12. Fix editor CreateNewSpider button
  - Ensure button creates exactly 4 legs with correct names
  - Verify all leg roots are assigned to spider.legRoots array
  - Call InitializeLegs() after leg creation
  - Set default walking parameters: stepThreshold=0.6, stepHeight=0.15, stepSpeed=4, stepStagger=0.3
  - Call SnapBodyAndFeetToGround to position spider correctly
  - _Requirements: 10.1, 10.2, 10.4, 10.5_

- [ ] 13. Add error handling for invalid configurations
  - Check for null joint references in UpdateLeg, skip if invalid
  - Add NaN/Infinity checks in SolveLegIK, revert to rest pose if detected
  - Log warnings for ground detection failures
  - Display colored gizmos: red for errors, yellow for warnings, green for normal
  - _Requirements: Error Handling_

- [ ] 14. Update gizmo visualization
  - Draw leg chains in yellow
  - Draw current target in green (planted) or red (stepping)
  - Draw rest target in blue
  - Draw bend normal in cyan
  - Add labels showing leg state and angles in editor
  - _Requirements: Debugging/Visualization_

- [ ] 15. Checkpoint - Verify spider creation and basic structure
  - Create new spider using editor button
  - Verify body is 1m above ground
  - Verify all 4 legs are present with correct hierarchy
  - Verify leg segments are 0.5m each
  - Verify knees have forward offset
  - Verify all feet touch ground
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Write property test for leg length consistency
  - **Property 1: Leg length consistency**
  - **Validates: Requirements 1.4, 2.6**
  - Generate 100 random spider configurations
  - For each spider, verify all legs have total length 1.0m ±0.1m
  - Measure distance from Hip to Foot for each leg

- [ ] 17. Write property test for leg hierarchy
  - **Property 2: Leg hierarchy correctness**
  - **Validates: Requirements 2.1**
  - Generate 100 random spiders
  - For each leg, verify Hip.parent == LegRoot, Knee.parent == Hip, Foot.parent == Knee

- [ ] 18. Write property test for downward orientation
  - **Property 3: Downward leg orientation**
  - **Validates: Requirements 2.2, 2.3**
  - Generate 100 random spiders in rest pose
  - For each leg, verify Hip.Y > Knee.Y > Foot.Y

- [ ] 19. Write property test for leg segment lengths
  - **Property 4: Leg segment length consistency**
  - **Validates: Requirements 2.4, 2.5**
  - Generate 100 random spiders
  - For each leg, verify Hip-Knee distance ≈ 0.5m ±0.05m
  - For each leg, verify Knee-Foot distance ≈ 0.5m ±0.05m

- [ ] 20. Write property test for knee forward offset
  - **Property 5: Knee forward offset**
  - **Validates: Requirements 3.1**
  - Generate 100 random spiders in rest pose
  - For each leg, calculate knee offset from Hip-Foot line
  - Verify offset is 0.1 to 0.3 meters in forward direction (positive Z)

- [ ] 21. Write property test for forward knee bending
  - **Property 6: Forward knee bending**
  - **Validates: Requirements 3.3**
  - Generate 100 random spiders with random IK targets
  - For each leg after IK solving, verify knee is forward of Hip-Foot line in body space

- [ ] 22. Write property test for leg root positioning
  - **Property 7: Leg root positioning**
  - **Validates: Requirements 4.2, 4.3, 4.4**
  - Generate 100 random spiders
  - For each leg root, verify horizontal distance from body center is 0.4 to 0.7 meters

- [ ] 23. Write property test for leg root height
  - **Property 8: Leg root height alignment**
  - **Validates: Requirements 4.5**
  - Generate 100 random spiders
  - For each leg root, verify Y position equals body Y position ±0.01m

- [ ] 24. Write property test for ground contact at rest
  - **Property 9: Ground contact at rest**
  - **Validates: Requirements 5.2**
  - Generate 100 random spiders on flat terrain
  - For each foot, raycast down and verify foot is within 0.05m of ground

- [ ] 25. Write property test for reachability
  - **Property 10: Reachability from body height**
  - **Validates: Requirements 5.5**
  - Generate 100 spiders with 1m body height and 1m legs
  - Verify all feet can reach ground (distance from foot to ground < 0.05m)

- [ ] 26. Write property test for IK max reach clamping
  - **Property 11: IK target clamping (max reach)**
  - **Validates: Requirements 6.3**
  - Generate 100 random targets beyond max reach
  - For each leg, verify foot ends up at max reach distance from hip

- [ ] 27. Write property test for IK min reach clamping
  - **Property 12: IK target clamping (min reach)**
  - **Validates: Requirements 6.4**
  - Generate 100 random targets closer than min reach
  - For each leg, verify foot ends up at min reach distance from hip

- [ ] 28. Write property test for IK accuracy
  - **Property 13: IK accuracy**
  - **Validates: Requirements 6.5**
  - Generate 100 random valid targets within reach
  - For each leg, verify foot position matches target within 0.01m

- [ ] 29. Write property test for diagonal coordination FL-BR
  - **Property 14: Diagonal pair coordination (FL-BR)**
  - **Validates: Requirements 7.2**
  - Simulate 100 random movement scenarios
  - When FL is stepping, verify BR is stepping or stepped within 0.5s

- [ ] 30. Write property test for diagonal coordination FR-BL
  - **Property 15: Diagonal pair coordination (FR-BL)**
  - **Validates: Requirements 7.3**
  - Simulate 100 random movement scenarios
  - When FR is stepping, verify BL is stepping or stepped within 0.5s

- [ ] 31. Write property test for diagonal stability
  - **Property 16: Diagonal pair stability**
  - **Validates: Requirements 7.4**
  - Simulate 100 random movement scenarios
  - When one diagonal pair steps, verify other pair has at least one leg planted

- [ ] 32. Write property test for step arc motion
  - **Property 17: Step arc motion**
  - **Validates: Requirements 8.2**
  - Trigger 100 random steps
  - For each step, verify foot Y increases then decreases (arc shape)

- [ ] 33. Write property test for step arc height
  - **Property 18: Step arc height bounds**
  - **Validates: Requirements 8.3**
  - Trigger 100 random steps
  - For each step, verify max vertical offset is 0.1 to 0.3 meters

- [ ] 34. Write property test for step completion
  - **Property 19: Step completion accuracy**
  - **Validates: Requirements 8.4**
  - Trigger 100 random steps
  - When progress reaches 1.0, verify foot is at target within 0.05m

- [ ] 35. Write property test for step triggering
  - **Property 20: Step triggering**
  - **Validates: Requirements 9.2**
  - Generate 100 scenarios where foot exceeds threshold
  - Verify leg initiates step when body is moving

- [ ] 36. Write property test for step non-interruption
  - **Property 21: Step non-interruption**
  - **Validates: Requirements 9.4**
  - Trigger 100 steps and attempt to interrupt
  - Verify no new step starts while isStepping = true

- [ ] 37. Write property test for stationary stability
  - **Property 22: Stability when stationary**
  - **Validates: Requirements 9.5**
  - Create 100 spiders with zero velocity
  - Verify all feet remain within 0.02m of planted positions

- [ ] 38. Final checkpoint - Complete spider testing
  - Test spider creation via editor button
  - Move spider body in scene, verify diagonal gait walking
  - Test on flat and sloped terrain
  - Verify all feet maintain ground contact
  - Verify smooth step arcs without jittering
  - Verify legs extend downward (not outward) from body
  - Verify spider is 2 meters tall (1m body + 1m legs)
  - Ensure all tests pass, ask the user if questions arise.
