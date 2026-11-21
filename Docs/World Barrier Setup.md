# World Barrier Setup (the edge of the world)

**What the player sees:** at the edge of the map stands a see-through force field, light blue with slowly sliding
stripes. From afar it shows only faintly; as you come near it gets brighter, and right where you are it lights up with
rings rippling out from you. You can't walk, jump or fall through it, so you can tell by looking that the world ends
there (instead of mountains along the edge).

- Enemies, thrown things and anything else with physics can't pass it either. Aiming, spawning rays and the player's
  sight go through it.
- Deer never walk out through it, and nothing spawns outside it: trees, bushes, rocks and chests (Sanyam's
  ObjectScatter), deer (AnimalSpawner) and night enemies (Marko's EnemySpawner moves a spot past the barrier to the
  player's other side).

## New world: nothing to do

The World Generator makes the barrier itself every game: a 1000 x 1000 m square around the world, from below the
lowest ground to 60 m above the highest hills. The land goes on 60 m past it (bare rock and mud, fading into the mist).

## Old world (GameScene): one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > World > Add World Barrier**, then **Add**.
3. Save the scene (**Ctrl + S**).

The tool puts a **World Barrier** object in the scene:
- **Where:** on the inner faces of Sanyam's walls (the `Boundaries` object: East, West, South and North Wall). In
  GameScene that's x 1005 to 1975, z 1005 to 1995 (970 x 990 m), the area the player spawns in. His walls are left as
  they are. (Three of them are triggers, which stop nothing; now the barrier's own walls stop the player.)
- Without such walls it goes just inside the edge of the terrain with the most ground textures.
- **How tall:** from 10 m below the lowest ground along its line to 60 m above the highest.
- **Again:** run it again to fit the existing barrier again (it never adds a second one). **Ctrl + Z** takes it back.
- In a scene with a World Generator (the new world) it does nothing: that one makes its own.
- Build New World Scene leaves GameScene's barrier out of the new world's copy.

## By hand

1. **GameObject > Create Empty**, name it **World Barrier**, **Add Component > WorldBarrier**.
2. Put it in the **middle** of the map, at or below the **lowest** ground (Rotation 0, Scale 1).
3. Set **Size** (how wide in x and long in z the inside is) and **Height** (how tall from the object up). The cyan box
   in the Scene view shows where it stands.
4. Save the scene.

## Settings (WorldBarrier)

| Setting | What it does |
|---|---|
| **Size** | The inside's width (x) and length (z) in meters, around this object. |
| **Height** | How tall the wall is from this object up. |
| **Solid** | On = bodies can't pass (walls 10 m thick just outside the line, on the Ignore Raycast layer). Off = only a look. |
| **Color** | The force field's color (light blue). |
| **Far Alpha** | How much it shows from afar (0.12). 0 = invisible until you're near. |
| **Near Alpha** | How much it shows where the player is (0.8). |
| **Glow Radius** | How far around the player it lights up and ripples (12 m). |
| **Stripe Spacing**, **Stripe Speed** | Meters between stripes (2.5), how fast they slide up (0.4). |
| **Fade Above** | It fades out this far above the player (40 m), so it doesn't stand like a cliff over the whole world. |

The look settings change at once while playing (Unity forgets Play-mode changes when you stop: set them again after).

## How it works

| Part | What it does |
|---|---|
| `Assets/Script/World/WorldBarrier.cs` | Makes the see-through wall (four upright sides, made in code) and the solid walls when the game starts, and tells the wall's material where the player is every frame. `WorldBarrier.Inside(spot, margin)` = is a spot inside (true when the scene has no barrier). |
| `Assets/Effects/WorldBarrier/Resources/WorldBarrier.shader` | The look: diagonal stripes, a faint base, brighter and rippling around the player, fading out high above them, drawn from both sides, hidden by the fog far away. In a Resources folder, so a built game keeps it. |
| `Assets/Script/Editor/WorldBarrierBuilder.cs` | **Tools > World > Add World Barrier**. |
| `WorldGenerator` | Makes the new world's barrier (`MakeBarrier`). |

Teammates' scripts that ask it (each change marked `[Tony]`):
- Sanyam's `World/ObjectScatter.cs`: skips spots outside (log `Docs/TonyEditMate/Sanyam/20-11-2026 Nothing Spawns Outside The Barrier.md`).
- Marko's `Enemy/EnemySpawner.cs`: a spawn spot past the barrier goes to the player's other side (log `Docs/TonyEditMate/Marko/20-11-2026 Night Enemies Burn Away At Day.md`).

## Testing

1. Play the new world (or GameScene after Add World Barrier). Walk to the edge of the map: the stripes show up, get
   brighter, and light up where you are. Push into it: you stop.
2. Jump at it, sprint into it: still stopped.
3. Watch deer near the edge: they turn away from it.

## Problems

| Problem | Why / fix |
|---|---|
| I walk through it | **Solid** is off, or the object is turned / scaled (keep Rotation 0, Scale 1). |
| I can't see it | Its shader is missing (the Console warns): `Assets/Effects/WorldBarrier/Resources/WorldBarrier.shader` must exist. Or **Far Alpha** / **Near Alpha** are 0. |
| It's in the wrong place in GameScene | Sanyam's walls moved: run **Add World Barrier** again, or change **Size** and the object's position by hand. |
| Chests or deer outside the barrier | Something spawns without asking `WorldBarrier.Inside`; ObjectScatter, AnimalSpawner and EnemySpawner do. |
