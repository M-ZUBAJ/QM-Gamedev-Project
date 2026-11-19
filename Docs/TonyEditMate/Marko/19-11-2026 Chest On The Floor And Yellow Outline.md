# Tony's edit to Marko's chest, 19/11/2026: chests on the floor, yellow outline, E that always works

**Files changed:**
- `Assets/Script/Perks/PerkChest.cs` (Marko's script). Two `[Tony]` changes; the replaced code is kept in a comment.
- `Assets/Prefabs/WorldItems/Chest.prefab` (Marko's prefab): two of Tony's components added, nothing removed. The
  tool **Tools > World > Set Up Chests** (`Assets/Script/Editor/ChestSetup.cs`) adds them.
- `Assets/Scenes/GameScene.unity`: the 4 placed chests (Chest, Chest (1), (2), (3)) are moved down onto the floor, by
  the same tool.

**Why:**
- **Floating:** Tony saw the chests hovering above the grass. ObjectScatter spawns them at the ground + Y Offset
  0.6. That fitted the old box chest, whose middle was its pivot, but the new Quaternius model is placed 0.21 m lower
  inside the prefab. So they hovered above the grass.
- **Outline:** Tony wants a signal on things you can use: a yellow outline on the chest while you can open it.
- **The E key:** in first person the camera sits on the edge of the player's own ball (eye height 0.5 on a 0.5 m
  ball). A plain `Physics.Raycast` often hit the player first, so E did nothing. Turki's berries skip the player for
  the same reason.

---

## PerkChest.cs

| What | Change |
|---|---|
| New read-only property | `public bool IsOpened => opened; // [Tony] added`. Tony's InteractOutline hides the outline once the chest is opened. Nothing else reads or changes `opened`. |
| `Update()`, the ray check | `[Tony] replaced`: the `Physics.Raycast` + "is it this chest" lines became `if (!InteractOutline.LooksAt(ray, interactionRange, transform)) return;`. LooksAt takes the nearest thing along the same ray, within the same range, skipping the player's own body. The outline uses the same test, so it shows exactly when E works. |

Unchanged: the key (E), the range, the cost, TryOpen, the perks, despawning.

## Chest.prefab

| Added | Settings |
|---|---|
| **StandOnGround** (Tony's) | **Foot Offset** = the model's bottom below the pivot, measured by the tool from the model's real corners (the Console prints it). When the game starts it puts the chest on the floor under it, sunk 2 cm, tilted up to 15° on slopes. This works whatever Y Offset the spawner uses, and for placed chests too. |
| **InteractOutline** (Tony's) | Yellow, **Show When** *Look At*, **Range** = your **Interaction Range** (3). |

Unchanged: the model, its Animator, the Box Collider, PerkChest and its perks.

**Sanyam's ObjectScatter** is not changed: its chest rules keep Y Offset 0.6, and StandOnGround settles the chests
after they spawn.

**To undo:**
- **PerkChest.cs:** delete the `IsOpened` lines, then put the commented original ray lines back in `Update()` in place
  of the `[Tony] replaced` line.
- **Chest.prefab:** remove its StandOnGround and InteractOutline components.
- **GameScene, before saving:** Ctrl + Z. **After saving:** set the 4 placed chests' Y back to 0.2.

Guide: `Docs/Interact Outline Setup.md`.
