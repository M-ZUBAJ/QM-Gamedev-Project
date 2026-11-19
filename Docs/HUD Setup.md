# HUD Setup

The HUD is what stays on screen while playing: the crosshair in the middle, the health / hunger / stamina bars in the
top left, the player's perks along the top, the day, time and coins in the top right, and a prompt under the
crosshair while you look at a perk chest ("E Open chest (coin) 2").

```
HUDCanvas
  Crosshair                           middle (aiming mark, turns green on an item)
  ChestPrompt                         under the crosshair: Pill > Key (KeyText), Label, CoinIcon, Price (ChestPrompt)
  HealthBar   (Background, Fill, Frame)   top left
  HungerBar   (Background, Fill, Frame)
  StaminaBar  (Background, Fill, Frame)
  TopRight                            top right
    DayText       "DAY 5 - Noon"            (DayCounter)
    TimeText      "14:30 - Night in 2:15"   (TimeCounter)
    CoinCounter   Pill > Icon (coin) + Text "12"   (CoinCounter)
  Perks                               top middle (PerkBar)
    Perk1 / Perk2 / Perk3   each: Title "Fireball x2" + Stats "Q with sword / 2.4 s cooldown"
  PerkIcons                           top middle, switched off (PerkBar shows it while GameRules > Perk Icons is on)
    PerkIcon1               Icon + Count "x2" + Label (copied for every perk)
```

## Quick way: one click

1. Open the scene (e.g. `Assets/Scenes/GameScene.unity`).
2. Menu **Tools > UI > Build HUD**.
   - It saves `Assets/Prefabs/UI/HUD/HUDCanvas.prefab` and puts a HUDCanvas in the scene.
   - If the prefab or a HUDCanvas in the scene already exists, it asks first. **Ctrl + Z** brings the old scene one back.
   - It also adds **PlayerPerks** to the Player (the perk row reads it).
   - It draws the coin and the round box pictures in `Assets/UI/Common` (only if the files aren't there yet).
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
| The chest prompt | Marko's perk chests (**PerkChest**) |

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

**Text Color** changes the number's color in every look. Build HUD sets **Static Icon** with `Assets/UI/Common/Coin.png`,
in a see-through dark round **Pill** that grows with the number.
**Pop Scale** (1.2) / **Pop Seconds** (0.3) / **Pop Target** (the Pill): it grows for a moment whenever the total
changes (a gain or a payment). Pop Scale 1 = no pop.

### Chest prompt (ChestPrompt)
While the crosshair is on a perk chest within its reach (where E opens it), `E  Open chest  (coin) 2` fades in under
the crosshair: the chest's key, what it does and its price. The price is red while you have fewer coins than that
(looking inside is free; the coins are paid when a perk is picked). Hidden while a menu is open, while dead, and once
the chest is used. **Open Text**, the two colors and **Fade Seconds** are on HUDCanvas > ChestPrompt.
Perk panel guide: `Docs/Perk Panel Setup.md`.

### Perks (Perks, PerkBar)
**GameRules > Perk Icons** (on by default) shows the perks as **icons** instead: one small tile per perk, centered at
the top (heart = Max Health, lightning = Max Stamina, arrows = Move Speed, flame = Fireball), with **x2** when the
player has it twice. The pictures come from the Player's **PlayerPerks > Icons** (Build HUD fills it with the drawn
ones in `Assets/UI/Perks`). Untick the rule (also while playing) for the text boxes below. The inventory's **Info**
tab always has the details.

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
6. **CoinCounter**: Create Empty in TopRight with a **Layout Element** (Preferred Height 60). In it **Pill**: UI > Image
   (`Assets/UI/Common/RoundedBox.png`, Image Type **Sliced**, black with Alpha about 115), anchor **middle right**
   (also Pivot 1, 0.5), height 54, with a **Horizontal Layout Group** (Padding 12 / 18 / 4 / 4, Spacing 10, Child
   Alignment Middle Center, Control Child Size Width + Height, no Force Expand) and a **Content Size Fitter**
   (Horizontal Fit = Preferred Size). Children of the Pill: **Icon** (UI > Image, `Coin.png`, Preserve Aspect, Layout
   Element Preferred Width / Height 40) and **Text** (TextMeshPro, size 36, Bold). Then Add Component >
   **CoinCounter** on CoinCounter: Look **Static Icon**, Static Icon = `Coin.png`, Pop Target = the Pill.
7. **ChestPrompt**: Create Empty under HUDCanvas, in the middle, Pos Y -92, with a **Canvas Group** (Alpha 0). In it a
   **Pill** like the coins' (Padding 8 / 18 / 6 / 6, Spacing 12) with: **Key** (Image `RoundedBox.png`, Sliced, light
   grey, Layout Element 40 x 40) with a TextMeshPro child **KeyText** ("E", dark, bold), **Label** (TextMeshPro "Open
   chest"), **CoinIcon** (Image `Coin.png`, Layout Element 32 x 32) and **Price** (TextMeshPro, gold, bold). Then Add
   Component > **ChestPrompt** on ChestPrompt and drag KeyText, Label and Price into its fields.
8. **Perks**: Create Empty under HUDCanvas, anchor **top middle** (also Pivot 0.5, 1), Pos X 40, Pos Y -8,
   1110 x 165. Add **Horizontal Layout Group** (Spacing 8, Control Child Size Width + Height, Child Force Expand
   Width + Height) and **PerkBar**. Inside it **Perk1**, **Perk2**, **Perk3**: each a UI > Image (dark, see-through,
   Raycast Target off) with two TextMeshPro children: **Title** (top, size 30, bold) and **Stats** (below, size 22).
   Then add **PlayerPerks** to the Player.
9. **PerkIcons**: Create Empty under HUDCanvas, anchor **top middle** (also Pivot 0.5, 1), Pos Y -8, 1110 x 68, with a
   **Horizontal Layout Group** (Spacing 10, Child Alignment Upper Center, Control Child Size Width + Height, no Force
   Expand). Inside it **PerkIcon1**: UI > Image (`Assets/UI/Common/RoundedBox.png`, Sliced, dark see-through) with a
   **Layout Element** (Preferred 68 x 68) and three children: **Icon** (Image, stretched with 8 px around), **Count**
   (TextMeshPro bottom right, gold, bold) and **Label** (TextMeshPro, switched off). Switch PerkIcons off. Drag it into
   **Perks > PerkBar > Icon Row** (or keep the name PerkIcons: PerkBar finds it). Then fill the Player's
   **PlayerPerks > Icons** with a picture for each perk.
10. Drag HUDCanvas into `Assets/Prefabs/UI/HUD` to make it a prefab. Save the scene.

## If something doesn't work

| Problem | Fix |
|---|---|
| Day / time shows "DAY -" / "--:--" | No **DaylightCycle** in the scene: add it to the Directional Light. |
| Coins stay 0 | The Player needs Marko's **PlayerCoins**; coins come from defeating enemies. |
| No coin picture next to the number | Run **Tools > UI > Build HUD** again: the new coin counter uses the coin PNG (`Assets/UI/Common/Coin.png`), which also shows in Edit mode. |
| Perks show as text, not icons | GameRules > **Perk Icons** is off, or the HUD has no PerkIcons row yet: run **Tools > UI > Build HUD**. |
| A perk icon shows the perk's name instead of a picture | The Player's **PlayerPerks > Icons** has no picture for it: run Build HUD (it fills empty ones) or drag one in. |
| No prompt at a chest | Look straight at it from closer than its Interaction Range (3 m, first person). The HUD needs **ChestPrompt** (rebuild the HUD). |
| Bars don't move | The Player needs PlayerHealth / PlayerHunger / PlayerStamina. |
| Text is invisible | Window > TextMeshPro > **Import TMP Essential Resources**. |
| The time never changes | GameRules > **Do Daylight Cycle** is off (time stopped). |
