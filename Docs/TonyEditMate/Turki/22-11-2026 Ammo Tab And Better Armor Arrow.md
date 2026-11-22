# Tony's change to Turki's crafting list, 22/11/2026: an Ammo tab, a blue arrow on weaker armor

**Why:** Tony asked for two things in the crafting list:
- **An Ammo tab**, so the arrows (Stone Arrow ... Diamond Arrow) are quick to find. They sat under Tools with the bow;
  now the tabs are All, Materials, Food, Tools, **Ammo**, Armor. The Wooden Bow stays under Tools.
- **A mark on a weaker armor piece while the player already has a better one** for the same slot (worn or in the
  inventory): a blue up arrow on its picture and, in blue, "(wearing Iron Helmet)" or "(you have Iron Helmet)" after
  its name. E.g. while wearing the Iron Helmet, the Copper Helmet's row has the arrow, so nobody crafts it for nothing.
  It can still be crafted. ("Better" = more Protection; the green tick from 21/11/2026 still marks a piece you have.)

## Files changed

| File | What changed |
|---|---|
| `Assets/Prefabs/UI/CraftingCanvas.prefab` (yours, by Tony's **Tools > UI > Build Crafting Menu**) | Only when the tool is run: **Panel > Tabs** is built again with six tabs instead of five (All, Materials, Food, Tools, **Ammo**, Armor; 136 wide each instead of 150 so all six fit), and it's saved with Tony's rebuilt row prefab linked (`Assets/Prefabs/UI/Crafting/CraftingRow.prefab`, which now has a second switched-off badge, **Better**, next to the **Owned** tick on its picture). Nothing else in your prefab changes. |
| `Assets/Script/Crafting/CraftingMenu.cs` (yours) | **Not** changed. |

Tony's own files: `CraftingTabs.cs` (the Ammo tab: Item Type Ammo goes there), `CraftingRow.cs` (the blue arrow and
the words), `PlayerArmor.cs` (`BetterOwned`: finds the better piece), `CraftingMenuBuilder.cs` (the sixth tab, the
Better badge on the row), `IconArt.cs` (`Assets/UI/Common/Better.png`). Guide: `Docs/Crafting Menu Setup.md`.

## How to undo

- **CraftingCanvas.prefab:** from git (`git checkout -- Assets/Prefabs/UI/CraftingCanvas.prefab` before committing), or
  by hand: delete Panel > Tabs > **Ammo** and set the other tabs' Layout Element widths back to 150. (The arrows then
  show only under All until `CraftingTabs.KindOf` sends Ammo back to Tools.)
- The blue arrow: nothing in your files; Tony's row prefab just has one more switched-off child.
