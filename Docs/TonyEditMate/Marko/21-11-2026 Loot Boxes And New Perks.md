# Tony's change to Marko's perks and combat, 21/11/2026: loot boxes and new perks

**Why:** Tony asked for three kinds of perk chest: the **common box** (your chest, as it is now: 2 coins, +1 for every
perk bought), a **rare box** (10 coins, x1.5) and an **epic box** (100 coins, x2, can reroll its perks), with new perks:
Second Wind, Precision, Spring Legs (common); Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler (rare);
Last Stand, Critical Strike, Glide (epic). Some can be picked only once a life, some only after another one
(Precision needs Critical Strike, Spring Legs needs Glide). Your four perks and your chest work as before.

Everything is marked `// [Tony] added / edited / replaced` with your original code kept in a comment. Tony's own new
scripts (not yours): ChestTier, PerkRules, PerkEffects, PlayerGlide, Knockback, MonsterBounty. Guide:
`Docs/Loot Chests Setup.md`.

## Files changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Perks/PerkData.cs` | 11 new kinds at the END of `ChestPerkType` (SecondWind, CriticalStrike, SpringLegs, BountyHunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler, LastStand, Glide), so your Perk Data keep their kinds; a comma after `Fireball`. Two new fields under "Offered when ([Tony])": `requires` (only offered once the player has that perk) and `maxPicks` (1 = once a life, 0 = no limit). Empty / 0 = offered like before. |
| `Assets/Script/Perks/PerkManager.cs` | A `default:` case in `ApplyPerk`: every new kind goes to Tony's `PerkEffects.Apply(perk)` (your four cases are unchanged). Tony's own HUD line now records the Perk Data itself (`PlayerPerks.Record(perk)`), and Tony's old `HudPerk` helper is gone: Tony's PlayerPerks uses your `ChestPerkType` now. |
| `Assets/Script/Perks/PerkChest.cs` | Finds Tony's `ChestTier` in `Start` (can be missing); `OpeningCost` = its price when there is one (else your random Min / Max Cost) and your Start log prints it; `TryOpen` needs at least 1 perk (was 3), only rolls perks Tony's `PerkRules.CanOffer` allows, rolls again if a kept perk can't be offered any more, says "Nothing left in this box for you." when none are left, and stops rolling when the pool runs out (1 or 2 cards); `OnPerkSelected` pays `OpeningCost` and then `tier.Bought()` (the next box of that kind costs more); new `RerollAllowed`, `RerollCost`, `RerollWouldChange`, `TryReroll` (Tony's REROLL button: pays, new perks, refreshes the cards); `Tier` getter; your `GrantRandomPerk`'s Tony lines use `ChestPerkType`. |
| `Assets/Script/UI/Perks/PerkSelectionUI.cs` | `Show` accepts 1 to 3 perks (was exactly 3); only the cards that get a perk are checked; your card loop moved into `FillCards` (the same steps), which hides the cards without a perk; new `Refresh(perks)` fills the cards again without touching the mouse or the menu state (a reroll), and `CardOf(i)`. |
| `Assets/Script/Player/PlayerCombat.cs` | In both damage branches (enemy and resource), right before `PunchEffect.Hit`: `bool critical = PerkEffects.RollCritical(); if (critical) damage *= PerkEffects.CriticalMultiplier;` and `critical` passed to `PunchEffect.Hit` (a red burst). No Critical Strike perk = never critical. |
| `Assets/Script/Enemy/EnemyCombat.cs` | In `Die`, after your coins: `MonsterBounty.Killed();` = extra coins from Tony's armor set bonus (copper set +1, iron set +2) and the Bounty Hunter perk. |
| `Assets/Script/Enemy/EnemyMovement.cs` | In Tony's own FixedUpdate block: `if (Knockback.IsPushed(body)) return;` so a punch's push isn't cancelled at once (punches push monsters back a little now; Brawler pushes farther). |
| `Assets/Prefabs/WorldItems/Chest.prefab` (data, by Tony's tool Tools > World > Build Loot Chests) | Tony's **ChestTier** added (Common box, 2 coins, +1); Second Wind, Precision and Spring Legs added to **Available Perks** (your four stay). While ChestTier is on it, your Min / Max Cost aren't used. |

New prefabs next to yours: `Assets/Prefabs/WorldItems/Rare Chest.prefab` and `Epic Chest.prefab` (your PerkChest on
DuNguyn's loot boxes).

## How to undo

- Delete every line marked `[Tony] added` (and its comment lines) in the files above, and put back the code in each
  `[Tony] replaced` / `[Tony] edited` comment.
- `PerkData.cs`: removing the 11 kinds breaks the new Perk Data (`Assets/Config/Perks/Perk_SecondWind`...): delete them
  too, and Tony's scripts that use those kinds (PerkEffects, IconArt, PlayerPerks' names).
- `Chest.prefab`: remove ChestTier and the 3 new perks from Available Perks.
