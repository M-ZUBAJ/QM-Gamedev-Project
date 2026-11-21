# Tony's change to Turki's crafting list, 21/11/2026: gold tools and gold armor

**Why:** Tony asked for gold tools and gold armor in the crafting list. Gold comes from the gold rocks in the new
world's desert caves (an Iron Pickaxe or better).
- **Gold tools:** Gold Pickaxe, Gold Axe and Gold Sword already existed as items (Tony's Tools > Items > Build Tier
  Tools) but had no recipe. Now each is made from the Iron one + 3 Plank + 6 Gold (the sword: 9 Gold), like the
  Wooden > Stone > Iron chain. The Iron tool is used up and the gold one takes its slot (your `Craft()` already does
  that through Tony's `[Tony]` lines from 19/11/2026).
- **Gold armor:** a third set after Copper and Iron: Gold Helmet, Gold Shoulders, Gold Chestplate, Gold Gloves, Gold
  Leggings and Gold Boots, each made from its Iron piece + Gold (4 / 3 / 6 / 2 / 5 / 3). All six protect 25 % and give
  +4 coins for every monster killed and 10 % off every loot box. A worn Iron piece can be upgraded where it's worn
  (your `Craft()` already calls Tony's `PlayerArmor.LendForUpgrade / FinishUpgrade`).
- **Protection follows Tony's formula** a[n] = 5/2 n^2 - 5/2 n + 10 % per set: copper 10 %, iron 15 % (it was 16 %),
  gold 25 %.

**Your scripts are NOT changed.** Only data in your prefab:

| File | What changed |
|---|---|
| `Assets/Prefabs/UI/CraftingCanvas.prefab` (your CraftingMenu's **Craftables** list, by Tony's tools Tools > Items > Update Tool Recipes and Tools > Items > Build Plate Armor, through `CraftingMenuBuilder.SortList`) | `GoldPickaxe`, `GoldAxe`, `GoldSword` added after the Iron tools, and the 6 gold armor pieces after the iron set (in their own gold box, Tony's CraftingSetPanels). Your other items keep their places at the top. |

Tony's own files: `Assets/Config/Items/Tools/Gold*.asset` (recipes), `Assets/Config/Items/Armor/Gold*.asset` +
`Assets/Prefabs/Item/Armor/Gold*.prefab` (new), the iron pieces' Protection (2.25 % each, 3.75 % for the chestplate).
Guides: `Docs/Tool Setup.md`, `Docs/Armor Setup.md`.

**To undo:** remove the 9 gold entries from CraftingMenu > **Craftables** on the CraftingCanvas prefab (or empty the
gold items' **Recipe** and run **Tools > UI > Build Crafting Menu**, which only lists items with a recipe).
