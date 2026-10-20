# Game End Setup (victory screen)

**The goal:** survive **5 days and nights**. When **Day 6** starts (the morning after the 5th night), the game stops
and the victory screen shows what the player did, with a **Play Again** button that starts a fresh game.

Dying resets the days back to Day 1 (the sun's DaylightCycle > Reset On Player Death), so the 5 days must be
survived in **one life**.

## Quick way: one click

1. Open the scene (e.g. `Assets/Scenes/GameScene.unity`).
2. Menu **Tools > UI > Build Game End Screen**.
   - It saves `Assets/Prefabs/UI/GameEnd/GameEndCanvas.prefab` and puts a GameEndCanvas in the scene.
   - If the prefab or a GameEndCanvas in the scene already exists, it asks first. **Ctrl + Z** brings the old one back.
3. Save the scene (**Ctrl + S**).

```
GameEndCanvas            Canvas (Sort Order 30) + VictoryScreen
  BackgroundPanel        dark Image over the whole screen
    TitleText            "YOU SURVIVED!"
    Status               FitToText: the box grows with the text
      StatusPanel        see-through white Image (the box)
      StatusText         the stats (written by VictoryScreen)
    PlayAgainButton      Button
      PlayAgainText      "Play Again"
```

## Settings (GameEndCanvas > VictoryScreen)

| Field | What it does |
|---|---|
| **Days To Win** | How many full days and nights to survive (counted from Day 1's dawn). |
| **Win Time** | Then the player wins at the next: **Dawn**, **Morning**, **Midday**, **Afternoon**, **Dusk**, **Night** or **Midnight**. |
| **Headline** | The first line of the stats, `{0}` = Days To Win. |

Examples (with the sun's default Parts Of The Day):

| Days To Win | Win Time | The player wins at |
|---|---|---|
| 5 | Dawn | Day 6, 05:31 (the morning after the 5th night) |
| 5 | Midday | Day 6, 12:00 |
| 5 | Night | Day 6, 18:28 (when the 6th night starts) |
| 5 | Midnight | the middle of Day 6's night, 00:00 |
| 3 | Dawn | Day 4, 05:31 |

When the game starts, the Console says the exact moment, e.g. **"Victory at Day 6, 12:00 (Midday)."**
Dawn, Morning, Afternoon, Dusk and Night follow the sun's **DaylightCycle > Parts Of The Day**; Midday is always 12:00
and Midnight always 00:00.

The stats below the headline are the same as the death screen's (PlayerStats on the Player): distance, jumps,
food, items eaten, buildings built...

## What happens when the player wins

- The page appears and the game **freezes** (time, enemies, hunger stop), the mouse is free.
- **Play Again** loads the scene again: a fresh world, Day 1, full bars, empty inventory.
- Other scripts can react with `VictoryScreen.Won += ...` (e.g. victory music).

## By hand (instead of the tool)

1. Hierarchy > right click > **UI > Canvas**, name it **GameEndCanvas**. Canvas: **Sort Order 30**.
   Canvas Scaler: **Scale With Screen Size**, 1920 x 1080, Match **0**.
2. Right click it > **UI > Image**, name **BackgroundPanel**, Anchor Presets **Alt + Shift + stretch stretch**,
   color almost black.
3. In BackgroundPanel:
   - **TitleText** (UI > Text - TextMeshPro): "YOU SURVIVED!", size 72, bold, centered, Pos Y 410, 1000 x 100.
   - **Status** (Create Empty): Pivot Y **1**, Pos Y 330, Width 1000, Add Component **FitToText**.
     Inside it **StatusPanel** (UI > Image, see-through white) and **StatusText** (TextMeshPro, size 36, white).
   - **PlayAgainButton** (UI > Button - TextMeshPro): anchor **bottom middle**, Pos Y 217, 320 x 100.
     Rename its text to **PlayAgainText**, text "Play Again".
4. Select **GameEndCanvas** > Add Component > **VictoryScreen**.
5. Keep GameEndCanvas **switched on** (the script hides it by itself). The scene needs an **EventSystem**.

The names **StatusText** and **PlayAgainButton** matter: VictoryScreen finds them by name.

## Test

- Play, select **GameEndCanvas**, VictoryScreen's **⋮** menu > **Test: Win**. Then click **Play Again**.
- Or for real: set the sun's **Day Length Minutes** to **0.5** and wait about 2.5 minutes.

| Problem | Fix |
|---|---|
| Nothing happens on Day 6 | The sun needs **DaylightCycle**, and GameRules > **Do Daylight Cycle** must be on. |
| The page shows at the start | GameEndCanvas needs **VictoryScreen** (it hides the page when the game starts). |
| Play Again does nothing | Add an **EventSystem**; the button must be named **PlayAgainButton**. |
| The new game is frozen | Shouldn't happen (Play Again un-freezes first); check the Console for errors. |
