# Tree Setup (low-poly grassland trees)

**What the player sees:** big low-poly trees with green crowns, 9 to 13 m tall: round oaks, broadleaf trees and
birches.
- **Gathering is unchanged:** punch one (or use the axe) and it gives Wood, and sometimes Apples.
- **Only the trunk is solid:** the player can walk and jump under the leaves.

**How it works:**
- **TreeNode is the tree:** Turki's `Assets/Prefabs/ResourceNodes/TreeNode.prefab`. Its `ResourceNode` gives the drops
  and its Capsule Collider is what gets hit.
- **The look:** a child called `Model` shows a tree mesh from Broken Vector's free **Low Poly Tree Pack**
  (`Assets/Imported3D/BrokenVector/LowPolyTreePack`), scaled to the look's height.
- **The other looks** are Prefab Variants of TreeNode, so they share its drops and health.
- **Most trees are spawned when the game starts** by Sanyam's `ObjectScatter` (on a scene object): one rule per look
  on the grass, at the ground height + each rule's **Y Offset**, in forest patches with meadows between
  (`Docs/World Spawning Setup.md`).
- **Change logs:** `Docs/TonyEditMate/Turki/12-11-2026 Low Poly Grassland Trees.md` and
  `Docs/TonyEditMate/Sanyam/12-11-2026 Tree Spawn Height And Looks.md`.

## The looks

| Look | Prefab | Pack tree | Height | Share of spawned trees |
|---|---|---|---|---|
| Round Oak | `ResourceNodes/TreeNode` | Tree Type0 06 | 9 m | 30 % |
| Oak | `ResourceNodes/Trees/TreeNode Oak` | Tree Type0 05 | 9.5 m | 20 % |
| Broadleaf | `ResourceNodes/Trees/TreeNode Broadleaf` | Tree Type4 03 | 11 m | 15 % |
| Tall Broadleaf | `ResourceNodes/Trees/TreeNode Tall Broadleaf` | Tree Type4 04 | 13 m | 10 % |
| Birch | `ResourceNodes/Trees/TreeNode Birch` | Tree Type5 03 | 11 m | 15 % |
| Birch Cluster | `ResourceNodes/Trees/TreeNode Birch Cluster` | Tree Type5 04 | 10 m | 10 % |

For size: the old placeholder tree was 5.65 m tall, and the player's character is 1.8 m.

## Quick way: one click

1. Import the **Low Poly Tree Pack** (free, Broken Vector): **Window > Package Manager > My Assets > Import**.
2. Open `Assets/Scenes/GameScene.unity`.
3. Menu **Tools > World > Use Low Poly Grassland Trees**. It asks first, because it changes Turki's TreeNode (the
   second time it asks **Build again**). Then:
   - **TreeNode becomes a Round Oak.**
     - Its cylinder and ball are removed. Its ResourceNode settings stay.
     - Its pivot moves to the foot of the trunk.
     - It's changed inside the same file, so every TreeNode changes too.
   - **It makes the 5 other looks** in `Assets/Prefabs/ResourceNodes/Trees`, each scaled to its height.
   - **It stands the trees placed in the scene on the ground**, and asks whether to mix their looks.
   - **It fixes the spawner.** The ObjectScatter's tree rule had **Y Offset 2**, made for the old tree whose pivot
     was in the middle of its 4 m trunk, so the new trees floated 2 m up. The tool sets it to **0**, and asks:
     - **Fix + all 6 looks:** one rule per look (Round Oak 60, Oak 40, Broadleaf 30, Tall Broadleaf 20, Birch 30,
       Birch Cluster 20 = the same 200 trees).
     - **Only fix the height:** keeps 200 Round Oaks.
   - **Ctrl + Z** undoes the scene part (trees and spawner). Running it again rebuilds TreeNode and the looks in the
     same files.
4. Save the scene (**Ctrl + S**).

```
TreeNode        ResourceNode (Turki) + Capsule Collider around the trunk; pivot = foot of the trunk
  Model         Mesh Filter (the pack's tree mesh) + Mesh Renderer (the pack's "Normal" material), scaled to the height
```

## Test

1. Press Play. Trees stand on the grass everywhere, none in the air.
2. Walk to one: it's big, and you can walk under its leaves.
3. Punch the trunk (or hit it with the axe) until it breaks: **"+3 Wood"** (sometimes Apples too) shows bottom left.
4. Punch with an empty hand: the grey wind burst pops on the trunk (`Docs/Punch Effect Setup.md`).

## Placing more trees

**By hand:**
1. Drag `TreeNode`, or any look in `ResourceNodes/Trees`, from the Project window onto the ground in the Scene view.
   The pivot is the foot of the trunk, so it lands standing on the ground.
2. **Tools > World > Mix Trees In Scene** gives every placed tree a random look, turn and size (0.9 to 1.15), and stands
   it on the lowest ground under its trunk. **Ctrl + Z** undoes it.

**Spawned:** select the object with **ObjectScatter**, then change the `Trees: ...` rules' **Count**. Keep **Y Offset**
at 0 for trees.

**Drops and health:** change them on `TreeNode` (its ResourceNode), and every look follows. Changing them on one look
makes only that look different.

## Settings

| Where | What |
|---|---|
| `Looks` at the top of `Assets/Script/Editor/GrasslandTreeBuilder.cs` | Which pack trees are used, their names, **heights** (meters) and shares of the spawned trees. The first one becomes TreeNode. **Bigger or smaller trees:** change the heights and run the tool again. |
| `SinkIntoGround` (0.25) | Meters the trunk goes into the ground, so it doesn't float on a slope (the spawner allows slopes up to 25°). |
| `MinTrunkCollider`, `MaxTrunkCollider` (0.3, 0.9) | The Capsule Collider is the trunk, 1.5 times as thick as it looks but within these, and 75 % of the tree's height. Thicker = easier to punch. |
| `MinSize`, `MaxSize` (0.9, 1.15) | The random size range of Mix Trees In Scene. |
| ObjectScatter's rules | **Count** = how many of each look. **Min Spacing** (10) = meters between trees (every look has the Spacing Group `Trees`). **Clearance** (3) = meters from rocks, bushes and chests. **Patch Size** / **Open Ground** = forests and meadows, **Min / Max Size** = random sizes (`Docs/World Spawning Setup.md`). **Terrain Layer** 0 = the grass. |

## Other biomes in the same pack

Kept out of the grassland, for later:

| Pack trees | What they are | Biome |
|---|---|---|
| Type1, Type2, Type0 03 | pines, spruces, a redwood | forest, snow |
| Type3, Type0 04 | umbrella trees, an acacia | savanna |
| Type6 | willows | lake shore, swamp |
| Type7 | palms | beach, desert |
| Type0 02 | a dead tree | desert, spooky |
| Type0 01 | a huge spreading oak with roots (8.7 m wide, low crown) | a landmark |

**Colour schemes:** the pack has 4 materials in `Materials/`: **Normal** (summer green, used here), **Fall**
(autumn), **Dry** and **Cold** (winter). To change one tree's colours, drag another material onto its `Model`.

## By hand (the same thing without the tool)

1. Open `TreeNode.prefab` (double-click it).
2. On the top object, remove **ShapeColor**, then **Mesh Renderer**, then **Mesh Filter** (ShapeColor first: it needs
   the Mesh Renderer).
3. Delete the child **Leaves**.
4. Top object: **Position** 0, 0, 0 and **Scale** 1, 1, 1.
5. Right-click the top object > **Create Empty**, name it `Model`.
   1. Add **Mesh Filter**, Mesh = `Tree Type0 06_mesh` (click the circle and search "Type0 06").
   2. Add **Mesh Renderer**, Material = `Imported3D/BrokenVector/LowPolyTreePack/Materials/Normal`.
   3. Position 0, -0.25, 0 and Scale 1.84 on X, Y and Z: 9 m ÷ 4.9 m, the mesh's height.
6. Top object's **Capsule Collider**: Center 0, 3.375, 0, Radius 0.41, Height 6.75, Direction Y-Axis. Save the prefab.
7. Another look:
   1. Right-click `TreeNode` > **Create > Prefab Variant**, and name it e.g. `TreeNode Birch`.
   2. Open it, select `Model`, and set its Mesh to `Tree Type5 03_mesh` and its Scale to its height ÷ the mesh's height.
   3. Fit the Capsule Collider to the new trunk.
8. Trees placed in the scene: lower them onto the ground. Their Y was set for the old pivot.
9. The spawner: select the object with **ObjectScatter**, open **Rules > Trees** and set **Y Offset** to 0. For more
   looks, add a rule per look (same Terrain Layer, Min Strength, Max Slope and Min Spacing) and share the Count.

## Problems

| Problem | Why / fix |
|---|---|
| "Low Poly Tree Pack isn't in the project" | Import it (step 1 of the quick way). |
| Spawned trees float about 2 m up | The ObjectScatter's tree rules still have Y Offset 2 (made for the old tree). Run the tool again, or set them to 0 by hand (step 9 above). |
| Placed trees float or are half underground | They were placed for another pivot. Run **Tools > World > Mix Trees In Scene**, or set their Y to the ground. |
| Trees are too small / too big | Change the heights in `Looks` (top of the tool) and run it again. |
| All spawned trees look the same | The spawner only has one tree rule (TreeNode). Run the tool again and choose **Fix + all 6 looks**. |
| Trees are pink | The pack's material lost its shader: select `LowPolyTreePack/Materials/Normal`, Shader = **Standard**. |
| A tree is invisible | The pack was deleted or moved out of the project, so `Model` has no mesh. Import it again and run the tool. |
| Punches miss the tree | Aim at the trunk. The leaves have no collider. Or make the Capsule Collider thicker. |
| One look wasn't made | The Console says which pack tree is missing. Check the names in `Looks` at the top of the tool. |
