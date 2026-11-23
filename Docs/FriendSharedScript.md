# Using Tony's Scripts (Player & Survival)

For **Turki**, **Marko** and **Sanyam**: how your scripts can read and change the player's health, food, stamina,
inventory, death, day/night, items and buildings.

All of these scripts live in `Assets/Script/` and are **Tony's**: please don't edit them.
If you need something that isn't here (a new method, event or setting), ask me then I'll add it.

---

## 1. How to reach my scripts (read this first)

You can't just write `PlayerHealth.TakeDamage(10f)`. You first need **the** PlayerHealth that sits on the Player,
stored in a variable, and then you call the method on that variable: `health.TakeDamage(10f)`.
Everywhere in this file, names like `health`, `stamina` or `daylight` mean **the variable you made in this section**.

There are 3 ways to reach a script, depending on the script:

| Kind | Scripts | How you get it |
|---|---|---|
| **A. A component on the Player** | `PlayerHealth`, `PlayerHunger`, `PlayerStamina`, `PlayerController`, `PlayerJump`, `PlayerRespawn`, `ItemConsumer`, `BlueprintPlacer` | Find it once in `Start()`, keep it in a variable (below) |
| **B. A component on another object** | `DaylightCycle` (on the Directional Light) | Same as A: `FindFirstObjectByType<DaylightCycle>()` |
| **C. Static: no finding needed** | `PlayerInventory.Instance`, `GameRules`, `MenuState`, `KeyBindings`, `Crosshair.Target`, `BlueprintPlacer.IsPlacing`, `NotificationFeed.Show`, `GameSetup` | Write the class name and use it directly, from any script |
| **D. Assets (config files)** | `ItemData`, `BuildingData` | Make a `public ItemData meat;` field and drag the file in the Inspector |

### A + B: find it once, keep it in a variable

| Script | Write this in `Start()` | Then use it like |
|---|---|---|
| PlayerHealth | `health = FindFirstObjectByType<PlayerHealth>();` | `health.TakeDamage(10f);` |
| PlayerHunger | `hunger = FindFirstObjectByType<PlayerHunger>();` | `hunger.Eat(20f);` |
| PlayerStamina | `stamina = FindFirstObjectByType<PlayerStamina>();` | `stamina.Use(15f);` |
| PlayerController | `controller = FindFirstObjectByType<PlayerController>();` | `if (controller.IsSprinting) …` |
| PlayerJump | `jump = FindFirstObjectByType<PlayerJump>();` | `if (jump.IsGrounded) …` |
| PlayerRespawn | `respawn = FindFirstObjectByType<PlayerRespawn>();` | `respawn.Respawn();` |
| ItemConsumer | `consumer = FindFirstObjectByType<ItemConsumer>();` | `consumer.TryConsumeHand();` |
| BlueprintPlacer | `placer = FindFirstObjectByType<BlueprintPlacer>();` | `placer.Built += OnBuilt;` |
| DaylightCycle | `daylight = FindFirstObjectByType<DaylightCycle>();` | `if (daylight.IsNight) …` |
| The player's position | `player = FindFirstObjectByType<PlayerHealth>().transform;` | `player.position` |

Each variable is declared at the top of your class with its type, e.g. `PlayerHealth health;`.

### A full script that reaches everything

Copy it, delete what you don't need:

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    // ----- Tony's scripts (kind A + B): found in Start, used everywhere below -----
    PlayerHealth  health;
    PlayerHunger  hunger;
    PlayerStamina stamina;
    PlayerRespawn respawn;
    DaylightCycle daylight;
    Transform     player;      // the player's position and rotation

    // ----- Tony's assets (kind D): drag the files in the Inspector -----
    public ItemData meatData;

    void Start()
    {
        // FindFirstObjectByType searches the scene for that script. There is only one player, so this finds it.
        health = FindFirstObjectByType<PlayerHealth>();
        if (health == null)
        {
            Debug.LogWarning("No player in this scene.");   // e.g. a test scene without the Player
            return;
        }

        // Everything else of the player is on the same GameObject: GetComponent is quicker than searching again.
        player  = health.transform;
        hunger  = health.GetComponent<PlayerHunger>();
        stamina = health.GetComponent<PlayerStamina>();
        respawn = health.GetComponent<PlayerRespawn>();

        daylight = FindFirstObjectByType<DaylightCycle>();   // on the Directional Light, not on the player
    }

    void Update()
    {
        if (health == null) return;   // no player: do nothing

        // Kind A: through the variables
        if (!health.IsDead && daylight != null && daylight.IsNight)
        {
            // e.g. enemies are active at night
        }

        // Kind C: static, no variable needed
        if (MenuState.AnyOpen) return;                          // a menu is open: ignore input
        if (Input.GetKeyDown(KeyBindings.Swing)) Attack();      // the key is set in KeyBindings
        int wood = PlayerInventory.Instance.Count("Wood");      // the inventory, from anywhere
        if (GameRules.KeepInventory) { /* ... */ }               // world rules
    }

    void Attack()
    {
        if (stamina != null && stamina.Stamina >= 10f && !stamina.IsTired)
        {
            stamina.Use(10f);
            // ... hit something
        }
    }
}
```

### When something touches the player (no searching at all)

If your enemy, weapon or trap gets a **collision or trigger**, the player's scripts come with it.
`GetComponentInParent` also finds them when the hit was on a child part of the player:

```csharp
void OnTriggerEnter(Collider other)
{
    PlayerHealth hitHealth = other.GetComponentInParent<PlayerHealth>();
    if (hitHealth != null)          // null = it wasn't the player (a tree, the ground...)
    {
        hitHealth.TakeDamage(25f);
    }
}
```

### Listening to events (death, respawn, jump, eat, build, inventory change)

An event is "call my method when this happens". Subscribe with `+=`, and **unsubscribe with `-=`** when your
object is turned off or destroyed, or Unity calls a method on a deleted object and you get errors:

```csharp
PlayerHealth health;

void Start()
{
    health = FindFirstObjectByType<PlayerHealth>();
    if (health != null) health.Died += OnPlayerDied;          // instance event: through the variable
    PlayerInventory.Changed += OnItemsChanged;                // static event: through the class name
}

void OnDestroy()
{
    if (health != null) health.Died -= OnPlayerDied;
    PlayerInventory.Changed -= OnItemsChanged;
}

void OnPlayerDied()   { Debug.Log("The player died"); }
void OnItemsChanged() { Debug.Log("Items changed"); }
```

| Event | Kind | Subscribe like |
|---|---|---|
| Player died | on PlayerHealth | `health.Died += OnPlayerDied;` |
| Player respawned | on PlayerRespawn | `respawn.Respawned += OnRespawned;` |
| Player jumped | on PlayerJump | `jump.Jumped += OnJumped;` |
| Player ate something | on ItemConsumer | `consumer.Consumed += OnAte;` (gets the `ItemData`) |
| Player built something | on BlueprintPlacer | `placer.Built += OnBuilt;` (gets the `BuildingData`) |
| Items changed | static | `PlayerInventory.Changed += OnItemsChanged;` |
| Items came in (which, how many) | static | `PlayerInventory.ItemAdded += OnItemAdded;` (gets the `Slot` and the amount) |
| A world rule changed | static | `GameRules.Changed += OnRulesChanged;` |

### Common mistakes

- **Calling `health.TakeDamage(...)` without finding `health` first** → "NullReferenceException". Find it in `Start()`.
- **Finding it in `Awake()`**: the player might not be ready yet. Use `Start()`.
- **Calling `FindFirstObjectByType` every frame** in `Update()`: it's slow. Find once, keep the variable.
- **Forgetting `-=`** for events → errors after your object is destroyed.
- **No null check** in a scene without the player (e.g. your own test scene) → errors. Check `if (health == null) return;`.

---

## 2. What each person probably needs

In these tables, `health`, `stamina`, `daylight`, `player`… are the variables you found in **section 1**
(e.g. `health = FindFirstObjectByType<PlayerHealth>();` in `Start()`). Names starting with a capital
(`PlayerInventory.Instance`, `MenuState`, `GameRules`, `Crosshair`) are static and work as written.

### Turki: items, gathering, crafting

| You want to… | Use |
|---|---|
| Give the player items (mined, crafted) | `PlayerInventory.Instance.Add(data.Id, data.DisplayName, data.GetIcon(), amount, data)` (returns `false` if full) |
| Know how many the player has | `PlayerInventory.Instance.Count("Wood")` (the Item Data **file name**) |
| Take items away | `PlayerInventory.Instance.Remove("Wood", 2)` (returns `false` and takes nothing if not enough) |
| Check / pay a recipe | `itemData.CanCraft(inv)`, `itemData.Missing(inv)` ("2 Wood", or null), `itemData.TakeRecipeFrom(inv)` |
| Redraw your UI when items change | `PlayerInventory.Changed += Redraw;` (and `-=` in `OnDisable`) |
| Tell the player what they got ("+3 Wood") | Already done: everything given with `PlayerInventory.Instance.Add(...)` (harvesting in `ResourceNode`, crafting...) shows a message by itself (GainNotifier). Your files aren't changed |
| Show any other message | `NotificationFeed.Show("Crafted an Axe!");` (see `NotificationFeed` in section 3) |
| Item in hand (for tools) | `PlayerInventory.Instance.ActiveItem` (null = no hotbar slot selected) and `.data` = its Item Data |
| Make a new item | Select the object, **Tools > Items > Make Item From Selection** (see `Docs/Item Setup.md`) |
| Make an item that heals / feeds | Item Data > **Item Type = Consumable**, fill Health / Hunger / Stamina. Right click eats it (already works) |
| Open / close a crafting window | `MenuState.SetOpen(this, true/false)` + free the cursor (see section 4) |
| The crafting menu's key | `Input.GetKeyDown(KeyBindings.Crafting)` (default **C**, change it on the KeyBindings object). `CraftingMenu` already uses it; its old `openKey` field is gone |
| Crafting inside the inventory | A CraftingCanvas under `InventoryWindow > Pages > CraftingPage` now shows itself whenever the Crafting tab is open (Tony's `[Tony]` lines in `CraftingMenu`, see `Docs/TonyEditMate/20-10-2026Edit.md`). The scene-root CraftingCanvas still opens with C |
| Animate the player mining / picking up | `PlayerAnimator.Play(PlayerAction.Mine);` / `PlayerAnimator.Play(PlayerAction.Gather);` (upper body only; does nothing if the player has no character) |
| Tabs on your crafting list | Tony's **Tools > UI > Build Crafting Menu** (20/11/2026, log `Docs/TonyEditMate/Turki/20-11-2026 Crafting Tabs.md`) added tabs over the list in your CraftingCanvas prefab: **All** (default), **Materials**, **Food**, **Tools**, **Ammo** (arrows, since 22/11/2026, log `Docs/TonyEditMate/Turki/22-11-2026 Ammo Tab And Better Armor Arrow.md`), **Armor**, sorted by each item's **Item Type**. Your CraftingMenu script is unchanged (Tony's CraftingRow rows ask Tony's CraftingTabs whether to show). A new item lands in its tab by itself. A weaker armor piece gets a blue arrow while the player has a better one for that slot (22/11/2026) |
| Armor in your crafting list | Tony's **Tools > Items > Build Plate Armor** (20/11/2026, log `Docs/TonyEditMate/Turki/20-11-2026 Plate Armor In The Crafting List.md`) added the 6 iron armor items (Iron Helmet ... Iron Boots, Iron recipes; first called Plate ..., renamed the same day, same files) to CraftingCanvas's Craftables, after the tools. An armor's Item Data has Item Type **Armor** (`item.IsArmor`, `item.Armor.slot / protection / set / look`). Guide: `Docs/Armor Setup.md` |
| Gold and desert rocks (new) | Tony's **Tools > World > Build Desert Content** (20/11/2026, guide `Docs/Desert Setup.md`) made a **Gold** item (`Assets/Config/Items/Gold`, a copy of your Iron item: Item Type Material, Mined With = Iron) with a gold cube prefab (copy of your Iron prefab), and two **Prefab Variants** of your rocks in `ResourceNodes/Rocks`: **GoldNode** (of IronNode: drops 2 - 4 Gold, 40 health, gold colored) and **SandstoneNode** (of StoneNode: drops 4 - 6 Stone, 75 health = harder to break, sandstone colored). Your IronNode / StoneNode and ResourceNode script aren't changed. Gold spawns in the new world's desert caves; a recipe can use it like any item |
| Copper and Iron armor and set boxes (changed) | Tony's two armor sets (20/11/2026, log `Docs/TonyEditMate/Turki/20-11-2026 Reinforced Iron Armor And Set Panels.md`, see its Copper-first update): **Copper** Helmet ... Boots (`Assets/Config/Items/Armor/Copper...`) crafted from Copper (5 / 4 / 8 / 3 / 7 / 4), copper colored, 10 % for all six, stamina 50 % faster; **Iron** Helmet ... Boots (`.../Iron...`), each = its Copper piece + Iron (4 / 3 / 6 / 2 / 5 / 3), steel grey, 16 % for all six, stamina 100 % faster (iron is the higher tier; until later on 20/11/2026 Iron came first and "Reinforced Iron" was the upgrade). Your `Craft()` got 5 `[Tony] added` lines so a WORN Copper piece can be upgraded (Tony's `PlayerArmor.LendForUpgrade / FinishUpgrade`). Each armor set's rows now sit in one box in the set's color with its name, protection and bonus (Tony's `CraftingSetPanels` on your Panel + a switched-off `Scroll > SetPanel` in your prefab). Your `CopperArmor` item (a Material placeholder, not in the list) is untouched |
| Gold tools and gold armor in your crafting list (changed) | Tony's tools (21/11/2026, log `Docs/TonyEditMate/Turki/21-11-2026 Gold Tools And Gold Armor In The Crafting List.md`): the **Gold Pickaxe / Axe / Sword** got recipes (the Iron one + 3 Plank + 6 Gold, the sword 9 Gold) and the **Gold** armor set (Gold Helmet ... Boots, each = its Iron piece + Gold, 25 % for all six, +4 coins per kill and 10 % off every chest) was made, so your CraftingCanvas prefab's **Craftables** list has them (tools after the Iron tools, the gold set after the iron set, in its own box). The iron set now protects 15 % (was 16 %). Your code is untouched |
| Diamond tools and diamond armor in your crafting list (changed) | Tony's tools (21/11/2026, logs `Docs/TonyEditMate/Turki/21-11-2026 Diamond, Snow Rocks And Snowy Pines.md` and `.../21-11-2026 Diamond Armor In The Crafting List.md`): the **Diamond** item (a copy of your Iron, from the new world's snow caves) gave the **Diamond Pickaxe / Axe / Sword** their recipes (the Gold one + 3 Plank + 6 Diamond, the sword 9), and the **Diamond** armor set was made (Diamond Helmet ... Boots, each = its Gold piece + Diamond, 40 % for all six, +8 coins per kill and 10 % off every chest), so your **Craftables** list has them (after the Gold tools / the gold set, in its own box). Your code is untouched |
| Lumberjack / Prospector perks (changed) | Tony's 2 `[Tony] added` lines in your `ResourceNode.Break()` (21/11/2026, log `Docs/TonyEditMate/Turki/21-11-2026 Lumberjack And Prospector Perks.md`): +1 wood per tree / +1 stone or ore per rock for each of those rare-box perks (`PerkEffects.ExtraDrop`), through your own `Give`. Nothing changes without the perks |
| Food that does more | Tony's **Gourmet** perk makes good food 10 % stronger (Tony's ItemConsumer); your Consumable numbers stay as they are |
| Your rocks are low-poly rocks now | Tony's **Tools > World > Use Low Poly Rocks** (20/11/2026, log `Docs/TonyEditMate/Turki/20-11-2026 Low Poly Rocks That Break.md`) rebuilt your StoneNode, CopperNode and IronNode prefabs: a random rock shape from Broken Vector's free Low Poly Rock Pack (Tony's RockLook), a Mesh Collider in that shape instead of the box, grey / copper / blue-grey stone, standing on the ground (StandOnGround). When your ResourceNode breaks one, Tony's RockBreak bursts it into chunks of the same stone, gravel and dust. Your script and its values (health, drops) are unchanged. Guide: `Docs/Rock Setup.md` |
| Your trees fall now | Tony's **Tools > Effects > Build Tree Fall** (19/11/2026, log `Docs/TonyEditMate/Turki/19-11-2026 Trees Fall Into Wood Chips.md`) put Tony's **TreeFall** on your TreeNode prefab: when your `ResourceNode` breaks a tree (`currentHealth` 0, `Destroy`), a look-only copy falls over away from the player and breaks into brown wood chips (the copy's open trunk bottom is closed with cut wood, so nobody sees through it). Your script and the drops are unchanged; berry bushes are skipped. Guide: `Docs/Tree Fall Setup.md` |
| An upgraded tool keeps its slot | Your `CraftingMenu.Craft` has two `[Tony]` lines (19/11/2026, log `Docs/TonyEditMate/Turki/19-11-2026 Upgraded Tool Keeps Its Slot.md`): an upgrade (Wooden Axe -> Stone Axe) goes into the slot the old tool was in (`PlayerInventory.SlotOf` + `AddInto`), not the first free slot. Anything that isn't an upgrade is added exactly as before |
| Crafted tools and food go to the hotbar (changed) | One `[Tony] replaced` line in your `CraftingMenu.Craft` (21/11/2026, log `Docs/TonyEditMate/Turki/21-11-2026 Crafted Tools And Food Go To The Hotbar.md`): `inv.AddCrafted(item, item.CraftAmount, oldToolSlot)`. Tools and consumables go to the hotbar first (a stack of the same item there, else its leftmost empty slot, else the inventory); an upgrade still takes the old tool's hotbar slot; materials and armor as before. The crafting rows also show a green tick on armor the player already has (Tony's CraftingRow) |
| Tool recipes are a chain | Since 19/11/2026 (Tony's **Tools > Items > Update Tool Recipes**): Wooden Pickaxe / Axe = 3 Plank + 3 Wood, Wooden Sword = 3 Plank + 6 Wood; Stone = the Wooden one + 3 Plank + 6 Stone (sword 9); Iron = the Stone one + 3 Plank + 6 Iron (sword 9). A tool can be an ingredient: `Recipe.CanAfford` / `TakeFrom` count it by Item Id like anything else, so your CraftingMenu needed no change. Table: `Docs/Crafting Menu Setup.md` |
| Your trees are low-poly trees now | Tony's tool (**Tools > World > Use Low Poly Grassland Trees**) changed only the look and pivot of your `TreeNode.prefab`. It's a Round Oak from Broken Vector's pack, standing on the foot of its trunk. Your `ResourceNode` and its settings (Wood, Apple bonus, health) aren't changed. 5 more looks are Prefab Variants in `Assets/Prefabs/ResourceNodes/Trees`, so they follow TreeNode's drops and health. Log: `Docs/TonyEditMate/Turki/12-11-2026 Low Poly Grassland Trees.md` |
| Your sheep (AnimalNode) | Not used in GameScene anymore: deer took its place (`DeerNode`, Tony's **Tools > World > Build Wild Animal**). Your `AnimalNode.prefab` and `Wander` script aren't changed. DeerNode got a copy of your sheep's ResourceNode settings (Meat 1 to 3, health 100). Change the deer's drops on DeerNode's ResourceNode. Guide: `Docs/Animal Setup.md` |
| An outline on your berry bushes | Add Tony's **InteractOutline** to `BerryNode`: Color green, Show When *Look At*, Range = your **Collect Range**. It glows while E would pick it, and goes away with the bush. `InteractOutline.LooksAt(ray, range, transform)` is the same "am I looking at it" test, skipping the player's body like yours. Guide: `Docs/Interact Outline Setup.md` |
| Keep Collect With Key on your bushes | Tony's **Tools > World > Set Up World Spawning** tells a berry bush from a tree by your **Collect With Key** tick (both have Resource Type Tree, the first choice), so the bushes don't get the trees' spacing. Nothing of yours is changed |
| Your crafting menu (changed) | Tony's **Tools > UI > Build Crafting Menu** (19/11/2026, log `Docs/TonyEditMate/Turki/19-11-2026 Crafting Menu Icons And Tier Tools.md`): each row now shows the item's picture and its recipe as pictures with "have/need" (green / red), through Tony's `CraftingRow` on a new row prefab (one `[Tony]` line in your `FillRows` calls `CraftingRow.Show(item)`; a row without it still gets your text). Your panel is 960 x 640 with the rows in a scroll list, and Craftables lists Plank, Stew, then the Wooden / Stone / Iron tools. Guide: `Docs/Crafting Menu Setup.md` |
| The bow and arrows in your crafting list (new) | Tony's **Tools > Items > Build Bow And Arrows** (22/11/2026, log `Docs/TonyEditMate/Turki/22-11-2026 Bow And Arrows In The Crafting List.md`, guide `Docs/Bow Setup.md`): **Wooden Bow** (10 Wood + 10 Plank; a Tool of Kind **Bow**) after the Wooden tools, the **Spruce Bow** (the Wooden Bow + 20 Spruce Wood + 15 Iron, later the same day: an upgrade, so your `Craft` puts it in the Wooden Bow's slot) after the Iron tools, and 5 arrows after the tools, 4 per craft: Stone Arrow = 2 Wood + 2 Stone, Copper / Iron / Gold / Diamond Arrow = 4 arrows of the kind before + 1 of that metal. Arrows are a new Item Type, **Ammo** (an Ammo section: Damage, Head); they're in the Ammo tab and go to the inventory when crafted. A bow's Item Data has a **Bow** section (Damage added to the arrow's, Draw Seconds, Arrow Speed): `itemData.IsBow` / `itemData.Bow.damage`, `itemData.IsAmmo` / `itemData.Ammo.damage` if you need them. Your scripts aren't changed |
| Your ResourceNode skips its empty Update (changed) | One `[Tony] added` line at the end of your `Start()` (22/11/2026, log `Docs/TonyEditMate/Turki/22-11-2026 Trees And Rocks Skip Their Empty Update.md`): `if (!collectWithKey) enabled = false;` Only berry bushes need your Update (the key); every tree, rock, ore and deer switches it off after setting its health (the new world has about 2000 near the player, each costing a call every frame). TakeDamage, Break and the drops work as before; the component just shows unticked in the Inspector while playing |
| Spruce Wood and the snow's trees (new) | Tony's **Tools > World > Build Snow Content** (22/11/2026, log `Docs/TonyEditMate/Turki/22-11-2026 Spruce Wood, Tough Snow Trees And The Spruce Bow.md`, guide `Docs/Snow Setup.md`): a new item **Spruce Wood** (`Assets/Config/Items/SpruceWood` + `Assets/Prefabs/Item/SpruceWood`, copies of your Wood item and log in a darker brown; Item Id `SpruceWood`). The snow's 4 trees (Prefab Variants of your TreeNode) now have their own **Drop Item = Spruce Wood** and **Max Health = 300** on their ResourceNode (only the numbers, not your script); their drop amounts and Apple bonus still follow TreeNode. Spruce Wood's **Mined With = Iron**: Tony's ToolRules now reads a wood's Mined With as the axe its tree needs (like an ore's pickaxe), so only an Iron Axe or better chops the snow's trees ("Needs an Iron Axe or better"); your other trees drop Wood (None) and are unchanged. Spruce Wood isn't Wood: recipes that ask for Wood don't take it |
| Tools and mining tiers | Item Data has a new **Item Type: Tool** (Kind + Tier) and, for materials, **Mining > Mined With**: the weakest pickaxe that can mine a rock that drops it. Copper and Iron are now **Stone** (a hand can't mine them), Stone stays None. New gold / diamond ores: set Mined With to Iron / Gold. Your ResourceNode got one `[Tony]` read-only line, `DropItem`. Your crafting list now offers **Iron Sword / Stone Axe / Stone Pickaxe** (same recipes as the old Sword / Axe / PickAxe). 15 tools with recipes are in `Assets/Config/Items/Tools`. Your panel fits ~8 rows: add a Scroll View to offer more. Log: `Docs/TonyEditMate/Turki/12-11-2026 Tier Tools.md`. (Since 19/11/2026 the list scrolls and shows every Wooden / Stone / Iron tool, and the tool recipes are a chain: see the rows above.) |
| Human enemies break your ResourceNodes (changed) | Tony's human enemies in the new world chop trees, mine rocks and pick berries like the player. `[Tony] added` at the end of your `ResourceNode` (23/11/2026, log `Docs/TonyEditMate/Turki/23-11-2026 Human Enemies Gather Like The Player.md`): `TakeDamageFor(damage, taker)` (like your TakeDamage, but the drop goes to `taker`, the human's bag, with your own Drop Min / Max and bonus) and `CollectWithKey` (read only). Your TakeDamage / Break / Give are unchanged |

**Example: crafting one item**

```csharp
PlayerInventory inv = PlayerInventory.Instance;
if (axeData.TakeRecipeFrom(inv))   // takes 2 Wood, or nothing if the player doesn't have them
{
    inv.Add(axeData.Id, axeData.DisplayName, axeData.GetIcon(), axeData.CraftAmount, axeData);
}
else
{
    Debug.Log("Missing " + axeData.Missing(inv));
}
```

Every Item Data already has a **Recipe**, **Craft Amount** and **Crafted At** (Crafting page / Furnace) in its Inspector.
The inventory's **Crafting** tab (`InventoryCanvas > InventoryWindow > Pages > CraftingPage`) is an empty page ready for your UI.

### Marko: combat, enemies, animals

| You want to… | Use |
|---|---|
| Hurt the player | `health.TakeDamage(10f)` (since 20/11/2026 the armor the player wears makes it smaller: Tony's PlayerArmor; your EnemyAttack needs no change) |
| Heal the player | `health.Heal(10f)` |
| Is the player dead? | `health.IsDead` (stop attacking a dead player) |
| Know when the player dies | `health.Died += OnPlayerDied;` (fires once per death) |
| Know when the player comes back | `player.GetComponent<PlayerRespawn>().Respawned += OnRespawn;` (e.g. despawn enemies) |
| Spend stamina (attack, dodge) | `if (stamina.Stamina >= cost && !stamina.IsTired) stamina.Use(cost);` |
| Is it night? | `DaylightCycle.Current.IsNight` (night = 0.77 to 0.23 Time Of Day, set on the sun) |
| Spawn enemies when night starts | `DaylightCycle.NightStarted += OnNight;` (gets the day number). **Already wired** into your `EnemySpawner` by Tony (lines marked `[Tony]`, see `Docs/TonyEditMate/18-10-2026Edit.md`) |
| Which day / night it is | `DaylightCycle.Current.Day` (for "more enemies each night") |
| Time of day | `DaylightCycle.Current.TimeOfDay` (0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset) |
| Show your coins on the HUD | Already done: `CoinCounter` reads `PlayerCoins.Instance.Coins` and `CoinsChanged` (your file isn't changed) |
| "+2 Coins" when an enemy is defeated | Already done: `GainNotifier` watches `CoinsChanged`, so every `AddCoins(...)` shows a message (your files aren't changed) |
| Show any other message | `NotificationFeed.Show("A boss appeared!");` (see `NotificationFeed` in section 3) |
| Make enemies stronger on Hard | `if (GameSetup.ChosenDifficulty == GameSetup.Difficulty.Hard) { ... }` (null = Normal, the game as it is). How often your `EnemySpawner` spawns already follows the New Game page (22/11/2026): when the scene loads, GameSetup multiplies your public **Night 1 - 5 Spawn Interval** (x2 on Easy = half as many monsters, / 1.5 on Hard) and Peaceful turns `spawnAtNight` off. Your file isn't changed: keep tuning the intervals in GameScene as Normal, and the New Game page's explanation ("A monster every 10 seconds on night 1...") follows when GameScene is saved |
| Drop food / items from an animal | see the example below |
| What the player aims at | `Crosshair.Target` (a GameObject, or null) and `Crosshair.HitPoint` |
| Is a menu open (don't attack / dodge then) | `MenuState.AnyOpen` |
| Read keys | `Input.GetKeyDown(KeyBindings.Swing)` (Swing = left mouse, free for attacking) |
| Is the game paused (Esc menu) | `PauseMenu.IsOpen` (Tony's, 21/11/2026, guide `Docs/Pause Menu Setup.md`): time is stopped (`Time.timeScale = 0`) and `MenuState.AnyOpen` is true, so a script that already skips its input while a menu is open needs nothing more. Something timed with `Time.unscaledTime` keeps running while paused: use `Time.time` / `Time.deltaTime` for game things |
| The player's mouse settings | `MouseSettings.Sensitivity` (1 - 10, 5 = normal), `MouseSettings.Multiplier` (= Sensitivity / 5), `MouseSettings.InvertY`, event `MouseSettings.Changed` (Tony's, saved in PlayerPrefs; set on Settings > CONTROLS, title screen or Esc menu). The camera already uses them |
| Your PlayerCombat with a bow (changed) | One `[Tony] added` line in your `Update()` (22/11/2026, log `Docs/TonyEditMate/Marko/22-11-2026 Bow Shoots Instead Of Punching.md`): `if (ToolRules.BowInHand) return;` With the Wooden Bow in the hand a left click no longer punches: Tony's `BowShooter` draws (hold) and shoots (let go) an arrow, and the arrow (`ArrowProjectile`) hurts the enemy where it lands through your `EnemyCombat.TakeDamage` (your dodge and coins still count; arrows hurt a turtle in its shell, like your fireball). Guide `Docs/Bow Setup.md` |
| Damage numbers on hits | Already done for every hit of the player (21/11/2026): your PlayerCombat's hits go through Tony's `PunchEffect.Hit`, the fireball and Thorns through `PerkEffects`, and they pop the damage the hit really did (white, or big orange "CRITICAL 24!"). Your files aren't changed for it. A new way of hurting something: `DamageNumbers.Show(point, damage, critical, creature)` (it checks the player's DISPLAY settings itself; see `DamageNumbers` in section 3) |
| Your fireball in menus (changed) | One `[Tony] added` line in your `PlayerFireball.Update()`: no fireball while `MenuState.AnyOpen` (Q in the pause menu cast a fireball that hung in the stopped time). Log: `Docs/TonyEditMate/Marko/21-11-2026 No Fireball While A Menu Is Open.md` |
| Bigger monsters drop more coins (changed) | One `[Tony] added` field and one `[Tony] replaced` line in your `EnemyCombat` (22/11/2026, log `Docs/TonyEditMate/Marko/22-11-2026 Bigger Monsters Drop More Coins.md`): `public float coinMultiplier = 1f;` (**Coin Multiplier**), and `Die()` gives `Mathf.RoundToInt(Random.Range(3, 7) * coinMultiplier)`. At 1 (every prefab, the slime) it's your 3 - 6 as before; Tony's NightMonsters sets it when a turtle (1.5 = 4 - 9), Chest Monster (2 = 6 - 12) or Beholder (3 = 9 - 18) spawns. Set it on an enemy prefab yourself for a monster that isn't in NightMonsters |
| Your EnemySpawner: at most 20 monsters on Easy only (changed) | Earlier on 22/11/2026 Tony capped every night at 20 monsters alive at once, then took it out (log `Docs/TonyEditMate/Marko/22-11-2026 Night Monsters Stop Piling Up.md`, UNDONE). Now it's only for **Easy** on the NEW GAME page: one `[Tony] edited` line in your `SpawnThroughoutNight` asks Tony's NightMonsters `RoomForMore()` before each spawn (log `Docs/TonyEditMate/Marko/22-11-2026 Easy Keeps At Most 20 Monsters.md`). On Normal and Hard it always says yes: your `enemyCounter` is still never counted up, so **Max Enemy Number** doesn't stop spawns, and the night's monsters gather until dawn, as you made it |
| Your EnemySpawner keeps going after night 5 (changed) | One `[Tony] replaced` line in your `GetSpawnInterval` (22/11/2026, log `Docs/TonyEditMate/Marko/22-11-2026 Monsters Keep Coming After Night 5.md`): its `default:` returned 0, so nights 6 and later had no monsters (the NEW GAME page allows up to 30 days to win). Now every night after night 5 uses your **Night 5 Spawn Interval**. Tony's NightMonsters got a **Most Share** (95 %) so its growing shares can't squeeze the Beholder out on those later nights |
| Your fireball on ores and spruce (changed) | 6 `[Tony] added` lines in your `FireballProjectile.OnCollisionEnter` (22/11/2026, log `Docs/TonyEditMate/Marko/22-11-2026 Fireball Leaves Ores And Spruce Alone.md`): a rock or tree that needs a tool (`ToolRules.NeededFor(resource) != ToolTier.None`: copper, iron, gold and diamond ores, the snow's spruce) isn't hurt by a fireball; it says which tool it needs ("Needs an Iron Axe or better") and the fireball goes. Plain stone, grassland trees, animals and enemies are hit as before |
| The interact key: chests and berry bushes (changed) | `KeyBindings.Interact` = **E** since 21/11/2026 (it was the right mouse button and unused). Marko's `PerkChest` (`InteractKey` returns it) and Turki's berry `ResourceNode` read it instead of their own `interactKey` / `collectKey` fields (commented out, `[Tony]` lines; logs `Docs/TonyEditMate/Marko/21-11-2026 Chest Key On The Key Bindings Page.md`, `Docs/TonyEditMate/Turki/21-11-2026 Berry Key On The Key Bindings Page.md`). Players change it on the title screen's Settings > Key Bindings ("Open chest / Pick berries"). Something new to use with E: `if (Input.GetKeyDown(KeyBindings.Interact)) ...`. **Grab** (F) stays the key for items lying on the ground |
| Your enemy is now an animated slime | `Assets/Prefabs/Enemy/SlimeEnemy.prefab` (made by **Tools > Enemies > Build Monster Enemy**, guide `Docs/Enemy Setup.md`) uses your EnemyCombat / EnemyMovement / EnemyAttack plus Tony's `EnemyAnimator`, and the EnemySpawner now spawns it. Tony's `[Tony]` lines in your 4 enemy scripts: physics movement (walls stop it), spawning on the ground, attack animation, death animation before removal. Every change and how to undo it: `Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md`. Your old `Enemy.prefab` still works |
| Enemies and animals dying into particles | Already done for the slime and the deer: Tony's `DeathEffect` (on the prefab, **Tools > Effects > Build Death Particles**, guide `Docs/Death Effect Setup.md`). Harmful = has your `EnemyAttack` = red, else green. Your `EnemyCombat.Die()` is unchanged: Tony's `EnemyAnimator.PlayDeath()` (already called from your `[Tony]` lines) asks DeathEffect to turn the body into red particles during the death animation and returns how long that takes. A new enemy prefab: add `DeathEffect` and drag `Prefabs/Effects/DeathParticles` into it, or run the tool. Burst one yourself: `GetComponent<DeathEffect>().Burst();` |
| Play an enemy animation from your code | `GetComponent<EnemyAnimator>().PlayAttack();` (`PlayDeath()` is already called by `EnemyCombat.Die()`; `IsDead` = it's dying or burning away) |
| Your night enemies at dawn (changed) | Your `EnemySpawner.DespawnEnemies()` (called at dawn from `OnDayStarted`) now burns them away into **orange embers** instead of blinking out: one `[Tony] replaced` line calls Tony's `EnemyAnimator.Vanish(enemy)` (stops it at a random moment within 1.5 s, orange dissolve from its `DeathEffect`, removed ~1.7 s later; no coins, no kill counted). Your **Test Despawn Enemies** shows it. Also one `[Tony] added` line in `SpawnEnemy()`: a spot past the world's barrier goes to the player's other side. Log: `Docs/TonyEditMate/Marko/20-11-2026 Night Enemies Burn Away At Day.md`, guide `Docs/Death Effect Setup.md` |
| Remove an enemy without killing it | `EnemyAnimator.Vanish(enemyGameObject);` (orange burn, no coins; an enemy without EnemyAnimator is destroyed at once) |
| Night monsters: a new one every night (changed) | Tony's `[Tony]` lines in your `EnemySpawner.SpawnEnemy()` (21/11/2026, log `Docs/TonyEditMate/Marko/21-11-2026 Night Monsters And The Turtle Shell.md`, guide `Docs/Night Monsters Setup.md`): Tony's **NightMonsters** on your EnemySpawner's object picks each spawn's monster (night 1 your Enemy Prefab, the slime; from night 2 also the **turtle**, about 3 in 10, at most 3 at once; from night 3 the **Chest Monster** and from night 4 the flying **Beholder**, 22/11/2026, RPG Monster Partners: your EnemyMovement / EnemyAttack / EnemyCombat on them, unchanged, and the Beholder also has Tony's **FlyingEnemy**, which only sets its height (3.5 m up out of sword reach, it dives to bite, then climbs away); a night-5 monster: add it under NightMonsters > Monsters with First Night 5), gives a picked one its own health / damage after your scaling, and finds a spot 24 - 34 m away where the player can't see it (so nothing pops up in front of them). The turtle has Tony's **ShellDefense**: now and then it hides in its shell (your EnemyMovement / EnemyAttack switched off meanwhile); a melee hit then bounces off and hurts the player (15) — the shell check is in Tony's PunchEffect, so your PlayerCombat is unchanged; the fireball still hurts it. Without NightMonsters your spawner works as before |
| A day monster in the desert (until 21/11/2026) | No longer used: the turtle became a night monster (above). Tony's **TurtleShellEnemy** (20/11/2026, **Tools > World > Build Desert Content**, guide `Docs/Desert Setup.md`): RPG Monster DUO's TurtleShell made into an enemy with YOUR EnemyCombat / EnemyMovement / EnemyAttack (unchanged) + Tony's EnemyAnimator, DeathEffect and **EnemyAggro**. Tony's **DesertMonsterSpawner** (not your EnemySpawner) keeps up to 5 on the sand around the player in the day (90 health + 10 a day, 12 damage + 2 a day), out of the player's sight; at night they dissolve into sand. EnemyAggro gives your scripts the player as `target` only while the player is within 14 m (or after the turtle is hurt) and sets it back to null beyond 32 m (it also zeroes the Rigidbody's speed then, because your FixedUpdate keeps the last velocity when the target is null). Your night slimes are unchanged |
| An enemy that waits until the player is near | Add Tony's `EnemyAggro` to its prefab: Notice Range / Give Up Range in meters, `IsChasing` |
| Don't spawn outside the world | `if (!WorldBarrier.Inside(spot, 2f)) { /* pick another spot */ }` (true when the scene has no barrier). See `WorldBarrier` in section 3 |
| Animate the player's attack / fireball | Already done: Tony's `[Tony]` lines in your `PlayerCombat.Attack()` play what's in the hand: nothing / a non-tool = `Punch`, sword or axe = `Attack`, pickaxe = `Mine` (Attack and Mine both show Mixamo's downward axe chop). Fireball: `PlayerAnimator.Play(PlayerAction.Cast);` right after its `Instantiate(...)`. Upper body only, so the legs keep running. The flinch when the player is hurt already plays by itself |
| Your tools | Replaced by 15 tier tools (pickaxe / axe / sword in Wood, Stone, Iron, Gold, Diamond, Abduo Bricks' Low Poly Tool Pack): Item Data with **Item Type = Tool** (Kind + Tier), held in the character's hand (Tony's `ToolInHand`). Your damage numbers stay (a wooden tool does them); `[Tony]` lines map the tool to your `"Sword"` / `"Axe"` / `"PickAxe"` checks (`ToolRules.WeaponName`) and add +25 % per tier at the tool's job (`ToolRules.Boost`). Copper / iron need a Stone pickaxe (`ToolRules.CanMine`). Your fireball works with any sword (`ToolRules.IsSword`). Your GiveSwordForTesting / EquippedWeaponDisplay left the scene (GameRules > **Tool Test** gives all tools now). Log: `Docs/TonyEditMate/Marko/12-11-2026 Tier Tools.md`, guide `Docs/Tool Setup.md` |
| What's in the player's hand | `ToolStats tool = ToolRules.ToolIn(PlayerInventory.Instance.ActiveItem);` (null = a bare hand), then `tool.kind` (Pickaxe / Axe / Sword) and `tool.tier` (Wood ... Diamond) |
| Bare-handed punches | Tony's `PunchCombo` (on the Player) picks them: left click = left / right jabs in turn, right click = heavy left hook / heavy right punch in turn, which hit and cost x2. `[Tony]` lines in your `PlayerCombat` ask `punchCombo.Next(...)` and call `Attack(punch)`; heavy damage = `PunchCombo.Power(swing)`. Log: `Docs/TonyEditMate/Marko/13-11-2026 Punch Combo.md`, guide `Docs/Mixamo Animations.md` |
| Walking into reach still hits (changed) | One `[Tony] edited` line in your `PlayerCombat.Attack()` (23/11/2026, log `Docs/TonyEditMate/Marko/23-11-2026 Walking Into Reach Still Hits.md`): the ray is `AttackReach + PunchEffect.LungeReach` long (3 m more). Tony's PunchEffect hits a target found past the reach only if the player has walked into reach when the blow lands (else a miss); targets in reach at the click are hit as before. Your damage code is unchanged |
| A burst where an attack hits | Bare-handed hits already burst, and they hurt **when the fist lands**: Tony's `[Tony] replaced` lines in your `PlayerCombat.Attack()` call `PunchEffect.Punch(target, hit, () => target.TakeDamage(damage))` instead of `TakeDamage` at once. It waits the Impact Delay (about 0.3 s), hurts, then bursts on the spot that was hit, even on a running deer. Weapons still hit at once. For a burst only (sword, fireball): `PunchEffect.Play(hit.point, hit.normal);`. Log: `Docs/TonyEditMate/Marko/12-11-2026 Punch Wind Burst.md` |
| Coins when the player dies | They go back to the starting amount: Tony's `[Tony]` lines in your `PlayerCoins` (Reset On Death tick box, `ResetCoins()`). Log: `Docs/TonyEditMate/Marko/10-11-2026 Coins Reset On Death, Perk Menu Closes, Punch.md` |
| Starting coins (changed) | Your test line `AddCoins(20)` in `PlayerCoins.Awake()` is now `AddCoins(startingCoins)`: Tony's `[Tony]` field **Starting Coins** (20) on your PlayerCoins (22/11/2026). The title screen's NEW GAME page can change it (Tony's GameSetup calls the `[Tony]` method `SetStartCoins(amount)` after the scene loads, before anything starts), and dying goes back to it. Log: `Docs/TonyEditMate/Marko/22-11-2026 Starting Coins On The New Game Page.md` |
| Your perk chest and panel (changed again) | Tony's `[Tony]` lines (19/11/2026, log `Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md`): E opens the panel while looking at a chest and closes it wherever you look; walking away (past Interaction Range + 0.5 m) closes it; the coins are paid when a perk is **picked** (not when the panel opens), so closing costs nothing; each chest rolls its 3 perks once and keeps them. Read only: `chest.OpeningCost`, `InteractionRange`, `InteractKey`, and `PerkChest.Showing` (the chest whose perks are on screen). Your PerkManager now also tells the HUD's perk row (one `[Tony]` line). Your PerkSelectionUI is unchanged; it sits on Tony's built panel (**Tools > UI > Build Perk Panel**, which also gave your Perk Data icons). Guide: `Docs/Perk Panel Setup.md` |
| Animals that run away | Already done: the sheep are deer now (ithappy's **Animals FREE** pack, `Assets/Prefabs/ResourceNodes/DeerNode`). Tony's `WildAnimal` makes them graze, wander, and run from the player when the player comes within 12 m or hits them. Your `PlayerCombat` damages them as `ResourceType.Animal`, like the sheep. More animals: select e.g. `Imported3D/ithappy/Animals_FREE/Prefabs/Horse_001`, then **Tools > World > Build Wild Animal**. Guide: `Docs/Animal Setup.md` |
| A sight perk or buff | `PlayerSight.Current.AddRange(20f);` (a perk: until the player dies) or `PlayerSight.Current.AddRange(40f, 30f);` (a buff: 30 seconds). Deer then spawn farther out by themselves (Tony's `AnimalSpawner` reads the range live). `PlayerSight.Current.Range` = the range now. In a scene with a `ViewDistance` on the camera (the new world: 100 m, then mist), its Distance replaces the Sight Range (the player sees exactly that far); the perk still adds on top |
| Your chest (changed) | Tony's **Tools > World > Set Up Chests** (19/11/2026, log `Docs/TonyEditMate/Marko/19-11-2026 Chest On The Floor And Yellow Outline.md`): the Chest prefab got Tony's **StandOnGround** (it now sits on the floor; it floated because the new model sits lower than the old box) and **InteractOutline** (a yellow outline while you can open it with E). Your PerkChest got `[Tony]` lines: `IsOpened` (read only) and the E ray now uses `InteractOutline.LooksAt(ray, interactionRange, transform)`, which skips the player's own ball (in first person the camera sits on its edge, so the old ray often hit the player and E did nothing). Guide: `Docs/Interact Outline Setup.md` |
| Loot boxes and new perks (changed) | Tony's `[Tony]` lines (21/11/2026, log `Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md`, guide `Docs/Loot Chests Setup.md`): your chest is the **common box** (Tony's **ChestTier**: 2 coins, +1 for every perk bought), next to a **rare box** (10, x1.5) and an **epic box** (100, x2, REROLL) made of DuNguyn's loot boxes with your PerkChest. Your `ChestPerkType` got 11 new kinds at its end, your Perk Data **Requires** / **Max Picks**; your PerkManager hands the new kinds to Tony's **PerkEffects**; your PerkChest shows 1 to 3 perks and only ones the player may get; your PerkSelectionUI hides unused cards and can `Refresh`. Your four perks work as before |
| Critical hits and knockback in your combat | `[Tony]` lines in your `PlayerCombat.Attack()`: `PerkEffects.RollCritical()` (the epic Critical Strike perk: x2 damage, a red burst). Punches push monsters back now (Tony's PunchEffect + `Knockback`; your `EnemyMovement` waits while `Knockback.IsPushed(body)`). Push something yourself: `Knockback.Push(enemy, velocity, 0.25f);` |
| The new chest system (changed) | Tony's `[Tony]` lines (21/11/2026, log `Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md`): the New Game page picks **Old chests** (your chests as they work now) or **New chests** (default: the common and epic boxes roll ONE random perk in Tony's pop-up `PerkRollUI` with BUY / REROLL, every box can reroll, a new orange **legendary box**, 150 coins, choose 1 of 3, desert only). Your PerkChest asks Tony's ChestTier which perks to offer (`PerksNow()`) and opens the pop-up for a random box; `BuyRolled()` = picking it. 4 new kinds at the end of your `ChestPerkType`: AttackSpeed, Vampire, Thorns, NightPower. Your `PlayerCombat`: the wait between attacks is `attackCooldown / PerkEffects.AttackSpeedMultiplier` and damage x `PerkEffects.DamageMultiplier` (Night Power); your `EnemyAttack` calls `PerkEffects.Reflect(this, attackDamage)` (Thorns); your `FireballProjectile` hurts through `PerkEffects.Hurt(...)` (Vampire heals; Night Power doesn't change the fireball). In your own melee damage code: `PerkEffects.Hurt(enemy, () => enemy.TakeDamage(dmg));` and `dmg *= PerkEffects.DamageMultiplier;` |
| Reroll perk, Keen Eye, Long Reach (changed) | Tony's `[Tony]` lines (21/11/2026, log `Docs/TonyEditMate/Marko/21-11-2026 Reroll Perk, Keen Eye And Long Reach.md`): REROLL only works once the player has the new **Reroll** perk (epic box; legendary box in the new system), in both chest systems (Tony's `ChestTier.CanReroll`, so your `PerkChest.RerollAllowed` follows it). New **Keen Eye** (epic / legendary): Tony's Crosshair turns yellow when your attack would hit what it points at. Then **Long Reach** (rare box; new system: common and rare) makes the attack reach 10 % farther. 3 new kinds at the end of your `ChestPerkType`: Reroll, KeenEye, LongReach. Your `PlayerCombat` got `public float AttackReach` (= `attackRange * PerkEffects.ReachMultiplier`), which the attack's ray uses; Tony's Crosshair casts the same ray from your `playerCamera` as far as `AttackReach`. If you change how the attack aims, keep `AttackReach` and `playerCamera` so the yellow crosshair stays right. Your `FireballProjectile.Initialize` is your original line again |
| Arrow Recovery perk (changed) | One more kind at the end of your `ChestPerkType` (22/11/2026, log `Docs/TonyEditMate/Marko/22-11-2026 Arrow Recovery Perk.md`): `ArrowRecovery`, an epic / legendary perk; your PerkManager hands it to Tony's PerkEffects like the others. Arrows hurt your `EnemyCombat` as before. Without the perk an arrow that hits a monster leaves nothing in it; with it the arrows stay in the monster (Tony's ArrowProjectile watches your `currentHealth`), fall out onto the ground when it reaches 0, and come back to the player who walks close |
| Extra coins per kill | Your `EnemyCombat.Die()` calls Tony's `MonsterBounty.Killed(gameObject)` after your coins: the armor set bonus (copper +1, iron +2) and the Bounty Hunter perk. Since 23/11/2026 it passes who died, so a human enemy counts as "Humans killed" (`MonsterBounty.HumanKilled`) and anything else as a monster (`MonsterBounty.MonsterKilled`) |
| Human enemies use your EnemyCombat (changed) | Tony's human enemies in the new world (`Docs/Human Enemies Plan.md`, guide `Docs/Human Enemies Setup.md`) have your `EnemyCombat` for their health, so all player attacks hurt them, and your Miss Chance. Their **Coin Multiplier** is 0 (Tony's `HumanLoot` pays them by their gear instead). One `[Tony] edited` line in your `Die()` (23/11/2026, log `Docs/TonyEditMate/Marko/23-11-2026 Humans Killed Count Apart From Monsters.md`): `MonsterBounty.Killed(gameObject)`. And two `[Tony] added` lines in your `TakeDamage()`, after your dodge roll (23/11/2026, log `Docs/TonyEditMate/Marko/23-11-2026 Human Armor Takes Damage Off.md`): a human's armor takes its share off the hit (`HumanGear.Reduce`, a full diamond set 40 %); monsters have no HumanGear, so nothing changes for them. Is something a human? `GetComponent<HumanBrain>() != null` |
| A new perk | Same kind, other numbers: right click > **Create > Game > Perk Data**, then drag it into a chest's **Available Perks**. A new kind: see "A new perk" in `Docs/Loot Chests Setup.md` |

**Example: drop meat when an animal dies**

```csharp
public ItemData meatData;   // drag the Meat Item Data in the Inspector

void DropMeat(int amount)
{
    GameObject meat = Instantiate(meatData.Prefab, transform.position + Vector3.up, Quaternion.identity);
    meat.GetComponent<ItemPickup>().Setup(meatData, amount);   // the player picks it up with F
}
```

### Sanyam: world, building, spawning

| You want to… | Use |
|---|---|
| Place resources in the world | Instantiate an **Item Data's Prefab** (`itemData.Prefab`). It already has `ItemPickup`, so F picks it up |
| Place trees | Drag `Assets/Prefabs/ResourceNodes/TreeNode` or a look from `ResourceNodes/Trees` (Oak, Broadleaf, Tall Broadleaf, Birch, Birch Cluster) onto the ground. The pivot is the foot of the trunk. Then **Tools > World > Mix Trees In Scene** gives them random looks, turns and sizes and stands them on the ground. Your `ObjectScatter` now spawns the 200 trees as one rule per look (`Trees: Round Oak`, ...) with **Y Offset 0**: the new trees' pivot is the foot of the trunk, so Y Offset 2 made them float (only its settings changed, not your script; log `Docs/TonyEditMate/Sanyam/12-11-2026 Tree Spawn Height And Looks.md`). Guide: `Docs/Tree Setup.md` |
| Animals in the world | Not in your ObjectScatter any more: Tony's tool (**Tools > World > Set Up World Spawning**) moved the `Animals` rule to Tony's `AnimalSpawner - DeerNode` object. It keeps 15 deer around the player and only spawns them where the player can't see (Tony's `PlayerSight` on the Player). Before, the rule ran after 1000+ trees and found no spot (0 of 40). Guide: `Docs/World Spawning Setup.md` |
| Your ObjectScatter (changed) | Tony's `[Tony]` lines (13/11/2026, log `Docs/TonyEditMate/Sanyam/13-11-2026 Scatter Spacing Groups And Forest Patches.md`): **Min Spacing** now counts only between rules with the same **Spacing Group** (empty = the prefab's name; all tree looks = `Trees`). Other kinds keep **Clearance** (3 m), so many trees still leave room for rocks and bushes. New per-rule settings: **Patch Size** / **Open Ground** (forests with meadows) and **Min / Max Size** (random sizes). Used spots are kept in 10 m squares (fast with 1000+ trees). Old rules still work |
| Spawn something where the player can't see it | `PlayerSight.Current.CanSee(feet, height)` (false = out of sight: behind the player, behind a hill / trunk / wall, or past the sight range). Tony's `AnimalSpawner` does this for the deer, and since 21/11/2026 Tony's `NightMonsters` for your night enemies |
| Make something sit on the floor | Add Tony's **StandOnGround** to the prefab: when the game starts it drops onto the floor under it (terrain, built floor, rock), tilts up to **Max Tilt** on slopes, whatever Y Offset your ObjectScatter used. The chests have it now (their rules still say Y Offset 0.6; StandOnGround settles them after they spawn). Guide: `Docs/Interact Outline Setup.md` |
| Your desert rocks are sand-colored | Tony's rock tool (20/11/2026, log `Docs/TonyEditMate/Sanyam/20-11-2026 Desert Rocks Use Sand Rocks.md`) set your **Desert Rocks** rule's Prefab to `ResourceNodes/Rocks/StoneNode Desert` (StoneNode in the pack's sand color). Forest Rocks still spawns StoneNode. Rocks now have their pivot on the ground, so Y Offset 0 is right |
| Your berry bushes spawn again | Tony's spawning tool (13/11) had put your **Berry Bushes** rule in the trees' Spacing Group, because Turki's BerryNode also says Resource Type Tree: the bushes then kept their 20 m from every tree, and 1 of 150 spawned. **Tools > World > Set Up World Spawning** now knows a bush by Collect With Key and puts its rule back in its own group (only its settings, not your script; log `Docs/TonyEditMate/Sanyam/19-11-2026 Berry Bushes Spawn Again.md`) |
| Dark shadows in a scene another scene opens | A scene opened by another scene (START GAME) or in a built game only has the light from the sky that was saved for it, else its shadows are pitch black. Open it and run **Tools > World > Set Up Sky Lighting** (GameScene needs it), again after changing its sky (Lighting > Environment), and commit the files it makes in `Assets/Scenes/`. Guide: `Docs/Sky Lighting Setup.md` |
| Make a new building piece | Select it, **Tools > Building > Make Building From Selection**, fill the recipe. It appears in the build menu (B) by itself |
| Know when the player built something | `FindFirstObjectByType<BlueprintPlacer>().Built += OnBuilt;` (gives the `BuildingData`) |
| Is the player placing a building now? | `BlueprintPlacer.IsPlacing` |
| Where the player respawns | `PlayerRespawn` on the Player, **Respawn Point** in the Inspector (default 0, 0.5, 0). Read it: `respawn.RespawnPoint`, move it: `respawn.SetRespawnPoint(point)` (the new world sets it to its start clearing) |
| Day / night | `DaylightCycle` on the Directional Light: `TimeOfDay`, `IsNight`; GameRules > **Do Daylight Cycle** stops time |
| World rules | `GameRules` object in the scene (see section 3) |
| Scenes / build order | The game starts at **MainTitleScene** (scene 0 in File > Build Settings), START GAME loads **GameScene** (NEW GAME > Map: Old world) or **ProceduralGenerateScene** (Map: New world). Rebuild the title with **Tools > UI > Build Main Title Scene** (`Docs/Main Title Setup.md`) |
| The new world (random every game) | Tony's **WorldGenerator** (20/11/2026; **endless** since 21/11/2026) makes a new world in `ProceduralGenerateScene` every game, in 256 x 256 m squares (chunks, each its own Unity Terrain) made while the player walks, in every direction, with no barrier: a grassland start with rock caves (stone, copper, iron), rocky hills, the desert from 400 m (sandstone caves with gold, `Docs/Desert Setup.md`) and the snow from 700 m (snow caves with diamonds, snowy pines, the most chests, `Docs/Snow Setup.md`), and a 100 m view distance (`ViewDistance` on the Main Camera: mist, nothing further drawn, for the web build's frame rate). Far chunks are hidden, never thrown away (what you chopped or opened stays so). Your **ObjectScatter** is switched off there and its **rules** fill every chunk (a rule's Count = how many for every 400,000 m² of its texture; your script isn't changed; rules copied from GameScene, minus Desert Rocks, plus `Hill Rocks`, `Sandstone Rocks` and the snow's rules in that scene only). **Tools > World > Build New World Scene** makes the scene from GameScene again, so change things in GameScene. Ask `WorldGenerator.Current` (null in the old world) for `BiomeAt(position)` (Grassland / Desert / Hills / Snow), `TerrainAt(position)` (the chunk's Terrain there, null if not made yet; or use `Terrain.activeTerrains`, which has the shown ones), `SpawnPoint`, `Seed`, `Terrain` (the start's chunk). There are many terrains there (one per chunk), so `Terrain.activeTerrain` (only one of them) isn't enough. Since 21/11/2026 dying there and clicking Respawn makes a brand-new world (**New World On Death**; `WorldGenerator.NewWorldOnRespawn`, `StartNewWorld()`): the scene loads again, so everything in it starts fresh, your scripts included. Guide: `Docs/New World Setup.md` |
| The edge of the world (barrier) | Tony's `WorldBarrier` (20/11/2026): a see-through, glowing force field at the edge that players see and can't pass (guide `Docs/World Barrier Setup.md`). The new world (endless since 21/11/2026) has none; your GameScene gets one from **Tools > World > Add World Barrier** (on the inner faces of your `Boundaries` walls, which are left as they are; three of them are triggers and stopped nothing). Your **ObjectScatter** got one `[Tony] added` line: it skips spots outside the barrier, so no chest lands where nobody can reach it (log `Docs/TonyEditMate/Sanyam/20-11-2026 Nothing Spawns Outside The Barrier.md`). In your own spawning: `if (!WorldBarrier.Inside(spot, 2f)) continue;` |
| Your chest spawn rules (changed) | Tony's **Tools > World > Build Loot Chests** (21/11/2026, logs `Docs/TonyEditMate/Sanyam/21-11-2026 Loot Box Spawn Rules.md` and `.../21-11-2026 Twice The Chests And A Legendary Rule.md`, your script untouched): **ChestsDesert** used the grass layer (every chest spawned on grass): it's on the sand layer now; twice as many boxes: ChestsForest 20, **Rare Chests Grass** 6, ChestsDesert 30, **Rare Chests Desert** 12, **Epic Chests Desert** 4, **Legendary Chests Desert** 2 (the legendary boxes remove themselves in the old chest system); all chest rules in Spacing Group "Chests" |
| Change scene with a fade | `ScreenFader.LoadScene("GameScene", "DAY 1", "Survive 5 nights");` (fades to black, title card, loads, fades in) |

Guides: `Docs/Build Menu Setup.md`, `Docs/Item Setup.md`, `Docs/Inventory Setup.md`.

---

## 3. Script reference

### PlayerHealth (on the Player)

Get it: `health = FindFirstObjectByType<PlayerHealth>();` in `Start()`

| Member | What it is |
|---|---|
| `float Health`, `float MaxHealth` | current / max (default 100; Night Power's extra included at night) |
| `float StartMaxHealth` | max health before any perk (`MaxHealth - StartMaxHealth` = what perks added) |
| `void SetMaxBoost(float times)` | max health x this (Night Power: 1.1 at night); the bar keeps its share. 1 = normal |
| `bool IsDead` | health is 0 |
| `void TakeDamage(float amount)` | a hit: the armor the player wears takes its share off first (PlayerArmor, since 20/11/2026: the full copper set takes 10 % off, the full diamond set 40 %), then health goes down; at 0 the player dies (`Died` fires once) |
| `void LoseHealth(float amount)` | lose health that no armor stops (starving, bad food) |
| `void Heal(float amount)` | gain health, never above max |
| `void Revive()` | full health again (PlayerRespawn uses it) |
| `event Action Dying` | fired once when health reaches 0, just BEFORE Died (21/11/2026): everything is still as it was in that life (the day, perks, max health, armor, coins); read them here, because Died is when they're reset or dropped (the death page does) |
| `event Action Died` | fired once when health reaches 0 |

### PlayerHunger (on the Player)

Get it: `hunger = FindFirstObjectByType<PlayerHunger>();`

| Member | What it is |
|---|---|
| `float Hunger`, `float MaxHunger`, `bool IsStarving` | current / max (100) / is it 0 |
| `void Eat(float amount)` | add food |
| `void LoseFood(float amount)` | take food away |

Food drains by itself: more while walking, sprinting and levitating. At 0 the player starves (dies or loses health, set in the Inspector).

### PlayerStamina (on the Player)

Get it: `stamina = FindFirstObjectByType<PlayerStamina>();`

| Member | What it is |
|---|---|
| `float Stamina`, `float MaxStamina` | current / max (100; Night Power's extra included at night) |
| `float StartMaxStamina` | max stamina before any perk |
| `void SetMaxBoost(float times)` | max stamina x this (Night Power: 1.1 at night); the bar keeps its share. 1 = normal |
| `bool IsTired` | hit 0 and hasn't refilled enough yet: no sprint, no jump |
| `void Use(float amount)` | spend stamina (attack, dodge…); hitting 0 makes the player tired |
| `void Restore(float amount)` | give stamina back at once |
| `bool CanSprint`, `bool CanJump` | used by movement |
| `void SetSprintCost(float times)` | sprinting uses this many times the normal stamina (0.75 = a quarter less; the Sprinter perk). 1 = normal |
| `void SetRegenBoost(Object from, float times)` | make it refill faster while you want (e.g. a perk or a potion): `stamina.SetRegenBoost(this, 1.5f)` = 50 % faster, `SetRegenBoost(this, 1f)` = back to normal. One boost per script (calling again replaces it); several multiply. The iron armor set's bonus uses it. |
| `float RegenBoost`, `float RegenPerSecond` | how much faster it refills now (1 = normal), and the refill per second with that |

It refills by itself after a short rest.

### PlayerController, PlayerJump (on the Player)

Get them: `controller = FindFirstObjectByType<PlayerController>();`, `jump = FindFirstObjectByType<PlayerJump>();`

| Member | What it is |
|---|---|
| `bool IsMoving`, `bool IsSprinting` | PlayerController: moving / sprinting (Left Shift) right now. Sprinting only counts on the ground |
| `bool SprintedLastStep` | PlayerController (22/11/2026): the last physics step moved the player at sprint speed. Use this one inside a `FixedUpdate`: there `IsSprinting` can say false while the player runs (its ground info is cleared at the end of PlayerController's FixedUpdate) |
| `bool IsGrounded` | PlayerController: standing on walkable ground (not in the air, not on a too-steep slope), or held up between too-steep slopes (below). PlayerJump has one too (standing on something not too steep, or held up like that) |
| `bool IsWedged` | PlayerController: held up between too-steep slopes (slid into a narrow V between two hills, a pit). Counts as standing, so the player can walk along the V and jump instead of being stuck in the falling pose |
| `float MaxSlopeAngle` | PlayerController: the steepest slope the player can walk up (default 45 degrees); steeper = a wall |
| `event Action Jumped` | PlayerJump: fired on every jump |
| `float Speed`, `float StartSpeed`, `float SprintMultiplier` | PlayerController: walking speed now / before any perk (m/s); sprinting = Speed x SprintMultiplier (perks included; `BaseSprintMultiplier` = without) |
| `void SetSprintBoost(float times)` | PlayerController: sprinting goes this many times as fast (the Sprinter perk); 1 = normal |
| `void SetMoveBoost(float times)` | PlayerController: walking (and so sprinting) goes this many times as fast (Night Power at night); `Speed` includes it. 1 = normal |
| `float JumpHeight`, `float BaseJumpHeight` | PlayerJump: how high a jump goes, in meters (perks included / without) |
| `void SetJumpBoost(float times)`, `void ForgetPress()` | PlayerJump: jumps go this many times as high (the Spring Legs perk); forget a jump press that hasn't jumped yet (Glide's dive) |
| `PlayerGlide` | on the Player, switched on by the epic Glide perk: hold Space while falling = float down, Space twice in the air = dive. `IsGliding`, `IsDiving` |

### PlayerAnimator (on the Player's child "Character"): the animated body

The player's look: Blink's low-poly human standing on the ball (the ball still does the moving and colliding).
It plays idle / run / sprint / jump / fall / death by itself. Get it: nothing to find, `Play` is static.

| Member | What it is |
|---|---|
| `static void Play(PlayerAction action)` | plays an action on the upper body, e.g. `PlayerAnimator.Play(PlayerAction.Attack);`. Does nothing if there's no character |
| `PlayerAction` | `Hit` (already plays by itself when hurt), `Punch` (the right jab), `Attack`, `Mine`, `Gather`, `Cast`, and PunchCombo's `PunchLeft`, `HeavyHook`, `HeavyPunch` |
| `static PlayerAnimator Current` | the character in the scene (null if none) |
| `const float ActionSpeed` | how fast actions play (2 = twice as fast as the clips), in the body AND the first-person hands, so they stay together |
| `static event Action<PlayerAction> ActionPlayed` | fired by every `Play(...)`. The first-person hands (`FirstPersonHands`, on the Main Camera) listen, so `Play` also animates the hands in first person |

Built by **Tools > Player > Add Character Model**. Guide: `Docs/Player Character Setup.md`.

### EnemyAnimator (on an enemy, e.g. SlimeEnemy): the enemy's animations

Plays the monster's animations next to Marko's enemy scripts: idle / walk / run by its real speed, a flinch when its
`currentHealth` goes down, and a cheer when the player dies. Get it: `GetComponent<EnemyAnimator>()` on the enemy.

| Member | What it is |
|---|---|
| `void PlayAttack()` | the attack animation (EnemyAttack calls it) |
| `float PlayDeath()` | the death animation; stops the enemy (no moving, attacking or being hit) and returns the seconds to wait before removing it (EnemyCombat.Die calls it) |
| `static void Vanish(GameObject enemy)` | removes an enemy that wasn't killed: it stops (at a random moment within **Vanish Spread**) and burns away into orange embers (its DeathEffect), then it's destroyed. No coins. Marko's EnemySpawner calls it for the night's enemies at dawn. Without an EnemyAnimator the enemy is destroyed at once |
| `bool IsDead` | it's dying or burning away (stopped for good) |
| `bool IsVanishing` | it's burning away (or about to), because day came |
| `float LastAttackTime` | (22/11/2026) `Time.time` of its last attack (FlyingEnemy climbs away after each bite) |

Built by **Tools > Enemies > Build Monster Enemy**. Guide: `Docs/Enemy Setup.md`.

### FlyingEnemy (on a flying enemy, e.g. BeholderEnemy): flying

Since 22/11/2026 (guide `Docs/Night Monsters Setup.md`). It turns the Rigidbody's gravity off and sets only the
enemy's height; your EnemyMovement still steers it over the ground (it runs after it in each physics step) and your
EnemyAttack still bites. To make another enemy fly: add FlyingEnemy (it needs a Rigidbody). Settings: **Cruise
Height** (3.5 m above the ground, trees and rocks don't count), **Bite Height** (0.4 m above the target's middle),
**Climb Speed**, **Dive Distance** (7 m), **Climb Seconds** / **Back Off Speed** (up and away after each bite), **Bob
Height** / **Bob Speed**. No public methods.

### PunchEffect (on the Player): the grey wind burst where a punch lands

Grey dust clouds, wind lines, a shock ring and a few specks, a little in front of the hit spot. It waits until the fist
lands in the punch animation (Impact Delay), then removes itself after about a second. Get it: nothing to find, `Punch`
and `Play` are static.

| Member | What it is |
|---|---|
| `static void Punch(Component target, RaycastHit hit, Action hurt)` | a punch that lands after the Impact Delay: then `hurt` runs (e.g. `() => enemy.TakeDamage(10)`) and the burst pops on the spot of `target` that was hit, even if it moved. Not run if the target is gone by then. Without a PunchEffect, `hurt` runs at once. PlayerCombat uses it for bare-handed hits |
| `static void Hit(Component target, RaycastHit hit, Action hurt, PlayerAction swing, bool critical = false)` | the same for any swing, with its own delay: Punch = Impact Delay, Attack = Swing Delay, Mine = Mine Delay; every hit bursts wind (tools: Tool Burst Size). PlayerCombat uses it for every hit |
| `static void Play(Vector3 point, Vector3 normal)` | a burst at `point`, flying out of the surface (`normal` = `RaycastHit.normal`), e.g. `PunchEffect.Play(hit.point, hit.normal);`. Does nothing if the Player has no PunchEffect or it's switched off |
| `static void PlayNow(Vector3 point, Vector3 normal, bool withTool = false)` | the same burst at once, without the punch's Impact Delay: for a swing timed by its own animation (Tony's human enemies use it when their axe or pickaxe lands). `withTool` = a tool's size (Tool Burst Size), else a fist's |
| `static PunchEffect Current` | the one on the Player (null if none) |
| `static float LungeReach` | how much farther than its reach PlayerCombat's ray looks (Lunge Reach, 3 m; 0 without a working PunchEffect). A target found past the reach lands only if the player is within reach when the blow does |
| Knockback, Heavy Knockback, Knockback Seconds, Critical Color | a punch that lands on a monster pushes it away (the Brawler perk farther); a critical hit (`critical = true`) bursts in Critical Color (red) |

Built by **Tools > Effects > Build Punch Wind Burst** (prefab `Assets/Prefabs/Effects/PunchWindBurst.prefab`). Guide:
`Docs/Punch Effect Setup.md`.

### DamageNumbers and DisplaySettings (nothing to find: static): numbers where a hit lands

Since 21/11/2026 a hit of the player pops the damage it did: white ("12"), or for a critical hit bigger, orange-red,
"CRITICAL" over it and "!" after it. PunchEffect (punches, tools, swords), the fireball and Thorns already do it. The
player turns it on / off on Settings > DISPLAY (title screen or Esc menu), with a child setting "Only monsters and
animals". Guide: `Docs/Display Settings Setup.md`.

| Member | What it is |
|---|---|
| `DamageNumbers.Show(Vector3 point, float amount, bool critical, bool creature)` | pops "amount" at "point" (if the player's settings allow it). `creature` = a monster or an animal (Only monsters and animals hides the rest), `critical` = the big orange look. E.g. `DamageNumbers.Show(hit.point, 12f, false, true);` |
| `DamageNumbers.HealthOf(target, out bool creature)` then `DamageNumbers.ShowDealt(target, before, creature, point, critical)` | measure the health before your hurt, hurt, then show the real difference (a dodge or a shell shows nothing). Works on EnemyCombat and ResourceNode |
| `DamageNumbers.TopOf(target)` | a spot near the top of a target, for hits without a point |
| `DisplaySettings.ShowDamage`, `DamageOnlyCreatures`, `ShowsDamageOn(creature)` | the player's choices (saved in PlayerPrefs), event `DisplaySettings.Changed` |
| `DisplaySettings.Fullscreen`, `SetFullscreen(bool)`, `ToggleFullscreen()` | fullscreen (also Alt + Enter or F11: `KeyBindings.Fullscreen`) |

### InteractOutline (on anything you can use, e.g. the Chest): a glowing outline

A colored outline around the thing (yellow on chests), drawn on top of the picture. By default it shows while the
player looks at it from within Range, the same test as the chest's E key. It hides while a menu is open, while dead,
and once a chest is opened. Settings: Color, Width, Fill, Pulse, Show When (Look At / Near / Always), Range.

| Member | What it is |
|---|---|
| `static bool LooksAt(Ray ray, float range, Transform target)` | true if the nearest thing along the ray (within `range`) is `target` or part of it, skipping the player's own ball. Use it for your own "press E while looking at it" code: `InteractOutline.LooksAt(new Ray(cam.transform.position, cam.transform.forward), 3f, transform)` |
| `bool IsShowing` | its outline shows right now (or is fading out) |
| switching the component off | hides the outline for good, e.g. once your thing is used up: `GetComponent<InteractOutline>().enabled = false;` |

### PlayerInfoPage (the inventory's Info tab)

The player's status, 4 times a second while the tab is open: health / hunger / stamina (now / max with a bar), the
armor (how much it takes off a hit, its set), walk and sprint speed, jump height, the fireball (locked, or its cooldown)
and coins. What perks added shows in green (e.g. "+44 from perks"), an armor set bonus in gold on a gold-tinted row
(e.g. "+50% recharge (Iron Set)"), and the perks the player has are listed with their icons. Nothing to call: it reads PlayerHealth,
PlayerHunger, PlayerStamina, PlayerController, PlayerJump, your PlayerFireball (`HasFireballPerk`, `PerkStacks`,
`CurrentCooldown`) and PlayerCoins, and PlayerPerks. Made by **Tools > UI > Add Inventory Scripts** (guide
`Docs/Inventory Setup.md`). **A new perk that changes a stat:** its row shows the change by itself if the stat's
script has a "start" value (like `StartMaxHealth`).

### ChestPrompt (in the HUD, under the crosshair)

While the crosshair is on a perk chest within its reach, it shows `E  Open common box  (coin) 2`: the chest's key,
what it does (a loot box says its name: ChestTier's Box Name) and its price (red while the player can't pay). Nothing to
call: it reads `PerkChest.OpeningCost`, `Tier`, `InteractionRange` and `InteractKey` by itself. Made by **Tools > UI > Build HUD**.

### PerkPanelInfo (on the perk panel, next to Marko's PerkSelectionUI)

Writes the open chest's price (`PerkChest.Showing.OpeningCost`) and the player's coins on the panel, the box's name
as the title in its color (RARE BOX), fades out the CHOOSE buttons ("NEED 2 COINS") while the player can't pay, shows
and prices the REROLL button at a box that can reroll (the epic box, once the player has the Reroll perk:
`PerkChest.TryReroll`), and pops the window in.
Nothing to call. Made by
**Tools > UI > Build Perk Panel** (guide `Docs/Perk Panel Setup.md`).

### HeldItemView (on the Player): the selected item in the hand

The selected hotbar item (not a tool: ToolInHand holds those) sits in the character's right fist, on the body and on
the first-person hands. Nothing to call: it follows `PlayerInventory.Changed`. Settings: Size In Hand, Item Nudge,
Item Turn. Guide: `Docs/Held Item Setup.md`.

### StandOnGround (on anything that should rest on the floor, e.g. the Chest)

When the game starts it drops the object onto the floor below it (not itself, not the player or anything physics
moves), sunk 2 cm, tilted up to Max Tilt on a slope. `Settle()` does it again now (true = a floor was found), e.g.
after you move it in code. Foot Offset = how far the model's bottom is below the pivot (-1 = measured at the start).
Guide for both: `Docs/Interact Outline Setup.md`. The rocks have it too (Foot Offset 0: their pivot is on the ground).

### WildAnimal (on an animal, e.g. DeerNode): grazes, wanders, runs from the player

It needs nothing from your code. It finds the player by itself, keeps itself on the ground and plays its own
animations. Its settings (Flee Distance, Run Speed, Wander Radius...) are in the Inspector. Hurting it the normal way
(`ResourceNode.TakeDamage`) also makes it run.

| Member | What it is |
|---|---|
| `const string SpeedName`, `AnimSpeedName` | its Animator's floats: `Speed` (0 = standing, 1 = walking, 2 = running) and `AnimSpeed` |

Built by **Tools > World > Build Wild Animal** (any ithappy animal; the deer by default). Guide: `Docs/Animal Setup.md`.
It never walks out through a `WorldBarrier`.

### Human enemies (HumanBrain, HumanGear, HumanLoot on each human; the Human Spawner in the new world)

Survivors in the new world, made by **Tools > Enemies > Build Human Enemies** from Polytope Studio's bodies, as tall as
the player's character (step 1 of `Docs/Human Enemies Plan.md`: type 1, which never attacks and runs away when hit). They
use Marko's `EnemyCombat` for health and Tony's `EnemyAnimator` for animations, so hurting one works like hurting a
monster. They really break trees, rocks and berry bushes (Turki's `ResourceNode.TakeDamageFor`), some with their own
axe and pickaxe.

| Member | What it is |
|---|---|
| `HumanBrain.Type` | which of the five types it is (`HumanBrain.Kind.Type1` ... `Type5`) |
| `HumanBrain.IsFleeing`, `IsWorking` | running away from the player; chopping, mining or picking (for real: what it breaks is gone) |
| `HumanBrain.IsFighting`, `HoldsGrudge` | after the player (it fights back when hit); the player hit it before (it attacks when the player comes within 8 m). Its blows hurt the player with `PlayerHealth.TakeDamage` and trigger Thorns (`PerkEffects.Reflect`), like Marko's EnemyAttack |
| `HumanGear.WornTier(piece)`, `WeaponTier`, `ShieldTier` | what it wears and holds: `HumanGear.Tier` None / Copper / Iron / Gold / Diamond (the number is its coins per piece) |
| `HumanGear.AxeTier`, `PickaxeTier` | what it chops / mines with, as a `ToolTier` for the player's tool rules (its axe or a war axe; only a pickaxe mines). None = bare hands |
| `HumanGear.HoldTool(ToolKind.Axe or Pickaxe)`, `HoldWeapon()`, `HoldsSomething` | takes out its axe or pickaxe (the weapon goes away), or puts it away again; something is in its hand right now |
| `HumanGear.ProtectionPercent`, `Reduce(damage)` | how much its armor takes off a hit (with the player's armor numbers), and a hit after it (Marko's EnemyCombat uses it) |
| `HumanGear.WeaponDamage` | one hit with its weapon: the weapon's Damage at copper, +25 % a tier (0 = bare hands) |
| `HumanLoot.Carry(item, amount)`, `CarriedCount` | what it gathered: it all goes into the player's inventory when the player kills it |
| `MonsterBounty.HumanKilled` | static event: the player killed a human (`MonsterKilled` is only for monsters now) |

Guide: `Docs/Human Enemies Setup.md`.

### CaveTrail (on every cave prefab): the way through a cave's entrance

A cave (Tools > World > Build Rock Caves: rock, desert and snow caves) is a ring of rocks with one entrance at its
front (+Z). CaveTrail gives anything that walks by itself waypoints through it (Tony's human enemies use it).

| Member | What it is |
|---|---|
| `static CaveTrail Around(Vector3 point)` | the cave whose floor holds that point (null = out in the open) |
| `Vector3 Door`, `Vector3 Front` | the spot just inside the entrance, and a few meters in front of it |
| `void AddWayOut(List<Vector3> trail, Vector3 toward)` / `AddWayIn(trail, from)` | adds the waypoints out of the cave (door, front, a side spot if `toward` is behind the cave) or into it |
| `bool Contains(Vector3 point)`, `float Radius` | on its floor; the floor's radius |

### WorldBarrier (on the "World Barrier" object): the edge of the world

A see-through, glowing force field around the map: faint from afar, bright and rippling where the player comes near,
solid for bodies (its walls are on the Ignore Raycast layer, so rays pass through). The new world has none (it's
endless since 21/11/2026); **Tools > World > Add World Barrier** adds one to the open scene (GameScene). Guide:
`Docs/World Barrier Setup.md`.

| Member | What it is |
|---|---|
| `static bool Inside(Vector3 spot, float margin = 0f)` | true if the spot is inside the barrier, at least `margin` meters from it (always true when the scene has no barrier). Ask it before spawning something |
| `bool Contains(Vector3 spot, float margin = 0f)` | the same for this one barrier |
| `void SetArea(Vector2 size, float height)` | moves its sides (x width, z length around the object) and height; remade at once while playing |
| `Vector2 Size`, `float Height` | where it stands now |

### ViewDistance (on the Main Camera): how far the player can see

The new world's "line of sight" (100 m): nothing further is drawn (the camera's Far Clip Plane), a mist hides the edge
(fog plus a band of mist low on the sky, colored by the time of day), terrain grass is drawn only that far, and
PlayerSight's range becomes that distance. Fewer things to draw = a higher frame rate, most of all in the web build.
Change it: **Main Camera > View Distance > Distance**. Guide: `Docs/View Distance Setup.md`.

| Member | What it is |
|---|---|
| `static float Limit` | how far the player can see now, in meters (no limit when the scene has none) |
| `static ViewDistance Current` | the one in the scene (null if none or switched off) |
| `float Distance` / `void SetDistance(float meters)` | read / change it (10 - 500 m), e.g. from a settings slider |

### ToolRules (nothing to find: static): how tools work

| Member | What it is |
|---|---|
| `ToolStats ToolIn(PlayerInventory.Slot slot)` | the tool in a slot (`kind`, `tier`), or null (empty, or not a tool) |
| `bool IsSword(slot)` | a sword of any tier |
| `PlayerAction SwingFor(tool)` | the animation a click plays: Punch (null), Mine (pickaxe), Attack (sword, axe) |
| `float Boost(tool, ToolKind job)` | 1.25 per tier above Wood, only at its own job (e.g. `Boost(tool, ToolKind.Sword)` for damage to enemies). Not for bows since 22/11/2026: a bow's own Damage is added to its arrows instead |
| `bool CanMine(tool, ResourceNode node)` | can this tool (null = a hand) break it: an ore needs its Mined With pickaxe or better; since 22/11/2026 a tree whose wood has a Mined With needs that axe or better (the snow's spruce: Iron) |
| `ToolTier NeededFor(ResourceNode node)` | the tier it needs (None = anything): its drop's Mined With, for rock (a pickaxe) and trees (an axe); animals None |
| `void TellCantMine(node)` | shows "Needs a Stone Pickaxe or better" / "Needs an Iron Axe or better" (not more than every 1.5 s) |

| `bool IsBow(slot)`, `bool BowInHand` | a bow of any tier (in that slot / in the selected hotbar slot): it shoots, it doesn't hit |

Tools are made by **Tools > Items > Build Tier Tools**. The selected tool shows in the hand by itself (`ToolInHand`), and
GameRules > **Tool Test** gives one of each. Guide: `Docs/Tool Setup.md`.

### BowShooter (on the Player) and ArrowProjectile (the flying arrow): the bow

Since 22/11/2026 (guide `Docs/Bow Setup.md`). With a bow in the hand: hold the left mouse button to draw, let go to
shoot; the arrow flies, falls a little and hurts a monster or an animal where it lands. Get it: nothing to find, the
members are static.

| Member | What it is |
|---|---|
| `BowShooter.HoldingBow`, `Drawing`, `Power` | a bow is in the hand; it's being drawn; how far (0 - 1, 1 = full damage) |
| `BowShooter.DrawSpeed` | (22/11/2026) how fast the draw animation plays for the bow in the hand: 1 = the Wooden Bow's 1 s, 1.67 = the Spruce Bow's 0.6 s (PlayerAnimator / FirstPersonHands put it in their controllers' BowDrawSpeed) |
| `BowShooter.NextArrow`, `ArrowCount` | the arrow the next shot uses (the strongest carried, an Item Data) and how many the player has |
| `BowShooter.InAimPose` | the arms are in the bow's pose (drawing, or just shot) |
| BowShooter > **Stamina Per Second** | (22/11/2026) stamina used per second while the bow is held drawn (4; sprinting uses 20); the shot itself is free. An empty bar lets the string go without shooting, and a tired player can't draw again until stamina is back to 20 (like sprinting) |
| `ArrowProjectile.HitLayers` | the layers an arrow can hit (your own sweeps can use it) |
| `arrow.Launch(velocity, damage, critical, arrowItem)` | starts a copy of the Arrow Projectile prefab flying (BowShooter does this; `arrowItem` = the arrow's Item Data: its head's look, and what comes back with Arrow Recovery) |

An arrow's damage = (the bow's Item Data > Bow > **Damage** + the arrow's Item Data > Ammo > **Damage**) x how far the
bow was drawn (since 22/11/2026; Wooden Bow 5, Spruce Bow 15; Stone 20 ... Diamond 50). The bow's **Draw Seconds** is
the time to full draw (Wooden 1, Spruce 0.6) and its **Arrow Speed** how fast the arrow flies (45 / 60 m/s); there is no
tier bonus for bows any more. The body plays `PlayerAction.BowDraw / BowRelease / BowCancel`.
An arrow that hits a monster or an animal is used up; with the **Arrow Recovery** perk (`PerkEffects.ArrowRecovery`) it
stays in it, falls out when it dies (its `currentHealth` reaches 0, or its EnemyAnimator is dying or burning away) and
comes back when the player walks close.

### PlayerArmor (on the Player): the armor the player wears

Get it: `PlayerArmor.Current` (it adds itself to the Player if the Player has none; null only without a player). Guide: `Docs/Armor Setup.md`.

| Member | What it is |
|---|---|
| `ItemData WornIn(ArmorSlot slot)` | what's worn in a slot (null = nothing) |
| `float ProtectionPercent`, `float Protection` | every worn piece's Protection added up, in percent (the full copper set: 10, iron: 15, gold: 25, diamond: 40); how much of a hit it takes off (0.1 = 10 %, at most 0.8) |
| `string MostWornSet(out int count)`, `string FullSet`, `int SetSize` | the set most worn pieces count toward and how many ("Copper", 4: Iron pieces count as Copper); the set every slot counts toward or null (all six Iron = "Iron"); 6 |
| `SetBonus ActiveBonus`, `SetBonus BonusFor(string set)` | the set bonus given now (null = none) / a set's bonus: `.set`, `.coinsPerKill` (copper 1, iron 2, gold 4, diamond 8; was a stamina bonus until 21/11/2026), `.chestDiscount` (gold and diamond 0.1 = every chest 10 % cheaper), `.countsAs` (Iron: "Copper", Gold: "Iron", Diamond: "Gold"), `.color` (its box in the crafting list), `.Describe()` ("+4 coins for every monster killed, 10% off every chest"), `.Short()` ("+4 coins per kill, chests -10%") |
| `int CoinsPerKill` | extra coins for every monster killed right now (the active set bonus; Tony's MonsterBounty gives them) |
| `float ChestDiscount` | how much cheaper every loot box is right now (the whole gold or diamond set: 0.1; Tony's ChestTier takes it off every price) |
| `int PiecesFor(string set)`, `int WornOf(string set)`, `bool CountsToward(string own, string target)` | worn pieces counting toward a set (with the sets that count as it) / of exactly that set / does a piece of set "own" count toward "target" |
| `string BetterSetInProgress(out int count)` | a better set than the full one, partly worn ("Iron", 3), or null |
| `bool IsWorn(string id)` | is a piece with this Item Id worn |
| `ItemData BetterOwned(ItemData piece, out bool isWorn)` | (22/11/2026) a piece the player has for the same slot that protects more than `piece` (worn, or the best in the inventory), null = none; `isWorn` = it's the worn one. The crafting list marks a weaker piece's row with a blue arrow and "(wearing Iron Helmet)" |
| `bool LendForUpgrade(ItemData upgrade)`, `void FinishUpgrade(ItemData made, bool crafted)` | crafting an upgrade of a worn piece: take it off for the recipe (Turki's CraftingMenu calls it), then wear the new piece (or the old one again) |
| `static List<SetBonus> DefaultBonuses()`, `CopperColor`, `IronColor`, `GoldColor` | the sets the armor tool makes, weakest first: Copper, Iron, Gold (a list saved without one gets it added when the game starts; `ReinforcedColor` is now `CopperColor`) |
| `bool UpdateOldBonuses()` | replaces a Set Bonuses list saved before 21/11/2026 (stamina bonuses, Iron first) with today's; Awake and the armor / loot chest tools call it |
| `float Reduce(float damage)` | a hit after the armor's share (PlayerHealth.TakeDamage uses it) |
| `bool Equip(ItemData item)`, `bool TakeOff(ArmorSlot slot)` | put a piece on from the inventory / back into it (false = can't, e.g. no room) |
| `bool EquipFromCursor(ArmorSlot slot)` | put on the piece held on the cursor; the old piece goes onto the cursor |
| `static event Action Changed` | a piece was put on or taken off |

### PlayerRespawn (on the Player)

Get it: `respawn = FindFirstObjectByType<PlayerRespawn>();`

| Member | What it is |
|---|---|
| `Vector3 RespawnPoint` | where the player comes back (set in the Inspector) |
| `void SetRespawnPoint(Vector3 point)` | move it (WorldGenerator puts it at the new world's start) |
| `void Respawn()` | bring the player back now (full health, food, stamina); in the new world with New World On Death it loads a brand-new world instead (no `Respawned` then: the scene starts again) |
| `bool StartsNewWorld` | true when `Respawn()` will load a new world (the new world, `WorldGenerator.NewWorldOnRespawn`) |
| `event Action Respawned` | fired after every respawn in the same world |

What happens to the items on death is decided by **GameRules** (below), not by your scripts.

### PlayerInventory (on the Player, `PlayerInventory.Instance`)

Get it: nothing to find, write `PlayerInventory.Instance` (or keep it: `PlayerInventory inv = PlayerInventory.Instance;`)

| Member | What it is |
|---|---|
| `bool Add(id, name, icon, amount, ItemData data)` | add items (stacks by id); `false` = inventory full |
| `bool PutOnCursor(id, name, icon, amount, ItemData data)` | put items on the cursor stack (when it's empty or the same item) |
| `bool PutBack(id, name, icon, amount, ItemData data)` | add items back without counting them as new (no `ItemAdded`, no "+1" message), e.g. armor taken off |
| `int SlotOf(ItemData item)` | index in `Slots` of the first slot with this item (the one `Remove` takes from first), -1 = none |
| `bool AddInto(int slot, id, name, icon, amount, ItemData data)` | add into that slot if it's empty (e.g. where an upgrade's old tool was); otherwise the same as `Add` |
| `bool AddToHotbar(id, name, icon, amount, ItemData data, int keepSlot = -1)` | add to the hotbar first: a hotbar stack of the same item, else the leftmost empty hotbar slot, else like `Add` (keepSlot: an upgrade's old tool slot on the hotbar is used first) |
| `bool AddCrafted(ItemData item, int amount, int oldSlot = -1)`, `static bool GoesToHotbar(ItemData)` | what crafting uses (Turki's `CraftingMenu.Craft`): tools and food (Item Type Tool / Consumable) go to the hotbar first, anything else like `AddInto` |
| `int Count(string id)` | how many of this item, all slots together |
| `bool Remove(string id, int amount)` | take items away (all or nothing) |
| `Slot TakeOne(string id)` | take 1, returns what was taken (or null) |
| `Slot FindAny(string id)` | first slot with this item (or null) |
| `Slot ActiveItem` | the selected hotbar slot (null = none selected) |
| `IReadOnlyList<Slot> Slots` | every slot: inventory first, then hotbar (from `HotbarStart`) |
| `void ClearAll()` | empty everything |
| `static event Action Changed` | items or the hotbar selection changed |
| `static event Action<Slot, int> ItemAdded` | items came in with `Add` (picked up, harvested, crafted): the slot they went into and how many. Not fired when items only move inside the inventory |

A `Slot` has `id`, `name`, `icon`, `count`, `data` (its Item Data) and `IsEmpty`. `slot.ShownName` = the name the
player sees: the Item Data's **Display Name**, or its file name when that's empty (not the prefab's name). The
inventory shows it next to the mouse (Tony's `ItemTooltip`; `ItemTooltip.Show(text)` / `Hide()` for your own UI).

### ItemData (asset in `Assets/Config/Items`) and ItemPickup (on world items)

Get them: ItemData = a `public ItemData wood;` field with the file dragged in. ItemPickup = on an item object: `obj.GetComponent<ItemPickup>()`

| ItemData member | What it is |
|---|---|
| `string Id` | the file name: the id everything uses |
| `string DisplayName`, `GameObject Prefab`, `Sprite GetIcon()` | name, world object, picture |
| `ItemType Type`, `bool IsConsumable`, `Consumable Consumable` | Material / Consumable; `Consumable.health / hunger / stamina` |
| `IReadOnlyList<Ingredient> Recipe`, `int CraftAmount`, `CraftStation CraftedAt` | crafting info |
| `bool IsCraftable`, `bool CanCraft(inv)`, `bool TakeRecipeFrom(inv)`, `string Missing(inv)` | crafting checks |
| `bool IsArmor`, `ArmorStats Armor` | worn armor: `Armor.slot` (Helmet, Shoulders, Chest, Gloves, Legs, Boots), `Armor.protection` (percent of every hit, e.g. 1.5), `Armor.set` (e.g. "Iron") |
| `bool IsBow`, `BowStats Bow` | (22/11/2026) a Tool of Kind Bow: `Bow.damage` (added to its arrows), `Bow.drawSeconds` (time to full draw), `Bow.arrowSpeed` (m/s at full draw) |
| `bool IsAmmo`, `AmmoStats Ammo` | an arrow: `Ammo.damage` (added to the bow's), `Ammo.head` (its head's material) |
| `ItemData UpgradedFrom` | for a tool: the tool in its recipe that crafting uses up (a Stone Axe's Wooden Axe); null otherwise |

| ItemPickup member | What it is |
|---|---|
| `ItemData Data`, `string ItemId`, `int Amount` | which item and how many this object gives |
| `void Setup(ItemData data, int amount)` | fill it in from code |

### ItemConsumer (on the Player)

Get it: `consumer = FindFirstObjectByType<ItemConsumer>();`

| Member | What it is |
|---|---|
| `bool TryConsumeHand()` | eat the item in hand (right click already does this) |
| `event Action<ItemData> Consumed` | something was eaten |

### GameRules (object in the scene, read from anywhere)

Get it: nothing to find, write `GameRules.KeepInventory` etc. directly

| Rule | Default | Meaning |
|---|---|---|
| `GameRules.DoDaylightCycle` | on | time of day moves |
| `GameRules.KeepInventory` | off | on = keep all items on death |
| `GameRules.ClearItemOnDead` | off | on = items are deleted on death (ignored if Keep Inventory is on) |
| `GameRules.AutoPickupDrops` | off | on = dropped items are collected by walking near them |
| `GameRules.DropItem` | off | on = the player can throw items out of the inventory; off = they can't (dying still drops items, see Keep Inventory) |
| `GameRules.OverwriteItemIcon` + `GameRules.ItemIconStyle` | off / Auto Icon | on = every item in the inventory, hotbar, cursor and build menu is shown the same way: **Auto Icon** (photo of the prefab), **Label** (the prefab name in capitals, e.g. "SWORD", like Marko's tool slots), **Static Image** or **Aseprite Animation** (the item's own; falls back to Auto Icon). Code: `ItemIconRules.Resolve(...)`, `ItemIconRules.Label(slot)` |
| `GameRules.WorldInteraction` | off | old rule for grabbing / levitating anywhere |
| `GameRules.PerkIcons` | on | on = the HUD shows the perks as icons (x2 for stacks); off = boxes with each perk's name and stats (PerkBar) |
| `GameRules.ToolTest` | off | on = the player gets one of every tool (ToolTestKit, for testing) |
| `GameRules.ArmorTest` | off | on = the player gets one of every armor piece: the Copper and Iron sets (ArmorTestKit, for testing; its own rule since 20/11/2026, Tool Test no longer gives armor) |
| `GameRules.NewChests` | on | the new chest system (random common / epic boxes, a legendary box, REROLL everywhere once the player has the Reroll perk); off = the old one. The New Game page's Testing row sets it (`GameSetup.NewChests`) |
| `GameRules.ShowNotifications` + `GameRules.NotificationSide` | on / Left | on = "+3 Wood" / "+2 Coins" messages show on the **Left** or **Right** side of the screen (NotificationFeed); off = no messages. Change the side from code: `GameRules.SetNotificationSide(NotificationSide.Right)` |

Read: `if (GameRules.KeepInventory) …`. Change: `GameRules.Set(GameRule.KeepInventory, true);`.

### DaylightCycle (on the Directional Light)

Get it: `DaylightCycle.Current` (no searching; null if there's no sun in the scene).

| Member | What it is |
|---|---|
| `float TimeOfDay` | 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset |
| `int Day` | which day it is: starts at 1, +1 when Dawn starts (so "Day 8 - Night" = the night after day 8) |
| `float DaysSurvived` | how long this life has lasted, in days of game time, from the start (2.25 = two days and six hours; the death page's "YOU SURVIVED 2 DAYS"). After a death the day goes back to the start, so read it in PlayerHealth's `Dying` |
| `DayPhase Phase` | `Night`, `Dawn`, `Morning`, `Noon`, `Afternoon` or `Dusk` |
| `bool IsNight` | `Phase == Night`: from **0.77** (18:28) to **0.23** (05:31); set in the sun's Inspector ("Parts Of The Day") |
| `string ClockText` / `int Hour`, `Minute` | the clock, e.g. "14:30" |
| `float SecondsUntilNight` / `SecondsUntilMorning` | real seconds left (0 if it's already night / day) |
| `float SunHeight` | how high the sun is: 1 = noon, 0 = on the horizon (sunrise / sunset), -1 = midnight |
| `float SkyDip` | 1 normally, dips to 0 for a moment while the light turns from sun into moon (the sky fades out then) |
| `static event Action<int> NightStarted` | night starts (the day number). **Use this to spawn enemies** |
| `static event Action<int> DayStarted` | night ends, a new day starts (the new day number) |
| `static event Action<DayPhase> PhaseChanged` | any part of the day starts |
| `void ResetToStart()` | back to the starting day, time and light. Happens by itself when the player dies (Reset On Player Death, on by default); dying at night fires DayStarted, so night enemies burn away |

The events are **static**: subscribe from anywhere, any time, no need to find the sun first.
They also fire once when the game starts (NightStarted if it starts at night, DayStarted if in the day).
At night the same Directional Light is the **moon** (Moonlight, on by default, since 19/11/2026), so don't read the
light's direction as the sun's: use `TimeOfDay` / `IsNight`. Guide: `Docs/Day And Night Setup.md`.

```csharp
void OnEnable()  { DaylightCycle.NightStarted += OnNight; }
void OnDisable() { DaylightCycle.NightStarted -= OnNight; }
void OnNight(int day) { /* spawn more enemies on later nights, e.g. 2 + day */ }
```

### PlayerPerks (on the Player): perks on the HUD

Remembers which perks the player has, for the HUD's perk row (PerkBar: name + stats per slot).

| Member | What it is |
|---|---|
| `static void Record(PerkData perk)` | call it after giving a perk (Marko's PerkManager already does, a `[Tony]` line): remembers its kind for the HUD (same kind twice = "x2") and the Perk Data itself. `Record(ChestPerkType type)` for a kind only |
| `static int TimesPicked(PerkData perk)` | how often this Perk Data was picked in this life (the chests' Requires / Max Picks: Tony's PerkRules) |
| kinds = Marko's `ChestPerkType` | since 21/11/2026 (Tony's own `PerkType` is gone): MaxHealth, MaxStamina, MoveSpeed, Fireball and the loot boxes' 18 (SecondWind ... Glide, AttackSpeed, Vampire, Thorns, NightPower, Reroll, KeenEye, LongReach). **New kind:** see "A new perk" in `Docs/Loot Chests Setup.md` |
| `static void Lose(ChestPerkType type)` | takes one of that kind away again: off the HUD at 0 stacks, and one pick less, so a box may offer it again (Last Stand once it has saved the player) |
| `Sprite IconOf(ChestPerkType type)`, `static string NameOf(ChestPerkType type)` | the kind's picture (from **Icons**, filled by Tools > UI > Build HUD, else the Perk Data's icon) and its name ("Max Health"). The HUD's perk icons and the inventory's Info tab use them |
| `IReadOnlyList<OwnedPerk> Owned`, `event Action Changed` | the perks in the order gained; fired when one is added or lost |
| `static PlayerPerks Current` | the one in the scene (null if the player has none) |
| `void ResetPerks()` | loses every perk and undoes the boosts (`ResetMaxHealth`, `ResetMaxStamina`, `ResetMoveSpeed`, `PlayerFireball.ResetFireballPerk`, `PerkEffects.ResetEffects`) and starts the loot boxes' prices again (`ChestTier.ResetPrices`). Runs by itself when the player dies (**Reset On Death**, on) |

### Loot boxes: ChestTier, PerkRules, PerkEffects (Tony's, 21/11/2026)

Guide `Docs/Loot Chests Setup.md` (built by **Tools > World > Build Loot Chests**).

| Member | What it is |
|---|---|
| `ChestTier` (on a box prefab) | `Tier` (Common / Rare / Epic / Legendary), `BoxName`, `Color`, `Price` (now, with the gold or diamond armor set's 10 % off), `FullPrice` (before it), `static float Discount`, `CanReroll` (the system in use, and only once the player has the Reroll perk while Reroll Needs Perk is on), `RerollPrice`, `RandomRoll` (one random perk: new system), `NewSystemOnly`, `List<PerkData> PerksFor(available)`; `static bool NewSystem` (= `GameRules.NewChests`), `static int BoughtOf(LootTier)`, `static void ResetPrices()` |
| `PerkChest` (Marko's, `[Tony]` getters) | `OpeningCost` (ChestTier's price when it has one), `Tier`, `RerollAllowed`, `RerollCost`, `RerollWouldChange`, `bool TryReroll()`, `CanBuyRolled` / `BuyRolled()` (the roll pop-up's BUY) |
| `PerkRollUI` (on `PerkRollCanvas`, **Tools > UI > Build Perk Roll Popup**) | the new system's roll pop-up: `static PerkRollUI Instance`, `bool Open(chest, perk, boxPerks, animate)`, `Close()`, `IsOpen`, `IsRolling`, `static bool IsOpenFor(chest)`, `static bool BlocksClose` |
| `PerkRules` (static) | `bool CanOffer(PerkData)` (Requires picked, under Max Picks), `List<PerkData> Pick(pool, count, shown)` |
| `PerkEffects` (on the Player; `PerkEffects.Current` adds itself) | `static void Apply(PerkData)` (Marko's PerkManager calls it for the new kinds), and the questions: `RollCritical()`, `CriticalChance`, `CriticalMultiplier`, `FoodMultiplier`, `KnockbackMultiplier`, `BountyCoins`, `StaminaRegenBonus`, `AttackSpeedMultiplier`, `DamageMultiplier` (Night Power at night: melee only), `NightPowerOn`, `RerollUnlocked` (Reroll), `KeenEye`, `ReachMultiplier` (Long Reach: 1.1 per pick), `ArrowRecovery` (arrows can be had back, 22/11/2026), `ExtraDrop(type, pickedByKey)`, `TryLastStand(out seconds)` (a used Last Stand leaves: `PlayerPerks.Lose`); hurting through it: `Hurt(target, hurt)` / `WithVampire(target, hurt)` (Vampire heals from the damage really done), `Reflect(attacker, damage)` (Thorns); `Describe(kind)`; ⋮ > Test: Every New Perk Once |
| `MonsterBounty` (static) | `int Killed()`: the extra coins for a kill (armor set + Bounty Hunter); Marko's EnemyCombat calls it. `static event Action MonsterKilled` (22/11/2026): fired for every kill (PlayerStats counts them for the victory and death pages); unsubscribe in OnDisable |
| `Knockback` (static + a small component) | `Push(target, velocity, seconds)`, `bool IsPushed(Rigidbody)` |
| `PlayerHealth.IsShielded` | true for a moment after Last Stand saved the player (hits do nothing then) |

See `Docs/TonyEditMate/21-10-2026Edit.md`.

### VictoryScreen (on GameEndCanvas): winning the game

Surviving **5 days and nights** wins: when Day 6 starts, the game freezes (`Time.timeScale = 0`) and the victory page
shows; **Play Again** reloads the scene. Dying resets the days (DaylightCycle), so it must be one life.
Since 22/11/2026 the page has a gold trophy, the coins (what's left in Marko's `PlayerCoins`, and what this life earned
and spent: every `SpendCoins`, e.g. a chest or a reroll, counts as spent; the coins lost by dying don't), all stats like
the inventory's Info tab (perks, armor, speeds...) with MINIMISE / SHOW ALL STATS, and a **Main Menu** button.

| Member | What it is |
|---|---|
| `static event Action Won` | fired once when the player wins (e.g. stop spawning, victory music) |
| `bool HasWon`, `int DaysToWin`, `WinTime WinTime` | find it with `FindFirstObjectByType<VictoryScreen>()`. Win = survive Days To Win days, then the next Win Time (Dawn / Morning / Midday / Afternoon / Dusk / Night / Midnight) |
| `void Win()` | wins right away (for testing, or another win condition) |

Built by **Tools > UI > Build Game End Screen**. Guide: `Docs/Game End Setup.md`.

### HUD counters (top right)

`DayCounter` ("DAY 5 - Noon"), `TimeCounter` ("14:30 - Night in 2:15") and `CoinCounter` (reads Marko's
`PlayerCoins.Instance.Coins` and redraws on `CoinsChanged`). Nothing to call: they find what they need.
**Tools > UI > Build HUD** rebuilds the whole HUD if it's deleted. Guide: `Docs/HUD Setup.md`.

### NotificationFeed: messages for the player ("+3 Wood")

Short messages on the side of the screen that fade away after 3 seconds. Get it: nothing to find, it's static.

| Member | What it is |
|---|---|
| `static void Show(string text, Sprite icon = null)` | shows a message, e.g. `NotificationFeed.Show("Night is coming!");` |
| `static void ShowGain(string key, string name, int amount, Sprite icon = null)` | shows "+5 Coins"; another gain with the same `key` while it's on screen adds up ("+3 Wood" + 2 = "+5 Wood") |

Items given with `PlayerInventory.Instance.Add` and coins given with `PlayerCoins.AddCoins` already show a message
(`GainNotifier`), so don't add your own for those. Nothing shows while GameRules > **Show Notifications** is off.
Built by **Tools > UI > Build Notifications**. Guide: `Docs/Notifications Setup.md`.

### GameSetup: what the player chose on the New Game page

Static, nothing to find. Every value is **null when the player left it on the default** (= the game as GameScene has it).
The choices are put into the game by themselves when GameScene loads (before anything starts), and Play Again keeps them.

| Member | What it is |
|---|---|
| `int? DaysToWin`, `WinTime? WinAt` | the goal (already given to VictoryScreen) |
| `float? DayLengthMinutes`, `float? StartTime` | the sun (already given to DaylightCycle) |
| `Difficulty? ChosenDifficulty` | `Peaceful`, `Easy`, `Hard` (null = Normal). Read it to scale your own things |
| `bool? SpawnEnemies`, `float? SpawnIntervalScale`, `int? MonstersAtOnce`, `float? MonsterLeaveDistance`, `float? HungerDrainPerSecond` | what the difficulty set (already given to EnemySpawner / NightMonsters / PlayerHunger). `MonstersAtOnce` / `MonsterLeaveDistance` (22/11/2026) are set only on Easy (20 and 90 m: NightMonsters' limits). `SpawnIntervalScale` (22/11/2026, replaced `int? EnemiesPerNight`, which did nothing since EnemySpawner moved to spawn intervals) = the seconds between night monsters x this: 2 on Easy, 0.67 on Hard. `GameSetup.Stretched(seconds, scale)` gives the same seconds the game uses |
| `DeathRule? WhenDying`, `bool? AutoPickupDrops`, `bool? ShowNotifications` | the rules (already given to GameRules) |
| `bool IsChosen` | START GAME was pressed this session (false when you press Play straight in GameScene) |
| `bool? NewWorld` | true = Map: New world (ProceduralGenerateScene, a new random world every game); null = the old world |
| `bool? StartWithAllTools`, `bool? StartWithAllArmor` | the TESTING ticks "Start with all tools" / "Start with all armor" (already given to GameRules Tool Test / Armor Test) |
| `int? StartCoins` | (22/11/2026) Starting coins (already given to Marko's PlayerCoins with `SetStartCoins`) |

### Menu effects you can reuse (title screen)

| Script | Drag it onto | What it does |
|---|---|---|
| `ButtonFX` | any Button (text child named `Label`) | grows + turns gold on hover, light shine, squash + spring on click, ripple, sparks |
| `CascadeIn` | a panel | its children slide in one after another every time it's shown |
| `PulseFX` | any picture or text | a slow glowing heartbeat |
| `TitleTextFX` | a TextMeshPro text | letters fall in, float in a wave, shine |
| `UITween.Run(this, seconds, ease, t => ...)` | (code) | a smooth animation with an ease (OutBack, OutBounce...), real time |
| `UISparks.Burst(rect, color, count)` | (code) | a burst of sparks from a UI object |
| `ScreenFader.LoadScene(scene, title, subtitle)` | (code) | fade to black, title card, load, fade in |

Other changes for the title screen: `KeyBindings.SetSeveral(actions, key)` sets several actions to one key without
swapping (the key menu uses it); `TabGroup.Changed` (from, to) fires when a tab opens.
Guide: `Docs/Main Title Setup.md`.

### BuildingData, BlueprintPlacer

Get them: BuildingData = a `public BuildingData wall;` field with the file dragged in. `placer = FindFirstObjectByType<BlueprintPlacer>();` (only for `Built`; `IsPlacing` / `Begin` are static)

| Member | What it is |
|---|---|
| `BuildingData`: `DisplayName`, `Prefab`, `Recipe`, `CanAfford(inv)` | one building (asset) |
| `BlueprintPlacer.IsPlacing` (static) | the player is placing a building |
| `BlueprintPlacer.Begin(data)` (static) | start placing this building from code |
| `blueprintPlacer.Built` (event, on the Player) | a building was built (gives its `BuildingData`) |
| `BlueprintPlacer.Mode` (static) | `PlaceMode.Snap` or `PlaceMode.Surface` (middle click switches) |
| `BlueprintPlacer.IsSnapped` (static) | the blueprint is locked onto a built piece right now (green) |
| `BuildingData.Piece` | how it snaps: `PieceType.Floor` / `Wall` / `Roof` / `Other` (Auto = guessed from the file name) |
| `BuiltPiece` (on every building the player built) | `Data` (its BuildingData), `Type`; `BuiltPiece.All` = every built piece in the scene |

---

## 4. Rules that keep everyone's scripts working together

- **Menus:** if you open a window that uses the mouse, call `MenuState.SetOpen(this, true)` when it opens and
  `MenuState.SetOpen(this, false)` when it closes, and free the cursor:
  `Cursor.lockState = CursorLockMode.None; Cursor.visible = true;` (lock it again on close).
  The camera, movement keys and other menus then step aside by themselves.
- **Close your menu when the player dies:** listen to `MenuState.CloseAllRequested` and close yourself there.
  Tony's PlayerRespawn and DeathScreen call `MenuState.CloseAll()` the moment the player dies, so only the death page
  stays on screen:
  `void OnEnable() { MenuState.CloseAllRequested += CloseNow; }`,
  `void OnDisable() { MenuState.CloseAllRequested -= CloseNow; }`,
  `void CloseNow() { if (isOpen) Close(); }`.
  Turki's CraftingMenu and Marko's PerkSelectionUI already do this (Tony's `[Tony]` lines, logs in
  `Docs/TonyEditMate/Turki/` and `Docs/TonyEditMate/Marko/`).
- **Before reacting to clicks / keys,** check `if (MenuState.AnyOpen) return;` so clicks inside a menu don't also attack or build.
- **Keys** come from `KeyBindings` (e.g. `KeyBindings.Swing`, `KeyBindings.Interact`), so players can change them in one place.
  Need a new key? Ask Tony to add it.
- **Menus in the game are for the mouse** (since 22/11/2026): in a scene with a player, Tony's `NoKeyNavigation` turns
  off the EventSystem's keyboard navigation (**Send Navigation Events**), so W A S D, the arrow keys, Space and Enter
  never scroll a list, move a slider or press the last clicked button while the player walks or jumps (the player can
  walk with the inventory open). Your buttons, scroll bars and sliders work with the mouse as usual; don't build a
  menu that needs the arrow keys or Enter. A key your menu needs: read it yourself (`Input.GetKeyDown(...)`).
- **Don't edit** files in `Assets/Script/`. Ask Tony instead, so nothing breaks for the others.
