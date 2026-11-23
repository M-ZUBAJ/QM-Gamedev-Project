# New World Setup (a new random, endless world every game)

**What the player sees:**
- On the title screen's **NEW GAME** page, the WORLD section starts with **Map**: **Old world** (the hand-made map,
  GameScene, as before) or **New world** (a new random world every game). START GAME loads the one picked.
- **The new world never ends** (since 21/11/2026): the land is made in squares (256 x 256 m "chunks") while you walk,
  in every direction. There's no barrier and no edge.
  - The player starts in a small clearing in **green grassland**: gentle hills painted green, with upright tufts of
    grass swaying in the wind, trees and berry bushes, deer herds (they flee when you come close) and **rock caves**
    with **Stone, Copper and Iron** rocks. Everything within about 250 m of the start is grassland.
  - Farther out the land changes: more grassland, **rocky hills** (some slopes too steep to climb; snowy tops above
    92 m where nothing grows), the sandy **desert** (never closer than 400 m to the start, usually about 450 - 500 m
    away) and the **snow** (never closer than 700 m, usually about 750 - 800 m away).
  - **The desert** (guide `Docs/Desert Setup.md`): hard **sandstone rocks** (they give Stone but take 2.5 times as
    long to break) and **sandstone caves** with **Gold** rocks (an Iron Pickaxe or better) and **Iron** rocks, and its
    chests.
  - **The snow** (guide `Docs/Snow Setup.md`): the best place. Snowy pines and spruces, frosty rocks, **snow caves**
    with **Diamond** rocks (a Gold Pickaxe or better; diamonds make the Diamond tools) and **Gold** rocks, more caves,
    and **more and better chests** than the desert.
  - **Lucky caves:** 1 desert cave in 20 has 4 gold rocks, 1 snow cave in 20 has 4 diamond rocks.
  - **Places you leave stay as you left them:** a chopped tree, a mined rock or an opened chest doesn't come back when
    you return. (Far squares are hidden, not thrown away.)
  - **A 100 m view distance:** the world fades into a mist 100 m away (orange at sunrise and sunset, dark blue at night),
    and nothing further is drawn, so the game runs faster, most of all in the web build. Guide:
    `Docs/View Distance Setup.md`.
  - Everything else is the same game: day and night, monsters at night (slimes, turtles from night 2; they burn away
    into orange embers when day comes), chests, crafting, building, armor, the New Game options.
- **Every game is a different world.** Play Again makes a new one too, and so does dying (New World On Death).

## Quick way: three clicks

1. Menu **Tools > World > Build New World Scene**, then **Build**. It takes a few seconds.
2. Menu **Tools > UI > Build Main Title Scene** (to get the Map row on the NEW GAME page; your Version text is kept).
3. Open `Assets/Scenes/MainTitleScene.unity`, press Play, **NEW GAME > Map: New world > START GAME**.

**Run step 1 again whenever GameScene changes** (new menus, player scripts, spawner settings) and after updates to
the new world: the new world's scene is made from GameScene each time. Changes made by hand in ProceduralGenerateScene
are lost then, so make them in GameScene.

Before step 1, once: **Tools > World > Use Low Poly Rocks** (the rock shapes for the caves), **Tools > World > Use Low
Poly Grassland Trees** (the trees the snow's pines are made from), **Tools > Items > Build Tier Tools** (the tools) and
**Tools > World > Build Loot Chests** (the rare, epic and legendary boxes the snow copies). They're already done in this
project.

The tool:
- **Caves and biome content:** builds the three rock caves (**Tools > World > Build Rock Caves**), the desert's content
  (**Tools > World > Build Desert Content**: Gold, GoldNode, SandstoneNode, the sandstone caves, the turtle) and the
  snow's content (**Tools > World > Build Snow Content**: Diamond, DiamondNode, StoneNode Snow, the snow caves, the snowy
  conifers, the Diamond tools' recipes).
- **The scene:** copies GameScene to `Assets/Scenes/ProceduralGenerateScene.unity`, with everything the game needs
  (the player, camera, sun, HUD and menus, GameRules, key bindings, the enemy and deer spawners, the tree spawner,
  Marko's fireball), and removes GameScene's hand-made map: its terrains, the trees, rocks, ores, chests, apples and deer
  placed by hand, building pieces, its walls and markers (`Boundaries`, `Spawn`, `Ore Testing`) and its `World Barrier`
  (the new world has none).
- **World Generator:** adds it, filled in: the old terrain's ground textures (same order) and material, the caves of
  all three biomes, their rocks (Stone / Copper / Iron, Gold / Sandstone, Diamond), and the grass picture.
  - The textures come from GameScene's **painted** terrain: the `Terrain (1)` inside Sanyam's `WorldMap` (Grass, Sand,
    Snow, Rock, Mud). GameScene's top-level `Terrain` has no textures at all.
  - The grass picture is Polytope Studio's grass blades (`PT_Grass_01`), copied once to
    `Assets/Effects/Grass/GrassBlades.png` with clamped edges and **Read/Write** on (Polytope's own file isn't
    changed). The grass is made while the game runs, and Unity then reads the picture's pixels to pack it into its grass
    sheet: a built game (the web build) can only do that with Read/Write on. Without it the grass shows as **black
    squares** in the built game while it looks right in the Editor (23/11/2026). The World Generator warns in the
    Console when its grass picture isn't readable.
- **View distance:** puts **ViewDistance** on the **Main Camera** (100 m) and saves the scene with its **fog on**
  (Linear, 40 - 100 m). A built game keeps fog only if a scene in it has fog on (GameScene's is off), and the mist is
  that fog.
- **ObjectScatter** (Sanyam's spawner): its rules stay as in GameScene (trees, berry bushes, forest rocks, chests),
  minus his Desert Rocks (no plain stones in the desert), plus, in this scene only: **Hill Rocks** (stone rocks on the
  hills' rock), **Sandstone Rocks** (the desert), and the snow's rules: **ChestsSnow** 40, **Rare Chests Snow** 20,
  **Epic Chests Snow** 8, **Legendary Chests Snow** 4 (copies of the desert's chest rules on the snow texture; the
  desert has 30, 12, 4 and 2), **Trees: Snow Pine / Tall Snow Pine / Snow Spruce / Tall Snow Spruce** (120 each) and
  **Snow Rocks** 40.
- **Sky light:** saves it for the new scene (like **Tools > World > Set Up Sky Lighting**), so shadows aren't pitch
  black when the title screen opens it.
- **Build Settings:** adds the scene after GameScene, so the title screen can load it.
- **Grass in builds:** adds Unity's terrain grass shaders to **Edit > Project Settings > Graphics > Always Included
  Shaders** (the grass is made while playing, so no saved scene uses them and a built game would leave them out).
- **Chunk template** (23/11/2026): makes `Assets/Prefabs/World/New World Chunk Template.asset`, a small empty terrain
  made in the Editor with the grass on it, and gives it to the World Generator (**Chunk Template**). Every chunk's
  terrain starts as a copy of it. That's the workaround for a Unity 2023.1 bug (UUM-25114, fixed only in later Unity
  versions): a terrain made from nothing while the game runs draws its grass with the wrong shader in a built game,
  so the grass showed as **green squares** in the web build (in the Editor it looks right). For an existing
  ProceduralGenerateScene, **Tools > World > Fix Grass In Builds** does only this step.

## How it works

| Part | Where | What it does |
|---|---|---|
| **WorldGenerator** | ProceduralGenerateScene > World Generator | Wakes up first and makes the squares of land (chunks) around the start before anything else starts, moves the player to the start (also their **Respawn Point**), then keeps making chunks ahead of the player while they walk, a few milliseconds of each frame so the game doesn't stop. Each chunk: its ground's shape and painted textures, grass tufts, maybe a cave or two with their rocks, and its trees, bushes, rocks and chests from ObjectScatter's rules. Near chunks are shown (their trees, rocks and chests wake up a small group per frame, so showing one doesn't stop the game); far ones are hidden (their ground stays solid, everything in them stays as it was). Farther than **Content Distance** (500 m, 22/11/2026) a chunk lets go of its trees, bushes, rocks, chests and caves to save memory: it remembers every one and which ones you used up (chopped, mined, picked, opened), and when you come back within Load Distance the rest are made again, the same, a few each frame (its ground always stays). A new chunk's ground is built over a few frames (heights, textures, grass, then the terrain). The same **Seed** always makes the same world. **New World On Death** (on): when the player dies, the death screen's **Respawn** makes a brand-new world ("DAY 1 - A new world"): Day 1, a new seed, everything fresh, like a new game; the New Game page's choices stay. Off = come back at the start of the same world. |
| **ObjectScatter** | ProceduralGenerateScene > Scatter | Sanyam's: in GameScene it spawns everything once when the game starts. Here the World Generator switches it off and uses its **Rules** for every chunk: each rule's **Count** is how many grow on **Rules Area** (400,000 m²) of ground painted with its texture, so a chunk that's half grass gets half the trees of an all-grass one. Spacing, patches (forests), sizes and slopes work as in his script. Rules on the snow texture grow only in the snow biome (not on the hills' snowy tops). |
| **ViewDistance** | ProceduralGenerateScene > Main Camera | How far the player can see (100 m): the camera stops drawing there, a mist hides the edge, the grass is drawn only that far, and the player's sight (for spawning deer out of view) ends there too. Chunks are shown up to 60 m past it. |
| **Caves** | `Assets/Prefabs/World/Caves` | Rock Cave Small / Rock Cave / Rock Cave Big (grassland), Desert Cave Small / Desert Cave (sandstone), Snow Cave Small / Snow Cave / Snow Cave Big (frosty rock): a ring of rocks with a gap at the front, a rock either side of the gap and one across the top, flat rocks as a roof, a few rocks around the outside, and empty `OreSpot` markers on the floor. Every rock has a collider of its own shape. |
| **AnimalSpawner** | ProceduralGenerateScene > AnimalSpawner - DeerNode | Keeps deer around the player on the grass, out of sight (as in GameScene), on whichever chunk is under them. |
| **EnemySpawner** + **NightMonsters** | ProceduralGenerateScene > EnemySpawner | Marko's night spawner with Tony's NightMonsters (`Docs/Night Monsters Setup.md`): monsters come out of sight around the player, wherever they are. |
| **NewGamePanel**, **GameSetup**, **TitleMenu** | the title screen | Map: New world sets `GameSetup.NewWorld`, and START GAME loads TitleMenu's **New World Scene** (`ProceduralGenerateScene`) instead of **Game Scene**. |

**The world's layout** (meters; the start is near 0, 0):
- **Chunks:** 256 x 256 m squares; chunk 0, 0 has the start in its middle. Each has a height about every 2 m (the
  low-poly look) and a painted texture spot about every 2 m. Neighboring chunks join without cracks.
- **Biomes:** two slow noises decide them. A "climate" makes hot areas (desert) and cold areas (snow) about 1100 m
  apart; a second noise makes rocky hills between the green grassland. The desert only from 400 m from the start on, the
  snow only from 700 m on, and everything within 250 m of the start is grassland (it fades into the rest over 120 m).
  Across a big area, about a quarter is desert and a quarter snow.
- **Grassland:** height 24 - 40 m, slopes mostly under 10 degrees. Painted grass, with patches of mud (fewer trees
  there). **Grass tufts** wherever the ground is mostly grass: up to 6 per 2 x 2 m, 0.45 - 0.7 m wide and 0.55 - 0.9 m
  tall, swaying gently, drawn only near the camera (Grass Distance, 80 m).
- **Desert:** low dunes, painted sand (even its steep dunes, so nothing meant for rock grows there).
- **Rocky hills:** up to about 100 m high: grass on their low slopes, rock higher up, snow above 92 m (the tree line:
  nothing grows up there). About one slope in eight is too steep to climb (steeper than 45 degrees).
- **Snow:** rolling snowy hills, about 40 - 65 m high, painted snow, easy to walk (slopes mostly under 17 degrees).
- **Bare rock** on every slope steeper than about 35 degrees, except in the desert (its dunes stay sand).
- **Caves:** each chunk gets 2 tries for a cave on gentle ground of one biome: grassland 45 %, desert 50 %, snow 70 % a
  try. The ground under a cave is flattened (13 m around) and painted so nothing grows into it. No cave within 50 m of
  the start.
- **How much grows** (a full 256 m square of one ground, with the scene's counts): grassland about 180 trees (in
  forest patches), 25 berry bushes, 3 common chests and 1 rare; desert about 5 common, 2 rare, a 2 in 3 chance of an epic
  and 1 in 3 of a legendary; snow about 80 snowy conifers, 6 to 7 common, 3 rare, 1 epic and a 2 in 3 chance of a
  legendary.
- **The start:** a flat clearing 6 m around, half grass and half mud (nothing spawns on the player).

## Settings (World Generator, in ProceduralGenerateScene)

| Setting | What it does |
|---|---|
| **Seed** | 0 = a new random world every game. Any other number = that world every time. The Console says each game's seed ("World Generator: seed 123456..."), so a world you liked can be made again: type its seed here. |
| **New World On Death** | on = dying makes a brand-new world; off = come back at the start |
| **Max Height** | the highest the ground can go (140 m; the hills' tops reach about 100 m) |
| **Chunk Size** | one square of land (256 m) |
| **Chunk Heights**, **Chunk Textures** | 129 / 128 = a height and a texture spot about every 2 m on a 256 m chunk (low-poly look, quick to make) |
| **Load Distance** | chunks this close to the player (to the chunk's nearest edge) are made ahead of time (300 m) |
| **Show Distance**, **Hide Distance** | chunks closer than Show (170 m) are drawn, chunks past Hide (240 m) are hidden. Both are at least the camera's View Distance + 60 / + 100 m |
| **Content Distance** | (22/11/2026) chunks farther than this (500 m) let go of their trees, bushes, rocks, chests and caves; what you used up there stays gone, the rest comes back when you return. At least Load Distance + half a chunk. 0 = keep everything (the old way: the game got slower the more land you walked) |
| **Frame Budget** | milliseconds of each frame spent making land (4). More = chunks ready sooner, but the game may stutter (keep it small for the web build) |
| **Start Radius** | everything this close to the start is grassland (250 m) |
| **Desert From**, **Snow From** | the desert / snow can only begin this far from the start (400 / 700 m) |
| **Climate Size**, **Hills Size** | about how big the hot / cold areas (1100 m) and the rocky hills' areas (700 m) are |
| **Snow Line** | snow covers the rocky hills above this height (92 m) |
| **Layers** + **Grass / Sand / Rock / Snow / Mud Layer** | the ground textures (filled in by the tool from GameScene's terrain) and which is which |
| **Terrain Material** | the old world's terrain material |
| **Grass Texture**, **Grass Density**, **Grass Width / Height**, **Grass Bending**, **Grass Wind Speed**, **Grass Healthy / Dry**, **Grass Distance** | the grass tufts: picture (empty = no grass), tufts per 2 x 2 m (6), size (keep the width below the height, or the blades look squashed flat), wind (0.2 / 0.4), colors, how far they're drawn (80 m, never past the View Distance) |
| **Cave Prefabs**, **Cave Chance**, **Cave Radius** | the grassland's caves, the chance of one per try (0.45), the flat ground made for each (13 m) |
| **Stone / Copper / Iron Ore**, **Iron / Copper Per Cave** | the grassland caves' rocks: 2 - 3 iron, 2 - 3 copper, the rest stone |
| **Desert Cave Prefabs**, **Desert Cave Chance**, **Gold Ore**, **Sandstone Ore**, **Gold Per Cave**, **Desert Iron Per Cave** | the desert's caves (0.5 a try): 2 - 3 gold, 2 - 3 iron, the rest sandstone (`Docs/Desert Setup.md`) |
| **Snow Cave Prefabs**, **Snow Cave Chance**, **Diamond Ore**, **Diamond Per Cave**, **Snow Gold Per Cave** | the snow's caves (0.7 a try): 2 - 3 diamond rocks, 2 - 3 gold, the rest stone (`Docs/Snow Setup.md`) |
| **Lucky Cave Chance**, **Lucky Count** | 1 desert / snow cave in 20 (0.05) gets 4 gold / 4 diamond rocks instead of Gold / Diamond Per Cave (a cave has 7 ore spots: the best rocks are put first) |
| **Scatter** | Sanyam's ObjectScatter (its Rules are used for every chunk) |
| **Rules Area** | a rule's Count is how many grow on this much ground of its texture (400,000 m², about 630 x 630 m, as thick as the first new world's grassland). Smaller = more of everything |
| **Player** | the player (found by itself if empty) |

Changing them: open ProceduralGenerateScene, select **World Generator**, change, save. (Running the tool again resets
them to the defaults, because the scene is made again from GameScene.) How many of each thing grows is in **Scatter >
Object Scatter > Rules** (each rule's **Count**). The view distance is on the **Main Camera** (**View Distance >
Distance**).

## The caves by hand

Open a cave prefab in `Assets/Prefabs/World/Caves` to move its rocks (then don't run the cave tools again, they would
rebuild them). Each needs:
- rocks with a **Mesh Collider** (the walls and roof must be solid), a gap big enough to walk in;
- empty children named **OreSpot ...** on the floor (at height 0) where the ores go.

A new cave: build one like that, save it as a prefab, and drag it into the World Generator's **Cave Prefabs** (or
**Desert Cave Prefabs**, **Snow Cave Prefabs**). Keep it within about 12 m of its middle: the flat ground made for a
cave is 13 m around.

## The new world by hand

1. In the Project window, select `Assets/Scenes/GameScene.unity`, **Ctrl + D**, rename the copy
   **ProceduralGenerateScene**, open it.
2. Delete the hand-made map: **Terrain**, **WorldMap**, **Boundaries**, **World Barrier** (if there is one), **Spawn**,
   **Ore Testing**, **OresSpawnCave**, and every tree, rock, ore, chest, apple, deer and building piece at the top of the
   Hierarchy. Keep the Player, Main Camera, Directional Light, the canvases, GameRules, KeyBindings, EventSystem,
   EnemySpawner, AnimalSpawner, Scatter and **FireballProjectiie** (Marko's fireball uses it).
3. **Create Empty**, name it **World Generator**, **Add Component > WorldGenerator**. Fill in **Layers** with the old
   terrain's textures in the same order (Grass, Sand, Snow, Rock, Mud: check the old terrain's Paint Texture list) and
   set the five layer numbers to match. Drag the caves into **Cave Prefabs**, **Desert Cave Prefabs** and **Snow Cave
   Prefabs**, StoneNode, CopperNode and IronNode into the ores, GoldNode and SandstoneNode into Gold / Sandstone Ore,
   DiamondNode into **Diamond Ore**, and the Scatter object into **Scatter**.
4. **Scatter > Object Scatter > Terrain**: leave it empty. Add the snow's rules (Terrain Layer = the snow texture's
   number, 2): copy ChestsDesert, Rare / Epic / Legendary Chests Desert and change their names, layer and counts (40, 20,
   8, 4); a rule per snow tree (`Assets/Prefabs/ResourceNodes/Trees/TreeNode Snow ...`, Count 120, Min Strength 0.7, Max
   Slope 25, Min Spacing 9, Spacing Group Trees, Patch Size 120); Snow Rocks (`Rocks/StoneNode Snow`, 40).
5. **AnimalSpawner > Terrain**: empty.
6. **Main Camera > Add Component > ViewDistance**. Then **Window > Rendering > Lighting > Environment > Other
   Settings**: tick **Fog**, **Mode** Linear, **Start** 40, **End** 100.
7. **Tools > World > Set Up Sky Lighting**, then **File > Build Settings > Add Open Scenes**.

## Testing

1. Open ProceduralGenerateScene and press Play: the Console says "World Generator: seed ..., an endless world in 256 m
   chunks". The player stands in a clearing in green grassland with grass tufts around, the world fading into a mist
   about 100 m away; trees, bushes, rocks and chests stand around, deer show up after a moment (out of sight first).
2. Walk in one direction for a few minutes: the land keeps coming, without stopping the game. Look for rock caves
   (rocky mounds) and mine their stone, copper and iron.
3. Walk back: the trees you chopped are still gone, an opened chest is still opened.
4. Jump to a biome: select **World Generator** while playing > **⋮ > Test: Go To The Snow** (or **Test: Go To The
   Desert**): the player lands on the nearest snow / desert. In the snow: snowy pines, snow caves with diamond rocks
   (Gold Pickaxe), lots of chests (`Docs/Snow Setup.md`).
5. Stop, Play again: a different world. Die: a brand-new world (Day 1).
6. From the title screen: **NEW GAME > Map: New world > START GAME**. **Map: Old world** still starts GameScene as before.

## Problems

| Problem | Why / fix |
|---|---|
| New world starts the old world | ProceduralGenerateScene isn't in Build Settings (the Console says so): run **Tools > World > Build New World Scene**. |
| The ground is a white checkerboard, no trees, no deer | The World Generator has no ground textures (its **Layers** list is empty; the Console warns "no ground textures"). Run the tool again: it takes the textures from GameScene's painted terrain. |
| A glowing barrier, or the land ends | An old copy of the scene, or old scripts: run **Build New World Scene** again. |
| Small stutters while walking | A chunk is being made or shown. Lower **Frame Budget**, or make fewer things grow (a bigger **Rules Area**, smaller rule Counts). |
| The game gets slower the longer I play | Fixed 22/11/2026 (see "Keeping it fast" below): far chunks let go of their things, trees and rocks skip their empty Update. If it still happens while exploring: lower **Content Distance**. At night, many monsters out at once cost time too (Normal and Hard don't limit them: kill them, or pick Easy, which keeps at most 20). In the Editor, keep the Hierarchy's "Generated World" folded and the Console's Collapse on: a big open Hierarchy slows the Editor. |
| A tree I chopped (or a chest I bought) is back | It shouldn't be: chunks remember what was used up. Only a tree left half-chopped comes back whole. Check that the World Generator's scripts are the 22/11/2026 ones. |
| The land pops in close by | You're faster than the chunk maker (e.g. a dive with Glide): raise **Load Distance** or **Frame Budget**. |
| No snow / desert after walking far | Biomes are patchy: walk another way, or use **⋮ > Test: Go To The Snow**. |
| No caves | The World Generator has no Cave Prefabs: run **Tools > World > Use Low Poly Rocks** (the rock shapes), then Build New World Scene again. Caves also need gentle ground: on rocky hills there are none. |
| No chests in the snow, or only common ones | Run **Tools > World > Build Loot Chests** (it makes the rare, epic and legendary rules in GameScene), then Build New World Scene again. |
| No snowy pines | Run **Tools > World > Use Low Poly Grassland Trees** first (TreeNode must be a low-poly tree), then Build New World Scene again. |
| The grass looks bent over or squashed flat | Set **Grass Width** below **Grass Height** (and **Grass Bending** low). |
| No grass tufts | **Grass Texture** is empty (the Polytope pack isn't in the project), or **Grass Density** is 0, or Quality settings draw no terrain details. |
| In the built game (web) the grass is green or black squares, but fine in the Editor | Run **Tools > World > Fix Grass In Builds** (the World Generator's **Chunk Template**: a Unity 2023.1 bug with terrain made while the game runs), check the grass picture has **Read/Write** on, then build again. If it's still wrong, untick World Generator > **Grass In Builds**: no grass in the built game, the Editor keeps it. |
| The mist starts too near / too far | **Main Camera > View Distance > Distance** (and **Clear Share**). See `Docs/View Distance Setup.md`. |
| No mist in a built game (the far world just ends) | The scene was saved with fog off: run **Build New World Scene** again (it saves the fog on). |
| No Map row on the NEW GAME page | Rebuild the title screen: **Tools > UI > Build Main Title Scene**. |
| No deer | AnimalSpawner > Terrain Layer must be the grass texture's number (0 as in GameScene). |
| Pitch-black shadows from the title screen | The new scene's sky light isn't saved: open it and run **Tools > World > Set Up Sky Lighting** (the tool does it by itself). |
| Something added to GameScene is missing in the new world | The new world is a copy made by the tool: run **Build New World Scene** again. |
| Always the same world | World Generator > **Seed** isn't 0. |

## Keeping it fast (22/11/2026)

The new world used to get slower the longer it was played. What did it, and what the game does now:

| What grew | Now | The setting |
|---|---|---|
| **Every chunk ever made kept all its things:** a few hundred trees, rocks and chests each, switched off but still in memory, and more to clean up for the game's garbage collector the farther you walked | Chunks past 500 m let go of their things and make them again when you come back (what you used up stays gone; rocks keep their shape: RockLook picks it from where the rock stands) | World Generator > **Content Distance** (500 m) |
| **Every tree and rock ran an empty Update** every frame (about 2000 near you in a forest) | Only berry bushes run it (Turki's ResourceNode switches it off for the rest after it starts) | (none) |
| **Building a chunk's ground** happened in one frame (a stutter each time new land appeared) | Spread over a few frames | World Generator > **Frame Budget** |
| **Each damage number** left a copy of the font's material behind (one a hit) | Removed with the number | (none) |
| **The web build** wrote every plain message (every hit, every chest that appeared) to the browser's console with its stack trace | A finished build writes only warnings and errors (Utility/BuildLogs; the Editor and Development Builds still show everything) | File > Build Settings > **Development Build** to see them |

**Night monsters aren't limited on Normal and Hard.** A limit was tried the same day (20 alive at once, ones left far
behind taken away) and taken out again: Tony noticed the game didn't slow down while he stayed in one place with 10 -
12 slimes around, only while exploring. So the nights are as busy as Marko made them (a monster every 5 s from night 3
on, until dawn). The limit lives on as **Easy** on the NEW GAME page.

Made from Sanyam's world pieces: his **ObjectScatter**'s rules fill every chunk of the new world (his script isn't
changed for it; log `Docs/TonyEditMate/Sanyam/21-11-2026 Endless World Uses The Scatter Rules.md`). His old
`Assets/Map/Cave.prefab` isn't used: the caves are made of the low-poly rocks.
