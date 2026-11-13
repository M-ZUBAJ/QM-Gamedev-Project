# Tony's edit to Marko's code, 12/11/2026: tier tools replace the Sword / Axe / PickAxe

Every changed line in your scripts is marked `// [Tony]`, and each line of yours that was replaced is kept in a comment
right above its new version.

**Why:** Tony asked to replace your three placeholder tools with Abduo Bricks' **Low Poly Tool Pack**:
- **15 tools:** a pickaxe, axe and sword in Wood, Stone, Iron, Gold and Diamond.
- **Rules:** tier rules for mining, and stronger swords per tier.
- **Look:** the tools held in the hands, and swing animations.

Tools are now Item Data with **Item Type = Tool** (Kind + Tier), made by Tony's **Tools > Items > Build Tier Tools**.
The rules are in Tony's `Assets/Script/Tool/ToolRules.cs`. Guide: `Docs/Tool Setup.md`.

---

## 1. `Assets/Script/Player/PlayerCombat.cs`

**Your damage numbers and if / switch code are unchanged.** A wooden tool does exactly your numbers. What changed:

| Where in `Attack()` | Change |
|---|---|
| Tony's punch-animation lines at the top | Now: `ToolStats tool = ToolRules.ToolIn(...)` (null = a bare hand), `PlayerAction swing = ToolRules.SwingFor(tool)` and `PlayerAnimator.Play(swing)`. Nothing / a non-tool = Punch, sword or axe = Attack (one-handed swing), pickaxe = Mine (mining swing). Before, only bare hands played anything. |
| `equippedItem = inventory.ActiveItem.id;` | **Replaced** by `equippedItem = ToolRules.WeaponName(tool);`. It gives your code the names it checks, `"Sword"`, `"Axe"` or `"PickAxe"`, from the tool's kind, whatever its tier (the new ids are `WoodenSword` ... `DiamondSword`). |
| Enemy branch, after your damage code | **Added** `damage *= ToolRules.Boost(tool, ToolKind.Sword);`: +25 % per tier above Wood (Stone x1.25, Iron x1.56, Gold x1.95, Diamond x2.44). |
| Enemy hit | **Replaced** (it was Tony's punch line from earlier today; your original `enemy.TakeDamage(damage);` is kept in its comment) by `PunchEffect.Hit(enemy, hit, () => enemy.TakeDamage(damage), swing);`. **Every swing** now hurts when it lands, not only punches. |
| Resource branch, first lines | **Added:** if `!ToolRules.CanMine(tool, resource)`, show *"Needs a Stone Pickaxe or better"* and `return`. Copper and iron need a Stone pickaxe (gold: Iron, diamond: Gold). A hand, axe or sword can't break them. |
| Resource branch, after your switch | **Added** `damage *= ToolRules.Boost(tool, ToolRules.JobFor(resource));`: the axe on trees, the pickaxe on rock and ores, and the sword on animals get +25 % per tier. |
| Resource hit | **Replaced** the same way: `PunchEffect.Hit(resource, hit, () => resource.TakeDamage(damage), swing);`. |

**Good to know:**
- **Timing:** each swing lands after its delay on Tony's PunchEffect: Impact Delay (punch), Swing Delay (sword / axe),
  Mine Delay (pickaxe). The delays are measured from the animations. Your attack cooldown (0.5 s) is longer than all
  of them.
- **The wind burst** still comes only with punches.
- **Without Tony's PunchEffect** on the Player, hits land at once like before.

## 2. `Assets/Script/Player/PlayerFireball.cs`

`inventory.ActiveItem.id != "Sword"` is **replaced** by `!ToolRules.IsSword(inventory.ActiveItem)`, so any sword (Wooden
... Diamond) can cast the fireball. Your message "Equip your sword to cast Fireball." is unchanged.

## 3. GameScene and your files (no code changed)

The tool asked before doing this ("Build + replace Marko's tools"):
- **Removed from the Player:** your **GiveSwordForTesting** and **EquippedWeaponDisplay** components. Their script files
  are still in the project, unused. Delete them when you like.
- **Testing replacement:** GameRules > **Tool Test** (default off) gives one of every tool.
- **Holding replacement:** Tony's ToolInHand puts the selected tool in the character's hand.
- **Removed from the Main Camera:** your three weapon models (instances of `Assets/Prefabs/Weapons/Sword`, `Axe`,
  `PickAxe`).
- **Moved to the trash:** `Assets/Prefabs/Weapons/Sword.prefab`, `Axe.prefab` and `PickAxe.prefab` (your computer's bin;
  git has them too).

**Not changed:** your hard-coded "SWORD / AXE / PICKAXE" labels in `SlotUI.Refresh` stay. No item has those ids any more,
so they simply never show. The new tools have pictures.

---

**To undo:**
1. In PlayerCombat and PlayerFireball, put back the lines from the `[Tony] replaced` comments and delete the `[Tony]`
   lines.
2. Restore the files from the bin, or from git:
   `git checkout 5fa06fc -- Assets/Prefabs/Weapons Assets/Config/Items/Sword.asset Assets/Config/Items/Axe.asset Assets/Config/Items/PickAxe.asset`.
3. Add your two components back to the Player, and the three weapon models under the Main Camera.

## For Marko: weapons from your code

- **What's in the hand:** `ToolRules.ToolIn(PlayerInventory.Instance.ActiveItem)` gives `kind` (Pickaxe / Axe / Sword)
  and `tier` (Wood ... Diamond), or null for a bare hand.
- **A sword?** `ToolRules.IsSword(slot)`.
- **The tier bonus:** `ToolRules.Boost(tool, ToolKind.Sword)`.
- **A swing animation:** `PlayerAnimator.Play(PlayerAction.Attack)` (or `Mine`).
