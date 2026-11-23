# Main Title Setup (title screen, New Game, Settings)

The game now starts on a title screen, **UNKNOWN SURVIVAL**, with three buttons:

- **PLAY** opens the **New Game** page. Every option there starts at the game *as it is now*. **START GAME** loads
  GameScene with the player's choices.
- **SETTINGS** opens the **Settings** page with tabs. **Key Bindings**, **Controls** and **Display** work (since
  21/11/2026: Controls = **Mouse sensitivity** 1 - 10, 5 = the game's normal speed, on a slider with - / +, and
  **Invert mouse**; Display = **Fullscreen** and the **damage numbers**, `Docs/Display Settings Setup.md`); **Audio**
  shows an animated "INCOMING..." message until it's made. The same KEY BINDINGS, CONTROLS and DISPLAY are in the Esc
  **pause menu** while playing (`Docs/Pause Menu Setup.md`), and both save to the computer, so they always match.
- **QUIT** closes the game. It's hidden in the WebGL build, because a web page can't close itself.

## Quick way: one click

1. Menu **Tools > UI > Build Main Title Scene**.
   - It asks to save the open scene first (the title scene is built as a new scene).
   - If MainTitleScene already exists, it asks before replacing it.
   - If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.
2. That's it. It opens the new scene. Press **Play** to see it.

What it makes:

| File | What it is |
|---|---|
| `Assets/Scenes/MainTitleScene.unity` | the title scene, now **first** in File > Build Settings (GameScene second), so the built game starts here |
| `Assets/Prefabs/UI/Title/TitleCanvas.prefab` | the menu (all three pages) |
| `Assets/Config/NewGame/NewGameDefaults.asset` | the game as it is now, read from GameScene (the New Game defaults) |
| `Assets/UI/Title/*.png`, `TitleText.mat` | the soft shapes (sky gradient, vignette, glow, ring, stripes) and the glowing title material. Replace a PNG with your own art if you like: the tool never overwrites existing files |

```
MainTitleScene
  Main Camera            (black background, AudioListener)
  EventSystem
  KeyBindings            (the same default keys as GameScene)
  BackgroundCanvas       TitleBackground: the sky that cycles night -> dawn -> night, with stars
    SkyBottom, SkyTop, Stars
  Embers                 a Particle System: glowing embers drifting up between the sky and the menu
  TitleCanvas            TitleMenu (prefab)
    Vignette             dark screen edges
    Pages
      TitlePage          Title (TitleTextFX), Subtitle, Buttons (CascadeIn: PLAY / SETTINGS / QUIT), Footer
      NewGamePage        NewGamePanel: BackButton, DefaultsButton, Panel > Rows, Summary, StartGlow, StartButton
      SettingsPage       BackButton, Tabs (TabGroup + TabFX), TabPages > KeyBindingsPage / ControlsPage / DisplayPage / AudioPage
    FX > Shockwave       the START GAME ring
    IntroBlack           the black screen the opening fades in from
```

## The New Game page

**Every option's default is the game as it is now.** Leave everything alone, press START GAME, and you get exactly
today's game. These values are read from GameScene and kept in `NewGameDefaults.asset`:

- They're read again **every time GameScene is saved**. Change something in GameScene, save, and the defaults
  follow (the Console says "New Game defaults updated from GameScene").
- The game only changes the options the player **changed**. A changed option gets a small **gold dot**.
- **DEFAULTS** (top right) puts every option back.

| Option | What it changes in the game | Default |
|---|---|---|
| **Map** (WORLD) | **Old world** = GameScene, the hand-made map; **New world** = ProceduralGenerateScene, a new random endless world every game (made by **Tools > World > Build New World Scene**, guide `Docs/New World Setup.md`). START GAME loads TitleMenu's **Game Scene** or **New World Scene** | **New world** (since 23/11/2026; NewGamePanel > **New World By Default**, untick it for Old world) |
| **Days to win** (1 - 30) | GameEndCanvas > VictoryScreen > Days To Win | from GameScene |
| **Win at** | VictoryScreen > Win Time (Dawn, Morning, Midday, Afternoon, Dusk, Night, Midnight; times from the sun) | from GameScene |
| **Day length** | the sun's Day Length Minutes: Short = half, Normal = as now, Long = double | Normal |
| **Start time** | the time of day the first day starts (and where dying brings you back to) | from GameScene ("Now (hh:mm)" if it isn't one of the listed times) |
| **Difficulty** | How often the night monsters come and how fast hunger drains. Peaceful = no monsters (so no monster coins: only the starting coins open chests), normal hunger; Easy = monsters half as often (the seconds between them doubled: night 1 every 20 s), at most 20 out at once and ones left more than 90 m behind (out of sight) give up, hunger 30% slower; **Normal = as now** (a monster every 10 s on night 1, every 7 s on night 2, every 5 s from night 3 on); Hard = monsters 1.5 times as often (night 1 every 6.7 s, from night 3 every 3.3 s), hunger 50% faster. On Normal and Hard nothing limits how many are out at once: the ones you don't kill gather until dawn. The two lines under the buttons explain the picked one | Normal |
| **Starting coins** (DIFFICULTY, 22/11/2026) | the coins the player starts with (Marko's PlayerCoins > Starting Coins), and what dying puts them back to. **-** / **+** change it by 10; or click the number, type one (0 - 9999) and press Enter or click somewhere else (Esc puts the old number back) | 20 (from GameScene) |
| **When I die** | Keep items / Drop items / Lose items (GameRules Keep Inventory / Clear Item On Dead) | from GameScene |
| **Pick up dropped items by walking over them** | GameRules Auto Pickup Drops | from GameScene |
| **Show "+3 Wood" messages** (RULES, left) | GameRules Show Notifications | from GameScene |
| **Pause menu (Esc) pauses the game** (RULES, right; 21/11/2026) | GameRules Pause Game: on = the Esc pause menu stops the game while it's open (monsters, the sun, hunger); off = the game goes on behind it (`Docs/Pause Menu Setup.md`) | on (from GameScene) |
| **Start with all tools** (TESTING, left) | GameRules Tool Test: one of every tier tool (pickaxe, axe, sword x Wood ... Diamond) in the inventory from the start (ToolTestKit) | off (from GameScene) |
| **Start with all armor** (TESTING, middle) | GameRules Armor Test: every armor piece, the Copper and Iron sets (12), in the inventory from the start (ArmorTestKit) | off (from GameScene) |
| **Old chests / New chests** (TESTING, right) | GameRules New Chests: the two loot box systems. Old = every box lets you choose 1 of 3; New = random common and epic boxes (a slot-machine roll), a legendary box, REROLL at every box (both: REROLL only once the Reroll perk is found; `Docs/Loot Chests Setup.md`) | New chests (from GameScene) |

The difficulty numbers can be changed on **NewGamePage > NewGamePanel**: Easy / Hard enemies (how often the night
monsters come compared to Normal: 0.5 = half as often, 1.5 = half as many again) and hunger (compared to Normal), and
Short / Long day.

**How Easy and Hard work** (22/11/2026): Marko's EnemySpawner spawns a monster every few seconds all night, with its
own **Night 1 - 5 Spawn Interval** (10 / 7 / 5 / 5 / 5 seconds). When the game loads, GameSetup multiplies those seconds:
x2 on Easy, / 1.5 on Hard (his file isn't changed: they're public settings). Easy also turns on two limits in Tony's
NightMonsters: at most 20 alive at once (Marko's spawner asks it before each spawn, a `[Tony]` line) and far unseen ones
give up after 90 m (NewGamePanel > **Easy Most At Once** / **Easy Leave Distance**). The explanation under the buttons reads
the same seconds from GameScene (saved into `NewGameDefaults` every time GameScene is saved), so if Marko changes them,
the New Game page follows. Every night after night 5 keeps night 5's seconds (a `[Tony]` line in Marko's
`GetSpawnInterval`, 22/11/2026; before, no monsters came after night 5, which a game of more than 5 days to win reached).

Pressing **Play straight in GameScene** (testing) still uses GameScene's own settings: the choices are only used
after START GAME. **Play Again** on the victory screen keeps them.

The **TESTING** section is new (19/11/2026; the armor tick box 20/11/2026, Old / New chests 21/11/2026): run the tool
again to get it. An older title scene without it still works. Its three parts share one row (`TestingRow`), because the
panel has no room for another row.

**Starting coins** is new too (22/11/2026): run the tool again to get it. To make room for it, **Days to win** and **Win
at** now share one row (`GoalPairRow`). An older title scene without it still works (the game keeps PlayerCoins'
Starting Coins). While you type the number, Esc only stops the typing; it doesn't leave the page.

**The two-line Difficulty explanation** is new too (22/11/2026): run the tool again to get it. To make room, the rows
are 2 px shorter (48) and the section titles 2 px shorter (32). An older title scene shows only the first line (what
the level means), because its `DifficultyInfo` is one line tall.

## The Settings page: Key Bindings

- Click a key box, then press the new key. **Esc** cancels. The choice is saved, so it works in the game too.
- If another row already uses that key, the two **swap**, and that row shakes red so you can see what moved.
  Normal play and building are separate, so left click can both place buildings and do other things.
- **Attack (left click)** and **Fireball (Q)** are greyed out: they're read straight from the keyboard in Marko's
  PlayerCombat / PlayerFireball, so they can't change until those scripts use KeyBindings.
- **ITEMS** (21/11/2026): **Grab item (on the ground)** = F (dropped stacks, apples: ItemGrabber), **Open chest / Pick
  berries** = E (KeyBindings' Interact: Marko's chests and Turki's berry bushes read it, so changing it here changes
  both, and the "[E] Open chest" prompt follows), **Eat / Levitate** = right click. Keys saved on a computer before
  then had Interact on the right mouse button (where it did nothing): the game forgets that one once, so it's E.
- **RESET ALL** puts every key back to its default (GameScene's KeyBindings).
- **GAME > Pause menu** (21/11/2026) = Esc: in the game it opens the pause menu (it was "Free the mouse").
- **GAME > Fullscreen (or Alt + Enter)** (21/11/2026) = F11: switches fullscreen anywhere, the title screen too.

## The Settings page: Controls

- **Mouse sensitivity:** drag the slider (tenths) or click - / + (half a step); the number pops. 1 = a fifth of the
  normal speed, 5 = normal (Main Camera > CameraController > Sensitivity), 10 = twice. Saved when the page closes.
- **Invert mouse (up / down):** a tick box.
- **RESET:** back to 5, not inverted.

The page is `ControlsPage` (ControlsMenu, a SliderRow named SensitivityRow, a Toggle in InvertRow). The values live in
`MouseSettings` (PlayerPrefs), which the camera reads every frame.

## The Settings page: Display

- **Fullscreen** (also Alt + Enter or F11; in the browser Esc leaves it).
- **DAMAGE NUMBERS**, one framed box: **Show damage numbers** and, indented under it and joined by a line, **Only
  monsters and animals** (locked and faded while Show damage numbers is off), with a sample of a normal and a critical
  number. **RESET:** numbers back on, for every hit.

The page is `DisplayPage` (DisplayMenu). The values live in `DisplaySettings` (PlayerPrefs). Everything else:
`Docs/Display Settings Setup.md`.

The list is the **Rows** table at the top of `KeyBindingMenu.cs`. Add a line there for a new key.

## The animations

| When | What happens | Where to tune it |
|---|---|---|
| Scene opens (click or any key skips it) | fade from black; title letters fall in and bounce, then a light sweeps over them; the line under it types itself; the buttons rise one by one; version and credits fade in | TitleCanvas > TitleMenu: **Play Intro** (Always / First Time Only / Never). Title > TitleTextFX: drop height, letter delay, fall time |
| Always | the sky cycles night blue -> purple -> dawn orange; stars twinkle and fade at dawn; embers drift up; the title floats in a wave and shines every few seconds; START GAME's gold glow pulses | BackgroundCanvas > TitleBackground (colors, Cycle Seconds, stars); Embers (Particle System); TitleTextFX (wave, shine); StartGlow > PulseFX |
| Mouse over a button | grows 10%, turns gold, text turns dark, a shine sweeps across, a ">" slides in, the buttons next to it dim | each button's ButtonFX |
| Click | squashes, then springs back past its size; a ripple spreads from the click; sparks fly out | ButtonFX (Press Scale, Ripple, Sparks); TitleMenu > **Sparks** turns all sparks off |
| Changing page | the page slides away, the new one slides in, its rows cascade in one by one | the page's CascadeIn (offset, stagger, duration) |
| New Game page | the days number rolls like an odometer; the gold highlight glides to the picked option (Difficulty colors it green / blue / gold / red); ticks pop; dropdowns unfold; changed options get a dot | StepperRow, ChoiceRow (Option Colors), TogglePop, DropdownUnfold |
| START GAME | white flash, a shockwave ring across the screen, a big burst of sparks, the menu zooms away, then **"DAY 1 - Survive N nights"** stamps in while GameScene loads, and the game fades in | TitleMenu (code), ScreenFader (code) |
| Settings tabs | the gold underline glides to the tab; the page slides in from that side; key rows cascade in; a waiting key box pulses gold, a set key pops, a moved key flips over | TabFX, KeyBindingMenu |
| QUIT | the title shrinks away and the screen fades to black | TitleMenu |

**For testing:** TitleMenu > **Animation Speed** 2 makes everything twice as fast, and **Play Intro = Never** skips
the opening.
**Sounds:** the project has no sound files yet. Drag sounds into TitleMenu's **Hover / Click / Page / Start Sound**
slots (a ButtonFX with its own sound keeps it). Empty slots stay silent.

## Changing the look

- **The title text:** select TitleCanvas > Pages > TitlePage > **Title** and change the text, then **Overrides > Apply
  All**. To keep it for every rebuild, change `GameTitle` at the top of `Editor/TitleSceneBuilder.cs`.
- **Credits:** the **Credits** text in the Footer (or `Credits` in the builder). The **version** is the
  **Version** text in the Footer (TitleCanvas > Pages > TitlePage > Footer > Version): type e.g. `v1.1` there.
  Left empty, it shows Project Settings > Player > Version. Running the tool again keeps what you typed there (it
  reads the old Version text before it replaces the scene).
- **Colors and sizes:** in the Inspector of each object, or in the Fields at the top of the builder for every rebuild.

## By hand (instead of the tool)

The title screen has over a hundred objects, so the tool is the recommended way. If you rebuild a piece by hand,
keep these **names**: the scripts find their parts by name.

- **TitleMenu** (on the menu canvas) looks for: `Pages`, `TitlePage`, `NewGamePage`, `SettingsPage`, `Title`,
  `Subtitle`, `Buttons`, `Footer`, `Version`, `PlayButton`, `SettingsButton`, `QuitButton`, a `BackButton` in each sub
  page, `StartButton` (with a `Flash` child), `Shockwave`, `IntroBlack`.
- **NewGamePanel** (on NewGamePage) looks for: `DaysRow` (StepperRow with `Minus`, `Plus`, `Value`), `WinAtRow` and
  `StartTimeRow` (each with a TextMeshPro `Dropdown`; DaysRow and WinAtRow sit side by side in `GoalPairRow`, a
  Horizontal Layout Group like `TestingRow`), the optional `CoinsRow` (a StepperRow with `Minus`, `Plus` and a
  TextMeshPro **Input Field** instead of `Value`, so the number can be typed; its Step is 10), `DayLengthRow`,
  `DifficultyRow`, `DeathRow` (ChoiceRows with an
  `Options` holder and a `Highlight`), `PickupRow`, `MessagesRow`, `AllToolsRow` and `ArmorTestRow` (each with a
  `Toggle`) and `ChestRow` (a ChoiceRow: Old chests / New chests, no label); the last three are optional and sit side by
  side in `TestingRow`, a Horizontal Layout Group with Control Child Size and Child Force Expand ticked; the optional
  `PauseRow` (a `Toggle`) sits beside `MessagesRow` in `RulesPairRow` the same way (the panel's rows are full); the optional
  `WorldRow` (a ChoiceRow: Old world / New world, with an `Info` text),
  `DayLengthRow/Info`,
  `DifficultyInfo`, `Summary`, `DefaultsButton`, and a `Dot` in each row.
- **KeyBindingMenu** (on KeyBindingsPage) looks for: `Content`, the switched-off `HeaderTemplate`, `RowTemplate`
  (`Label`, `Key`, `Key2`), `HotbarRowTemplate` (`Label`, `Key1` ... `Key8`), and optionally `Hint`, `ResetAllButton`.
- **TitleBackground** (on a Screen Space - Camera canvas, Plane Distance 100) looks for `SkyBottom`, `SkyTop`, `Stars`.
- A button gets the effects by adding **ButtonFX** (its text named `Label`, an optional switched-off `Arrow`).

## Test

1. Open **MainTitleScene**, press **Play**: the opening plays. Press Play again and click during it: it skips.
2. Hover and click the buttons: gold, shine, arrow, squash, ripple, sparks.
3. **PLAY**: change Days to win (the number rolls), pick Hard (red highlight), tick and untick a rule. Dots appear and
   the summary changes. **DEFAULTS** puts it all back.
4. **START GAME**: flash, shockwave, "DAY 1", then the game fades in. Check the Console: "Victory at Day ..." matches
   Days to win.
5. **SETTINGS**: click a tab (the underline glides), change Jump to another key, **RESET ALL**.
6. **PLAY**, tick **Start with all tools**, START GAME: the 15 tier tools are in the inventory.
7. **PLAY**, tick **Start with all armor** (a gold dot appears next to it), START GAME: the 12 armor pieces (Copper and
   Iron) are in the inventory; Tab > Gear to put them on.
8. **PLAY**, pick **Old chests** (a gold dot), START GAME: every chest lets you choose 1 of 3. New chests (the default):
   common chests roll a random perk.
9. **PLAY**, click the **Starting coins** number, type 500 and press Enter (a gold dot appears), START GAME: the HUD
   shows 500 coins, and dying puts them back to 500. Click **+** / **-**: it changes by 10. Type something and press
   Esc: the old number comes back and the page stays.

| Problem | Fix |
|---|---|
| The New Game options show nothing | NewGamePage > NewGamePanel > **Defaults** is empty: run the tool again. |
| START GAME stays on black | GameScene must be in File > Build Settings (the tool adds it), and the scene name must match TitleMenu's **Game Scene**. |
| Map: New world starts the old world | ProceduralGenerateScene isn't in Build Settings: run **Tools > World > Build New World Scene** (the Console says so). |
| The defaults don't match GameScene | Save GameScene once (they're read on save), or run the tool again. |
| No Starting coins row | Run the tool again (22/11/2026). |
| A typed number doesn't stay | It's kept when you press Enter or click somewhere else; an empty box gets its number back, and it's kept between 0 and 9999. |
| No embers | Select **Embers**: its Renderer needs a material (Default-ParticleSystem). |
| The game doesn't start on the title screen in a build | MainTitleScene must be scene **0** in File > Build Settings (the tool puts it there). |
