# HUD Setup

The HUD is what stays on screen while playing: the crosshair in the middle, the health / hunger / stamina bars in the
top left, and the day, time and coins in the top right.

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
```

## Quick way: one click

1. Open the scene (e.g. `Assets/Scenes/GameScene.unity`).
2. Menu **Tools > UI > Build HUD**.
   - It saves `Assets/Prefabs/UI/HUD/HUDCanvas.prefab` and puts a HUDCanvas in the scene.
   - If the prefab or a HUDCanvas in the scene already exists, it asks first. **Ctrl + Z** brings the old scene one back.
3. Save the scene (**Ctrl + S**).

Use it any time the HUD was deleted or broken: everything comes back connected.

## What else the scene needs

| Needed for | Put in the scene |
|---|---|
| The bars | The Player with **PlayerHealth**, **PlayerHunger**, **PlayerStamina** (the bars find them by themselves) |
| Day and time | The Directional Light with **DaylightCycle** |
| Coins | The Player with Marko's **PlayerCoins** |
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
7. Drag HUDCanvas into `Assets/Prefabs/UI/HUD` to make it a prefab. Save the scene.

## If something doesn't work

| Problem | Fix |
|---|---|
| Day / time shows "DAY -" / "--:--" | No **DaylightCycle** in the scene: add it to the Directional Light. |
| Coins stay 0 | The Player needs Marko's **PlayerCoins**; coins come from defeating enemies. |
| Bars don't move | The Player needs PlayerHealth / PlayerHunger / PlayerStamina. |
| Text is invisible | Window > TextMeshPro > **Import TMP Essential Resources**. |
| The time never changes | GameRules > **Do Daylight Cycle** is off (time stopped). |
