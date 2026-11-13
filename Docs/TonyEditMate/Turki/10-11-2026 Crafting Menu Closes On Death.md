# Tony's edits to Turki's code, 10/11/2026: the crafting menu closes when the player dies

Every changed line is marked `// [Tony]` in the file. Nothing of Turki's was removed or replaced: only lines were
added.

**Why:** Tony asked that when the player dies, every open menu closes, so only the death page is left on screen.
Tony's PlayerRespawn and DeathScreen now call `MenuState.CloseAll()` when the player dies. Each menu that listens to
`MenuState.CloseAllRequested` closes itself: the inventory, build menu, building placement, Marko's perk choice, and
this crafting menu.

## `Assets/Script/Crafting/CraftingMenu.cs`

**What changed** (only added lines):
- First line of `OnEnable()` (the `OnEnable` Tony added on 20/10/2026): `MenuState.CloseAllRequested += CloseNow;`
- `OnDisable()`: `MenuState.CloseAllRequested -= CloseNow;`
- `CloseNow()`: `if (isOpen) Close();`, your own `Close()`, the same as pressing C again.

The crafting list inside the inventory's Crafting tab isn't opened with C (`isOpen` stays false there), so
`CloseNow` does nothing for it. The inventory closing hides it.

**To undo:** delete the lines marked `[Tony] added (10/11/2026)` and the `OnDisable` / `CloseNow` block below
`OnDestroy`.

## For Turki: a new menu of yours

Make it close when the player dies, like the others:

```csharp
void OnEnable()  { MenuState.CloseAllRequested += CloseNow; }
void OnDisable() { MenuState.CloseAllRequested -= CloseNow; }
void CloseNow()  { if (isOpen) Close(); }   // your own close method
```
