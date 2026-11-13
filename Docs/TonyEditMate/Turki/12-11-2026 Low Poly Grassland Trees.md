# Tony's edit to Turki's tree, 12/11/2026: low-poly grassland trees

**No code of Turki's was changed:** `ResourceNode.cs` is untouched. What changed is the look of Turki's
`TreeNode.prefab` and where its pivot is. It was changed by Tony's tool **Tools > World > Use Low Poly Grassland Trees**
(`Assets/Script/Editor/GrasslandTreeBuilder.cs`).

**Why:** Tony asked to replace the placeholder tree (a brown cylinder with a green ball) with a low-poly tree that
suits a grassland. The trees come from Broken Vector's free **Low Poly Tree Pack** (`Assets/BrokenVector/LowPolyTreePack (now Assets/Imported3D/BrokenVector/LowPolyTreePack)`).

---

## `Assets/Prefabs/ResourceNodes/TreeNode.prefab`

**Kept as it was:** the `ResourceNode` component and all its settings (Drop Item Wood 3 to 5, Max Health 30, Resource
Type Tree, bonus Apple 1 to 3 at 30 %), the object's name, and its Capsule Collider component (only resized).

**What changed:**

| | Before | After |
|---|---|---|
| Top object's position / scale | (0, 2, 0) / (1, 2, 1): the pivot was the middle of the 4 m trunk | (0, 0, 0) / (1, 1, 1): the pivot is the foot of the trunk, so a tree dropped on the ground stands on it |
| Trunk look | Cylinder (Mesh Filter + Mesh Renderer) on the top object, brown (0.478, 0.294, 0.165) by ShapeColor | removed (Mesh Filter, Mesh Renderer and ShapeColor) |
| Leaves look | child `Leaves`: a Sphere at (0, 1.2, 0), scale (2.5, 1.25, 2.5), green (0.18, 0.545, 0.227) by ShapeColor | removed |
| New look | — | child `Model`: the pack's `Tree Type0 06` mesh ("Round Oak") with its `Normal` material, scaled 1.84 times to 9 m tall, the foot of its trunk 25 cm under the pivot |
| Capsule Collider | radius 0.5, height 2 (4 m with the old scale), around the whole trunk | radius 0.41, height 6.75, center (0, 3.375, 0): the trunk, up into the crown. The leaves have no collider (walk under them) |

**Good to know:**
- **Hitting it:** gathering works as before. Marko's PlayerCombat finds your `ResourceNode` on the collider it hits.
- **Its new pivot** is at the foot of the trunk. Drop new trees straight onto the ground, no need to raise them 2 m.

**Sizes (second run, same day):** the first build used the pack's own sizes (about 5 m). Tony found them too small, so
every look is now scaled to a height: Round Oak 9 m, Oak 9.5 m, Broadleaf 11 m, Tall Broadleaf 13 m, Birch 11 m, Birch
Cluster 10 m. The old tree was 5.65 m.

**The trees that spawn when the game starts** come from Sanyam's ObjectScatter. Its tree rule lifted them 2 m (for the
old pivot), so they floated. That's fixed in its settings: `Docs/TonyEditMate/Sanyam/12-11-2026 Tree Spawn Height And Looks.md`.

## New: 5 more tree looks

In `Assets/Prefabs/ResourceNodes/Trees/`: `TreeNode Oak`, `TreeNode Broadleaf`, `TreeNode Tall Broadleaf`,
`TreeNode Birch`, `TreeNode Birch Cluster`.
- **Prefab Variants of TreeNode:** they change only the Model's mesh and the collider's size.
- **Your settings flow through:** change the drops or health on `TreeNode` and every look follows.
- **Don't change those numbers on a look,** unless that one tree should be different.

## `Assets/Scenes/GameScene.unity`: the 2 placed trees

The two TreeNodes near the spawn were at y = 2 (the old middle-of-the-trunk pivot). The tool stood them on the ground
below them. If Tony chose **Mix them**, each also got a random look, turn and size (0.9 to 1.15).

---

**To undo:**
1. `git checkout 5fa06fc -- Assets/Prefabs/ResourceNodes/TreeNode.prefab` (the last commit before this change).
2. Delete the folder `Assets/Prefabs/ResourceNodes/Trees`.
3. In GameScene, set the two trees' Y back to 2 and their prefab back to TreeNode. Or, before saving the scene, press
   Ctrl + Z.

## For Turki and Sanyam: placing trees

- **Place trees:** drag `TreeNode`, or any look from `Trees/`, onto the ground.
- **Mix them:** after placing many, **Tools > World > Mix Trees In Scene** gives each a random look, turn and size and
  stands it on the ground.
- **The leaves aren't solid:** only the trunk is.
- **Guide:** `Docs/Tree Setup.md`.
