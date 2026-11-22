# Tony's change to Marko's EnemySpawner, 22/11/2026: Max Enemy Number works (night monsters stop piling up)

> **UNDONE the same day (22/11/2026).** Tony found the slowdown came from exploring, not from the monsters: playing in
> one place with 10 - 12 slimes around didn't slow down, walking through the new world did (every visited chunk kept
> all its trees and rocks; that's fixed in Tony's WorldGenerator). He wanted the nights as busy as before, so your
> `SpawnThroughoutNight` is your own code again (`if (enemyCounter < maxEnemyNumber)`), the two methods
> (`AliveEnemies`, `MostAtOnce`) are gone, and Tony's NightMonsters lost its Most At Once and Leave Distance.
> Nothing of this change is left in your file. The text below is what it was, if a limit is ever wanted again.
>
> **Later the same day the limit came back for Easy only** (Tony: "Keep this right now to normal mode, the previous is
> easy"), with a smaller change: see `22-11-2026 Easy Keeps At Most 20 Monsters.md`. On Normal and Hard your
> `SpawnThroughoutNight` runs as you made it.

**Why:** Tony reported that the game gets laggier the longer it's played. One big reason was the night: your
`SpawnThroughoutNight` only spawns while `enemyCounter < maxEnemyNumber`, but `enemyCounter` is never counted up, so
**Max Enemy Number** (40) never stopped a spawn. On nights 3 - 5 a monster came every 5 seconds for the whole night (a
night is about 4.6 minutes with 10-minute days), about 55 by dawn, each one walking, animating and pushing the others,
and the ones left far behind kept chasing until morning. The game got slower every night.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemySpawner.cs` | **Edited** the check in `SpawnThroughoutNight` (your line kept in a comment above it, `[Tony] edited`): `if (enemyCounter < maxEnemyNumber && AliveEnemies() < MostAtOnce())`. **Added** two methods under Tony's `GroundHeight`, each line `[Tony] added`: `AliveEnemies()` (removes the destroyed ones from your `spawnedEnemies` list and counts the rest: killed, burnt away at dawn or taken away = gone) and `MostAtOnce()` (your Max Enemy Number, or Tony's `NightMonsters > Most At Once` (20) when that's lower). |

So your Max Enemy Number now means what your comment says ("total number of enemies allowed to exist in game at any
one time"), and Tony's NightMonsters lowers it to 20 for speed (change **Most At Once** on the EnemySpawner object to
allow more). Your `enemyCounter` check is still there (always true), so nothing else changes. Your Test Spawn Enemy
button still spawns one whenever you click it.

Tony's NightMonsters (on the same object) also takes away, every 2 seconds, a monster it saw spawn that's farther than
90 m from the player and out of their sight (no coins: nobody killed it), so ones left far behind don't take a place.
Your scripts aren't changed for that.

## How to undo

In `EnemySpawner.cs`: put your line `if (enemyCounter < maxEnemyNumber)` back in place of the `[Tony] edited` one, and
delete the `AliveEnemies` and `MostAtOnce` methods (the lines marked `[Tony] added` under `GroundHeight`).
