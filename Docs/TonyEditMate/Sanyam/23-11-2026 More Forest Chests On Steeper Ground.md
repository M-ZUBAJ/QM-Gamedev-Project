# Tony's change to Sanyam's chest spawn rules, 23/11/2026: more forest chests, on steeper ground too

**Why:** Tony felt there were few chests in the forest. Nothing had changed (the rules were the same since 21/11/2026),
but the grassland is the poorest biome: about 3 common and 1 rare box per 256 x 256 m of the new world, and only on
ground up to 10 degrees steep. Tony chose 35 common and 10 rare boxes, and the same Max Slope as the desert (15).

**Your script `World/ObjectScatter.cs` is NOT changed.** Only two rules of the ObjectScatter on the **Scatter** object
in `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ProceduralGenerateScene.unity`:

| Rule | Setting | Before | After |
|---|---|---|---|
| ChestsForest | Count | 20 | **35** |
| | Max Slope | 10 | **15** |
| Rare Chests Grass | Count | 6 | **10** |
| | Max Slope | 10 | **15** |

- Every other setting (terrain layer, Min Strength, Min Spacing, Y Offset, Spacing Group "Chests") is unchanged.
- In the new world the counts are shared out per piece of land (Tony's WorldGenerator: count x grass area / Rules Area
  400,000 m²): a full 256 m grassland square now gets about 5.7 common and 1.6 rare boxes (was 3.3 and 1).
- Tony's Tools > World > Build Loot Chests now raises these two counts to at least 35 and 10 too (it never lowers a
  count and doesn't touch Max Slope), so running it again keeps this.

**To undo:** select **Scatter** in each scene, Rules > **ChestsForest**: Count 20, Max Slope 10; **Rare Chests Grass**:
Count 6, Max Slope 10.
