# Tree Fall Setup (a chopped tree falls and breaks into brown wood chips)

**What the player sees:** chop a tree down (punch it, or hit it with an axe) and instead of vanishing it:
1. tips over from the foot of its trunk, **away from the player**, slowly at first and then faster, while chips fly
   from the cut;
2. slams into the ground in a burst of **brown wood chips** all along the trunk;
3. breaks apart into more chips, shrinking away, and is gone (about 2.5 seconds in all).

The wood (and maybe an apple) is in the inventory the moment the tree breaks, as before: the fall is only the show.

## Quick way: one click

1. Menu **Tools > Effects > Build Tree Fall**. It:
   - draws `Assets/Effects/WoodChips/WoodChip.png` (a small splinter) and makes `WoodChip.mat` (lit by the sun, so
     chips are darker at night), only if they aren't there yet;
   - makes `Assets/Prefabs/Effects/WoodChips.prefab`: splinters that tumble, fall back down and fade;
   - draws `Assets/Effects/TreeFall/CutWood.png` (tree rings) and makes `CutWood.mat`, only if they aren't there yet;
   - makes a **closed** copy of each tree mesh in `Assets/Effects/TreeFall/Closed Trunks` (see "The closed trunk");
   - puts **TreeFall** on every tree prefab in `Assets/Prefabs` (Turki's `ResourceNodes/TreeNode`; its looks in
     `ResourceNodes/Trees` get it from there) and fills in its **Chips**, **Cut Wood** and **Closed Meshes**. Berry
     bushes are skipped.
2. Nothing else: the trees in the scene and the ones the ObjectScatter spawns are copies of those prefabs. Run the
   tool again after adding a new tree look. (**Tools > World > Build Snow Content** runs it by itself for the snow's
   pines and spruces.)

## How it works

- **TreeFall** (on the tree) remembers the tree's meshes and materials when the game starts. When Turki's
  ResourceNode reaches 0 health and removes the tree, TreeFall builds a look-only copy from that (no scripts, no
  colliders, so nothing can bump into it) and hands it to **FallingTree**.
- **FallingTree** (on the copy) turns it over from its foot toward the side away from the player, emits the chips
  (each a random brown between Dark Wood and Light Wood), shrinks it into chips, and removes it and the chips.
- A tree removed without being chopped (the scene closing) shows nothing.

## The closed trunk

The pack's tree trunks are open tubes at the bottom: there is no face there, because a standing tree hides that end
0.25 m in the ground. A falling tree turns that end toward the player, who looked straight through the trunk (a
see-through hole right at the crosshair). So the tool finds the holes at the bottom of each tree mesh and closes each
one with a flat face of light, fresh-cut wood with rings, in a copy of the mesh. Only the falling copy uses it:
standing trees keep the pack's mesh.

## Settings (TreeFall, on the TreeNode prefab)

| Setting | What it does |
|---|---|
| **Chips** | the particle prefab (WoodChips) |
| **Dark Wood**, **Light Wood** | every chip is a random brown between these two |
| **Fall Seconds** (1.2) | how long it takes to fall over |
| **Fall Angle** (86) | how far it tips (90 = flat; a little less looks like it rests on its branches) |
| **Lie Seconds** (0.25) | how long it lies there before breaking apart |
| **Break Seconds** (0.9) | how long breaking apart into chips takes |
| **Cut Chips** (30), **Land Chips** (160), **Break Rate** (220 per second) | how many chips |
| **Chip Speed** (2.6) | how fast the chips fly, in meters per second |
| **Closed Meshes** | each tree mesh and its copy with the bottom of the trunk closed (filled by the tool) |
| **Cut Wood** | the material of that closed end (CutWood: light wood with rings) |

## By hand (instead of the tool)

1. Make a Particle System (GameObject > Effects > Particle System) named **WoodChips**: Looping on, Play On Awake
   off, Start Speed 0, Start Lifetime 0.8 - 1.5, Start Size 0.05 - 0.15, Start Rotation 0 - 360, Gravity Modifier
   1.1, Simulation Space **World**; **Emission off**, **Shape off**; Rotation over Lifetime on; Color over Lifetime
   fading out at the end; Renderer Material = a particle material with a splinter picture. Save it as a prefab.
2. Open `Assets/Prefabs/ResourceNodes/TreeNode`, **Add Component > TreeFall**, drag WoodChips into **Chips**, save.
3. The closed trunk can't be made by hand easily (it's a new mesh): run the tool for it. Without it the bottom of the
   falling trunk is see-through.

## Testing

1. Play and chop a tree down: it falls away from you and breaks into brown chips.
2. Select a standing tree while playing, TreeFall **⋮** > **Test: Fall**: a copy falls next to it (the tree stays).

## Problems

| Problem | Why / fix |
|---|---|
| The tree still just vanishes | It has no TreeFall: run the tool (the Console lists the prefabs it changed). |
| It falls but there are no chips | TreeFall's **Chips** is empty: run the tool. |
| Chips are bright at night | WoodChip.mat uses the unlit particle shader: set its Shader to **Legacy Shaders/Particles/VertexLit Blended**. |
| It falls into a hill | It always falls away from the player; on a slope part of it goes into the ground for a moment. Lower **Fall Angle**. |
| You can see through the bottom of the falling trunk | That tree's **Closed Meshes** is empty or lists another mesh (e.g. a new look): run the tool again. |

Turki's change log: `Docs/TonyEditMate/Turki/19-11-2026 Trees Fall Into Wood Chips.md`.
