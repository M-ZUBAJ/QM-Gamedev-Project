# Pause Menu Setup (Esc: key bindings and mouse settings while playing)

**What the player sees:**
- **Esc** while playing opens the **pause menu**: the game stops (monsters, the sun, hunger, the player), the mouse is
  free, and the game's own keys don't work. Whether the game stops is a rule: **GameRules > Pause Game** (on), picked on
  the title screen's NEW GAME page (RULES: **Pause menu (Esc) pauses the game**). Off = the game goes on behind the menu.
- **KEY BINDINGS:** the same list as the title screen's Settings. Click a key box, press the new key (Esc cancels).
  It's saved at once and used straight away.
- **CONTROLS:**
  - **Mouse sensitivity:** 1 - 10, like many games. 5 is the game's normal speed, 10 twice as fast, 1 a fifth. Drag the
    slider (it moves in tenths) or click - / + (half a step); the camera follows at once.
  - **Invert mouse (up / down):** moving the mouse up looks down.
  - **RESET:** back to 5, not inverted.
- **< RESUME** (or **Esc** again) goes back to the game; **MAIN MENU** fades to the title screen.
- Everything is saved on the computer (PlayerPrefs), so the title screen's Settings and the pause menu show the same
  keys and mouse settings, and they're kept for next time.
- The pause menu doesn't open on top of another menu (inventory, crafting, build menu, a chest's panel, the death or
  victory page). Esc closes those first; press Esc again for the pause menu.

## Quick way: one click

1. Menu **Tools > UI > Build Pause Menu**, then **Build**. It saves your open scenes first.
2. That's it: it makes `Assets/Prefabs/UI/Pause/PauseCanvas.prefab`, puts it in GameScene and ProceduralGenerateScene
   (every scene in File > Build Settings with a player) and saves them.
3. For the title screen's Settings > CONTROLS page (the same mouse settings), rebuild the title screen once: **Tools >
   UI > Build Main Title Scene** (your Version text is kept).

Run it again any time: the prefab is made again, each scene keeps one copy.

## What it makes

```
PauseCanvas            <- Canvas (Sort Order 40), Canvas Scaler (1920 x 1080), Graphic Raycaster, PauseMenu
  Pause                <- switched off until Esc (CanvasGroup: it fades in)
    Backdrop           <- dark see-through, covers the screen (nothing behind it can be clicked)
    PageTitle          <- "PAUSED"
    ResumeButton       <- "< RESUME"
    MainMenuButton     <- "MAIN MENU"
    Tabs               <- KEY BINDINGS / CONTROLS (TabGroup + TabFX: the gold underline)
    TabPages
      KeyBindingsPage  <- KeyBindingMenu (the key list, made from its Templates)
      ControlsPage     <- ControlsMenu: List > Content > MouseHeader, SensitivityRow (SliderRow: Minus, Slider, Plus,
                          ValueBox > Value), SensitivityNote, InvertRow (Toggle with TogglePop); Hint; ResetControlsButton
    PauseHint          <- "Esc: back to the game"
```

The tabs and their pages are made by the title screen's own builder (`TitleSceneBuilder.MakeSettingsTabs`), so they
look and work the same in both places.

## How it works

| Part | Where | What it does |
|---|---|---|
| **PauseMenu** | PauseCanvas | Esc (KeyBindings' Unlock Cursor, "Pause menu" on the key list) opens it when no other menu is open (and none was in the last frame, so the Esc that closes the inventory doesn't also open it); Esc closes it, except while a key box waits for a key. While open: MenuState open + keys captured, mouse free, `Time.timeScale = 0` while **GameRules > Pause Game** is on (read each time it opens). Closed by RESUME, Esc or MenuState.CloseAll (death, victory). MAIN MENU: `ScreenFader.LoadScene` to **Main Menu Scene** (MainTitleScene) after time runs again. In the web build, losing the mouse while playing (the browser frees it on Esc) opens it too. |
| **MouseSettings** | (code only) | The saved mouse choices: `Sensitivity` (1 - 10, default 5), `Multiplier` (= Sensitivity / 5), `InvertY`, `Changed`. |
| **CameraController** | Main Camera | Turns at its own **Sensitivity** x `MouseSettings.Multiplier`, inverted when its Invert Y or the player's Invert mouse is on. With a pause menu in the scene it leaves Esc to it. |
| **ControlsMenu** | ControlsPage | Shows the saved choices, changes MouseSettings at once, writes them to the computer when the page closes, RESET. |
| **SliderRow** | SensitivityRow | A number on a slider with - / + buttons (Min 1, Max 10, Step 0.5, Decimals 1). |
| **KeyBindingMenu** | KeyBindingsPage | The key list (`Docs/Main Title Setup.md`). |

## Settings

| Where | Setting | What it does |
|---|---|---|
| GameRules | **Pause Game** | on = time stops while the menu is open (the NEW GAME page's "Pause menu (Esc) pauses the game"; off = the game goes on) |
| PauseCanvas > PauseMenu | **Main Menu Scene** | where MAIN MENU goes (`MainTitleScene`, in File > Build Settings) |
| Main Camera > CameraController | **Sensitivity** | the camera's speed at the player's sensitivity 5 (3) |
| ControlsPage > SensitivityRow > SliderRow | **Min**, **Max**, **Step**, **Decimals** | the slider's range (1 - 10), the - / + step (0.5), how the number is written ("5.5") |

## By hand (instead of the tool)

1. **GameObject > UI > Canvas**, name it `PauseCanvas`, Sort Order 40, Canvas Scaler: Scale With Screen Size, 1920 x
   1080. Add **PauseMenu**.
2. Inside it an empty full-screen object named `Pause` (add a CanvasGroup), with: an Image `Backdrop` (black, alpha
   0.78, covering the screen), a text "PAUSED", the Buttons `ResumeButton` and `MainMenuButton`, and the title screen's
   Settings tabs: copy `Tabs` and `TabPages` from MainTitleScene's TitleCanvas > Pages > SettingsPage (keep KEY
   BINDINGS and CONTROLS, delete DISPLAY and AUDIO and their pages, and drop those two from the TabGroup's list).
3. Switch `Pause` off, save PauseCanvas as a prefab in `Assets/Prefabs/UI/Pause`, and put it in GameScene (and the new
   world, or rebuild that: Tools > World > Build New World Scene).

## Testing

1. Play GameScene, press **Esc**: the game stops, PAUSED shows with KEY BINDINGS open.
2. Change a key (e.g. Jump to J), press Esc to close: J jumps now. The title screen's key list shows J too.
3. Esc, **CONTROLS**: drag Mouse sensitivity to 10, close: the camera turns twice as fast. Tick Invert mouse: up looks
   down. RESET: back to normal.
4. Open the inventory (Tab), press Esc: only the inventory closes. Esc again: the pause menu.
5. Esc, **MAIN MENU**: the title screen; its Settings > CONTROLS shows your sensitivity.
6. NEW GAME: untick **Pause menu (Esc) pauses the game**, START GAME, press Esc at night: the monsters keep coming while
   the menu is open.

## Problems

| Problem | Why / fix |
|---|---|
| Esc only frees the mouse | The scene has no PauseCanvas: run **Tools > UI > Build Pause Menu** (the new world copies GameScene, so it's in there too after Build New World Scene). |
| The buttons can't be clicked | The scene needs an **EventSystem** (the tool adds one). |
| The title screen's CONTROLS still says "INCOMING" | Rebuild it: **Tools > UI > Build Main Title Scene**. |
| The camera is too slow even at 10 | Raise Main Camera > CameraController > **Sensitivity** (10 = twice it). |
| In the web build, Esc closes the menu but the mouse stays free | Browsers only lock the mouse after a click: click in the game (or use RESUME). |
| A fireball was cast while paused | Fixed: Marko's PlayerFireball now ignores Q while any menu is open (log `Docs/TonyEditMate/Marko/21-11-2026 No Fireball While A Menu Is Open.md`). |
