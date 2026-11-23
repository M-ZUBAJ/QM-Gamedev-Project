# Human Enemies Setup

The new world has **human enemies**: survivors who live there by the same rules as the player (the whole design is in
`Docs/Human Enemies Plan.md`). This is step 1 of the plan:

- **Type 1:** a loner **as tall as you** (your character's 1.8 m, each one a little taller or shorter) who never
  attacks first. It walks as fast as you do, goes to a tree, a rock or a berry bush (rocks about as often as trees)
  and **breaks it for real** like you do (the tree falls, the rock bursts, the bush is gone), then goes on to the next
  one or rests. **Hit it and it fights you like another player would**: it keeps its eyes on you, runs at you and
  **sprints while its stamina lasts** (as fast as your sprint, about 5 s, like yours), times its swing for where
  you'll be and **keeps running in while it winds up** (walking backward doesn't dodge it: sprinting away does),
  **circles you between attacks**, and strafes and backpedals with the right animations (its arms attack while its
  legs keep running). It flinches at most once every 2 s, so fast hits can't keep it stunned. **Its weapon decides
  how hard it hits** (a mace harder than a sword, a
  longsword harder still; a better tier +25 % each), and **its armor takes its share off your hits**, with your own
  armor's numbers (a full copper set 10 %, iron 15 %, gold 25 %, diamond 40 %). Get more than **10 m** away and it stops following and goes back to its life,
  but it **remembers**: come within **8 m** of it again and it attacks again. It always knows who hurt it: **shoot it
  with an arrow from afar and it comes after you** for 8 s whatever the distance (then the 10 m rule again).
- **Gear by area:** each human is dressed when it appears, by how rich its biome is. The grassland's are mostly poor
  (plain clothes, some copper, rarely iron, gold or even diamond), the desert's mostly iron and gold, the snow's gold
  and diamond. Its armor's metal shows the tier (copper, steel, gold, ice blue). The richer ones hold a weapon
  (sword, mace, war axe, longsword or pike), sometimes with a shield, and may wear a cape. Every one also carries an
  **axe and a pickaxe** (your own tier tools: wooden for the poorest, then stone, iron, gold, diamond by wealth).
- **Working:** it takes out its **axe to chop** and its **pickaxe to mine** stone and ore (its weapon goes away until
  it's done). **Without a tool for it, it puts its weapon away and punches** the tree or rock, and it picks berries by
  hand (bending down). Each swing lands when the tool comes down in the animation, with the same grey **wind burst** as your
  hits where it strikes, and takes health off the tree or rock (bare hands 10; the right tool 15 for wooden, +25 %
  each tier above); when it breaks, the human gets what you would (Wood, Spruce Wood, Stone, ore, an apple now and then; a
  bush gives its berries at once) and keeps it in its bag. Your tool rules count: only a pickaxe mines ore (of the
  ore's tier, like yours), the snow's spruce needs an iron axe (a war axe counts as an axe of its tier); without the
  right tool it takes plain trees, stone and berries. The trees and rocks it breaks are gone for you too.
- **Loot:** kill one and you get 1 of its tier's material for each armor piece, its weapon, its shield and each tool
  (a diamond helmet, chestplate and leggings = 3 diamonds; a wooden axe 1 Wood, a stone pickaxe 1 Stone), plus
  everything it gathered, **straight into your inventory** like chopping a tree ("+3 Wood" messages; something with
  no room left falls next to the body instead), and coins: copper 1, iron 2, gold 3, diamond 4 for each of those
  (wooden and stone tools give none). One with nothing at all gives 3 - 5 coins. Your armor set's
  coins per kill and Bounty Hunter count too.
- **Caves:** every cave has an **invisible trail** through its entrance (CaveTrail). A human that walked into a cave
  to mine walks out through the entrance, one going to an ore inside walks in through it (round the side if it comes
  from behind), and one stuck inside takes the way out.
- **Humans killed:** the victory and death pages count them apart from monsters.
- **Where:** the new world only (Map: New world on the NEW GAME page): type 1 lives in the grassland, the desert and
  the snow. New ones come in the daytime, out of your sight, a few around you at a time.

## Quick way: one click

1. Make sure the new world exists (**Tools > World > Build New World Scene**, once).
2. Menu **Tools > Enemies > Build Human Enemies**. Read the box and click **Build**.
3. The tool builds the human prefabs and their animations, puts the **Human Spawner** in ProceduralGenerateScene and
   saves it, then opens your scenes again. The box at the end says what it made (and anything missing).

Run it again any time: the prefabs are rebuilt in place, and the numbers you changed on their scripts (and on the
scene's Human Spawner) stay. **Build New World Scene** also puts the Human Spawner back when it rebuilds the new world.

It needs these packs in `Assets/Imported3D`: Polytope Studio's **Lowpoly Modular Armors Free** (the bodies and armor)
and **Lowpoly Weapons** (the weapons and shields), and Blink's animations. The axes and pickaxes are the game's own tier
tools (**Tools > Items > Build Tier Tools** made them; the box says if some are missing).

## What it makes

| What | Where | What it is |
|---|---|---|
| The animations | `Assets/Animation/Enemy/Human/HumanEnemy.controller` | Blink's Humanoid clips. **Legs by direction, like yours:** a 2D blend on MoveX / MoveZ (its speed to its right and forward, m/s, set by HumanBrain): Idle, RunForward slowed down for starting, the runs forward / backward / left / right / back diagonals at your walking speed (7.8 m/s), StrafeLeft / Right for circling, Sprint at your sprint. MiningLoop for chopping and mining with a tool, Gathering for picking berries, GetHit, Death. **An Actions layer for the upper body only** (`HumanUpperBody.mask`) with three attacks by what it holds: PunchRight (fists; also its punches at a tree or rock), MeleeAttack_OneHanded (sword, mace, war axe), MeleeAttack_TwoHanded (longsword, pike), played 1.2 times as fast and left as soon as the move is over (the tool measures when each blow lands and ends), so it swings while its legs run, strafe or step in. The walk and run play faster for a smaller body (**StrideSpeed**) |
| Cave trails | every cave prefab in `Assets/Prefabs/World/Caves` | a **CaveTrail** with the cave's floor radius (the tool adds it to caves made before; **Tools > World > Build Rock Caves** puts it on new ones) |
| The humans | `Assets/Prefabs/Enemy/Human/Human Type 1 Male` and `... Female` | the Polytope body (Model) at its own size: 1.8 m tall, **the same as your character** (the Human Spawner makes each one a little taller or shorter), a capsule collider and a Rigidbody, and the scripts below |
| The spawner | ProceduralGenerateScene > **Human Spawner** | keeps a few humans around the player |

## How it works

| Part | Where | What it does |
|---|---|---|
| **EnemyCombat** (Marko's) | on each human | its health (80 for type 1), so every attack you have hurts it: punches, tools, arrows, the fireball, Thorns, critical hits, damage numbers. Its **Coin Multiplier** is 0 on humans: HumanLoot pays by the gear instead of Marko's 3 - 6 coins |
| **EnemyAnimator** | on each human | walking / running speed for the animations, the flinch when hurt (not while it swings), the attack (`PlayAttack`), the death animation, then the body is removed |
| **DeathEffect** | on each human | its body turns into (green) particles while it dies, like the monsters' |
| **HumanGear** | on each human | dresses it once when it appears: rolls its wealth tier from the area's odds, wears each armor piece with Piece Chance at that tier (sometimes one lower), the rest plain clothes or bare skin, one armor style, the metal tinted by tier, a random head, hair (none under a helmet), beard and sometimes a cape; a weapon in the right hand's slot bone and sometimes a shield in the left (Polytope made those two slot bones only for the man's body: the tool copies them onto the woman's hands); with **Tool Chance** an axe and a pickaxe of its wealth's tier in the same hand, hidden until it works; removes the pieces it doesn't wear. **Outfit** shows what it got |
| **HumanLoot** | on each human | keeps what it gathered (**Carried**); when it dies, puts the materials of its gear and what it carried into your inventory (what doesn't fit falls next to the body) and gives the coins |
| **HumanBrain** | on each human | rests, walks around its home at your walking speed, walks to a nearby tree, rock or berry bush it can break (a rock first with **Rock Share**; your tool rules: its axe, war axe or pickaxe), takes out the axe or the pickaxe and swings at it until it breaks, each swing landing when the tool comes down in the animation (**Swing At**) with your wind burst (PunchEffect); without a tool for it, it punches (the fist attack, each punch landing when the fist does), and it picks berries by hand (Turki's ResourceNode gives the drop to its bag: a `[Tony]` addition). Through a cave's entrance by the cave's trail (CaveTrail). Hurt by you, it fights like a player: faces you, runs at you and sprints with its own stamina, starts a swing when you'll be within reach as the blow lands and keeps running in while it winds up, circles you between attacks, flinches at most once in Flinch Every; each blow hurts you only if you're still within reach and in front of it (your armor and Last Stand count, Thorns hits it back); farther than Give Up Range it stops, but it holds a grudge and attacks again when you come within Notice Range. Looks ahead and goes around trunks, rocks and walls. **Doing** shows what it's doing ("Punching TreeNode", "Sprinting at the player | stamina 60", "Circling the player") |
| **CaveTrail** | on each cave | the cave's invisible trail: the door just inside the entrance, the spot in front of it, and a spot at each side (select a cave to see them). `CaveTrail.Around(point)` = the cave a point is in |
| **Human Spawner** | the new world | every few seconds: takes away humans left far behind (out of sight), and in the daytime brings a new one where you can't see it, of a type that lives in that biome, dressed with that biome's odds |
| **MonsterBounty** | (code) | when a human dies, the armor set's coins per kill and Bounty Hunter's coins, and "Humans killed" instead of "Monsters killed" (Marko's EnemyCombat now passes who died: a `[Tony]` line) |

## By hand (instead of the tool)

The tool is much easier; this is what it does, if you want to see it.

1. **Animations:** make an Animator Controller with the parameters Speed (float), Hit (trigger), Dead (bool), Work
   (bool), StrideSpeed (float, default 1), Attack (trigger), AttackKind (int), Attacking (bool), MoveX and MoveZ
   (float) and Gather (trigger). The default state Locomotion is a **2D Freeform Directional** blend tree on MoveX /
   MoveZ: Idle at (0, 0), RunForward at (0, 2) with Speed 0.45 and at (0, 7.8), RunBackward (0, -7.8), RunLeft
   (-7.8, 0), RunRight (7.8, 0), RunBackwardLeft (-5.5, -5.5), RunBackwardRight (5.5, -5.5), StrafeLeft (-3.9, 0),
   StrafeRight (3.9, 0), Sprint (0, 16.5); tick its Speed's **Multiplier > Parameter** and pick StrideSpeed. A Work
   state (MiningLoop) goes both ways on Work. From Any State: Death (Death clip) when Dead, Hit (GetHit) on Hit when
   Attacking is false, back to Locomotion at 90 %, Gather (Gathering) on Gather, back at 95 %. Add a layer
   **Actions** (Weight 1, Override) with an Avatar Mask that has only the upper body (no Root, legs or feet IK): an
   empty default state Empty, Any State -> Empty when Dead, and three attack states (Speed 1.2): Attack Fists
   (PunchRight) on Attack with AttackKind Equals 0, Attack One Hand (MeleeAttack_OneHanded) with 1, Attack Two Hands
   (MeleeAttack_TwoHanded) with 2, each back to Empty with Exit Time 0.47 / 0.5 / 0.48 (where its move is over).
2. **The human:** an empty object with a Rigidbody (mass 5, Interpolate, Continuous Dynamic, Freeze Rotation x y z) and a
   Capsule Collider (height 1.8, radius 0.3, center y 0.9, the EnemyNoFriction material). Drag
   `PT_Lowpoly_Armors_Male_Moduar_Free` in as its child named **Model** (position 0, scale 1), and give its Animator the
   controller (Apply Root Motion off).
3. Add **EnemyCombat** (Max Health 80, Current Health 80, Coin Multiplier 0), **EnemyAnimator**, **HumanGear**,
   **HumanLoot** and **HumanBrain**.
4. HumanGear: add every armor piece of the Model to **Parts** (Piece, Style 01_a / 05_c / cloth / naked, Armored on for
   01_a and 05_c), the heads, hairs, beards and capes to their lists, `PT_Right_Hand_Weapon_slot` and
   `PT_Left_Hand_Shield_slot` to the slots (the woman's body has no such bones: make two empty children with those
   names under her `PT_RightHand` / `PT_LeftHand`, with the Position and Rotation of the man's, the position a bit
   smaller), Polytope's weapon and shield prefabs to **Weapons** / **Shields** (tick
   Is Axe on the war axe), and the game's WoodenAxe, StoneAxe, IronAxe, GoldAxe, DiamondAxe (`Assets/Prefabs/Item/Tools`)
   to **Axes** in that order, the pickaxes the same way to **Pickaxes**.
5. HumanLoot: drag the Copper, Iron, Gold, Diamond, Wood and Stone items from `Assets/Config/Items` in.
6. Save it as a prefab. In ProceduralGenerateScene: Create Empty "Human Spawner", add **HumanSpawner**, add a line to
   **Kinds** with your prefab in Bodies and Grassland / Desert / Snow ticked.

## Settings

| Script | Setting | What it changes |
|---|---|---|
| HumanBrain | **Match Player Speed** (on), **Flee Times Player** (1.3) | it walks as fast as you walk (your PlayerController > Speed) and runs away 1.3 times as fast (you can still catch it by sprinting). Off: **Walk Speed** / **Flee Speed** |
| | **Wander Radius** (40 m), **Rest Seconds** (2 - 6) | how far from home it walks, how long it stands still |
| | **Work Chance** (0.7), **Keep Working Chance** (0.7), **Work Search Radius** (50 m) | how often it works, whether it goes straight to the next tree, how far it looks |
| | **Rock Share** (0.45), **Berry Share** (0.1) | when it goes to work, the chance it looks for a rock / a berry bush first (else a tree); none of that kind near = any kind |
| | **Work Reach** (1.2 m), **Hand Reach** (0.7 m) | how close it stands to the tree or rock to swing a tool, or to punch it / pick a bush, for a human your size (a taller or shorter one stands that much further or closer) |
| | **Swing At** (about 0.46, measured by the tool) | when its tool lands in each swing of the work animation, as a share of the swing: the hit and the wind burst happen there. Blink's MiningLoop swing takes 1.27 s and the pick comes down at 46 % |
| | **Swing Damage** (10), **Tool Damage** (15) | its punches at a tree or rock do Swing Damage, the right tool Tool Damage for wooden and 25 % more each tier above. A tree has 30 health, a stone 30, the snow's spruce 300 |
| | **Swing Every** (1 s) | only for a human whose Animator has no Work state: seconds between its swings |
| | **Work Most Seconds** (60), **Carry Most** (40) | gives up on one still standing after this long; stops working with this much in its bag |
| | **When Hit** (Fight Back) | what it does when you hurt it: Fight Back, or Run Away (the numbers under Running Away) |
| | **Give Up Range** (10 m), **Holds Grudge** (on), **Notice Range** (8 m) | it stops following you farther than Give Up Range; with a grudge it attacks again when you come within Notice Range (keep it below Give Up Range) |
| | **Chase After Hit** (8 s) | after each hit (an arrow from afar too) it chases you this long whatever the distance; then Give Up Range counts again. 0 = only within Give Up Range |
| | **Chase Times Player** (1.2) | its running speed while chasing: your walking speed x this (it sprints faster while its stamina lasts) |
| | **Fist Damage** (5), **Attack Reach** (2 m), **Attack Every** (0.9 s) | one punch with bare hands (with a weapon it's the weapon's Damage below; your armor takes its share), how close it gets to hit (fists 70 % of it, a two-handed weapon 130 %), seconds between its attacks (at least the attack's move) |
| | **Sprints** (on), **Sprint From** (4 m), **Sprint Drain** (20), **Stamina Regen** (15), **Recover To** (30) | like your stamina: it sprints at you (as fast as your sprint) when you're farther than Sprint From, using 20 of its 100 stamina a second (5 s), gets 15 a second back a second after, and out of stamina it can't sprint until it has 30 again |
| | **Strafes** (on) | between attacks it circles you at reach, facing you, like a player strafing; off: it stands and waits |
| | **Flinch Every** (2 s) | it flinches at most once in this many seconds (never mid-swing): you can't keep it stunned by hitting fast. 0 = every hit |
| | **Fist / One Hand / Two Hand Strike**, **Gather Timing** (measured by the tool) | seconds from the start of each attack (or pick) until the blow lands (or the hand picks), and until the move is over |
| | **Flee Seconds** (6), **Safe Distance** (40 m), **Flee Most Seconds** (15) | how long it runs away (When Hit = Run Away) |
| HumanGear | **Piece Chance** (0.6) | the chance it wears each armor piece once it has a tier (all 5 = a full set) |
| | **Lower Chance** (0.25) | a piece, weapon or shield one tier below its wealth |
| | **Shield Chance** (0.35), **Cape Chance** (0.3), **Beard Chance** (0.6), **Bare Chance** (0.3) | the looks |
| | **Tool Chance** (1) | the chance it carries an axe and a pickaxe (of its wealth's tier: none = wooden, copper = stone, then iron, gold, diamond); 1 = every human |
| | **Copper / Iron / Gold / Diamond Metal** | the tint of each tier's metal |
| | **Weapons > Damage** | one hit with that weapon at copper tier (sword 10, mace 12, war axe 11, longsword 15, pike 14); each tier above +25 % (iron x1.25, gold x1.56, diamond x1.95). The tool writes these only where they're still 0, so your numbers stay. **Outfit** shows the damage of the weapon it got |
| | (its armor) | every hit you land is cut by its armor's share (**Outfit** > protection): the chestplate counts 28 % of a set, the other four pieces 18 % each; a full set 10 / 15 / 25 / 40 % for copper / iron / gold / diamond. Marko's EnemyCombat asks HumanGear (a `[Tony]` line) |
| | **Weapon Offset / Turn**, **Shield Offset / Turn**, **Tool Offset / Turn** | moves the weapon, shield, axe and pickaxe in the hand if they don't sit right (Tool Turn 0, 0, 180 turns the game's tools to point the way Polytope's weapons do) |
| | **Axe Turn** (180) | axes only: spun around the handle after Tool Turn, so the sharp edge faces the tree (like your own ToolInHand > Axe Turn) |
| HumanLoot | **Empty Coins** (3 - 5) | coins from one that has nothing to drop |
| CaveTrail (on each cave) | **Radius** | the cave floor's radius (the tool sets it from the cave's design) |
| Human Spawner | **Kinds** | each type: its prefabs (a man and a woman), weight, the biomes it lives in, group size |
| | **Height Variation** (0.06) | each new human is up to 6 % taller or shorter than its prefab (about 11 cm), most near the middle; 0 = all the same |
| | **Grassland / Hills / Desert / Snow** (under Gear By Area) | how rich the humans of each biome are (weights for none, copper, iron, gold, diamond) |
| | **Population** (6), **Spawn Every Seconds** (8), **Only In Day** | how many at once, how often a missing one comes, daytime only |
| | **Min / Max Distance** (35 / 110 m), **Despawn Distance** (220 m), **Spacing** (30 m) | where they appear and are taken away |

## Testing

1. From the title screen: **NEW GAME > Map: New world > START GAME**. The Console says "Human Spawner: N human(s)
   around the player".
2. Look around: humans walk, rest, and work on trees and rocks; press F5 to see that they're about as tall as you. Select one (Hierarchy:
   Human Spawner > its children): HumanGear > **Outfit** says what it wears ("| tools: stone axe + pickaxe" when it
   carries some), HumanBrain > **Doing** what it's doing, HumanLoot > **Carried** what it gathered. It swaps its
   weapon for the axe at a tree and the pickaxe at a rock, and each time the tool comes down the grey wind bursts
   where it strikes (like your hits). Men and women both do it.
3. Hit one: it flinches and hits you back (HumanBrain > **Doing**: "Fighting the player"); its blows hurt you (your
   armor counts), harder with a heavier or better weapon. Your damage numbers on it are smaller the more armor it
   wears (HumanGear > **Outfit** says its protection). Sprint more than 10 m away: it stops and goes back to work.
   Walk back within 8 m: it attacks again.
4. Shoot one with an arrow from 30 m: it runs at you (**Doing**: "Chasing the player"). Keep shooting and it keeps
   coming; walk away and after 8 s it gives up if you're still more than 10 m away.
5. Fight one up close and walk backward: it keeps coming and still lands its blows; sprint away and it can't keep up
   for long (its stamina runs out too). Between its attacks it circles you (**Doing**: "Circling the player"). Its
   legs run sideways and backward the right way, and it swings while running.
6. Watch one work: a tree it chops falls, a rock bursts, and its **Carried** grows (set HumanGear > **Tool Chance** to
   0 on a prefab to see one punch trees and rocks). Then kill it: it turns into green particles and "+3 Wood",
   "+1 Copper"... and "+N Coins" show: it's all in your inventory.
7. Caves: follow one that mines in a cave (**Doing**: "... (through a cave entrance)"): it walks out through the
   entrance. Select a cave to see its trail (yellow spots).
8. Die or win: the page shows **Humans killed**.
9. Human Spawner's **⋮ > Test: Spawn One Now** makes one out of sight (turn around).

## Problems

| Problem | Why / fix |
|---|---|
| No humans | You're in the old world (they live in the new world only), it's night (new ones come in the daytime), the scene has no Human Spawner (run the tool), or the Player has no PlayerSight (Tools > World > Set Up World Spawning). |
| Humans in a T-pose | The animations weren't found: the tool's box says MISSING clips. Blink's pack must be in the project. |
| The weapon sits wrong in the hand | Change HumanGear > **Weapon Offset / Turn** while playing until it looks right, then copy the numbers onto the prefab. |
| Women never hold anything (no weapon, shield or tool) | A prefab built before 23/11/2026's fix: Polytope made the hand slot bones only for the man's body. Run the tool again: it copies them onto the woman's hands (the box says "added the man's hand slots"). |
| No wind burst when they chop or mine | The Player needs PunchEffect (**Tools > Effects > Build Punch Wind Burst**). There's none farther than 60 m from the camera (too small to see), and none for picking berries. |
| The burst comes before or after the tool lands | Change HumanBrain > **Swing At** while playing (smaller = sooner), then copy it onto both prefabs (the tool measures it again on every run). |
| The axe or pickaxe sits wrong (upside down, edge backwards) | An axe's edge backwards: HumanGear > **Axe Turn** (180 or 0). Both tools: **Tool Offset / Turn** (0, 0, 0 = the handle the other way). They apply to humans that appear after the change: Human Spawner's **⋮ > Test: Spawn One Now**. Copy the numbers onto both prefabs. |
| They look tall in first person | They're as tall as your character (1.8 m), but your first-person camera is only 1.0 m above the ground (your 1 m ball + Main Camera > CameraController > **Eye Height** 0.5), lower than your character's eyes (about 1.65 m). Eye Height 1.15 puts the camera at your character's eyes. |
| A human shot from far away gives up before it reaches you | It chases you Chase After Hit (8 s) after each hit, then only within Give Up Range (10 m). Raise **Chase After Hit** for a longer chase. |
| It never hits you | The controller has no attacks yet (a prefab built before 23/11/2026's fight-back change): run the tool again. Sprinting away still gets you out of reach (walking backward doesn't). |
| It slides with its legs frozen, or runs sideways with a forward-running animation | The controller is older than its legs by direction and the Actions layer (23/11/2026): run the tool again. |
| One stays stuck in a cave | The cave has no CaveTrail (made before 23/11/2026): run the tool again (the box says "cave trails on N caves"). A cave made another way (not by Build Rock Caves) needs a CaveTrail added by hand, with its floor's Radius, the entrance at its +Z. |
| They hardly ever mine | No rock near: in the grassland rocks are rare (about one in a 30 m circle). Raise **Rock Share** or **Work Search Radius**. |
| All of them look the same | Normal for the grassland (most are poor); the desert and the snow have more armor. |
| Killing one counts as a monster | Marko's EnemyCombat is older than 23/11/2026 (no `MonsterBounty.Killed(gameObject)` line). |
| It gives only coins | It wore no armor and carried nothing; or the Copper / Iron / Gold / Diamond items are missing on HumanLoot (run the tool again). |
| It never mines ore or chops the spruce | It has no pickaxe, or not a good enough one (your tool rules: copper and iron ore need a stone pickaxe or better, gold an iron one, diamond a gold one; the spruce an iron axe or a copper-or-better war axe). One without tools only takes plain trees, stone and berries. |
| They're slow, or the wrong size | A prefab built before 23/11/2026's changes: run the tool again (it gives them the new defaults once). |
