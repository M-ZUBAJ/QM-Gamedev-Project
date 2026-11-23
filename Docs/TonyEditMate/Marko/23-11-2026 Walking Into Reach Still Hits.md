# Tony's change to Marko's PlayerCombat, 23/11/2026: walking into reach still hits

**Why:** Tony clicked to attack while an enemy was a little too far away and walked toward it during the swing. The
fist (or tool) came down right on the enemy, but nothing happened. Your `Attack()` picks its target with one ray at
the click (`AttackReach`, 2.5 m): when that ray finds nothing, the swing has no target at all, even if the player is
in reach by the time the blow lands (Tony's PunchEffect lands every hit after the swing's delay, about 0.3 s).
Tony wants the blow to count where it actually lands.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Player/PlayerCombat.cs` | **Edited** one line in `Attack()`, the ray (marked `[Tony] edited (23/11/2026)`, the line before it kept in the comment): `Physics.Raycast(ray, out hit, AttackReach + PunchEffect.LungeReach)` instead of `Physics.Raycast(ray, out hit, AttackReach)`. That line was already Tony's (the Long Reach perk, 21/11/2026). |

How it works:

- The ray now looks `PunchEffect.LungeReach` (3 m by default) past the reach. Everything after it in your code is the
  same: the damage, the sword upgrade, the perks, `PunchEffect.Hit(...)`.
- Tony's PunchEffect sees how far away the target was. **In reach at the click:** hit when the blow lands, exactly
  as before. **Past the reach:** when the blow lands it checks again: if the spot aimed at is now within the reach
  (+ 0.3 m) and in front of the camera, the hit lands (damage, burst, knockback); if the player didn't get close
  enough, it's a miss (no damage, no burst).
- Without a PunchEffect on the Player (or with it switched off), `LungeReach` is 0, so your ray is exactly as long
  as before (a hit then lands at once and can't be checked again).
- Stamina, the cooldown and the swing animation are unchanged: they already happened at the click.

One small side effect: with the wrong tool, the "Needs a Stone Pickaxe or better" message can now show for an ore up
to 3 m past the reach, since your `CanMine` check comes right after the ray.

## How to undo

In `PlayerCombat.cs`, `Attack()`: put `if (Physics.Raycast(ray, out hit, AttackReach))` back in place of the
`[Tony] replaced` line and delete the `[Tony] edited (23/11/2026)` comment lines above it. Or set the Player's
PunchEffect > Lunge Reach to 0 (same result, no code change).
