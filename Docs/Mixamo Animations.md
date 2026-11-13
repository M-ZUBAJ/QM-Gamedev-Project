# Mixamo Animations (punch combo, flinch, tool swing)

**What the player sees** with nothing in the hand (or wood, food, anything that isn't a tool):
- **Left click:** a **left jab**. Left click again: a **right jab**, then left, right...
- **Right click:** a **heavy left hook**. Right click again: a **heavy right punch**, then left hook, heavy punch...
  - While food is in the hand, right click eats it instead.
- **After a 2 s pause**, each click starts its line again from its first punch.

**About heavy punches:**
- They hit twice as hard and cost twice the stamina.
- They make a bigger wind burst.
- Every punch lands when its fist does.

**When the player is hurt,** the body flinches with Mixamo's **Get Hit Body**.

**With a tool in the hand:**
- **Left click:** every tool (sword, axe, pickaxe) swings with Mixamo's **Axe Attack Downward**, a chop from above.
- **Selecting a tool** shows it in the fist at once, with no animation. The Unarmed Equip clips were tried and dropped
  on 13/11/2026; Set Up Player Animations removes their states.

All the animations come from **Mixamo** (free, mixamo.com). They play on Blink's character because both are Humanoid.

## 1. Download from Mixamo

1. Search mixamo.com for an animation, e.g. "Punching". Pick one and tick **In Place** if the box is there.
2. Click **Download** and choose:

| Setting | Choose |
|---|---|
| Format | **FBX for Unity (.fbx)** |
| Skin | **Without Skin** (only the movement: our character is Blink's) |
| Frames per Second | **30** |
| Keyframe Reduction | **none** |

3. Put the `.fbx` file in `Assets/Imported3D/Mixamo` (with the other imported 3D files; `Assets/Animation` works too).

**What each file plays** (picked from a word in its file name):

| The file name contains | Plays | Our files |
|---|---|---|
| `PunchingLeft` | the left jab (`PunchLeft`) | BotPunchingLeft |
| `PunchingRight` | the right jab (`Punch`) | BotPunchingRight |
| `LeftHook` | the heavy left hook (`HeavyHook`, right click) | BotHeavyLeftHook (BotHeavyUppercutLeft is no longer used) |
| `HeavyPunch` | the heavy right punch (`HeavyPunch`, right click) | BotHeavyPunchRight |
| `GetHit` | the flinch when hurt (`Hit`, body only) | BotGetHitBody |
| `AxeAttack` | every tool's swing: sword and axe (`Attack`), pickaxe (`Mine`) | BotAxeAttackDownward |
| `Mining` | a pickaxe-only swing (`Mine`): wins over AxeAttack | none yet |
| `Slash` or `SwordSwing` | a sword / axe-only swing (`Attack`): wins over AxeAttack | none yet |

One file can play several actions (AxeAttack plays both Attack and Mine). A file made for one tool comes earlier in
the tool's table, so adding e.g. a mining file later gives the pickaxe its own swing and leaves the others as they are.

## 2. One click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Player > Set Up Player Animations** (the one tool for the player's animations). For each file it:
   - sets the import to **Humanoid** (Avatar: Create From This Model);
   - names its clip after the file (Mixamo calls every clip "mixamo.com");
   - makes it play once, with the root motion baked in so the body stays on the ball;
   - puts the clip into the body's controller (`PlayerCharacter`, Actions layer) and the first-person hands'
     (`FirstPersonHands`). New actions get their state, trigger and arrows.
   - gives every action in both controllers the same timing, so your shadow punches together with the hands;
   - cuts the first-person arms at the elbow and draws them from both sides (no open shoulder, no holes).
3. On the Player it also:
   - adds **PunchCombo**, and takes off components whose script was deleted (e.g. the old ToolEquip);
   - measures when each fist lands from the clips, and writes it into **PunchEffect** (Impact / Left Jab / Left Hook /
     Heavy Punch Delay).
4. Save the scene (**Ctrl + S**).

Run it again after adding more files, or after rebuilding the character. The character builders also pick the Mixamo
clips by themselves when they find them, and Blink's when not.

## Test

1. Press Play with an empty hand and look at a tree or stone in first person.
2. Left click a few times: left, right, left jabs. The shadow on the ground punches with the hands.
3. Right click: a left hook and a big burst. Right click again: a heavy right punch. The stamina bar drops twice as
   much.
4. On the Player's PlayerHealth, click the ⋮ menu > **Test: Take Damage**: the body flinches.

## Settings (PunchCombo, on the Player)

| Field | What it does |
|---|---|
| **Heavy Damage** (2) | A heavy punch hits this many times harder than a jab. |
| **Heavy Stamina** (2) | A heavy punch costs this many times a jab's stamina. |
| **Combo Reset Seconds** (2) | A pause this long starts each line again: the jabs with the left, the heavies with the left hook (0 = never). |

On **PunchEffect**:
- **Left Jab / Left Hook / Heavy Punch Delay:** when each punch lands. They're measured by the tool, and you can change
  them while testing.
- **Heavy Burst Size** (1.5): how much bigger a heavy punch's burst is.

**Speed:** jabs play twice as fast as the clip (`PlayerAnimator.ActionSpeed`), heavy punches 1.5 times
(`PlayerAnimator.HeavySpeed`), and the flinch as it was made.

## By hand (one file)

1. **The file:** select it in the Project window.
   1. **Rig** tab: Animation Type **Humanoid**, Avatar Definition **Create From This Model**, then **Apply**.
   2. **Animation** tab: rename the clip to the file's name.
   3. Untick **Loop Time**.
   4. Tick **Bake Into Pose** for Root Transform Rotation, Position (Y) and Position (XZ). Click **Apply**.
2. **The body:** open `Assets/Animation/Player/PlayerCharacter.controller` and go to the **Actions** layer.
   - **An existing action** (Punch, Hit): drag the clip into its state's **Motion**.
   - **A new action** (e.g. PunchLeft):
     1. Add a Trigger parameter `PunchLeft`.
     2. Add a state `PunchLeft` with the clip, Speed 2.
     3. Add **Any State → PunchLeft**: condition PunchLeft + Dead false, no exit time, Duration 0.05, Can Transition To
        Self ticked.
     4. Add **PunchLeft → Empty**: Exit Time 0.85, Duration 0.15.
3. **The hands:** do the same in `FirstPersonHands.controller` (base layer, back to **Idle**, no Dead condition). Skip
   Hit there.
4. **The Player:** Add Component > **PunchCombo**.

## Problems

| Problem | Why / fix |
|---|---|
| "couldn't be made Humanoid" in the Console | Mixamo sometimes needs it by hand: Rig > Humanoid > Create From This Model > **Configure...**, check the bones are green, then Apply and run the tool again. |
| The character slides forward and snaps back | **In Place** wasn't ticked on Mixamo. Download it again with In Place, or the tool's Bake Into Pose will keep the body still. |
| A click does nothing | Not during the 0.5 s attack cooldown, and not with no stamina left. With a tool in the hand, left click swings the tool instead. |
| Right click doesn't punch | While food is in the hand, right click eats it. Something levitating also keeps right click for itself. |
| The burst comes before / after the fist | Change that punch's delay on PunchEffect, or run the tool again to measure it. |
| The shadow punches late | Run **Tools > Player > Set Up Player Animations** again. |
