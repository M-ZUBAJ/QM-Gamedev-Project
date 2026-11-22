# Tony's change to Marko's EnemySpawner, 22/11/2026: monsters keep coming after night 5

**Why:** Tony asked for nights 6 and later to use night 5's spawn interval. Your `GetSpawnInterval` has a setting for
nights 1 - 5 and returned 0 for every later night, so `OnNightStarted` logged "No enemy spawning configured for night 6"
and no monsters came at all. The default game is won on day 6 (5 days to win), so it never got that far, but the NEW
GAME page lets the player pick 1 - 30 days to win: with 10 days, nights 6 - 10 were empty.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemySpawner.cs` | **Replaced** the `default:` line of the switch in `GetSpawnInterval` (your line kept in a comment above it, `[Tony] replaced`): `default: return night > 5 ? night5SpawnInterval : 0f;` Every night after night 5 now uses your **Night 5 Spawn Interval** (5 s). Night 0 or below can't happen (the day count starts at 1) and still returns 0. |

Nothing else in your file changed: the night still starts and stops the same way, and your health / damage scaling
(50 + 10 and 10 + 2 for every day passed) keeps growing on the later nights, so they get harder. On Normal and Hard
nothing limits how many are out at once (your `enemyCounter` is never counted up, so Max Enemy Number doesn't stop
spawns); on Easy at most 20 are (see `22-11-2026 Easy Keeps At Most 20 Monsters.md`).

## Also changed today (Tony's scripts only, your files untouched)

- **Tony's NightMonsters** (on your EnemySpawner's object) picks which monster each spawn is. Its shares grow every
  night (the turtle 30 % on night 2, +5 % a night...), and until now its list was read top to bottom, so once the shares
  added up to more than 100 % (night 6 on) the last monsters were squeezed out: no Beholders from night 8. A new
  **Most Share** setting (95 %) fixes that: the shares are shrunk evenly to fit it, so from night 6 on about 43 %
  turtles, 30 % Chest Monsters, 22 % Beholders and 5 % your slimes. Nights 1 - 5 are exactly as before.
- **Difficulty on the NEW GAME page** (no change to your file: your intervals are public settings): when the game
  loads, Tony's `GameSetup` multiplies your **Night 1 - 5 Spawn Interval**: x2 on Easy (half as many monsters), / 1.5
  on Hard, and Peaceful turns `spawnAtNight` off. Keep tuning the intervals in GameScene as Normal; the NEW GAME page
  reads them when GameScene is saved and explains them ("A monster every 10 seconds on night 1, every 7 on night 2 and
  every 5 from night 3 on.").

## How to undo

In `EnemySpawner.cs`, `GetSpawnInterval`: put your line `default: return 0f;` back in place of the `[Tony] replaced`
one, and delete the comment lines above it. Nights after night 5 then have no monsters again.
