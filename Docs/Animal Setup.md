# Animal Setup (deer that run away)

**What the player sees:** deer on the grass instead of sheep.
- **Calm:** they graze, then walk a little, around their spot.
- **Scared:** when the player comes closer than 12 m, or hits one, it bolts away: it runs, steering around trees, rocks
  and steep slopes, and stays on the ground.
- **Calm again:** once the player is more than 24 m away, it stops and grazes again, and that spot becomes its new
  home.
- **Hunting:** hit one until its health is gone and it gives **Meat** (1 to 3), like the sheep did.

**How it works:**
- **DeerNode:** `Assets/Prefabs/ResourceNodes/DeerNode.prefab`.
  - **Turki's ResourceNode:** health and the meat drop, copied from the sheep (AnimalNode).
  - **Tony's WildAnimal:** the grazing, walking and running away.
  - **A kinematic Rigidbody:** WildAnimal moves the deer itself, so physics never pushes it or tips it over.
  - **A Box Collider:** what punches hit, and what the player bumps into.
- **The look:** ithappy's free **Animals FREE** pack (`Assets/Imported3D/ithappy/Animals_FREE`), with idle, walk and run
  animations.
- **The pack is made for URP:** its material would show pink in our Built-in project. The tool makes a Built-in copy
  with the same picture.
- **Spawning:** an **AnimalSpawner** keeps 15 deer around the player, and only makes new ones where the player can't
  see them (PlayerSight on the Player). Set it up with **Tools > World > Set Up World Spawning**, guide
  `Docs/World Spawning Setup.md`. (Until 13/11/2026 Sanyam's ObjectScatter placed 40 at the start.)

## Quick way: one click

1. Import **Animals FREE** (ithappy, free): **Window > Package Manager > My Assets > Import**.
2. Open `Assets/Scenes/GameScene.unity`.
3. Menu **Tools > World > Build Wild Animal** (nothing selected = the deer). It makes:
   - `Assets/Prefabs/ResourceNodes/DeerNode.prefab` (it asks before replacing it, and rebuilds it inside the same file);
   - `Assets/Animation/Animals/DeerNode.controller`;
   - `Assets/Materials/Animals/Material (Built-in).mat`, a Standard copy of the pack's material (only if missing).
4. It asks **"Replace the sheep (AnimalNode) in the open scene with DeerNode?"** Click **Replace them**:
   - **The ObjectScatter's sheep rule** now spawns DeerNode, with **Y Offset** 0 instead of 0.6. The sheep's pivot was
     the middle of its body; the deer's is its feet.
   - **The sheep placed by hand** become deer, standing on the ground.
   - **Ctrl + Z** undoes it.
5. Menu **Tools > World > Set Up World Spawning**: the deer move from the ObjectScatter to an **AnimalSpawner**, which
   spawns them out of the player's sight (`Docs/World Spawning Setup.md`).
6. Save the scene (**Ctrl + S**).

**Another animal:** select its prefab in the Project window first, e.g. `Imported3D/ithappy/Animals_FREE/Prefabs/Horse_001`. You
get `HorseNode`. The pack also has a chicken, dog, kitty, penguin and tiger.

```
DeerNode     Rigidbody (kinematic) + Box Collider + ResourceNode (Turki: Meat 1-3, health 100) + WildAnimal (Tony)
  Model      the deer from the pack: Animator (DeerNode controller), Skinned Mesh Renderer (Built-in material)
```

## Test

1. Press Play and find a deer on the grass. To place one for testing, drag `DeerNode` into the scene near the spawn.
2. Stand still far away: it grazes and walks around.
3. Walk toward it: closer than 12 m it runs away from you, around trees and rocks.
4. Chase it. It runs at 7 m/s and you walk at 7.8, so you can catch it, faster when sprinting. Hit it: it keeps
   running for 5 s even if you stop.
5. Hit it until it falls: **"+N Meat"** shows bottom left.
6. Select a deer in the Scene view to see its circles:
   - **yellow:** where it wanders;
   - **red:** how close you can come before it runs;
   - **green:** how far away you must be before it calms down.

## Settings (WildAnimal, on DeerNode)

| Field | What it does |
|---|---|
| **Wander Radius** (12) | Meters from its home that it wanders. Home = where it appeared, or where it calmed down. |
| **Walk Speed** (1.4) | Meters per second while wandering. |
| **Idle Seconds** (2, 7) | It grazes for a random time between X and Y seconds between walks. |
| **Flee Distance** (12) | It runs when the player comes closer than this. |
| **Safe Distance** (24) | It calms down when the player is farther than this. Always bigger than Flee Distance. |
| **Run Speed** (7) | Meters per second when running away. The player walks at 7.8. Raise it to make hunting harder. |
| **Panic Seconds** (5) | Seconds it keeps running after being hit. |
| **Turn Speed** (270) | Degrees per second. |
| **Max Slope** (35) | Steeper ground is a wall to it. |
| **Look Ahead** (2) | Meters ahead it checks for trees, rocks and walls (more when running). |
| **Walk / Run Animation Speed** (1, 1.5) | How fast the legs move, so they keep up with the speed. |

**Meat and health:** change them on DeerNode's **ResourceNode** (Drop Item, Drop Min / Max, Max Health). The tool
copied them from the sheep, so running it again copies the sheep's again.

**Size:** `Size` at the top of `Assets/Script/Editor/WildAnimalBuilder.cs` (1 = the pack's own size, about 2 m with
the antlers). Run the tool again after changing it.

## By hand (the same thing without the tool)

1. **A Built-in material:**
   1. Right-click in `Assets/Materials/Animals` > **Create > Material**.
   2. Set **Shader** to **Standard**, **Albedo** to `Imported3D/ithappy/Animals_FREE/Textures/Texture`, and **Smoothness** to 0.1.
2. **A controller:**
   1. Right-click in `Assets/Animation/Animals` > **Create > Animator Controller**, named `DeerNode`.
   2. Add two Float parameters: `Speed`, and `AnimSpeed` (default 1).
   3. Right-click the grid > **Create State > From New Blend Tree**, named `Move`. Double-click it and set Parameter
      **Speed**.
   4. Add three motions: `Deer_001_idle` at threshold 0, `Deer_001_walk` at 1, `Deer_001_run` at 2. Untick
      **Automate Thresholds** first. The clips are inside `Meshes/Deer_001.fbx`.
   5. Back on the `Move` state, tick **Multiplier > Parameter** and choose `AnimSpeed`.
3. **The prefab:**
   1. Create an empty object `DeerNode` at 0, 0, 0.
   2. Drag `Imported3D/ithappy/Animals_FREE/Meshes/Deer_001.fbx` onto it, and rename the child `Model`. Use the model, not the
      pack's prefab: the prefab has the pack's demo scripts (keyboard control, CharacterController).
   3. On Model's **Animator**: Controller `DeerNode`, **Apply Root Motion** off.
   4. On its Skinned Mesh Renderer: the Built-in material.
4. **On DeerNode:**
   1. Add **Rigidbody**: **Is Kinematic** on, **Use Gravity** off.
   2. Add **Box Collider** around the body.
   3. Add **ResourceNode**: Drop Item Meat, Max Health 100, Resource Type Animal, Drop Min 1, Drop Max 3.
   4. Add **WildAnimal**.
   5. Drag it into `Assets/Prefabs/ResourceNodes`.
5. **The spawner:** an **AnimalSpawner** with **Animal Prefab** = `DeerNode`, and **PlayerSight** on the Player. Steps:
   `Docs/World Spawning Setup.md` (By hand).

## Problems

| Problem | Why / fix |
|---|---|
| The deer is pink | It uses the pack's URP material. Run the tool again (it makes the Built-in copy), or set the material by hand (step 1 above). |
| The deer slides without moving its legs | The Animator has no controller, or the controller has no `Speed` / `AnimSpeed`. Run the tool again. |
| It floats or sinks | The ObjectScatter's rule still has Y Offset 0.6 (made for the sheep). Set it to 0. |
| No deer at all | The Player has no PlayerSight (the Console says so), or the ObjectScatter still spawns them after 1000 trees took every spot. Run **Tools > World > Set Up World Spawning**. |
| It doesn't run away | **No player:** WildAnimal finds the player by its PlayerController.<br>**Too far:** you're outside Flee Distance.<br>**You're dead:** animals ignore a dead player. |
| No green burst when it dies | DeerNode has no DeathEffect: run **Tools > Effects > Build Death Particles** (`Docs/Death Effect Setup.md`). |
| It never calms down | Safe Distance is bigger than you can get away. Lower it. |
| It gets stuck at a wall | It turns up to 160° to find a way. In a tight corner it turns around; give it more room, or lower Look Ahead. |
| Still sheep in the game | The tool only changes the open scene. Open the other scene and run it again, or click **Replace them** this time. |
