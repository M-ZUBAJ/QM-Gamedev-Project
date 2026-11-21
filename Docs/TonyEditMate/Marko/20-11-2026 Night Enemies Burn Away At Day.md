# Tony's change to Marko's EnemySpawner, 20/11/2026: night enemies burn away at day, none spawn outside the world

**Files changed:**
- `Assets/Script/Enemy/EnemySpawner.cs` (yours): 2 places, each marked `// [Tony]`, your old line kept in a comment.
- Tony's own scripts (not yours): `Enemy/EnemyAnimator.cs` (new `Vanish`), `Effects/DeathEffect.cs` (new `Vanish`),
  `World/WorldBarrier.cs` (new).

**Why:**
1. Tony asked: "monsters that spawn at night: when day comes they vanish into orange particles." Your `DespawnEnemies()`
   (called from `OnDayStarted`) already removed the night's enemies at dawn, but with `Destroy(enemy)` they just
   blinked out.
2. Both worlds now have a barrier at the edge of the map (Tony's `WorldBarrier`). `SpawnEnemy()` picks a spot
   `spawnDistance` (10 m) from the player in a random direction, so near the edge half of the spots were past the
   barrier: the enemy got stuck behind it (or fell, in the old world, where there's no ground past the edge).

**What changed:**

| Where | Was | Now |
|---|---|---|
| `DespawnEnemies()` | `Destroy(enemy);` | `EnemyAnimator.Vanish(enemy);` (`[Tony] replaced`) |
| `SpawnEnemy()`, right after `spawnPosition = player.position + randomDirection * spawnDistance;` | nothing | `if (!WorldBarrier.Inside(spawnPosition, 2f)) spawnPosition = player.position - randomDirection * spawnDistance;` (`[Tony] added`) |

- `EnemyAnimator.Vanish(enemy)`: the enemy waits a random moment (0 - 1.5 s, so they don't all go at once), stops
  (no moving, attacking or being hit), and burns away into orange embers that float up (its `DeathEffect`), then it's
  removed after about 1.7 s. It wasn't killed, so **no coins** and no kill counted for the weapon upgrade. An enemy
  without Tony's `EnemyAnimator` (your old ball enemy) is still destroyed at once, like before.
- Your **Test Despawn Enemies** (⋮ on the EnemySpawner) now shows the burning away too.
- `WorldBarrier.Inside(spot, 2f)` is true when the spot is at least 2 m inside the barrier, and always true in a scene
  without a barrier, so nothing changes there.
- Nothing else in your file changed: night intervals, enemy strength per day, the list of spawned enemies (still
  cleared at dawn) all work as before.

**To undo:** in `EnemySpawner.cs`, put `Destroy(enemy);` back in place of the `[Tony] replaced` line and delete the
`[Tony] added` line in `SpawnEnemy()` (and their comment lines).

Guides: `Docs/Death Effect Setup.md` (the orange embers), `Docs/World Barrier Setup.md`.
