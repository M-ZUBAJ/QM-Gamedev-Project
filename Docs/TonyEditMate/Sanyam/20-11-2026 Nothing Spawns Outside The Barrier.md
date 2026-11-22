# Tony's change to Sanyam's ObjectScatter and old world, 20/11/2026: a barrier at the edge, nothing spawns outside it

**Files changed:**
- `Assets/Script/World/ObjectScatter.cs` (yours): 1 line added, marked `// [Tony] added`, plus a note in the comment at
  the top of the file.
- `Assets/Scenes/GameScene.unity` (your map): one new object, **World Barrier**, when Tony runs **Tools > World > Add
  World Barrier** (Tony's tool, `Assets/Script/Editor/WorldBarrierBuilder.cs`) with GameScene open and saves. Your
  `Boundaries` walls, terrain and spawn rules are **not** changed.

**Why:** Tony asked for a barrier at the edge of both worlds, "that tells the player by common sense that they can't
pass", instead of hills around the border. It's a see-through, glowing force field (Tony's `WorldBarrier`): faint
from afar, bright where the player comes near, solid. Two things in the old world:
- Your `Boundaries` (East, West, South and North Wall, around x 1000 - 2000, z 1000 - 2000): the West, South and North
  walls are **triggers**, so they don't stop anyone; only the East Wall (at x 1980) is solid. The barrier's own walls
  now stop the player on every side.
- ObjectScatter picks spots over the whole terrain, so a few trees, bushes and **chests** landed in the strips between
  the walls and the terrain's edge (5 m on three sides, 25 m past the East Wall), where nobody can reach them.

**What changed:**

| Where | What |
|---|---|
| `ObjectScatter.Scatter()`, after the patch check | `if (!WorldBarrier.Inside(pos, 2f)) continue;` = a new check **2c**: a spot less than 2 m inside the barrier (or outside it) is skipped, like a spot on the wrong texture. In a scene without a barrier `Inside` is always true, so nothing changes there. |
| GameScene (after Tony runs the tool) | **World Barrier** placed on the inner faces of your walls: x 1005 to 1975, z 1005 to 1995 (970 x 990 m, the area the player starts in), from 10 m under the lowest ground on that line to 60 m over the highest. Your walls stay where they are. |
| The new world (ProceduralGenerateScene) | Not your map, but it uses your ObjectScatter: its World Generator makes its own barrier, so your rules never spawn past it there either. |

**To undo:** delete the `[Tony] added` line (and its comment lines) in `ObjectScatter.cs`; select **World Barrier** in
GameScene and delete it (then save).

Guide: `Docs/World Barrier Setup.md`.
