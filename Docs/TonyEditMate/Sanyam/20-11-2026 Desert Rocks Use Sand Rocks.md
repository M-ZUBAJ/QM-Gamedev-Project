# Tony's edit to Sanyam's spawning, 20/11/2026: the desert rocks are sand-colored

**What changed:** in GameScene, your ObjectScatter rule **Desert Rocks** spawns
`Assets/Prefabs/ResourceNodes/Rocks/StoneNode Desert.prefab` instead of `StoneNode`. Everything else in the rule (count
70, Terrain Layer 1, Min Strength, Max Slope, spacing, sizes) is the same. **Forest Rocks** still spawns StoneNode. Your
`ObjectScatter.cs` is **not** changed.

**Why (Tony asked for it):** the rocks are now low-poly rocks from Broken Vector's free Low Poly Rock Pack (Tony's
**Tools > World > Use Low Poly Rocks**). StoneNode is grey. StoneNode Desert is the same rock (a Prefab Variant of
StoneNode) in the pack's sand color, so the desert's rocks match the sand.

Their pivot is now where the rock meets the ground, so Y Offset 0 (what both rock rules already had) puts them right
on the ground. Each rock also stands itself on the floor and tilts on slopes (StandOnGround).

**To undo:** select the object with ObjectScatter, open the Desert Rocks rule, and drag `StoneNode` back into
**Prefab**. Then save the scene.

Guide: `Docs/Rock Setup.md`.
