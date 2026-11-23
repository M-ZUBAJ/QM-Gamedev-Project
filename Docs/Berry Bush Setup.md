# Berry Bush Setup (a real bush with red berries you pick with E)

**What the player sees:** berry bushes are no longer a green ball with three red balls. They're leafy bushes from
Yughues' free **Free Bushes 2018** pack, with small red berries (bunches of three) on the outside of the leaves. Two
looks are used: green leaves in rows (like an elderberry, the pack's `P_Bush01`) and twigs with small round leaves
(like a currant bush, `P_Bush05`). Each bush picks one from where it stands, so they don't all look the same.

**What the player does:**
- **E picks the berries:** look at a bush from close by (3 m) and press **E** (the Interact key). Every berry on it
  goes into your inventory at once ("+4 Berry") and **the bush stays**.
- **They grow back:** one berry every **60 seconds** of game time, until the bush is full again (each bush holds 3 - 5,
  as many as the old bush gave). The pause menu stops the time.
- **The prompt under the crosshair** says `E  Pick berries  x4`, or on a picked bush `E  Berries grow back in  0:42`
  (counting down to the next berry).
- **Hitting breaks it:** punch it or hit it with a tool (health 10, like before): it breaks and you get the berries
  still on it, nothing more. Before, hits did nothing to a berry bush.

## Quick way: one click

1. Import the pack: Asset Store, **Yughues Free Bushes 2018** (free). Put its folder in `Assets/Imported3D`
   (`Assets/Imported3D/YughuesFreeBushes2018`). Its materials work in our Built-in pipeline as they are (nothing pink).
2. Menu **Tools > World > Build Berry Bushes**, then **Rebuild**. Any scene can be open: no scene changes.
3. Play and test (below).

The tool:
- **Berries:** `Assets/Prefabs/ResourceNodes/BerryBush/Berry Cluster.asset` (three small red balls in one mesh, made by
  the tool) and `Assets/Materials/BerryBush/Berry.mat` (shiny red; made only if missing, so you can recolor it).
- **Looks:** reads the pack's `P_Bush01` and `P_Bush05` (their 4 levels of detail and material). For each it works out
  a **Body** box (the thick part of the leaves) and **berry spots**: it looks at the bush from all around and from
  above, stops at the first leaf that is really there (not a see-through part of the leaf picture), and keeps the 12
  spots farthest apart. The pack's other three bushes aren't used (orange autumn leaves, purple flowers, bamboo-like
  leaves).
- **The bush:** rebuilds Turki's `Assets/Prefabs/ResourceNodes/BerryNode.prefab` in the same file, so every scene,
  ObjectScatter rule and the new world keep their link to it:
  - the green ball, the three red balls and their ShapeColor go;
  - **Model > LOD0 - LOD3**: the leaves, near to far;
  - **Berries > Berry 1 - 5**: the berry bunches;
  - a **LOD Group**: Unity draws the near leaves up close and simpler ones farther away; the berries only on the 3
    nearest levels (farther than about 25 m they're too small to see);
  - a **Box Collider** (the Body) instead of the Sphere Collider: what you bump into, hit and look at;
  - Tony's **BerryBush**, filled in (the first time, Fewest / Most Berries = Turki's Drop Min / Max: 3 / 5);
  - **StandOnGround**: the bush stands on the ground whatever height it's spawned at (the scatter rules still say
    0.6 m, made for the old ball).
  - Turki's **ResourceNode** stays as it is (Drop Item Berry, Max Health 10, Collect With Key on, Collect Range 3).

Run it again any time: the bush is rebuilt, and your BerryBush numbers (Fewest / Most Berries, Grow Seconds) stay. To
make more berries per bush, raise **Most Berries** first, then run it again (it makes that many, up to 12).

## Testing

1. Open `Assets/Scenes/GameScene.unity` (or start the new world from the title screen) and press **Play**.
2. Walk on the grass until you find a berry bush (a bush with red berries).
3. Look at it from close by (first person): the prompt says `E  Pick berries  x3` (or x4, x5).
4. Press **E**: "+3 Berry" at the bottom left, the berries are gone, the bush stays, and the prompt counts down
   `0:59, 0:58...`. After a minute one berry is back (`x1`).
5. Select the bush in the Hierarchy (it's a child of the scatter object, "BerryNode(Clone)"), then **⋮** (top right of
   its BerryBush) > **Test: Grow All Now**: it's full at once.
6. Punch it twice (or hit it with a tool): it breaks and the berries still on it go into your inventory.

## Settings

| Where | Setting | What it does |
|---|---|---|
| BerryNode > **BerryBush** | **Fewest Berries** / **Most Berries** (3 / 5) | how many berries a full bush holds; each bush picks its own number between them. Most Berries can't be more than the berries the tool made (run the tool again after raising it) |
| | **Grow Seconds** (60) | seconds for one berry to grow back, on game time (the pause menu stops it) |
| | **Empty Text** | message when E is pressed on a bush with no berries yet (empty = none) |
| | **Looks**, **Leaf Parts**, **Berry Parts**, **Body** | filled by the tool: the bushes it can wear, its leaves, its berries, its box |
| BerryNode > **ResourceNode** (Turki's) | **Drop Item** | what a berry is (Berry) |
| | **Max Health** (10) | how much hitting it takes to break |
| | **Collect Range** (3) | how close you must be for E (and the prompt) |
| | Drop Min / Max | no longer used by a bush with BerryBush (Fewest / Most Berries are) |
| BerryNode > **StandOnGround** | **Sink** (0.04), **Max Tilt** (10) | how far it sinks into the ground and how much it leans on a slope |
| `Assets/Materials/BerryBush/Berry.mat` | **Albedo** color, **Smoothness** | the berries' color and shine |
| HUDCanvas > **ChestPrompt** | **Pick Text**, **Growing Text**, **Wait Color** | the words and the countdown's color on a bush |

## How it works

| Script | On | What it does |
|---|---|---|
| **BerryBush** (Tony's, `Assets/Script/World/BerryBush.cs`) | the bush | when the bush appears: picks its look and its number of berries from where it stands (a bush made again in the same spot, as the new world does, looks the same), and puts its berries on its look's spots in its own order. Then: `Pick()` gives all berries, `GiveLeft()` gives what's left when it breaks, and the berries grow back one every Grow Seconds |
| **ResourceNode** (Turki's) | the bush | watches for E (as before) and calls BerryBush's `Pick()` instead of removing the bush; takes hits (a bush with BerryBush can now be hit); when its health is 0, BerryBush gives the berries still on it. Tony's 5 marked lines, log `Docs/TonyEditMate/Turki/23-11-2026 Berry Bushes You Pick With E.md` |
| **ChestPrompt** (Tony's, HUD) | under the crosshair | `E  Pick berries  x4`, or the countdown to the next berry |
| **StandOnGround** (Tony's) | the bush | puts it on the ground when the game starts |

## Problems

| Problem | Fix |
|---|---|
| The bushes are still green balls | Run **Tools > World > Build Berry Bushes**. |
| "nothing done: P_Bush01, P_Bush05 not found" | Import **Yughues Free Bushes 2018** first (its prefabs `P_Bush01` and `P_Bush05` must be in the project). |
| E does nothing | Look straight at the bush from closer than 3 m (first person). In third person the camera is too far away (like the chests). |
| No prompt under the crosshair | The HUD needs **ChestPrompt** (Tools > UI > Build HUD). E still works without it. |
| A bush floats or sinks | StandOnGround puts it on the ground at the start: check the bush has StandOnGround (run the tool again). |
| A bush has fewer berries than Most Berries | Each bush picks its own number between Fewest and Most. Also, Most Berries can't be more than its Berry Parts: run the tool again after raising it. |
| "+N Berry" doesn't show | That's the notifications (GameRules > Show Notifications); the berries still go into the inventory. |
| Berries grow back while the game is paused | They don't: they count game time. A bush in a part of the new world you walked away from keeps growing and shows the new berries when you come back. |

## By hand (what the tool does)

1. Open `Assets/Prefabs/ResourceNodes/BerryNode.prefab` (double-click).
2. Delete its children (the red spheres). On BerryNode, remove **Shape Color**, **Mesh Renderer** and **Mesh Filter**,
   and set its Scale to 1, 1, 1.
3. **Add Component > Box Collider**, then remove the **Sphere Collider**. Set the box around the bush, a little
   inside its leaves (about Center 0, 0.58, 0 and Size 0.9, 1.17, 0.8).
4. Right click BerryNode > **Create Empty**, name it `Model`. In it make 4 empties `LOD0` - `LOD3`, each with Scale
   0.1, 0.1, 0.1, a **Mesh Filter** (Mesh: `Bush01_LOD0` ... `Bush01_LOD3` from `Meshes/Bush01.FBX`) and a **Mesh
   Renderer** (Material `M_Bush01`).
5. Make an empty `Berries` with empties `Berry 1` - `Berry 5`, each with a **Mesh Filter** (Mesh: a small sphere, about
   7 cm) and a **Mesh Renderer** (a red material, Cast Shadows Off). Put them on the outside of the leaves.
6. **Add Component > LOD Group** on BerryNode: LOD 0 = LOD0 + the berries, LOD 1 = LOD1 + the berries, LOD 2 = LOD2 +
   the berries, LOD 3 = LOD3.
7. **Add Component > Berry Bush**: drag LOD0 - LOD3 into **Leaf Parts**, Berry 1 - 5 into **Berry Parts**, the Box
   Collider into **Body**. Leave **Looks** empty: then the bush keeps the look you made, and its berries stay where you
   put them.
8. **Add Component > Stand On Ground**: Foot Offset 0, Sink 0.04.
9. Save the prefab (Ctrl + S).
