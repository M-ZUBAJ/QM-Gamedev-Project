# Sky Lighting Setup (no pitch-black shadows when the title screen starts the game)

**The problem:** start the game from **MainTitleScene** (START GAME) and every shadow in GameScene is pitch black:
the inside of the trees, the ground under them, the shady side of a chest. Playing GameScene directly looks fine.

**Why:** shadows are lit by the light that comes from the whole sky (Unity calls it *ambient* or *environment*
light), and shiny things reflect the sky. Unity works both out by itself only for the scene that's open in the
Editor, and only for the moment. A scene opened by another scene (START GAME loads GameScene) or in a built game gets
only what was saved for it with **Generate Lighting**. GameScene never had any saved, so from the title screen its
shadows got no light at all.

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > World > Set Up Sky Lighting**, then **Save Sky Light**. It:
   - gives GameScene its own Lighting Settings, `Assets/Scenes/GameScene Lighting.lighting`, with **Baked Global
     Illumination**, **Realtime Global Illumination** and **Auto Generate** off;
   - generates the lighting (a few seconds). Unity saves the sky's light and reflection in the folder
     `Assets/Scenes/GameScene/` (`LightingData.asset` and a reflection picture);
   - saves the scene.
3. Commit the new files together with the scene, so your teammates get the light too.

**Why Global Illumination is off:** baked GI would bake the sky's light into the ground once, so the ground would
stay lit at night (the day / night cycle can't change baked light), and baking the big terrain takes minutes.
Realtime GI isn't needed for this look.

## By hand (instead of the tool)

1. Open GameScene, then **Window > Rendering > Lighting**, tab **Scene**.
2. **Lighting Settings Asset** says None: click **New**.
3. Untick **Realtime Global Illumination** and **Baked Global Illumination**.
4. At the bottom: untick **Auto Generate**, then click **Generate Lighting**.
5. Save the scene (**Ctrl + S**).

## When to run it again

- After changing the sky in **Lighting > Environment** (Skybox Material, Sun Source, Environment Lighting or
  Environment Reflections): the saved light is a picture of the sky as it was.
- After **Clear Baked Data** in the Lighting window (that deletes it).
- For a new scene that another scene opens.

Moving trees, chests or the terrain doesn't need it: only the sky counts.

## Testing

1. Open **MainTitleScene**, Play, **START GAME**: shadows are soft and blue-grey, like when GameScene is played
   directly.
2. The Console has no warning "has no saved light from the sky".
3. At night the DaylightCycle on the sun fades this light into a dark blue (**Night Ambient Color**, with the moon:
   `Docs/Day And Night Setup.md`).

## Problems

| Problem | Why / fix |
|---|---|
| Still pitch black from the title screen | GameScene wasn't saved after the tool, or a teammate doesn't have the files yet (commit / pull `Assets/Scenes/GameScene/` and `GameScene Lighting.lighting`). Run the tool again with GameScene open. |
| Console: "... has no saved light from the sky" | Same: run the tool. Until then the DaylightCycle asks Unity to work the sky's light out when the game starts (a backup; shiny surfaces still miss the sky's reflection). |
| Too dark or too bright | DaylightCycle (on the Directional Light): **Day Ambient**; at night **Moon Intensity** / **Night Ambient Color** (`Docs/Day And Night Setup.md`). |
| "Unity couldn't generate the lighting" | The Console says why. Stop Play first, and save the scene once. |
| Undo it | Lighting window: **Clear Baked Data**, set **Lighting Settings Asset** back to None, delete `GameScene Lighting.lighting`. |
