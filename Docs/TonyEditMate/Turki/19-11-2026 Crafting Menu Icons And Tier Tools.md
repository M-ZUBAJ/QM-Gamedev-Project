# Tony's edit to Turki's crafting menu, 19/11/2026: pictures, a scroll list and the tier tools

**Files changed:**
- `Assets/Script/Crafting/CraftingMenu.cs` (Turki's): one `[Tony] replaced` line in `FillRows()`; the original is kept
  in a comment.
- `Assets/Prefabs/UI/CraftingCanvas.prefab` (Turki's): changed by Tony's **Tools > UI > Build Crafting Menu**
  (`Assets/Script/Editor/CraftingMenuBuilder.cs`), see below.
- New, Tony's: `Assets/Script/UI/Crafting/CraftingRow.cs` and `Assets/Prefabs/UI/Crafting/CraftingRow.prefab`.
- `Assets/Prefabs/UI/CraftRow.prefab` (Turki's old row): **not** changed, just not used any more.

**Why (Tony asked for it):** pictures of the items and their recipes in the crafting menu, and every tool to craft,
Wooden > Stone > Iron. The list had 5 text rows; 11 rows didn't fit the 700 x 600 panel.

---

## CraftingMenu.cs

| Where | Change |
|---|---|
| `FillRows()`, the row's text | `[Tony] replaced`: if the row has Tony's **CraftingRow**, `CraftingRow.Show(item)` fills it (the item's icon and name, and each ingredient's icon with "have/need" and its name, green / red). A row without CraftingRow still gets your `RowText(item)` text. |

Unchanged: the key (C), opening inside the inventory, `Craft()` (taking the recipe, giving the item, "Inventory is
full!"), `RowText()`, the message line.

## CraftingCanvas.prefab

| What | Change |
|---|---|
| CraftingMenu > **Row Prefab** | your `CraftRow` → Tony's `Crafting/CraftingRow` |
| **Panel** size | 700 x 600 → 960 x 640 |
| **Rows** | moved into a scroll list: Panel > **Scroll** (Scroll Rect, Layout Element Flexible Height 1) > **Viewport** (see-through Image + Rect Mask 2D) > Rows (now with a Content Size Fitter), plus a **Scrollbar**. Your Title and Message stay where they were. |
| CraftingMenu > **Craftables** | Plank, Stew (your order), then Wooden Pickaxe / Axe / Sword, Stone Pickaxe / Axe / Sword, Iron Pickaxe / Axe / Sword (before: Plank, Iron Sword, Stew, Stone Pickaxe, Stone Axe). Recipes are unchanged. |

**To undo:**
- **CraftingMenu.cs:** delete the lines marked `[Tony]` from 19/11/2026 and put back the original line from the comment.
- **CraftingCanvas.prefab:** from git (`git checkout -- Assets/Prefabs/UI/CraftingCanvas.prefab` before committing),
  or by hand: Row Prefab = CraftRow, move Rows back under Panel (delete Scroll), Panel 700 x 600, your old Craftables.

Guide: `Docs/Crafting Menu Setup.md`.
