# New World Setup (a new random world every game)

**What the player sees:**
- On the title screen's **NEW GAME** page, the WORLD section starts with **Map**: **Old world** (the hand-made map,
  GameScene, as before) or **New world** (a new random world every game). START GAME loads the one picked.
- **The new world** is 1000 x 1000 m:
  - The player starts in a small clearing in the middle of **green grassland**: gentle hills painted green, with
    tufts of grass waving in the wind, trees and berry bushes, and deer herds wandering around (they flee when you come
    close).
  - Further out: a sandy **desert** on one side (with sand-colored rocks), and **rocky hills** with snowy tops all
    around the rest. Some hill slopes are too steep to climb.
  - Along the edge, steep mountains (and invisible walls behind them): nobody can walk off the world.
  - **Rock caves** in the grassland (6, between 60 and 230 m from the start): a walk-in mound of rocks with a roof,
    dim inside, with **Stone, Copper and Iron** rocks to mine (2 - 3 iron rocks and 2 - 3 copper rocks per cave, the
    rest stone; each rock gives 3 - 5).
  - Everything else is the same game: day and night, enemies at night, chests, crafting, building, armor, the
    New Game options (days to win, difficulty...), dying and coming back at the start.
- **Every game is a different world.** Play Again makes a new one too.

## Quick way: three clicks

1. Menu **Tools > World > Build New World Scene**, then **Build**. It takes a few seconds.
2. Menu **Tools > UI > Build Main Title Scene** (to get the Map row on the NEW GAME page; your Version text is kept).
3. Open `Assets/Scenes/MainTitleScene.unity`, press Play, **NEW GAME > Map: New world > START GAME**.

**Run step 1 again whenever GameScene changes** (new menus, player scripts, spawner settings): the new world's scene
is made from GameScene each time. Changes made by hand in ProceduralGenerateScene are lost then, so make them in
GameScene.

The tool:
- **Caves:** builds the three rock caves first (**Tools > World > Build Rock Caves**, `Assets/Prefabs/World/Caves`).
  They're made from the rock shapes of **Tools > World > Use Low Poly Rocks**, so run that once before.
- **The scene:** copies GameScene to `Assets/Scenes/ProceduralGenerateScene.unity`, with everything the game needs
  (the player, camera, sun, HUD and menus, GameRules, key bindings, the enemy, deer and tree spawners, Marko's
  fireball), and removes GameScene's hand-made map: its terrains, the trees, rocks, ores, chests, apples and deer
  placed by hand, building pieces, and its walls and markers (`Boundaries`, `Spawn`, `Ore Testing`).
- **World Generator:** adds it, filled in: the old terrain's ground textures (same order) and material, the caves,
  the Stone / Copper / Iron rocks, and the grass picture.
  - The textures come from GameScene's **painted** terrain: the `Terrain (1)` inside Sanyam's `WorldMap` (Grass, Sand,
    Snow, Rock, Mud). GameScene's top-level `Terrain` has no textures at all; the first version of the tool took that
    one, so the new world came out as a white checkerboard with no trees and no deer.
  - The grass picture is Polytope Studio's grass blades (`PT_Grass_01`, from the pack imported with the armors),
    copied once to `Assets/Effects/Grass/GrassBlades.png` with clamped edges (Polytope's own file isn't changed).
- **ObjectScatter** (Sanyam's spawner, his script isn't changed): its rules stay as in GameScene (trees, berry bushes,
  forest and desert rocks, chests), with one more in this scene, **Hill Rocks** (60 stone rocks on the hills).
- **Sky light:** saves it for the new scene (like **Tools > World > Set Up Sky Lighting**), so shadows aren't pitch
  black when the title screen opens it.
- **Build Settings:** adds the scene after GameScene, so the title screen can load it.
- **Grass in builds:** adds Unity's terrain grass shaders to **Edit > Project Settings > Graphics > Always Included
  Shaders** (the grass is made while playing, so no saved scene uses them and a built game would leave them out).

## How it works

| Part | Where | What it does |
|---|---|---|
| **WorldGenerator** | ProceduralGenerateScene > World Generator | Makes the world while the scene loads, before anything else starts (it wakes up first): the terrain's shape and textures, the grass, the start clearing, the caves and their ores, the invisible edge walls. It moves the player to the start (also their **Respawn Point**) and gives ObjectScatter the new terrain. The same **Seed** always makes the same world. |
| **Rock caves** | `Assets/Prefabs/World/Caves` | Rock Cave Small / Rock Cave / Rock Cave Big: a ring of rocks with a gap at the front, a rock either side of the gap and one across the top, flat rocks as a roof, a few rocks around the outside, and empty `OreSpot` markers on the floor. Every rock has a collider of its own shape. |
| **ObjectScatter** | ProceduralGenerateScene > Scatter | Sanyam's: spawns the trees, bushes, rocks and chests on the painted textures when the game starts (trees on grass, desert rocks on sand, hill rocks on rock). |
| **AnimalSpawner** | ProceduralGenerateScene > AnimalSpawner - DeerNode | Keeps deer around the player on the grass, out of sight (as in GameScene). |
| **NewGamePanel**, **GameSetup**, **TitleMenu** | the title screen | Map: New world sets `GameSetup.NewWorld`, and START GAME loads TitleMenu's **New World Scene** (`ProceduralGenerateScene`) instead of **Game Scene**. |

**The world's layout** (sizes in meters, the middle of the world at 0, 0):
- **Grassland:** within about 280 m of the middle (its edge wobbles). Height 24 - 40 m, slopes mostly under 10 degrees.
  Painted grass, with patches of mud (fewer trees there). **Grass tufts** grow wherever the ground is painted mostly
  grass (also on the hills' green lower slopes): up to 6 per 2 x 2 m, thinner at the edges of the grass and in
  thinner patches, none on mud, sand, rock or snow. They're Unity's own terrain grass: they wave in the wind and are
  only drawn near the camera (80 m).
- **Desert:** a 140 degree wedge around the grassland, low dunes, painted sand.
- **Rocky hills:** the rest, up to about 100 m high: grass on their low slopes, rock higher up, snow above 92 m.
  About one slope in eight is too steep to climb (steeper than 45 degrees).
- **Bare rock** on every slope steeper than about 35 degrees, wherever it is.
- **Edge mountains:** the last 70 m before the edge rise to 133 m, too steep to climb.
- **Caves:** the ground under each is flattened (13 m around) and painted mud and rock, so no tree grows into them.
- **The start:** a flat clearing 6 m around, half grass and half mud (no tree spawns on the player).

## Settings (World Generator, in ProceduralGenerateScene)

| Setting | What it does |
|---|---|
| **Seed** | 0 = a new random world every game. Any other number = that world every time. The Console says each game's seed ("World Generator: seed 123456..."), so a world you liked can be made again: type its seed here. |
| **World Size**, **Max Height** | 1000 m square; the highest ground (the edge mountains) |
| **Height Resolution**, **Texture Resolution** | 513 / 512 = a height and a texture color every 2 m (low-poly look, quick to make) |
| **Grassland Radius**, **Desert Width**, **Snow Line** | how big the grassland is, how much of the land around it is desert (degrees), where the snow starts |
| **Layers** + **Grass / Sand / Rock / Snow / Mud Layer** | the ground textures (filled in by the tool from GameScene's terrain) and which is which |
| **Cave Prefabs**, **Cave Count**, **Cave Min / Max Distance**, **Cave Spacing**, **Cave Radius** | which caves, how many, how far from the start, how far apart, the flat ground made for each |
| **Stone / Copper / Iron Ore**, **Iron / Copper Per Cave** | the rocks put in the caves, and how many iron and copper rocks each cave gets (the other spots get stone) |
| **Grass Texture** | the grass blades picture (empty = no grass) |
| **Grass Density** | tufts per 2 x 2 m of full grass (6). More = thicker, but more to draw |
| **Grass Width / Height** | a tuft's size in meters (0.6 - 1.1 wide, 0.45 - 0.85 high) |
| **Grass Healthy / Dry** | the two colors mixed across the ground |
| **Grass Distance** | how far from the camera the grass is drawn (80 m) |

Changing them: open ProceduralGenerateScene, select **World Generator**, change, save. (Running the tool again resets
them to the defaults, because the scene is made again from GameScene.)

## The caves by hand

Open a cave prefab in `Assets/Prefabs/World/Caves` to move its rocks (then don't run Build Rock Caves again, it would
rebuild them). Each needs:
- rocks with a **Mesh Collider** (the walls and roof must be solid), a gap big enough to walk in;
- empty children named **OreSpot ...** on the floor (at height 0) where the ores go.

A new cave: build one like that, save it as a prefab, and drag it into the World Generator's **Cave Prefabs**.

## The new world by hand

1. In the Project window, select `Assets/Scenes/GameScene.unity`, **Ctrl + D**, rename the copy
   **ProceduralGenerateScene**, open it.
2. Delete the hand-made map: **Terrain**, **WorldMap**, **Boundaries**, **Spawn**, **Ore Testing**, **OresSpawnCave**,
   and every tree, rock, ore, chest, apple, deer and building piece at the top of the Hierarchy. Keep the Player,
   Main Camera, Directional Light, the canvases, GameRules, KeyBindings, EventSystem, EnemySpawner, AnimalSpawner,
   Scatter and **FireballProjectiie** (Marko's fireball uses it).
3. **Create Empty**, name it **World Generator**, **Add Component > WorldGenerator**. Fill in **Layers** with the old
   terrain's textures in the same order (Grass, Sand, Rock, Snow, Mud: check the old terrain's Paint Texture list) and
   set the five layer numbers to match. Drag the caves into **Cave Prefabs** and StoneNode, CopperNode and IronNode
   into the ores.
4. **Scatter > Object Scatter > Terrain**: leave it empty (the World Generator fills it in). Optional: add a rule for
   the hills (Terrain Layer = the rock layer's number).
5. **AnimalSpawner > Terrain**: empty.
6. **Tools > World > Set Up Sky Lighting**, then **File > Build Settings > Add Open Scenes**.

## Testing

1. Open ProceduralGenerateScene and press Play: the Console says "World Generator: seed ..., 6 caves". The player
   stands in a clearing in green grassland with grass tufts around; trees, bushes and rocks appear, and deer show up
   after a moment (out of sight first, so look around).
2. Walk out: a desert on one side, rocky hills elsewhere, mountains at the edge that can't be climbed.
3. Find a cave (60 - 230 m from the start, a rocky mound): walk in, mine the stone, copper and iron rocks.
4. Stop, Play again: a different world.
5. From the title screen: **NEW GAME > Map: New world > START GAME**. **Play Again** on the victory screen makes a
   new world; dying brings the player back to the start clearing.
6. **Map: Old world** still starts GameScene as before.

## Problems

| Problem | Why / fix |
|---|---|
| New world starts the old world | ProceduralGenerateScene isn't in Build Settings (the Console says so): run **Tools > World > Build New World Scene**. |
| The ground is a white checkerboard, no trees, no deer | The World Generator has no ground textures (its **Layers** list is empty; the Console warns "no ground textures"). Run **Tools > World > Build New World Scene** again: it now takes the textures from GameScene's painted terrain. |
| No grass tufts | **Grass Texture** is empty (the Polytope pack isn't in the project), or **Grass Density** is 0, or Quality settings draw no terrain details. |
| No Map row on the NEW GAME page | Rebuild the title screen: **Tools > UI > Build Main Title Scene**. |
| No caves (the Console says "0 caves") | The World Generator has no Cave Prefabs: run **Tools > World > Use Low Poly Rocks** (the rock shapes), then Build New World Scene again. Or the grassland had no gentle spot far enough from the start (rare: try another game). |
| No trees, bushes or rocks | The World Generator has no ground textures (the Console warns): GameScene's terrain must have its textures painted. Run the tool again. |
| No deer | AnimalSpawner > Terrain Layer must be the grass texture's number (0 as in GameScene). |
| Pitch-black shadows from the title screen | The new scene's sky light isn't saved: open it and run **Tools > World > Set Up Sky Lighting** (the tool does it by itself). |
| Something added to GameScene is missing in the new world | The new world is a copy made by the tool: run **Build New World Scene** again. |
| Always the same world | World Generator > **Seed** isn't 0. |
| A cave has a gap you can see through | The rocks are random shapes: open the cave prefab and move or scale a rock to cover it. |

Made from Sanyam's world pieces: his **ObjectScatter** fills the new world (unchanged; only the new scene's copy gets
the Hill Rocks rule). His old `Assets/Map/Cave.prefab` isn't used: the new caves are made of the low-poly rocks.
