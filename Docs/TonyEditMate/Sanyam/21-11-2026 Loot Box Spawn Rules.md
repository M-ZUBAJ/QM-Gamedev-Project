# Tony's change to Sanyam's chest spawn rules, 21/11/2026: loot boxes

**Why:** Tony added rare and epic loot boxes (guide: `Docs/Loot Chests Setup.md`) and asked for: in the grassland the
common chests as now and just a few rare boxes, never an epic box; in the desert more common chests, the usual rare
boxes and a few epic boxes.

**Your script `World/ObjectScatter.cs` is NOT changed.** Only the rules list of the ObjectScatter in
`Assets/Scenes/GameScene.unity` and `Assets/Scenes/ProceduralGenerateScene.unity`, by Tony's tool
**Tools > World > Build Loot Chests**:

| Rule | Before | After |
|---|---|---|
| ChestsForest | Terrain Layer 0 (grass), 10 | the same, Spacing Group "Chests" |
| ChestsDesert | **Terrain Layer 0 (grass)**, 10 | **the sand layer** (1 in GameScene; the new world generator's Sand Layer), at least 15, Spacing Group "Chests" |
| Rare Chests Grass (new) | - | a copy of ChestsForest with `Rare Chest.prefab`, 3 |
| Rare Chests Desert (new) | - | a copy of ChestsDesert with `Rare Chest.prefab`, 6 |
| Epic Chests Desert (new) | - | a copy of ChestsDesert with `Epic Chest.prefab`, 2 |

- **ChestsDesert used the grass layer**, so all 20 chests spawned on grass and none in the desert. It's on the sand
  layer now. Its other settings (Min Strength, Max Slope, Min Spacing, Y Offset...) are unchanged.
- The new rules come right after the chest rules (so the boxes find their spots before the trees), with the same
  settings as the rule they copy.
- One **Spacing Group** ("Chests") for all of them: Min Spacing (20 m) now keeps every box away from every other box,
  rare and epic included (before, the two chest rules used the same chest prefab, so they were one group anyway).
- Counts you change later stay when the tool runs again (it only sets a count when it makes the rule, and keeps
  ChestsDesert's at 15 or more).

**To undo:** select the ObjectScatter in each scene, delete the three new rules, and set ChestsDesert back to Terrain
Layer 0 and Count 10 (and Spacing Group empty on both chest rules).
