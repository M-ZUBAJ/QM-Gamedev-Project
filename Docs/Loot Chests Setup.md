# Loot Chests Setup (common, rare, epic and legendary boxes, two chest systems, and their perks)

**Two chest systems.** The New Game page's **TESTING** row picks one: **Old chests** or **New chests** (the default).
In the game it's the GameRules tick box **New Chests** (on = new). Every perk in one table: `Docs/Perk list table.md`.

**Old chests** (how the boxes first worked):

| Box | Looks like | Where | First price | Each perk bought from it | How you get a perk | Perks |
|---|---|---|---|---|---|---|
| **Common** | Marko's wooden chest, yellow outline | grassland (20), desert (30), snow (40) | 2 coins | +1 (2, 3, 4...) | choose 1 of 3 | Vitality, Stamina, Agility, Fireball, Second Wind, Precision*, Spring Legs* |
| **Rare** | blue loot box, blue outline | grassland (6), desert (12), snow (20) | 10 coins | x1.5 (10, 15, 23...) | choose 1 of 3 | Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler, Attack Speed, Long Reach* |
| **Epic** | purple loot box, purple outline | desert (4) and snow (8) only | 100 coins | x2 (100, 200, 400...) | choose 1 of 3, REROLL** | Last Stand, Critical Strike, Glide, Vampire, Thorns, Night Power, **Reroll**, **Keen Eye**, **Arrow Recovery** |

**New chests** (the default):

| Box | Where | First price | Each perk bought | How you get a perk | Perks |
|---|---|---|---|---|---|
| **Common** | grassland (20), desert (30), snow (40) | 2 coins | +1 | **a random one** (a slot-machine roll), REROLL** | the old common **and** rare perks together (15) |
| **Rare** | grassland (6), desert (12), snow (20) | 10 coins | x1.5 | choose 1 of 3, REROLL** | the same 15 |
| **Epic** | desert (4) and snow (8) only | 100 coins | x2 | **a random one**, REROLL** | the epic perks (with Reroll, Keen Eye and Arrow Recovery) |
| **Legendary** | orange loot box, orange outline; desert (2) and snow (4) only | 150 coins | x2 (150, 300, 600...) | choose 1 of 3, REROLL** | the epic perks (with Reroll, Keen Eye and Arrow Recovery) |

The numbers in **Where** are the ObjectScatter rules' counts. In the old world (GameScene) a count is for the whole
map. In the new world (endless since 21/11/2026) a count is for every 400,000 m² of that ground, so a 256 m square of
desert has about 5 common boxes, 2 rare, a 2 in 3 chance of an epic and 1 in 3 of a legendary. The snow (only in the
new world, the farthest biome) has more and better boxes than the desert: its rules (ChestsSnow, Rare / Epic /
Legendary Chests Snow) are made by **Tools > World > Build New World Scene** as copies of the desert's
(`Docs/Snow Setup.md`).

\* Precision only shows up once you have **Critical Strike**; Spring Legs only once you have **Glide**; Long Reach only
once you have **Keen Eye** (in the new system the common boxes offer it too).

\*\* **REROLL only works once you have the Reroll perk** (an epic box offers it, and the legendary box in the new
system). Until then no box shows the REROLL button, in either system.

- Every box of one kind costs the same, and a perk bought from one box makes all boxes of that kind dearer. The prices
  start again when you die (your perks are lost then too) and in a new game.
- **The whole gold or diamond armor set** makes every box **10 % cheaper** (they don't add up), rerolls too (rounded down, never under 1 coin: 2 -> 1,
  10 -> 9, 100 -> 90). The panel and the roll pop-up show the full price crossed out in front (`Docs/Armor Setup.md`).
- Under the crosshair: **[E] Open rare box (coin) 10**. The title of the panel / pop-up is the box's name in its color.
- **When you buy a perk the box bursts into particles in its color** (common gold, rare blue, epic purple, legendary
  orange) and disappears. In both systems.
- A choose-1-of-3 box shows up to 3 perks; when you can't get more than 1 or 2 of its perks any more, it shows 1 or 2
  cards; with none left it says "Nothing left in this box for you."
- **REROLL** (old system: the epic box only; new system: every box), **only once you have the Reroll perk**: new
  perks for a tenth of the box's price (10 coins at a 100-coin box, at least 1), twice as much each time at that same
  box (10, 20, 40...); another box starts again at a tenth. "NOTHING NEW TO ROLL" = the box has no other perk you can
  get.
- **The random boxes (new system):** E opens the roll pop-up. The box's perks flick past like a slot machine for about
  2.5 seconds, slowing down, and land on yours (a pop, a glow and sparks). **SKIP**, **E**, **Space** or a **click**
  shows it at once. Then the perk stays on screen with **BUY** (pays, you get it) and, once you have the Reroll perk,
  **REROLL** (without it BUY sits in the middle alone). Nothing is paid until BUY: **E** again or walking away closes
  it, and opening that box again shows the same perk (no free reroll).

**The new perks** (the numbers are each Perk Data's **Value**, so they're easy to change):

| Perk | Box (old system) | What it does | Value |
|---|---|---|---|
| Second Wind | common | stamina recharges 30 % faster (stacks) | 0.3 |
| Precision | common | +1.25 % critical strike chance (needs Critical Strike) | 0.0125 |
| Spring Legs | common | jump 25 % higher (needs Glide) | 0.25 |
| Bounty Hunter | rare | +1 coin for every monster you kill (stacks) | 1 |
| Gourmet | rare | food heals, feeds and refreshes 10 % more (bad food doesn't get worse) | 0.1 |
| Lumberjack | rare | +1 wood from every tree you chop down | 1 |
| Prospector | rare | +1 stone or ore from every rock you break | 1 |
| Sprinter | rare | sprint 15 % faster, using 25 % less stamina | 0.15 |
| Brawler | rare | punches knock monsters back 10 % farther | 0.1 |
| **Attack Speed** | rare | attack 10 % faster (the wait between attacks is shorter; stacks) | 0.1 |
| **Long Reach** | rare (new system: common and rare) | your melee attacks (punches, tools) reach 10 % farther: 2.75 m instead of 2.5 (stacks; needs Keen Eye) | 0.1 |
| Last Stand | epic | a killing blow leaves you at 1 health, and nothing can hurt you for 2 seconds (a gold flash). Then it's **used up**: it leaves the perks at the top of the screen, and a box can offer it again | 2 (seconds) |
| Critical Strike | epic | 5 % of your hits do double damage; their wind burst turns red | 0.05 |
| Glide | epic | hold Space in the air: you float down slowly; tap Space twice in the air: you dive to the ground | - |
| **Vampire** | epic, legendary | heal 5 % of the damage you really do to monsters and animals (punches, tools, the fireball; stacks) | 0.05 |
| **Thorns** | epic, legendary | a monster that hits you takes 10 % of that hit back (stacks) | 0.1 |
| **Night Power** | epic, legendary | at night: +10 % max health, max stamina, walking speed and melee damage (punches, tools; the fireball stays the same) (stacks); a message says when it wakes up and when it rests at dawn | 0.1 |
| **Reroll** | epic, legendary | unlocks the **REROLL** button at loot boxes (without it no box can reroll) | - |
| **Keen Eye** | epic, legendary | the crosshair turns **yellow** when your attack will reach what you aim at (a monster, an animal, a tree or a rock): a click hits it. Then boxes offer Long Reach | - |
| **Arrow Recovery** | epic, legendary | your arrows can be had back: one in a monster or an animal stays in it (walk within 2.5 m to take it back) and falls out onto the ground when it dies; a missed arrow stays where it landed (walk within 1.6 m to pick it up). Without it an arrow that hits a creature is used up (22/11/2026, `Docs/Bow Setup.md`) | - |

Last Stand, Critical Strike, Glide, Reroll, Keen Eye and Arrow Recovery can be held one at a time (**Max Picks** 1); the others stack.
Like every perk, they're lost when you die (so REROLL locks again until you find another Reroll perk).

Also with them:
- **Punches push monsters back** a little (a heavy punch more). That's what Brawler makes stronger.
- **The armor set bonus is coins:** the whole copper set gives +1 coin for every monster killed, the whole iron set
  +2. Bounty Hunter adds on top. See `Docs/Armor Setup.md`.

## Quick way: one click

1. Menu **Tools > World > Build Loot Chests**, then **Build**. It saves your open scenes first.
2. That's it: it changes and saves both game scenes itself, then opens your scenes again.
3. For the New Game page's **Old chests / New chests** choice, run **Tools > UI > Build Main Title Scene** once too
   (it rebuilds the title screen; `Docs/Main Title Setup.md`).

The tool:
- **Perk Data:** makes the 20 new perks in `Assets/Config/Perks` (`Perk_SecondWind`, `Perk_AttackSpeed`,
  `Perk_Vampire`, `Perk_Reroll`, `Perk_KeenEye`, `Perk_LongReach`, `Perk_ArrowRecovery`...), only if missing, with drawn icons
  (`Assets/UI/Perks`), and puts the new ones in their boxes' lists. A perk you changed keeps your numbers (Last Stand's
  old "Once a life..." text and Night Power's old "...speed and damage." are brought up to date if you didn't change
  them).
- **Common box:** **ChestTier** (Common, 2 coins, +1, gold) on Marko's `Assets/Prefabs/WorldItems/Chest.prefab`, and
  Second Wind, Precision and Spring Legs added to its **Available Perks** (his four stay).
- **Rare / Epic / Legendary box:** makes `Assets/Prefabs/WorldItems/Rare Chest.prefab` (DuNguyn's blue `SM_LootBox_01`,
  1 m wide), `Epic Chest.prefab` (the purple `SM_LootBox_03`, 1.2 m) and `Legendary Chest.prefab` (the orange
  `SM_LootBox_02`, 1.35 m). The model is taken from DuNguyn's prefab, which has the textured materials (not from the
  .fbx): a box to aim at, Marko's **PerkChest** with the box's perks, **ChestTier**, **StandOnGround** (stands on the
  ground) and **InteractOutline** (blue / purple / orange). The pack's materials are made for URP (pink here), so it makes
  Standard copies with their glow in `Assets/Materials/Chests`, and imports the pack's 4096 px textures at 1024 (enough
  for a chest; the web game loads faster). Run again: the model is rebuilt, your numbers on the components stay.
- **Both systems on each ChestTier:** the common and epic boxes roll a random perk in the new system, every box can
  reroll there (once the player has the Reroll perk: **Reroll Needs Perk**, on for every box), the common and rare boxes
  offer the 15 old common + rare perks there (**New System Perks**), the
  legendary box is **New System Only**, and every box bursts with `Assets/Prefabs/Effects/DeathParticles.prefab`
  (**Tools > Effects > Build Death Particles** makes it; without it there's no burst). A box built by the first version
  of the tool gets these settings once; after that your changes stay.
- **Perk panel:** the REROLL button and the box's name as the title in `PerkSelectCanvas.prefab`
  (`Docs/Perk Panel Setup.md`).
- **Roll pop-up:** builds `Assets/Prefabs/UI/Perks/PerkRollCanvas.prefab` (the same as **Tools > UI > Build Perk Roll
  Popup**).
- **Each game scene** (the Build Settings scenes with Sanyam's ObjectScatter: GameScene and ProceduralGenerateScene):
  - Sanyam's chest rules, **twice as many boxes as before**: **ChestsForest** 35, **Rare Chests Grass** 10 (since
    23/11/2026, both on ground up to 15 degrees steep like the desert's; 20 and 6 before),
    **ChestsDesert** 30 (on the **sand** layer: it used the grass layer, so every chest spawned on grass), **Rare Chests
    Desert** 12, **Epic Chests Desert** 4 and **Legendary Chests Desert** 2 (new rules are copies of the common ones,
    right after them); all chest rules share the spacing group **Chests** (20 m between any two boxes). Counts only go
    up: a bigger count you set stays. In the old system the legendary boxes remove themselves as the game starts;
  - the **roll pop-up** (one copy of `PerkRollCanvas`) and an EventSystem if there's none;
  - **PerkEffects** and **PlayerGlide** (switched off) on the Player;
  - PlayerArmor's set bonuses switched to coins;
  - then saved.

## How it works

| Part | Where | What it does |
|---|---|---|
| **GameRules > New Chests** | the scene | which system: on = new (default). The New Game page sets it (GameSetup) |
| **ChestTier** | each box prefab | its kind, name, color, price rule, reroll (only once the player has the Reroll perk), and how it works in the new system (Random In New System, New System Perks, New System Only); one shared count per kind (the price), reset on death and in a new game; the burst when a perk is bought |
| **PerkChest** (Marko's) | each box prefab | E opens the panel (or, for a random box in the new system, the roll pop-up); `[Tony]` lines: the price from ChestTier, the perks of the system in use, only perks PerkRules allows (1 to 3), pays and counts the purchase, `TryReroll`, `BuyRolled` |
| **PerkRollUI** | `PerkRollCanvas` in each game scene | the roll pop-up: the slot-machine roll, SKIP, BUY, REROLL |
| **PerkRules** | (code only) | which perks a box may offer: **Requires** picked already, fewer than **Max Picks** held |
| **PerkSelectionUI** (Marko's) | the perk panel | `[Tony]` lines: 1 to 3 cards (the others hide), `Refresh` for a reroll |
| **PerkPanelInfo** | the perk panel | the box's name and color, the price, the REROLL button and its price |
| **PerkManager** (Marko's) | the scene | gives his four perks; a `[Tony]` `default:` hands every new kind to PerkEffects |
| **PerkEffects** | the Player | does what the new perks do (see the table), undone when the player dies |
| **PlayerGlide** | the Player (off until Glide) | hold Space while falling: fall at most Glide Fall Speed; Space twice in the air: dive |
| **Knockback** | added to a monster when a punch pushes it | its push; Marko's EnemyMovement (`[Tony]` line) doesn't walk while pushed |
| **MonsterBounty** | (code only) | the extra coins per kill: the armor set bonus + Bounty Hunter (Marko's EnemyCombat calls it, `[Tony]` line) |
| **PunchEffect** | the Player | red burst on a critical hit; pushes the monster a punch lands on; Vampire heals from the damage the hit really did |
| **PlayerCombat** (Marko's) | the Player | `[Tony]` lines: Attack Speed shortens the wait between attacks, Night Power adds damage at night, **AttackReach** = Attack Range x Long Reach (the attack's ray uses it) |
| **Crosshair** | the HUD's crosshair | with Keen Eye: **Reach Color** (yellow) when the attack would hit what it points at (the same ray and reach as PlayerCombat's attack) |
| **EnemyAttack** (Marko's) | each monster | `[Tony]` line: Thorns sends part of the hit back |
| **FireballProjectile** (Marko's) | the fireball | `[Tony]` lines: Vampire heals from it (Night Power doesn't change it) |
| **PlayerHealth / PlayerStamina / PlayerController** | the Player | **SetMaxBoost** / **SetMoveBoost**: Night Power's extra max health, max stamina and speed at night (the bars keep their share) |
| **ResourceNode** (Turki's) | trees, rocks | `[Tony]` lines: +Lumberjack wood / +Prospector stone or ore when it breaks |
| **PlayerPerks** | the Player | remembers each perk picked (for PerkRules), the HUD names and stats, loses everything on death; **Lose** takes a used Last Stand away |

Change logs: `Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md`,
`Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md`,
`Docs/TonyEditMate/Marko/21-11-2026 Reroll Perk, Keen Eye And Long Reach.md`,
`Docs/TonyEditMate/Turki/21-11-2026 Lumberjack And Prospector Perks.md`,
`Docs/TonyEditMate/Sanyam/21-11-2026 Loot Box Spawn Rules.md`,
`Docs/TonyEditMate/Sanyam/21-11-2026 Twice The Chests And A Legendary Rule.md`.

## By hand (instead of the tool)

1. **A perk:** right click in `Assets/Config/Perks` > **Create > Game > Perk Data**. Display Name, Description, Icon,
   **Perk Type** (the kind), **Value** (how much). Under **Offered when**: **Requires** (drag the perk it needs) and
   **Max Picks** (1 = one at a time).
2. **A box prefab:** drag the loot box model into the scene, scale it to about 1 m, put it under an empty object
   (the root) with its bottom at the root. On the root: **Box Collider** around the model, **PerkChest** (Interact Key
   E, Interaction Range 3, Despawn After Opened, its **Available Perks**), **ChestTier** (see Settings),
   **StandOnGround**, **InteractOutline** (Color, Show When Look At, Range 3). If the model is pink, give it a Standard
   material with the pack's textures (Albedo = BaseColor, Emission = Emissive). Drag the root into
   `Assets/Prefabs/WorldItems`.
3. **The burst:** on the box's ChestTier, drag `Assets/Prefabs/Effects/DeathParticles.prefab` into **Vanish Particles**.
4. **Where it spawns:** select the scene's ObjectScatter > **Rules** > +: Name, Prefab, Count, Terrain Layer (0 grass,
   1 sand in GameScene), Min Strength, Max Slope, Min Spacing 20, Spacing Group **Chests**.
5. **The roll pop-up:** **Tools > UI > Build Perk Roll Popup** in each game scene, then save the scene.
6. On the Player: Add Component > **PerkEffects** and **PlayerGlide** (untick PlayerGlide).

## Settings

| Where | Setting | What it does |
|---|---|---|
| GameRules (scene) | **New Chests** | on = the new system (the New Game page sets it when you change it there) |
| ChestTier (box prefab) | **Tier** | Common / Rare / Epic / Legendary (every box of a kind shares one price) |
| PlayerArmor (Player) | **Set Bonuses > Chest Discount** | the gold and the diamond set's 0.1 = every box 10 % cheaper while all six are worn |
| | **Box Name**, **Color** | the prompt ("Open rare box"), the title and the burst's color |
| | **Start Price**, **Growth** (Add / Multiply), **Grows By** | 2, Add, 1 (common); 10, Multiply, 1.5 (rare); 100, Multiply, 2 (epic); 150, Multiply, 2 (legendary) |
| | **Can Reroll** | old system: the REROLL button (the epic box) |
| | **Reroll Needs Perk** | on (every box): REROLL only works once the player has the Reroll perk, in both systems; off = from the start |
| | **Reroll Share** (0.1), **Reroll Growth** (2) | the first reroll = this share of the price, then x Reroll Growth at the same box |
| | **Random In New System** | new system: one random perk in the roll pop-up (common, epic) |
| | **Reroll In New System** | new system: REROLL at this box (every box) |
| | **New System Perks** | new system: what it offers (common and rare: the 15); empty = its Available Perks |
| | **New System Only** | the legendary box: gone in the old system |
| | **Vanish Particles**, **Burst Count**, **Burst Speed** | the burst when a perk is bought (110 / 140 / 180 / 230 particles) |
| PerkRollUI (roll pop-up) | **Roll Seconds** (2.5), **Fast Step** (0.05), **Slow Step** (0.34) | how long the roll lasts and how quickly the pictures change at its start / end |
| | **Slide Distance**, **Sparks** | each picture's slide into the frame, the sparks when it lands |
| Perk Data | **Value** | the perk's size (see the table above) |
| | **Requires**, **Max Picks** | when a box may offer it |
| PerkEffects (Player) | **Critical Multiplier** (2) | how much a critical hit multiplies the damage |
| | **Sprint Stamina Saving** (0.25) | each Sprinter: sprinting uses this share less stamina |
| | **Default Shield Seconds**, **Flash Color**, **Flash Seconds** | Last Stand |
| | **Tell Night Power** | the message when Night Power wakes up / rests |
| Crosshair (HUD) | **Reach Color** (yellow) | Keen Eye: the crosshair's color while a click would hit |
| PlayerCombat (Marko's, Player) | **Attack Range** (2.5) | the attack's reach before Long Reach (Long Reach x1.1 per pick) |
| PlayerGlide (Player) | **Glide Fall Speed** (2 m/s), **Dive Speed** (22 m/s), **Double Tap Seconds** (0.3) | Glide |
| PunchEffect (Player) | **Knockback** (3 m/s), **Heavy Knockback** (6), **Knockback Seconds** (0.25), **Knockback Lift** | the punches' push (Brawler multiplies it) |
| | **Critical Color** | the critical hit's burst |
| PlayerArmor (Player) | **Set Bonuses > Coins Per Kill** | copper 1, iron 2 |
| ObjectScatter (Sanyam's) | the chest rules' **Count** | how many boxes spawn (the tool only raises them) |

## A new perk

- **A stronger or weaker version of a kind that exists** (e.g. "Vitality II" = Max Health 0.4): a new Perk Data, then
  drag it into a box's **Available Perks** (and, for the new system's common / rare boxes, their ChestTier's **New
  System Perks**). No code.
- **A new kind:** a new line at the end of `ChestPerkType` (Marko's `PerkData.cs`, a `[Tony]` line + his log), what it
  does in **PerkEffects** (`Add`, `Describe`, `ResetEffects`), its name in **PlayerPerks.NameOf**, its picture in
  **IconArt.ForPerk**; then a Perk Data of that kind.
- **A fifth box:** a new line at the end of `LootTier` (ChestTier.cs), a box prefab with ChestTier set to it, and an
  ObjectScatter rule.

## Testing

1. Run the tool (and Build Main Title Scene), then Play from the title screen: NEW GAME > TESTING > **New chests**
   (default) or **Old chests**.
2. **New chests:** look at a common chest and press **E**: the pictures roll and land on a perk; press **E** while it
   rolls to skip. BUY (2 coins): the chest bursts into gold particles and you have the perk. Another common chest: 3
   coins. Press E at one, then walk away: closed, nothing paid; E again: the same perk, no roll. There's **no REROLL**
   yet. A rare box: 3 cards from 15 perks. In the desert: purple epic boxes (random) and orange legendary boxes (choose
   1 of 3, 150 coins); buy **Reroll** from one: now every box shows REROLL (1 coin at a common box: a new roll).
3. **Old chests:** every box shows 3 cards; no legendary boxes; REROLL only at the epic box, and only once you have
   the Reroll perk (from an epic box).
4. Die: every price is back to its start.
5. Player > **PerkEffects** > **⋮** > **Test: Every New Perk Once**, then:
   - punch a slime: some punches burst red (critical), every punch pushes it back, and you heal a little (Vampire);
   - let a slime hit you: it flinches (Thorns);
   - attack: the punches come faster (Attack Speed);
   - drag the sun's **Time Of Day** (Directional Light > DaylightCycle) to 0.8: "Night Power: +10%...", the health and
     stamina bars stay as full but their max grows (Tab > Info); back to 0.5: "Dawn: Night Power rests until night.";
   - jump and hold **Space**: you float down; in the air tap **Space** twice: you dive;
   - chop a tree / break a rock: one more wood / stone than before; kill a slime: an extra coin;
   - let a slime hit you to 0: "Last Stand!", a gold flash, you're at 1 health and safe for 2 seconds, and Last Stand
     is gone from the perks at the top; an epic box can offer it again;
   - open any box: REROLL is there (Reroll);
   - in first person walk slowly toward a tree: the crosshair turns yellow once a punch reaches it (Keen Eye), a
     little farther away than 2.5 m (Long Reach: 2.75 m); a fireball at night does its normal damage (Night Power
     doesn't change it).

## Problems

| Problem | Why / fix |
|---|---|
| The loot boxes are pink | Their materials are URP: run the tool (it makes Standard copies), or give them a Standard material by hand. |
| The loot boxes are plain grey (no colors) | The tool's first version took the model file (.fbx), whose own materials have no pictures. Run **Tools > World > Build Loot Chests** again: it takes DuNguyn's prefab (`SM_LootBox_01` / `02` / `03` in `Loot Box/Prefabs`) with its textured materials, makes their Standard copies in `Assets/Materials/Chests` and rebuilds the boxes. The Console warns if a box still has a material without a picture. |
| The New Game page has no Old / New chests choice | The title screen was built before: run **Tools > UI > Build Main Title Scene**. |
| A common box shows 3 cards in the new system | The scene has no roll pop-up (then the panel shows its one perk), or the box's ChestTier has Random In New System off: run the tool. |
| No burst when a perk is bought | The box's ChestTier has no Vanish Particles: run **Tools > Effects > Build Death Particles**, then this tool. |
| No rare / epic / legendary boxes spawn | The scene's ObjectScatter has no box rules: run the tool. Epic and legendary boxes are desert-only (the new world's desert, or the old world's sand); legendary boxes only in the new system. |
| Desert boxes spawn on grass | The ChestsDesert rule still uses Terrain Layer 0: run the tool (it moves it onto the sand layer). |
| A new perk does nothing | The Player has no PerkEffects (it adds itself when a perk needs it), or the kind isn't in PerkEffects.Add (the Console says "PerkEffects doesn't know..."). |
| Night Power never turns on | There's no DaylightCycle (the sun) in the scene, or it isn't night yet (Night starts at 0.77 by default). |
| No REROLL button | You don't have the **Reroll** perk yet (an epic box, or a legendary box in the new system), or you died (perks are lost). Old system: only epic boxes have REROLL. To allow rerolls from the start, untick **Reroll Needs Perk** on the box prefabs' ChestTier. |
| The crosshair never turns yellow (Keen Eye) | It turns yellow only when a click would really hit. Marko's attack measures its 2.5 m from the camera, so in third person (the camera behind the player) things are almost always out of reach; in first person walk up to a tree, a rock or a monster. |
| Glide doesn't work after picking it | PlayerGlide needs a Rigidbody on the Player (it has one) and is switched on by PerkEffects: check it's ticked while playing. |
| Prices don't go up | The box has no ChestTier (then Marko's random Min / Max Cost is used): run the tool. |
| "Nothing left in this box for you." | You have every perk that box can give (one-at-a-time perks), or the rest need another perk first. |
