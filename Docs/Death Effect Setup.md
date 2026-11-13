# Death Effect Setup (creatures die into particles)

When a creature dies, its body turns into particles. The color tells you what you killed:

- **Green** = harmless (the deer). It bursts into green particles the moment it dies, in the shape of its body.
- **Red** = harmful (the slime). It plays its death animation. During the animation its body breaks into red
  particles: a few at first, then more and more while the body shrinks into them, and a last pop as the animation ends.

A creature that is removed without being killed shows nothing. That includes a deer left far behind, or the game
closing.

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
- **The particles:** `Assets/Prefabs/Effects/DeathParticles.prefab`. White dots drift up a little, swirl and fade.
  DeathEffect colors them and decides where they appear.

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

On the **DeathParticles** prefab: **Start Size** = dot size, **Start Lifetime** = how long dots stay, **Gravity
Modifier** = how they drift (negative = up).

## Testing

1. Play, select a deer or slime in the Hierarchy, then click **⋮** on DeathEffect > **Test: Burst Here**. The burst
   shows without killing anything.
2. Hunt a deer: green burst, and "+Meat" as before.
3. Kill a slime at night: it falls over and breaks into red particles, then it's gone.

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
