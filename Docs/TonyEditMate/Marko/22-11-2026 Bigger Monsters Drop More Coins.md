# Tony's change to Marko's EnemyCombat, 22/11/2026: bigger monsters drop more coins

**Why:** Tony asked for the night monsters to pay by how hard they are: the slime drops your coins (x), the turtle
1.5x, the Chest Monster 2x and the flying Beholder 3x. Your `Die()` gives `Random.Range(3, 7)` coins (3 - 6) to every
enemy, so a multiplier was needed there.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemyCombat.cs` | **Added** a public field under `currentHealth`, `[Tony] added`: `public float coinMultiplier = 1f;` (Inspector: **Coin Multiplier**). **Replaced** the coin line in `Die()` (your line kept in a comment above it, `[Tony] replaced`): `int coinsEarned = Mathf.RoundToInt(Random.Range(3, 7) * coinMultiplier);` |

With the multiplier at 1 (every enemy prefab, the slime included) your coins are exactly as before: 3 - 6. Tony's
NightMonsters (on your EnemySpawner's object) sets it on each monster when it spawns, from its line's new **Coins**
number (Tools > Enemies > Set Up Night Monsters fills it in):

| Monster | Coin Multiplier | Coins for a kill |
|---|---|---|
| Slime (your Enemy Prefab) | 1 | 3 - 6 (as before) |
| Turtle | 1.5 | 4 - 9 |
| Chest Monster | 2 | 6 - 12 |
| Beholder | 3 | 9 - 18 |

(Rounded to whole coins; on average exactly 1.5x, 2x and 3x.) Tony's extra coins from `MonsterBounty.Killed()` (the
armor set bonus and the Bounty Hunter perk) stay the same for every monster: they're "+N for every monster killed".

## How to undo

In `EnemyCombat.cs`: put your line `int coinsEarned = Random.Range(3, 7);// player can get 1-3 coins per kill` back in
place of the `[Tony] replaced` one in `Die()`, and delete the `coinMultiplier` field and the comment lines above both.
Then Tony's NightMonsters won't compile until its `combat.coinMultiplier = ...` line in `Prepare` is deleted too.
