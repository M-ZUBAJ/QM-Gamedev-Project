# Desert Setup (gold, sandstone, desert caves and the turtle)

**What the player sees in the new world's desert:**
- **Sandstone rocks** across the sand: warm, reddish desert rocks. Mining one gives **Stone** (4 - 6), but it's
  **harder to break**: 75 health, 2.5 times a normal stone rock (30). Bigger, too.
- **Desert caves** (up to 3, anywhere deep in the desert): walk-in mounds of sandstone, wider and a little lower than
  the grassland's rock caves, with a sandy floor. Inside: **2 - 3 gold rocks** (shiny gold, 2 - 4 **Gold** each) and the
  other spots sandstone rocks. Gold rocks need an **Iron Pickaxe** or better ("Needs an Iron Pickaxe or better").
  Gold makes the gold tools (Iron tool + Gold) and the gold armor (Iron piece + Gold): `Docs/Tool Setup.md`,
  `Docs/Armor Setup.md`.
- **No day monster any more (21/11/2026):** the turtles that lived on the sand in the day are now **night monsters**,
  from night 2, everywhere, with a shell they hide in (`Docs/Night Monsters Setup.md`). The desert is quiet in the
  day. (The day turtle's parts below, the Desert Monster Spawner and EnemyAggro, are kept but no scene uses them.)

## Quick way: one click

1. Menu **Tools > World > Build New World Scene**, then **Build**. It now runs **Tools > World > Build Desert Content**
   first and puts it all in the new world.
2. Play the new world (title screen: NEW GAME > Map: New world) and walk to the desert (the sandy side).

Needs: the rock shapes (**Tools > World > Use Low Poly Rocks**, once) and the **RPG Monster DUO PBR Polyart** pack (in
`Assets/Imported3D/RPG Monster DUO PBR Polyart`, for the turtle).

## What Build Desert Content makes

| What | Where | Details |
|---|---|---|
| **Gold** (item) | `Assets/Config/Items/Gold.asset` | A raw material (Item Type Material), **Mining > Mined With = Iron**. A copy of Turki's Iron item (made once, so your changes stay). |
| Gold's prefab | `Assets/Prefabs/Item/Gold.prefab` | A small gold cube (ShapeColor) with ItemPickup = Gold, copied from Turki's Iron prefab. Its photo is the inventory icon. |
| **GoldNode** | `Assets/Prefabs/ResourceNodes/Rocks/GoldNode.prefab` | A **Prefab Variant** of Turki's IronNode: Gold Ore material, 40 health, drops 2 - 4 Gold. Everything else (random rock shape, standing on the ground, breaking into pieces) comes from IronNode. |
| **SandstoneNode** | `Assets/Prefabs/ResourceNodes/Rocks/SandstoneNode.prefab` | A Prefab Variant of StoneNode: Sandstone material, **75 health**, drops 4 - 6 Stone. |
| Materials | `Assets/Materials/Rocks/Gold Ore.mat`, `Sandstone.mat` | Copies of the rock pack's sand color sheet, tinted gold (a little metal shine) and warm sandstone. Made once: change their colors by hand, or delete them to get the tool's again. |
| Rock-break dust | (Tools > Effects > Build Rock Break runs again) | The broken rocks' dust and gravel in each rock's own colors. |
| **Desert caves** | `Assets/Prefabs/World/Caves/Desert Cave Small`, `Desert Cave` | Built like the rock caves (Tools > World > Build Rock Caves' way), all of sandstone, radius 7 and 8 m, walls 4.6 and 5.2 m, with OreSpot markers inside. |
| **TurtleShellEnemy** | `Assets/Prefabs/Enemy/TurtleShellEnemy.prefab` (+ `Assets/Animation/Enemy/TurtleShellEnemy.controller`) | The TurtleShell made into an enemy like the slime (Tools > Enemies > Build Monster Enemy's way: Marko's EnemyCombat / EnemyMovement / EnemyAttack, Tony's EnemyAnimator and DeathEffect). Since 21/11/2026 built as a **night monster** (NightMonsterBuilder: no EnemyAggro, orange embers at dawn, **ShellDefense**), the same as Tools > Enemies > Set Up Night Monsters. |

Run it again any time: everything is refilled in place, and the new world keeps its links to it.

## How the new world uses it

| Part | What it does |
|---|---|
| **WorldGenerator** (World Generator) | **Desert Cave Prefabs**, **Desert Cave Count** (3), **Gold Ore**, **Sandstone Ore**, **Gold Per Cave** (2 - 3): picks spots deep in the desert (desert 90 % or more, gentle ground, away from the barrier, Cave Spacing from every cave), flattens them, places a desert cave on each and fills its OreSpots: gold first, the rest sandstone. The floor is painted sand and dirt (too little of either for the spawners' rocks to grow inside). |
| **ObjectScatter** (Sanyam's, the new scene's copy) | A new rule **Sandstone Rocks**: 40 SandstoneNodes on the sand texture (min strength 0.6, max slope 30, spacing 16, size 1 - 1.6). His Desert Rocks rule (plain stone) is still left out of the new world. |
| **Desert Monster Spawner** (DesertMonsterSpawner) | Not used since 21/11/2026 (Set Up Night Monsters removes it from the new world). It kept up to 5 turtles on the sand around the player in the day (see below). |
| **EnemyAggro** (not on the turtle any more) | Gives Marko's EnemyMovement / EnemyAttack the player as target only while the player is near (Notice Range 14 m) or after being hit; takes it away beyond Give Up Range (32 m) or when the player dies, and stops the turtle. |

## Desert Monster Spawner settings (not used since 21/11/2026; kept for a future day monster)

| Setting | What it does |
|---|---|
| **Monster Prefab** | the monster (TurtleShellEnemy) |
| **Population** (5), **Spawn Every Seconds** (6) | how many live around the player at once; how often it checks (removes far ones, adds one missing) |
| **Terrain Layer**, **Min Strength**, **Max Slope** | where: the sand texture's number (set by the tool), how strongly painted, how steep at most |
| **Min Distance** (25), **Max Distance** (90), **Beyond Sight** (25), **Despawn Distance** (170) | how near / far from the player they appear (always out of sight: PlayerSight), and when far ones are taken away |
| **Only In Day**, **Leave At Night** | day monsters: none at night, and the ones there dissolve when night comes |
| **Health** (90) + **Health Per Day** (10), **Damage** (12) + **Damage Per Day** (2) | how strong, growing each day like the night slimes |

⋮ > **Test: Spawn One Now** (while playing, in the desert) brings one at once.

## Doing it by hand

1. **Gold:** duplicate `Assets/Config/Items/Iron` (Ctrl + D), rename it **Gold**, set **Mining > Mined With = Iron**.
   Duplicate `Assets/Prefabs/Item/Iron`, rename it **Gold**, give its ShapeColor a gold color and its ItemPickup's
   **Data** = Gold; drag that prefab into the Gold item's **Prefab**.
2. **GoldNode / SandstoneNode:** right click IronNode (StoneNode) > **Create > Prefab Variant**, name it, move it into
   `ResourceNodes/Rocks`, give its Mesh Renderer the new material, and in its ResourceNode set **Drop Item**, **Max
   Health** and **Drop Min / Max**. Then **Tools > Effects > Build Rock Break**.
3. **Desert caves:** a rock cave prefab with the Sandstone material on every rock (or run the tool).
4. **The turtle:** see `Docs/Night Monsters Setup.md` (it's a night monster now).
5. **In ProceduralGenerateScene:** World Generator > drag the desert caves, GoldNode and SandstoneNode into Desert Cave
   Prefabs, Gold Ore and Sandstone Ore. Add an ObjectScatter rule for SandstoneNode on the sand texture. Save.

## Testing

1. Play the new world; the Console says "... 9 caves (3 of them in the desert, with gold)".
2. Walk into the desert in the day: sandstone rocks on the sand; punch one: it takes about 2.5 times as long as a stone
   rock, and gives Stone.
3. No turtles in the day (they come at night from night 2: `Docs/Night Monsters Setup.md`).
4. Find a desert cave (a sandstone mound): inside, gold rocks shine. With a Stone Pickaxe: "Needs an Iron Pickaxe or
   better"; with an Iron Pickaxe they break into Gold.

## Problems

| Problem | Why / fix |
|---|---|
| Turtles in the desert in the day | The new world was made before 21/11/2026: run Tools > Enemies > Set Up Night Monsters (it removes the Desert Monster Spawner). |
| No desert caves (the Console says "0 of them in the desert") | The World Generator has no Desert Cave Prefabs (run Build New World Scene again), or the desert had no gentle spot (rare: try another game). |
| Gold rocks can't be mined | That's on purpose until you have an Iron Pickaxe (Gold's Mined With = Iron). |
| No Gold item | Run Build Desert Content (it copies Turki's Iron item; if there's no Iron item it makes an empty one). |
