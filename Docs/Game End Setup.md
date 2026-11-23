# Game End Setup (victory screen)

**The goal:** survive **5 days and nights**. When **Day 6** starts (the morning after the 5th night), the game stops
and the victory page shows:
- **A gold trophy** in the middle at the top (22/11/2026): it pops in with sparks, floats up and down a little, and
  light rays turn slowly behind it.
- **YOU SURVIVED!**, and under it **5 DAYS AND NIGHTS** with the day and time they won ("won on Day 6 at 05:31").
- **The coins** (22/11/2026): how many are left, how many this life earned, and how many it spent (chests, rerolls).
- **All stats** (22/11/2026), like the inventory's **Info** tab (Tab):
  - THIS LIFE: what they did (monsters killed, coins earned / spent, distance, jumps...);
  - STATUS: health, hunger, stamina, armor, walk / sprint speed, jump height, fireball, coins, with what perks added
    (green) and the armor set bonus (gold);
  - PERKS: every perk with its picture and stats (the mouse wheel scrolls a long list).

  **MINIMISE** swaps them for the short numbers (in two columns), **SHOW ALL STATS** brings them back.
- **Play Again** starts a fresh game, **Main Menu** (22/11/2026) fades to the title screen.

Dying resets the days back to Day 1 (the sun's DaylightCycle > Reset On Player Death), so the 5 days must be
survived in **one life**.

## Quick way: one click

1. Menu **Tools > UI > Build Game End Screen**, then **Rebuild** (**Build** the first time).
2. That's it: it rebuilds `Assets/Prefabs/UI/GameEnd/GameEndCanvas.prefab` in place, so the victory page in GameScene
   and in the new world both change (nothing to save in the scenes). Its VictoryScreen settings (Days To Win, Win
   Time...) stay as they are; changes made to the page by hand are replaced.
   - The first time (no prefab yet) it builds the prefab and puts a GameEndCanvas in the open scene: save the scene
     (**Ctrl + S**). A GameEndCanvas made by hand in that scene: it asks first (**Ctrl + Z** brings the old one back).
   - The trophy and its light rays are drawn the first time into `Assets/UI/GameEnd` (`Trophy.png`, `Rays.png`). Save
     your own picture there under the same name to use it instead (the tool only draws missing files).

```
GameEndCanvas                 Canvas (Sort Order 30, follows the screen's height) + VictoryScreen
  BackgroundPanel             dark Image over the whole screen
    TrophyRays                gold light rays (they turn slowly)
    TrophyGlow                a soft gold glow (it breathes)
    TrophyIcon                the trophy (pops in, floats)
    TitleText                 "YOU SURVIVED!"
    DaysText                  "5 DAYS AND NIGHTS" and when they won
    CoinsBox                  a faint gold pill, as wide as its words
      CoinIcon, CoinsText     "45 coins left   earned 120   spent 95"
    StatsToggle               MINIMISE / SHOW ALL STATS
    Status                    FitToText: the short numbers (shown after MINIMISE)
      StatusPanel             see-through white box
      StatusText              the short numbers, in two columns (written by VictoryScreen)
    StatsPanel                all stats (shown first)
      LifeColumn > LifeTitle, LifeBox > LifeText    THIS LIFE
      Info                    PlayerInfoPage: Status (rows), PerkScroll > PerkList (perks)
    PlayAgainButton           Button
      PlayAgainText           "Play Again"
    MainMenuButton            Button
      MainMenuText            "Main Menu"
```

The stats panel and the MINIMISE pill are made by the death page's tool (Tools > UI > Upgrade Death Screen), so both
pages look alike. The page follows the screen's **height** (Canvas Scaler Match 1), so it fits top to bottom on any
screen shape, also a wide browser window.

## Settings (GameEndCanvas > VictoryScreen)

| Field | What it does |
|---|---|
| **Days To Win** | How many full days and nights to survive (counted from Day 1's dawn). |
| **Win Time** | Then the player wins at the next: **Dawn**, **Morning**, **Midday**, **Afternoon**, **Dusk**, **Night** or **Midnight**. |
| **Headline** | Only for a page built before 22/11/2026 (no DaysText): the first line of the stats, `{0}` = Days To Win. |
| **Main Menu Scene** | The scene Main Menu goes back to (`MainTitleScene`; it must be in File > Build Settings). |
| **Start Showing All** | On (the built page): it opens with all stats. Off: it opens with the short numbers. |
| **Show All Text** / **Minimise Text** | The toggle's words (e.g. type MAXIMISE instead of SHOW ALL STATS). |
| **Two Columns** | The short numbers side by side in two columns (the built page ticks it; a narrow box would make them overlap). |
| **Earned Color** / **Spent Color** | The colors of "earned" and "spent" in the coins line. |
| **Rays Turn Speed** | How fast the light rays behind the trophy turn, in degrees per second (0 = still). |
| **Float Height** | How far the trophy floats up and down, in pixels (0 = still). |
| **Sparks** | How many sparks fly out when the trophy pops in (0 = none). |

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

The numbers (THIS LIFE and the short numbers) are the same as the death screen's (PlayerStats on the Player): monsters
killed, coins earned and spent, distance walked and distance run (walked + run = every meter; "23.1 km" once it's long),
jumps, stamina used, health lost / healed, food lost / eaten, items eaten, buildings built. Big numbers get a comma
("10,748").

**Coins spent** (22/11/2026) = every coin paid in this life (chests, rerolls). Nothing is counted while the player is
dead: dying puts the coins back to the start (Marko's PlayerCoins), and that isn't spending (or earning).

Fixed 22/11/2026:
- **Distance run** always said 0 m (PlayerStats asked "is the player sprinting?" at a moment in the physics step when
  the answer is always no), and all the running was counted as walking. Now running is counted as running.
- A **bigger or smaller bar** isn't healing or damage any more: a Max Health perk (which fills the new part of the
  bar) and Night Power (bars grow at night, shrink at dawn) used to count as "healed" / "lost" and "stamina used".
- A **damage number** that showed just before the page opened stayed frozen in the middle of the screen (time stops
  on this page). Damage numbers now fade on real time, so they're gone a moment later.

## What happens when the player wins

- Every other open menu closes, the page appears and the game **freezes** (time, enemies, hunger stop), the mouse is
  free. The trophy and the panel pop in.
- **MINIMISE** / **SHOW ALL STATS** switch between all stats and the short numbers.
- **Play Again** loads the scene again: a fresh world, Day 1, full bars, empty inventory.
- **Main Menu** fades to the title screen (the page stays up meanwhile, its buttons off).
- Other scripts can react with `VictoryScreen.Won += ...` (e.g. victory music).

## By hand (instead of the tool)

1. Hierarchy > right click > **UI > Canvas**, name it **GameEndCanvas**. Canvas: **Sort Order 30**.
   Canvas Scaler: **Scale With Screen Size**, 1920 x 1080, Match **1** (follow the height).
2. Right click it > **UI > Image**, name **BackgroundPanel**, Anchor Presets **Alt + Shift + stretch stretch**,
   color almost black.
3. In BackgroundPanel (Pos Y = up from the middle of the screen):
   - **TrophyIcon** (UI > Image): Source Image `Assets/UI/GameEnd/Trophy.png` (or your own), Preserve Aspect,
     150 x 150, Pos Y 425. Optional, above it in the Hierarchy (= drawn behind it): **TrophyRays**
     (`Assets/UI/GameEnd/Rays.png`, see-through gold, 330 x 330) and **TrophyGlow** (`Assets/UI/Common/Glow.png`,
     gold, 285 x 285), both at Pos Y 425.
   - **TitleText** (UI > Text - TextMeshPro): "YOU SURVIVED!", size 68, bold, centered, Pos Y 308, 1100 x 90.
   - **DaysText** (TextMeshPro, Rich Text on): centered, Pos Y 238, 1100 x 72.
   - **CoinsBox** (UI > Image, left above the stats) with a **CoinIcon** Image and a **CoinsText** TextMeshPro inside.
     A Horizontal Layout Group (Control Child Size on) + a Content Size Fitter (Horizontal Fit = Preferred Size) make
     it as wide as its words.
   - **StatsToggle** (UI > Button - TextMeshPro, its text can have any name): top right above the stats.
   - **Status** (Create Empty): Pivot Y **1**, Pos Y 124, Width 1163, Add Component **FitToText**.
     Inside it **StatusPanel** (UI > Image, see-through white) and **StatusText** (TextMeshPro, size 32, white,
     Wrapping off).
   - **StatsPanel**: like the death page's (`Docs/Death Screen Setup.md`, "The new parts by hand", step 4), at Pos Y
     124 with Pivot Y 1, but leave PlayerInfoPage's **Last Life** OFF (the victory page follows the player; the game
     is frozen anyway). Switch StatsPanel off.
   - **PlayAgainButton** and **MainMenuButton** (UI > Button - TextMeshPro): anchor **bottom middle**, Pos Y 70,
     300 x 80, Pos X -170 and 170. Rename their texts to **PlayAgainText** / **MainMenuText**: "Play Again" / "Main
     Menu".
4. Select **GameEndCanvas** > Add Component > **VictoryScreen**. Tick **Two Columns** if Status is wide (1100 or more).
5. Keep GameEndCanvas **switched on** (the script hides it by itself). The scene needs an **EventSystem**.

The names matter: VictoryScreen finds the parts by name. **StatusText** and **PlayAgainButton** are needed; the others
can be left out (the page then works without that part).

## Test

- Play, select **GameEndCanvas**, VictoryScreen's **⋮** menu > **Test: Win**: the trophy pops in with sparks, the
  coins show, and all stats (your perks, your armor). Click **MINIMISE** (the short numbers) and **SHOW ALL STATS**.
  Then **Main Menu** (fades to the title screen) or **Play Again** (a fresh game).
- Or for real: set the sun's **Day Length Minutes** to **0.5** and wait about 2.5 minutes.

| Problem | Fix |
|---|---|
| No trophy, coins, all stats or Main Menu | Run **Tools > UI > Build Game End Screen** (Rebuild). |
| Nothing happens on Day 6 | The sun needs **DaylightCycle**, and GameRules > **Do Daylight Cycle** must be on. |
| The page shows at the start | GameEndCanvas needs **VictoryScreen** (it hides the page when the game starts). |
| Play Again does nothing | Add an **EventSystem**; the button must be named **PlayAgainButton**. |
| Main Menu fades out and back in | `MainTitleScene` isn't in File > Build Settings (or VictoryScreen's **Main Menu Scene** has another name). |
| The new game is frozen | Shouldn't happen (Play Again and Main Menu un-freeze first); check the Console for errors. |
| The short numbers overlap each other | The Status box is too narrow for two columns: untick **Two Columns**, or rebuild the page. |
| The page runs off the top and bottom of a wide screen | A page made by hand with Canvas Scaler Match 0: set Match to **1**, or rebuild the page. |
| Distance run: 0 m | Fixed 22/11/2026 (see the numbers above): running is counted now. |
| A number hangs in the middle of the page | A damage number from the last hit; fixed 22/11/2026 (it fades away now). |
| Monsters killed stays 0 | Only kills count (monsters burning away at dawn don't); Marko's EnemyCombat tells Tony's MonsterBounty about each kill. |
| Humans killed stays 0 | Human enemies live in the new world only (Tools > Enemies > Build Human Enemies, `Docs/Human Enemies Setup.md`); a human you killed counts here, not under Monsters killed (since 23/11/2026). |
