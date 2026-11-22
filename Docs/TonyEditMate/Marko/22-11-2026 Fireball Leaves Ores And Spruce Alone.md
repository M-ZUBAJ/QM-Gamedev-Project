# Tony's change to Marko's FireballProjectile, 22/11/2026: ores and spruce need their tool, even against a fireball

**Why:** Tony asked for the new world's snow trees (spruce) to need an **Iron Axe or better** to chop. Your
`PlayerCombat` already checks this before a hit lands (its `[Tony]` lines from 12/11/2026 ask `ToolRules.CanMine`,
which now covers trees too). But your fireball hurt every `ResourceNode` it touched, so it could burn a spruce down
without an axe, and it already broke copper, iron, gold and diamond rocks without the pickaxe they need (a sword can't
break them, but its fireball could).

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Player/FireballProjectile.cs` | **Added** 6 lines at the top of the `else if (resource != null)` branch in `OnCollisionEnter`, each marked `[Tony] added`: if `ToolRules.NeededFor(resource)` isn't `ToolTier.None` (the rock or tree needs a tool: an ore, the snow's spruce), it calls `ToolRules.TellCantMine(resource)` ("Needs an Iron Axe or better"), destroys the fireball and returns, so that thing isn't hurt. Your own lines (including Tony's `[Tony] replaced` PerkEffects.Hurt line from 21/11/2026) are unchanged below it. |

Everything else is hit exactly as before: enemies, animals (deer), plain stone rocks and grassland trees (they need no
tool).

## How to undo

In `FireballProjectile.cs`, delete the 6 lines marked `[Tony] added` (and the 4 comment lines above them) in
`OnCollisionEnter`. The fireball then hurts every rock and tree again, whatever tool it needs.
