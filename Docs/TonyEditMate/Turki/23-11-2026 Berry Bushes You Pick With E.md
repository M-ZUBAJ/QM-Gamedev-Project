# Tony's change to Turki's berry bush, 23/11/2026: a real bush whose berries you pick with E

**Why:** Tony wanted the berry bush to look like a real one and to work like this: "berry bush can be destroyed but
when press E player should pick a berry". He chose: E picks all the berries at once, they grow back (one every 60 s),
and breaking the bush gives nothing extra. Before, E removed the whole bush (giving 3 - 5 berries) and hits did nothing.

## Files changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Gathering/ResourceNode.cs` | 5 lines marked `[Tony]` (the 2 replaced lines are kept in comments above them): **added** a field `BerryBush bush;` (Tony's new script, found in `Start()` with `GetComponent<BerryBush>()`, null on every other node) and a read-only `public float CollectRange => collectRange;` (Tony's HUD prompt uses your Collect Range); **replaced** in `Update()`: `if (lookingAtThis) Break();` -> a bush with BerryBush calls `bush.Pick()` instead (all berries into the inventory, the bush stays), else `Break()` as before; **replaced** in `TakeDamage`: `if (collectWithKey) return;` -> `if (collectWithKey && bush == null) return;` (a bush with BerryBush can be hit and broken); **added** at the top of `Break()`: a bush with BerryBush gives only the berries still on it (`bush.GiveLeft()`), then is removed. |
| `Assets/Prefabs/ResourceNodes/BerryNode.prefab` | Rebuilt in place by Tony's **Tools > World > Build Berry Bushes** (same file and top object, so scenes and Sanyam's ObjectScatter rules keep their link): the green sphere, the 3 red spheres and their ShapeColor are gone; it has a Yughues bush (Model > LOD0 - LOD3), red berry bunches (Berries > Berry 1 - 5), a LOD Group, a Box Collider instead of the Sphere Collider, Tony's **BerryBush** and **StandOnGround**, and Scale 1, 1, 1 (was 1, 1.2, 1). **Your ResourceNode and its values are unchanged** (Drop Item Berry, Max Health 10, Collect With Key on, Collect Range 3). |

What a node without BerryBush does is unchanged: trees, rocks, ores and deer work exactly as before, and so would a
berry bush without BerryBush (E removes it, hits do nothing). Your Drop Min / Max aren't used by a bush with BerryBush:
its own **Fewest / Most Berries** are (the tool copied your 3 / 5 into them). Your Lumberjack / Prospector lines aren't
reached by a bush with BerryBush (it returns first), so picking or breaking a bush never gives perk extras.

Guide: `Docs/Berry Bush Setup.md`.

## How to undo

1. In `ResourceNode.cs`: delete the lines marked `[Tony] added (23/11/2026)` (the comment block, the field, the
   `CollectRange` line, the line in `Start()` and the first line of `Break()`), and replace each line marked
   `[Tony] replaced (23/11/2026)` with the line in the comment above it.
2. Put the old bush back with git (57a0aa6 = the last commit before this change):
   `git checkout 57a0aa6 -- Assets/Prefabs/ResourceNodes/BerryNode.prefab`.
