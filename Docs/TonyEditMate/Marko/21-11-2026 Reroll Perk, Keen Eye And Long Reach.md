# Tony's change to Marko's perks and combat, 21/11/2026: the Reroll perk, Keen Eye and Long Reach

**Why:** after testing the new chest system Tony asked for:
- **REROLL becomes something you unlock**, in both chest systems: no box can reroll until the player has the new
  **Reroll** perk, which the epic box offers (and the legendary box in the new system; the old system has no legendary
  box). After that the boxes reroll as before (old system: the epic box; new system: every box).
- A new **Keen Eye** perk (epic box, and legendary box in the new system): the crosshair turns **yellow** when your
  attack will reach what it points at (a monster, an animal, a tree or a rock), so the player knows a click will hit.
- Once the player has Keen Eye, a new **Long Reach** perk can show up: melee attacks (punches and tools) reach 10 %
  farther (stacks). The old system's rare box offers it; in the new system the common and rare boxes do.
- **Night Power no longer powers the fireball** (only max health, max stamina, speed and melee damage at night).

Everything is marked `// [Tony] added / replaced` with your original code kept in a comment. Tony's own scripts (not
yours): ChestTier (REROLL only after the Reroll perk: Reroll Needs Perk), PerkEffects (what the perks do), Crosshair
(the yellow color), PlayerPerks (their names), IconArt (their pictures), LootChestBuilder (the Perk Data and the boxes'
lists). Guide: `Docs/Loot Chests Setup.md`.

## Files changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Perks/PerkData.cs` | 3 new kinds at the END of `ChestPerkType`: `Reroll`, `KeenEye`, `LongReach` (a comma after `NightPower`), so every Perk Data keeps its kind. |
| `Assets/Script/Player/PlayerCombat.cs` | New getter under `attackRange`: `public float AttackReach => attackRange * PerkEffects.ReachMultiplier;` (Long Reach: x1.1 per pick, else x1). The attack's ray uses it: `Physics.Raycast(ray, out hit, AttackReach)` (was `attackRange`). Tony's crosshair reads `AttackReach` and `playerCamera` and casts the same ray, so yellow = your attack hits. |
| `Assets/Script/Player/FireballProjectile.cs` | `Initialize` is your original line again: `damage = fireballDamage;` (the `[Tony] replaced` line with Night Power's `* PerkEffects.DamageMultiplier` from earlier today is gone). The Vampire lines in `OnCollisionEnter` stay. |
| `Assets/Script/Perks/PerkChest.cs` | One comment line above `RerollAllowed` (no code change): REROLL now also needs the Reroll perk, which Tony's `ChestTier.CanReroll` checks. |
| `Assets/Prefabs/WorldItems/Epic Chest.prefab`, `Legendary Chest.prefab`, `Rare Chest.prefab` (data, by Tony's tool Tools > World > Build Loot Chests) | Your PerkChest's **Available Perks** get the new Perk Data at the end: epic and legendary + `Perk_Reroll`, `Perk_KeenEye`; rare + `Perk_LongReach`. |
| `Assets/Prefabs/WorldItems/Chest.prefab` (data, by the same tool) | Only Tony's ChestTier on it: its **New System Perks** get `Perk_LongReach`. Your PerkChest and its Available Perks don't change. |

New Perk Data next to yours: `Assets/Config/Perks/Perk_Reroll.asset`, `Perk_KeenEye.asset`, `Perk_LongReach.asset`.

## How to undo

- `PlayerCombat.cs`: delete the `AttackReach` lines and put back `if (Physics.Raycast(ray, out hit, attackRange))`
  from the `[Tony] replaced` comment. (Tony's Crosshair uses `AttackReach`: delete its `InReach` method too.)
- `PerkData.cs`: removing the 3 kinds breaks their Perk Data: delete `Perk_Reroll`, `Perk_KeenEye`, `Perk_LongReach`
  too, and their cases in Tony's PerkEffects, IconArt and PlayerPerks.
- `PerkChest.cs`: delete the comment line.
- `FireballProjectile.cs`: nothing to undo (it's your original line).
- To keep REROLL without the perk and change no code: untick **Reroll Needs Perk** on each box prefab's ChestTier.
