# Bow Setup (the Wooden Bow and five tiers of arrows)

**What the player sees** (22/11/2026):
- **Wooden Bow** (crafting page > Tools): 10 Wood + 10 Plank. It goes to the hotbar like the other tools and is held
  in the **left hand**.
- **Arrows** (crafting page > **Ammo** tab), 4 per craft, each tier with its own arrowhead color (the same arrow model):

  | Arrow | Recipe (makes 4) | Damage (fully drawn) | Head |
  |---|---|---|---|
  | Stone Arrow | 2 Wood + 2 Stone | 20 | grey stone |
  | Copper Arrow | 4 Stone Arrows + 1 Copper | 25 | copper |
  | Iron Arrow | 4 Copper Arrows + 1 Iron | 30 | steel |
  | Gold Arrow | 4 Iron Arrows + 1 Gold | 40 | gold |
  | Diamond Arrow | 4 Gold Arrows + 1 Diamond | 50 | ice blue |

  (A Wooden Sword hits for 25.) Arrows go into the inventory, not the hotbar.
- **Shooting:** select the bow, **hold the left mouse button** to draw (the character takes an arrow and draws; a ring
  around the crosshair fills, gold when fully drawn after 1 second) and **let go** to shoot. The arrow flies where the
  crosshair points and falls a little on the way, so aim a bit higher far away.
  - Half drawn = half the damage and a slower, lower arrow. Let go within a quarter second and nothing is shot (no
    arrow used).
  - **Stamina** (22/11/2026): holding the bow drawn tires the arms. Stamina drains **4 a second** while the button is
    held (sprinting drains 20, out of 100), and the shot itself is free, so a quick shot costs about 4 and aiming for a
    long time costs more. It fills again after a second's rest. If the bar runs out mid-draw, the string is let go
    without shooting (the arrow stays in the inventory) and "Too tired to hold the bow! Rest a moment." shows; like
    sprinting, you can draw again once stamina is back to 20.
  - The bow shoots your **strongest** arrows first (under the crosshair: the arrow and how many you have).
  - It's a **projectile**: the damage happens where the arrow lands, not with the melee ray. A turtle in its shell is
    hurt too (like the fireball), and a monster can still dodge (Marko's 5 %).
  - Hits show their damage number. Only a **fully drawn** shot can be a **critical hit** (Critical Strike perk: double
    damage, a big orange number). The **Vampire** perk heals from arrow hits too; **Night Power**, **Long Reach** and
    **Keen Eye** are melee perks and don't change arrows.
  - An arrow that hits a monster or an animal is **used up**: no arrow stays in its skin. One that misses sticks where
    it landed (ground, tree, rock) and disappears after 20 seconds. Animals can be hunted too.
  - **Arrow Recovery** perk (epic and legendary boxes): your arrows can be had back. One in a monster or an animal stays
    in it: walk within 2.5 m to take it back. When that creature dies its arrows fall out onto the ground. A missed
    arrow stays where it landed (one in a tree or a rock falls out when that breaks): walk within 1.6 m to pick it up.
    Each one is "+1 Stone Arrow" (or whichever kind) in the inventory; arrows to pick up stay 2 minutes.
  - No arrows: "No arrows! Craft some: 2 Wood + 2 Stone = 4 Stone Arrows".
- **Holding the bow:** in first person it's up in front of you, ready, at the bottom right (a moment of Mixamo's
  Shooting Arrow); drawing brings it to the middle with the arrow at your cheek. Held fully drawn, the character
  breathes (BotStandingAimIdle01); hold it longer than 2.5 s and they pull the string further back
  (BotStandingAimOverdraw). After the shot the bow comes back up to the ready pose. In third person the bow stays in the
  left hand while you walk, like the other tools, and the body draws, holds and shoots the same way.

## Quick way: one click

1. Have GameLair's **Medieval Fantasy Weapon Pack** in `Assets/Imported3D/GameLair` and Mixamo's **Shooting Arrow** as
   `Assets/Imported3D/Mixamo/BotShootingArrow.fbx` (FBX for Unity, Without Skin, 30 fps). Optional, in the same folder:
   Mixamo's **Standing Aim Idle 01** (`BotStandingAimIdle01.fbx`: the breathing hold) and **Standing Aim Overdraw**
   (`BotStandingAimOverdraw.fbx`: the longer pull).
2. (Optional) To use **BowTwo** or **BowThree** instead of BowOne, select it in the Project window.
3. Menu **Tools > Items > Build Bow And Arrows**, then **Build**. It saves your open scenes first, and sets up
   GameScene and ProceduralGenerateScene (and saves them).

Run it again any time: everything is made again in the same files (the arrows' recipes and damage are set again too;
the head materials and the flying arrow's numbers are kept). After rebuilding the character (Tools > Player > Add
Character Model), run **Tools > Player > Set Up Player Animations** (it makes the bow's animation states again).

## What it makes

| What | Where | Notes |
|---|---|---|
| Wooden Bow | `Assets/Config/Items/Tools/WoodenBow` + `Assets/Prefabs/Item/Tools/WoodenBow` | Item Type **Tool**, Kind **Bow**, Tier Wood. The prefab's `Model` has the bow upright with its grip at the middle, shooting along x (ToolInHand puts that in the left fist). |
| 5 arrows | `Assets/Config/Items/Arrows/<Tier>Arrow` + `Assets/Prefabs/Item/Arrows/<Tier>Arrow` | Item Type **Ammo** (its Ammo section: **Damage**, **Head**). |
| Arrow mesh | `Assets/Prefabs/Item/Arrows/Meshes/ArrowOne Split` | ArrowOne copied with its **head as a second part**: the faces at the pointed end in the tip's color on the pack's color sheet. Part 0 keeps the pack's colors, part 1 gets the tier's head. |
| Head colors | `Assets/Materials/Arrows/<Tier> Arrow Head` | Standard materials, made once: change their Color / Metallic / Smoothness by hand. |
| Flying arrow | `Assets/Prefabs/Item/Arrows/Arrow Projectile` | ArrowProjectile, with a `Look` child (forward = the way it points, the tip at its middle). |
| Bow animation | `BotShootingArrow` cut into **BowDraw**, **BowAim**, **BowRelease**, **BowReady**, **BowReadyDraw**; `BotStandingAimIdle01` = the hold (looping), `BotStandingAimOverdraw` = the longer pull | Done by Set Up Player Animations' file part (measured on the character on every run, cut again only when that changes): both controllers get BowDraw / BowAim / BowOverdraw / BowRelease; the first-person hands also get **BowIdle** (the ready pose) and the **BowHeld** bool. |
| Draw ring | `Assets/Prefabs/UI/Bow/BowCanvas` (BowHUD) + `Assets/UI/Bow/DrawRing.png` | One copy in each game scene. |
| On the Player | BowShooter (Arrow Prefab set), ToolTestKit (the bow + the arrows) | Each game scene, saved. |
| Crafting list | Turki's CraftingCanvas | The bow after the Wooden tools, the arrows after all tools (log `Docs/TonyEditMate/Turki/22-11-2026 Bow And Arrows In The Crafting List.md`). |

## How it works

| Part | Where | What it does |
|---|---|---|
| **BowShooter** | Player | Hold KeyBindings' Swing (left mouse) with a bow in the hand to draw, let go to shoot. Picks the arrow with the most Damage, takes one from the inventory, works out where the crosshair points and launches an ArrowProjectile from the bow (first person: in front of the eyes; third person: the body's left hand). Damage = arrow Damage x draw (0 - 1) x the bow's tier bonus (25 % per tier above Wood); fully drawn = a Critical Strike roll. Not while a menu is open or a building is being placed; switching the slot or dying cancels. |
| **ArrowProjectile** | the flying arrow | Flies with its own gravity, sweeps a thin sphere along each frame's stretch, hurts a monster (EnemyCombat) or an animal (ResourceNode, Animal) it hits (Vampire, damage number, a small push) and is then used up; a miss sticks where it landed. With the Arrow Recovery perk (`PerkEffects.ArrowRecovery`) it stays in the creature, falls out when that dies (checked after every Update, before the body is removed) and goes back into the inventory when the player comes close. |
| **ToolInHand** | the character (body and first-person hands) | A bow sits in the **left** fist (HandGrip measures the left hand); while drawn, an arrow of the kind it will shoot lies on the string, from the right hand to the bow. |
| **PlayerAnimator** | the character | Three actions: **BowDraw** (draw, then hold at full draw: breathing, then the overdraw after 2.5 s), **BowRelease**, **BowCancel**. No flinch while drawing. |
| **FirstPersonHands** | Main Camera > FirstPersonHands | With a bow in the hand the hands show where the character really holds them (the archer's own view) and it switches its controller's **BowHeld** bool on: the ready pose while only held, then the draw and the hold. (A hands controller made before 22/11/2026, without BowIdle, still hides the hands until you draw: run the tool once.) |
| **BowHUD** | BowCanvas | The ring (fills while drawing, gold when full) and the arrow + count under the crosshair. |
| **PlayerCombat** (Marko's) | Player | One `[Tony]` line: no punch while a bow is in the hand (log `Docs/TonyEditMate/Marko/22-11-2026 Bow Shoots Instead Of Punching.md`). |
| **Crosshair** | HUD | With a bow, the Keen Eye perk's yellow "in reach" color is off (a bow doesn't hit up close). |

## Settings

| Where | Setting | What it does |
|---|---|---|
| Player > BowShooter | **Draw Seconds** | seconds to full draw (1). The draw animation is timed to it: run Set Up Player Animations after changing it |
| | **Min Draw Seconds** | shorter = no shot, no arrow used (0.25) |
| | **Stamina Per Second** | stamina used per second while the bow is held drawn (4; sprinting 20, out of 100). 0 = free |
| | **Tired Text** | the message when stamina runs out mid-draw (empty = no message) |
| | **Full Speed**, **Slowest Share** | arrow speed at full draw (45 m/s) and at the least draw (35 % of it) |
| | **Aim Distance** | how far the crosshair picks a target (250 m) |
| | **No Arrows Text** | the message with no arrows |
| Arrow Projectile prefab | **Gravity** | how fast it falls (6; real gravity 9.8) |
| | **Hit Radius** | how thick it is for hitting (0.05 m) |
| | **Knockback**, **Knockback Seconds** | the push on a hit monster (1.5 m/s for 0.15 s) |
| | **Stick Depth**, **Stuck Seconds**, **Most Stuck** | how deep it sticks, how long it stays (20 s), how many can stay (60) |
| | **Take Back Distance**, **Pick Up Distance**, **Pick Up Seconds** | Arrow Recovery: how close you take an arrow back from a creature (2.5 m) or pick one up from the ground (1.6 m), and how long an arrow to pick up stays (120 s) |
| | **Misses For Everyone** | on = missed arrows can be picked up even without the perk (Minecraft-style); off by default |
| Each arrow's Item Data | **Ammo > Damage**, **Ammo > Head** | its damage at full draw, its head material (the tool sets them again when it runs) |
| Character > ToolInHand | **Bow Size**, **Bow Nudge**, **Bow Turn**, **Arrow Size** | the bow in the left fist and the arrow on the string (change while playing, then Copy / Paste Component) |
| FirstPersonHands | **Aim Blend Speed** | how fast the hands move to the archer's view when a bow is taken in hand (8) |
| PlayerAnimator (code) | `BowOverdrawAfter` | seconds held at full draw before the overdraw starts (2.5); run Set Up Player Animations after changing it |
| BowCanvas > BowHUD | colors, **No Arrows Text**, **Fade Speed** | the ring and the count |
| GameRules > Tool Test | | gives the bow and tops each arrow up to 32 (ToolTestKit > **Arrows Each**) |

## By hand (instead of the tool)

1. **Bow item:** Assets/Config/Items/Tools > right click > Create > Items > Item Data `WoodenBow`. Item Type **Tool**,
   Tool > Kind **Bow**, Tier **Wood**, Display Name "Wooden Bow", Recipe 10 Wood + 10 Plank.
2. **Bow prefab:** an empty `WoodenBow` with a BoxCollider and ItemPickup (Data = WoodenBow); a child `Model` with a
   child holding the pack's bow mesh (MeshFilter + MeshRenderer with WeaponMaterial), placed so the bow's grip is at
   `Model`'s middle, the bow standing up (y) and shooting along `Model`'s x. Drag the prefab into the Item Data's Prefab.
3. **Arrows:** for each tier an Item Data (Item Type **Ammo**, Damage, Head = a Standard material in the head's color,
   Craft Amount 4, the recipe) and a prefab like the bow's with the arrow model. To color only the head, the arrow mesh
   needs the head as its own part (the tool does that); with the pack's mesh as it is, Head changes nothing.
4. **Flying arrow:** an empty `Arrow Projectile` with **ArrowProjectile**, a child `Look` (forward = the way the arrow
   points, its tip at Look's middle) with the arrow model under it; drag Look into ArrowProjectile's Look and set Length.
5. **Animation:** Tools > Player > Set Up Player Animations (cuts BotShootingArrow, adds the bow states).
6. **Player:** add **BowShooter**, drag Arrow Projectile into Arrow Prefab. Add the bow to Turki's crafting list
   (CraftingCanvas > CraftingMenu > Craftables) or run Tools > UI > Build Crafting Menu.
7. **Ring:** a Canvas with **BowHUD** and children named Bow (CanvasGroup), DrawRing (Image, Filled, Radial 360),
   ArrowIcon (Image) and ArrowCount (TextMeshPro).

## Testing

1. Tick **GameRules > Tool Test** (or NEW GAME > Testing > Start with all tools), Play GameScene: a Wooden Bow and 32
   of each arrow.
2. Select the bow: under the crosshair the Diamond Arrow and 32. Hold the left mouse button: the character draws, the
   ring fills and turns gold; let go: the arrow flies and sticks where the crosshair was. A slime loses 50 (a white
   number), or 100 with a critical (Critical Strike perk).
3. Tap the button quickly: nothing is shot, the count stays.
4. Drop the Diamond Arrows: the Gold ones are used next.
5. F5 for first person: holding the bow shows it up in front, ready (bottom right); drawing brings it to the middle
   with the arrow; held fully drawn it breathes, and after 2.5 s the string goes further back.
6. Arrow Recovery: Player > PerkEffects ⋮ > **Test: Every New Perk Once** (or buy it from an epic box). Shoot a deer:
   the arrow stays in it; walk up to it: "+1 Diamond Arrow". Shoot the ground and walk over the arrow: the same. Kill a
   slime with arrows in it: they fall out onto the ground. Without the perk, an arrow that hits leaves nothing behind.
6. Crafting page > Tools: the Wooden Bow after the Wooden Sword, then the five arrows.

## Problems

| Problem | Why / fix |
|---|---|
| Clicking punches instead of shooting | Marko's PlayerCombat lacks the `[Tony]` line (see the log), or the item isn't a Tool of Kind Bow. |
| The bow doesn't draw, "No arrows!" | No arrows in the inventory: craft Stone Arrows (2 Wood + 2 Stone). |
| The hands are hidden while holding the bow (first person) | The hands controller has no ready pose yet: run **Tools > Items > Build Bow And Arrows** (or Tools > Player > Set Up Player Animations) once. |
| It draws but nothing flies | BowShooter has no **Arrow Prefab** (the Console says so, and the tool's report says "Arrow Prefab MISSING"). The tool's first version (22/11/2026) left it empty in both scenes; run the tool again (it fills it now), or drag `Assets/Prefabs/Item/Arrows/Arrow Projectile` into the Player's **BowShooter > Arrow Prefab** and save the scene. |
| A pickaxe swing or the flinch shows a T-pose | The tool's first version also changed Blink's `GetHit` and `MiningLoop` animation files (see Mixamo Animations.md, Problems). Fixed: it only uses Mixamo files now. |
| The character doesn't move its arms | The controllers have no bow states: run **Tools > Player > Set Up Player Animations** (BotShootingArrow must be in Assets/Imported3D/Mixamo). |
| The draw animation is too fast / slow | Change BowShooter's **Draw Seconds**, then run Set Up Player Animations again. |
| The bow sits wrong in the hand | Tune ToolInHand's **Bow Nudge / Bow Turn / Bow Size** while playing (body: Character, first person: FirstPersonHands), Copy Component, stop, Paste Component Values. |
| The whole arrow is recolored, or none of it | The head is found by the tip's color on the pack's color sheet; the Console line "head = N of M faces (by its color)" says how many faces it took and how it found them. With another arrow model, check that its tip is the thinner end. |
| The bow and arrows are pink | The pack's WeaponMaterial wasn't found (or an URP extract was imported): the pack's own material is Standard. |
