# Tony's change to Turki's ResourceNode, 23/11/2026: human enemies gather like the player

**Why:** Tony is adding **human enemies** to the new world (`Docs/Human Enemies Plan.md`): survivors in Polytope
Studio's armor who live by the same rules as the player. Tony asked that they really chop trees, mine rocks and pick
berries to get items, like the player does. Your `TakeDamage` and `Break` give the drop to the **player's** inventory
(`PlayerInventory.Instance`), so a human breaking a tree with them would have handed the wood to the player. They
needed a way to break a node and keep what it gives.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Gathering/ResourceNode.cs` | **Added** at the end of the class, every line marked `[Tony] added`: `public bool CollectWithKey` (read only: is it a berry bush?), `public void TakeDamageFor(float damage, System.Action<ItemData, int> taker)` and `void BreakFor(taker)`. |

How they work:

- `TakeDamageFor` takes health off like your `TakeDamage` (a berry bush isn't hit: it's picked at once). When the
  health reaches 0 it calls `BreakFor`.
- `BreakFor` uses your own drop settings (Drop Item, Drop Min / Max, the Bonus item and its chance and amounts) but
  hands the items to `taker` (the human's bag, Tony's `HumanLoot.Carry`) instead of the player's inventory, then
  removes the node like your `Break` does. No perk extras (Lumberjack / Prospector are the player's).
- A tree a human chops still falls (Tony's TreeFall), a rock still bursts (RockBreak), and the new world remembers it's
  gone, because the node's health is 0 when it's removed.

Your `TakeDamage`, `Break`, `Give` and the berry key code are **unchanged**: the player gathers exactly as before. When
a human enemy dies, Tony's `HumanLoot` gives everything it gathered to the player (straight into the inventory).

The humans follow the player's tool rules too (Tony's `ToolRules.NeededFor`, which reads each drop's Mined With): some
humans carry an axe and a pickaxe (the game's tier tools), and only a pickaxe of the ore's tier mines ore, the snow's
spruce needs an iron axe (Polytope's war axe counts as an axe of its tier). One without tools only takes plain trees,
stone and berries.

## How to undo

In `ResourceNode.cs`: delete the lines marked `[Tony] added (23/11/2026)` (the comment block, `CollectWithKey`,
`TakeDamageFor` and `BreakFor`) at the end of the class. Tony's `HumanBrain` then won't compile until its
`TakeDamageFor` / `CollectWithKey` lines are removed too.
