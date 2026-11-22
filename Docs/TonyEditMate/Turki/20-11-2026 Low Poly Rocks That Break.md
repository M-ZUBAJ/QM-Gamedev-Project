# Tony's edit to Turki's rocks, 20/11/2026: low-poly rocks that break into pieces

**Files changed (prefabs only):** `Assets/Prefabs/ResourceNodes/StoneNode.prefab`, `CopperNode.prefab` and
`IronNode.prefab`, rebuilt in place by Tony's **Tools > World > Use Low Poly Rocks** (`Editor/RockBuilder.cs`), so
every link to them (the scene, ObjectScatter) stays. Your `ResourceNode.cs` is **not** changed, and neither are its
values (health, drops, type Stone).

On each of the three:
- **Removed:** ShapeColor (the cube's color) and the Box Collider.
- **Changed:** the Mesh Filter shows a low-poly rock (Broken Vector's free Low Poly Rock Pack, copied to about the
  old cube's size), and the Mesh Renderer uses the pack's grey (StoneNode), `Copper Ore.mat` or `Iron Ore.mat`
  (`Assets/Materials/Rocks`). Scale (1.5, 1, 1.5) is now 1, because the size is in the shape. Its pivot is now where
  the rock meets the ground, not the middle of a cube.
- **Added:**
  - a **Mesh Collider** in the rock's shape: your ResourceNode needs a collider, and this is it now;
  - Tony's **RockLook**, which gives each rock a random shape when the game starts;
  - Tony's **StandOnGround**, which puts it on the floor and tilts it on slopes;
  - Tony's **RockBreak**, which bursts it into chunks of the same stone, gravel and dust when your ResourceNode
    breaks it (it watches for `currentHealth <= 0` when the rock is removed).

**In GameScene:** your 12 StoneNodes, 12 CopperNodes and 13 IronNodes placed by hand were moved down onto the ground
(their old pivot was the middle of the cube). Nothing else about them changed.

**Why (Tony asked for it):** real rocks instead of cubes, and pieces when a rock breaks.

**To undo:** restore the three prefabs from git (`git checkout <commit before> -- Assets/Prefabs/ResourceNodes/StoneNode.prefab`
and the same for the other two) and the placed rocks with the scene.

Guide: `Docs/Rock Setup.md`.
