# Tony's edits to Marko's code, 10/11/2026: coins reset on death, perk menu closes, punch

Every changed line is marked `// [Tony]` in the file. Nothing of Marko's was removed or replaced: only lines were added.

**Why:** Tony asked for three things:
1. When the player dies, the coins are reset.
2. When the player dies, every open menu closes, so only the death page is left on screen.
3. Attacking with bare hands plays the player character's punch animation.

---

## 1. `Assets/Script/Player/PlayerCoins.cs`: coins reset when the player dies

**What changed** (only added lines):
- `public bool resetOnDeath = true;` (a tick box in the Inspector), `private int startCoins;`, `private PlayerHealth health;`
- At the end of `Awake()`: `startCoins = Coins;`, the coins the player starts with. That's 20 right now because of
  the test line `AddCoins(20)`, and 0 once that line is removed.
- `Start()`: listens to the player dying (`PlayerHealth.Died`, on the same Player). `OnDestroy()` stops listening.
- `OnPlayerDied()`: if Reset On Death is ticked, it calls `ResetCoins()`.
- `public void ResetCoins()`: puts `Coins` back to `startCoins` and fires `CoinsChanged`, so the HUD's coin counter
  updates.

**Good to know:**
- Untick **Reset On Death** on the Player's PlayerCoins to keep coins after dying.
- Tony's "+N Coins" messages stay quiet while the player is dead. Resetting to 20 after spending some would
  otherwise look like a gain.
- Perks are also lost on death (Tony's PlayerPerks > Reset On Death), so the player starts over cleanly.

**To undo:** delete the lines marked `[Tony]`.

## 2. `Assets/Script/UI/Perks/PerkSelectionUI.cs`: the perk choice closes when the player dies

**What changed** (only added lines):
- `OnEnable()` / `OnDisable()`: start and stop listening to Tony's `MenuState.CloseAllRequested`.
- `CloseNow()`: `if (IsOpen) Hide();`, the same `Hide()` your buttons use. No perk is chosen.

**Who asks:** Tony's PlayerRespawn and DeathScreen call `MenuState.CloseAll()` when the player dies. The inventory,
build menu, building placement and Turki's crafting menu close the same way.

**Good to know:** the chest already took the coins and was marked opened when this menu opened (your chest code), so
dying while choosing loses that choice. The coins are reset on death anyway.

**To undo:** delete the lines marked `[Tony]`.

## 3. `Assets/Script/Player/PlayerCombat.cs`: punch animation for bare-handed attacks

**What changed** (only added lines): in `Attack()`, right after the stamina is spent (so only when the attack really
happens), it reads the item in hand. If it's not the `"Sword"`, `"Axe"` or `"PickAxe"` (the same items your damage code
checks, so this is exactly when hand damage is used), it calls `PlayerAnimator.Play(PlayerAction.Punch);`.

- **It plays even when the swing hits nothing**, because the check comes before the ray.
- **Without Tony's character** on the player, `PlayerAnimator.Play` does nothing.
- **The damage code is unchanged.**
- **Sword, axe and pickaxe swings have no animation yet.** `PlayerAction.Attack` (a one-handed swing) and
  `PlayerAction.Mine` are ready for them.

**To undo:** delete the lines marked `[Tony]`.

---

## For Marko: a new menu of yours

Make it close when the player dies, like the others:

```csharp
void OnEnable()  { MenuState.CloseAllRequested += CloseNow; }
void OnDisable() { MenuState.CloseAllRequested -= CloseNow; }
void CloseNow()  { if (isOpen) Close(); }   // your own close method
```
