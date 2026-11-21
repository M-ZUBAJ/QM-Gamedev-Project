# Loot Chests Setup (common, rare and epic boxes, and their perks)

**What the player sees:**

| Box | Looks like | Where | First price | Each perk bought from it | Perks |
|---|---|---|---|---|---|
| **Common** | Marko's wooden chest, yellow outline | grassland (10) and desert (15) | 2 coins | +1 (2, 3, 4, 5...) | Vitality, Stamina, Agility, Fireball, Second Wind, Precision*, Spring Legs* |
| **Rare** | blue loot box, blue outline | grassland (a few: 3) and desert (6) | 10 coins | x1.5 (10, 15, 23, 34...) | Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler |
| **Epic** | purple loot box, purple outline | desert only (2) | 100 coins | x2 (100, 200, 400...) | Last Stand, Critical Strike, Glide (each once a life) |

\* Precision only shows up once an epic box gave **Critical Strike**; Spring Legs only once it gave **Glide**.

- Every box of one kind costs the same, and a perk bought from one box makes all boxes of that kind dearer. The prices
  start again when you die (your perks are lost then too) and in a new game.
- Under the crosshair: **[E] Open rare box (coin) 10**. The perk panel's title is the box's name in its color.
- A box shows up to 3 perks. When you can't get more than 1 or 2 of its perks any more, it shows 1 or 2 cards; with
  none left it says "Nothing left in this box for you."
- **REROLL** (epic box only, bottom right of the panel): new perks for a tenth of the box's price (10 coins at a
  100-coin box), twice as much each time at that same box (10, 20, 40...); another epic box starts again at a tenth.
  With only the 3 epic perks there's nothing else to roll into yet ("NOTHING NEW TO ROLL"): add more epic perks and it
  comes alive.

**The new perks** (the numbers are each Perk Data's **Value**, so they're easy to change):

| Perk | Box | What it does | Value |
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
| Last Stand | epic | once a life, a killing blow leaves you at 1 health, and nothing can hurt you for 2 seconds (a gold flash) | 2 (seconds) |
| Critical Strike | epic | 5 % of your hits do double damage; their wind burst turns red | 0.05 |
| Glide | epic | hold Space in the air: you float down slowly; tap Space twice in the air: you dive to the ground | - |

Also new with them:
- **Punches push monsters back** a little (a heavy punch more). That's what Brawler makes stronger.
- **The armor set bonus is coins now:** the whole copper set gives +1 coin for every monster killed, the whole iron set
  +2 (instead of faster stamina). Bounty Hunter adds on top. See `Docs/Armor Setup.md`.

## Quick way: one click

1. Menu **Tools > World > Build Loot Chests**, then **Build**. It saves your open scenes first.
2. That's it: it changes and saves both game scenes itself, then opens your scenes again.

The tool:
- **Perk Data:** makes the 12 new perks in `Assets/Config/Perks` (`Perk_SecondWind`, `Perk_Precision`...), only if
  missing, with drawn icons (`Assets/UI/Perks`). A perk you changed keeps your numbers.
- **Common box:** puts **ChestTier** (Common, 2 coins, +1) on Marko's `Assets/Prefabs/WorldItems/Chest.prefab` and adds
  Second Wind, Precision and Spring Legs to its **Available Perks** (his four stay).
- **Rare / Epic box:** makes `Assets/Prefabs/WorldItems/Rare Chest.prefab` (DuNguyn's blue `SM_LootBox_01`, 1 m wide)
  and `Epic Chest.prefab` (the purple `SM_LootBox_03`, 1.2 m): a box to aim at, Marko's **PerkChest** with the box's
  perks, **ChestTier**, **StandOnGround** (stands on the ground) and **InteractOutline** (blue / purple). The pack's
  materials are made for URP (pink here), so it makes Standard copies with their glow in `Assets/Materials/Chests`, and
  imports the pack's 4096 px textures at 1024 (enough for a chest; the web game loads faster). The orange loot box
  (`SM_LootBox_02`) isn't used yet. Run again: the model is rebuilt, your numbers on the components stay.
- **Perk panel:** adds the REROLL button and the box's name as the title to `PerkSelectCanvas.prefab`
  (`Docs/Perk Panel Setup.md`).
- **Each game scene** (the Build Settings scenes with Sanyam's ObjectScatter: GameScene and ProceduralGenerateScene):
  - Sanyam's chest rules: **ChestsDesert** is moved onto the **sand** layer (it used the grass layer, so every chest
    spawned on grass) with at least 15 boxes; **Rare Chests Grass** (3), **Rare Chests Desert** (6) and **Epic Chests
    Desert** (2) are added right after the common ones, copying their settings; all chest rules share the spacing group
    **Chests** (20 m between any two boxes);
  - **PerkEffects** and **PlayerGlide** (switched off) on the Player;
  - PlayerArmor's set bonuses switched to coins;
  - then saved.

## How it works

| Part | Where | What it does |
|---|---|---|
| **ChestTier** | each box prefab | its kind, name, color, price rule and reroll; one shared count per kind (the price), reset on death and in a new game |
| **PerkChest** (Marko's) | each box prefab | E opens the panel; `[Tony]` lines: the price from ChestTier, only perks PerkRules allows (1 to 3), pays and counts the purchase, `TryReroll` |
| **PerkRules** | (code only) | which perks a box may offer: **Requires** picked already, picked fewer than **Max Picks** |
| **PerkSelectionUI** (Marko's) | the perk panel | `[Tony]` lines: 1 to 3 cards (the others hide), `Refresh` for a reroll |
| **PerkPanelInfo** | the perk panel | the box's name and color, the price, the REROLL button and its price |
| **PerkManager** (Marko's) | the scene | gives his four perks; a `[Tony]` `default:` hands every new kind to PerkEffects |
| **PerkEffects** | the Player | does what the new perks do (see the table), undone when the player dies |
| **PlayerGlide** | the Player (off until Glide) | hold Space while falling: fall at most Glide Fall Speed; Space twice in the air: dive |
| **Knockback** | added to a monster when a punch pushes it | its push; Marko's EnemyMovement (`[Tony]` line) doesn't walk while pushed |
| **MonsterBounty** | (code only) | the extra coins per kill: the armor set bonus + Bounty Hunter (Marko's EnemyCombat calls it, `[Tony]` line) |
| **PunchEffect** | the Player | red burst on a critical hit; pushes the monster a punch lands on |
| **ResourceNode** (Turki's) | trees, rocks | `[Tony]` lines: +Lumberjack wood / +Prospector stone or ore when it breaks |
| **PlayerPerks** | the Player | remembers each perk picked (for PerkRules), the HUD names and stats, loses everything on death |

Change logs: `Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md`,
`Docs/TonyEditMate/Turki/21-11-2026 Lumberjack And Prospector Perks.md`,
`Docs/TonyEditMate/Sanyam/21-11-2026 Loot Box Spawn Rules.md`.

## By hand (instead of the tool)

1. **A perk:** right click in `Assets/Config/Perks` > **Create > Game > Perk Data**. Display Name, Description, Icon,
   **Perk Type** (the kind), **Value** (how much). Under **Offered when**: **Requires** (drag the perk it needs) and
   **Max Picks** (1 = once a life).
2. **A box prefab:** drag the loot box model into the scene, scale it to about 1 m, put it under an empty object
   (the root) with its bottom at the root. On the root: **Box Collider** around the model, **PerkChest** (Interact Key
   E, Interaction Range 3, Despawn After Opened, its **Available Perks**), **ChestTier** (see Settings),
   **StandOnGround**, **InteractOutline** (Color, Show When Look At, Range 3). If the model is pink, give it a Standard
   material with the pack's textures (Albedo = BaseColor, Emission = Emissive). Drag the root into
   `Assets/Prefabs/WorldItems`.
3. **Where it spawns:** select the scene's ObjectScatter > **Rules** > +: Name, Prefab, Count, Terrain Layer (0 grass,
   1 sand in GameScene), Min Strength, Max Slope, Min Spacing 20, Spacing Group **Chests**.
4. On the Player: Add Component > **PerkEffects** and **PlayerGlide** (untick PlayerGlide).

## Settings

| Where | Setting | What it does |
|---|---|---|
| ChestTier (box prefab) | **Tier** | Common / Rare / Epic (every box of a kind shares one price) |
| | **Box Name**, **Color** | the prompt ("Open rare box") and the panel's title |
| | **Start Price**, **Growth** (Add / Multiply), **Grows By** | 2, Add, 1 (common); 10, Multiply, 1.5 (rare); 100, Multiply, 2 (epic) |
| | **Can Reroll**, **Reroll Share** (0.1), **Reroll Growth** (2) | the REROLL button: the first reroll = this share of the price, then x Reroll Growth at the same box |
| Perk Data | **Value** | the perk's size (see the table above) |
| | **Requires**, **Max Picks** | when a box may offer it |
| PerkEffects (Player) | **Critical Multiplier** (2) | how much a critical hit multiplies the damage |
| | **Sprint Stamina Saving** (0.25) | each Sprinter: sprinting uses this share less stamina |
| | **Default Shield Seconds**, **Flash Color**, **Flash Seconds** | Last Stand |
| PlayerGlide (Player) | **Glide Fall Speed** (2 m/s), **Dive Speed** (22 m/s), **Double Tap Seconds** (0.3) | Glide |
| PunchEffect (Player) | **Knockback** (3 m/s), **Heavy Knockback** (6), **Knockback Seconds** (0.25), **Knockback Lift** | the punches' push (Brawler multiplies it) |
| | **Critical Color** | the critical hit's burst |
| PlayerArmor (Player) | **Set Bonuses > Coins Per Kill** | copper 1, iron 2 |
| ObjectScatter (Sanyam's) | the chest rules' **Count** | how many boxes spawn (counts you change stay when the tool runs again) |

## A new perk

- **A stronger or weaker version of a kind that exists** (e.g. "Vitality II" = Max Health 0.4): a new Perk Data, then
  drag it into a box's **Available Perks**. No code.
- **A new kind:** a new line at the end of `ChestPerkType` (Marko's `PerkData.cs`, a `[Tony]` line + his log), what it
  does in **PerkEffects** (`Add`, `Describe`, `ResetEffects`), its name in **PlayerPerks.NameOf**, its picture in
  **IconArt.ForPerk**; then a Perk Data of that kind.
- **A fourth box** (e.g. the orange loot box as "Legendary"): a new line at the end of `LootTier` (ChestTier.cs), a box
  prefab with ChestTier set to it, and an ObjectScatter rule.

## Testing

1. Run the tool, then Play (title screen > NEW GAME, or GameScene). Walk around the grassland: common chests and a few
   blue boxes. In the new world's desert: more of both, and purple epic boxes.
2. Look at a blue box: blue outline, "E Open rare box (coin) 10". **E**: the title says RARE BOX in blue.
3. Buy a common perk (2 coins), then look at another common chest: it costs 3 now. Die: back to 2.
4. Player > **PerkEffects** > **⋮** > **Test: Every New Perk Once**, then:
   - punch a slime: some punches burst red (critical) and every punch pushes it back;
   - jump and hold **Space**: you float down; in the air tap **Space** twice: you dive;
   - chop a tree / break a rock: one more wood / stone than before; kill a slime: an extra coin;
   - let a slime hit you to 0: "Last Stand!", a gold flash, you're at 1 health and safe for 2 seconds;
   - Tab > Info: the Stamina, Sprint, Jump and Coins rows show the perks in green, and the perks' list shows them all.
5. At an epic box: REROLL shows, but says NOTHING NEW TO ROLL (only 3 epic perks exist). Pick Glide: the next common
   chests can offer Spring Legs; the next epic box offers only the other two.

## Problems

| Problem | Why / fix |
|---|---|
| The loot boxes are pink | Their materials are URP: run the tool (it makes Standard copies), or give them a Standard material by hand. |
| No rare / epic boxes spawn | The scene's ObjectScatter has no box rules: run the tool. Epic boxes are desert-only (the new world's desert, or the old world's sand). |
| Desert boxes spawn on grass | The ChestsDesert rule still uses Terrain Layer 0: run the tool (it moves it onto the sand layer). |
| A new perk does nothing | The Player has no PerkEffects (it adds itself when a perk needs it), or the kind isn't in PerkEffects.Add (the Console says "PerkEffects doesn't know..."). |
| Glide doesn't work after picking it | PlayerGlide needs a Rigidbody on the Player (it has one) and is switched on by PerkEffects: check it's ticked while playing. |
| Prices don't go up | The box has no ChestTier (then Marko's random Min / Max Cost is used): run the tool. |
| "Nothing left in this box for you." | You have every perk that box can give (once-a-life perks), or the rest need another perk first. |
