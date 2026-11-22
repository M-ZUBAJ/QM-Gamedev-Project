# Tony's change to Marko's EnemySpawner, 21/11/2026: a new monster every night, monsters out of sight

**Why:** Tony asked for:
- **A new monster every night, the ones before keep coming:** night 1 slimes, night 2 slimes and turtles, night 3
  slimes, turtles and a third monster (not chosen yet).
- **The turtle moves from the desert's day to the night**, from night 2, with a shell phase: now and then it hides in
  its shell; melee hits on the shell do nothing to it and hurt the player (15); the fireball still hurts it.
- **No monster popping up right in front of the player** when they turn around (they appeared 10 m away, in any
  direction).

Tony's own scripts do the work: **NightMonsters** (on your EnemySpawner's object: which monster, how strong, where),
**ShellDefense** (the turtle's shell), and his PunchEffect / EnemyAnimator / FirstPersonHands / FloatingText. Guide:
`Docs/Night Monsters Setup.md`.

## Files changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemySpawner.cs` | In `SpawnEnemy()`, all marked `[Tony]` (21/11/2026): (1) after your "Enemy prefab has not been assigned" check: `NightMonsters nightMonsters = GetComponent<NightMonsters>();` and `int night = DaylightCycle.Current != null ? DaylightCycle.Current.Day : 1;`. (2) After your random spot (and Tony's barrier line): `if (nightMonsters != null && nightMonsters.FindSpot(player, out Vector3 hidden)) spawnPosition = hidden;` (24 - 34 m away, where the player can't see; your `GroundHeight` still sets the height). (3) `Instantiate(` now takes `nightMonsters != null ? nightMonsters.Pick(enemyPrefab, night) : enemyPrefab` (was `enemyPrefab`; the original is in the comment): night 1 your Enemy Prefab only, from night 2 sometimes the turtle. (4) After your health / damage scaling: `if (nightMonsters != null) nightMonsters.Prepare(enemy, night);` (a picked monster's own health and damage; the slime keeps yours). Your spawn timing, max number, targets and dawn removal are unchanged. |
| Both game scenes (data, by Tony's Tools > Enemies > Set Up Night Monsters) | Tony's **NightMonsters** added to your EnemySpawner's object, with the Turtle from night 2. Your EnemySpawner's own settings don't change. |

The turtle prefab (`Assets/Prefabs/Enemy/TurtleShellEnemy`) uses your EnemyCombat, EnemyMovement and EnemyAttack as
before (none of them changed): while it's in its shell, Tony's ShellDefense switches your EnemyMovement and EnemyAttack
off, then on again.

## How to undo

- `EnemySpawner.cs`: delete the lines marked `[Tony] added (21/11/2026)` and put `enemyPrefab,` back into
  `Instantiate(` (the `[Tony] replaced` line). Or simply remove NightMonsters from the EnemySpawner's object: without
  it your code runs exactly as before.
