# Crafting Menu Setup (pictures, a scroll list and the tier tools)

**What the player sees** (inventory > **Crafting** tab, or **C**):
- One row per item: its **picture** and **name** ("Plank x2" when one craft makes two), and under it the **recipe as
  pictures**, each with **have / need** and its name, e.g. `1/2 Plank`: **green** when you have enough, **red** when
  not.
- A row you can craft says **CRAFT** on the right; a row with something missing is faded.
- Click a row to craft it (Turki's message line says what happened or what's missing). The amounts update at once.
- The list scrolls (mouse wheel, or the bar on the right), so any number of items fits.
- The list: Plank, Stew, then the tools **Wooden > Stone > Iron**, each Pickaxe, Axe, Sword.

## Quick way: one click

1. Menu **Tools > UI > Build Crafting Menu**, then **Build** (it asks first: it changes Turki's prefab). It:
   - makes `Assets/Prefabs/UI/Crafting/CraftingRow.prefab` (Tony's **CraftingRow** row) and puts it in Turki's
     CraftingMenu > **Row Prefab** (his old `CraftRow.prefab` stays in the project, just unused);
   - makes Turki's **Panel** 960 x 640 and puts its **Rows** in a scroll list: Panel > **Scroll** (ScrollRect) >
     **Viewport** > Rows, with a **Scrollbar**;
   - sorts **Craftables**: the items that aren't tools stay first, in their order, then every tool that has a recipe
     made at the Crafting page, weakest tier first. Gold and Diamond tools have no recipe, so they aren't listed.
2. Nothing else: the inventory's Crafting page and the C menu are both copies of `Assets/Prefabs/UI/CraftingCanvas`.

Run it again after giving more items recipes: it builds the row again and sorts the list again.

## The recipes (each tool's Item Data > Recipe)

| Tier | Pickaxe | Axe | Sword |
|---|---|---|---|
| Wooden | 3 Plank + 3 Wood | 3 Plank + 3 Wood | 3 Plank + 6 Wood |
| Stone | Wooden Pickaxe + 3 Plank + 6 Stone | Wooden Axe + 3 Plank + 6 Stone | Wooden Sword + 3 Plank + 9 Stone |
| Iron | Stone Pickaxe + 3 Plank + 6 Iron | Stone Axe + 3 Plank + 6 Iron | Stone Sword + 3 Plank + 9 Iron |

A chain (19/11/2026): each tier is made **from the same tool of the tier below**, which crafting uses up. So a Stone
Axe needs a Wooden Axe first, and the new Stone Axe goes into **the slot the Wooden Axe was in** (hotbar or
inventory; a tool in hand stays in hand). If the old tool was a stack of 2, one stays there and the new tool goes
to the first free slot. (Plank x2 = 1 Wood.) The crafting list shows the tool you need with its picture, like
any other ingredient.

To change one, select the tool's Item Data in `Assets/Config/Items/Tools` and edit its **Recipe**. To change them all
for every future build, edit `RecipeFor` in `Editor/TierToolBuilder.cs`, then run **Tools > Items > Update Tool
Recipes** (it puts only the recipes on the existing tools).

## Adding an item to the list

1. Its Item Data needs a **Recipe** and **Crafted At = Crafting** (Furnace items aren't listed here).
2. Open `Assets/Prefabs/UI/CraftingCanvas`, select CraftingCanvas, drag the Item Data into **Craftables** (or, for a
   tool, run the tool again: it adds every tool with a recipe by itself).

## By hand (instead of the tool)

1. **The row:** UI > Button with a white box, **Layout Element** Preferred Height 76, a **Canvas Group** and
   **CraftingRow**. Children (CraftingRow finds them by name): **Icon** (Image, 58 x 58 on the left), **Name**
   (TextMeshPro, bold), **Ingredients** (Horizontal Layout Group, Spacing 22) with a switched-off **Ingredient**
   inside it (Horizontal Layout Group: an Image **Icon** 26 x 26 and a TextMeshPro **Amount**), and **Status**
   (TextMeshPro on the right). Save it as a prefab and drag it into Turki's CraftingMenu > **Row Prefab**.
2. **The scroll list:** in Turki's Panel, make an empty **Scroll** where Rows is (Layout Element: Flexible Height 1),
   in it a **Viewport** (stretched, a see-through Image + **Rect Mask 2D**), and move **Rows** into the Viewport
   (anchor top stretch, Pivot Y 1, Add Component > **Content Size Fitter**, Vertical Fit = Preferred Size). Add a
   UI > Scrollbar (Direction Bottom To Top) on the right of Scroll. On Scroll, Add Component > **Scroll Rect**:
   Horizontal off, Viewport = Viewport, Content = Rows, Vertical Scrollbar = the Scrollbar (Auto Hide And Expand
   Viewport).
3. **The list:** drag the tools into Craftables, Wooden first.

## Problems

| Problem | Why / fix |
|---|---|
| Rows are still plain text | CraftingMenu > Row Prefab isn't the CraftingRow prefab: run the tool. |
| A picture is missing | The item has no icon: its Item Data > Icon is Capture (photographed while playing) and needs a Prefab. |
| A tool isn't in the list | It has no Recipe, or Crafted At isn't Crafting. Give it one, then run the tool again. |
| The list doesn't scroll | Scroll needs **Scroll Rect** with Content = Rows, and Rows a **Content Size Fitter**. |

Turki's change log: `Docs/TonyEditMate/Turki/19-11-2026 Crafting Menu Icons And Tier Tools.md`.
