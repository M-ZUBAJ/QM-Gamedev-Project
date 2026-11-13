# World Spawning Setup (trees, rocks, bushes, deer, line of sight)

How the world fills itself:

- **Trees, rocks, berry bushes and chests** are placed once, when the game starts, by Sanyam's **ObjectScatter**.
- **Deer** are kept around the player by Tony's **AnimalSpawner**. It only makes a new deer where the player can't see
  it, like Minecraft spawning animals out of view.
- **PlayerSight** on the Player decides what "can see" means. It has an Inspector you can tune, and perks or buffs can
  make it see farther.

## What each part does

**PlayerSight** (on the Player, `Assets/Script/Player/PlayerSight.cs`):
- **Seen** means all three: closer than **Sight Range** (80 m), on the screen (a little past the edges counts too), and
  not hidden behind a hill, rock, tree trunk or wall.
- **Notice Radius** (20 m): anything closer always counts as seen, even behind you. So a deer never appears right next
  to you.
- **Perks and buffs:** a perk calls `PlayerSight.Current.AddRange(20f)` (until you die), a buff calls
  `PlayerSight.Current.AddRange(40f, 30f)` (for 30 seconds). Everything that asks PlayerSight follows the new range at
  once.

**AnimalSpawner** (on its own object, `Assets/Script/Animal/AnimalSpawner.cs`):
- **When the game starts:** **Population** (15) deer come in small herds of 1 to 3, all around the player and all out
  of sight.
- **Where:** between **Min Distance** (30 m) and **Max Distance** (150 m) from the player, on the grass, not on steep
  slopes, not inside a tree or rock.
- **Sight perks:** the spawn reach is never smaller than the sight range + **Beyond Sight** (40 m). When a perk makes
  you see 200 m, new deer appear up to 240 m away by themselves. Nothing to change.
- **Every 10 seconds** (**Spawn Every Seconds**): a hunted deer is replaced by a new herd, out of sight. Deer left
  more than **Despawn Distance** (250 m) behind, and out of sight, are taken away. So the herds follow you around the map.

**ObjectScatter** (Sanyam's, on a scene object), with Tony's new settings on each rule (lines marked `[Tony]`):
- **Min Spacing** now counts only between things of the same **Spacing Group**. All tree looks share the group
  "Trees", so trees stay 10 m apart. Empty group = the prefab's name.
- **Clearance** (3 m): how far it stays from other kinds. A rock isn't put inside a tree trunk, but 1000 trees still
  leave room for rocks and bushes. Before this, every rule kept its Min Spacing from everything, so after 1000 trees the
  bushes, rocks and deer found almost no spot.
- **Patch Size / Open Ground:** the trees grow in forests about 120 m across, with meadows between them (about a third
  of the grass stays open).
- **Min / Max Size:** each tree gets a random size from 0.85 to 1.2, and each rock from 0.7 to 1.3.
- **Deer:** they aren't in its list any more; the AnimalSpawner has them.

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > World > Set Up World Spawning**. It:
   - fills the ObjectScatter's new settings (Spacing Group "Trees" + forest patches + sizes for the tree rules, sizes
     for the rock rules, Clearance 3 for every rule). Your **Counts** are not changed;
   - moves the **Animals** rule out of the ObjectScatter into a new object **AnimalSpawner - DeerNode**, with the
     same ground settings;
   - adds **PlayerSight** to the Player.
3. Save the scene (**Ctrl + S**). **Ctrl + Z** undoes the tool.
4. Play. The Console lists each rule ("Trees: Oak: spawned 500 of 500", "Berry Bushes: spawned 150 of 150"...) and
   "AnimalSpawner - DeerNode: 15 DeerNode around the player, all out of sight".

Run the tool again any time: it only fills settings that are still empty.

## By hand (instead of the tool)

1. **Line of sight:** select the **Player** > **Add Component** > **PlayerSight**.
2. **The deer spawner:**
   1. **GameObject > Create Empty**, name it `AnimalSpawner - DeerNode`.
   2. **Add Component** > **AnimalSpawner**.
   3. Drag `Assets/Prefabs/ResourceNodes/DeerNode` into **Animal Prefab**, and the Terrain into **Terrain**.
3. **ObjectScatter:** select its object. In **Rules**:
   1. Remove the **Animals** rule (right-click it > **Delete Array Element**), or the deer come twice.
   2. On every `Trees: ...` rule: **Spacing Group** `Trees`, **Patch Size** 120, **Open Ground** 0.35, **Min Size**
      0.85, **Max Size** 1.2.
   3. On **Forest Rocks** and **Desert Rocks**: **Min Size** 0.7, **Max Size** 1.3.
   4. On every rule: **Clearance** 3.
4. Save the scene.

## Testing

1. **Deer appear out of sight:** Play, stand still and look around: no deer pops up on screen. Walk away more than
   250 m: the old deer are taken away behind you and new ones appear ahead, out of view.
2. **A sight perk moves the spawns:** select the Player > PlayerSight > **⋮** > **Test: Sight +40 m For 20 s**. The
   Console says "Player sight range: 120 m". Select the AnimalSpawner: its outer yellow circle grows.
3. **Forests and meadows:** look at the map from above (Scene view while playing): patches of trees, open grass
   between them, rocks and bushes in both.

## Settings

| Where | Setting | What it does |
|---|---|---|
| PlayerSight | **Sight Range** (80) | How far the player sees things, in meters. Perks add to it. |
| | **Notice Radius** (20) | Closer than this always counts as seen. |
| | **Only On Screen**, **Screen Margin** (0.15) | Behind you is unseen. The margin stops things popping up at the edge while you turn. |
| | **Walls Block Sight**, **Blocking Layers** | Hills, rocks, trunks and walls hide what's behind them. Leaves don't (no collider). |
| | **Reset On Death** | Sight perks and buffs end when you die, like the other perks. |
| AnimalSpawner | **Population** (15), **Herd Min / Max** (1, 3) | How many deer live around you, and how many come together. |
| | **Min / Max Distance** (30, 150), **Beyond Sight** (40) | Where new deer may appear. The reach grows with your sight. |
| | **Despawn Distance** (250) | Deer farther away and out of sight are taken away. |
| | **Terrain Layer**, **Min Strength**, **Max Slope** | The ground they like: grass (layer 0), not steep. |
| | **Clearance** (1.5), **Herd Spacing** (25) | Room around a new deer, and distance between herds. |
| ObjectScatter rule | **Min Spacing** | Meters from things of the same Spacing Group. |
| | **Spacing Group** | Rules with the same text keep Min Spacing from each other. Empty = the prefab's name. |
| | **Clearance** (3) | Meters from things of other groups. |
| | **Patch Size** (0 = even), **Open Ground** (0.35) | Forests and meadows. A bigger Patch Size = bigger forests. |
| | **Min / Max Size** | Random size range (1, 1 = all the same). |

**Another animal** (e.g. a horse from **Tools > World > Build Wild Animal**): make a second AnimalSpawner, or drag
`HorseNode` into this one's **Animal Prefab**.

## Problems

| Problem | Why / fix |
|---|---|
| "the Player has no PlayerSight, so no animals spawn" | Add PlayerSight to the Player (step 1 by hand), or run the tool. |
| Fewer deer than Population | No free spot out of sight this time (forest, steep hills, water). It tries again every 10 seconds. Lower **Clearance** or **Herd Spacing**, or raise **Max Distance**. |
| A deer appears where I'm looking | It came out from behind a tree's leaves (they don't block sight), or Walls Block Sight is off. Raise **Screen Margin** or **Notice Radius**. |
| Deer come twice as often | The ObjectScatter still has its Animals rule. Remove it (by hand step 3.1). |
| "Berry Bushes: spawned 8 of 150" in the Console | The rules don't have their new settings yet (Clearance 0 and no Spacing Group): run the tool. |
| The forests are too thin / too thick | Change **Open Ground** on the tree rules (lower = more forest), or their **Counts**. |
| The game takes long to start | 1000+ trees are placed in the first frame. Lower the tree Counts. |

Sanyam's change log: `Docs/TonyEditMate/Sanyam/13-11-2026 Scatter Spacing Groups And Forest Patches.md`.
