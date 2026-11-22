# Night Monsters Setup (a new monster every night: turtles, chest monsters, flying beholders; monsters out of sight)

**What the player sees:**
- **Night 1:** slimes. **Night 2:** slimes **and turtles**. **Night 3:** + **chest monsters** (3 kinds). **Night 4 and
  later:** + **beholders**, which fly (4 kinds; nights 5 and after keep these 4 for now). Each night can bring in a
  new monster, and the ones from before keep coming.
- **The chest monster** (22/11/2026, RPG Monster Partners' mimic): a treasure chest with one big eye, teeth and four
  legs, 1.1 m tall. It walks up and bites like the slime, but it's tougher (120 health on night 3, the turtle 105),
  bites harder (15) and is a bit faster.
- **The beholder** (22/11/2026, RPG Monster Partners' floating eye with tentacles, 1.3 m): it **flies**. Away from
  you it cruises **3.5 m above the ground**, too high for a sword or a punch, so shoot it with the bow (or the
  fireball). Within 7 m it **dives down to your face to bite**, then climbs back up and backs away for a moment and
  comes again. Hit it while it's down. It has less health (70 on night 4) and a lighter bite (12), but keeps coming
  back. It flies over trees and rocks, bobs gently, and when it dies or the sun comes up it dissolves in the air.
- **Monsters never pop up right in front of you.** They appear 24 - 34 m away, where you can't see them: behind you,
  or behind a hill or a tree. Before, they appeared 10 m away in any direction, so turning around could show one
  right there.
- **At most 20 at once** (22/11/2026), every kind together, and one left farther than 90 m behind you, out of sight, is
  taken away quietly (a new one comes nearer). Before, nothing stopped them: nights 3 - 5 brought one every 5 s all
  night (about 55 by dawn) and the game got slower and slower.
- **The turtle** (RPG Monster DUO's TurtleShell) fights for about 6 seconds near you, then **pulls into its shell** for
  3 seconds: it stops walking and attacking. When you hurt it, there's also a 1 in 3 chance it hides at once.
- **Hitting the shell:** a punch, sword, axe or pickaxe hit on a turtle in its shell **does nothing to it and hurts
  you**: 15 damage, the shell's own damage, not your weapon's (your armor takes its share off). You can see it didn't
  go in:
  - sparks fly off the shell where you hit it, with a white flash;
  - **BLOCKED!** pops up over the turtle;
  - the turtle's shell shakes (its DefendGetHit animation);
  - in first person your hands bounce back; in third person your character flinches.
- **The fireball** is a projectile, not melee: it still hurts a turtle in its shell.
- **At dawn** turtles burn away into orange embers like the slimes. The desert has **no day monster** any more (the
  turtles used to live on the sand in the day).

## Quick way: one click

1. Menu **Tools > Enemies > Set Up Night Monsters**, then **Build**. It saves your open scenes first.
2. That's it: it changes and saves both game scenes itself, then opens your scenes again.

The tool:
- **The turtle:** rebuilds `Assets/Prefabs/Enemy/TurtleShellEnemy.prefab` (and its controller) with the shell
  animations, takes off **EnemyAggro** (it hunted only when you came near: night monsters come for you), gives it the
  night's orange embers at dawn, and adds **ShellDefense** with the sparks.
- **The sparks:** `Assets/Prefabs/Effects/ShellDeflect.prefab` (bright stretched sparks + a white flash; material in
  `Assets/Effects/ShellDeflect`).
- **The chest monster** (22/11/2026): `Assets/Prefabs/Enemy/ChestMonsterEnemy.prefab` (and
  `Assets/Animation/Enemy/ChestMonsterEnemy.controller`) from RPG Monster Partners' `ChestMonsterPolyartDefault`, 1.1 m
  tall, Move Speed 2.5, the night's orange embers at dawn.
- **The beholder** (22/11/2026): `Assets/Prefabs/Enemy/BeholderEnemy.prefab` (and its controller) from
  `BeholderPolyartDefault`, 1.3 m tall, Move Speed 3.5, no gravity, its collider around its body (not at its feet),
  and **FlyingEnemy**; the night's embers at dawn.
- **Each game scene** (GameScene, ProceduralGenerateScene): **NightMonsters** on the EnemySpawner's object with the
  **Turtle** (night 2), the **Chest Monster** (night 3) and the **Beholder** (night 4), and the new world's **Desert
  Monster Spawner** removed. Then saved.

Run it again any time: the monsters and the sparks are rebuilt; your numbers on ShellDefense, FlyingEnemy and
NightMonsters stay (a monster's line is only added when it isn't there). Without RPG Monster Partners PBR Polyart in
the project only the turtle is set up (the Console says so). Tools > World > Build Desert Content builds the turtle the
same way (no day monster any more).

## How it works

| Part | Where | What it does |
|---|---|---|
| **EnemySpawner** (Marko's) | the EnemySpawner object | spawns the night's monsters every few seconds (night 1: 10 s, night 2: 7 s, nights 3 - 5: 5 s) and removes them at dawn. `[Tony]` lines ask NightMonsters which monster, where, and how strong |
| **NightMonsters** | next to the EnemySpawner | **Pick:** night 1 = the EnemySpawner's **Enemy Prefab** (the slime) only; from a monster's **First Night** on, its **Share** of the spawns is that monster, while fewer than its **Max Alive** are out. **Prepare:** that monster's own Health and Damage. **FindSpot:** a spot 24 - 34 m away the player can't see (PlayerSight), inside the world barrier. **MostAtOnce** (22/11/2026): the EnemySpawner asks it before each spawn (20 alive at most); every 2 s a monster it spawned that's past **Leave Distance** (90 m) and out of sight is removed |
| **ShellDefense** | on the turtle | fights, hides, fights...; while hidden: Marko's EnemyMovement / EnemyAttack off, the body stops. `Blocks` / `Deflect`: a melee hit on the shell bounces off |
| **PunchEffect** | the Player | when a melee swing lands, asks ShellDefense first: a hit on a shell bounces off (no damage, no wind burst, no push) |
| **EnemyAnimator** | each monster | the **Shell** (Defend) and **ShellHit** (DefendGetHit) animations; **LastAttackTime** (when it last bit) |
| **FlyingEnemy** | on the beholder | makes it fly: gravity off; Marko's EnemyMovement still steers it toward you over the ground and his EnemyAttack still bites, FlyingEnemy only sets its height (Cruise Height above the ground under it, ignoring trees and rocks; within Dive Distance down to Bite Height above your middle; for Climb Seconds after each bite up and away). Stops when it dies, is pushed by a punch (Knockback) or burns away |
| **FloatingText** | (made by itself) | "BLOCKED!" over the turtle: pops up, rises, fades, drawn over everything |
| **FirstPersonHands** | the first-person hands | **Recoil**: the hands jump back when a hit bounces off |
| **PlayerSight** | the Player | "could the player see a monster here?" (on screen, in range, nothing in the way); 20 m or closer always counts as seen |

Change log for Marko's EnemySpawner: `Docs/TonyEditMate/Marko/21-11-2026 Night Monsters And The Turtle Shell.md`.

## A new monster for a later night (night 5...)

1. Make it an enemy: select the monster prefab (with an Animator) in the Project window, then **Tools > Enemies >
   Build Monster Enemy** (`Docs/Enemy Setup.md`). You get `Assets/Prefabs/Enemy/<Name>Enemy.prefab`. To make it fly,
   add **FlyingEnemy** to it and untick its Rigidbody's **Use Gravity** (the Beholder's way).
2. Select the EnemySpawner in GameScene > **NightMonsters** > **Monsters** > **+**: Name, drag the prefab into
   **Prefab**, **First Night** 5, and its Share / Max Alive / Health / Damage (see Settings).
3. Save GameScene, then make the same change in ProceduralGenerateScene (or run Tools > World > Build New World Scene:
   the new world is a copy of GameScene).

Keep the Shares low: they're taken in the list's order, and what's left of each roll is a slime. On night 4 the
turtle has 0.4, the chest monster 0.25, the beholder 0.15: the slimes still get 0.2. On night 5 they add up to 0.95.

## By hand (instead of the tool)

1. Turtle: select `Imported3D/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/TurtleShellPolyart`, **Tools >
   Enemies > Build Monster Enemy** (its controller gets the Shell states by itself). Open `TurtleShellEnemy.prefab`:
   remove **EnemyAggro**, add **ShellDefense**, drag `Assets/Prefabs/Effects/ShellDeflect.prefab` into **Deflect
   Effect**; on its **DeathEffect** set Vanish Color orange (1, 0.5, 0.08) and Vanish Rise 0.35.
2. Chest monster: select `Imported3D/RPGMonsterPartnersPBRPolyart/Prefabs/Character/ChestMonsterPolyartDefault`,
   **Tools > Enemies > Build Monster Enemy** (1 m tall: scale its Model to 1.1 m if you like). On `ChestMonsterEnemy`:
   EnemyMovement **Move Speed** 2.5; DeathEffect Vanish Color / Rise as for the turtle.
3. Beholder: the same with `BeholderPolyartDefault`; on `BeholderEnemy`: Rigidbody **Use Gravity** off, its Sphere
   Collider's **Center Y** about half its height, **Add Component > FlyingEnemy**, EnemyMovement **Move Speed** 3.5;
   DeathEffect as for the turtle.
4. Each game scene: select the EnemySpawner > **Add Component > NightMonsters** > Monsters + > the turtle (as in "A
   new monster" above, First Night 2), then the chest monster (First Night 3: Share 0.2, Max Alive 2, Health 120 + 20,
   Damage 15 + 2) and the beholder (First Night 4: Share 0.15, Max Alive 2, Health 70 + 15, Damage 12 + 2). In
   ProceduralGenerateScene delete **Desert Monster Spawner**. Save.

## Settings

| Where | Setting | What it does |
|---|---|---|
| NightMonsters (EnemySpawner object) | **Monsters** | each monster after the first night's: **First Night**, **Share** (0.3 = 3 in 10 of the spawns) + **Share Per Night**, **Max Alive** (3) + **Max Alive Per Night** (1), **Health** (90) + **Health Per Night** (15), **Damage** (12) + **Damage Per Night** (2); Health / Damage 0 = the spawner's (50 + 10 / 10 + 2 per day passed). The tool's lines: Turtle night 2, 0.3 + 0.05, 3 + 1, 90 + 15, 12 + 2; Chest Monster night 3, 0.2 + 0.05, 2 + 1, 120 + 20, 15 + 2; Beholder night 4, 0.15 + 0.05, 2 + 1, 70 + 15, 12 + 2 |
| | **Most At Once** (20) | (22/11/2026) the most night monsters alive together, every kind; more = harder, but slower (each walks, animates and pushes the others). Marko's EnemySpawner > **Max Enemy Number** (40) caps it too, whichever is lower |
| | **Leave Distance** (90 m) | a monster farther than this from you, out of sight, is taken away quietly (no coins); 0 = never |
| | **Min / Max Distance** (24 / 34 m) | how far from the player monsters appear |
| | **Out Of Sight** (on), **Tries** (16) | only where the player can't see; tries before taking the spot most behind the player |
| ShellDefense (turtle prefab) | **Fight Seconds** (6) + **Fight Jitter** (1.5) | how long it fights near the player before hiding (counted only within **Fight Range**, 10 m) |
| | **Shell Seconds** (3) | how long it hides |
| | **Hide When Hurt Chance** (0.33) | the chance it hides at once when hurt |
| | **Shell Damage** (15) | what a melee hit on the shell does to the player |
| | **Deflect Effect**, **Blocked Text**, **Blocked Color** | the sparks and the word |
| FlyingEnemy (beholder prefab) | **Cruise Height** (3.5 m) | how high it flies above the ground while it's away from you (its lowest point); a sword reaches about 2.5 m from your eyes |
| | **Bite Height** (0.4 m) | how far above your middle it comes down to bite (its eye about at your face) |
| | **Climb Speed** (3 m/s) | the fastest it climbs or comes down |
| | **Dive Distance** (7 m) | how close over the ground before it dives at you |
| | **Climb Seconds** (1.6), **Back Off Speed** (2.5 m/s) | after each bite it climbs and backs away this long, this fast (0 = it stays down) |
| | **Bob Height** (0.15 m), **Bob Speed** (0.8 a second) | the gentle hover |
| ChestMonsterEnemy / BeholderEnemy | EnemyMovement **Move Speed** (2.5 / 3.5), EnemyAttack **Attack Range** (2) | how fast it walks / flies, how close it bites (Marko's numbers on Tony's prefabs) |
| FirstPersonHands | **Recoil Distance** (0.1 m), **Recoil Seconds** (0.3) | the hands bouncing back |
| EnemySpawner (Marko's) | **Enemy Prefab**, night spawn intervals | the first night's monster and how often monsters come |

## Testing

1. Run the tool, then Play (GameScene or from the title screen).
2. Select the sun (Directional Light > **DaylightCycle**), set **Day** to 2, drag **Time Of Day** to 0.8: night 2.
   Monsters come, about 3 in 10 of them turtles (at most 3 at once).
3. Turn around quickly a few times: no monster appears right in front of you; they walk in from the dark.
4. Walk up to a turtle and fight it: after a few seconds it pulls into its shell and stops. Punch it: sparks,
   **BLOCKED!**, its shell shakes, your hands bounce back and you lose 15 health (less with armor). Wait 3 seconds:
   it comes out and fights again.
5. Shoot a fireball (Q with a sword) at a turtle in its shell: it's hurt.
6. Or select a turtle while playing > ShellDefense > **⋮ > Test: Hide In Shell**.
7. Drag Time Of Day to 0.3 (morning): the turtles burn away into orange embers with the slimes.
8. Set **Day** to 3 and Time Of Day to 0.8: chest monsters come too (at most 2 at once). Fight one: it takes more hits
   than a slime.
9. Set **Day** to 4, Time Of Day 0.8: beholders come, flying high. Swing at one up there: nothing (too high); shoot it
   with the bow. Let it come: it dives to your face, bites, climbs away and comes back; hit it with the sword while
   it's down. Select one in the Hierarchy to see its Cruise Height (cyan) and Dive Distance (red ring) in the Scene view.
10. Kill a beholder up high: it dissolves into red particles in the air. At dawn it burns away up there too.

## Problems

| Problem | Why / fix |
|---|---|
| No turtles at night | It's night 1 (turtles start on night 2), the EnemySpawner has no NightMonsters (run the tool), or 3 turtles are already out (Max Alive). |
| Fewer monsters than before, late at night | On purpose since 22/11/2026: at most **Most At Once** (20) alive together. Raise it for a harder night (the game gets slower). |
| The game gets slower and slower at night | Marko's spawner is older than 22/11/2026 (its Max Enemy Number didn't stop spawns), or Most At Once is set very high. |
| Monsters still appear near me | The EnemySpawner object has no NightMonsters, or the Player has no **PlayerSight** (Tools > World > Set Up World Spawning): without it only the screen is checked. |
| Hitting the shell does nothing at all | The turtle has no ShellDefense (run the tool), or the Player has no PunchEffect (Tools > Effects > Build Punch Wind Burst): the shell check is in PunchEffect. |
| The turtle never hides | You're farther than Fight Range (10 m) from it, or it was built before the tool: run the tool. |
| No "BLOCKED!" text | TextMeshPro's Essential Resources are missing (Window > TextMeshPro > Import TMP Essential Resources). |
| Turtles stand still and wait | Their prefab still has EnemyAggro (the old day monster's): run the tool. |
| No chest monsters or beholders | It's before night 3 / 4, RPG Monster Partners PBR Polyart isn't in the project (the tool's Console line says "no monster prefab called ..."), or the scene's NightMonsters has no line for them: run the tool. |
| The beholder walks on the ground | Its prefab has no FlyingEnemy (or its Rigidbody still uses gravity): run the tool. |
| I can't hit the beholder | It's 3.5 m up on purpose: shoot it, use the fireball, or hit it when it comes down to bite. Lower its **Cruise Height** to make it easier. |
| The beholder never bites | Its **Bite Height** is too high for Marko's **Attack Range** (2 m between its lowest point and your middle), or **Dive Distance** is 0. |
