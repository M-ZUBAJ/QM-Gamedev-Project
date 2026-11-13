# Tony's edit to Turki's code and items, 12/11/2026: tier tools replace the Sword / Axe / PickAxe

**Why:** Tony asked to replace Marko's three placeholder tools with Abduo Bricks' **Low Poly Tool Pack**:
- **15 tools:** a pickaxe, axe and sword in Wood, Stone, Iron, Gold and Diamond.
- **Pickaxe tiers:** a hand can't mine copper or iron, and better ores need better pickaxes.

Tony's tool **Tools > Items > Build Tier Tools** made the changes below (it asked first). Guide: `Docs/Tool Setup.md`.

---

## 1. `Assets/Script/Gathering/ResourceNode.cs`: one line added

```csharp
public ItemData DropItem => dropItem; // [Tony] added
```

- **Read only:** your `dropItem` stays private and nothing else changed.
- **Why:** Tony's `ToolRules` needs to know what a rock drops to know which pickaxe mines it.

**To undo:** delete the line marked `[Tony]` and its comment.

## 2. Your items (Item Data, in `Assets/Config/Items`)

- **New settings on every Item Data:**
  - **Item Type** has a third choice, **Tool**, with a **Tool** section (Kind: Pickaxe / Axe / Sword; Tier: Wood ...
    Diamond).
  - Materials show a **Mining > Mined With** setting: the weakest pickaxe that can mine a rock that drops this item.
  - Tony's ItemData script and its Inspector changed, not your files.
- **Copper and Iron:** their **Mined With** is now **Stone**, so CopperNode and IronNode need a Stone pickaxe or better.
  Stone stays **None** (a hand can still mine stone).
- **15 new tools** in `Assets/Config/Items/Tools` (e.g. `StonePickaxe`, `IronSword`), with recipes:

| Tier | Pickaxe | Axe | Sword |
|---|---|---|---|
| Wood | 4 Plank | 3 Plank | 2 Plank |
| Stone | 2 Plank + 2 Stone | 2 Plank + 1 Stone | 2 Plank + 1 Stone |
| Iron | 2 Plank + 2 Iron | 2 Plank + 1 Iron | 2 Plank + 1 Iron |
| Gold, Diamond | no recipe yet (no gold or diamond in the game) | | |

- **Removed:** the old `Sword.asset`, `Axe.asset` and `PickAxe.asset` went to the trash (your computer's bin; git has
  them too).

## 3. `Assets/Prefabs/UI/CraftingCanvas.prefab`: your Craftables list

The three old tools were swapped one for one, in the same places, for new ones with **exactly the same recipes**:

| Before | After |
|---|---|
| Sword (2 Plank + 1 Iron) | **Iron Sword** (2 Plank + 1 Iron) |
| Axe (2 Plank + 1 Stone) | **Stone Axe** (2 Plank + 1 Stone) |
| PickAxe (2 Plank + 2 Stone) | **Stone Pickaxe** (2 Plank + 2 Stone) |

Plank and Stew are untouched.

**Only three were added on purpose:** your panel (700 x 600, rows 55 px high) fits about 8 rows, and all nine Wood /
Stone / Iron tools would overflow it. To offer them all, give the Rows a Scroll View, then add the other tools to
**Craftables**. Their recipes are ready.

---

**To undo:**
1. Delete the `[Tony]` line in ResourceNode.
2. Set Copper's and Iron's Mined With back to None.
3. Restore the old items:
   `git checkout 5fa06fc -- Assets/Config/Items/Sword.asset Assets/Config/Items/Axe.asset Assets/Config/Items/PickAxe.asset`.
4. Put them back into CraftingCanvas's **Craftables**.
