# Tony's edit to Marko's perk chest, 19/11/2026: the panel opens and closes with E, pay when a perk is picked

**Files changed:**
- `Assets/Script/Perks/PerkChest.cs` (Marko's): `[Tony]` lines; the replaced code is kept in comments.
- `Assets/Script/Perks/PerkManager.cs` (Marko's): one `[Tony]` line and a small helper.
- `Assets/Scenes/GameScene.unity`: Marko's hand-made **PerkSelectCanvas** is replaced by Tony's built one
  (**Tools > UI > Build Perk Panel**, `Assets/Script/Editor/PerkPanelBuilder.cs`), which uses Marko's
  **PerkSelectionUI** unchanged.
- `Assets/Config/Perks/*.asset` (Marko's Perk Data): the same tool fills each empty **Icon**.
- `Assets/Script/UI/Perks/PerkSelectionUI.cs` (Marko's): **not** changed.

**Why (Tony asked for it):**
- A proper perk panel (it showed white squares and text over the buttons), a coin display, and the chest's price.
- E opens the panel while looking at the chest and closes it wherever you look; walking away closes it too.
- Closing doesn't use up the chest: come back, press E, choose.
- Pay when a perk is picked. Before, the coins went when the panel opened, so closing it would have wasted them.
- Each chest keeps its own 3 random perks (no re-rolling by closing and opening it).
- Winning the game closes the panel (Tony's VictoryScreen now calls `MenuState.CloseAll()`; dying already did).

---

## PerkChest.cs

| Where | Change |
|---|---|
| New fields (after `IsOpened`) | Read only: `OpeningCost`, `InteractionRange`, `InteractKey` (for Tony's ChestPrompt and PerkPanelInfo). `static PerkChest Showing` = the chest whose perks are on screen. `choices` = this chest's 3 perks. `closedFrame`, `CloseMargin` (0.5 m). |
| `Update()`, the start | `[Tony] added`: while `Showing == this`, E or `TooFar()` calls `ClosePanel()`; if the panel closed another way, `Showing` is cleared. Then nothing opens while another chest's panel is open, in the frame E closed one, or while another menu is open (`MenuState.AnyOpen`). |
| `TryOpen()`, the coin check | `[Tony] replaced`: removed (looking is free). The original is kept in a comment. |
| `TryOpen()`, rolling the perks | `[Tony] edited`: only while `choices` is still empty. `choices` is now a field (it was a new local list each time), so the same 3 come back. Your rolling code is unchanged, inside the new `if` (one step further in). |
| `TryOpen()`, after Show | `[Tony] added` `Showing = this;`. `[Tony] replaced`: the payment and `opened = true` moved to OnPerkSelected (the original is kept in a comment). The log line also says the price. |
| `OnPerkSelected()` | `[Tony] added`: `Showing = null`; after your PerkManager check, the payment (`SpendCoins`; a price of 0 is free). Not enough = no perk, and the chest stays. Then `opened = true`. |
| New methods | `[Tony] added`: `PanelOpen()`; `ClosePanel()` (hides the panel, and nothing opens again in that frame); `TooFar()` (camera to the nearest point of the chest's collider farther than Interaction Range + 0.5 m); `OnDestroy()` (a chest that disappears closes its panel). |

Unchanged: the price roll in Start, the key, the range, the `InteractOutline.LooksAt` ray (earlier log
`19-11-2026 Chest On The Floor And Yellow Outline.md`), the perk list, despawning, GrantRandomPerk.

## PerkManager.cs

| Where | Change |
|---|---|
| End of `ApplyPerk()` | `[Tony] added`: `PlayerPerks.Record(HudPerk(perk.perkType));`, so the HUD's perk row (Tony's PlayerPerks and PerkBar) shows perks picked on the panel. Only the old GrantRandomPerk did that before. |
| New method | `[Tony] added`: `HudPerk(ChestPerkType)` turns your perk into the same one in Tony's `PerkType` (the same four names). |

## GameScene and the Perk Data

| What | Change |
|---|---|
| PerkSelectCanvas | Marko's hand-made canvas is removed. The built prefab `Assets/Prefabs/UI/Perks/PerkSelectCanvas.prefab` (same name) takes its place: Marko's PerkSelectionUI (its fields filled by the tool) plus Tony's PerkPanelInfo. |
| Perk_Health, Perk_Stamina, Perk_MoveSpeed, Perk_Fireball | **Icon** was empty and is now the drawn heart, lightning, double arrows and flame from `Assets/UI/Perks`. Names, descriptions and values are unchanged. |

**To undo:**
- **PerkChest.cs / PerkManager.cs:** delete the lines marked `[Tony]` from this change and put the commented
  "Original" code back (in TryOpen: the coin check, the local `choices` list without the `if`, the payment and
  `opened = true`).
- **GameScene:** Ctrl + Z right after the tool, or delete PerkSelectCanvas and bring Marko's back from git.
- **Perk Data:** clear their **Icon**.

Guide: `Docs/Perk Panel Setup.md`.
