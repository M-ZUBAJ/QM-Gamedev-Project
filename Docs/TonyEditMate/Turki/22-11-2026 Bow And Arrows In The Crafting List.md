# Tony's change to Turki's crafting list, 22/11/2026: the bow and five arrows

**Why:** Tony asked for a bow from GameLair's Medieval Fantasy Weapon Pack and arrows whose head color shows their tier
(guide `Docs/Bow Setup.md`):
- **Wooden Bow:** 10 Wood + 10 Plank. Held in the left hand; hold the left mouse button to draw, let go to shoot.
- **Arrows, 4 per craft:** Stone Arrow = 2 Wood + 2 Stone; Copper Arrow = 4 Stone Arrows + 1 Copper; Iron Arrow = 4
  Copper Arrows + 1 Iron; Gold Arrow = 4 Iron Arrows + 1 Gold; Diamond Arrow = 4 Gold Arrows + 1 Diamond. Each tier hits
  harder (damage 20 / 25 / 30 / 40 / 50 at full draw). The bow shoots the strongest arrows the player carries.
- Arrows are a new Item Type, **Ammo** (Tony's ItemData: an Ammo section with Damage and Head). They show in the
  crafting page's **Tools** tab with the bow (Tony's CraftingTabs), and a craft puts them in the inventory (not the
  hotbar); the bow goes to the hotbar like the other tools.

**Your scripts are NOT changed.** Only data in your prefab:

| File | What changed |
|---|---|
| `Assets/Prefabs/UI/CraftingCanvas.prefab` (your CraftingMenu's **Craftables** list, by Tony's Tools > Items > Build Bow And Arrows, through `CraftingMenuBuilder.SortList`) | `WoodenBow` added with the Wooden tools (after the Wooden Sword), and `StoneArrow`, `CopperArrow`, `IronArrow`, `GoldArrow`, `DiamondArrow` after the tools (weakest first), before the armor. Your other items keep their places at the top. |

Tony's own files: `Assets/Config/Items/Tools/WoodenBow.asset` + `Assets/Prefabs/Item/Tools/WoodenBow.prefab`,
`Assets/Config/Items/Arrows/*.asset` + `Assets/Prefabs/Item/Arrows/*.prefab` (new), `Assets/Materials/Arrows`
(the five head colors). If the Gold or Diamond item didn't exist yet, the tool made it (the same copy of your Iron
item that the desert and snow tools make).

**To undo:** remove the 6 entries from CraftingMenu > **Craftables** on the CraftingCanvas prefab (or empty their
**Recipe** and run **Tools > UI > Build Crafting Menu**, which only lists items with a recipe).
