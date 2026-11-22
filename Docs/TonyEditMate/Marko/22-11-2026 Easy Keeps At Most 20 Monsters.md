# Tony's change to Marko's EnemySpawner, 22/11/2026: Easy keeps at most 20 monsters

**Why:** Earlier today Tony capped every night at 20 monsters alive at once (log `22-11-2026 Night Monsters Stop Piling
Up.md`), then took the cap out again: the game's slowdown came from exploring, not from the monsters, and he wanted the
busy nights back. Having played both, he asked to keep the busy nights as **Normal** and the capped ones as **Easy**
("Keep this right now to normal mode, the previous is easy").

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemySpawner.cs` | In `SpawnThroughoutNight`: **added** one line, `NightMonsters limit = GetComponent<NightMonsters>();`, and **edited** your check (your line kept in a comment above it, `[Tony] edited`): `if (enemyCounter < maxEnemyNumber && (limit == null \|\| limit.RoomForMore()))` |

`RoomForMore()` is in Tony's NightMonsters (on your EnemySpawner's object). On **Normal** and **Hard** it always says
yes, so your spawner works exactly as you made it: a monster every Night N Spawn Interval all night, and the ones not
killed gather until dawn (your `enemyCounter` is never counted up, so Max Enemy Number doesn't stop spawns).

On **Easy** (the NEW GAME page sets it when the game loads, through Tony's GameSetup) it says no while 20 night
monsters are alive, and NightMonsters also takes away one that's more than 90 m from the player where they can't see
it (no coins). Easy's spawn intervals are doubled too (your public settings, no change to your file).

To try it without the title screen: select your EnemySpawner object > NightMonsters > **Most At Once** (0 = no limit)
and **Leave Distance** (0 = never).

## How to undo

In `EnemySpawner.cs`, `SpawnThroughoutNight`: put your line `if (enemyCounter < maxEnemyNumber)` back in place of the
`[Tony] edited` one, and delete the `NightMonsters limit = ...` line and the comment lines above them. Easy then has no
limit either (only its slower spawning).
