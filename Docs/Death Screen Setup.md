# Death Screen Setup (the GAME OVER page)

When the player's health reaches 0, the **GAME OVER** page shows:
- **How long they survived** (21/11/2026), in gold under the title: "YOU SURVIVED 2 DAYS", and smaller "and 6 hours ·
  died on Day 3 at 14:30" (game time, from the start of that life). Under a day: "YOU SURVIVED 6 HOURS".
- **What they did in that life:** monsters killed, coins earned / spent, distance walked / run, jumps, stamina used,
  health lost / healed, food lost / eaten, items eaten, buildings built (the victory page shows the same; see
  `Docs/Game End Setup.md` for what was fixed and added on 22/11/2026). Coins spent = paid for chests and rerolls; the
  coins lost by dying (they go back to the start) aren't counted.
- **SHOW ALL STATS / MINIMISE** (21/11/2026): a button under the numbers. SHOW ALL STATS swaps them for a big panel
  with everything about the character **at the moment they died**, like the inventory's **Info** tab (Tab):
  - THIS LIFE: the numbers above;
  - STATUS: health, hunger, stamina, armor, walk / sprint speed, jump height, fireball, coins, with what perks added
    (green) and the armor set bonus (gold);
  - PERKS: every perk they had, with its picture and stats (the mouse wheel scrolls a long list).

  MINIMISE goes back to the short numbers. Dying resets the perks, max health, coins, the day and drops the armor, so the
  page keeps them from the moment just before.
- **Respawn** brings them back at the respawn point with full health, food and stamina. **Main Menu** (21/11/2026)
  fades to the title screen.

**In the new world** (21/11/2026) Respawn makes a **brand-new world** instead (World Generator > **New World On
Death**, on): the page stays up with its buttons switched off, the screen fades to black ("DAY 1 - A new world") and a
new random world loads, Day 1, everything fresh, like a new game. While the page is up, the day's clock stands still
in both worlds (the day already went back to its start when the player died), so a long look at the page doesn't let
night or the next day go by.

**Items:** decided by the GameRule **Keep Inventory** (on the GameRules object):
- **Off** (default, like Minecraft): every stack (inventory, hotbar, cursor) is thrown out around the body
  (ItemDropper on the Player). Walk back and pick them up. They disappear after ItemDropper's Despawn Minutes (5).
- **On**: the player keeps everything.

## Quick way: one click (for the days, SHOW ALL STATS and Main Menu)

1. Menu **Tools > UI > Upgrade Death Screen**, then **Upgrade**.
2. That's it: it changes `Assets/Prefabs/UI/Dead/DeadCanvas.prefab`, so the death page in GameScene and in the new
   world both change (nothing to save in the scenes).

It needs the death page made by hand first (sections 1 and 2 below). The tool:
- adds **DaysText** under **TitleText** (gold, two lines);
- moves the stats box (**Status**) so it hangs down from under the days (its top stays put, it grows downward with its
  text) instead of growing both ways into them;
- adds **StatsPanel** (hidden) where the stats box is: a light box like the inventory window with THIS LIFE on the left
  and the Info tab's STATUS and PERKS, made by the Info tab's own builder, drawn at 75 % so it fits between the days
  and the button;
- adds **StatsToggle**, a see-through pill under the stats ("SHOW ALL STATS", gold under the mouse);
- moves **RespawnButton** a little to the left and lower, and puts a copy of it beside it: **MainMenuButton**
  ("Main Menu"), so both keep your button's look.

Run it again any time: its own parts are made again; your title, stats box and Respawn button keep their look (only
their places are set). The victory page (Tools > UI > Build Game End Screen) makes its stats panel and pill with this
tool's code, so both pages look alike. Since 22/11/2026 a long line in THIS LIFE shrinks to fit instead of breaking in
two (run the tool again to get that on the death page).

## 1. The Player

Select the **Player** and Add Component (if it isn't there yet):

| Script | What it does |
|---|---|
| **PlayerRespawn** | Brings the player back. **Respawn Point** = where (default 0, 0.5, 0). |
| **PlayerStats** | Counts this life: monsters killed, coins earned / spent, distance walked / run, jumps, stamina used, health lost / healed, food lost / eaten, items eaten, buildings built. |

## 2. The page (DeadCanvas)

The hierarchy (the names matter, the script finds them by name):

```
DeadCanvas              <- Canvas + DeathScreen script
  BackgroundPanel       <- an Image covering the screen
    TitleText           <- "GAME OVER" (anything you like)
    DaysText            <- how long they survived (added by the tool)
    Status              <- FitToText: the stats box (hidden while all stats show)
      StatusPanel       <- an Image (the background)
      StatusText        <- the stats are written here (TextMeshPro or legacy Text)
    StatsPanel          <- all stats (added by the tool, switched off)
      LifeColumn > LifeTitle, LifeBox > LifeText    <- THIS LIFE
      Info              <- PlayerInfoPage (Last Life on): Status (rows), PerkScroll > PerkList (perks)
    StatsToggle         <- a Button with a text inside: SHOW ALL STATS / MINIMISE (added by the tool)
    RespawnButton       <- a Button
      RespanText        <- the button's label, e.g. "Respawn"
    MainMenuButton      <- a Button (added by the tool: a copy of RespawnButton)
      MainMenuText      <- "Main Menu"
```

1. Select **DeadCanvas** > Add Component > **DeathScreen**.
2. Canvas: set **Sort Order** higher than the HUD's (e.g. 10), so the page covers the bars.
3. Leave DeadCanvas **switched on** (ticked) in the Hierarchy. The script hides it when the game starts.
   (It turns off the Canvas component, not the object, so it still hears the player die.)
4. To give the stats a background that grows with the text, group them like this and put **FitToText** on **Status**:

   ```
   Status        <- FitToText (set its Width; its height follows the text. Pivot Y = 1 = grows downward)
     StatusPanel <- an Image (the background)
     StatusText  <- the stats text
   ```

   FitToText stretches the panel behind the text and the text inside it (Padding = the margin), and sets
   Status's height every frame. Don't also add a Vertical Layout Group / Content Size Fitter to Status.
5. The scene needs an **EventSystem** for the button to be clickable (Tools > UI > Build Inventory UI adds one,
   or right click in the Hierarchy > UI > Event System).

Nothing to drag in: no On Click to set up on the buttons.

### The new parts by hand (instead of Upgrade Death Screen)

1. **DaysText:** under BackgroundPanel, **UI > Text - TextMeshPro**, name it `DaysText`, under the title, about
   1100 x 86, centered, gold, Rich Text on.
2. **MainMenuButton:** select RespawnButton, **Ctrl + D**, name the copy `MainMenuButton`, change its text to "Main
   Menu", and move the two buttons side by side.
3. **StatsToggle:** **UI > Button - TextMeshPro**, name it `StatsToggle` (its text can have any name).
4. **StatsPanel:** an empty UI object named `StatsPanel` where the stats box is, with an Image behind. Inside it, a
   text named `LifeText` (this life's numbers) and the Info tab's rows and perk list (copy the inventory's
   `Pages > InfoPage` children into it) with **PlayerInfoPage**, its **Last Life** ticked. Switch StatsPanel off.

## Settings (DeathScreen, on DeadCanvas)

| Setting | What it does |
|---|---|
| **Main Menu Scene** | the scene Main Menu goes back to (`MainTitleScene`; it must be in File > Build Settings) |
| **Start Showing All** | off = the page opens with the short numbers; on = it opens with all stats |
| **Show All Text** / **Minimise Text** | the toggle's words |

## How it works

| Part | Where | What it does |
|---|---|---|
| **PlayerHealth** | the Player | At 0 health it fires **Dying** first (everything still as it was) and then **Died** (PlayerRespawn, PlayerPerks, PlayerCoins, ItemDropper and DaylightCycle reset or drop things there). |
| **DeathScreen** | DeadCanvas | On Dying it remembers the days survived and has the panel capture the character; on Died it shows the page; the buttons: Respawn, Main Menu (ScreenFader to the title scene), the toggle. |
| **DaylightCycle** | the sun | `DaysSurvived`: game time since this life started, in days (2.25 = two days and six hours). |
| **PlayerInfoPage** | StatsPanel > Info | The Info tab's script. With **Last Life** on it doesn't follow the player: it only writes what Capture saw the moment the player died. |
| **PlayerStats** | the Player | This life's numbers (the short stats and THIS LIFE). |

## Test

1. Press **Play**, walk, sprint, jump, eat something, take a perk from a chest, put on some armor.
2. Click the Player > PlayerHealth's **⋮** menu > **Test: Die** (or type 0 into its Health).
3. The page appears: GAME OVER, "YOU SURVIVED ..." in gold, the numbers, the mouse is free.
4. Click **SHOW ALL STATS**: the panel pops in with THIS LIFE, STATUS (your max health with the perk's bonus in green,
   the armor you wore, your coins) and PERKS (the perk you took). Click **MINIMISE**: back to the numbers.
5. Click **Respawn**: you're back at the respawn point, the bars are full and the numbers start from 0 again. Die
   again and click **Main Menu**: the screen fades to the title screen.

### If something doesn't work

| Problem | Fix |
|---|---|
| Respawned at once, no page | DeadCanvas needs **DeathScreen**, and DeadCanvas must be switched on in the Hierarchy. |
| Page shows, no numbers | The Player needs **PlayerStats**; the text object must be named exactly **StatusText**. |
| Button can't be clicked | Add an **EventSystem**; the button must be named exactly **RespawnButton**; BackgroundPanel must not block it (the button is its child, so it's fine). |
| Page behind the health bars | Raise DeadCanvas's **Sort Order**. |
| No days line, no SHOW ALL STATS, no Main Menu | Run **Tools > UI > Upgrade Death Screen**. The days line also needs a **DaylightCycle** on the sun. |
| SHOW ALL STATS shows no perks / reset stats | StatsPanel > Info > PlayerInfoPage needs **Last Life** ticked (the tool ticks it). |
| Main Menu fades out and back in | `MainTitleScene` isn't in File > Build Settings (or DeathScreen's **Main Menu Scene** has another name). |
| Without a death page | Just leave DeathScreen out: PlayerRespawn then respawns straight away. |
