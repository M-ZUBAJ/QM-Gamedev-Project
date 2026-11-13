# Tony's edit to Sanyam's spawner settings, 12/11/2026: deer instead of sheep

**No code of Sanyam's was changed:** `Assets/Script/World/ObjectScatter.cs` is untouched. What changed is one rule in
the ObjectScatter component's **Rules** list in `Assets/Scenes/GameScene.unity` (its Inspector settings). Tony's tool
**Tools > World > Build Wild Animal** (`Assets/Script/Editor/WildAnimalBuilder.cs`) changed it, after asking.

**Why:** Tony asked to replace the sheep with deer from ithappy's free **Animals FREE** pack. The deer walk around and
run away from the player when the player comes near.

---

## The change (ObjectScatter on the scene object, Rules list)

| Rule `Animals` | Before | After |
|---|---|---|
| Prefab | `ResourceNodes/AnimalNode` (Turki's sheep) | `ResourceNodes/DeerNode` (Tony's deer) |
| Y Offset | 0.6: the sheep's pivot was the middle of its body | 0: the deer's pivot is its feet |
| Count, Terrain Layer, Min Strength, Max Slope, Min Spacing | 40, 0, 0.6, 20, 20 | unchanged |

**Other rules** (chests, trees, berry bushes, rocks) are untouched.

**Also in GameScene:** the one sheep placed by hand, at (-9.8, 0.5, -11.3), was replaced by a DeerNode standing on the
ground at the same spot.

**Good to know:**
- **Spawned deer move by themselves:** Tony's `WildAnimal` makes the spot where you spawn one its home, and it wanders
  up to 12 m from there.
- **New spawn rules:** a rule using DeerNode, or any animal made by the tool, needs **Y Offset 0**.
- **Turki's sheep isn't deleted:** `AnimalNode.prefab` and his `Wander` script are untouched, just no longer used in
  the scene.

**To undo:**
- **Before saving the scene:** Ctrl + Z.
- **After saving:** set the `Animals` rule's Prefab back to `AnimalNode` and its Y Offset to 0.6, and swap the placed
  DeerNode for an AnimalNode at Y 0.5.

Animal guide: `Docs/Animal Setup.md`.
