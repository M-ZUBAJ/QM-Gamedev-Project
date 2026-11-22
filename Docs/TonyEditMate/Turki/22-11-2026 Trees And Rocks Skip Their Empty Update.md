# Tony's change to Turki's ResourceNode, 22/11/2026: trees and rocks skip their empty Update

**Why:** Tony reported that the game, most of all the new world, gets laggy. Your `ResourceNode.Update` is only needed
by berry bushes (Collect With Key: it watches for the E key). For every other node it returns at once, but Unity still
calls it every frame: the new world shows about 2000 trees, rocks and bushes near the player (a full grassland chunk
alone has about 180 trees), so those empty calls added up every frame (in the Editor most of all).

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Gathering/ResourceNode.cs` | **Added** one line at the end of `Start()`, marked `[Tony] added`: `if (!collectWithKey) enabled = false;` After its health is set, a node that isn't collected with the key switches the component off, which only stops Unity calling its Update. |

Everything else works as before: `TakeDamage` (punches, tools, arrows, the fireball), `Break`, the drops, the bonus
drop and Tony's Lumberjack / Prospector lines don't need Update, and scripts that find your component
(`GetComponentInParent<ResourceNode>()`) still find it. Berry bushes keep their Update and are picked with E as before.
While playing, the ResourceNode on a tree or a rock shows unticked in the Inspector: that's this line, not a problem.

## How to undo

In `ResourceNode.cs`: delete the line marked `[Tony] added` (and its comment) at the end of `Start()`.
