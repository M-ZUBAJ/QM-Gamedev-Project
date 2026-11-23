# Tony's change to Marko's EnemyCombat, 23/11/2026: humans killed count apart from monsters

**Why:** Tony is adding **human enemies** to the new world (`Docs/Human Enemies Plan.md`): survivors made of Polytope
Studio's bodies and armor. They use your `EnemyCombat` for their health, so every attack the player has (punches,
tools, arrows, the fireball, Thorns) hurts them with no change to your attack code. But your `Die()` told Tony's
`MonsterBounty` about every kill the same way, so the victory and death pages counted a dead human as a "Monster
killed". Tony wants them counted apart, as "Humans killed".

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemyCombat.cs` | **Edited** the `[Tony]` line in `Die()` (the old line kept in a comment above it, `[Tony] edited`): `MonsterBounty.Killed(gameObject);` instead of `MonsterBounty.Killed();`. It now says who died. Tony's MonsterBounty checks for his `HumanBrain` script: a human counts as "Humans killed", anything else as "Monsters killed", exactly as before. The extra coins (the armor set's coins per kill, Bounty Hunter) are the same for both. |

That line was Tony's already (added 21/11/2026 for the armor set coins); none of your own lines changed.

## Also good to know (no change to your files)

- On the human prefabs your `EnemyCombat` has **Coin Multiplier 0** (the field Tony added on 22/11/2026), so your
  3 - 6 coins are 0 for them. Tony's `HumanLoot` pays them instead, by the gear they wore (copper 1, iron 2, gold 3,
  diamond 4 a piece) and drops the matching materials.
- They also have your **Miss Chance** (5 %), so they dodge now and then like the monsters.

## How to undo

In `EnemyCombat.cs`, `Die()`: put `MonsterBounty.Killed();` back in place of the `[Tony] edited` line and delete the
comment lines above it. Human kills then count as monster kills again.
