# Main Title Setup (title screen, New Game, Settings)

The game now starts on a title screen, **UNKNOWN SURVIVAL**, with three buttons:

- **PLAY** opens the **New Game** page. Every option there starts at the game *as it is now*. **START GAME** loads
  GameScene with the player's choices.
- **SETTINGS** opens the **Settings** page with tabs. **Key Bindings** works now; **Controls**, **Display** and **Audio**
  show an animated "INCOMING..." message until they're made.
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
| **Days to win** (1 - 30) | GameEndCanvas > VictoryScreen > Days To Win | from GameScene |
| **Win at** | VictoryScreen > Win Time (Dawn, Morning, Midday, Afternoon, Dusk, Night, Midnight; times from the sun) | from GameScene |
| **Day length** | the sun's Day Length Minutes: Short = half, Normal = as now, Long = double | Normal |
| **Start time** | the time of day the first day starts (and where dying brings you back to) | from GameScene ("Now (hh:mm)" if it isn't one of the listed times) |
| **Difficulty** | Peaceful = no enemies; Easy = half the enemies, slower hunger; **Normal = as now**; Hard = 1.5x enemies, faster hunger | Normal |
| **When I die** | Keep items / Drop items / Lose items (GameRules Keep Inventory / Clear Item On Dead) | from GameScene |
| **Pick up dropped items by walking over them** | GameRules Auto Pickup Drops | from GameScene |
| **Show "+3 Wood" messages** | GameRules Show Notifications | from GameScene |

The difficulty numbers can be changed on **NewGamePage > NewGamePanel**: Easy / Hard enemies and hunger
(compared to Normal), and Short / Long day.

Pressing **Play straight in GameScene** (testing) still uses GameScene's own settings: the choices are only used
after START GAME. **Play Again** on the victory screen keeps them.

## The Settings page: Key Bindings

- Click a key box, then press the new key. **Esc** cancels. The choice is saved, so it works in the game too.
- If another row already uses that key, the two **swap**, and that row shakes red so you can see what moved.
  Normal play and building are separate, so left click can both place buildings and do other things.
- **Attack (left click)** and **Fireball (Q)** are greyed out: they're read straight from the keyboard in Marko's
  PlayerCombat / PlayerFireball, so they can't change until those scripts use KeyBindings.
- **RESET ALL** puts every key back to its default (GameScene's KeyBindings).

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
  Left empty, it shows Project Settings > Player > Version.
- **Colors and sizes:** in the Inspector of each object, or in the Fields at the top of the builder for every rebuild.

## By hand (instead of the tool)

The title screen has over a hundred objects, so the tool is the recommended way. If you rebuild a piece by hand,
keep these **names**: the scripts find their parts by name.

- **TitleMenu** (on the menu canvas) looks for: `Pages`, `TitlePage`, `NewGamePage`, `SettingsPage`, `Title`,
  `Subtitle`, `Buttons`, `Footer`, `Version`, `PlayButton`, `SettingsButton`, `QuitButton`, a `BackButton` in each sub
  page, `StartButton` (with a `Flash` child), `Shockwave`, `IntroBlack`.
- **NewGamePanel** (on NewGamePage) looks for: `DaysRow` (StepperRow with `Minus`, `Plus`, `Value`), `WinAtRow` and
  `StartTimeRow` (each with a TextMeshPro `Dropdown`), `DayLengthRow`, `DifficultyRow`, `DeathRow` (ChoiceRows with an
  `Options` holder and a `Highlight`), `PickupRow` and `MessagesRow` (each with a `Toggle`), `DayLengthRow/Info`,
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

| Problem | Fix |
|---|---|
| The New Game options show nothing | NewGamePage > NewGamePanel > **Defaults** is empty: run the tool again. |
| START GAME stays on black | GameScene must be in File > Build Settings (the tool adds it), and the scene name must match TitleMenu's **Game Scene**. |
| The defaults don't match GameScene | Save GameScene once (they're read on save), or run the tool again. |
| No embers | Select **Embers**: its Renderer needs a material (Default-ParticleSystem). |
| The game doesn't start on the title screen in a build | MainTitleScene must be scene **0** in File > Build Settings (the tool puts it there). |
