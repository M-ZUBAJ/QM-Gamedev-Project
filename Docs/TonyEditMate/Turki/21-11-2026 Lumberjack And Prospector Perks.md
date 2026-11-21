# Tony's change to Turki's ResourceNode, 21/11/2026: Lumberjack and Prospector perks

**Why:** Tony added loot boxes with new perks (guide: `Docs/Loot Chests Setup.md`). Two of the rare box's perks give
more from gathering: **Lumberjack** (+1 wood from every tree chopped down) and **Prospector** (+1 stone or ore from
every rock broken). Each pick adds one more.

**File changed:** `Assets/Script/Gathering/ResourceNode.cs` (yours): 2 lines in `Break()`, marked `// [Tony] added`,
right after your normal drop. None of your lines were changed or removed.

```csharp
int extra = PerkEffects.ExtraDrop(resourceType, collectWithKey);          // [Tony] added
if (extra > 0) Give(inv, dropItem, extra, extra);                         // [Tony] added
```

- `PerkEffects.ExtraDrop` (Tony's) gives the Lumberjack total for a Tree, the Prospector total for Stone, and 0 for an
  animal, for berries picked with the key (`collectWithKey`), and when the player has neither perk. So without the perks
  nothing changes.
- The extra goes through your own `Give` (same item, same "inventory is full" message), so the gain message adds up
  ("+5 Wood" instead of "+4 Wood").
- Your bonus item (apples) isn't changed.

**To undo:** delete the 2 lines marked `[Tony] added` (and their comment lines) in `Break()`.
