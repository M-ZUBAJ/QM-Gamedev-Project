# Crafting Menu Setup (pictures, tabs, armor set panels, a scroll list and the tier tools)

**What the player sees** (inventory > **Crafting** tab, or **C**):
- One row per item: its **picture** and **name** ("Plank x2" when one craft makes two), and under it the **recipe as
  pictures**, each with **have / need** and its name, e.g. `1/2 Plank`: **green** when you have enough, **red** when
  not.
- A row you can craft says **CRAFT** on the right; a row with something missing is faded.
- **An armor piece you already have** gets a **green tick** on its picture and "(owned)" after its name ("(worn)" while
  you wear it), so you don't craft the same piece twice by mistake. You still can: it's only a reminder.
- Click a row to craft it (Turki's message line says what happened or what's missing). The amounts update at once.
- **Crafted tools and food go to the hotbar first** (21/11/2026): onto a hotbar stack of the same item, else the
  leftmost empty hotbar slot, ready to use; only with no room on the hotbar do they go to the inventory. Materials
  (Plank...) and armor go to the inventory as before.
- **Tabs** over the list: **All** (picked at the start), **Materials**, **Food**, **Tools**, **Armor**. A tab shows
  only that kind (by the item's Item Type: Material, Consumable, Tool, Armor), from the top of the list. A tab with
  nothing to craft is faded, and picking it says "Nothing to craft here yet." The picked tab stays picked when the
  list is closed and opened again.
- **Armor set panels:** each armor set's pieces sit together in one box in the set's color (copper for Copper, steel
  grey for Iron), with a header: the set's name ("COPPER SET"), how much the whole set protects and what wearing all
  six gives ("10% protection - all 6: +1 coin for every monster killed"; Iron also says "counts as Copper"), and on the
  right how many pieces are worn ("Worn 2/6"), or **BONUS ON** in gold while the whole set is worn.
  A box shows in the All and Armor tabs and hides in the others.
- An armor upgrade's recipe counts the piece you wear: "1/1 Copper Helmet (worn)". Crafting it takes the Copper Helmet
  off and puts the Iron Helmet on in its place.
- The list scrolls (mouse wheel, or the bar on the right), so any number of items fits.
- The list: Plank, Stew, then the tools **Wooden > Stone > Iron**, each Pickaxe, Axe, Sword, then the armor set by set
  (Copper, then Iron), each Helmet to Boots.

## Quick way: one click

1. Menu **Tools > UI > Build Crafting Menu**, then **Build** (it asks first: it changes Turki's prefab). It:
   - makes `Assets/Prefabs/UI/Crafting/CraftingRow.prefab` (Tony's **CraftingRow** row) and puts it in Turki's
     CraftingMenu > **Row Prefab** (his old `CraftRow.prefab` stays in the project, just unused);
   - makes Turki's **Panel** 960 x 640 and puts its **Rows** in a scroll list: Panel > **Scroll** (ScrollRect) >
     **Viewport** > Rows, with a **Scrollbar**;
   - adds the tabs: Panel > **Tabs** (All, Materials, Food, Tools, Armor) above the list, Panel > Scroll > **Empty**
     (the "nothing here" text), and Tony's **CraftingTabs** on the Panel (built again every run);
   - adds the armor set panels: Panel > Scroll > **SetPanel** (switched off: the look of a set's box) and Tony's
     **CraftingSetPanels** on the Panel (built again every run);
   - sorts **Craftables**: the items that aren't tools stay first, in their order, then every tool that has a recipe
     made at the Crafting page, weakest tier first, then every armor piece with a recipe, set by set (the weakest set
     first, by all its pieces' Protection added up: Copper, Iron, Gold), each helmet to boots. Diamond tools have no
     recipe, so they aren't listed. (Tools > Items > Build Plate Armor and Tools > Items > Update Tool Recipes sort the
     list too; Build Plate Armor also adds the set panels.)
2. Nothing else: the inventory's Crafting page and the C menu are both copies of `Assets/Prefabs/UI/CraftingCanvas`.

Run it again after giving more items recipes: it builds the row again and sorts the list again.

## The recipes (each tool's Item Data > Recipe)

| Tier | Pickaxe | Axe | Sword |
|---|---|---|---|
| Wooden | 3 Plank + 3 Wood | 3 Plank + 3 Wood | 3 Plank + 6 Wood |
| Stone | Wooden Pickaxe + 3 Plank + 6 Stone | Wooden Axe + 3 Plank + 6 Stone | Wooden Sword + 3 Plank + 9 Stone |
| Iron | Stone Pickaxe + 3 Plank + 6 Iron | Stone Axe + 3 Plank + 6 Iron | Stone Sword + 3 Plank + 9 Iron |

A chain (19/11/2026): each tier is made **from the same tool of the tier below**, which crafting uses up. So a Stone
Axe needs a Wooden Axe first. When the Wooden Axe was on the hotbar, the new Stone Axe goes into **its slot** (a tool in
hand stays in hand); otherwise (it was in the inventory, or a stack of 2) the new tool goes to the hotbar's leftmost
empty slot, and only with a full hotbar into the old tool's slot. (Plank x2 = 1 Wood.) The crafting list shows the
tool you need with its picture, like any other ingredient.

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
4. **The tabs:** in the Panel, between the Title and Scroll, an empty **Tabs** (Layout Element: Min and Preferred
   Height 40, **Flexible Height 0** (left empty, the row takes half the panel's spare height and the tabs come out
   huge); Horizontal Layout Group: Spacing 8, Child Alignment Middle Center, Control Child Size both ticked, Child Force
   Expand Height ticked, Width not). In it five UI > Buttons named exactly **All**, **Materials**, **Food**, **Tools**,
   **Armor** (each a Layout Element: Min and Preferred Width 150, Flexible Width 0), each with its text child renamed
   **Label** (font size 20).
   In Scroll, a TextMeshPro **Empty** (stretched, "Nothing to craft here yet.", switched off). Then Add Component >
   **CraftingTabs** on the Panel. (The rows must be CraftingRow rows: they ask CraftingTabs whether to show.)
5. **The armor set panels:** in Scroll (not in Rows: the list empties Rows every time it opens), a UI > Image named
   **SetPanel** with the RoundedBox picture (Image Type Sliced, white) and a **Vertical Layout Group** (Padding 12 /
   12 / 8 / 12, Spacing 8, Control Child Size both ticked, Child Force Expand Width ticked, Height not). In it an empty
   **Header** (Layout Element: Min and Preferred Height 58) with an Image **Stripe** (an 8 px bar down its left), and
   TextMeshPro texts **Title** (top half, bold 24), **Info** (bottom half, 18, Auto Size 13 - 18) and **Progress**
   (top right, bold 20, right-aligned). Switch SetPanel off, then Add Component > **CraftingSetPanels** on the Panel.
   Each set's name, color and bonus come from the Player's **PlayerArmor > Set Bonuses**.

## Problems

| Problem | Why / fix |
|---|---|
| Rows are still plain text | CraftingMenu > Row Prefab isn't the CraftingRow prefab: run the tool. |
| A picture is missing | The item has no icon: its Item Data > Icon is Capture (photographed while playing) and needs a Prefab. |
| A tool isn't in the list | It has no Recipe, or Crafted At isn't Crafting. Give it one, then run the tool again. |
| The list doesn't scroll | Scroll needs **Scroll Rect** with Content = Rows, and Rows a **Content Size Fitter**. |
| No tabs over the list | Run the tool again (it adds them). The tabs need the scroll list (Panel > Scroll). |
| The tabs are huge and the list short | Tabs > Layout Element > **Flexible Height** must be 0. Run the tool again (it builds the tabs again, 40 high). |
| A tab shows the wrong items | The item's Item Data > **Item Type** decides its tab (Material, Consumable = Food, Tool, Armor). |
| Clicking a tab does nothing | The tabs must be named exactly All / Materials / Food / Tools / Armor inside Panel > Tabs, and CraftingTabs must be on the Panel. |
| The armor isn't in boxes | Run the tool again (it adds Scroll > SetPanel and CraftingSetPanels). A set's pieces must sit next to each other in Craftables (the tool sorts them so). |
| A set's box is grey instead of its color | PlayerArmor > Set Bonuses has no line for that set (or no Color): run Tools > Items > Build Plate Armor with GameScene open. |
| One set is split into two boxes | Something else sits between its pieces in Craftables: run the tool to sort the list again. |

Turki's change logs: `Docs/TonyEditMate/Turki/19-11-2026 Crafting Menu Icons And Tier Tools.md`,
`Docs/TonyEditMate/Turki/20-11-2026 Crafting Tabs.md`, `Docs/TonyEditMate/Turki/20-11-2026 Reinforced Iron Armor And Set Panels.md`.
