# Tony's edit to Turki's crafting list, 20/11/2026: the plate armor can be crafted

**File changed (prefab data only):** `Assets/Prefabs/UI/CraftingCanvas.prefab`, CraftingMenu > **Craftables**: the 6
plate armor items were added at the end, after the tools. These are Plate Helmet, Plate Shoulders, Plate Chestplate,
Plate Gloves, Plate Leggings and Plate Boots (`Assets/Config/Items/Armor`, recipes of Iron 5 / 4 / 8 / 3 / 7 / 4). Tony's
**Tools > Items > Build Plate Armor** did it through Build Crafting Menu's sorting (`CraftingMenuBuilder.SortList`).

Your **CraftingMenu.cs is not changed.** Crafting armor works like crafting anything else, and the crafted piece goes
into the inventory. The player wears it from the inventory's Gear tab (Tony's PlayerArmor).

**Why (Tony asked for it):** an armor system with Blink's plate set (the armor inside the player's character).

**Later the same day:** the six pieces became the **iron set**: Iron Helmet, Iron Shoulders, Iron Chestplate, Iron
Gloves, Iron Leggings, Iron Boots. Build Plate Armor renamed their files (`PlateHelmet.asset` -> `IronHelmet.asset`
and so on). A rename keeps the file's ID, so the entries in your Craftables list stayed linked: only the names you see
changed. Nothing else in your CraftingCanvas changed.

**To undo:** open `CraftingCanvas.prefab`, select CraftingMenu, remove the 6 Plate items from **Craftables**, save.

Guide: `Docs/Armor Setup.md`.
