# Tony's change to Marko's EnemyCombat, 23/11/2026: a human's armor takes damage off

**Why:** Tony's **human enemies** in the new world (`Docs/Human Enemies Plan.md`) wear Polytope Studio armor by tier
(copper, iron, gold, diamond). Tony wants that armor to protect them, like the player's armor protects the player: a
human in a full diamond set should take 40 % less from every hit. Every attack the player has (punches, tools,
swords, arrows, the fireball, Thorns) hurts a human through your `EnemyCombat.TakeDamage`, so the armor has to be
taken off there, before the health goes down. Otherwise a hit that only looked smaller could still kill.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Enemy/EnemyCombat.cs` | **Added** two lines in `TakeDamage()`, after your dodge roll and before `currentHealth -= damage;`, marked `[Tony] added (23/11/2026)`: `HumanGear armor = GetComponent<HumanGear>();` and `if (armor != null) damage = armor.Reduce(damage);`. |

How it works:

- Tony's `HumanGear` (only on human enemies) adds up the armor pieces it wears with the player's armor numbers:
  the chestplate counts 28 % of a set, the helmet, leggings, boots and gauntlets 18 % each. A full set takes off
  10 % (copper), 15 % (iron), 25 % (gold) or 40 % (diamond); mixed pieces in between. `Reduce(damage)` returns the hit
  with that share taken off.
- Your dodge (Miss Chance) still comes first: a dodged hit takes nothing, as before.
- **Monsters have no HumanGear**, so `armor` is null for them and their hits are exactly as before. Nothing changes
  for the slime, the turtle, the Chest Monster or the Beholder.
- Your `Debug.Log("Enemy took ...")` now prints the damage after the armor, and the player's damage numbers show it
  too (they measure the health that was really lost).

None of your own lines changed.

## How to undo

In `EnemyCombat.cs`, `TakeDamage()`: delete the comment block and the two lines marked `[Tony] added (23/11/2026)`.
Human enemies then take full damage whatever they wear.
