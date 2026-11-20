# Rock Setup (low-poly rocks that break into pieces)

**What the player sees:** the rocks are no longer grey cubes but rounded low-poly rocks from Broken Vector's free
**Low Poly Rock Pack**, each one a different shape:
- **stone** is grey, and sand-colored in the desert;
- **copper** is a warm reddish orange;
- **iron** is a dark blue-grey.

When a rock breaks (punched, or hit with a pickaxe), it bursts into **chunks of the same stone** that tumble, bounce
on the ground and shrink away, with small gravel and a puff of dust.

Mining works exactly as before: the same health, drops and pickaxe rules (Turki's ResourceNode isn't changed).

## Quick way: one click

1. Import the pack: Asset Store, [Low Poly Rock Pack](https://assetstore.unity.com/packages/3d/environments/low-poly-rock-pack-57874)
   (Broken Vector, free). Put it in `Assets/Imported3D/BrokenVector/LowPolyRockPack`, next to the trees.
2. Open `Assets/Scenes/GameScene.unity`.
3. Menu **Tools > World > Use Low Poly Rocks**. When it asks about the scene, click **Do it**. Then save the scene
   (**Ctrl + S**).

The tool:
- **Shapes:** copies the pack's rounder rocks into `Assets/Prefabs/ResourceNodes/Rocks/Shapes`. It skips the flat
  slabs and the tiniest rocks. Each copy is about 1.3 m big (like the old cube), with its pivot where it meets the
  ground, so 15 % of the rock sits in the ground.
- **Rock prefabs:** rebuilds Turki's `StoneNode`, `CopperNode` and `IronNode` in the same files:
  - a rock shape and a **Mesh Collider** in that shape, instead of the cube and its box;
  - **RockLook**, which picks a random shape when the game runs;
  - **StandOnGround**, which puts the rock on the floor and tilts it on slopes;
  - the stone: grey, `Assets/Materials/Rocks/Copper Ore.mat` or `Iron Ore.mat`. The two ore materials are made
    once; change their **Color** to retint them.
- **Desert look:** makes `Rocks/StoneNode Desert.prefab`, a StoneNode in the pack's sand color.
- **Rock break:** runs **Tools > Effects > Build Rock Break**, which makes the debris and puts **RockBreak** on every
  rock.
- **The scene** (it asks first):
  - Sanyam's ObjectScatter rule **Desert Rocks** spawns StoneNode Desert.
  - The rocks placed by hand are put on the ground (the old cube's pivot was its middle, so they would float).

Run it again any time, e.g. after changing the numbers at the top of `Editor/RockBuilder.cs`. Ctrl + Z undoes the
scene part; the prefabs are simply rebuilt again.

## Rock break

**Tools > Effects > Build Rock Break** (the rock tool runs it too):
- **Pictures and materials** in `Assets/Effects/RockBreak`: the dust and gravel pictures and their materials (lit
  by the sun, so darker at night), made only if they're missing.
- **Chunk shapes:** `Chunk 1-3`, the pack's smallest rocks shrunk to chunk size.
- **Debris prefab:** `Assets/Prefabs/Effects/RockDebris.prefab`, with three parts:
  - **Chunks:** 3D pieces that bounce off the ground and shrink away. Each break gives them the broken rock's own
    material.
  - **Gravel:** small specks.
  - **Dust:** soft puffs.
- **RockBreak on every rock prefab** (a ResourceNode of type Stone), with **Dust Color** measured from the rock's
  material.

## Settings

| Where | Setting | What it does |
|---|---|---|
| RockLook (on a rock) | **Shapes** | the shapes it picks from (empty = it keeps the one it has) |
| RockBreak (on a rock) | **Debris** | the RockDebris prefab |
| | **Dust Color** | dust and gravel color (the tool measures it from the material) |
| | **Chunks** (16), **Gravel** (40), **Dust** (10) | how many, for a rock 1.3 m big; bigger rocks give more |
| | **Chunk Size** (0.22 m), **Burst Speed** (3.2 m/s) | how big the chunks are and how fast everything flies |
| StandOnGround (on a rock) | **Max Tilt** (15) | how far it tilts on a slope |
| Iron Ore / Copper Ore material | **Color** | the ore's tint over the pack's colors |

## By hand (without the tool)

1. Open `Assets/Prefabs/ResourceNodes/StoneNode`.
2. Remove **ShapeColor**.
3. Set the Mesh Filter's mesh to a rock from the pack, and the Mesh Renderer's material to the pack's `Grey`.
4. Add a **Mesh Collider** with the same mesh, then remove the **Box Collider**.
5. Set Scale to 1, then save.
6. The break needs the debris prefab, so run **Tools > Effects > Build Rock Break** for it.

## Testing

1. Play and walk up to some rocks: different shapes, standing on the ground, tilted on slopes; desert rocks are
   sand-colored.
2. Break one: chunks of its stone fly out and bounce, with gravel and dust. The stone is in the inventory at once, as
   before.
3. To see the break again without mining: select a rock while playing, **RockBreak ⋮ > Test: Break Here** (the rock
   stays).

## Problems

| Problem | Why / fix |
|---|---|
| "Low Poly Rock Pack wasn't found" | Import it (step 1). The tool looks for models called `Rock Type1 01` and so on, with a `Materials` folder next to their `Models` folder. |
| Placed rocks float | The scene part was skipped or not saved: run the tool again with GameScene open and click **Do it**, then save. |
| A rock is pink | Its material lost its shader: run the tool again (the pack's materials are Standard, Built-in). |
| No pieces when a rock breaks | The rock has no RockBreak or its **Debris** is empty: run **Tools > Effects > Build Rock Break**. |
| The chunks fall through the ground | They only bounce off colliders: the terrain needs its Terrain Collider. |
| Copper and iron look too alike | Change the Color of `Assets/Materials/Rocks/Copper Ore.mat` or `Iron Ore.mat`. |

Change logs: `Docs/TonyEditMate/Turki/20-11-2026 Low Poly Rocks That Break.md` (the rock prefabs) and `Docs/TonyEditMate/Sanyam/20-11-2026 Desert Rocks Use Sand Rocks.md`
(the desert rule).
