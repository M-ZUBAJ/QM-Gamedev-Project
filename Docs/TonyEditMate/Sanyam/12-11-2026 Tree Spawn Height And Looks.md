# Tony's edit to Sanyam's spawner settings, 12/11/2026: tree spawn height and looks

**No code of Sanyam's was changed:** `Assets/Script/World/ObjectScatter.cs` is untouched. What changed is the
**Rules** list of the ObjectScatter component in `Assets/Scenes/GameScene.unity` (the settings in its Inspector). Tony's
tool **Tools > World > Use Low Poly Grassland Trees** (`Assets/Script/Editor/GrasslandTreeBuilder.cs`) changed it.

**Why:** Turki's `TreeNode` is now a big low-poly tree, and its pivot moved:
- **Old tree:** a 4 m cylinder with its pivot in the middle, so your tree rule had **Y Offset 2** to lift it onto the
  ground.
- **New tree:** the pivot is at the foot of the trunk, so with Y Offset 2 every spawned tree floated 2 m in the air.

Tony also wanted the spawned trees to use all the new tree looks, not just one.

---

## The change (ObjectScatter on the scene object, Rules list)

**Before:** one rule, `Trees`:
- Prefab `TreeNode`, Count 200, **Y Offset 2**;
- Terrain Layer 0, Min Strength 0.7, Max Slope 25, Min Spacing 10.

**After, if Tony chose "Fix + all 6 looks":** that rule is replaced, in the same place in the list, by 6 rules with the
same Terrain Layer, Min Strength, Max Slope and Min Spacing, and **Y Offset 0**:

| Rule | Prefab | Count |
|---|---|---|
| Trees: Round Oak | `ResourceNodes/TreeNode` | 60 |
| Trees: Oak | `ResourceNodes/Trees/TreeNode Oak` | 40 |
| Trees: Broadleaf | `ResourceNodes/Trees/TreeNode Broadleaf` | 30 |
| Trees: Tall Broadleaf | `ResourceNodes/Trees/TreeNode Tall Broadleaf` | 20 |
| Trees: Birch | `ResourceNodes/Trees/TreeNode Birch` | 30 |
| Trees: Birch Cluster | `ResourceNodes/Trees/TreeNode Birch Cluster` | 20 |

Together that's the same 200 trees.

**If Tony chose "Only fix the height":** the `Trees` rule stays as it was, with Y Offset 0 instead of 2.

**Other rules** (chests, berry bushes, rocks, animals) are untouched.

**Good to know:**
- **Spacing:** your `placed` list is shared by all rules, so trees of different looks still keep Min Spacing (10 m)
  from each other and from everything else.
- **New tree prefabs need Y Offset 0:** every tree look's pivot is the foot of its trunk.

**To undo:**
- **Before saving the scene:** Ctrl + Z.
- **After saving:** in the ObjectScatter's Rules, delete the six `Trees: ...` rules and add one back with the settings
  under **Before**.

Tree guide: `Docs/Tree Setup.md`.
