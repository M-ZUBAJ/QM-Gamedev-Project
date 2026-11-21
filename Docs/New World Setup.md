# New World Setup (a new random world every game)

**What the player sees:**
- On the title screen's **NEW GAME** page, the WORLD section starts with **Map**: **Old world** (the hand-made map,
  GameScene, as before) or **New world** (a new random world every game). START GAME loads the one picked.
- **The new world** is 1000 x 1000 m:
  - The player starts in a small clearing in the middle of **green grassland**: gentle hills painted green, with
    upright tufts of grass swaying gently in the wind, trees and berry bushes, and deer herds wandering around (they
    flee when you come close).
  - Further out: a sandy **desert** on one side, and **rocky hills** with snowy tops all around the rest. Some hill
    slopes are too steep to climb.
  - **The desert** (guide `Docs/Desert Setup.md`): hard **sandstone rocks** (they give Stone but take 2.5 times as
    long to break), and up to 3 **sandstone caves** with **Gold** rocks inside (an Iron Pickaxe or better). Since
    21/11/2026 no day monster: the turtles come at night from night 2, in both worlds (`Docs/Night Monsters Setup.md`).
  - **A glowing barrier** all round the edge (no mountains): a see-through force field with sliding stripes. It shows
    faintly as you come near, lights up and ripples where you touch it, and nobody gets through. The land goes on a
    little behind it, fading into the mist. Guide: `Docs/World Barrier Setup.md`.
  - **A 100 m view distance:** the world fades into a mist 100 m away (orange at sunrise and sunset, dark blue at night),
    and nothing further is drawn, so the game runs faster, most of all in the web build. Guide:
    `Docs/View Distance Setup.md`.
  - **Rock caves** in the grassland (6, between 60 and 230 m from the start): a walk-in mound of rocks with a roof,
    dim inside, with **Stone, Copper and Iron** rocks to mine (2 - 3 iron rocks and 2 - 3 copper rocks per cave, the
    rest stone; each rock gives 3 - 5).
  - Everything else is the same game: day and night, enemies at night (they burn away into orange embers when day
    comes), chests, crafting, building, armor, the New Game options (days to win, difficulty...), dying and coming back
    at the start.
- **Every game is a different world.** Play Again makes a new one too.

## Quick way: three clicks

1. Menu **Tools > World > Build New World Scene**, then **Build**. It takes a few seconds.
2. Menu **Tools > UI > Build Main Title Scene** (to get the Map row on the NEW GAME page; your Version text is kept).
3. Open `Assets/Scenes/MainTitleScene.unity`, press Play, **NEW GAME > Map: New world > START GAME**.

**Run step 1 again whenever GameScene changes** (new menus, player scripts, spawner settings) and after updates to
the new world (like the barrier, the view distance and the grass size): the new world's scene is made from GameScene
each time. Changes made by hand in ProceduralGenerateScene are lost then, so make them in GameScene.

The old world gets its barrier from **Tools > World > Add World Barrier** (run it once with GameScene open, then save).

The tool:
- **Caves:** builds the three rock caves first (**Tools > World > Build Rock Caves**, `Assets/Prefabs/World/Caves`).
  They're made from the rock shapes of **Tools > World > Use Low Poly Rocks**, so run that once before.
- **The scene:** copies GameScene to `Assets/Scenes/ProceduralGenerateScene.unity`, with everything the game needs
  (the player, camera, sun, HUD and menus, GameRules, key bindings, the enemy, deer and tree spawners, Marko's
  fireball), and removes GameScene's hand-made map: its terrains, the trees, rocks, ores, chests, apples and deer
  placed by hand, building pieces, its walls and markers (`Boundaries`, `Spawn`, `Ore Testing`) and its
  `World Barrier` (the World Generator makes the new world's own).
- **World Generator:** adds it, filled in: the old terrain's ground textures (same order) and material, the caves,
  the Stone / Copper / Iron rocks, and the grass picture.
  - The textures come from GameScene's **painted** terrain: the `Terrain (1)` inside Sanyam's `WorldMap` (Grass, Sand,
    Snow, Rock, Mud). GameScene's top-level `Terrain` has no textures at all; the first version of the tool took that
    one, so the new world came out as a white checkerboard with no trees and no deer.
  - The grass picture is Polytope Studio's grass blades (`PT_Grass_01`, from the pack imported with the armors),
    copied once to `Assets/Effects/Grass/GrassBlades.png` with clamped edges (Polytope's own file isn't changed).
- **View distance:** puts **ViewDistance** on the **Main Camera** (100 m) and saves the scene with its **fog on**
  (Linear, 40 - 100 m). A built game keeps fog only if a scene in it has fog on (GameScene's is off), and the mist is
  that fog.
- **ObjectScatter** (Sanyam's spawner): its rules stay as in GameScene (trees, berry bushes, forest rocks, chests),
  minus his Desert Rocks (no stones in the new world's desert), plus one more in this scene, **Hill Rocks** (60 stone
  rocks on the hills).
- **Sky light:** saves it for the new scene (like **Tools > World > Set Up Sky Lighting**), so shadows aren't pitch
  black when the title screen opens it.
- **Build Settings:** adds the scene after GameScene, so the title screen can load it.
- **Grass in builds:** adds Unity's terrain grass shaders to **Edit > Project Settings > Graphics > Always Included
  Shaders** (the grass is made while playing, so no saved scene uses them and a built game would leave them out).

## How it works

| Part | Where | What it does |
|---|---|---|
| **WorldGenerator** | ProceduralGenerateScene > World Generator | Makes the world while the scene loads, before anything else starts (it wakes up first): the terrain's shape and textures, the grass, the start clearing, the caves and their ores, and the **World Barrier** around it all. It moves the player to the start (also their **Respawn Point**) and gives ObjectScatter the new terrain. The same **Seed** always makes the same world. **New World On Death** (on, 21/11/2026): when the player dies, the death screen's **Respawn** makes a brand-new world: the scene loads again behind the title card "DAY 1 - A new world", so it's Day 1 with a new seed (unless Seed is set) and everything fresh (inventory, armor, perks, coins), like a new game; the New Game page's choices stay. Off = come back at the start of the same world, like the old world. |
| **WorldBarrier** | World Barrier (made while playing) | The glowing edge of the world, with solid walls just outside it. Spawners and deer ask it whether a spot is inside. |
| **ViewDistance** | ProceduralGenerateScene > Main Camera | How far the player can see (100 m): the camera stops drawing there, a mist hides the edge, the grass is drawn only that far, and the player's sight (for spawning deer out of view) ends there too. |
| **Rock caves** | `Assets/Prefabs/World/Caves` | Rock Cave Small / Rock Cave / Rock Cave Big: a ring of rocks with a gap at the front, a rock either side of the gap and one across the top, flat rocks as a roof, a few rocks around the outside, and empty `OreSpot` markers on the floor. Every rock has a collider of its own shape. |
| **ObjectScatter** | ProceduralGenerateScene > Scatter | Sanyam's: spawns the trees, bushes, rocks and chests on the painted textures when the game starts (trees and forest rocks on grass, chests on grass, hill rocks on rock), never outside the barrier. No stones in the desert: the tool leaves the rules that put stone rocks on sand (his Desert Rocks) out of this scene; GameScene keeps them. |
| **AnimalSpawner** | ProceduralGenerateScene > AnimalSpawner - DeerNode | Keeps deer around the player on the grass, out of sight (as in GameScene), inside the barrier. |
| **NewGamePanel**, **GameSetup**, **TitleMenu** | the title screen | Map: New world sets `GameSetup.NewWorld`, and START GAME loads TitleMenu's **New World Scene** (`ProceduralGenerateScene`) instead of **Game Scene**. |

**The world's layout** (sizes in meters, the middle of the world at 0, 0):
- **Grassland:** within about 280 m of the middle (its edge wobbles). Height 24 - 40 m, slopes mostly under 10 degrees.
  Painted grass, with patches of mud (fewer trees there). **Grass tufts** grow wherever the ground is painted mostly
  grass (also on the hills' green lower slopes): up to 6 per 2 x 2 m, thinner at the edges of the grass and in
  thinner patches, none on mud, sand, rock or snow. They're Unity's own terrain grass, 0.45 - 0.7 m wide and 0.55 -
  0.9 m tall (narrower than tall, so the blades stand up), swaying gently, drawn only near the camera (up to the view
  distance, 80 m: Grass Distance).
- **Desert:** a 140 degree wedge around the grassland, low dunes, painted sand.
- **Rocky hills:** the rest, up to about 100 m high: grass on their low slopes, rock higher up, snow above 92 m.
  About one slope in eight is too steep to climb (steeper than 45 degrees).
- **Bare rock** on every slope steeper than about 35 degrees, except in the desert (its dunes stay sand).
- **The barrier:** 500 m from the middle on every side (a 1000 x 1000 m square). No mountains at the edge any more.
- **Past the barrier:** 60 m more land on every side (at least the view distance), never reached: bare rock and mud (snow up high), so no trees,
  bushes, chests or deer grow there. Through the barrier it fades into the mist instead of ending in an edge.
- **Caves:** the ground under each is flattened (13 m around) and painted mud and rock, so no tree grows into them.
- **The start:** a flat clearing 6 m around, half grass and half mud (no tree spawns on the player).

## Settings (World Generator, in ProceduralGenerateScene)

| Setting | What it does |
|---|---|
| **Seed** | 0 = a new random world every game. Any other number = that world every time. The Console says each game's seed ("World Generator: seed 123456..."), so a world you liked can be made again: type its seed here. |
| **World Size** | the square inside the barrier (1000 m) |
| **Max Height** | the highest the ground can go (140 m; the hills' tops reach about 100 m) |
| **Land Beyond Barrier** | how far the land goes on past the barrier (60 m, or the Main Camera's View Distance when that's further, up to 250 m: with a 100 m view the land goes on 100 m, so it never visibly ends before the mist), bare, never reached |
| **Height Resolution**, **Texture Resolution** | 513 / 512 = a height and a texture color about every 2 m (low-poly look, quick to make) |
| **Grassland Radius**, **Desert Width**, **Snow Line** | how big the grassland is, how much of the land around it is desert (degrees), where the snow starts |
| **Layers** + **Grass / Sand / Rock / Snow / Mud Layer** | the ground textures (filled in by the tool from GameScene's terrain) and which is which |
| **Desert Cave Prefabs**, **Desert Cave Count** (3), **Gold Ore**, **Sandstone Ore**, **Gold Per Cave** (2 - 3) | the desert's sandstone caves (anywhere deep in the desert), and their rocks: gold first, the rest sandstone (`Docs/Desert Setup.md`) |
| **Cave Prefabs**, **Cave Count**, **Cave Min / Max Distance**, **Cave Spacing**, **Cave Radius** | which caves, how many, how far from the start, how far apart, the flat ground made for each |
| **Stone / Copper / Iron Ore**, **Iron / Copper Per Cave** | the rocks put in the caves, and how many iron and copper rocks each cave gets (the other spots get stone) |
| **Grass Texture** | the grass blades picture (empty = no grass) |
| **Grass Density** | tufts per 2 x 2 m of full grass (6). More = thicker, but more to draw |
| **Grass Width / Height** | a tuft's size in meters (0.45 - 0.7 wide, 0.55 - 0.9 high). Keep the width below the height: the picture is square, so a tuft wider than tall squashes its blades flat, as if bent over (the first version had 0.6 - 1.1 wide and 0.45 - 0.85 high) |
| **Grass Bending** | how far the wind bends the grass (0.2; Unity's own is 0.5, a strong wind) |
| **Grass Wind Speed** | how fast the wind's waves move over the grass (0.4) |
| **Grass Healthy / Dry** | the two colors mixed across the ground |
| **Grass Distance** | how far from the camera the grass is drawn (80 m, but never past the camera's View Distance) |

Changing them: open ProceduralGenerateScene, select **World Generator**, change, save. (Running the tool again resets
them to the defaults, because the scene is made again from GameScene.) The view distance is on the **Main Camera**
(**View Distance > Distance**), the barrier's look on the **World Barrier** while playing (see their guides).

## The caves by hand

Open a cave prefab in `Assets/Prefabs/World/Caves` to move its rocks (then don't run Build Rock Caves again, it would
rebuild them). Each needs:
- rocks with a **Mesh Collider** (the walls and roof must be solid), a gap big enough to walk in;
- empty children named **OreSpot ...** on the floor (at height 0) where the ores go.

A new cave: build one like that, save it as a prefab, and drag it into the World Generator's **Cave Prefabs**.

## The new world by hand

1. In the Project window, select `Assets/Scenes/GameScene.unity`, **Ctrl + D**, rename the copy
   **ProceduralGenerateScene**, open it.
2. Delete the hand-made map: **Terrain**, **WorldMap**, **Boundaries**, **World Barrier** (if there is one), **Spawn**,
   **Ore Testing**, **OresSpawnCave**, and every tree, rock, ore, chest, apple, deer and building piece at the top of the
   Hierarchy. Keep the Player, Main Camera, Directional Light, the canvases, GameRules, KeyBindings, EventSystem,
   EnemySpawner, AnimalSpawner, Scatter and **FireballProjectiie** (Marko's fireball uses it).
3. **Create Empty**, name it **World Generator**, **Add Component > WorldGenerator**. Fill in **Layers** with the old
   terrain's textures in the same order (Grass, Sand, Rock, Snow, Mud: check the old terrain's Paint Texture list) and
   set the five layer numbers to match. Drag the caves into **Cave Prefabs** and StoneNode, CopperNode and IronNode
   into the ores.
4. **Scatter > Object Scatter > Terrain**: leave it empty (the World Generator fills it in). Optional: add a rule for
   the hills (Terrain Layer = the rock layer's number).
5. **AnimalSpawner > Terrain**: empty.
6. **Main Camera > Add Component > ViewDistance**. Then **Window > Rendering > Lighting > Environment > Other
   Settings**: tick **Fog**, **Mode** Linear, **Start** 40, **End** 100.
7. **Tools > World > Set Up Sky Lighting**, then **File > Build Settings > Add Open Scenes**.

## Testing

1. Open ProceduralGenerateScene and press Play: the Console says "World Generator: seed ..., 6 caves". The player
   stands in a clearing in green grassland with upright grass tufts around, the world fading into a mist about 100 m
   away; trees, bushes and rocks appear, and deer show up after a moment (out of sight first, so look around).
2. Walk out: a desert on one side, rocky hills elsewhere. At the edge (500 m from the middle) a striped barrier shows
   up as you come near, lights up where you are, and stops you. Deer never cross it.
3. Find a cave (60 - 230 m from the start, a rocky mound): walk in, mine the stone, copper and iron rocks. In the
   desert: sandstone caves with gold (Iron Pickaxe) and sandstone rocks (`Docs/Desert Setup.md`); turtles come at night
   from night 2, everywhere (`Docs/Night Monsters Setup.md`).
4. Stop, Play again: a different world.
5. From the title screen: **NEW GAME > Map: New world > START GAME**. **Play Again** on the victory screen makes a
   new world; dying brings the player back to the start clearing.
6. **Map: Old world** still starts GameScene as before (with its own barrier once Add World Barrier has been run).

## Problems

| Problem | Why / fix |
|---|---|
| New world starts the old world | ProceduralGenerateScene isn't in Build Settings (the Console says so): run **Tools > World > Build New World Scene**. |
| The ground is a white checkerboard, no trees, no deer | The World Generator has no ground textures (its **Layers** list is empty; the Console warns "no ground textures"). Run **Tools > World > Build New World Scene** again: it now takes the textures from GameScene's painted terrain. |
| The grass looks bent over or squashed flat | The scene still has the first version's tuft sizes (wider than tall): run **Build New World Scene** again, or set **Grass Width** below **Grass Height** (and **Grass Bending** low). |
| No grass tufts | **Grass Texture** is empty (the Polytope pack isn't in the project), or **Grass Density** is 0, or Quality settings draw no terrain details. |
| Mountains at the edge, no barrier | An old copy of the scene, or old scripts: run **Build New World Scene** again. |
| The mist starts too near / too far | **Main Camera > View Distance > Distance** (and **Clear Share**). See `Docs/View Distance Setup.md`. |
| No mist in a built game (the far world just ends) | The scene was saved with fog off: run **Build New World Scene** again (it saves the fog on). |
| No Map row on the NEW GAME page | Rebuild the title screen: **Tools > UI > Build Main Title Scene**. |
| No caves (the Console says "0 caves") | The World Generator has no Cave Prefabs: run **Tools > World > Use Low Poly Rocks** (the rock shapes), then Build New World Scene again. Or the grassland had no gentle spot far enough from the start (rare: try another game). |
| No trees, bushes or rocks | The World Generator has no ground textures (the Console warns): GameScene's terrain must have its textures painted. Run the tool again. |
| No deer | AnimalSpawner > Terrain Layer must be the grass texture's number (0 as in GameScene). |
| Pitch-black shadows from the title screen | The new scene's sky light isn't saved: open it and run **Tools > World > Set Up Sky Lighting** (the tool does it by itself). |
| Something added to GameScene is missing in the new world | The new world is a copy made by the tool: run **Build New World Scene** again. |
| Always the same world | World Generator > **Seed** isn't 0. |
| A cave has a gap you can see through | The rocks are random shapes: open the cave prefab and move or scale a rock to cover it. |

Made from Sanyam's world pieces: his **ObjectScatter** fills the new world (the new scene's copy gets the Hill Rocks
rule and loses Desert Rocks; his script got one `[Tony]` line so nothing spawns outside a barrier, log
`Docs/TonyEditMate/Sanyam/20-11-2026 Nothing Spawns Outside The Barrier.md`). His old `Assets/Map/Cave.prefab` isn't
used: the new caves are made of the low-poly rocks.
