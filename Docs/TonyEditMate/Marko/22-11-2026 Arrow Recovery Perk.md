# Tony's change to Marko's PerkData, 22/11/2026: the Arrow Recovery perk

**Why:** Tony asked that arrows no longer stay in an animal's or a monster's skin, unless the player has a perk from
the epic and legendary boxes. With that perk an arrow in a monster comes back when the player is close, a dead monster
drops its arrows on the floor, and missed arrows stay on the floor. Tony's `ArrowProjectile` does all of it; the perk
only needs one new kind in your `ChestPerkType`, so a Perk Data can be of that kind.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Perks/PerkData.cs` | `ChestPerkType`: a comma after `LongReach` and one new kind at the end, `ArrowRecovery`, with `[Tony]` comments. It's at the end, so every Perk Data keeps its kind. |

Nothing else of yours changed:
- Your `PerkManager` hands the new kind to Tony's `PerkEffects` (its `[Tony]` `default:` line from 21/11/2026), like the
  other new perks.
- Your `EnemyCombat` is hurt by arrows as before (`TakeDamage`; your dodge and coins count). Without the perk an arrow
  that hits a monster disappears; with it the arrow stays in the monster (Tony's ArrowProjectile watches your
  `currentHealth`) and falls out onto the ground when that reaches 0.
- Tony's Tools > World > Build Loot Chests makes `Assets/Config/Perks/Perk_ArrowRecovery.asset` (Max Picks 1) and adds
  it to the epic box's and the legendary box's perk lists (your PerkChest's **Available Perks**).

## How to undo

Delete the `[Tony]` lines under `LongReach` in `ChestPerkType` and the comma after `LongReach`. Remove
`Perk_ArrowRecovery` from the epic and legendary boxes' Available Perks first, then delete that Perk Data file.
