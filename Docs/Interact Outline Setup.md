# Interact Outline Setup (glowing outlines + chests on the floor)

**What the player sees:**
- **A yellow outline on a chest:** look at it from closer than 3 m (where **E** opens it) and a yellow outline glows
  around it, pulsing gently. Look away and it fades. Once the chest is opened, it doesn't glow any more.
- **Chests stand on the floor:** spawned or placed, on flat ground or a slope (tilted a little to lie flat). They no
  longer hover above the grass.

**Why chests floated:** Sanyam's ObjectScatter spawns chests at the ground + **Y Offset 0.6**. That was right for the
old box chest, whose middle was its pivot. Marko's new Quaternius model sits lower inside the prefab, so every chest
hovered above the grass.

## How it works

- **InteractOutline** (`Assets/Script/Effects/InteractOutline.cs`) goes on anything you can use:
  - It draws an outline around the thing's meshes, on top of the picture, the same thickness on any screen size.
  - **Show When = Look At:** it shows while the camera looks straight at the thing from within **Range**. This is
    the same test the chest's **E** key uses, so "glowing" means "E works now".
  - It hides while a menu is open, while the player is dead, and once a chest is opened.
  - All outlines on screen are drawn together by the main camera. Nothing is added to the scene.
- **StandOnGround** (`Assets/Script/World/StandOnGround.cs`) goes on anything that should rest on the floor:
  - When the game starts, it looks straight down for the nearest solid thing: the terrain, a built floor or a rock.
    It ignores itself, the player and anything else physics moves.
  - It stands the thing's bottom there, sunk 2 cm so no gap shows on bumpy ground.
  - On a slope it tilts to lie flat, up to **Max Tilt** (15°).
  - **Foot Offset** = how far the model's bottom is below the object's pivot. The tool measures it.
- **The E key:** in first person the camera sits right on the edge of the player's own ball, so a plain ray often hit
  the player first and E did nothing. The chest and the outline now both skip the player's body
  (`InteractOutline.LooksAt`).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > World > Set Up Chests**. It:
   - adds **StandOnGround** to the Chest prefab (`Assets/Prefabs/WorldItems/Chest`, and any other prefab with
     PerkChest) and measures its **Foot Offset**;
   - adds a yellow **InteractOutline** with **Range** = the chest's **Interaction Range** (3);
   - puts the 4 chests placed in GameScene on the floor right away (**Ctrl + Z** undoes that part).
3. Save the scene (**Ctrl + S**).
4. Play. Spawned chests settle by themselves when the game starts.

Running it again measures again (e.g. after the chest model changes). It keeps an outline color you changed.

## By hand (instead of the tool)

1. Open the **Chest** prefab (`Assets/Prefabs/WorldItems/Chest`).
2. **Add Component > StandOnGround.** Set **Foot Offset** to how far the model's bottom is below the prefab's pivot.
   Leave it at -1 and it measures itself when the game starts.
3. **Add Component > InteractOutline.** Set **Color** to yellow, **Show When** to *Look At*, and **Range** to the
   PerkChest's **Interaction Range** (3).
4. Save the prefab. For a chest already in the scene: click **⋮** on its StandOnGround > **Settle Now**.

## Other things that can be used

Put **InteractOutline** on them and pick a color. For example, Turki's berry bush (picked with E):
- **Color:** green.
- **Show When:** *Look At*.
- **Range:** its **Collect Range**.

A bush disappears when picked, so its outline goes with it. For something that stays after it's used up, switch
its InteractOutline off from your script, e.g. `GetComponent<InteractOutline>().enabled = false;`.

## Settings

| On | Setting | What it does |
|---|---|---|
| InteractOutline | **Color** | The outline's color (yellow = can open / use). |
| | **Width** (4) | Pixels on a 1080-high screen; it keeps that size on any screen. |
| | **Fill** (0) | A light tint over the thing itself (0.15 = a soft glow). |
| | **Pulse** (0.3) | How much it pulses to catch the eye (0 = steady). |
| | **Show When** | *Look At* (when E works), *Near* (player within Range) or *Always*. |
| | **Range** (3) | Meters. Look At measures from the camera, Near from the player. |
| | **Fade Seconds** (0.12) | How fast it fades in and out. |
| StandOnGround | **Foot Offset** | Meters from the pivot down to the model's bottom (-1 = measure when the game starts). |
| | **Max Tilt** (15) | The most it tilts to lie flat on a slope (0 = always upright). |
| | **Sink** (0.02) | How far it sinks into the floor. |
| | **Look Above** (1) | How far above its pivot it starts looking for the floor. Keep it lower than a roof over it. |

## Testing

1. Play in first person, walk to a chest and look at it: yellow outline. Step back past 3 m: it fades.
2. Press **E**: the perk panel opens (looking is free) and the outline hides. **E** again, or walking away, closes it
   and the outline comes back. After choosing a perk (that's when the coins are paid), the chest is gone (or stays
   without an outline, if its **Despawn After Opened** is off). The panel: `Docs/Perk Panel Setup.md`.
3. Look around the map: no chest floats, also on slopes.
4. To check the look anywhere: set the chest's **Show When** to *Always* while playing.

## Problems

| Problem | Why / fix |
|---|---|
| No outline at all | Look straight at it from closer than **Range**. In third person the camera is too far back (E doesn't work there either). Check that the chest has InteractOutline (run the tool). |
| "its shaders ... are missing" in the Console | `Assets/Effects/InteractOutline/Resources` must hold InteractOutlineMask.shader and InteractOutlineComposite.shader. |
| The outline shows through walls | It shows only while you look right at the thing (Look At), so that hardly happens. *Near* and *Always* do show through walls. |
| A chest still floats or sinks | Run **Tools > World > Set Up Chests** again (it measures Foot Offset), or click **Settle Now** on it. |
| A chest landed on a roof or shelf | Lower its **Look Above**. |
| A chest stands crooked on a slope | Lower **Max Tilt** (0 = always upright). |

Marko's change log: `Docs/TonyEditMate/Marko/19-11-2026 Chest On The Floor And Yellow Outline.md`.
