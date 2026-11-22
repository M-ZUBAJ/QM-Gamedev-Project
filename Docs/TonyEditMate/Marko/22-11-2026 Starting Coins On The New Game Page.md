# Tony's change to Marko's PlayerCoins, 22/11/2026: Starting Coins (set on the NEW GAME page)

**Why:** Tony asked for a field on the title screen's NEW GAME page that sets how many coins the player starts with,
20 by default. Those 20 coins came from your test line in `PlayerCoins.Awake()` (`AddCoins(20);`, under "temporary
test code"), so the amount is now a setting on your script that the NEW GAME page can change.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Player/PlayerCoins.cs` | **Added** a public field `startingCoins = 20` ("Starting Coins" in the Inspector, under Reset On Death), marked `[Tony] added`. **Replaced** your `AddCoins(20);` in `Awake()` with `AddCoins(startingCoins);` (your line is kept in a comment above it, `[Tony] replaced`). **Added** `public void SetStartCoins(int amount)` after Tony's `ResetCoins()`: sets the starting amount, what dying goes back to (Tony's `startCoins`) and the coins now, then fires your `CoinsChanged`. Tony's older comment about the "20 right now" was updated to say Starting Coins. |

So with nothing changed on the NEW GAME page (or when you press Play straight in GameScene) the player still starts
with 20 coins, exactly as before. If you change Starting Coins on the Player in GameScene, the NEW GAME page's default
follows it the next time GameScene is saved (Tony's NewGameDefaultsSync reads it).

How the NEW GAME page uses it: Tony's `GameSetup` calls `SetStartCoins(...)` right after the game scene loads, when
every Awake has run (your `AddCoins(startingCoins)` included) but before anything starts. So the HUD's coin counter
just shows the new number (no "+80 Coins" message), and Tony's PlayerStats doesn't count the start amount as earned.
Dying still puts the coins back to the starting amount (Reset On Death), which is now the chosen one.

## How to undo

In `PlayerCoins.cs`: delete the `[Tony]` lines for `startingCoins` and `SetStartCoins`, and put your
`AddCoins(20);` back in place of `AddCoins(startingCoins);`. Then also remove the `StartCoins` lines from Tony's
`GameSetup.cs` (it calls `SetStartCoins`) and `NewGameDefaultsSync.cs` (it reads `startingCoins`), or they won't
compile.
