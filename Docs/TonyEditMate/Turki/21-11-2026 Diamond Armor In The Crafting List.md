# Tony's change to Turki's crafting list, 21/11/2026: diamond armor

**Why:** Tony asked for a diamond armor set: the fourth set after Copper, Iron and Gold, with a set bonus of +8 coins
for every monster killed and 10 % off every loot box. Diamonds come from the diamond rocks in the new world's snow caves
(a Gold Pickaxe or better; `Docs/Snow Setup.md`).
- **Diamond armor:** Diamond Helmet, Diamond Shoulders, Diamond Chestplate, Diamond Gloves, Diamond Leggings and Diamond
  Boots, each made from its Gold piece + Diamond (4 / 3 / 6 / 2 / 5 / 3). All six protect **40 %** (Tony's formula
  a[n] = 5/2 n^2 - 5/2 n + 10 for the 4th set: 6 % a piece, 10 % for the chestplate) and give +8 coins for every monster
  killed and 10 % off every loot box (the same discount as the gold set; they don't add up). Diamond pieces count as
  Gold, so upgrading one piece at a time keeps the Gold bonus. A worn Gold piece can be upgraded where it's worn (your
  `Craft()` already calls Tony's `PlayerArmor.LendForUpgrade / FinishUpgrade`).

**Your scripts are NOT changed.** Only data in your prefab:

| File | What changed |
|---|---|
| `Assets/Prefabs/UI/CraftingCanvas.prefab` (your CraftingMenu's **Craftables** list, by Tony's Tools > Items > Build Plate Armor, through `CraftingMenuBuilder.SortList`) | the 6 diamond armor pieces added after the gold set (in their own ice-blue box, Tony's CraftingSetPanels). Your other items keep their places at the top. |

Tony's own files: `Assets/Config/Items/Armor/Diamond*.asset` + `Assets/Prefabs/Item/Armor/Diamond*.prefab` +
`Assets/Materials/Armor/Diamond Armor.mat` / `Diamond Armor Palette.png` (new). If the Diamond item didn't exist yet,
Build Plate Armor makes it first (`Assets/Config/Items/Diamond`, a copy of your Iron item, exactly as Tools > World >
Build Snow Content does: see `Docs/TonyEditMate/Turki/21-11-2026 Diamond, Snow Rocks And Snowy Pines.md`). Guide:
`Docs/Armor Setup.md`.

**To undo:** remove the 6 diamond armor entries from CraftingMenu > **Craftables** on the CraftingCanvas prefab (or
empty the diamond pieces' **Recipe** and run **Tools > UI > Build Crafting Menu**, which only lists items with a recipe).
