# Enemy Setup (animated monster enemies)

**What the player sees:** at night, slimes appear on the ground around the player. Each one:
- walks (or runs) to the player and attacks;
- flinches when hit;
- falls over when killed, gives coins, and its body breaks into red particles during the fall (DeathEffect,
  `Docs/Death Effect Setup.md`);
- cheers when the player dies.

Walls and buildings stop them, and they walk up and down the hills.

**How it works:**
- **Movement, attacks, health, coins:** Marko's scripts (EnemyMovement, EnemyAttack, EnemyCombat, EnemySpawner).
- **The look:** Tony's EnemyAnimator plays the monster's animations.
- **Physics:** a Rigidbody and a Sphere Collider on the enemy make walls stop it and keep it on the ground.

The changes to Marko's scripts are listed in `Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md`.

## What you need

The **RPG Monster Duo PBR Polyart** pack (free, Asset Store), imported into `Assets/Imported3D/RPG Monster DUO PBR Polyart`.
It has a slime and a turtle-shell monster, each with 15 animations, and Standard materials (Built-in pipeline).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Enemies > Build Monster Enemy**.
   - **It makes** `Assets/Animation/Enemy/SlimeEnemy.controller`, `Assets/Prefabs/Enemy/SlimeEnemy.prefab` and
     `Assets/Prefabs/Enemy/EnemyNoFriction.physicMaterial`. It asks before replacing them. **Replace** rebuilds them
     inside the same files, so the EnemySpawner keeps its link to SlimeEnemy.
   - **It sizes the slime** to 1 m tall from its real mesh, with its bottom on the ground.
   - **It asks:** *"Make the EnemySpawner spawn SlimeEnemy instead?"* Click **Use SlimeEnemy**
     (**Ctrl + Z** undoes it).
   - **Another monster:** select its prefab in the Project window first, e.g.
     `Imported3D/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/TurtleShellPolyart`. You get `TurtleShellEnemy.prefab`.
3. Save the scene (**Ctrl + S**).

```
SlimeEnemy      Rigidbody (mass 5, rotation frozen) + Sphere Collider (no friction)
                EnemyCombat, EnemyMovement, EnemyAttack (Marko) + EnemyAnimator (Tony)
  Model         the slime: Animator with the SlimeEnemy controller, 1 m tall, feet at the bottom
```

## Test

1. Press Play. Select **EnemySpawner** in the Hierarchy, click the **⋮** on its **EnemySpawner** component, then
   **Test Spawn Enemy**. A slime appears on the ground 10 m from the player. (Or wait for night: 4 come.)
2. It walks to you, stops close, and attacks: your health goes down.
3. Hit it with left click: it flinches. Kill it: it falls over, "+N Coins" shows, and its body turns into red
   particles until it's gone. Attack in first person (**F5**): see the problems table.
4. Stand behind a wall or a building: it can't walk through.

## Settings (open SlimeEnemy.prefab)

| Component | Field | What it does |
|---|---|---|
| EnemyMovement | **Move Speed** (2) | Meters per second. The walk animation plays at 2, the run at 4, and speeds in between mix them. The player walks at 7.8. |
| | **Stop Distance** (1.5) | Stops this close to the player. Keep it smaller than Attack Range, or it never attacks. |
| | **Turn Speed** (360) | How fast it turns to face the player, in degrees per second. |
| EnemyAttack | **Attack Range** (2), **Attack Damage** (10), **Attack Cooldown** (1.5) | The EnemySpawner raises the damage each day. |
| EnemyCombat | **Max Health** (50), **Miss Chance** (5) | The EnemySpawner raises the health each day. |
| EnemyAnimator | **Blend Time** (0.15) | Seconds to blend between standing, walking and running. |
| | **Flinch When Hurt** | Plays the Get Hit animation when it loses health. |
| | **Body Stays For** (2.5) | Seconds the body stays after dying (the death animation plays in this time). With a DeathEffect on the enemy, its Dissolve Start + Dissolve Seconds decide instead. |
| | **Cheer When Player Dies** | Plays the Victory animation when the player dies. |
| Rigidbody | **Mass** (5) | Heavier than the player's ball (1), so the player can't shove it away easily. |

**Size:** select **Model** and change its **Scale**, then the Sphere Collider's **Radius** and **Center Y** to match
(Radius = half its width, Center Y = Radius). For every future build, change `Height` at the top of
`Assets/Script/Editor/EnemyBuilder.cs`.

## The animations

| State in SlimeEnemy.controller | Slime clip | When |
|---|---|---|
| Locomotion | IdleBattle (speed 0), WalkFWD (speed 2), RunFWD (speed 4) | Always, picked by how fast it moves |
| Attack | Attack01 | EnemyAttack attacks |
| Hit | GetHit | It loses health |
| Victory | Victory | The player dies |
| Dead | Die | It dies |

Not used yet: IdleNormal, WalkBWD, WalkLeft, WalkRight, Attack02, Taunt, Dizzy, SenseSomethingST / RPT.
To swap one, double click the controller, click the state and drag another clip into **Motion**. The clips are
inside `Assets/Imported3D/RPG Monster DUO PBR Polyart/Animations/Slime/*.fbx`: click the arrow on a file to see its clip.

## By hand (instead of the tool)

1. **Controller:** in `Assets/Animation/Enemy`, right click > **Create > Animator Controller**, name it **SlimeEnemy**.
   In its **Parameters** tab: Float **Speed**, Trigger **Attack**, Trigger **Hit**, Bool **Dead**, Trigger **Victory**.
2. **Locomotion:** right click > **Create State > From New Blend Tree**, rename it **Locomotion**. Double click it,
   click the Blend Tree: **Blend Type 1D**, parameter **Speed**, untick **Automate Thresholds**. Add 3 motions:
   IdleBattle (threshold 0), WalkFWD (2), RunFWD (4). The clips are in the `*_Slime_Anim.fbx` files.
3. **States:** **Attack** (Attack01), **Hit** (GetHit), **Victory** (Victory), **Dead** (Die).
4. **Transitions.** Make the Any State → Dead one first: Any State arrows are checked in the order they were made.

   | From → To | Has Exit Time | Duration | Conditions |
   |---|---|---|---|
   | Any State → Dead | off | 0.1 | Dead = true |
   | Any State → Hit / Attack / Victory | off | 0.05 | its trigger, and Dead = false |
   | Hit / Attack / Victory → Locomotion | on, Exit Time 0.9 | 0.15 | none |

   On every Any State arrow, untick **Can Transition To Self**.
5. **Prefab:**
   - In the scene: **Create Empty**, name it **SlimeEnemy**, Position 0, 0, 0.
   - Drag `Prefabs/PolyartDefault/SlimePolyart` onto it and rename it **Model**.
   - Model's Animator: Controller = **SlimeEnemy**, untick **Apply Root Motion**.
   - Scale Model so it's about 1 m tall, and move it so its feet are at height 0, in the middle.
6. **On SlimeEnemy, Add Component:**
   - **Rigidbody**: Mass 5, Interpolate **Interpolate**, Collision Detection **Continuous Dynamic**,
     Constraints **Freeze Rotation** X, Y and Z.
   - **Sphere Collider**: Radius 0.5, Center 0, 0.5, 0. For **Material**, use a Physic Material
     (right click > **Create > Physic Material**) with both frictions 0 and Friction Combine **Minimum**.
   - **EnemyCombat**, **EnemyMovement** (Move Speed 2, Stop Distance 1.5), **EnemyAttack** (Attack Range 2),
     **EnemyAnimator**.
7. Drag SlimeEnemy into `Assets/Prefabs/Enemy` (choose **Original Prefab**), then delete it from the scene.
8. Select **EnemySpawner** and drag SlimeEnemy into **Enemy Prefab**.

## Problems

| Problem | Fix |
|---|---|
| It walks backwards or sideways | Turn **Model**: Rotation Y 180 (or 90 / -90). |
| It floats or sinks | Run **Tools > Enemies > Build Monster Enemy** again (**Replace**). The first version of the tool measured Unity's loose box around the slime (1.67 m tall, reaching below its feet), so it made the slime small and lifted it. It now measures the mesh itself. By hand: move **Model** up or down so its bottom touches the bottom of the Sphere Collider. |
| It doesn't hop | It hops while it walks or runs (its WalkFWD / RunFWD animations), so it needs a target to chase. Standing still plays IdleBattle. A higher **Move Speed** (4) plays the run. |
| It stands still | Its **Target** is empty. Enemies placed by hand don't get one: spawn them with the EnemySpawner (Test Spawn Enemy), or drag the Player into Target on both EnemyMovement and EnemyAttack. |
| It stands with no animation | Model's Animator needs **Controller = SlimeEnemy**. If the Console says the monster "has no state called ...", drag those clips in by hand. |
| It never attacks | **Stop Distance** must be smaller than **Attack Range**. |
| I can't hit it in third person | Marko's PlayerCombat attack ray starts at the camera and is 2.5 m long (`PlayerCombat.cs:90`), and in third person the camera is behind the player. Attack in first person (F5). |
| It gets stuck behind a wall | It walks straight at the player (no path finding). |
| It's pink | Its materials need the **Standard** shader (the pack's already do). |
