# Tony's edit to Marko's code, 13/11/2026: bare-handed punch combo

Every changed line is marked `// [Tony]` in the file, and the one line of yours that was replaced is kept in a comment
above its new version.

**Why:** Tony added Mixamo punch animations and asked for a combo with an empty hand:
- **Left click:** left jab, then right jab, left, right...
- **Right click:** heavy left hook, then heavy right punch, left hook... They cost more stamina.

The choosing is done by Tony's new `Assets/Script/Player/PunchCombo.cs` (on the Player). Your PlayerCombat only asks it.
Guide: `Docs/Mixamo Animations.md`.

---

## `Assets/Script/Player/PlayerCombat.cs`

| Where | Change |
|---|---|
| Fields | **Added** `private PunchCombo punchCombo;` |
| `Awake()` | **Added** `punchCombo = GetComponent<PunchCombo>();` (null = no combo, nothing changes) |
| `Update()`, before your left-click check | **Added:** while no tool is in the hand, `punchCombo.Next(Time.time >= nextAttackTime)` says which punch a left or right click throws this frame (or none), then `Attack(punch)` and `return`. With a tool in the hand your code runs as before. |
| `void Attack()` | **Replaced** by `void Attack(PlayerAction? punch = null)`: the punch PunchCombo chose. Your `Attack()` calls still work. |
| Tony's lines at the top of `Attack()` | `swing = punch ?? ToolRules.SwingFor(tool)`, and a heavy punch spends extra stamina: `attackStaminaCost x (Heavy Stamina - 1)` more, so x2 in total by default. |
| Enemy branch and resource branch | **Added** `damage *= PunchCombo.Power(swing);`: a heavy punch hits Heavy Damage times harder (x2 by default). Jabs do your Hand Damage as before. |

**Good to know:**
- **What's unchanged:** punches are thrown on the click as before, with the same cooldown, stamina check, ray and
  damage numbers. Your guards still come first: menus, building placement and clicks on UI stop the combo too.
- **The right mouse button now attacks with an empty hand** (heavy punches). While food is in the hand it still eats
  (Tony's ItemConsumer), and the combo leaves it alone.

**To undo:**
1. Delete the lines marked `[Tony] added (13/11/2026)` and the `punchCombo` block in `Update()`.
2. Put back `void Attack()` from its `[Tony] replaced` comment.
3. Change `punch ?? ToolRules.SwingFor(tool)` back to `ToolRules.SwingFor(tool)`.
4. Delete the two `PunchCombo.Power` lines and the heavy stamina lines.
