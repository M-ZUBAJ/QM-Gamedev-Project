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
| Your trees are low-poly trees now | Tony's tool (**Tools > World > Use Low Poly Grassland Trees**) changed only the look and pivot of your `TreeNode.prefab`. It's a Round Oak from Broken Vector's pack, standing on the foot of its trunk. Your `ResourceNode` and its settings (Wood, Apple bonus, health) aren't changed. 5 more looks are Prefab Variants in `Assets/Prefabs/ResourceNodes/Trees`, so they follow TreeNode's drops and health. Log: `Docs/TonyEditMate/Turki/12-11-2026 Low Poly Grassland Trees.md` |
| Your sheep (AnimalNode) | Not used in GameScene anymore: deer took its place (`DeerNode`, Tony's **Tools > World > Build Wild Animal**). Your `AnimalNode.prefab` and `Wander` script aren't changed. DeerNode got a copy of your sheep's ResourceNode settings (Meat 1 to 3, health 100). Change the deer's drops on DeerNode's ResourceNode. Guide: `Docs/Animal Setup.md` |
| Tools and mining tiers | Item Data has a new **Item Type: Tool** (Kind + Tier) and, for materials, **Mining > Mined With**: the weakest pickaxe that can mine a rock that drops it. Copper and Iron are now **Stone** (a hand can't mine them), Stone stays None. New gold / diamond ores: set Mined With to Iron / Gold. Your ResourceNode got one `[Tony]` read-only line, `DropItem`. Your crafting list now offers **Iron Sword / Stone Axe / Stone Pickaxe** (same recipes as the old Sword / Axe / PickAxe). 15 tools with recipes are in `Assets/Config/Items/Tools`. Your panel fits ~8 rows: add a Scroll View to offer more. Log: `Docs/TonyEditMate/Turki/12-11-2026 Tier Tools.md` |

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
| "+2 Coins" when an enemy is defeated | Already done: `GainNotifier` watches `CoinsChanged`, so every `AddCoins(...)` shows a message (your files aren't changed) |
| Show any other message | `NotificationFeed.Show("A boss appeared!");` (see `NotificationFeed` in section 3) |
| Make enemies stronger on Hard | `if (GameSetup.ChosenDifficulty == GameSetup.Difficulty.Hard) { ... }` (null = Normal, the game as it is). Enemies per night is already set from the New Game page through your `EnemySpawner`'s public `spawnAtNight` / `enemiesPerNight` (your file isn't changed) |
| Drop food / items from an animal | see the example below |
| What the player aims at | `Crosshair.Target` (a GameObject, or null) and `Crosshair.HitPoint` |
| Is a menu open (don't attack / dodge then) | `MenuState.AnyOpen` |
| Read keys | `Input.GetKeyDown(KeyBindings.Swing)` (Swing = left mouse, free for attacking) |
| Your enemy is now an animated slime | `Assets/Prefabs/Enemy/SlimeEnemy.prefab` (made by **Tools > Enemies > Build Monster Enemy**, guide `Docs/Enemy Setup.md`) uses your EnemyCombat / EnemyMovement / EnemyAttack plus Tony's `EnemyAnimator`, and the EnemySpawner now spawns it. Tony's `[Tony]` lines in your 4 enemy scripts: physics movement (walls stop it), spawning on the ground, attack animation, death animation before removal. Every change and how to undo it: `Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md`. Your old `Enemy.prefab` still works |
| Enemies and animals dying into particles | Already done for the slime and the deer: Tony's `DeathEffect` (on the prefab, **Tools > Effects > Build Death Particles**, guide `Docs/Death Effect Setup.md`). Harmful = has your `EnemyAttack` = red, else green. Your `EnemyCombat.Die()` is unchanged: Tony's `EnemyAnimator.PlayDeath()` (already called from your `[Tony]` lines) asks DeathEffect to turn the body into red particles during the death animation and returns how long that takes. A new enemy prefab: add `DeathEffect` and drag `Prefabs/Effects/DeathParticles` into it, or run the tool. Burst one yourself: `GetComponent<DeathEffect>().Burst();` |
| Play an enemy animation from your code | `GetComponent<EnemyAnimator>().PlayAttack();` (`PlayDeath()` is already called by `EnemyCombat.Die()`; `IsDead` = it's dying) |
| Animate the player's attack / fireball | Already done: Tony's `[Tony]` lines in your `PlayerCombat.Attack()` play what's in the hand: nothing / a non-tool = `Punch`, sword or axe = `Attack`, pickaxe = `Mine` (Attack and Mine both show Mixamo's downward axe chop). Fireball: `PlayerAnimator.Play(PlayerAction.Cast);` right after its `Instantiate(...)`. Upper body only, so the legs keep running. The flinch when the player is hurt already plays by itself |
| Your tools | Replaced by 15 tier tools (pickaxe / axe / sword in Wood, Stone, Iron, Gold, Diamond, Abduo Bricks' Low Poly Tool Pack): Item Data with **Item Type = Tool** (Kind + Tier), held in the character's hand (Tony's `ToolInHand`). Your damage numbers stay (a wooden tool does them); `[Tony]` lines map the tool to your `"Sword"` / `"Axe"` / `"PickAxe"` checks (`ToolRules.WeaponName`) and add +25 % per tier at the tool's job (`ToolRules.Boost`). Copper / iron need a Stone pickaxe (`ToolRules.CanMine`). Your fireball works with any sword (`ToolRules.IsSword`). Your GiveSwordForTesting / EquippedWeaponDisplay left the scene (GameRules > **Tool Test** gives all tools now). Log: `Docs/TonyEditMate/Marko/12-11-2026 Tier Tools.md`, guide `Docs/Tool Setup.md` |
| What's in the player's hand | `ToolStats tool = ToolRules.ToolIn(PlayerInventory.Instance.ActiveItem);` (null = a bare hand), then `tool.kind` (Pickaxe / Axe / Sword) and `tool.tier` (Wood ... Diamond) |
| Bare-handed punches | Tony's `PunchCombo` (on the Player) picks them: left click = left / right jabs in turn, right click = heavy left hook / heavy right punch in turn, which hit and cost x2. `[Tony]` lines in your `PlayerCombat` ask `punchCombo.Next(...)` and call `Attack(punch)`; heavy damage = `PunchCombo.Power(swing)`. Log: `Docs/TonyEditMate/Marko/13-11-2026 Punch Combo.md`, guide `Docs/Mixamo Animations.md` |
| A burst where an attack hits | Bare-handed hits already burst, and they hurt **when the fist lands**: Tony's `[Tony] replaced` lines in your `PlayerCombat.Attack()` call `PunchEffect.Punch(target, hit, () => target.TakeDamage(damage))` instead of `TakeDamage` at once. It waits the Impact Delay (about 0.3 s), hurts, then bursts on the spot that was hit, even on a running deer. Weapons still hit at once. For a burst only (sword, fireball): `PunchEffect.Play(hit.point, hit.normal);`. Log: `Docs/TonyEditMate/Marko/12-11-2026 Punch Wind Burst.md` |
| Coins when the player dies | They go back to the starting amount: Tony's `[Tony]` lines in your `PlayerCoins` (Reset On Death tick box, `ResetCoins()`). Log: `Docs/TonyEditMate/Marko/10-11-2026 Coins Reset On Death, Perk Menu Closes, Punch.md` |
| Animals that run away | Already done: the sheep are deer now (ithappy's **Animals FREE** pack, `Assets/Prefabs/ResourceNodes/DeerNode`). Tony's `WildAnimal` makes them graze, wander, and run from the player when the player comes within 12 m or hits them. Your `PlayerCombat` damages them as `ResourceType.Animal`, like the sheep. More animals: select e.g. `Imported3D/ithappy/Animals_FREE/Prefabs/Horse_001`, then **Tools > World > Build Wild Animal**. Guide: `Docs/Animal Setup.md` |
| A sight perk or buff | `PlayerSight.Current.AddRange(20f);` (a perk: until the player dies) or `PlayerSight.Current.AddRange(40f, 30f);` (a buff: 30 seconds). Deer then spawn farther out by themselves (Tony's `AnimalSpawner` reads the range live). `PlayerSight.Current.Range` = the range now |

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
| Spawn something where the player can't see it | `PlayerSight.Current.CanSee(feet, height)` (false = out of sight: behind the player, behind a hill / trunk / wall, or past the sight range). Tony's `AnimalSpawner` does this for the deer; you could do the same for enemies |
| Make a new building piece | Select it, **Tools > Building > Make Building From Selection**, fill the recipe. It appears in the build menu (B) by itself |
| Know when the player built something | `FindFirstObjectByType<BlueprintPlacer>().Built += OnBuilt;` (gives the `BuildingData`) |
| Is the player placing a building now? | `BlueprintPlacer.IsPlacing` |
| Where the player respawns | `PlayerRespawn` on the Player, **Respawn Point** in the Inspector (default 0, 0.5, 0). Read it: `respawn.RespawnPoint` |
| Day / night | `DaylightCycle` on the Directional Light: `TimeOfDay`, `IsNight`; GameRules > **Do Daylight Cycle** stops time |
| World rules | `GameRules` object in the scene (see section 3) |
| Scenes / build order | The game starts at **MainTitleScene** (scene 0 in File > Build Settings), START GAME loads **GameScene**. Rebuild the title with **Tools > UI > Build Main Title Scene** (`Docs/Main Title Setup.md`) |
| Change scene with a fade | `ScreenFader.LoadScene("GameScene", "DAY 1", "Survive 5 nights");` (fades to black, title card, loads, fades in) |

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
| `bool IsDead` | it's dying |

Built by **Tools > Enemies > Build Monster Enemy**. Guide: `Docs/Enemy Setup.md`.

### PunchEffect (on the Player): the grey wind burst where a punch lands

Grey dust clouds, wind lines, a shock ring and a few specks, a little in front of the hit spot. It waits until the fist
lands in the punch animation (Impact Delay), then removes itself after about a second. Get it: nothing to find, `Punch`
and `Play` are static.

| Member | What it is |
|---|---|
| `static void Punch(Component target, RaycastHit hit, Action hurt)` | a punch that lands after the Impact Delay: then `hurt` runs (e.g. `() => enemy.TakeDamage(10)`) and the burst pops on the spot of `target` that was hit, even if it moved. Not run if the target is gone by then. Without a PunchEffect, `hurt` runs at once. PlayerCombat uses it for bare-handed hits |
| `static void Hit(Component target, RaycastHit hit, Action hurt, PlayerAction swing)` | the same for any swing, with its own delay: Punch = Impact Delay, Attack = Swing Delay, Mine = Mine Delay; every hit bursts wind (tools: Tool Burst Size). PlayerCombat uses it for every hit |
| `static void Play(Vector3 point, Vector3 normal)` | a burst at `point`, flying out of the surface (`normal` = `RaycastHit.normal`), e.g. `PunchEffect.Play(hit.point, hit.normal);`. Does nothing if the Player has no PunchEffect or it's switched off |
| `static PunchEffect Current` | the one on the Player (null if none) |

Built by **Tools > Effects > Build Punch Wind Burst** (prefab `Assets/Prefabs/Effects/PunchWindBurst.prefab`). Guide:
`Docs/Punch Effect Setup.md`.

### WildAnimal (on an animal, e.g. DeerNode): grazes, wanders, runs from the player

It needs nothing from your code. It finds the player by itself, keeps itself on the ground and plays its own
animations. Its settings (Flee Distance, Run Speed, Wander Radius...) are in the Inspector. Hurting it the normal way
(`ResourceNode.TakeDamage`) also makes it run.

| Member | What it is |
|---|---|
| `const string SpeedName`, `AnimSpeedName` | its Animator's floats: `Speed` (0 = standing, 1 = walking, 2 = running) and `AnimSpeed` |

Built by **Tools > World > Build Wild Animal** (any ithappy animal; the deer by default). Guide: `Docs/Animal Setup.md`.

### ToolRules (nothing to find: static): how tools work

| Member | What it is |
|---|---|
| `ToolStats ToolIn(PlayerInventory.Slot slot)` | the tool in a slot (`kind`, `tier`), or null (empty, or not a tool) |
| `bool IsSword(slot)` | a sword of any tier |
| `PlayerAction SwingFor(tool)` | the animation a click plays: Punch (null), Mine (pickaxe), Attack (sword, axe) |
| `float Boost(tool, ToolKind job)` | 1.25 per tier above Wood, only at its own job (e.g. `Boost(tool, ToolKind.Sword)` for damage to enemies) |
| `bool CanMine(tool, ResourceNode node)` | can this tool (null = a hand) break it: an ore needs its Mined With pickaxe or better |
| `void TellCantMine(node)` | shows "Needs a Stone Pickaxe or better" (not more than every 1.5 s) |

Tools are made by **Tools > Items > Build Tier Tools**. The selected tool shows in the hand by itself (`ToolInHand`), and
GameRules > **Tool Test** gives one of each. Guide: `Docs/Tool Setup.md`.

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
| `static event Action<Slot, int> ItemAdded` | items came in with `Add` (picked up, harvested, crafted): the slot they went into and how many. Not fired when items only move inside the inventory |

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
| `GameRules.ShowNotifications` + `GameRules.NotificationSide` | on / Left | on = "+3 Wood" / "+2 Coins" messages show on the **Left** or **Right** side of the screen (NotificationFeed); off = no messages. Change the side from code: `GameRules.SetNotificationSide(NotificationSide.Right)` |

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

### PlayerPerks (on the Player): perks on the HUD

Remembers which perks the player has, for the HUD's perk row (PerkBar: name + stats per slot).

| Member | What it is |
|---|---|
| `static void Record(PerkType type)` | call it after giving a perk (Marko's PerkChest already does, `[Tony]` lines). Same perk twice = "x2" |
| `enum PerkType` | MaxHealth, MaxStamina, MoveSpeed, Fireball. **New perk:** add a line here + a case in `PerkName` and `PerkStats` |
| `IReadOnlyList<OwnedPerk> Owned`, `event Action Changed` | the perks in the order gained; fired when one is added |
| `static PlayerPerks Current` | the one in the scene (null if the player has none) |
| `void ResetPerks()` | loses every perk and undoes the boosts (`ResetMaxHealth`, `ResetMaxStamina`, `ResetMoveSpeed`, `PlayerFireball.ResetFireballPerk`). Runs by itself when the player dies (**Reset On Death**, on). **New perk:** also add its undo here |

See `Docs/TonyEditMate/21-10-2026Edit.md`.

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
| `bool? SpawnEnemies`, `int? EnemiesPerNight`, `float? HungerDrainPerSecond` | what the difficulty set (already given to EnemySpawner / PlayerHunger) |
| `DeathRule? WhenDying`, `bool? AutoPickupDrops`, `bool? ShowNotifications` | the rules (already given to GameRules) |
| `bool IsChosen` | START GAME was pressed this session (false when you press Play straight in GameScene) |

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
- **Don't edit** files in `Assets/Script/`. Ask Tony instead, so nothing breaks for the others.
