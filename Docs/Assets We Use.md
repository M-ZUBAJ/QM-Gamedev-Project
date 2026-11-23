# Assets We Use

Every asset in the project that **we didn't make ourselves**: what it is, where it is, what the game uses it for,
its licence and its link. Checked on **23/11/2026**.

When you import something new, add a row here, put 3D packs in `Assets/Imported3D/<pack name>`, and check the
licence first. Packs we looked at but haven't imported are in `Docs/3DExploration.md`.

## Used in the game

| Asset | By | In the project | What the game uses it for | Licence | Link |
|---|---|---|---|---|---|
| FREE Low Poly Human - RPG Character (v1.0) | Blink | `Assets/Imported3D/Blink` | the player's character, the first-person arms, its starter animations (run, jump, punch, mine...), the plate armor | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979) |
| Mixamo animations | Adobe | `Assets/Imported3D/Mixamo` | the punch combo, the hit reaction, the tool swing, the bow (draw, aim, release) | free for games (Mixamo terms) | [mixamo.com](https://www.mixamo.com) |
| Low Poly Tree Pack (v1.3) | Broken Vector | `Assets/Imported3D/BrokenVector/LowPolyTreePack` | every tree: grassland trees, the snow's pines and spruces | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-tree-pack-57866) |
| Low Poly Rock Pack (v1.3) | Broken Vector | `Assets/Imported3D/BrokenVector/LowPolyRockPack` | rocks and ores, the rock caves, the chunks when a rock breaks | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/environments/low-poly-rock-pack-57874) |
| Low Poly Tool Pack - 25 Stylized Tools (v1.0) | Abduo Bricks | `Assets/Imported3D/Abduo Bricks` | the tools: wooden to diamond pickaxes, axes and swords | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/props/tools/low-poly-tool-pack-25-stylized-tools-390696) |
| Medieval fantasy weapon pack (v1.0) | GameLair | `Assets/Imported3D/GameLair` | the Wooden Bow, the Spruce Bow and the arrows | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/props/weapons/medieval-fantasy-weapon-pack-286634) |
| Animals FREE - Animated Low Poly 3D Models (v2.3) | ithappy | `Assets/Imported3D/ithappy` | the deer | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/animals/animals-free-animated-low-poly-3d-models-260727) |
| RPG Monster Duo PBR Polyart (v1.5) | Dungeon Mason | `Assets/Imported3D/RPG Monster DUO PBR Polyart` | the night 1 slime and the night 2 turtle | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-duo-pbr-polyart-157762) |
| RPG Monster Partners PBR Polyart (v1.5) | Dungeon Mason | `Assets/Imported3D/RPGMonsterPartnersPBRPolyart` | the night 3 chest monster and the night 4 beholder | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-partners-pbr-polyart-168251) |
| Loot Box (v0.0.1; the store now calls it Loot Box Sci-Fi) | DuNguyn | `Assets/Imported3D/DuNguyn` | the rare, epic and legendary loot boxes | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/props/loot-box-sci-fi-305240) |
| Chest (with its open animation) | Quaternius | `Assets/Imported3D/Chest by Quaternius` | the common chest | CC0 (all of Quaternius's models are) | [quaternius.com](https://quaternius.com) (Marko imported it: please add its exact page here) |
| Yughues Free Bushes (v2.0) | Nobiax / Yughues | `Assets/Imported3D/YughuesFreeBushes2018` | the berry bushes (2 of its 5 bushes) | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/vegetation/plants/yughues-free-bushes-13168) |

## Imported, not used yet

| Asset | By | In the project | For | Licence | Link |
|---|---|---|---|---|---|
| Low Poly Modular Armors - Free (v1.3.0) | Polytope Studio | `Assets/Imported3D/Polytope Studio` | the human enemies' bodies, armor and weapons: only on the experiment branch `Tony-HumanEnermyAi` so far | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/lowpoly-modular-armors-free-pack-199890) |
| Evil Dragon Pack HP (v1.2; the store page is "Dragon for Boss Monster: HP") | Dungeon Mason | `Assets/Imported3D/FourEvilDragonsHP` | four dragons, for later (nothing uses them yet) | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/3d/characters/creatures/dragon-for-boss-monster-hp-79398) |
| Simple SciFi Gun Turret Set (v1.0; package "SciFi Simple Gun Turret Set") | Tarko (Tarko 3D) | `Assets/Imported3D/Sci Fi Assets` | 4 sci-fi gun turret models (`Prefabs/Turret 1a` - `1d`, made as upgrades of one base turret), for later (nothing uses them yet; imported 23/11/2026). It brings its own copy of Unity's old **Post Processing Stack v1** (`Sci Fi Assets/PostProcessing`) for its test scene: two of its editor scripts were fixed for Unity 2023 so the project compiles (`MinDrawer.cs`: the name `MinAttribute` written in full, CS0104; `CurveEditor.cs`: `EventType.repaint` / `mouseDown` / `keyDown` -> `Repaint` / `MouseDown` / `KeyDown`, CS0619; both marked `[Tony] edited`) | Unity Asset Store EULA | [Asset Store](https://assetstore.unity.com/packages/slug/62750) |
| Metalic Bear trap Rigged Free | a CGTrader artist | `Assets/Imported3D/CGTraderBearTrap` | the four **Bear Traps** you build with B (Copper, Iron, Gold, Diamond; Traps tab; branch `Tony-Traps`; Tools > Building > Build Traps cuts it into a base and two jaws that all four share, each in its own metal color, guide `Docs/Trap Setup.md`). `beartrap.fbx` (from Blender): one mesh (about 4,000 triangles), 9 bones, one material "metal" with no texture; its 2 "animations" are single still frames, so the trap closes its jaws itself. It's 4.6 m wide as exported (the tool sizes it to 0.7 m) | free CGTrader model: its licence is shown next to the download on the page | [CGTrader](https://cgtrader.com/3d-models/industrial/tool/beartrap-21e3e898-8858-4c0b-92ef-68013d9b07d5) |

## Editor tools (not part of the game)

| Asset | By | In the project | What it does | Licence | Link |
|---|---|---|---|---|---|
| Custom Hierarchy | Febucci | `Assets/Plugins/Febucci`, `Assets/Editor Default Resources/Febucci` | colors and icons in the Hierarchy window | free to use, may not be sold (its README) | [GitHub](https://github.com/febucci/unitypackage-custom-hierarchy) |

## From Unity (nothing to credit)

TextMesh Pro (`Assets/TextMesh Pro`: the text and its Liberation Sans font) and Unity's own packages (Aseprite
Importer, Test Framework, Visual Studio Editor...), listed in `Packages/manifest.json`.

## Made by us

Everything else: all the scripts, the UI pictures (drawn in code by our tools, plus the key hints in `Assets/UI`),
the terrain colors (`Assets/World_Textures`: plain colors), the grass blades (`Assets/Effects/Grass`), the map pieces
in `Assets/Map`, the berry bunches, the armor colors and every effect texture.

## Credits

To copy into the game's credits or a report:

- Player character: FREE Low Poly Human - RPG Character by **Blink**
- Animations: **Mixamo** (Adobe)
- Trees and rocks: Low Poly Tree Pack and Low Poly Rock Pack by **Broken Vector**
- Tools: Low Poly Tool Pack by **Abduo Bricks**
- Bows and arrows: Medieval fantasy weapon pack by **GameLair**
- Deer: Animals FREE by **ithappy**
- Monsters: RPG Monster Duo and RPG Monster Partners by **Dungeon Mason**
- Loot boxes: Loot Box by **DuNguyn**
- Chest: **Quaternius**
- Berry bushes: Yughues Free Bushes by **Nobiax / Yughues**

## Notes

- `Assets/GameLair` is an empty folder left over from moving that pack into `Assets/Imported3D`: it can be deleted
  in Unity.
- Import packs with Unity open (Window > Package Manager > My Assets), and commit their `.meta` files with them. Never
  commit `.meta` changes you didn't make yourself (see `Docs/TonyEditMate/Marko/23-11-2026 Asset Links Restored After A Pull.md`).
