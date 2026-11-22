# Tony's change to Marko's PlayerFireball, 21/11/2026: no fireball while a menu is open

**Why:** Tony added a pause menu (Esc while playing: key bindings and mouse settings; guide `Docs/Pause Menu Setup.md`).
It stops time while it's open (`Time.timeScale = 0`). Your fireball read Q straight from the keyboard even while a menu
was open, so pressing Q in the pause menu cast a fireball that hung in the air until the game went on. The same was true
for the inventory, a chest's panel and the death page (your PlayerCombat already had this check for the left click).

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Player/PlayerFireball.cs` | In `Update()`, right after `if (!HasFireballPerk) return;`: one `[Tony] added` line, `if (MenuState.AnyOpen) return;`. Nothing else changed: Q, the sword check, the cooldown and the cast work as before whenever no menu is open. |

## How to undo

Delete the lines marked `[Tony] added` (the comment and the check) in `Update()`.
