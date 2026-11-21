# Tony's change around your items, rocks, trees and crafting list, 21/11/2026: diamonds and the snow

**Why:** Tony asked for a snow biome in the new world with caves holding diamond and stone rocks, and more chests
(guide: `Docs/Snow Setup.md`). Diamonds make the Diamond tools, so they now have recipes in your crafting list.

**None of your scripts changed** (CraftingMenu.cs, ResourceNode.cs, ItemData.cs's fields...). Tony's tool **Tools >
World > Build Snow Content** (also run by Build New World Scene) made new files next to yours, copied from yours:

| New file | Made from yours | What's different |
|---|---|---|
| `Assets/Config/Items/Diamond.asset` | a copy of your **Iron** item | name Diamond, its own prefab, **Mined With = Gold** (its rock needs a Gold Pickaxe). Your Iron item is untouched. |
| `Assets/Prefabs/Item/Diamond.prefab` | a copy of your **Iron** cube prefab | an ice-blue ShapeColor, ItemPickup = Diamond |
| `Assets/Prefabs/ResourceNodes/Rocks/DiamondNode.prefab` | a **Prefab Variant** of your **IronNode** | Diamond Ore material, Max Health 50, Drop Item Diamond, Drop 1 - 3. Your changes to IronNode (other than those) still reach it. |
| `Assets/Prefabs/ResourceNodes/Rocks/StoneNode Snow.prefab` | a Prefab Variant of your **StoneNode** | only its material (frosty Snow Rock); health and Stone drops are StoneNode's |
| `Assets/Prefabs/ResourceNodes/Trees/TreeNode Snow Pine`, `Tall Snow Pine`, `Snow Spruce`, `Tall Snow Spruce` | Prefab Variants of your **TreeNode** | the tree pack's pines / spruces in its winter colors; your Wood drops, health and gathering stay |

## Changed data (no code)

| What | Before | After |
|---|---|---|
| `Assets/Config/Items/Tools/DiamondPickaxe`, `DiamondAxe`, `DiamondSword` recipes | empty (no Diamond item existed) | the Gold tool + 3 Plank + 6 Diamond (the sword 9 Diamond), like Tony's other tier recipes (Tools > Items > Update Tool Recipes, run without asking) |
| Your **CraftingCanvas** prefab's **Craftables** list | ... the Gold tools | sorted again by Tony's CraftingMenuBuilder.SortList: the 3 Diamond tools join the tools, after the Gold ones |
| `Assets/Config/Items/Gold.asset` (Tony's copy of your Iron, 20/11/2026) **Mined With** | Stone (kept from your Iron by mistake: a Stone Pickaxe mined gold) | Iron, as planned |

## How to undo

- Delete the new files above (and `Assets/Materials/Rocks/Diamond Ore.mat`, `Snow Rock.mat`), then run **Tools > UI >
  Build Crafting Menu** to sort your list without them.
- Clear the three Diamond tools' recipes (Inspector > Recipe > remove the entries).
- Gold's Mined With: set it back in `Assets/Config/Items/Gold`.
