# The grass in the web build: what went wrong and how it was fixed

**Date:** 23/11/2026 (fixed in v2.25). **Where:** the new world (ProceduralGenerateScene) in the built game, here the
web build at https://m-zubaj.github.io/QM-Gamedev-Project/Webgl_New/. The old world (GameScene) has no grass tufts, so
it never had this problem.

## In short

In the Editor the new world's grass tufts looked right. In the built web game they didn't, for two separate reasons:

| What we saw in the web build | Why | Fix |
|---|---|---|
| 1. Every grass tuft was a **solid black rectangle** | The grass picture had **Read/Write** off, so the built game couldn't read it | Read/Write on for `Assets/Effects/Grass/GrassBlades.png` |
| 2. Every tuft was a **full green card with white edges** (the see-through parts weren't cut away) | A **Unity 2023.1 bug** (UUM-25114): terrain made while the game runs gets the wrong grass shader in a built game | Each chunk's terrain now starts as a copy of a terrain made in the Editor (**Tools > World > Fix Grass In Builds**) |

Both are needed: the second problem only showed once the first was fixed.

## Why the Editor looked fine

The new world doesn't have a saved terrain. The **World Generator** makes the land in squares (chunks) while you play,
and gives each square its grass in code (`WorldGenerator.AddGrass`). Two things then work differently in the Editor and
in a built game:

1. **Reading the grass picture.** Unity packs all grass pictures into one sheet by reading their pixels. The Editor
   can always read a picture. A built game can only read pictures that have **Read/Write** turned on (to save memory,
   it throws the pixels of the others away once they're on the graphics card). With Read/Write off, the sheet stayed
   empty: black squares.
2. **The grass shader.** A terrain needs Unity's grass shader (`Hidden/TerrainEngine/Details/WavingDoublePass`): it
   cuts away the see-through parts of the picture and makes the grass wave in the wind. In Unity 2023.1, a terrain made
   from nothing while the game runs (`new TerrainData()` in code) gets that shader in the Editor, but in a built game
   it gets a plain one (Legacy Shaders/Diffuse) instead: the whole picture is drawn, see-through parts and all, and the
   grass doesn't wave. That's Unity's bug UUM-25114. Unity fixed it in later versions (2022.3.59f1, Unity 6), but not
   in 2023.1, the version we use. Adding the shader to Always Included Shaders doesn't help (Unity's own report says so,
   and ours was already there). A terrain **made in the Editor** doesn't have the bug.

## The fix

**1. Read/Write on the grass picture**

- `Assets/Effects/Grass/GrassBlades.png` > Inspector > **Advanced > Read/Write** is ticked.
- Tools > World > Build New World Scene (and Fix Grass In Builds) tick it themselves (`NewWorldBuilder.MakeReadable`).
- The World Generator writes a warning in the Console if its grass picture isn't readable.

**2. A chunk template made in the Editor**

- **Tools > World > Fix Grass In Builds** makes `Assets/Prefabs/World/New World Chunk Template.asset`. It's a small
  empty terrain made in the Editor, with the same grass on it as the World Generator's. The tool then puts it in the
  World Generator's new **Chunk Template** slot in ProceduralGenerateScene. Nothing else in the scene changes.
- The World Generator now makes each chunk's terrain as a **copy of the template** (`Instantiate(chunkTemplate)`),
  not from nothing, and then sets its size, heights, textures and grass as before. Being a copy of an Editor-made
  terrain, it gets the right grass shader in the built game.
- Tools > World > Build New World Scene does this step too, so a rebuilt new world keeps the fix.
- In a built game, the World Generator warns in the Console if the Chunk Template slot is empty.

**3. A switch, just in case**

- World Generator > **Grass In Builds** (ticked). Untick it and the built game has **no grass tufts** at all (the
  ground is still painted green); the Editor keeps them. Use it if the built game's grass ever looks wrong again and
  there's no time to fix it.

## How to check it

The Editor can't show this problem, so check in a build:

1. **File > Build Settings**, WebGL, **Build And Run** (into the `Webgl_New` folder). Unity opens the game in your
   browser from your own computer.
2. NEW GAME > Map: **New world** > START GAME.
3. Look at the grass around you: thin waving blades with the ground showing between them. Not black or green
   rectangles.

## If you make terrain or grass in code again

- Start a terrain made while the game runs from a copy of a **TerrainData made in the Editor**
  (`Instantiate(template)`), not from `new TerrainData()`, as long as we're on Unity 2023.1.
- Turn **Read/Write** on for every picture used as terrain grass.
- Test it in a **build**, not only in the Editor.

## Files changed (v2.25)

| File | What changed |
|---|---|
| `Assets/Effects/Grass/GrassBlades.png.meta` | Read/Write on |
| `Assets/Script/World/WorldGenerator.cs` | **Chunk Template** and **Grass In Builds** settings, each chunk made as a copy of the template, the two warnings |
| `Assets/Script/Editor/NewWorldBuilder.cs` | **Tools > World > Fix Grass In Builds**, the template made by Build New World Scene too, Read/Write ticked on the grass picture |
| `Assets/Script/Editor/MenuOrder.cs` | the menu item's place |
| `Assets/Prefabs/World/New World Chunk Template.asset` | the template (new) |
| `Assets/Scenes/ProceduralGenerateScene.unity` | the World Generator's Chunk Template slot filled |
| `Webgl_New/` | the web build, made again |
| `Docs/New World Setup.md` | the template step and a row in its Problems table |

## Sources

- Unity Issue Tracker: [Wrong grass shader when creating TerrainData at runtime in a build (UUM-25114)](https://issuetracker.unity.com/issues/15186/wrong-grass-shader-when-creating-terraindata-at-runtime-in-a-build): green rectangles in a build, Legacy Shaders/Diffuse instead of WavingDoublePass, reproduced in 2023.1; "If TerrainData is not dynamic, the grass is displayed correctly".
- Unity Issue Tracker: [Terrain Detail objects are not rendered in the build when the Terrain is generated at runtime (UUM-62407)](https://issuetracker.unity.com/issues/10753/terrain-detail-objects-are-not-rendered-in-the-build-when-the-terrain-is-generated-at-runtime): the issue the first one was merged into; fixed in 2021.3.49f1, 2022.3.59f1 and Unity 6, not in 2023.1.
- Unity Issue Tracker: [Grass/Detail not properly displayed in Build when Terrain is created with script (UUM-9765)](https://issuetracker.unity.com/issues/12353/grassdetail-not-properly-displayed-in-build-when-terrain-is-created-with-script): Won't Fix; Unity's workaround: start from a terrain made in the Editor, then change it in code.
