SpiderEnemyController – quick setup
-----------------------------------

What it does
- Simple state machine: idle/patrol, chase the player, shoot in range, retreat to cover when low on health.
- Uses `NavMeshAgent` for movement and tries to pick a cover spot that has an obstacle between spider and player.
- Works with any spider rig (pairs well with `AbsoluteSpiderFreakout` for leg animation).

Required scene setup
1) Add a `NavMeshAgent` to the spider root. Tune agent radius/height to match the rig.
2) Add a `Health` component so the controller knows when to retreat.
3) Add `SpiderEnemyController` to the same GameObject.
4) Assign `Player` in the inspector (or leave empty and tag the player object as `Player`).
5) Drag a bullet/projectile prefab into `Projectile Prefab` and set a `Fire Point` transform (a child pointing forward).
6) (Optional) Provide `Patrol Points` (array of Transforms) if you want looping patrols; leave empty to have it wander around its spawn.

Tuning the behavior
- Detection: `Detection Radius`, `Lose Sight Radius`, and `Field Of View` control when it spots/forgets the player.
- Movement: `Patrol/Chase/Retreat Speed` adjust per-state movement; `Waypoint Tolerance` sets how close it must get to patrol points.
- Combat: `Attack Range`, `Shooting Cooldown`, `Projectile Speed` control how it fires. It needs clear line of sight.
- Survival: `Low Health Fraction` decides when to flee; `Cover Search Radius/Samples` set how aggressively it looks for obstacles to hide behind. Set `Obstacle Layers` to the environment geometry layer.

How to test quickly
- Drop the spider on a baked NavMesh, add the components above, and press play.
- Move the player into `Detection Radius`; it should chase, then stop to fire when inside `Attack Range`.
- Reduce the spider’s health in the inspector below `Low Health Fraction`; it will try to run to cover or away from the player.
