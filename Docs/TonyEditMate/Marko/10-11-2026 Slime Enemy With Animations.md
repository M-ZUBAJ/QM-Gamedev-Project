# Tony's edits to Marko's code, 10/11/2026: slime enemy with animations

Every changed line is marked `// [Tony]` in the file. A replaced line keeps Marko's original in a
`// [Tony] replaced: ...` comment just above it, so everything can be put back.

**Why:** Tony asked for a proper enemy. The ball enemy:
- slid through walls and buildings: it moved by changing its position (`transform.position +=`), which skips physics;
- floated or sank into hills: it always spawned at height 0.5 and never moved up or down;
- had no animations.

The new **SlimeEnemy** (the slime from the "RPG Monster Duo PBR Polyart" pack):
- walks on the ground and is stopped by walls;
- plays idle, walk / run, attack, get hit, death and a victory cheer when the player dies.

**Marko's old ball enemy still works like before.** The movement change only happens when an enemy has a
Rigidbody, and the animation and death delay only when it has Tony's `EnemyAnimator`. `Enemy.prefab` itself is
untouched. The one change it does feel is the spawn height (section 4).

---

## 1. `Assets/Script/Enemy/EnemyMovement.cs`

**What changed** (only added lines, nothing of Marko's removed or replaced):
- `public float turnSpeed = 360f;` and `private Rigidbody body;`
- `Awake()`: finds the enemy's Rigidbody (if it has one).
- First line of `Update()`: `if (body != null) return;`. An enemy with a Rigidbody is moved in `FixedUpdate` instead.
- `FixedUpdate()`:
  - sets the Rigidbody's speed (velocity) towards the target, flat, keeping its falling speed so gravity works;
  - stops at `stopDistance`, like Update does;
  - turns the enemy to face the target at `turnSpeed`.

  Setting the speed instead of the position lets the physics stop the enemy at walls.

**To undo:** delete the lines marked `[Tony]`.

## 2. `Assets/Script/Enemy/EnemyAttack.cs`

**What changed** (only added lines): 2 lines at the end of `Attack()`. If the enemy has an `EnemyAnimator`, they
call `PlayAttack()` (the attack animation). The damage is the same and still lands when the attack starts.

**To undo:** delete the 2 lines marked `[Tony]`.

## 3. `Assets/Script/Enemy/EnemyCombat.cs`

**What changed:**
- **Added** `private bool dead;`, `if (dead) return;` as the first line of `TakeDamage`, and `dead = true;` as the
  first line of `Die()`.
- **Replaced** `Destroy(gameObject);` at the end of `Die()` with `Destroy(gameObject, removeAfter);`.
  - `removeAfter` comes from `EnemyAnimator.PlayDeath()`: 2.5 seconds by default.
  - In that time it plays the death animation, stops the enemy moving and attacking, and switches off its colliders
    so it can't be hit again.
  - Without an EnemyAnimator, `removeAfter` is 0, so the enemy is removed at once, like before.

**Why the `dead` flag:** the body now stays for a moment. Without it, a second hit in that time would call `Die()`
again and give the coins twice. Coins are unchanged (3 to 6, given the moment it dies).

**To undo:** delete the lines marked `[Tony]` and put `Destroy(gameObject);` back.

## 4. `Assets/Script/Enemy/EnemySpawner.cs`

**What changed:**
- **Replaced** `spawnPosition.y = 0.5f;` with `spawnPosition.y = GroundHeight(spawnPosition);`
- **Added** `GroundHeight(Vector3 spot)`. A ray from 500 m above the spot finds the ground there (terrain, rock,
  floor...) and returns that height plus 0.5 m. If nothing is found, it returns 0.5, like before.

**Why:** a fixed 0.5 put enemies inside hills or in the air over dips. An enemy with physics that spawns inside a hill
would fall through the world.

**Effect on the old ball:** its middle is 0.5 above its bottom, so it now stands on the ground where it spawns.
The slime's pivot is at its feet, so it drops the last half meter onto the ground.

**To undo:** put `spawnPosition.y = 0.5f;` back and delete `GroundHeight`.

---

## New files (Tony's, nothing of Marko's)

| File | What it is |
|---|---|
| `Assets/Script/Enemy/EnemyAnimator.cs` | On the enemy. Plays the animations; `PlayAttack()` and `PlayDeath()` are called by the lines above. Flinches by itself when `currentHealth` goes down, and cheers when the player dies. |
| `Assets/Script/Editor/EnemyBuilder.cs` | **Tools > Enemies > Build Monster Enemy**: turns a monster model into an enemy prefab. |
| `Assets/Script/Editor/AnimatorBuildHelper.cs` | Shared pieces for the tools that build Animator Controllers. |
| `Assets/Prefabs/Enemy/SlimeEnemy.prefab` | Made by the tool (see below). |
| `Assets/Animation/Enemy/SlimeEnemy.controller` | Its animations: Locomotion (IdleBattle / WalkFWD / RunFWD by speed), Attack (Attack01), Hit (GetHit), Victory, Dead (Die). |
| `Assets/Prefabs/Enemy/EnemyNoFriction.physicMaterial` | No friction, so the enemy slides along walls instead of sticking. |

**SlimeEnemy.prefab** uses Marko's scripts as they are:
- **SlimeEnemy:**
  - Rigidbody: mass 5, rotation frozen.
  - Sphere Collider with the no-friction material.
  - Marko's EnemyCombat (defaults).
  - Marko's EnemyMovement: Move Speed 2, **Stop Distance 1.5**.
  - Marko's EnemyAttack: Attack Range 2.
  - Tony's EnemyAnimator.
- **Model:** the slime, 1 m tall.

**Why Stop Distance 1.5:** the slime's pivot is at its feet, but the player's pivot is the middle of the ball, 0.5 up.
At 2 m apart on the flat they were 2.06 m apart in total, just outside Attack Range 2, so it never attacked.

**GameScene:** the tool offers to set the EnemySpawner's **Enemy Prefab** to SlimeEnemy. To go back, drag
`Enemy.prefab` into that slot again. The spawner still raises health and damage each day, as before.

---

## Good to know (not changed, Marko's)

- **The player's attack may not reach enemies in third person.** The attack ray starts at the camera and is
  `attackRange` (2.5 m) long: `Assets/Script/Player/PlayerCombat.cs:90`,
  `Physics.Raycast(ray, out hit, attackRange)`. In third person (F5) the camera is several meters behind the player,
  so it may not reach any enemy. First person works.
- **No path finding.** The slime walks straight at the player, so it can get stuck behind a wall.

## For Marko: other monsters and the animations

- **Another monster:** select its prefab in the Project window (e.g. `TurtleShellPolyart`), then
  **Tools > Enemies > Build Monster Enemy**. You get `TurtleShellEnemy.prefab` in the same way.
- **From your own code:** `GetComponent<EnemyAnimator>().PlayAttack();`. Its `IsDead` tells you if it's dying.
- **Guide:** `Docs/Enemy Setup.md`.
