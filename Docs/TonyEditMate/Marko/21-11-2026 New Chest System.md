# Tony's change to Marko's perks and combat, 21/11/2026: the new chest system and four more perks

**Why:** Tony asked for a second way the loot boxes work, picked on the New Game page (Testing: **Old chests** /
**New chests**, New is the default), plus four new perks:
- **New chests:** the common box and the epic box give **one random perk** (a slot-machine roll in a pop-up, which can
  be skipped; then BUY or REROLL; nothing is paid until BUY, and E or walking away closes it), the rare box and a new
  **legendary box** (150 coins, x2, desert only) let you choose 1 of 3, **every box can reroll**, and the common and rare
  boxes offer the old common and rare perks together.
- **Old chests:** exactly as your chests work now (choose 1 of 3; REROLL only at the epic box).
- Both: when a perk is bought the box bursts into particles in its color (common gold, rare blue, epic purple,
  legendary orange), and twice as many boxes spawn.
- New perks: **Attack Speed** (rare box: attack 10 % faster, stacks), **Vampire** (heal 5 % of the damage you do),
  **Thorns** (a monster that hits you takes 10 % of that hit back), **Night Power** (+10 % max health, max stamina, speed
  and damage at night; since the later change of the same day only melee damage, not the fireball); the last three are
  in the epic and legendary boxes and stack.
- **Last Stand** is now used up when it saves you: it leaves the HUD and a box can offer it again.

Everything is marked `// [Tony] added / edited / replaced` with your original code kept in a comment. Tony's own scripts
(not yours): ChestTier (the systems, prices and burst), PerkRollUI (the roll pop-up) + PerkRollBuilder, PerkEffects
(what the perks do), PlayerHealth / PlayerStamina / PlayerController (Night Power's boosts), PlayerPerks (Lose).
Guide: `Docs/Loot Chests Setup.md`. The first loot box changes: `21-11-2026 Loot Boxes And New Perks.md`.

## Files changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Perks/PerkData.cs` | 4 new kinds at the END of `ChestPerkType`: `AttackSpeed`, `Vampire`, `Thorns`, `NightPower` (a comma after `Glide`), so every Perk Data keeps its kind. The `maxPicks` tooltip (Tony's line) now says "how many of it the player can have at a time". |
| `Assets/Script/Perks/PerkChest.cs` | New `rollSeen`, `RandomRoll` (Tony's ChestTier says this box rolls one perk in the new system) and `PerksNow()` (the perks it offers now: ChestTier's New System Perks in the new system, else your Available Perks). `TryOpen`: the "nothing in the list" check, the pool, `OtherPerksLeft` and `TryReroll` use `PerksNow()` instead of `availablePerks`; a random box rolls 1 perk (the loop `i < (RandomRoll ? 1 : 3)`, was `i < 3`) and shows it in Tony's `PerkRollUI.Instance.Open(...)` instead of your panel (the roll plays the first time; without the pop-up in the scene your panel shows that one card). Update: E doesn't close while the roll is still rolling (E skips it then: `!PerkRollUI.BlocksClose`). `PanelOpen()` also counts the roll pop-up; `ClosePanel()` hides whichever is open. `TryReroll`: a random box rolls 1 perk again and the pop-up plays the roll again (else your `Refresh` as before). New `CanBuyRolled` / `BuyRolled()`: the pop-up's BUY = `OnPerkSelected(choices[0])` (the price, the burst, the perk, the chest goes). |
| `Assets/Script/Player/PlayerCombat.cs` | `nextAttackTime = Time.time + attackCooldown / PerkEffects.AttackSpeedMultiplier;` (was `+ attackCooldown`): Attack Speed. In both damage branches, before the critical roll: `damage *= PerkEffects.DamageMultiplier;` (Night Power: x1.1 at night per pick, else x1). |
| `Assets/Script/Enemy/EnemyAttack.cs` | After `playerHealth.TakeDamage(attackDamage);`: `PerkEffects.Reflect(this, attackDamage);` (Thorns: this enemy takes 10 % per pick back through your `EnemyCombat.TakeDamage`, so its dodge chance still counts). |
| `Assets/Script/Player/FireballProjectile.cs` | ~~`Initialize`: `damage = fireballDamage * PerkEffects.DamageMultiplier;` (was `damage = fireballDamage;`: Night Power).~~ **Undone later the same day** (Tony: Night Power no longer powers the fireball): `Initialize` is your original line again, see `21-11-2026 Reroll Perk, Keen Eye And Long Reach.md`. `OnCollisionEnter`: `PerkEffects.Hurt(enemy, () => enemy.TakeDamage(damage));` and the same for a resource (was `enemy.TakeDamage(damage);` / `resource.TakeDamage(damage);`): the same hit, and with Vampire the player heals 5 % of the damage it really did (animals and monsters only). |
| `Assets/Prefabs/WorldItems/Chest.prefab` (data, by Tony's tool Tools > World > Build Loot Chests) | Tony's **ChestTier** on it gets the new system's settings once: gold color (was grey-white), Random In New System, Reroll In New System, New System Perks (your 4 + Tony's 3 common + the 7 rare perks), the burst (DeathParticles, 110). Your PerkChest settings and Available Perks don't change. |

New prefab next to yours: `Assets/Prefabs/WorldItems/Legendary Chest.prefab` (your PerkChest on DuNguyn's orange loot
box). In the old system it removes itself when the game starts.

## How to undo

- Delete every line marked `[Tony] added` (and its comment lines) in the files above, and put back the code in each
  `[Tony] replaced` / `[Tony] edited` comment.
- `PerkData.cs`: removing the 4 kinds breaks their Perk Data (`Assets/Config/Perks/Perk_AttackSpeed`, `Perk_Vampire`,
  `Perk_Thorns`, `Perk_NightPower`): delete them too, and the cases in Tony's PerkEffects, IconArt and PlayerPerks.
- To keep only the old system without undoing code: untick **New Chests** on the scene's GameRules (and leave the New
  Game page on Old chests).
