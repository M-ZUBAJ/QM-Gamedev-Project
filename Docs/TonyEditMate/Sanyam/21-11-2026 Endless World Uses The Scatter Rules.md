# Tony's change to how the new world uses your ObjectScatter, 21/11/2026: an endless world, and the snow's rules

**Why:** Tony asked for the new world (ProceduralGenerateScene) to go on forever ("map generate to infinite") and for a
snow biome with diamond caves and more chests than the desert (guides: `Docs/New World Setup.md`,
`Docs/Snow Setup.md`). The land is now made in 256 x 256 m squares (chunks, each its own Unity Terrain) while the
player walks, so there's no single terrain for your ObjectScatter to fill when the game starts.

**Your script `World/ObjectScatter.cs` is NOT changed**, and GameScene (the old world) works exactly as before.

## What changed in the new world only

| What | Before | After |
|---|---|---|
| Your ObjectScatter component in ProceduralGenerateScene | Tony's WorldGenerator gave it the generated terrain before its Start, and it spawned everything once | Tony's WorldGenerator switches the component off in its Awake (before your Start runs) and uses your **Rules** list for every new chunk instead. Your rules are read, never changed while playing. |
| What a rule's **Count** means there | how many on the whole map | how many on every **Rules Area** (WorldGenerator setting, 400,000 m², about 630 x 630 m) of ground painted with the rule's texture: a chunk that's half grass gets half the trees of an all-grass one. In GameScene it still means the whole map. |
| Your other rule settings | | used the same way as in your script: Terrain Layer + Min Strength, Max Slope, Min Spacing within the Spacing Group, Clearance from other groups, Patch Size / Open Ground (the same Perlin formula), Min / Max Size, Y Offset, a random turn. Spacing works across chunk edges (one shared grid). |
| Rules on the snow texture | | grow only in the snow biome (not on the rocky hills' snowy tops), so the snow's chests stay far from the start |

## New rules in ProceduralGenerateScene's copy of your ObjectScatter

Added by Tony's **Tools > World > Build New World Scene** (via **AddSnowRules**; GameScene doesn't get them, it has no
snow biome), all on the snow texture (layer 2):

| Rule | What | Count | Settings |
|---|---|---|---|
| ChestsSnow | Marko's common chest | 40 | a copy of your ChestsDesert (else ChestsForest), right after the desert's chest rules |
| Rare Chests Snow | the rare loot box | 20 | a copy of Rare Chests Desert |
| Epic Chests Snow | the epic loot box | 8 | a copy of Epic Chests Desert |
| Legendary Chests Snow | the legendary loot box | 4 | a copy of Legendary Chests Desert |
| Trees: Snow Pine, Tall Snow Pine, Snow Spruce, Tall Snow Spruce | snowy conifers (Tony's Prefab Variants of Turki's TreeNode) | 120 each | Min Strength 0.7, Max Slope 25, Min Spacing 9, Spacing Group Trees, Patch Size 120, size 0.85 - 1.2 |
| Snow Rocks | StoneNode Snow (a frosty Prefab Variant of Turki's StoneNode) | 40 | Min Strength 0.6, Max Slope 30, Min Spacing 14, size 0.8 - 1.5 |

The desert keeps 30 / 12 / 4 / 2 boxes; the snow has more on purpose (it's the farthest, best place). The chest copies
keep the "Chests" spacing group (20 m between any two boxes).

## How to undo

- The snow's rules: open ProceduralGenerateScene, select the ObjectScatter, delete the rules above, save. (Running
  Build New World Scene again adds them again: the scene is remade from GameScene.)
- Your ObjectScatter spawning by itself in the new world: that needs the old one-terrain WorldGenerator
  (`Assets/Script/World/WorldGenerator.cs` from git before 21/11/2026, v2.16).
