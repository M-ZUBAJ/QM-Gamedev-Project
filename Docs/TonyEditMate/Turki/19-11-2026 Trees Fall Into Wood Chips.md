# Tony's edit to Turki's trees, 19/11/2026: chopped trees fall and break into brown wood chips

**Files changed:**
- `Assets/Prefabs/ResourceNodes/TreeNode.prefab` (Turki's): Tony's **TreeFall** component added (Tools > Effects >
  Build Tree Fall, `Assets/Script/Editor/TreeFallBuilder.cs`). The looks in `ResourceNodes/Trees` (Prefab Variants
  of TreeNode) get it from there. Nothing else on the prefab changes.
- Your `ResourceNode.cs` is **not** changed. Your berry bush (`BerryNode`) is skipped (it's picked with E).
- New, Tony's: `Assets/Script/Effects/TreeFall.cs`, `Assets/Script/Effects/FallingTree.cs`,
  `Assets/Prefabs/Effects/WoodChips.prefab`, `Assets/Effects/WoodChips/`.

**Why (Tony asked for it):** brown particles when a tree falls. A chopped tree used to vanish at once.

**How it works with your script:** when your `ResourceNode.Break()` gives the drops and calls `Destroy(gameObject)`,
TreeFall sees (in its OnDestroy) that `currentHealth` is 0 and makes a look-only copy of the tree that falls over,
bursts into chips and disappears. The drops, the apple bonus and the health stay exactly as you set them.

**To undo:** open `TreeNode.prefab`, remove the **TreeFall** component, save.

## Update, same day: the bottom of the falling trunk is closed

The pack's trunks are open at the bottom (hidden in the ground while the tree stands), so a falling tree showed a
see-through hole where the player had chopped it. Build Tree Fall now also makes closed copies of the tree meshes
(`Assets/Effects/TreeFall/Closed Trunks/`) and fills TreeFall's **Closed Meshes** and **Cut Wood** on TreeNode and on
its looks. Only the falling copy uses them: your TreeNode's own mesh, collider and ResourceNode are unchanged.
To undo: the same as above (remove TreeFall).

Guide: `Docs/Tree Fall Setup.md`.
