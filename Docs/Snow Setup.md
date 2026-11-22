# Snow Setup (the snow biome: diamonds, snow caves, snowy pines and the best chests)

**What the player sees in the new world's snow:**
- **Where:** the farthest biome. The snow is never closer than 700 m to the start (usually about 750 - 800 m away), in
  the cold areas of the world's climate; about a quarter of the land out there is snow. The new world is endless, so
  there's always more of it further on (`Docs/New World Setup.md`).
- **Rolling snowy hills** (about 40 - 65 m high, easy to walk) painted white, with groves of **snowy pines and
  spruces** (dark winter greens against the snow) and **frosty rocks** (light grey, they give Stone like a normal
  stone rock).
- **Spruce wood** (22/11/2026): the snow's trees (all four kinds) give **Spruce Wood** instead of Wood (the same 3 - 5
  a tree; Turki's Wood log in a darker spruce brown) and they're tough: they need an **Iron Axe or better** (a hand, a
  Wooden or Stone Axe or any other tool says "Needs an Iron Axe or better", and a fireball doesn't burn them either)
  and take **300 damage** to chop down, 10 times a grassland tree's 30: 8 swings with an Iron Axe, 7 with Gold, 5 with
  Diamond (10 stamina a swing). Spruce Wood makes the **Spruce Bow** (the Wooden Bow + 20 Spruce Wood + 15 Iron: faster
  and stronger, `Docs/Bow Setup.md`). Like every tree they can still drop an Apple sometimes (TreeNode's bonus).
- **Snow caves:** walk-in mounds of frosty rock, a little taller than the grassland's caves. Every 256 m square of snow
  gets 2 tries for one, each a **70 %** chance (the grassland has 45 %, the desert 50 %), so more caves than anywhere
  else. Inside: **2 - 3 diamond rocks** (shiny blue-white, harder: 50 health; **1 - 3 Diamonds** each), **2 - 3 gold
  rocks** (2 - 4 Gold each, an Iron Pickaxe or better) and the other spots stone. Diamond rocks need a **Gold Pickaxe**
  or better ("Needs a Gold Pickaxe or better"). **Lucky caves:** 1 snow cave in 20 (5 %) has **4 diamond rocks**
  instead (since 21/11/2026).
- **Diamonds** make the **Diamond Pickaxe, Axe and Sword**: the Gold one + 3 Plank + 6 Diamond (the sword 9 Diamond),
  the best tools (`Docs/Tool Setup.md`), and the **diamond armor**: each piece = its Gold piece + 2 - 6 Diamonds, 40 %
  for the whole set, +8 coins per monster killed and every chest 10 % cheaper (`Docs/Armor Setup.md`).
- **More and better chests than the desert** (for every 400,000 m² of their ground):

  | Box | Desert | Snow |
  |---|---|---|
  | Common | 30 | **40** |
  | Rare | 12 | **20** |
  | Epic | 4 | **8** |
  | Legendary (new chest system only) | 2 | **4** |

  On one 256 m square of snow that's about 6 - 7 common boxes, 3 rare, 1 epic and a 2 in 3 chance of a legendary.
- **Nothing grows on the rocky hills' snowy tops** (above the tree line): the snow's trees, rocks and chests are only in
  the snow biome itself, so its loot stays far from the start.
- Night monsters come here too (slimes, turtles from night 2).

## Quick way: one click

1. Menu **Tools > World > Build New World Scene**, then **Build**. It runs **Tools > World > Build Snow Content** (after
   the rock caves and the desert's content) and puts it all in the new world.
2. Play the new world (title screen: NEW GAME > Map: New world) and walk far out, or use the test jump below.

Needs (once, already done in this project): the rock shapes (**Tools > World > Use Low Poly Rocks**), TreeNode as a
low-poly tree (**Tools > World > Use Low Poly Grassland Trees**), the tier tools (**Tools > Items > Build Tier Tools**)
and the loot boxes (**Tools > World > Build Loot Chests**: the snow's rare, epic and legendary rules are copies of the
desert's).

## What Build Snow Content makes

| What | Where | Details |
|---|---|---|
| **Diamond** (item) | `Assets/Config/Items/Diamond.asset` | A raw material (Item Type Material), **Mining > Mined With = Gold**. A copy of Turki's Iron item, like Gold (made once, so your changes stay). |
| Diamond's prefab | `Assets/Prefabs/Item/Diamond.prefab` | A small ice-blue cube (ShapeColor) with ItemPickup = Diamond, copied from Turki's Iron prefab. Its photo is the inventory icon. |
| **Spruce Wood** (item, 22/11/2026) | `Assets/Config/Items/SpruceWood.asset` | A raw material, Display Name "Spruce Wood" (the Item Id recipes use is `SpruceWood`), **Mining > Mined With = Iron**: a tree that drops it needs an Iron Axe or better (Tony's ToolRules reads a wood's Mined With as the axe its tree needs, like an ore's for the pickaxe). A copy of Turki's Wood item (made once, so your changes stay; Mined With is only set while it's None). |
| Spruce Wood's prefab | `Assets/Prefabs/Item/SpruceWood.prefab` | Turki's Wood log copied, its ShapeColor a darker spruce brown (0.36, 0.24, 0.13; Wood is 0.52, 0.33, 0.22), ItemPickup = Spruce Wood. |
| **DiamondNode** | `Assets/Prefabs/ResourceNodes/Rocks/DiamondNode.prefab` | A **Prefab Variant** of Turki's IronNode: Diamond Ore material, **50 health**, drops 1 - 3 Diamond. Everything else (random rock shape, standing on the ground, breaking into pieces) comes from IronNode. |
| **StoneNode Snow** | `Assets/Prefabs/ResourceNodes/Rocks/StoneNode Snow.prefab` | A Prefab Variant of StoneNode in the Snow Rock material; its health and Stone drops stay StoneNode's. |
| Materials | `Assets/Materials/Rocks/Diamond Ore.mat`, `Snow Rock.mat` | Copies of the rock pack's grey color sheet (StoneNode's), tinted shiny blue-white (smoothness 0.85) and frosty light grey. Made once: change their colors by hand, or delete them to get the tool's again. |
| Rock-break dust | (Tools > Effects > Build Rock Break runs again) | The broken rocks' dust and gravel in each rock's own colors. |
| **Snow caves** | `Assets/Prefabs/World/Caves/Snow Cave Small`, `Snow Cave`, `Snow Cave Big` | Built like the rock caves (Tools > World > Build Rock Caves' way), all of Snow Rock, radius 7 / 7.5 / 8.5 m, walls 5.2 / 5.8 / 6.4 m, with OreSpot markers inside. |
| **Snowy conifers** | `Assets/Prefabs/ResourceNodes/Trees/TreeNode Snow Pine`, `TreeNode Tall Snow Pine`, `TreeNode Snow Spruce`, `TreeNode Tall Snow Spruce` | Prefab Variants of Turki's TreeNode (its drop amounts, Apple bonus and falling), showing the tree pack's pines (Tree Type1 04 / 05) and spruces (Tree Type2 04 / 05) in the pack's winter **Cold** colors, 10 / 13 / 10 / 12.5 m tall. Since 22/11/2026 each one's ResourceNode has **Drop Item = Spruce Wood** and **Max Health = 300** (set every run); Spruce Wood's Mined With makes them need an Iron Axe or better. Then **Tools > Effects > Build Tree Fall** runs again, so their trunks are closed when they fall. |
| Diamond tools' recipes | `Assets/Config/Items/Tools/DiamondPickaxe`, `DiamondAxe`, `DiamondSword` | Like **Tools > Items > Update Tool Recipes** (without asking): with the Diamond item the Diamond tools get their recipes, and Turki's crafting list is sorted again so they show in it. |

Run it again any time: everything is refilled in place, and the new world keeps its links to it.

**Since 22/11/2026 (spruce wood): run Build Snow Content once** (or Build New World Scene). The new world's snow trees
then drop Spruce Wood, have 300 health and need an Iron Axe or better; the scene needs no change (its spawn rules
point at the same tree prefabs).

## How the new world uses it

| Part | What it does |
|---|---|
| **WorldGenerator** (World Generator) | **Snow Cave Prefabs**, **Snow Cave Chance** (0.7), **Diamond Ore**, **Diamond Per Cave** (2 - 3), **Snow Gold Per Cave** (2 - 3, the desert's Gold Ore), **Lucky Cave Chance** (0.05) / **Lucky Count** (4): each new 256 m square of land gets 2 tries for a cave; a try that finds gentle ground that's all snow gets one with this chance. The ground is flattened, a snow cave placed and its 7 OreSpots filled in a random order: diamonds first (4 in a lucky cave), then gold, the rest Stone Ore. The floor is painted snow and dirt (too little of either for the spawners' rocks, trees and chests to grow inside). The snow itself: **Snow From** (700 m), **Climate Size** (1100 m). |
| **ObjectScatter** (Sanyam's, the new scene's copy) | The snow's rules, all on the snow texture (the World Generator grows them only in the snow biome): **ChestsSnow** 40, **Rare Chests Snow** 20, **Epic Chests Snow** 8, **Legendary Chests Snow** 4 (copies of the desert's chest rules, right after them in the list), **Trees: Snow Pine / Tall Snow Pine / Snow Spruce / Tall Snow Spruce** (120 each, min strength 0.7, max slope 25, spacing 9, one forest with the other trees, in patches), **Snow Rocks** (40 StoneNode Snow, spacing 14, size 0.8 - 1.5). A count is how many for every Rules Area (400,000 m²) of snow. |

## Doing it by hand

1. **Diamond:** duplicate `Assets/Config/Items/Iron` (Ctrl + D), rename it **Diamond**, set **Mining > Mined With =
   Gold**. Duplicate `Assets/Prefabs/Item/Iron`, rename it **Diamond**, give its ShapeColor an ice-blue color and its
   ItemPickup's **Data** = Diamond; drag that prefab into the Diamond item's **Prefab**.
2. **DiamondNode / StoneNode Snow:** right click IronNode (StoneNode) > **Create > Prefab Variant**, name it, move it
   into `ResourceNodes/Rocks`, give its Mesh Renderer the new material, and in DiamondNode's ResourceNode set **Drop
   Item** Diamond, **Max Health** 50, **Drop Min / Max** 1 / 3. Then **Tools > Effects > Build Rock Break**.
3. **Snow caves:** a rock cave prefab with the Snow Rock material on every rock (or run the tool).
4. **Spruce Wood:** duplicate `Assets/Config/Items/Wood`, rename it **SpruceWood**, Display Name "Spruce Wood",
   **Mining > Mined With = Iron** (its trees then need an Iron Axe or better).
   Duplicate `Assets/Prefabs/Item/Wood`, rename it **SpruceWood**, give its ShapeColor a darker brown and its
   ItemPickup's **Data** = SpruceWood; drag that prefab into the item's **Prefab**.
5. **Snowy conifers:** right click TreeNode > **Create > Prefab Variant**, name it `TreeNode Snow Pine`, move it into
   `ResourceNodes/Trees`; on its **Model** child set the Mesh Filter's mesh to a pine of the tree pack and the Mesh
   Renderer's material to the pack's **Cold**, scale it to about 10 m; on its ResourceNode set **Drop Item** = SpruceWood
   and **Max Health** = 300. Then **Tools > Effects > Build Tree Fall**.
6. **Diamond tools:** **Tools > Items > Update Tool Recipes**.
7. **In ProceduralGenerateScene:** World Generator > drag the snow caves into **Snow Cave Prefabs** and DiamondNode
   into **Diamond Ore** (the gold comes from Gold Ore; Snow Gold Per Cave says how many). Add the snow's ObjectScatter rules (see `Docs/New World Setup.md`, "The new world by hand").
   Save.

## Testing

1. Play the new world, select **World Generator** > **⋮ > Test: Go To The Snow**: the player lands on the nearest snow
   (or walk there: 750 - 800 m from the start, usually).
2. Snowy pines and spruces, frosty rocks and lots of chests stand around. Hit a pine or a spruce by hand or with a
   Stone Axe: "Needs an Iron Axe or better", nothing happens. With an Iron Axe: 8 swings (numbers show each hit), then
   it falls and gives 3 - 5 **Spruce Wood** ("+4 Spruce Wood", a darker log). Mine a frosty rock: Stone.
3. Find a snow cave (a frosty mound): inside, blue-white diamond rocks shine next to gold rocks. With an Iron
   Pickaxe: the gold breaks, the diamonds say "Needs a Gold Pickaxe or better"; with a Gold Pickaxe they break into
   Diamonds.
4. Open the crafting page (Tab > Crafting > Tools): the Diamond Pickaxe, Axe and Sword are there (Gold tool + 3 Plank +
   6 / 9 Diamond).
5. Turn on **Testing > All tools** on the NEW GAME page to try a Gold Pickaxe at once.
6. With a Wooden Bow, 20 Spruce Wood and 15 Iron, the crafting page's Tools tab makes the **Spruce Bow** (the Wooden
   Bow is used up).

## Problems

| Problem | Why / fix |
|---|---|
| No snow anywhere near | It's at least 700 m from the start, and only in the cold areas: walk another way, or use **⋮ > Test: Go To The Snow**. |
| No snow caves | The World Generator has no Snow Cave Prefabs (run Build New World Scene again). Each square of snow gets one 70 % of the time per try: walk on. |
| Diamond rocks can't be mined | That's on purpose until you have a Gold Pickaxe (Diamond's Mined With = Gold). Gold is in the desert's caves and in the snow caves themselves (an Iron Pickaxe mines it). |
| No Diamond tools in the crafting list | Run **Tools > World > Build Snow Content** (or Tools > Items > Update Tool Recipes) after the Diamond item exists. |
| No snowy pines | TreeNode isn't a low-poly tree yet: run **Tools > World > Use Low Poly Grassland Trees**, then Build Snow Content. |
| Snow trees still give Wood, break after a few hits, or any axe chops them | Their prefabs or the Spruce Wood item are older than 22/11/2026: run **Tools > World > Build Snow Content** once (it sets Spruce Wood's Mined With to Iron while it's None; Tools > Items > Build Bow And Arrows does that too). |
| Only common chests in the snow | The rare / epic / legendary rules are copied from the desert's: run **Tools > World > Build Loot Chests**, then Build New World Scene again. |
| Pines on the rocky hills' tops | Not possible on purpose (the snow's rules grow only in the snow biome). If you see snowy trees there, the scene is old: run Build New World Scene again. |
