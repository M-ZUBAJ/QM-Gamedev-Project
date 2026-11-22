# Day And Night Setup (the sun, the moon and the blue night)

**What the player sees:** the sun crosses the sky and sets in orange. A little after sunset the sky goes dark for a
moment, then the **moon** rises on the other side of the sky: a soft pale-blue light with soft shadows, a dark blue
sky with the moon in it, and blue light in the shadows, so the world is still easy to see at night. Before sunrise
the moon sets and the sun comes back.

Everything is done by **DaylightCycle** on the scene's **Directional Light**. There is nothing to build: the blue
night is on by default (**Moonlight**).

## How it works

- **One light does both.** In the day it's the sun. Once the sun is about 6 degrees below the horizon, the light
  turns into the moon, on the other side of the sky (a full moon every night). At that moment it has no brightness
  at all, so the change can't be seen; then the moon gets brighter as it rises.
- **The light from everywhere** (Unity's *ambient* light: what lights the shadows). In the day it's the sky's light
  saved by **Tools > World > Set Up Sky Lighting** (`Docs/Sky Lighting Setup.md`). At night it fades into **Night
  Ambient Color**, a dark blue.
- **The sky** (the Procedural Skybox). The sky is drawn from where the light is, so at night it shows the moon where
  the sun's disk was. DaylightCycle turns the sky down to a dim blue (**Night Sky Exposure**, **Night Sky Tint**). It
  changes a copy of the sky material made when the game starts, so the material file itself never changes.

## Settings (DaylightCycle, on the Directional Light)

| Setting | What it does |
|---|---|
| **Moonlight** (on) | off = the old dark nights: no moon, the day's light from everywhere times Night Ambient |
| **Moon Intensity** (0.5) | how bright the moon is high in the sky (the sun at noon is Max Sun Intensity, 1.2) |
| **Moon Color** | pale blue |
| **Moon Shadow Strength** (0.6) | how dark the moon's shadows are (1 = as dark as the sun's) |
| **Day Ambient** (1) | the day's light from everywhere, times this |
| **Night Ambient** (0.2) | only with Moonlight off: the night's light from everywhere = the day's times this |
| **Night Ambient Color** | with Moonlight on: the dark blue that lights the shadows at night (what a white wall in the shade looks like) |
| **Night Sky Exposure** (0.2) | how bright the night sky is (the day keeps the sky material's own, 1.3) |
| **Night Sky Tint** | the night sky's color |
| **Reset On Player Death** (on) | when the player dies, the day, the time and the light go back to how the game started, and the clock stands still on the death screen until the player respawns (21/11/2026: before, a long look at the death screen let night and the next day go by, so the player came back on Day 2). Off = time carries on |

## Testing

1. Play GameScene, select the **Directional Light** and drag **Time Of Day** to **0** (midnight): moonlight, blue
   shadows, a dark blue sky and the moon straight up.
2. Change the Moon, Ambient and Night Sky settings while playing: the night changes at once. Values changed while
   playing go back when you stop, so remember the ones you like and type them in again after stopping.
3. Drag Time Of Day slowly from 0.7 to 0.85: sunset, a short moment of dark sky, then the moon rising.

## Problems

| Problem | Why / fix |
|---|---|
| Night too dark | Raise **Moon Intensity**, or make **Night Ambient Color** lighter. |
| Night too bright or not blue enough | Lower Moon Intensity, make Night Ambient Color darker or bluer, lower **Night Sky Exposure**. |
| The sky stays black at night | The scene's sky material has no Exposure (or there is none): Window > Rendering > Lighting > Environment > Skybox Material = Default-Skybox. |
| No moon in the sky | The sky material's **Sun** is None, or **Sun Source** (Lighting > Environment) is another light. |
| Shadows pitch black when starting from the title screen | The scene has no saved sky light: Tools > World > Set Up Sky Lighting. Until then the night's blue light from everywhere isn't used. |
| After dying it isn't Day 1 at the start time | **Reset On Player Death** is off, or the Player has no PlayerHealth. (In the new world, Respawn makes a whole new world: Day 1 anyway.) |
