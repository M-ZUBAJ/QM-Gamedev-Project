# Tony's fix to Sanyam's ObjectScatter settings, 19/11/2026: berry bushes spawn again

**Files changed:**
- `Assets/Scenes/GameScene.unity`: 3 settings of the **Berry Bushes** rule in your ObjectScatter, when Tony runs
  **Tools > World > Set Up World Spawning** (`Assets/Script/Editor/WorldSpawnSetup.cs`, Tony's tool) again.
- Your `ObjectScatter.cs` is **not** changed this time.

**Why:** the Console said "Berry Bushes: spawned 1 of 150". On 13/11/2026 Tony's tool gave the tree settings to every
rule whose prefab's ResourceNode has Resource Type **Tree**. Turki's BerryNode has Resource Type Tree too (it's the
first choice; berries are picked with E through his **Collect With Key**), so the bush rule got them as well:
- **Spacing Group "Trees":** the bushes' Min Spacing (20 m) then counted against every tree (about 950), so there was
  almost no spot left for a bush.
- **Patch Size 120:** bushes only inside the forests.
- **Min / Max Size 0.85 / 1.2.**

**What changed:** the tool now treats a ResourceNode with **Collect With Key** on as a bush, not a tree, and puts back
a bush rule it had filed with the trees (each value only while it's still exactly the tree value):

| Berry Bushes setting | Was (from the 13/11 run) | Now |
|---|---|---|
| Spacing Group | Trees | empty (= its own group: Min Spacing only between bushes) |
| Patch Size | 120 | 0 (anywhere on its ground, like before 13/11) |
| Min Size / Max Size | 0.85 / 1.2 | 0 / 0 (= the prefab's own size) |

Unchanged: its Count (150), Y Offset, Terrain Layer, Min Strength, Max Slope, Min Spacing (20) and Clearance (3).
The other rules aren't touched.

**To undo:** Ctrl + Z right after the tool, or type the "Was" values back into the Berry Bushes rule.

Guide: `Docs/World Spawning Setup.md`.
