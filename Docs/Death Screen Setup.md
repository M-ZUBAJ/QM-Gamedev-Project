# Death Screen Setup

When the player's health reaches 0, a page shows what they did in that life, and a **Respawn** button brings
them back at the respawn point (0, 0.5, 0) with full health, food and stamina.

**Items:** decided by the GameRule **Keep Inventory** (on the GameRules object):
- **Off** (default, like Minecraft): every stack (inventory, hotbar, cursor) is thrown out around the body
  (ItemDropper on the Player). Walk back and pick them up. They disappear after ItemDropper's Despawn Minutes (5).
- **On**: the player keeps everything.

## 1. The Player

Select the **Player** and Add Component (if it isn't there yet):

| Script | What it does |
|---|---|
| **PlayerRespawn** | Brings the player back. **Respawn Point** = where (default 0, 0.5, 0). |
| **PlayerStats** | Counts this life: distance walked / run, jumps, stamina used, health lost / healed, food lost / eaten, items eaten, buildings built. |

## 2. The page (DeadCanvas)

The hierarchy (the names matter, the script finds them by name):

```
DeadCanvas            <- Canvas + DeathScreen script
  BackgroundPanel     <- an Image covering the screen
    TitleText         <- "You died" (anything you like)
    StatusText        <- the stats are written here (TextMeshPro or legacy Text)
    RespawnButton     <- a Button
      RespanText      <- the button's label, e.g. "Respawn"
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

Nothing to drag in: no On Click to set up on the button.

## Test

1. Press **Play**, walk, sprint, jump, eat something.
2. Click the Player > PlayerHealth's **⋮** menu > **Test: Die** (or type 0 into its Health).
3. The page appears with the numbers and the mouse is free. Click **Respawn**: you're back at 0, 0.5, 0,
   the bars are full and the numbers start from 0 again.

### If something doesn't work

| Problem | Fix |
|---|---|
| Respawned at once, no page | DeadCanvas needs **DeathScreen**, and DeadCanvas must be switched on in the Hierarchy. |
| Page shows, no numbers | The Player needs **PlayerStats**; the text object must be named exactly **StatusText**. |
| Button can't be clicked | Add an **EventSystem**; the button must be named exactly **RespawnButton**; BackgroundPanel must not block it (the button is its child, so it's fine). |
| Page behind the health bars | Raise DeadCanvas's **Sort Order**. |
| Without a death page | Just leave DeathScreen out: PlayerRespawn then respawns straight away. |
