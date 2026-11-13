# Player Character Setup (an animated body for the player)

**What the player sees:** instead of the red ball, a low-poly person (Blink's free human, wearing starter clothes).
It stands, runs (forwards, backwards and sideways), sprints, jumps, falls off edges, flinches when hurt and falls
down when dead. In first person (F5) the body is hidden, but its shadow stays.

**How it works:** the ball is still the player. Its collider and scripts do all the moving, jumping and bumping
into things. The character is a look-only child of the ball (no collider): 1.8 m tall, standing on the bottom of the
ball. **PlayerAnimator** watches the ball (speed, ground, jump, health) and the camera, and tells the character's
Animator what to play.

## What you need

Blink's **FREE Low Poly Human - RPG Character** from the Asset Store, imported into `Assets/Imported3D/Blink/`
(**Window > Package Manager > My Assets** > the pack > **Download** > **Import**).
It has the character and 32 Humanoid animations (in `Assets/Imported3D/Blink/Art/Animations/Animations_Starter_Pack`).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Player > Add Character Model**.
   - It makes `Assets/Animation/Player/PlayerCharacter.controller` (which animation plays when) and `UpperBody.mask`.
   - It makes `Assets/Prefabs/Player/PlayerCharacter.prefab`: a **Prefab Variant** of Blink's character, so Blink's
     own files never change. It wears Blink's starter clothes (the original only wears underwear).
   - It puts that on the **Player** as **Character**, 1.8 m tall with its feet on the bottom of the ball, and switches
     off the ball's **Mesh Renderer** (the ball's look; its collider and scripts keep working).
   - If the prefab or the controller already exists, it asks: **Replace** (build both again) or **Keep** (use them as
     they are, e.g. after changing the clothes). **Ctrl + Z** undoes the scene changes.
   - Another character instead of Blink's: select its prefab or model in the Project window first. It must be
     Humanoid (see the problems table at the bottom).
3. Save the scene (**Ctrl + S**) and press Play.

```
Player                 the ball: SphereCollider, Rigidbody, every player script (Mesh Renderer switched off)
  Character            PlayerCharacter prefab: Animator (PlayerCharacter controller) + PlayerAnimator
    Armors, Mesh...    Blink's clothes, body parts and bones
```

## Settings (Character > PlayerAnimator)

| Field | What it does |
|---|---|
| **Facing** | **Camera Direction** (default): the body looks where you aim, like Minecraft, and runs sideways or backwards. **Move Direction**: the body turns to run the way it's going, like most adventure games. |
| **Turn Speed** | How fast the body turns, in degrees per second (720 = turning around in a quarter of a second). |
| **Blend Time** | Seconds to blend between standing, running and sprinting (0.1). |
| **Run Animation Speed** | How fast the running animations play (1). Raise it if the feet look like they slide. |
| **Fall Delay** | Seconds in the air before the falling animation starts (0.25), so small bumps don't count. |
| **Flinch When Hurt** / **Flinch Min Damage** | Flinch when losing at least this much health at once (5). Starving takes health slowly, so it doesn't flinch. |
| **Shadow Only In First Person** | In first person, hide the body but keep its shadow (on). |

All of them can be changed while playing.

## Change the look

Double click `Assets/Prefabs/Player/PlayerCharacter.prefab` to open it, then tick parts on or off in the Hierarchy:

- **Armors > StarterClothes**: Starter_Boots, Starter_Chest, Starter_Pants (on).
- **Armors > Plate1**: PlateSet1_Helmet, Chest, Shoulders, Gloves, Pants, Boots (off). Tick them on for plate armor
  (and switch the matching starter clothes off).
- **Mesh > Customization**: Hair, Beards, Moustaches, Face.

The Character in the scene updates by itself. Afterwards, if you run the tool again, choose **Keep**
(**Replace** builds the prefab again and loses these changes).

**Height:** select **Character** in the scene and change its **Scale** (all three numbers the same), then move it up
or down so the feet touch the ground. For every future build, change `Height` at the top of
`Assets/Script/Editor/PlayerCharacterBuilder.cs`.

## The animations it uses

| State | Clip from Blink's pack |
|---|---|
| **Locomotion** (blend by speed and direction) | Idle, RunForward, RunBackward, RunLeft, RunRight, RunBackwardLeft, RunBackwardRight, Sprint |
| **Jump** / **Falling** / **Dead** | Jump_Up / FallingLoop / Death |
| **Actions** layer (upper body only) | Hit = GetHit, Punch = PunchRight, Attack = MeleeAttack_OneHanded, Mine = MiningLoop, Gather = Gathering, Cast = SpellCast |

To swap one: double click the controller, click the state and drag another clip into its **Motion** (double click
**Locomotion** to see its blend tree). The clips are inside the FBX files: click the little arrow on an FBX to see them.
Not used yet (free for later): rolls, strafes, block, bow shot, buff, stunned, combat idle, two-handed attack,
punch left, jump while running.

## Play an action from any script (for teammates)

```csharp
PlayerAnimator.Play(PlayerAction.Attack);   // the upper body swings, the legs keep running
```

`PlayerAction` = `Hit`, `Punch`, `Attack`, `Mine`, `Gather`, `Cast`. It does nothing if there's no character, so it's
safe to call anywhere. **Hit** already plays by itself when the player gets hurt.

**Already called:** Marko's `PlayerCombat.Attack()` plays **Punch** for bare-handed attacks: nothing in hand, or not
the Sword / Axe / PickAxe (Tony's `[Tony]` lines, 10/11/2026).

**Where the others would fit (teammates' files, not changed yet):**
- **Attack:** sword / axe / pickaxe swings, in the same place in `PlayerCombat.Attack()`.
- **Cast:** Marko's `PlayerFireball.Update()`, right after the fireball's `Instantiate(...)`.
- **Mine:** Turki's harvesting.
- **Gather:** Turki's picking up.

**Punch combo and Mixamo animations:** with an empty hand, left click jabs left / right in turn and right click throws
heavy punches (left hook / heavy right) in turn. The clips come from Mixamo: see `Docs/Mixamo Animations.md`.

## First-person hands (Minecraft-style)

**What the player sees:**
- **In first person, whenever a click would punch:** your two fists in a fighting guard at the bottom of the screen,
  bobbing a little while you walk. A click punches when you hold nothing, or anything that isn't the Sword, Axe or
  PickAxe.
- **Left click:** the right fist punches. It stays visible even right against the tree, rock or enemy you hit.
- **They hide:** in third person, while you hold the Sword, Axe or PickAxe (they swing instead), and while you're dead.

**How it works:**
- **The copy:** a second copy of your character sits on the Main Camera with its head where the camera is. Only its
  arms and hands are drawn, with no shadow; your real character still casts the shadow.
- **The guard:** it stands in Blink's **IdleCombat** guard, so the fists are in view.
- **The punch:** when PlayerCombat plays the punch (`PlayerAnimator.Play`), **FirstPersonHands** plays it on the copy
  too.
  - **Same timing as your body:** both use `PlayerAnimator.ActionSpeed` (2 = twice as fast as the clip, so a click
    feels snappy) and the same quick blend.
  - **Every click starts it again,** so your shadow on the ground punches together with the hands.
- **Drawn on top:** the hands are on their own layer (30). The main camera stops drawing that layer, and a
  hands-only camera (**HandsCamera**, made when the game starts) draws them after the world. So the thing you punch
  can't hide your fist, and the fists can come close to the eyes without being cut off. The HUD still goes on top.
- **Forearms only, both sides:** the copy's arm meshes are cut at the elbow, so the open end at the shoulder never
  shows. Every face is also drawn from the back, so looking into a sleeve shows its inside, not a hole. These are copies
  in `Assets/Prefabs/Player/FirstPersonArms`, made by Add First Person Hands (and again by **Tools > Player > Set Up
  Player Animations**). Your real body is unchanged.

**Quick way:**
1. Run **Tools > Player > Add Character Model** first, if you haven't: the hands copy that character's look.
2. Open GameScene and use **Tools > Player > Add First Person Hands**. It makes
   `Assets/Animation/Player/FirstPersonHands.controller` and `Assets/Prefabs/Player/FirstPersonHands.prefab`, then
   puts that on the **Main Camera** as **FirstPersonHands**, the same size as your character. It asks before
   replacing anything.
3. Save the scene, press Play in first person, hold no tool and left click.

**Settings (Main Camera > FirstPersonHands > FirstPersonHands):**

| Field | What it does |
|---|---|
| **Fists On Screen** | Where the middle of the two fists sits on the screen: x 0 = left edge, 0.5 = middle, 1 = right edge; y 0 = bottom, 1 = top (0.3 = a little above the hotbar). The first time the hands show, the script measures where the fists are and moves them there, whatever the character's size, the camera's view or the window's shape. Change it **while playing**, then copy the component values and paste them after stopping. |
| **Nudge** | An extra small move in meters: x = right, y = up, z = forward (e.g. z 0.1 = a bit further away, so the fists look smaller). |
| **Shown Parts** | Only meshes whose name contains one of these words are drawn (Arm, Hand, Glove). |
| **Hide While Holding** | Item Ids that hide the hands while held. Empty by default: the hands always show, and a selected tool sits in the right fist (**ToolInHand**, see `Docs/Tool Setup.md`). |
| **Bob Amount** / **Bob Speed** | The bob while walking. |
| **Hands Layer** | The layer only the hands use (30). Nothing else may use it. 31 is taken by the item icons. |
| **Hands Near Clip** | How close to the eyes the hands camera still draws (0.01 m). The main camera's own near clip plane isn't changed. |

**By hand:**
1. **Controller:** make an Animator Controller **FirstPersonHands** with Triggers **Punch**, **Attack**, **Mine**,
   **Gather**, **Cast**.
   - Default state **Idle** = IdleCombat.
   - A state per trigger (Punch = PunchRight, Attack = MeleeAttack_OneHanded, Mine = MiningLoop, Gather = Gathering,
     Cast = SpellCast), each with **Speed 2** (the same as the body's Actions layer).
   - **Any State → it**: its trigger, no exit time, Duration 0.05, **Can Transition To Self** ticked.
   - **It → Idle**: Has Exit Time 0.85, Duration 0.15.
2. **Copy:** drag `PlayerCharacter.prefab` onto the **Main Camera** (it becomes its child) and name it
   **FirstPersonHands**.
   - Untick its **PlayerAnimator**.
   - On its Animator: Controller = FirstPersonHands, untick Apply Root Motion, Culling Mode **Always Animate**.
   - Add Component > **FirstPersonHands**.
3. **Size:** give it the same Scale as the Player's **Character**. The script moves it onto the camera when the
   game starts, and puts the fists on **Fists On Screen** the first time they show.

| Problem | Fix |
|---|---|
| No hands in first person | Check that the Main Camera has the FirstPersonHands child, and that its **Hide While Holding** list is empty (an id there hides the hands while held). |
| The punch disappears into the tree, rock or enemy I hit | Fixed by the hands camera. Check that **Hands Layer** (30) isn't used by anything else in the scene. |
| The fists are too high, low or hidden behind the hotbar | Change **Fists On Screen** while playing: a bigger Y = higher (0.35), a smaller Y = lower (0.25). |
| The fists look too big | Push them away with **Nudge** Z (e.g. 0.1). |
| The tool covers the view | Raise **Tool Lower** on FirstPersonHands (0.15 by default = 15 % of the screen's height lower while a tool is in the hand). Change it while playing. |
| The arms and hands are gone, only a tool floats (after Set Up Player Animations) | Until 13/11/2026 the tool re-saved the arm meshes without their corners. Fixed: run **Tools > Player > Set Up Player Animations** again and it rebuilds them. |
| Parts of the fists are cut off | Lower **Hands Near Clip** (e.g. 0.005). |
| I see the whole body or the inside of the head | Its **Shown Parts** words match too much. Keep only Arm, Hand and Glove. |
| It doesn't punch | Punches come from bare-handed attacks (Marko's PlayerCombat): empty hand, enough stamina, and not faster than the attack cooldown. |
| My shadow punches after the hands | **Why:** the body's controller was built before 12/11/2026. It played the punch at half the hands' speed, and a click during a punch had to wait for the old one to end (about 1.7 s).<br>**Fix:** run **Tools > Player > Set Up Player Animations**. It puts both controllers' timing right (running it again changes nothing). |
| The arms vanish while punching | Unity skipped drawing them: the stored box of an animated mesh is fixed to one bone and swings out of view when the punch twists the body. FirstPersonHands switches on **Update When Offscreen** for the arm and hand meshes when the game starts, so this is fixed. By hand: tick **Update When Offscreen** on the copy's Arms and Hands (Skinned Mesh Renderer). |
| An open shoulder, or see-through holes in the arm | Run **Tools > Player > Set Up Player Animations**: it cuts the arms at the elbow and draws them from both sides. To keep more or less of the arm, change `KeepWeight` at the top of `Assets/Script/Editor/FirstPersonArmsHelper.cs` (lower = more upper arm) and run it again. |

## By hand (instead of the tool)

1. **Controller:** in `Assets/Animation/Player`, right click > **Create > Animator Controller**, name it **PlayerCharacter**.
   Double click it to open the Animator window.
2. **Parameters** tab, click **+** for each (the names must be exactly these):
   Float **MoveX**, Float **MoveZ**, Float **RunSpeed** (default **1**), Bool **Falling**, Trigger **Jump**, Bool **Dead**,
   Triggers **Hit**, **Punch**, **Attack**, **Mine**, **Gather**, **Cast**.
3. **Locomotion:** in Base Layer, right click > **Create State > From New Blend Tree**, rename it **Locomotion** (orange =
   the starting state). Double click it, click the Blend Tree: **Blend Type 2D Freeform Directional**, parameters
   **MoveX** and **MoveZ**. Click **+ > Add Motion Field** 8 times and fill them in:

   | Motion | Pos X | Pos Y |
   |---|---|---|
   | Idle | 0 | 0 |
   | RunForward | 0 | 1 |
   | RunBackward | 0 | -1 |
   | RunLeft | -1 | 0 |
   | RunRight | 1 | 0 |
   | RunBackwardLeft | -0.7 | -0.7 |
   | RunBackwardRight | 0.7 | -0.7 |
   | Sprint | 0 | the Player's **Sprint Multiplier** (2.11) |

   Back in Base Layer, click Locomotion: under **Speed** tick **Parameter** and pick **RunSpeed**.
4. **More states** (right click > Create State > Empty, then drag the clip into Motion): **Jump** (Jump_Up, inside
   Jumps.fbx), **Falling** (FallingLoop), **Dead** (Death).
5. **Transitions** (right click a state > **Make Transition**, then click the other one). Click the arrow to set it:

   | From → To | Has Exit Time | Duration | Conditions |
   |---|---|---|---|
   | Any State → Jump | off | 0.05 | Jump, Dead = false |
   | Jump → Falling | on, Exit Time 0.9 | 0.1 | none |
   | Locomotion → Falling | off | 0.2 | Falling = true |
   | Falling → Locomotion | off | 0.15 | Falling = false |
   | Any State → Dead | off | 0.1 | Dead = true |
   | Dead → Locomotion | off | 0.2 | Dead = false |

   On the two **Any State** arrows, untick **Can Transition To Self** (under Settings).
6. **Upper body mask:** right click > **Create > Avatar Mask**, name it **UpperBody**. Under **Humanoid**, click the
   legs, the two foot dots and the circle under the feet so they turn red (= not used).
7. **Actions layer:** **Layers** tab, click **+**, name it **Actions**, click its gear: **Weight 1**, **Mask** UpperBody,
   **Blending Override**. In it, create an empty state named **Empty** (the starting state, no Motion), and the states
   **Hit**, **Punch**, **Attack**, **Mine**, **Gather**, **Cast** with their clips (table above).
   - Each state's **Speed**: 2 (`PlayerAnimator.ActionSpeed`, the same as the first-person hands), but Hit 1.
   - Any State → each: Has Exit Time off, Duration 0.05, **Can Transition To Self** ticked (a new click starts it
     again), conditions = its trigger (e.g. Attack) + Dead = false.
   - Each → Empty: Has Exit Time on, Exit Time 0.85, Duration 0.15, no conditions.
8. **Character:** drag `Assets/Imported3D/Blink/Art/Characters/LowPoly/FREE_HumanLowPoly/Prefabs_Humans/HumanMale_Character_FREE.prefab`
   onto **Player** in the Hierarchy (it becomes its child). Rename it **Character**.
   - Its **Animator**: Controller = **PlayerCharacter**, untick **Apply Root Motion**, Culling Mode **Always Animate**.
   - **Add Component > PlayerAnimator**.
   - Clothes (optional): tick on Armors > StarterClothes > Starter_Boots, Starter_Chest, Starter_Pants.
9. **Size and place:** set its **Scale** so it's about 1.8 m tall (a default Cube is 1 m), then move it down until its
   feet touch the bottom of the ball (the ball is 1 m wide, so its bottom is 0.5 below its middle).
10. Select **Player** and untick its **Mesh Renderer** (the red ball's look).
11. Optional: drag **Character** into `Assets/Prefabs/Player` and choose **Prefab Variant**.

## Test

1. Play: the character stands where the ball was. **W A S D** runs; **S** runs backwards (Camera Direction) or turns
   around (Move Direction); **Left Shift + W** sprints; **Space** jumps and lands; walking off a ledge falls.
2. **F5**: first person. The body disappears but its shadow stays. F5 again: third person.
3. On the Player's **PlayerHealth**, the ⋮ menu > **Test: Take Damage**: the body flinches.
   **Test: Die**: it falls down; after the respawn it stands up again.

| Problem | Fix |
|---|---|
| The character floats or sinks into the ground | Move **Character** up or down (Position Y) until the feet touch the bottom of the ball. |
| It stands with arms out (T-pose) and never moves. The Console may say "...has no Controller" or "Animator is not playing an AnimatorController" | Its Animator has no controller. Run **Tools > Player > Add Character Model** again, or select **Character** and drag `Assets/Animation/Player/PlayerCharacter.controller` into **Animator > Controller**. The model must also be Humanoid (next row). |
| The tool says "isn't a Humanoid character" | Click the model file (.fbx) > Inspector > **Rig > Animation Type = Humanoid** > **Apply**. A tick next to **Configure...** = it works. |
| It walks away from the ball | Untick **Apply Root Motion** on its Animator. |
| The feet slide over the ground | Raise PlayerAnimator > **Run Animation Speed**. |
| The Console says some animations weren't found | Blink's pack isn't imported, or another character uses other clip names. Drag clips into those states by hand. |
| It's pink | Its materials need the **Standard** shader (Blink's already use it). |
| In first person I see the inside of the head | Tick **Shadow Only In First Person**. |

## Good to know

- Only the ball (1 m) bumps into things, so the head (1.8 m) can poke through low ceilings or branches.
- The item in hand (HeldItemView) and Marko's weapon models still show in front of the camera, not in the
  character's hand.
