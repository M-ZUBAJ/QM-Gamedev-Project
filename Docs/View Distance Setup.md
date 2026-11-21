# View Distance Setup (how far the player can see)

**What the player sees:** the world fades into a mist about 100 m away (it starts thinning the view from 40 m). The mist is light blue
in the day, orange at sunrise and sunset, and dark blue at night, and it also lies low on the sky all round, so the far
ground melts into the sky. Trees, rocks and deer come out of the mist as you walk toward them.

**Why:** nothing past the mist is drawn at all, so there are far fewer trees, rocks, grass tufts and shadows to draw
every frame. That keeps the frame rate up, most of all in the web (WebGL) build. It's the game's "line of sight", like
Minecraft's Render Distance (100 m is about 6 chunks).

The new world has it (Tools > World > Build New World Scene puts it there). The old world doesn't, unless you add it.

## Where it is and how to change it

The script is **ViewDistance** (`Assets/Script/Camera/ViewDistance.cs`), on the **Main Camera**.

1. Open `Assets/Scenes/ProceduralGenerateScene.unity`.
2. In the Hierarchy, select **Main Camera**.
3. In the Inspector, find the **View Distance** component and change:

| Setting | What it does |
|---|---|
| **Distance** | How far the player can see, in meters (100; it was 30 at first). Lower = faster, higher = see further (10 - 500). |
| **Clear Share** | How much of that is clear before the mist starts (0.4 = clear for the first 40 m of 100, then thicker and thicker until nothing shows at 100 m). |
| **Mist On Sky** | How high up the sky the mist reaches (0.35 = about 20 degrees above the horizon; 0 = only below it; 1 = no sky at all). |
| **Day / Sunset / Night Color** | The mist's color at each time of day (it follows the scene's DaylightCycle by itself). Pick colors close to the sky's, so the mist blends into it. |

4. Save the scene (**Ctrl + S**).

- While playing you can drag **Distance** and watch the mist move, but Unity forgets changes made in Play mode when
  you stop: change it after stopping, then save.
- **Build New World Scene** makes the scene again from GameScene, which puts Distance back to 100. To keep another
  distance for good, change the number in `ViewDistance.cs` (`float distance = 100f;`), or set it again after each
  rebuild.
- How much faster: in the Game view, click **Stats**. Compare **FPS** and **Batches** with the component ticked and
  unticked (unticked = everything back as before).

## What it changes (only while it's on)

| What | How |
|---|---|
| **Main Camera > Far Clip Plane** | = Distance: nothing further is drawn. Shadows end there too (Unity never draws them past the far clip plane). |
| **Fog** (Lighting window) | On, Linear, from Distance x Clear Share to Distance, its color following the time of day. |
| **The sky's mist** | A flat card at the far end of the camera's view, the fog's color, solid near the horizon and fading out higher up (shader `Assets/Effects/ViewDistance/Resources/ViewDistanceMist.shader`, in a Resources folder so a built game keeps it). Anything closer hides it, so it only covers the sky and the empty space past the last drawn hill. |
| **Terrain grass** | Drawn only up to Distance (the World Generator's Grass Distance, 80 m, is capped). |
| **PlayerSight** (on the Player) | Its range is Distance (instead of its own Sight Range), so the AnimalSpawner brings deer just past the mist, where they can't be seen appearing. |

Untick the component (or remove it) and the camera, fog and grass go back to how the scene had them.

## Adding it to another scene (e.g. the old world)

1. Select the scene's **Main Camera** > **Add Component** > **ViewDistance**.
2. **Window > Rendering > Lighting > Environment > Other Settings**: tick **Fog**, **Mode** Linear (Start 40, End 100).
   ViewDistance sets the fog itself while playing; this is for **built games**: Unity leaves fog out of a build when
   no scene in it has fog on (Graphics settings > Fog Modes = Automatic), and then the mist is missing.
3. Save the scene.

## For teammates (code)

- `ViewDistance.Limit`: how far the player can see right now, in meters (no limit without a ViewDistance). E.g. a
  spawner that only spawns out of view.
- `ViewDistance.Current.SetDistance(50f)`: changes it while playing (e.g. a settings slider).

## Problems

| Problem | Why / fix |
|---|---|
| No mist, the far world just ends at 100 m in a built game | The scene's fog is off in the Lighting window: tick **Fog** (Linear) and save (Build New World Scene does it for the new world). |
| A hard line where the ground meets the sky | The mist shader is missing (the Console warns), or **Mist On Sky** is 0. |
| The mist's color doesn't match the sky | Change **Day / Sunset / Night Color** (pick from the sky with the color picker's eyedropper while playing, then set the same after stopping). |
| Deer appear in plain view | The Player needs **PlayerSight** (**Tools > World > Set Up World Spawning** adds it): with it, deer only appear past Distance or where you aren't looking. |
| The old world is foggy too | Its Main Camera has a ViewDistance: remove it. |
