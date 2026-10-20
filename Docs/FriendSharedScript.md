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
| **C. Static: no finding needed** | `PlayerInventory.Instance`, `GameRules`, `MenuState`, `KeyBindings`, `Crosshair.Target`, `BlueprintPlacer.IsPlacing` | Write the class name and use it directly, from any script |
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
| Item in hand (for tools) | `PlayerInventory.Instance.ActiveItem` (null = no hotbar slot selected) and `.data` = its Item Data |
| Make a new item | Select the object, **Tools > Items > Make Item From Selection** (see `Docs/Item Setup.md`) |
| Make an item that heals / feeds | Item Data > **Item Type = Consumable**, fill Health / Hunger / Stamina. Right click eats it (already works) |
| Open / close a crafting window | `MenuState.SetOpen(this, true/false)` + free the cursor (see section 4) |
| The crafting menu's key | `Input.GetKeyDown(KeyBindings.Crafting)` (default **C**, change it on the KeyBindings object). `CraftingMenu` already uses it; its old `openKey` field is gone |
| Crafting inside the inventory | A CraftingCanvas under `InventoryWindow > Pages > CraftingPage` now shows itself whenever the Crafting tab is open (Tony's `[Tony]` lines in `CraftingMenu`, see `Docs/TonyEditMate/20-10-2026Edit.md`). The scene-root CraftingCanvas still opens with C |

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
| Hurt the player | `health.TakeDamage(10f)` |
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
| Drop food / items from an animal | see the example below |
| What the player aims at | `Crosshair.Target` (a GameObject, or null) and `Crosshair.HitPoint` |
| Is a menu open (don't attack / dodge then) | `MenuState.AnyOpen` |
| Read keys | `Input.GetKeyDown(KeyBindings.Swing)` (Swing = left mouse, free for attacking) |

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
| Make a new building piece | Select it, **Tools > Building > Make Building From Selection**, fill the recipe. It appears in the build menu (B) by itself |
| Know when the player built something | `FindFirstObjectByType<BlueprintPlacer>().Built += OnBuilt;` (gives the `BuildingData`) |
| Is the player placing a building now? | `BlueprintPlacer.IsPlacing` |
| Where the player respawns | `PlayerRespawn` on the Player, **Respawn Point** in the Inspector (default 0, 0.5, 0). Read it: `respawn.RespawnPoint` |
| Day / night | `DaylightCycle` on the Directional Light: `TimeOfDay`, `IsNight`; GameRules > **Do Daylight Cycle** stops time |
| World rules | `GameRules` object in the scene (see section 3) |

Guides: `Docs/Build Menu Setup.md`, `Docs/Item Setup.md`, `Docs/Inventory Setup.md`.

---

## 3. Script reference

### PlayerHealth (on the Player)

Get it: `health = FindFirstObjectByType<PlayerHealth>();` in `Start()`

| Member | What it is |
|---|---|
| `float Health`, `float MaxHealth` | current / max (default 100) |
| `bool IsDead` | health is 0 |
| `void TakeDamage(float amount)` | lose health; at 0 the player dies (`Died` fires once) |
| `void Heal(float amount)` | gain health, never above max |
| `void Revive()` | full health again (PlayerRespawn uses it) |
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
| `float Stamina`, `float MaxStamina` | current / max (100) |
| `bool IsTired` | hit 0 and hasn't refilled enough yet: no sprint, no jump |
| `void Use(float amount)` | spend stamina (attack, dodge…); hitting 0 makes the player tired |
| `void Restore(float amount)` | give stamina back at once |
| `bool CanSprint`, `bool CanJump` | used by movement |

It refills by itself after a short rest.

### PlayerController, PlayerJump (on the Player)

Get them: `controller = FindFirstObjectByType<PlayerController>();`, `jump = FindFirstObjectByType<PlayerJump>();`

| Member | What it is |
|---|---|
| `bool IsMoving`, `bool IsSprinting` | PlayerController: moving / sprinting (Left Shift) right now. Sprinting only counts on the ground |
| `bool IsGrounded` | PlayerController: standing on walkable ground (not in the air, not on a too-steep slope). PlayerJump has one too (standing on something not too steep) |
| `float MaxSlopeAngle` | PlayerController: the steepest slope the player can walk up (default 45 degrees); steeper = a wall |
| `event Action Jumped` | PlayerJump: fired on every jump |

### PlayerRespawn (on the Player)

Get it: `respawn = FindFirstObjectByType<PlayerRespawn>();`

| Member | What it is |
|---|---|
| `Vector3 RespawnPoint` | where the player comes back (set in the Inspector) |
| `void Respawn()` | bring the player back now (full health, food, stamina) |
| `event Action Respawned` | fired after every respawn |

What happens to the items on death is decided by **GameRules** (below), not by your scripts.

### PlayerInventory (on the Player, `PlayerInventory.Instance`)

Get it: nothing to find, write `PlayerInventory.Instance` (or keep it: `PlayerInventory inv = PlayerInventory.Instance;`)

| Member | What it is |
|---|---|
| `bool Add(id, name, icon, amount, ItemData data)` | add items (stacks by id); `false` = inventory full |
| `int Count(string id)` | how many of this item, all slots together |
| `bool Remove(string id, int amount)` | take items away (all or nothing) |
| `Slot TakeOne(string id)` | take 1, returns what was taken (or null) |
| `Slot FindAny(string id)` | first slot with this item (or null) |
| `Slot ActiveItem` | the selected hotbar slot (null = none selected) |
| `IReadOnlyList<Slot> Slots` | every slot: inventory first, then hotbar (from `HotbarStart`) |
| `void ClearAll()` | empty everything |
| `static event Action Changed` | items or the hotbar selection changed |

A `Slot` has `id`, `name`, `icon`, `count`, `data` (its Item Data) and `IsEmpty`.

### ItemData (asset in `Assets/Config/Items`) and ItemPickup (on world items)

Get them: ItemData = a `public ItemData wood;` field with the file dragged in. ItemPickup = on an item object: `obj.GetComponent<ItemPickup>()`

| ItemData member | What it is |
|---|---|
| `string Id` | the file name: the id everything uses |
| `string DisplayName`, `GameObject Prefab`, `Sprite GetIcon()` | name, world object, picture |
| `ItemType Type`, `bool IsConsumable`, `Consumable Consumable` | Material / Consumable; `Consumable.health / hunger / stamina` |
| `IReadOnlyList<Ingredient> Recipe`, `int CraftAmount`, `CraftStation CraftedAt` | crafting info |
| `bool IsCraftable`, `bool CanCraft(inv)`, `bool TakeRecipeFrom(inv)`, `string Missing(inv)` | crafting checks |

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

Read: `if (GameRules.KeepInventory) …`. Change: `GameRules.Set(GameRule.KeepInventory, true);`.

### DaylightCycle (on the Directional Light)

Get it: `DaylightCycle.Current` (no searching; null if there's no sun in the scene).

| Member | What it is |
|---|---|
| `float TimeOfDay` | 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset |
| `int Day` | which day it is: starts at 1, +1 when Dawn starts (so "Day 8 - Night" = the night after day 8) |
| `DayPhase Phase` | `Night`, `Dawn`, `Morning`, `Noon`, `Afternoon` or `Dusk` |
| `bool IsNight` | `Phase == Night`: from **0.77** (18:28) to **0.23** (05:31); set in the sun's Inspector ("Parts Of The Day") |
| `string ClockText` / `int Hour`, `Minute` | the clock, e.g. "14:30" |
| `float SecondsUntilNight` / `SecondsUntilMorning` | real seconds left (0 if it's already night / day) |
| `static event Action<int> NightStarted` | night starts (the day number). **Use this to spawn enemies** |
| `static event Action<int> DayStarted` | night ends, a new day starts (the new day number) |
| `static event Action<DayPhase> PhaseChanged` | any part of the day starts |
| `void ResetToStart()` | back to the starting day, time and light. Happens by itself when the player dies (Reset On Player Death, on by default); dying at night fires DayStarted, so night enemies are removed |

The events are **static**: subscribe from anywhere, any time, no need to find the sun first.
They also fire once when the game starts (NightStarted if it starts at night, DayStarted if in the day).

```csharp
void OnEnable()  { DaylightCycle.NightStarted += OnNight; }
void OnDisable() { DaylightCycle.NightStarted -= OnNight; }
void OnNight(int day) { /* spawn more enemies on later nights, e.g. 2 + day */ }
```

### VictoryScreen (on GameEndCanvas): winning the game

Surviving **5 days and nights** wins: when Day 6 starts, the game freezes (`Time.timeScale = 0`) and the victory page
shows; **Play Again** reloads the scene. Dying resets the days (DaylightCycle), so it must be one life.

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

### BuildingData, BlueprintPlacer

Get them: BuildingData = a `public BuildingData wall;` field with the file dragged in. `placer = FindFirstObjectByType<BlueprintPlacer>();` (only for `Built`; `IsPlacing` / `Begin` are static)

| Member | What it is |
|---|---|
| `BuildingData`: `DisplayName`, `Prefab`, `Recipe`, `CanAfford(inv)` | one building (asset) |
| `BlueprintPlacer.IsPlacing` (static) | the player is placing a building |
| `BlueprintPlacer.Begin(data)` (static) | start placing this building from code |
| `blueprintPlacer.Built` (event, on the Player) | a building was built (gives its `BuildingData`) |

---

## 4. Rules that keep everyone's scripts working together

- **Menus:** if you open a window that uses the mouse, call `MenuState.SetOpen(this, true)` when it opens and
  `MenuState.SetOpen(this, false)` when it closes, and free the cursor:
  `Cursor.lockState = CursorLockMode.None; Cursor.visible = true;` (lock it again on close).
  The camera, movement keys and other menus then step aside by themselves.
- **Before reacting to clicks / keys,** check `if (MenuState.AnyOpen) return;` so clicks inside a menu don't also attack or build.
- **Keys** come from `KeyBindings` (e.g. `KeyBindings.Swing`, `KeyBindings.Interact`), so players can change them in one place.
  Need a new key? Ask Tony to add it.
- **Don't edit** files in `Assets/Script/`. Ask Tony instead, so nothing breaks for the others.
