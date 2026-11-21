# Tony's change to Turki's crafting, 21/11/2026: crafted tools and food go to the hotbar, a tick on owned armor

**Why:** Tony asked for two things in the crafting list:
- **Crafted tools and food go to the hotbar first**, starting at its leftmost free slot, instead of into the
  inventory, so they're ready to use. (A hotbar stack of the same item gets them first; with no room on the hotbar
  they go to the inventory as before.) An upgraded tool still takes the old tool's slot when that was on the hotbar
  (the rule from 19/11/2026), so a tool in hand stays in hand. Materials (Plank...) and armor are added as before.
- **A green tick on an armor piece the player already has** (in the inventory or worn), with "(owned)" or "(worn)"
  after its name, so nobody crafts the same piece twice by mistake. It can still be crafted.

## Files changed

| File | What changed |
|---|---|
| `Assets/Script/Crafting/CraftingMenu.cs` (yours) | In `Craft()`: `if (inv.AddCrafted(item, item.CraftAmount, oldToolSlot))` (`[Tony] replaced`), was `if (inv.AddInto(oldToolSlot, item.Id, item.DisplayName, item.GetIcon(), item.CraftAmount, item))` (Tony's line from 19/11/2026; your original `inv.Add(...)` is still in the comment above it). Tony's `PlayerInventory.AddCrafted` decides: a tool or a consumable goes to the hotbar first (`AddToHotbar`), anything else exactly as before (`AddInto`). The "Inventory is full!" branch is unchanged. |
| `Assets/Prefabs/UI/CraftingCanvas.prefab` (yours, by Tony's Tools > UI > Build Crafting Menu) | Saved again with Tony's rebuilt row prefab linked (`Assets/Prefabs/UI/Crafting/CraftingRow.prefab`, which now has the switched-off "Owned" tick on its picture). Nothing else in your prefab changes. |

Tony's own files: `PlayerInventory.cs` (`AddCrafted`, `AddToHotbar`, `GoesToHotbar`), `CraftingRow.cs` (the tick and
"(owned)" / "(worn)"), `CraftingMenuBuilder.cs` (the tick on the row prefab), `IconArt.cs` (`Assets/UI/Common/Tick.png`).
Guide: `Docs/Crafting Menu Setup.md`.

## How to undo

- `CraftingMenu.cs`: put back the line in the `// Was:` comment and delete the `[Tony] replaced (21/11/2026)` lines.
- The tick: nothing in your files; Tony's row prefab just has an extra switched-off child.
