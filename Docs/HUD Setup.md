# HUD Setup

The HUD is what stays on screen while playing: the crosshair in the middle, the health / hunger / stamina bars in the
top left, the player's perks along the top, and the day, time and coins in the top right.

```
HUDCanvas
  Crosshair                           middle (aiming mark, turns green on an item)
  HealthBar   (Background, Fill, Frame)   top left
  HungerBar   (Background, Fill, Frame)
  StaminaBar  (Background, Fill, Frame)
  TopRight                            top right
    DayText       "DAY 5 - Noon"            (DayCounter)
    TimeText      "14:30 - Night in 2:15"   (TimeCounter)
    CoinCounter   Icon + Text "12"          (CoinCounter)
  Perks                               top middle (PerkBar)
    Perk1 / Perk2 / Perk3   each: Title "Fireball x2" + Stats "Q with sword / 2.4 s cooldown"
```

## Quick way: one click

1. Open the scene (e.g. `Assets/Scenes/GameScene.unity`).
2. Menu **Tools > UI > Build HUD**.
   - It saves `Assets/Prefabs/UI/HUD/HUDCanvas.prefab` and puts a HUDCanvas in the scene.
   - If the prefab or a HUDCanvas in the scene already exists, it asks first. **Ctrl + Z** brings the old scene one back.
   - It also adds **PlayerPerks** to the Player (the perk row reads it).
3. Save the scene (**Ctrl + S**).

Use it any time the HUD was deleted or broken: everything comes back connected.

## What else the scene needs

| Needed for | Put in the scene |
|---|---|
| The bars | The Player with **PlayerHealth**, **PlayerHunger**, **PlayerStamina** (the bars find them by themselves) |
| Day and time | The Directional Light with **DaylightCycle** |
| Coins | The Player with Marko's **PlayerCoins** |
| Perks | The Player with **PlayerPerks** (the tool adds it); perk chests (Marko's **PerkChest**) fill it |
| The crosshair colors | The Player with **ItemGrabber** |

Nothing has to be dragged in: every HUD script finds what it needs.

## The counters

### Day (DayText, DayCounter)
Shows `DAY 5 - Noon`. **Format**: `{0}` = the day number, `{1}` = the part of the day.
The parts of the day and when they start are set on the sun's **DaylightCycle > Parts Of The Day**:
Night 0.77 (18:28) to Dawn 0.23 (05:31), Morning 0.30, Noon 0.45, Afternoon 0.55, Dusk 0.70.
The day number goes up when Dawn starts.

### Time (TimeText, TimeCounter)
Shows the clock and a countdown in **real** minutes: `14:30 - Night in 2:15`, or at night `22:10 - Morning in 3:40`.
**Clock 24 Hour** off = `2:30 PM`. **Show Countdown** off = only the clock.

### Coins (CoinCounter)
Choose the **Look**; only the fields it needs are shown:

| Look | What to fill in |
|---|---|
| **Auto Icon** | nothing: a gold coin drawn by the script (a gold square in Edit mode, the coin while playing) |
| **Static Icon** | **Static Icon** = a PNG (Texture Type = Sprite) |
| **Animated Icon** | **Aseprite** = an `.aseprite` file (Aseprite Tag only if it has several animations) |
| **Text Name** | **Text Name** = the word, e.g. "Coins" shows `Coins: 12` (no picture) |

**Text Color** changes the number's color in every look.

### Perks (Perks, PerkBar)
Boxes along the top, one per perk: the perk's **name** ("x2" when the player has it twice) and its **stats right now**,
e.g. *Max Health / +20% each / Max Health 144*. Boxes are **invisible until the player gets a perk**, then pop in and
stay. When the player **dies, every perk is lost** (the boosts are undone) and the boxes disappear
(Player > PlayerPerks > **Reset On Death**, on by default).

**One Box Per Perk** (on Perks > PerkBar):
- **Off** (default): your boxes Perk1, Perk2, Perk3 keep their places and fill left to right. More perks than boxes:
  the newest show. More boxes: select **Perk3** and press **Ctrl + D** (any name starting with **Perk** works).
- **On**: a new box for every perk, from the left, all sharing the row: 1 perk fills the whole row, 2 perks half
  each, 10 perks 10 boxes (the text shrinks to fit, down to **Min Text Size**).

In Edit mode all boxes stay visible so you can arrange them. Test: Play, Player > PlayerPerks **⋮** >
**Test: Add Random Perk**; then PlayerHealth **⋮** > **Test: Die** and the boxes disappear.

## By hand (instead of the tool)

1. Hierarchy > right click > **UI > Canvas**, name it **HUDCanvas**. Canvas Scaler: **Scale With Screen Size**,
   Reference Resolution **1920 x 1080**, Match **0** (width).
2. **Crosshair**: right click HUDCanvas > UI > Image, 32 x 32 in the middle, Source Image `Assets/UI/Cursor/CursorPlus.png`,
   untick Raycast Target, Add Component > **Crosshair**.
3. **Each bar** (HealthBar, HungerBar, StaminaBar): Create Empty under HUDCanvas, anchor **top left**, size 288 x 69,
   Pos X 182, Pos Y -37 / -90 / -144. Inside it three Images, each stretched (anchor preset Alt + Shift + bottom right):
   - **Background**: `fill_bar.png`, red.
   - **Fill**: `fill_bar.png`, Image Type **Filled**, Fill Method **Horizontal**, the bar's color.
   - **Frame**: no picture, Image switched off (for border art later).
   Then Add Component > **StatBar** on the bar and choose its **Stat** (Health / Hunger / Stamina).
4. **TopRight**: Create Empty under HUDCanvas, anchor **top right** (also Pivot 1, 1), Pos X -30, Pos Y -25, Width 460.
   Add **Vertical Layout Group** (Child Alignment Upper Right, Control Child Size Width + Height, Child Force Expand
   Width only) and **Content Size Fitter** (Vertical Fit = Preferred Size).
5. In TopRight: UI > Text - TextMeshPro **DayText** (size 36, Bold, right aligned) + Add Component **DayCounter**;
   **TimeText** (size 26, right aligned) + **TimeCounter**.
6. **CoinCounter**: Create Empty in TopRight, Add **Horizontal Layout Group** (Child Alignment Middle Right,
   Control Child Size Width + Height, no Force Expand) and **Layout Element** (Preferred Height 42).
   Children: **Icon** (UI > Image, Layout Element Preferred Width / Height 36) and **Text** (TextMeshPro, size 34,
   Bold, right aligned). Then Add Component > **CoinCounter** on CoinCounter.
7. **Perks**: Create Empty under HUDCanvas, anchor **top middle** (also Pivot 0.5, 1), Pos X 40, Pos Y -8,
   1110 x 165. Add **Horizontal Layout Group** (Spacing 8, Control Child Size Width + Height, Child Force Expand
   Width + Height) and **PerkBar**. Inside it **Perk1**, **Perk2**, **Perk3**: each a UI > Image (dark, see-through,
   Raycast Target off) with two TextMeshPro children: **Title** (top, size 30, bold) and **Stats** (below, size 22).
   Then add **PlayerPerks** to the Player.
8. Drag HUDCanvas into `Assets/Prefabs/UI/HUD` to make it a prefab. Save the scene.

## If something doesn't work

| Problem | Fix |
|---|---|
| Day / time shows "DAY -" / "--:--" | No **DaylightCycle** in the scene: add it to the Directional Light. |
| Coins stay 0 | The Player needs Marko's **PlayerCoins**; coins come from defeating enemies. |
| Bars don't move | The Player needs PlayerHealth / PlayerHunger / PlayerStamina. |
| Text is invisible | Window > TextMeshPro > **Import TMP Essential Resources**. |
| The time never changes | GameRules > **Do Daylight Cycle** is off (time stopped). |
