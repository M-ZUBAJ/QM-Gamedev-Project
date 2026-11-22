# Tony's changes to Turki's items and trees, 22/11/2026: Spruce Wood, tough snow trees and the Spruce Bow

**Why:** Tony asked for the snow biome's trees to have 300 health and to give **spruce wood** instead of wood ("shape
like wood but the color is spruce"), for those trees to need an **iron axe or better**, and for a better bow that
shoots faster than the Wooden Bow, made from **the Wooden Bow + 20 spruce + 15 iron** (first 20 spruce + 20 iron;
changed the same day). None of your scripts changed. Only data did: a new item copied from yours, the numbers on the
snow trees (Prefab Variants of your TreeNode), and your crafting list.

## What changed

| What (yours, or made from yours) | What changed |
|---|---|
| **New item: Spruce Wood**, `Assets/Config/Items/SpruceWood.asset` | A copy of your `Wood` item (Display Name "Spruce Wood", Item Id `SpruceWood`, a raw material, no recipe) with **Mining > Mined With = Iron**. Made by Tony's **Tools > World > Build Snow Content** (`SnowBuilder.MakeSpruceWood`), only if missing; Mined With is only set while it's None. |
| **New prefab: Spruce Wood**, `Assets/Prefabs/Item/SpruceWood.prefab` | A copy of your `Wood` prefab (the same log shape) with its ShapeColor a darker spruce brown (0.36, 0.24, 0.13; your Wood is 0.52, 0.33, 0.22) and its ItemPickup's Data = Spruce Wood. |
| The snow's 4 trees: `Assets/Prefabs/ResourceNodes/Trees/TreeNode Snow Pine`, `TreeNode Tall Snow Pine`, `TreeNode Snow Spruce`, `TreeNode Tall Snow Spruce` (Tony's Prefab Variants of your `TreeNode`) | Their **ResourceNode** now overrides two of TreeNode's values: **Drop Item = Spruce Wood** (was your Wood) and **Max Health = 300** (was your 30). Set by `GrasslandTreeBuilder.BuildSnowTrees` through SerializedObject (your field names `dropItem` and `maxHealth`). Drop Min / Max (3 - 5), the Apple bonus and everything else still come from your TreeNode. The grassland trees and your TreeNode itself are unchanged. |
| Your crafting list (`CraftingCanvas` > CraftingMenu > **Craftables**) | Tony's **Tools > Items > Build Bow And Arrows** adds the **Spruce Bow** (`Assets/Config/Items/Tools/SpruceBow`: Wooden Bow + 20 Spruce Wood + 15 Iron; a Tool of Kind Bow, Tier Iron) after the Iron tools (CraftingMenuBuilder.SortList, as before). With a tool in its recipe it's an upgrade (`ItemData.UpgradedFrom` = the Wooden Bow), so your `Craft` (its `[Tony]` lines from 19/11 and 21/11) uses the Wooden Bow up and puts the Spruce Bow in its slot. |

Your `ResourceNode.Break` already gives whatever Drop Item says, so chopping a snow tree now puts 3 - 5 Spruce Wood in
the inventory ("+4 Spruce Wood"), and Tony's Lumberjack perk (your `[Tony]` lines from 21/11/2026) gives +1 Spruce
Wood there. Spruce Wood has its own Item Id, so a recipe asking for Wood (planks, the Wooden tools...) doesn't take it.

**The iron axe:** Tony's `ToolRules` used Mined With only for rocks (the pickaxe an ore needs). Since 22/11/2026 it
reads it for trees too, with an axe: a tree whose drop has a Mined With needs an axe of that tier or better. Only
Spruce Wood has one (Iron), so only the snow's trees are affected: a hand, a Wooden / Stone Axe or another tool gets
"Needs an Iron Axe or better" (Marko's PlayerCombat already asks `ToolRules.CanMine` before hitting a ResourceNode).
Your trees dropping Wood (Mined With None) are chopped by anything, as before. Your ResourceNode is not changed.

## How to undo

- Spruce Wood: delete `Assets/Config/Items/SpruceWood.asset` and `Assets/Prefabs/Item/SpruceWood.prefab`, and the
  Spruce Bow (`Assets/Config/Items/Tools/SpruceBow.asset`, `Assets/Prefabs/Item/Tools/SpruceBow.prefab`). Then run
  Tools > UI > Build Crafting Menu (or Tools > Items > Update Tool Recipes) to sort your list without it.
- The iron axe only: set Spruce Wood's **Mining > Mined With** back to None (Build Snow Content would set it to Iron
  again while it's None: change `SpruceAxe` in `SnowBuilder.cs` too).
- The snow trees: open each of the 4 prefabs, right-click **Drop Item** and **Max Health** on its ResourceNode >
  **Revert** (back to your TreeNode's Wood and 30). Running Build Snow Content would set them again, unless
  `SnowBuilder.BuildAll` passes `null, 0f` to `GrasslandTreeBuilder.BuildSnowTrees`.
