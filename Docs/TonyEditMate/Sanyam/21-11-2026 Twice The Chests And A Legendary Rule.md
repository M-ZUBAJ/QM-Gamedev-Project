# Tony's change to Sanyam's chest spawn rules, 21/11/2026: twice the chests and a legendary box

**Why:** Tony asked for more chests in both chest systems (twice as many) and added a fourth kind of loot box, the
**legendary box** (orange, desert only, the new chest system only; guide: `Docs/Loot Chests Setup.md`).

**Your script `World/ObjectScatter.cs` is NOT changed.** Only the rules list of the ObjectScatter in
`Assets/Scenes/GameScene.unity` and `Assets/Scenes/ProceduralGenerateScene.unity`, by Tony's tool
**Tools > World > Build Loot Chests**:

| Rule | Before (the first loot box build) | After |
|---|---|---|
| ChestsForest | 10 | **20** |
| Rare Chests Grass | 3 | **6** |
| ChestsDesert | 15 (sand layer) | **30** |
| Rare Chests Desert | 6 | **12** |
| Epic Chests Desert | 2 | **4** |
| Legendary Chests Desert (new) | - | a copy of Epic Chests Desert with `Legendary Chest.prefab`, **2**, right after it |

- Counts only go up: a rule with a bigger count keeps it when the tool runs again.
- Every other setting (terrain layer, Min Strength, Max Slope, Min Spacing, Y Offset, Spacing Group "Chests") is
  unchanged; the new rule copies Epic Chests Desert's.
- In the old chest system the legendary boxes remove themselves as soon as the game starts (Tony's ChestTier), so that
  rule only shows boxes in the new system.

**To undo:** select the ObjectScatter in each scene, set the counts back (10, 3, 15, 6, 2) and delete the Legendary
Chests Desert rule.
