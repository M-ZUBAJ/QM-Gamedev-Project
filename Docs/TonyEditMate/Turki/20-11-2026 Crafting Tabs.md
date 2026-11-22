# Tony's edit to Turki's crafting menu, 20/11/2026: tabs (All, Materials, Food, Tools, Armor)

**Files changed:**
- `Assets/Prefabs/UI/CraftingCanvas.prefab` (Turki's): changed by Tony's **Tools > UI > Build Crafting Menu**
  (`Assets/Script/Editor/CraftingMenuBuilder.cs`), see below.
- New, Tony's: `Assets/Script/UI/Crafting/CraftingTabs.cs`. Tony's `CraftingRow.cs` got a few lines (each row asks the
  tabs whether it shows).
- `Assets/Script/Crafting/CraftingMenu.cs` (Turki's): **not** changed.

**Why (Tony asked for it):** with the tools and the armor the list has over 20 items. Tabs over the list (All by
default, Materials, Food, Tools, Armor) show one kind at a time, in the inventory's Crafting tab and in the C menu.

---

## CraftingCanvas.prefab

| What | Change |
|---|---|
| **Panel > Tabs** (new) | between your Title and the Scroll list: five rounded buttons, All / Materials / Food / Tools / Armor, 150 x 40 each, in the middle (Horizontal Layout Group; Layout Element height 40, Flexible Height 0 so the row doesn't take spare height from your list) |
| **Panel > Scroll > Empty** (new) | "Nothing to craft here yet." (switched off; shown when a tab has nothing in it) |
| **Panel** | Tony's **CraftingTabs** added |

How it works: your `FillRows()` still makes every row as before. Each row (Tony's CraftingRow) asks CraftingTabs
whether its item is in the picked tab and hides itself if not; the list closes up around the rest. Clicking a tab
shows that kind's rows (by the item's **Item Type**: Material = Materials, Consumable = Food, Tool = Tools, Armor =
Armor). Your Craftables, the key (C), `Craft()` and the message line are unchanged.

**To undo:**
- **CraftingCanvas.prefab:** from git (`git checkout -- Assets/Prefabs/UI/CraftingCanvas.prefab` before committing), or
  by hand: delete Panel > **Tabs** and Panel > Scroll > **Empty**, and remove **CraftingTabs** from the Panel.

Guide: `Docs/Crafting Menu Setup.md`.
