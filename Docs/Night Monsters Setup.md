# Night Monsters Setup (a new monster every night, the turtle's shell, monsters out of sight)

**What the player sees:**
- **Night 1:** slimes. **Night 2:** slimes **and turtles**. **Night 3:** slimes, turtles and the night-3 monster (once
  you add one). Each night can bring in a new monster, and the ones from before keep coming.
- **Monsters never pop up right in front of you.** They appear 24 - 34 m away, where you can't see them: behind you,
  or behind a hill or a tree. Before, they appeared 10 m away in any direction, so turning around could show one
  right there.
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
- **Each game scene** (GameScene, ProceduralGenerateScene): **NightMonsters** on the EnemySpawner's object with the
  **Turtle** from night 2, and the new world's **Desert Monster Spawner** removed. Then saved.

Run it again any time: the turtle and the sparks are rebuilt; your numbers on ShellDefense and NightMonsters stay.
Tools > World > Build Desert Content builds the turtle the same way (no day monster any more).

## How it works

| Part | Where | What it does |
|---|---|---|
| **EnemySpawner** (Marko's) | the EnemySpawner object | spawns the night's monsters every few seconds (night 1: 10 s, night 2: 7 s, nights 3 - 5: 5 s) and removes them at dawn. `[Tony]` lines ask NightMonsters which monster, where, and how strong |
| **NightMonsters** | next to the EnemySpawner | **Pick:** night 1 = the EnemySpawner's **Enemy Prefab** (the slime) only; from a monster's **First Night** on, its **Share** of the spawns is that monster, while fewer than its **Max Alive** are out. **Prepare:** that monster's own Health and Damage. **FindSpot:** a spot 24 - 34 m away the player can't see (PlayerSight), inside the world barrier |
| **ShellDefense** | on the turtle | fights, hides, fights...; while hidden: Marko's EnemyMovement / EnemyAttack off, the body stops. `Blocks` / `Deflect`: a melee hit on the shell bounces off |
| **PunchEffect** | the Player | when a melee swing lands, asks ShellDefense first: a hit on a shell bounces off (no damage, no wind burst, no push) |
| **EnemyAnimator** | each monster | the **Shell** (Defend) and **ShellHit** (DefendGetHit) animations |
| **FloatingText** | (made by itself) | "BLOCKED!" over the turtle: pops up, rises, fades, drawn over everything |
| **FirstPersonHands** | the first-person hands | **Recoil**: the hands jump back when a hit bounces off |
| **PlayerSight** | the Player | "could the player see a monster here?" (on screen, in range, nothing in the way); 20 m or closer always counts as seen |

Change log for Marko's EnemySpawner: `Docs/TonyEditMate/Marko/21-11-2026 Night Monsters And The Turtle Shell.md`.

## A new monster for night 3

1. Make it an enemy: select the monster prefab (with an Animator) in the Project window, then **Tools > Enemies >
   Build Monster Enemy** (`Docs/Enemy Setup.md`). You get `Assets/Prefabs/Enemy/<Name>Enemy.prefab`.
2. Select the EnemySpawner in GameScene > **NightMonsters** > **Monsters** > **+**: Name, drag the prefab into
   **Prefab**, **First Night** 3, and its Share / Max Alive / Health / Damage (see Settings).
3. Save GameScene, then **Tools > World > Build New World Scene** (the new world is a copy of GameScene), or make the
   same change in ProceduralGenerateScene.

Keep the Shares of all monsters below 1 together (the rest of the spawns are slimes).

## By hand (instead of the tool)

1. Turtle: select `Imported3D/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/TurtleShellPolyart`, **Tools >
   Enemies > Build Monster Enemy** (its controller gets the Shell states by itself). Open `TurtleShellEnemy.prefab`:
   remove **EnemyAggro**, add **ShellDefense**, drag `Assets/Prefabs/Effects/ShellDeflect.prefab` into **Deflect
   Effect**; on its **DeathEffect** set Vanish Color orange (1, 0.5, 0.08) and Vanish Rise 0.35.
2. Each game scene: select the EnemySpawner > **Add Component > NightMonsters** > Monsters + > the turtle (as in "A
   new monster" above, First Night 2). In ProceduralGenerateScene delete **Desert Monster Spawner**. Save.

## Settings

| Where | Setting | What it does |
|---|---|---|
| NightMonsters (EnemySpawner object) | **Monsters** | each monster after the first night's: **First Night**, **Share** (0.3 = 3 in 10 of the spawns) + **Share Per Night**, **Max Alive** (3) + **Max Alive Per Night** (1), **Health** (90) + **Health Per Night** (15), **Damage** (12) + **Damage Per Night** (2); Health / Damage 0 = the spawner's (50 + 10 / 10 + 2 per day passed) |
| | **Min / Max Distance** (24 / 34 m) | how far from the player monsters appear |
| | **Out Of Sight** (on), **Tries** (16) | only where the player can't see; tries before taking the spot most behind the player |
| ShellDefense (turtle prefab) | **Fight Seconds** (6) + **Fight Jitter** (1.5) | how long it fights near the player before hiding (counted only within **Fight Range**, 10 m) |
| | **Shell Seconds** (3) | how long it hides |
| | **Hide When Hurt Chance** (0.33) | the chance it hides at once when hurt |
| | **Shell Damage** (15) | what a melee hit on the shell does to the player |
| | **Deflect Effect**, **Blocked Text**, **Blocked Color** | the sparks and the word |
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

## Problems

| Problem | Why / fix |
|---|---|
| No turtles at night | It's night 1 (turtles start on night 2), the EnemySpawner has no NightMonsters (run the tool), or 3 turtles are already out (Max Alive). |
| Monsters still appear near me | The EnemySpawner object has no NightMonsters, or the Player has no **PlayerSight** (Tools > World > Set Up World Spawning): without it only the screen is checked. |
| Hitting the shell does nothing at all | The turtle has no ShellDefense (run the tool), or the Player has no PunchEffect (Tools > Effects > Build Punch Wind Burst): the shell check is in PunchEffect. |
| The turtle never hides | You're farther than Fight Range (10 m) from it, or it was built before the tool: run the tool. |
| No "BLOCKED!" text | TextMeshPro's Essential Resources are missing (Window > TextMeshPro > Import TMP Essential Resources). |
| Turtles stand still and wait | Their prefab still has EnemyAggro (the old day monster's): run the tool. |
