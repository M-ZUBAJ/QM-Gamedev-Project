# Punch Effect Setup (grey wind burst)

**What the player sees:** punch an enemy, a tree, a stone or an animal with an empty hand. When the fist lands, a grey
burst of wind pops out where it hit:
- **Puff:** grey dust clouds shoot out, slow down and hang for a moment;
- **Streaks:** light wind lines zip outward;
- **Ring:** a ring grows like a shock wave;
- **Dust:** a few dark specks fly off and fall.

It's gone after about a second.

**How it works:**
1. **The punch:** Marko's `PlayerCombat.Attack()` hits something with health (an enemy, or a tree, stone or animal)
   while the hand holds anything but the Sword, Axe or PickAxe. Tony's `[Tony]` lines there hand the hit to
   `PunchEffect.Punch(target, hit, hurt)` instead of hurting at once. Change log:
   `Docs/TonyEditMate/Marko/12-11-2026 Punch Wind Burst.md`.
2. **The timing:** Tony's `PunchEffect` on the Player waits **Impact Delay** seconds, until the fist is fully out in
   the punch animation.
3. **The hit:** then the target is hurt.
   - **A tree or stone** broken by this punch breaks now, with the burst, not at the click.
   - **Everything else that comes with a hit** happens now too: an enemy's flinch, coins, "+3 Wood".
4. **The burst:** a copy of `Assets/Prefabs/Effects/PunchWindBurst.prefab` appears just in front of the spot that was
   hit, facing out of it, and removes itself when its particles are gone.
   - **A moving target keeps the burst:** the spot is remembered on the target, so a running deer or a walking slime
     gets it on its body.

**Tools:** a sword, axe or pickaxe swing also lands when the tool does (**Swing Delay** / **Mine Delay**, measured
from the animations), and bursts wind too (**Tool Burst Size**). See `Docs/Tool Setup.md`.

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Effects > Build Punch Wind Burst**.
   - **It makes** `Assets/Prefabs/Effects/PunchWindBurst.prefab`, and in `Assets/Effects/PunchWindBurst` three pictures
     (`WindPuff`, `WindRing`, `WindDot`) with a material each.
     - **Replacing:** it asks before replacing the prefab. **Replace** rebuilds it inside the same file, so the
       Player's link to it keeps working.
     - **Your own art is kept:** pictures and materials are only made when missing. To get the drawn ones back,
       delete them and run the tool again.
   - **It measures the punch:** it plays the first-person punch on a hidden copy of the hands and finds the moment
     the right fist is furthest forward. That moment becomes **Impact Delay**.
     - **No first-person hands yet:** it measures the third-person punch instead.
     - **No punch at all:** Impact Delay is set to 0.25 s.
   - **It puts PunchEffect on the Player** and fills in its **Burst Prefab** and **Impact Delay**. **Ctrl + Z** undoes
     that.
   - The Console says what it did, e.g. *"Impact Delay = 0.31 s (when the fist is furthest forward in PunchRight)"*.
3. Save the scene (**Ctrl + S**).

```
PunchWindBurst   the top: plays its parts together, makes no particles itself
  Puff           grey dust clouds
  Streaks        wind lines
  Ring           the shock ring
  Dust           dark specks
```

## Test

**In Play mode:**
1. Press Play and select an empty hotbar slot (or wood, food... anything but the Sword, Axe or PickAxe).
2. Walk up to a tree or a stone, or spawn a slime: select **EnemySpawner**, click the **⋮** on its EnemySpawner
   component, then **Test Spawn Enemy**.
3. Look at it from up close (closer than 2.5 m) and left-click. The burst pops when the fist lands.
4. **Without punching anything:** click the **⋮** on the Player's **PunchEffect**, then **Test: Burst In Front**.
   A burst appears 1.5 m in front of the camera, with no delay.

**Without playing:** open the prefab (double-click it). In the **Particle Effect** box at the bottom right of the Scene
view, click **Play** (or **Restart**).

## Settings (PunchEffect, on the Player)

| Field | What it does |
|---|---|
| **Burst Prefab** | The burst to show. Empty = no burst. |
| **Size** (1) | How big the burst is: 2 = twice as big (spread, particles and speed all grow together). |
| **Impact Delay** (measured, about 0.3) | Seconds from the click until the punch lands: the target is hurt and the burst pops, when the fist is out. 0 = at the click. Keep it below Marko's Attack Cooldown (0.5). Rebuilt the hands with another speed? Run the tool again. |
| **Push Out** (0.15) | Meters in front of the hit spot where the burst starts, so the enemy or tree doesn't hide half of it. |
| **Left Jab / Left Hook / Heavy Punch Delay** | When PunchCombo's other punches land (the right jab uses Impact Delay). Measured by Tools > Player > Set Up Player Animations. |
| **Heavy Burst Size** (1.5) | How much bigger a heavy punch's burst is. |
| **Tool Burst Size** (1) | How big the burst is when a sword, axe or pickaxe hits (1 = like a jab, 0 = none). |

- **Switch it off:** untick the PunchEffect component.
- **Changes made while playing are lost** when you stop. Write the number down, stop, then type it in again.

## Changing the look

Open `PunchWindBurst.prefab`, select a part and change its Particle System:
- **Colour:** Start Color. Its 4th number (A) = how solid.
- **How many:** Emission > Bursts > Count.
- **How far it flies:** Start Speed, or Limit Velocity over Lifetime > Drag (bigger = stops sooner).
- **How long it lasts:** Start Lifetime.

**A new picture:** use any white picture with a see-through background. Drag it into the material's **Particle
Texture**. The particle colour makes it grey.

Running the tool again rebuilds the prefab, so changes made there are lost. To keep yours, copy the prefab first, then
drag your copy into the Player's **Burst Prefab**.

## By hand (the same thing without the tool)

**1. Pictures.**
- **Easiest:** let the tool draw them.
- **Your own:** any soft white PNGs with a see-through background:
  - a cloud for Puff;
  - a thin ring for Ring;
  - a soft round dot for Streaks and Dust.
- **Import settings** (select each PNG): **Alpha Is Transparency** on, **Wrap Mode** Clamp, click **Apply**.

**2. Materials.** For each picture:
1. Right-click in the Project window > **Create > Material**.
2. Set **Shader** to **Legacy Shaders > Particles > Alpha Blended**.
3. Drag the picture into **Particle Texture**. Leave **Tint Color** grey (128, 128, 128, 128), which means no change.

**3. The top object.**
1. **GameObject > Effects > Particle System**, named `PunchWindBurst`.
2. Set its **Rotation** to 0, 0, 0. The menu tilts it, and the parts must point along the blue arrow, which
   PunchEffect turns out of the hit surface.
3. Main settings: **Duration** 0.6, **Looping** off, **Simulation Space** Local, **Scaling Mode** Hierarchy.
4. Untick **Emission**, **Shape** and **Renderer**.

**4. The four parts.**
1. Right-click `PunchWindBurst` > **Effects > Particle System**, four times. Name them `Puff`, `Streaks`, `Ring`,
   `Dust`.
2. Set each one's **Rotation** to 0, 0, 0.
3. Every part: **Looping** off, **Play On Awake** on, **Simulation Space** Local, **Scaling Mode** Hierarchy,
   **Max Particles** 50, Renderer **Cast Shadows** Off, **Receive Shadows** off.
4. Then the settings below. For "from – to" numbers, click the small arrow next to the field >
   **Random Between Two Constants** (or **Two Colors**). Colours are 0–255; A = alpha.

| Setting | Puff | Streaks | Ring | Dust |
|---|---|---|---|---|
| Duration | 0.6 | 0.3 | 0.3 | 0.3 |
| Start Lifetime | 0.35 – 0.6 | 0.12 – 0.22 | 0.25 | 0.35 – 0.6 |
| Start Speed | 2.5 – 4 | 7 – 11 | 0 | 1.5 – 3 |
| Start Size | 0.3 – 0.55 | 0.05 – 0.08 | 1 | 0.03 – 0.06 |
| Start Rotation | 0 – 360 | 0 | 0 | 0 |
| Start Color | 230 grey, A 217 – 168/168/173, A 217 | 247 grey, A 242 – 204/204/209, A 230 | 235 grey, A 204 | 153 grey – 97/97/102 (A 255) |
| Gravity Modifier | -0.1 (drifts up) | 0 | 0 | 1.5 (falls) |
| Emission | Rate over Time 0, Bursts: Time 0, Count 12 – 16 | 0, Count 10 – 14 | 0, Count 1 | 0, Count 6 – 10 |
| Shape | Cone, Angle 75, Radius 0.1, Radius Thickness 0.6 | Cone, Angle 75, Radius 0.1, Radius Thickness 0 | untick Shape | Cone, Angle 70, Radius 0.05, Radius Thickness 1 |
| Limit Velocity over Lifetime: Speed 100, Dampen 0, Drag ..., both "Multiply Drag" boxes off | Drag 6 | Drag 9 | off | Drag 3 |
| Size over Lifetime (curve) | 0.45 → 0.9 at 25 % → 1.25 | 1 → 0.3 | 0.25 → 0.9 at 40 % → 1.2 | off |
| Color over Lifetime (alpha keys) | 0 → 255 at 8 % → 166 at 55 % → 0 | 255 → 204 at 60 % → 0 | 230 → 179 at 35 % → 0 | 255 → 255 at 70 % → 0 |
| Rotation over Lifetime | Random Between -115 and 115 | off | off | off |
| Renderer: Render Mode | Billboard | Stretched Billboard, Speed Scale 0.07, Length Scale 2.5 | Billboard | Billboard |
| Renderer: Material | WindPuff | WindDot | WindRing | WindDot |
| Renderer: Sort Mode, Sorting Fudge | By Distance, 2 | None, 0 | None, 1 | None, -1 |
| Renderer: Max Particle Size | 0.35 | 0.5 | 0.5 | 0.5 |

- **Sorting Fudge** decides which part is drawn on top: smaller = more on top, so the specks show over the clouds.
- **Drag** slows a part down fast: it bursts out, then hangs.
- **Wide cones** (70–75) send most of the burst sideways. From the front, sideways is what looks like an explosion,
  and nothing flies into the player's face when punching from up close.
- **Max Particle Size** 0.35 = a cloud right in front of the camera covers at most a third of the screen.

**5. Save it.** Drag `PunchWindBurst` into `Assets/Prefabs/Effects` and delete it from the scene.

**6. The Player.**
1. Select the Player > **Add Component > PunchEffect**.
2. Drag the prefab into **Burst Prefab**.
3. Set **Impact Delay**: play, punch something, and change the number until the burst pops when the fist lands.
   About 0.3 for the first-person punch.

## Problems

| Problem | Why / fix |
|---|---|
| No burst at all | **Setup:** the Player has no PunchEffect, or its Burst Prefab is empty: run the tool with GameScene open.<br>**Tools:** **Tool Burst Size** is 0. Set it to 1.<br>**Target:** it has no health (ground, buildings, items). Only enemies, trees, stones and animals count. |
| No burst in third person | Marko's attack ray starts at the camera and reaches 2.5 m (`PlayerCombat.cs`, `attackRange`). In third person the camera is behind the player, so punches rarely reach. |
| The burst (and the hit) comes before / after the fist | Change **Impact Delay**. |
| A tree or stone vanishes before the fist lands | The punch isn't going through `PunchEffect.Punch`. Check Marko's `PlayerCombat.Attack()` still has the `[Tony] replaced` lines, and the Player has a switched-on PunchEffect. |
| The burst appears in the air behind a running deer | Same: without `PunchEffect.Punch` the burst stays where the deer was hit. |
| Pink squares | A material lost its shader. Select WindPuff / WindRing / WindDot and set Shader to **Legacy Shaders > Particles > Alpha Blended**. Or delete the materials and run the tool again. |
| Half the burst is hidden inside the enemy | Raise **Push Out**. |
| Too big / too small | Change **Size**. |
| Too bright at night | The burst isn't lit (it's wind), so it looks the same day and night. Lower the A (alpha) of the parts' Start Colors. |
| Console: "couldn't measure the punch" | Impact Delay stays 0.25. Set it yourself (step 6 above). |
