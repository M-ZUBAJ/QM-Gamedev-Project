# Display Settings Setup (damage numbers and fullscreen)

**What the player sees** (21/11/2026):
- **Damage numbers:** when a hit lands, the damage it really did pops up at the hit spot, rises and fades, like many
  games. It's drawn over everything with a dark outline and is about the same size on screen near or far.
  - A normal hit: a **white** number ("12"). Small hits keep their tenth ("2.5").
  - A **critical hit** (the epic box's **Critical Strike** perk, and Precision): **bigger and orange-red**, with
    **CRITICAL** over the number and a **"!"** after it ("24!"). It pops harder and stays longer, so you can tell at a
    glance.
  - Every hit counts: punches, tools and swords on trees, rocks, ores, animals and monsters, the **fireball**, and
    **Thorns** (the damage a monster takes back when it hits you). A hit that does nothing (a turtle in its shell:
    "BLOCKED!", a dodge) shows no number.
- **Settings > DISPLAY** (the title screen) and the **DISPLAY** tab of the **Esc pause menu**:
  - **SCREEN > Fullscreen:** the game fills the monitor. **Alt + Enter** or **F11** switch it anywhere in the game
    too (F11 can be changed on KEY BINDINGS: GAME > Fullscreen). In the web build the browser leaves fullscreen on
    **Esc**; the tick follows what the screen really does.
  - **DAMAGE NUMBERS**, one framed box, so it's clear the two settings belong together:
    - **Show damage numbers** (on): the parent setting.
    - **Only monsters and animals** (off): its child, indented under it and joined to it by a line. On = hits on trees,
      rocks and ores show no number. While **Show damage numbers** is off, the child is **locked**: faded, it can't be
      ticked or unticked, and it says "Turn on Show damage numbers first."
    - **LOOKS LIKE:** a white 12 and an orange CRITICAL 24! show what the numbers look like.
  - **RESET:** damage numbers back on, for every hit.
- It's the player's own choice, saved on the computer (PlayerPrefs, like the keys and the mouse), not a game rule:
  the title screen and the pause menu always show the same.

## Quick way

1. Menu **Tools > UI > Build Main Title Scene** (the title screen's Settings gets the DISPLAY page; your Version text
   is kept).
2. Menu **Tools > UI > Build Pause Menu** (the pause menu gets the DISPLAY tab, in GameScene and
   ProceduralGenerateScene).

The damage numbers and the Alt + Enter / F11 keys need nothing else: they work in every scene by themselves.

## What it makes

```
TabPages
  DisplayPage            <- DisplayMenu
    List > Content       <- the rows (VerticalLayoutGroup + CascadeIn)
      ScreenHeader       <- "SCREEN"
      FullscreenRow      <- Label, Toggle (TogglePop), Note "Alt + Enter or F11 too..."
      Damage numbersHeader <- "DAMAGE NUMBERS"
      DamagePanel        <- the framed box (rounded, a little lighter) with GroupBar (the gold bar on its left)
        DamageRow        <- Toggle (the parent), Label "Show damage numbers", Info
        DamageCreaturesRow <- CanvasGroup (DisplayMenu fades it); BranchDown + BranchAcross (the line from the parent's
                              tick box), Toggle (the child, indented), Label "Only monsters and animals", Note, LockNote
        DamagePreview    <- a dark box: Caption "LOOKS LIKE", Normal "12", Critical "CRITICAL 24!"
    Hint                 <- "Changes work at once..."
    ResetDisplayButton   <- RESET
```

The page is made by `TitleSceneBuilder.MakeDisplayPage`, used for both the title screen and the pause menu
(`MakeSettingsTabs`), so it looks and works the same in both.

## How it works

| Part | Where | What it does |
|---|---|---|
| **DisplaySettings** | (code only) | The saved choices: `ShowDamage` (on), `DamageOnlyCreatures` (off), `ShowsDamageOn(creature)`, `Fullscreen` (= Screen.fullScreen, not saved: the browser decides), `SetShowDamage`, `SetDamageOnlyCreatures`, `SetFullscreen`, `ToggleFullscreen`, `ResetToDefaults`, `Save`, the event `Changed`. |
| **DisplayMenu** | DisplayPage | Finds the rows by name, shows the saved choices, locks the child while the parent is off (Toggle not interactable, the row's CanvasGroup faded, LockNote instead of Note), dims the sample numbers, saves when the page closes, RESET. The Fullscreen tick asks for fullscreen as soon as the mouse button goes **down**: browsers only allow fullscreen while handling the player's click, and Unity passes the request on with the next mouse or key event, so a tick that changes when the button comes up needed a second click. |
| **DamageNumbers** | (code only) | `Show(point, amount, critical, creature)` pops a number (if DisplaySettings allows it). `HealthOf(target, out creature)` + `ShowDealt(target, before, creature, point, critical)` show the damage a hit really did (health before minus after; a monster's EnemyCombat or a tree's / rock's / animal's ResourceNode). `TopOf(target)` = a spot near the top of a target (for the fireball). |
| **FloatingText** | (code only) | Draws the number: a 3D TextMeshPro text over everything, facing the camera, rising and fading (its pop and rise are now settable: the critical pops harder). |
| **PunchEffect** | Player | When a punch / tool / sword hit lands, measures the target's health before the hurt and shows the difference, critical when PerkEffects rolled one. |
| **PerkEffects** | Player | The fireball's hits (`Hurt`) and Thorns (`Reflect`) show their numbers the same way (never critical). |
| **FullscreenKeys** | (makes itself) | A hidden object kept through every scene: Alt + Enter or **KeyBindings' Fullscreen** (F11) switches fullscreen, but not while the key list waits for a new key. A Windows / Mac build does Alt + Enter by itself (Player Settings > Resolution and Presentation > Allow Fullscreen Switch), so there it only adds F11. |

## Settings

| Where | Setting | What it does |
|---|---|---|
| DisplayPage > DisplayMenu | **Locked Alpha** | how see-through the locked child row and the sample numbers are while Show damage numbers is off (0.35) |
| KeyBindings | **Fullscreen** | the fullscreen key (F11); the player can change it on KEY BINDINGS |
| `DamageNumbers.cs` | the colors and sizes at the top | white normal / orange-red critical (1, 0.38, 0.12), sizes 3.2 / 5.6 at 6 m, growing up to 3 times farther away |

## By hand (instead of the tools)

1. In MainTitleScene's TitleCanvas > Pages > SettingsPage > TabPages, make an empty `DisplayPage` filling it. Inside: a
   panel with the rows as in **What it makes** (the names in quotes matter: DisplayMenu finds them by name; each row's
   tick box is a **Toggle**). Put **DisplayMenu** on DisplayPage. Give `DamageCreaturesRow` a **CanvasGroup**.
2. Add a DISPLAY tab button to `Tabs` and a new element to its **TabGroup** list: the button and DisplayPage.
3. Do the same in the pause menu's prefab (`Assets/Prefabs/UI/Pause/PauseCanvas.prefab`), or copy the page over.

## Testing

1. Play GameScene, punch a tree: a white number pops up where the fist lands ("3"). Hit a slime with a sword: its
   damage. Cast a fireball at a slime: its number too.
2. Get **Critical Strike** (an epic box; NEW GAME > Testing helps) and punch until a big orange **CRITICAL 7!** shows.
3. Esc > **DISPLAY**: tick **Only monsters and animals**, close: trees show no number, slimes do.
4. Untick **Show damage numbers**: the child fades, can't be clicked, and says "Turn on Show damage numbers first".
   No numbers at all now. RESET: back on, for every hit.
5. In a build: press **F11** or **Alt + Enter**: fullscreen; again (or Esc in the browser): back. The DISPLAY tick
   follows. (In the editor's Game view nothing happens: Unity can't make it fullscreen.)
6. MAIN MENU > SETTINGS > DISPLAY: the same choices as in the pause menu.

## Problems

| Problem | Why / fix |
|---|---|
| No DISPLAY tab / it says "INCOMING" | Rebuild: **Tools > UI > Build Main Title Scene** and **Tools > UI > Build Pause Menu**. |
| No numbers at all | Show damage numbers is off (DISPLAY), or Only monsters and animals is on and you're hitting trees / rocks. |
| A hit shows no number | It did no damage (the turtle's shell, a dodge), or the target has no EnemyCombat / ResourceNode health. |
| F11 acts strangely in the browser | F11 is also many browsers' own fullscreen key, so the browser may switch by itself as well. Alt + Enter or the DISPLAY tick only use the game's fullscreen. |
| The Fullscreen tick does nothing in the editor | Unity's Game view can't go fullscreen; try it in a build. |
