# Tony's edit to Sanyam's ObjectScatter, 13/11/2026: spacing groups, forest patches, deer out of sight

**Files changed:**
- `Assets/Script/World/ObjectScatter.cs` (Sanyam's script). Every change is marked `// [Tony] added / edited /
  replaced`, and replaced code is kept in comments.
- `Assets/Scenes/GameScene.unity`: the ObjectScatter's Rules settings, once Tony runs **Tools > World > Set Up World
  Spawning** (`Assets/Script/Editor/WorldSpawnSetup.cs`).

**Why:** Tony raised the tree counts to 500 + 500 (1100 trees in all). ObjectScatter kept each rule's Min Spacing from
**everything** already placed, and the rules after the trees need 20 m. Almost no grass spot was 20 m from every tree,
so the Console showed:
- Berry Bushes 8 of 150,
- Forest Rocks 6 of 70,
- Animals (deer) 0 of 40.

Tony wants many trees, some forest rocks and berry bushes, and deer that appear only where the player can't see them.

---

## Code changes in ObjectScatter.cs

| What | Change |
|---|---|
| Top of the file | `[Tony]` note: what changed and why, plus the link to this log. |
| `minSpacing` | Only its comment changed: it now counts between things of the same Spacing Group. |
| New rule settings | `spacingGroup`, `clearance` (3), `patchSize` (0 = off), `openGround` (0.35), `minSize` / `maxSize` (1). Each has a tooltip. |
| `placed` | `List<Vector3>` replaced by `Dictionary<Vector2Int, List<Spot>>`: the used spots sorted into 10 m squares, each with its group and clearance. Checks look only at the nearby squares, so 1000+ trees stay fast. |
| `Start()` | Added: picks `patchSeed`, a random start for the patch pattern, so the forests are in new places each game. |
| `Scatter()` | Added step 2b `InPatch` (skip the meadows between patches). `TooClose(pos, rule.minSpacing)` replaced by `TooClose(pos, rule)`. `Instantiate` now keeps the new object and scales it by `RandomSize(rule)`. `placed.Add(pos)` replaced by `Remember(pos, rule)`. |
| `TooClose` | The old version is kept in a comment. The new one: Min Spacing from the same group, the bigger Clearance from other groups. |
| New methods | `Remember`, `CellOf`, `GroupOf` (empty group = the prefab's name), `InPatch` (Perlin noise), `RandomSize`. |

**Unchanged:** the biome, slope and height logic, Y Offset, the random turn, the parent, the attempt limit and the
Console line.

## Scene settings changed by the tool (Rules list)

| Rules | Before | After |
|---|---|---|
| `Trees: ...` (6 looks) | Kept 10 m from everything | Spacing Group `Trees` (10 m from each other), Clearance 3, Patch Size 120, Open Ground 0.35, Size 0.85 to 1.2 |
| Forest Rocks, Desert Rocks | Kept 20 m from everything | 20 m from each other (same prefab = same group), Clearance 3, Size 0.7 to 1.3 |
| Berry Bushes, ChestsForest, ChestsDesert | Kept 20 m from everything | 20 m from their own kind, Clearance 3 |
| Animals (DeerNode) | Spawned 40 once, at the start | **Removed from the list.** Tony's new `AnimalSpawner - DeerNode` object keeps 15 deer around the player, spawned only out of the player's sight (Tony's `PlayerSight` on the Player). It uses the same Terrain Layer, Min Strength and Max Slope. |
| Counts, Y Offsets, layers, slopes | | Unchanged (Tony set the tree counts himself). |

**Good to know:**
- **Old rules keep working:** a rule with an empty Spacing Group spaces itself only from rules with the same prefab.
- **Clearance 0** means it may touch other kinds.
- **Patch Size 0** spreads it evenly, like before. **Min / Max Size 1** keeps the prefab's size.
- **New tree rules** made by Tony's Use Low Poly Grassland Trees tool get Spacing Group `Trees` and copy the other new
  settings.

**To undo:**
- **The scene, before saving:** Ctrl + Z.
- **The scene, after saving:** clear Spacing Group, set Clearance to the rule's Min Spacing, Patch Size 0, Size 1 to 1.
  Add the Animals rule back (DeerNode, 40, layer 0, Min Strength 0.6, Max Slope 20, Min Spacing 20, Y Offset 0), and
  delete the `AnimalSpawner - DeerNode` object.
- **The code:** in ObjectScatter.cs, put the commented originals back and delete the `[Tony] added` lines. Those are
  the `placed` list, `TooClose(Vector3, float)`, the `Instantiate` line, `placed.Add(pos)` and the `minSpacing`
  comment.

Guide: `Docs/World Spawning Setup.md`.
