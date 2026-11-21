# Death Effect Setup (creatures die into particles)

When a creature dies, its body turns into particles. The color tells you what you killed:

- **Green** = harmless (the deer). It bursts into green particles the moment it dies, in the shape of its body.
- **Red** = harmful (the slime). It plays its death animation. During the animation its body breaks into red
  particles: a few at first, then more and more while the body shrinks into them, and a last pop as the animation ends.

- **Orange** = a night enemy still around when **day comes**. It isn't killed: it burns away where it stands, like
  burning in the sunlight. It glows with a few orange embers, then shrinks into a cloud of embers that float up, and
  it's gone. No coins (nobody killed it).

A creature that is removed without being killed shows nothing (except that orange burn at dawn). That includes a deer
left far behind, or the game closing.

## How it works

- **DeathEffect** (`Assets/Script/Effects/DeathEffect.cs`) sits on the creature's top object.
- **Harmful or harmless:** with **Kind = Auto**, a creature that has Marko's **EnemyAttack** is harmful (red).
  Everything else is harmless (green).
- **The deer:** Turki's ResourceNode removes a killed deer at once. Just before it goes, DeathEffect bursts it into
  particles placed along its bones, so the cloud has the deer's shape.
- **The slime:** Marko's EnemyCombat calls Tony's EnemyAnimator, which plays the death animation and asks
  DeathEffect to **Dissolve**:
  - the particles come off the body's real surface, so they follow the falling body;
  - the slime is removed once nothing is left.
- **Burning away at dawn:** Marko's EnemySpawner removes the night's enemies when day starts (DaylightCycle's
  DayStarted, also when dying at night brings the morning back). Since 20/11/2026 it calls Tony's
  **EnemyAnimator.Vanish** for each one (a `[Tony]` line, log `Docs/TonyEditMate/Marko/20-11-2026 Night Enemies Burn Away
  At Day.md`): the enemy waits a random moment (up to **Vanish Spread**, 1.5 s, so they don't all go at once), stops,
  and DeathEffect's **Vanish** burns it away in **Vanish Color**. Enemies placed by hand (not spawned at night) stay.
- **The particles:** `Assets/Prefabs/Effects/DeathParticles.prefab`. White dots drift up a little, swirl and fade.
  DeathEffect colors them and decides where they appear (the orange embers get a stronger upward drift, on their own
  copy only).

## Quick way: one click

1. Menu **Tools > Effects > Build Death Particles** (any scene). It makes:
   - `Assets/Effects/DeathParticles/DeathDot.png` and `DeathParticle.mat`: a soft dot and its see-through material
     (only if missing, so your own art is kept);
   - `Assets/Prefabs/Effects/DeathParticles.prefab`;
   - **DeathEffect** on `DeerNode` and `SlimeEnemy` (every prefab with WildAnimal or EnemyAnimator).
2. For the slime it also:
   - times the dissolve to its death animation: it starts at 30 % of the animation and ends with it;
   - ticks **Read/Write** on the slime's model (`SlimeMesh` / `SlimePolyartMesh`), so the particles can come off its
     surface.
3. Play and kill a deer, then a slime.

**Build Wild Animal** and **Build Monster Enemy** add DeathEffect by themselves to the creatures they build.

## By hand (instead of the tool)

1. **The particles:**
   1. **GameObject > Effects > Particle System**, name it `DeathParticles`.
   2. **Main:** Looping on, Play On Awake off, Start Lifetime 0.7 to 1.4, Start Speed 0.2 to 0.9, Start Size 0.05 to
      0.13, Gravity Modifier -0.12, Simulation Space **World**, Max Particles 3000.
   3. **Emission** off. **Shape:** Sphere, radius 0.3.
   4. **Limit Velocity over Lifetime:** Drag 1.5. **Noise:** Strength 0.35, Frequency 1.2.
   5. **Color over Lifetime:** fade in quickly, fade out at the end. **Size over Lifetime:** shrink at the end.
   6. **Renderer:** a material with the shader `Legacy Shaders/Particles/Alpha Blended` and a soft white dot picture.
   7. Drag it into `Assets/Prefabs/Effects` and delete it from the scene.
2. **On each creature prefab** (`DeerNode`, `SlimeEnemy`): **Add Component > DeathEffect**, and drag `DeathParticles`
   into **Particles**.
3. **For the slime only:**
   1. Select its model (`Imported3D/RPG Monster DUO PBR Polyart/Meshes/SlimePolyartMesh`) > **Model** tab.
   2. Tick **Read/Write** > **Apply**.
   3. Set DeathEffect's **Dissolve Start** and **Dissolve Seconds** so they add up to its death animation's length.

## Settings (on DeathEffect)

| Setting | What it does |
|---|---|
| **Kind** | Auto (red if it has EnemyAttack, else green), or choose Harmless / Harmful yourself. |
| **Harmless Color**, **Harmful Color** | The green and the red. Each particle is a mix of the color and a lighter shade of it. |
| **Burst Count** (220), **Burst Speed** (1.6) | Dying at once: how many particles, how fast they fly apart (m/s). |
| **Dissolve Start** | With a death animation: seconds before the body starts turning into particles (it falls first). |
| **Dissolve Seconds** | How long the body takes to become particles. Start + Seconds = when the body is removed. |
| **Dissolve Rate** (320), **End Burst** (120) | The most particles per second while dissolving, and the last pop. |
| **Vanish Color** | The orange a night enemy burns away in when day comes. |
| **Vanish Seconds** (1.6) | How long burning away takes: a quarter glowing, then shrinking into the embers. |
| **Vanish Rise** (0.35) | How fast the embers float up (0 = they drift like the death particles; below 0 = they fall: the desert's turtle has -0.4 and a sand Vanish Color, so it turns to falling sand at night). |

On the enemy's **EnemyAnimator**: **Vanish Spread** (1.5) = the night's enemies burn away at random moments within
this many seconds.

On the **DeathParticles** prefab: **Start Size** = dot size, **Start Lifetime** = how long dots stay, **Gravity
Modifier** = how they drift (negative = up).

## Testing

1. Play, select a deer or slime in the Hierarchy, then click **⋮** on DeathEffect > **Test: Burst Here**. The burst
   shows without killing anything.
2. Hunt a deer: green burst, and "+Meat" as before.
3. Kill a slime at night: it falls over and breaks into red particles, then it's gone.
4. Burning away at dawn: at night, select a slime and click **⋮** on EnemyAnimator > **Test: Burn Away Now** (or ⋮ on
   the EnemySpawner > **Test Despawn Enemies** for all of them). Or wait for morning: set the DaylightCycle's **Day
   Length Minutes** to 1 and watch them burn away when Dawn starts. No "+N Coins".

## Problems

| Problem | Why / fix |
|---|---|
| "DeathEffect has no Particles" in the Console | Run **Tools > Effects > Build Death Particles**. |
| Nothing happens when a creature dies | It has no DeathEffect. Run the tool, or add it by hand (step 2). |
| The slime's particles come from a few lines, not its whole body | Its model isn't Read/Write, so the particles use its bones. Run the tool again, or tick Read/Write by hand. |
| The slime is gone before the particles finish / stays too long | Change **Dissolve Start** / **Dissolve Seconds**, or run the tool again to time them to its death animation. |
| Dots are too big or too small | Change **Start Size** on the DeathParticles prefab. |
| Pink squares | The particle material's shader is missing. Set it to `Legacy Shaders/Particles/Alpha Blended`. |
| A deer far away vanished with no burst | That's the AnimalSpawner taking away a deer left behind, not a death. |
| Night enemies blink out at dawn, no orange embers | The enemy prefab has no EnemyAnimator (Marko's old ball enemy: use **Tools > Enemies > Build Monster Enemy**) or no DeathEffect (run **Tools > Effects > Build Death Particles**). |
