# Tony's change to Turki's ResourceNode, 21/11/2026: the berry key comes from KeyBindings

**Why:** Tony asked for the title screen's **Settings > Key Bindings** page to show the key that picks berries and opens
chests. Your berry bush (a ResourceNode with **Collect With Key**) had its own `collectKey` field (E, "same key as the
chests"), so the page couldn't show it or change it. Now it reads `KeyBindings.Interact`, which is **E** by default,
listed on the page as **Open chest / Pick berries**: one key for both, as you meant, and changing it there changes it
for the bushes and Marko's chests together.

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Gathering/ResourceNode.cs` | (1) Your field `collectKey` and its Tooltip are commented out (`[Tony] replaced`), so they no longer show in the Inspector. (2) In `Update()`, `if (!Input.GetKeyDown(collectKey)) return;` became `if (!Input.GetKeyDown(KeyBindings.Interact)) return;` (`[Tony] replaced`, the original in a comment above it). Collect With Key, Collect Range, the ray that skips the player and everything else are unchanged. |

The berry bush prefab still has the old `collectKey` value saved in it; Unity ignores it.

## How to undo

Remove the `//` in front of your Tooltip and field, and in `Update()` put back the commented original line and delete
the `[Tony] replaced` one.
