# Tony's edit to Turki's CraftingMenu, 19/11/2026: an upgraded tool keeps the old tool's slot

**File changed:** `Assets/Script/Crafting/CraftingMenu.cs`, in `Craft(ItemData item)`, marked `// [Tony]`:
- `[Tony] added`: `int oldToolSlot = inv.SlotOf(item.UpgradedFrom);` just before `item.TakeRecipeFrom(inv);`
- `[Tony] replaced`: `inv.Add(...)` in the `if` is now `inv.AddInto(oldToolSlot, ...)` (same arguments, the slot first).
  The original line is kept in a comment above it.

Nothing else changed: the checks, the message, and giving the materials back when the inventory is full are yours.

**Why (Tony asked for it):** tools are upgraded in a chain (Wooden Axe + 3 Plank + 6 Stone = Stone Axe). The old tool
was taken out of its slot and the new one went to the first free inventory slot, so an axe kept in the hotbar (or in
the hand) vanished from there. Now the new tool goes into the same slot, hotbar or inventory.

**How it works:** Tony's new code:
- `ItemData.UpgradedFrom`: the tool in a tool's recipe (the Stone Axe's Wooden Axe); null for everything else.
- `PlayerInventory.SlotOf(ItemData)`: which slot holds it (the slot `Remove` takes it from first), -1 = none.
- `PlayerInventory.AddInto(slot, ...)`: puts the new item in that slot if crafting emptied it; otherwise (-1, or the
  old tool was a stack of 2) it's exactly your old `Add`.

So crafting a Plank or anything that isn't an upgrade works exactly as before.

**To undo:** in `Craft`, delete the `[Tony] added` line and change `inv.AddInto(oldToolSlot, ` back to `inv.Add(`.

**Update 21/11/2026:** the `AddInto` line was replaced by `inv.AddCrafted(item, item.CraftAmount, oldToolSlot)`: crafted tools and food go to the hotbar first, and an upgrade keeps the old tool's slot when that was on the hotbar (else the hotbar's leftmost empty slot). See `21-11-2026 Crafted Tools And Food Go To The Hotbar.md`.
