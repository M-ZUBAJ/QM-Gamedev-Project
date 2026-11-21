# Tony's change to Turki's crafting list, 20/11/2026: Reinforced Iron armor upgrades and armor set panels

**Files changed:**
- `Assets/Script/Crafting/CraftingMenu.cs` (yours): 5 lines in `Craft()`, each marked `// [Tony] added`. None of your
  lines were changed or removed.
- `Assets/Prefabs/UI/CraftingCanvas.prefab` (yours), when Tony runs **Tools > UI > Build Crafting Menu** or **Tools >
  Items > Build Plate Armor** (Tony's tools): a switched-off `Panel > Scroll > SetPanel` (the look of an armor set's
  box), Tony's **CraftingSetPanels** on the Panel, and **Craftables** sorted with the armor set by set.
- Tony's own scripts (not yours): `PlayerArmor.cs` (LendForUpgrade / FinishUpgrade), `CraftingRow.cs`,
  `CraftingSetPanels.cs` (new), `ItemData.cs` (UpgradedFrom also knows armor).

**Why:** Tony added a second armor set, **Reinforced Iron** (copper colored, 16 % protection for all six, stamina
recharges 100 % faster), crafted from each Iron piece + Copper (e.g. Iron Helmet + 4 Copper = Reinforced Iron Helmet).
1. A player usually WEARS the Iron piece they want to upgrade, and a worn piece isn't in the inventory, so your
   `CanCraft` said "Missing: 1 Iron Helmet". Now the worn piece can be used.
2. With 12 armor pieces in the list, Tony asked for "a panel to cover an armor set so the player sees it easier in the
   craft menu": each set's rows now sit in one box in the set's color, with its name, protection and bonus.

**What changed in `Craft()`:**

| Where | What |
|---|---|
| after the `inv == null` check | `PlayerArmor armor = item.IsArmor ? PlayerArmor.Current : null;` and `if (armor != null) armor.LendForUpgrade(item);`: for an armor upgrade whose lower piece is worn and not in the inventory, Tony's PlayerArmor takes the worn piece off into the inventory, so your recipe check finds it. Anything that isn't armor: nothing happens. |
| in the `!item.CanCraft(inv)` branch, after your message | `armor.FinishUpgrade(item, false)`: the worn piece goes back on (after the message, so it isn't listed as missing). |
| after a successful `inv.AddInto(...)` | `armor.FinishUpgrade(item, true)`: the new piece is worn in its place (it's taken out of the inventory again). |
| in the "Inventory is full!" branch, after the materials are given back | `armor.FinishUpgrade(item, false)`: the worn piece goes back on. |

- Your `oldToolSlot` line now also works for armor: `ItemData.UpgradedFrom` gives the lower piece for the same slot,
  so an upgrade made from a piece in the inventory takes that piece's slot, like the tools.
- The rows count a worn piece too ("1/1 Iron Helmet (worn)", Tony's CraftingRow).
- The set boxes don't touch your script: Tony's CraftingSetPanels moves each set's rows into a box after your
  `FillRows()` made them (your `foreach (Transform child in rows) Destroy(child.gameObject);` still removes everything,
  the boxes included, the next time the list opens).

**To undo:** delete the 5 lines marked `[Tony] added` in `Craft()` (and their comment lines). For the boxes: remove
CraftingSetPanels from `CraftingCanvas > Panel` and delete `Panel > Scroll > SetPanel`.

Guides: `Docs/Armor Setup.md`, `Docs/Crafting Menu Setup.md`.

## Update (later on 20/11/2026): Copper armor first, Iron is the upgrade

Tony felt iron is a higher tier than copper, so the two sets were swapped: **Copper** armor is crafted first (from
Copper, 10 % protection), and **Iron** armor is its upgrade (each Copper piece + Iron, e.g. Copper Helmet + 4 Iron =
Iron Helmet; 16 %). There is no Reinforced Iron set any more. Since 21/11/2026 the whole set's bonus is coins (copper
+1, iron +2 for every monster killed) instead of faster stamina; nothing in your files changed for that.

- `CraftingMenu.cs` (yours): only the example in Tony's `[Tony] added` comment in `Craft()` changed (it said "Iron
  Helmet + 4 Copper = Reinforced Iron Helmet"). No code line changed: the 5 `[Tony] added` lines work for any upgrade.
- `CraftingCanvas.prefab` (yours): **Craftables** still holds the same 12 item files. Tony's tool renamed them (the old
  Iron pieces are the Copper pieces now, the old Reinforced Iron pieces are the Iron pieces), so the list keeps its
  links and its order: Copper first, then Iron.
- To undo: as above.
