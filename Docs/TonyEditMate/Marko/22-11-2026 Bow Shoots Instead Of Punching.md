# Tony's change to Marko's PlayerCombat, 22/11/2026: a bow shoots instead of punching

**Why:** Tony added a bow (the Wooden Bow) and arrows in five tiers (guide `Docs/Bow Setup.md`). With a bow in the
selected hotbar slot, holding the left mouse button draws it and letting go shoots an arrow (Tony's `BowShooter`). The
arrow is a projectile: it flies, falls a little, and hurts the monster (or animal) where it lands, through your
`EnemyCombat.TakeDamage` (your dodge chance still counts, and the coins on a kill too). Without this change the same left
click would also punch: a bow is a Tool, but not a Sword / Axe / PickAxe, so your Attack() used hand damage, played a
punch and spent 10 stamina on every shot.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Player/PlayerCombat.cs` | In `Update()`, right after Tony's "a left click on a menu" checks: one `[Tony] added` line, `if (ToolRules.BowInHand) return;` (with a comment above it). Nothing else changed: with any other item, or nothing, in the hand, clicks attack exactly as before. |

`ToolRules.BowInHand` is Tony's: true while the selected hotbar slot holds an item whose Item Data is a Tool of Kind
Bow (`Assets/Config/Items/Tools/WoodenBow`).

Your other scripts are not changed. `EnemyCombat.TakeDamage(damage)` is called by Tony's `ArrowProjectile` when an arrow
hits (no weapon id, so `WeaponUpgradeSystem` doesn't count arrow kills for a sword).

## How to undo

Delete the line marked `[Tony] added` in `Update()` (and its comment). The bow then shoots and punches at the same time.
