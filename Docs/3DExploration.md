# 3D Asset Exploration (Unity Asset Store and Mixamo)

Free 3D models and animations that suit our game, found on the web on **10/11/2026**, with their links.
Prices, versions and compatibility can change: open the page and check again before downloading.

**What our project needs from an asset**

- **Built-in render pipeline.** Our project is not URP / HDRP. On each Asset Store page, the
  **Render pipeline compatibility** table must say **Compatible** for **Built-in**, or the models show up pink.
- **Low poly**, to match our world (flat colors, round trees) and Blink's character.
- **Small**, because we build for WebGL and share the project on GitHub. In the Import window, untick what you don't use.
- **Free** (everything recommended here is).

**Whose part of the game** each asset fits: Marko = enemies, animals, combat, chest; Turki = items, ores,
tools, crafting; Sanyam = world, nature, building pieces; Tony = the player. Show the owner before replacing their models.

---

## 1. Already in the project

### Blink: FREE Low Poly Human - RPG Character

[FREE Low Poly Human - RPG Character](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979)
(Blink, free, 8.3 MB, 48 files), imported into `Assets/Imported3D/Blink/`.

- **The store page lists:** a male human body with underwear, 2 armor sets, 64 color variations per armor set,
  one hairstyle, one beard, one moustache.
- **What we found inside:**
  - Humanoid rig.
  - Standard materials, so it works in Built-in.
  - No ProjectSettings files.
  - One Blink editor tool (adds a **BLINK** menu).
  - A demo scene.
  - **32 Humanoid animations** in `Assets/Imported3D/Blink/Art/Animations/Animations_Starter_Pack`.

| Group | Animations (✓ = used by our PlayerCharacter controller) |
|---|---|
| Movement (17) | ✓ Idle, ✓ RunForward, ✓ RunBackward, ✓ RunLeft, ✓ RunRight, ✓ RunBackwardLeft, ✓ RunBackwardRight, ✓ Sprint, ✓ Jumps (Jump_Up used; also Jump_Down and the whole Jump), ✓ FallingLoop, StrafeLeft, StrafeRight, JumpWhileRunning, RollForward, RollBackward, RollLeft, RollRight |
| Combat (13) | ✓ Death, ✓ GetHit, ✓ PunchRight, ✓ MeleeAttack_OneHanded, ✓ SpellCast (also SpellCast_Start / SpellCast_End), PunchLeft, MeleeAttack_TwoHanded, BowShot, BlockingLoop, CastingLoop, Buff, IdleCombat, StunnedLoop |
| Gathering (2) | ✓ Gathering, ✓ MiningLoop |

There's no slow walking animation, only running. Every file also has a 2-frame `tpose` clip, which is a reference pose,
not an animation. How the character is set up: `Docs/Player Character Setup.md`.

### RPG Monster Duo PBR Polyart (Dungeon Mason)

Imported into `Assets/Imported3D/RPG Monster DUO PBR Polyart`. It has a slime and a turtle-shell monster, each with a PBR and a
Polyart look, 15 animations each, and Standard materials (Built-in). **Tools > Enemies > Build Monster Enemy** turns
the slime into `SlimeEnemy.prefab`, the night enemy (guide: `Docs/Enemy Setup.md`).

### Broken Vector: Low Poly Tree Pack

[Low Poly Tree Pack](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-tree-pack-57866) (free),
imported into `Assets/Imported3D/BrokenVector/LowPolyTreePack` on 12/11/2026.

- **Works in Built-in:** 4 Standard materials, each with a tiny colour-sheet texture (Point filter, no mipmaps).
- **The 38 models** are Collada (`.dae`) with **Scale Factor 1.5** and **Generate Colliders** on. The 38 prefabs are
  just a Mesh Filter and a Mesh Renderer (no collider), all using the **Normal** material.
- **Colour schemes:** Normal (summer green), Fall (autumn), Dry, Cold (winter).

| Type | What it is (sizes as imported) | Biome |
|---|---|---|
| Type0 (6) | a mix: 01 huge spreading oak with roots (5.3 m tall, 8.7 m wide), 02 dead tree, 03 redwood (12.7 m), 04 acacia, 05 and 06 round broadleaf trees (5 m) | 05 / 06: grassland |
| Type1 (5) | simple pines, 2 to 10 m | forest, snow |
| Type2 (5) | layered spruces, 3 to 10 m | forest, snow |
| Type3 (5) | umbrella trees with flat crowns | savanna |
| Type4 (5) | broadleaf trees with clumps of leaves, 3 to 10 m | grassland |
| Type5 (4) | birches (white trunk, light green), 2.6 to 6.4 m | grassland, meadow |
| Type6 (4) | willows | lake shore, swamp |
| Type7 (4) | palms | beach, desert |

**Used for:** the grassland trees. **Tools > World > Use Low Poly Grassland Trees** turns Turki's TreeNode into a Round
Oak (Type0 06) and makes 5 more looks (Type0 05, Type4 03, Type4 04, Type5 03, Type5 04). Guide: `Docs/Tree Setup.md`.

### ithappy: Animals FREE

Imported into `Assets/Imported3D/ithappy/Animals_FREE` on 12/11/2026 (free, Asset Store).

- **7 animals:** Chicken, Deer, Dog, Horse, Kitty, Pinguin, Tiger.
  - Each is an FBX with a Generic rig (no avatar) and looping clips inside: idle, walk and run. The horse also has eat;
    the tiger also has idle_rare.
  - Each has a prefab and an Animator Controller.
- **Its prefabs carry demo scripts** (`Controller` namespace): `CreatureMover`, `MovePlayerInput` (keyboard control)
  and a CharacterController. Use the FBX model in a game, not the prefab.
- **Made for URP:** its materials use the URP Lit shader, so they're pink in our Built-in project.
  - **The pack's own fix:** `Render_Pipeline_Convert/Unity_2021_Built-In_source.unitypackage` (double-click >
    Import). It changes the pack's materials.
  - **Our tool:** makes its own Standard copy instead (same picture, `Textures/Texture.png`).
- **One texture atlas** for every animal, and a sample scene per pipeline.

**Used for:** the deer that replaced the sheep. **Tools > World > Build Wild Animal** makes `DeerNode`, or any
other animal from the pack when it's selected. Guide: `Docs/Animal Setup.md`.

### Abduo Bricks: Low Poly Tool Pack

Imported into `Assets/Imported3D/Abduo Bricks/Low Poly Tool Pack` on 12/11/2026 (free).

- **25 tools:** sword, axe, pickaxe, hoe and shovel, each in Wood, Stone, Iron, Gold and Diamond. 40–64 triangles each,
  no textures.
- **Each tool** is a `Handle` (the root, pivot at the very bottom of the grip, handle along +Y) with a `ToolHead`
  child.
  - **Lengths:** axe 0.97 m, its blade toward +X; pickaxe 0.92 m, its picks along X; sword 0.99 m, its blade up Y with
    the flat facing Z.
  - **FBX:** Y-up, scale 1.
- **Made for URP:** 6 URP Lit materials (pink in our Built-in project). Our tool makes Standard copies with the same
  colours.
- **No animations, rigs or scripts.** Swings use Blink's MeleeAttack_OneHanded and MiningLoop.

**Used for:** the 15 tier tools (pickaxe, axe, sword) that replaced Marko's placeholders. **Tools > Items > Build
Tier Tools** makes them. Hoes and shovels aren't used yet. Guide: `Docs/Tool Setup.md`.

### Related Blink packs (seen while searching)

| Pack | Price | Notes |
|---|---|---|
| [FREE - 32 RPG Animations](https://assetstore.unity.com/packages/3d/animations/free-32-rpg-animations-215058) | Free | Standard Unity Asset Store EULA. Its listed animations include Idle, Combat Idle and Run Forward. The character pack above already brought 32 animations, so we don't need it for now. |
| [Low Poly RPG Characters Bundle](https://marketplace.unity.com/packages/3d/characters/humanoids/fantasy/low-poly-rpg-characters-bundle-232729) | Paid | More humans and armor, "32x Humanoid Animations", an RPG Builder prefab. |
| [FREE - Low Poly Swords - RPG Weapons](https://assetstore.unity.com/packages/3d/props/weapons/free-low-poly-swords-rpg-weapons-198166) | Free | See the recommended table below. |

---

## 2. Recommended: free, and the page says Built-in is compatible

Each page was opened and read on 10/11/2026. "Built-in ✓ (2020.3)" = the page lists Built-in as compatible for that
Unity version (the pages don't list 2023.1 itself; older versions working normally means newer ones work too).

| For | Asset | Publisher | What's inside | Size | Pipelines | Version |
|---|---|---|---|---|---|---|
| Night enemies (Marko) | [RPG Monster Duo PBR Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-duo-pbr-polyart-157762) | Dungeon Mason | A slime (1009 triangles) and a turtle-shell monster (1021 triangles), 15 animations each. Rated 4.9 (39 ratings). | 4.1 MB | Built-in ✓ (2020.3), URP ✓, HDRP ✓ | 1.5, 19/7/2023 |
| More enemies (Marko) | [RPG Monster Partners PBR Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-partners-pbr-polyart-168251) | Dungeon Mason | More monsters; its tags include rat and chest (count and animations not shown on the page) | 7.0 MB | Built-in ✓ (2020.3), URP ✓, HDRP ✓ | 1.5, 6/7/2023 |
| Animals (Marko) | [Animals FREE - Animated Low Poly 3D Models](https://assetstore.unity.com/packages/3d/characters/animals/animals-free-animated-low-poly-3d-models-260727) | ithappy | Animated, rigged animals; tags include dog, horse, tiger, kitty, farm animals | 10.7 MB | Built-in ✓ (2021.3 and Unity 6), URP ✓, HDRP ✓ | 2.3, 5/9/2025 |
| Ores and mining (Turki) | [Free Mining Pack - Low Poly Ores, Gems, Tools & Props](https://assetstore.unity.com/packages/3d/props/free-mining-pack-low-poly-ores-gems-tools-props-189962) | Pure Poly | Ore rocks, gems, mining tools (pickaxe), torch, mine props | 1.4 MB | Built-in ✓ (2021.3 and Unity 6), URP ✓, HDRP ✓ | 2.0, 17/9/2025 |
| Crafting materials (Turki) | [Simple Ores, Chunks, and Bars](https://assetstore.unity.com/packages/3d/props/simple-ores-chunks-and-bars-285315) | thatbluemuffin | Iron, coal, copper, gold, nickel, silver and zinc, each as ore, chunk and bar (good for ore → bar crafting) | 0.5 MB | Built-in ✓ (2022.3) only | 1.0.0, 11/6/2024 |
| Survival items (Turki) | [Pandazole - Survival Crafting Low poly Pack](https://assetstore.unity.com/packages/3d/props/pandazole-survival-crafting-low-poly-pack-208575) | Pandazole | Food, campfire, bow, crossbow, skulls, resources | 3.6 MB | Built-in ✓ (2020.3), URP ✓, HDRP ✓ | 1.0.1, 4/1/2022 |
| Perk chest (Marko) | [Low-Poly Chest](https://assetstore.unity.com/packages/3d/props/low-poly-chest-277655) | alexxzag | One chest | 0.5 MB | Built-in ✓ (2022.3) only | 1.0, 12/3/2024 |
| Weapons (Turki / Marko) | [FREE - Low Poly Swords - RPG Weapons](https://assetstore.unity.com/packages/3d/props/weapons/free-low-poly-swords-rpg-weapons-198166) | Blink | Swords by the same artist as our character (a review says over 50 prefabs). **Very big:** import only the swords you want. | **181.3 MB** | Built-in ✓ (2019.4), URP ✓, HDRP ✓ | 1.0, 21/7/2021 |

Dungeon Mason's monster packs also come with a "Mask Tint" shader for recoloring (Built-in, URP, HDRP), according to
their [Monster Bundle](https://marketplace.unity.com/packages/3d/characters/creatures/rpg-monster-bundle-pbr-260493) listing.

---

## 3. Worth trying, but check first

| For | Asset | Publisher | Size | Why check |
|---|---|---|---|---|
| Trees (Sanyam) | [Low Poly Tree Pack](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-tree-pack-57866) | Broken Vector | 942.8 KB | **Imported 12/11/2026: it works in Built-in (Standard materials). See section 1.** The page has no pipeline table, but it was made in 2018 for Unity 5.6, before URP existed. Popular: about 4,994 favourites; a reviewer praised the many tree types. Version 1.3, 2/7/2018. |
| Rocks (Sanyam) | [Low Poly Rock Pack](https://assetstore.unity.com/packages/3d/environments/low-poly-rock-pack-57874) | Broken Vector | 236.8 KB | Same as the trees (2018, no pipeline table). Its [itch.io page](https://brokenvector.itch.io/low-poly-rock-pack) says 24 rocks with 4 color schemes. Version 1.3, 9/7/2018. |
| Building pieces (Sanyam) | [WOODLAND - FREE - Low Poly Modular Village & Forest Kit](https://assetstore.unity.com/packages/3d/environments/fantasy/woodland-free-low-poly-modular-village-forest-kit-424362) | ForgeWorks | 6.7 MB | Built-in ✓, but only listed for Unity 6 (6000.3), so it may not open in our 2023.1. Cottages, buildings, props. Version 1.0, 6/11/2026. |
| Building pieces (Sanyam) | [Low Poly Survival modular Kit VR and Mobile](https://assetstore.unity.com/packages/3d/environments/low-poly-survival-modular-kit-vr-and-mobile-128903) | Albert_Einsteinn | 4.7 MB | Old (2018, no pipeline table). A reviewer says the colliders let you go through the walls and roof. Version 1.0, 24/9/2018. |

---

## 4. Skip: URP only (they would be pink in our project)

| Asset | Publisher | Size | Pipelines |
|---|---|---|---|
| [Low Poly Trees & Rocks : TinyNature Free](https://assetstore.unity.com/packages/3d/environments/landscapes/trees-and-rocks-low-poly-pack-tinynature-130107) | EmacEArt | 2.1 MB | Built-in ✗, URP ✓, HDRP ✗ (version 1.3, 13/5/2026) |
| [Low Poly Tool Pack - 25 Stylized Tools](https://assetstore.unity.com/packages/3d/props/tools/low-poly-tool-pack-25-stylized-tools-390696) | abduo | 304.4 KB | Built-in ✗, URP ✓, HDRP ✗ (version 1.0, 27/7/2026, made with Unity 6) |

---

## 5. All-in-one survival sets (one artist, one style)

**No single free pack on the Asset Store covers everything** (nature, resources, tools, building, animals, enemies):
every search on 10/11/2026 came to the same answer. The closest are two artists who give away many packs in one
matching style, free under **CC0** (free for any use, no credit needed). They're **not on the Asset Store**:
download the zip from their site (no account needed), unzip it, and drag the FBX files into their own folder in
`Assets`. Unity gives FBX models normal Standard materials, which work in our Built-in pipeline.

### Quaternius (quaternius.com): covers almost every part of our game

| For | Pack | What's inside | Date |
|---|---|---|---|
| Items, tools (Turki) | [Survival Pack](https://quaternius.com/packs/survival.html) | 53 survival models ("50+ survival assets"), flat colors (no textures), not animated. FBX, OBJ, Blend. | 9/2020 |
| Nature (Sanyam) | [Ultimate Nature Pack](https://quaternius.com/packs/ultimatenature.html) | 150 models, flat colors, not animated. FBX, OBJ, Blend. | 6/2019 |
| Animals (Marko) | [Ultimate Animated Animal Pack](https://quaternius.com/packs/ultimateanimatedanimals.html) | 12 animals, each with 12+ animations (attack, death, walk, jump...). FBX, OBJ, glTF, Blend. | 7/2021 |
| Night enemies (Marko) | [Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) | 50 fully animated monsters (attack, death, running, walking...). FBX, OBJ, glTF, Blend. | 10/2022 |
| Building pieces (Sanyam) | [Medieval Village MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | 304 modular, grid-snapped pieces: walls, floors, stairs, roofs, doors, windows, vines; textured. The free **Standard** tier is FBX / OBJ / glTF. The paid **Source** tier is a ready Unity project for URP, so we'd use the free FBX files. | 1/2025 |
| Small monsters | [LowPoly Animated Monsters](https://quaternius.itch.io/lowpoly-animated-monsters) (itch.io) | Pay what you want (free is allowed), 1.3 MB zip: bat, dragons, skeletons, slime, with punch / attack / jump / fly / walk animations. OBJ, FBX, Blend. | not shown |

Quaternius's animals and monsters aren't human-shaped, so they keep their own animations (Generic rig in Unity),
not Blink's or Mixamo's. Other Quaternius packs linked from the home page (not opened):
[Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html),
[Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html),
[Medieval Village Pack](https://quaternius.com/packs/medievalvillage.html),
[Farm Animal Pack](https://quaternius.com/packs/farmanimal.html),
[Cute Animated Monsters Pack](https://quaternius.com/packs/cutemonsters.html),
[Animated Monster Pack](https://quaternius.com/packs/animatedmonster.html),
[Animated Fish Pack](https://quaternius.com/packs/animatedfish.html),
[Animated Cute Fish Pack](https://quaternius.com/packs/cutefish.html).
Quaternius's packs are also listed on [OpenGameArt](https://opengameart.org/content/all-cc0-uploader-quaternius).

### Kenney (kenney.nl): items, structures and nature (no animals or monsters)

| For | Pack | What's inside | Version |
|---|---|---|---|
| Items, tools, structures (Turki / Sanyam) | [Survival Kit](https://www.kenney.nl/assets/survival-kit) | 80 files: structures, items, tools and resources. Version 2.0 was "completely remade, added 40 models". OBJ, FBX, glTF (per its [itch.io page](https://kenney-assets.itch.io/survival-kit)). | 2.0 |
| Nature (Sanyam) | [Nature Kit](https://kenney.nl/assets/nature-kit) | 330 files: trees, rocks, stones, plants, terrain pieces, waterfalls, camping equipment (per its [OpenGameArt page](https://opengameart.org/content/nature-kit)). | 1.0 (2020) |

### On the Asset Store: the closest matches

| Asset | Price | Size | Built-in | Notes |
|---|---|---|---|---|
| [POLY - Lite Survival Collection](https://assetstore.unity.com/packages/3d/props/poly-lite-survival-collection-220452) (ANIMPIC STUDIO) | Free | 3.3 MB | not shown | Food, medkit, weapons: a small free sample of the paid kit below. Version 1.0, 6/5/2024. |
| [POLY - Mega Survival Construction Kit (SRP, URP, HDRP)](https://assetstore.unity.com/packages/3d/environments/poly-mega-survival-construction-kit-srp-urp-hdrp-308834) (ANIMPIC STUDIO) | €27.59 | 103.9 MB | no table on the page; the title lists SRP (the standard Built-in pipeline), URP and HDRP | Survival base-building kit. Version 2.0, 3/2/2025. |
| [Modular Survival Outposts Pack](https://assetstore.unity.com/packages/3d/environments/urban/survival-building-pack-low-poly-stylized-326565) (Static Soul Studio) | €9.20 | 2.8 MB | ✓ (URP ✓, HDRP ✗) | Modular outposts and tents in stone / wood / metal, on a grid. Version 1.0, 31/3/2026. |
| [Low Poly Ultimate Pack](https://assetstore.unity.com/packages/3d/props/low-poly-ultimate-pack-54733) (polyperfect) | €138.01 | 246.9 MB | ✓ (URP ✓, HDRP ✓) | Huge general pack (medieval, dungeon, fantasy, nature), not survival-specific. Version 10.0, 15/10/2025. |
| [Low Poly Megapack](https://assetstore.unity.com/packages/3d/environments/low-poly-megapack-136330) (Terra Nova Creations) | €13.79 | 15.1 MB | not shown | General scenes and buildings. Version 1.2, 25/3/2019. |
| [Low Poly Environment - Nature Free](https://assetstore.unity.com/packages/3d/environments/lowpoly-environment-nature-pack-free-187052) (Polytope Studio) | Free | 51.6 MB | ✓ (URP ✓, HDRP ✗) | Trees, rocks, grass, forest. Version 1.1.2, 13/5/2026. |
| [LowPoly Environment Pack](https://assetstore.unity.com/packages/3d/environments/landscapes/lowpoly-environment-pack-99479) (k0rveen) | Free | 1.8 MB | not shown (made in 2017, before URP existed) | Version 1.0, 6/10/2017. |

**Also seen in these searches (not checked):**
- [Survival Multiplayer Framework](https://assetstore.unity.com/packages/templates/packs/survival-multiplayer-framework-341820): a whole game template with its own code, not just art, so it doesn't fit our scripts.
- [Low Poly Post-Apocalyptic Survival Pack (110 Props)](https://assetstore.unity.com/packages/3d/props/exterior/low-poly-post-apocalyptic-survival-pack-110-props-426554): barricades, crates, camp items; its listing says it was created with AI.
- [Ultimate Crafting Stations Pack](https://assetstore.unity.com/packages/3d/props/ultimate-crafting-stations-pack-stylized-low-poly-100-modular-309852): modular crafting stations.
- polyperfect's [POLY - Low Poly Ultimate Bundle](https://marketplace.unity.com/packages/3d/environments/poly-low-poly-ultimate-bundle-238513) and the [Low Poly Ultimate Mega Pack](https://assetstore.unity.com/packages/3d/environments/low-poly-ultimate-mega-pack-243990).
- Synty POLYGON packs are paid. No "POLYGON Survival" pack was found; the [POLYGON - Adventure Pack](https://syntystore.com/products/polygon-adventure-pack) says it supports URP and Built-in.
- Asset Store pages: [Base Builder game](https://assetstore.unity.com/lists/base-builder-game-87545) list, [Building a low-poly game](https://assetstore.unity.com/building-a-low-poly-game).

---

## 6. Other packs seen in the searches (not checked in detail)

These came up in the searches. Price and pipeline support were **not** checked unless written: open the page first.

**Nature**
- [Low poly trees - free nature pack](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-trees-free-nature-pack-300824) (Nebula): 1.0 MB, version 1.0, 2/12/2024, tagged mobile-friendly.
- [Low Poly Trees Pack Lite](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-trees-pack-lite-free-stylized-nature-environment-assets-295464) (CoolPoly Design): 2.4 MB, version 1.0.2, 12/11/2025, trees, plants and rocks.
- [Low Poly Trees & Nature Pack (70 Props)](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-trees-nature-pack-70-props-325300) (Anastasita3D): 2.1 MB, version 1.1, 2/2/2026, bushes, rocks, flowers, mushrooms, season variants.
- [Nature Pack - Low Poly Trees & Bushes](https://assetstore.unity.com/packages/3d/vegetation/nature-pack-low-poly-trees-bushes-210184) (Downtown Game Studio): 1.0 MB, version 1.0, 27/1/2022, includes winter / snow.
- [3D Low Poly Forest Pack Free](https://assetstore.unity.com/packages/3d/environments/3d-low-poly-forest-pack-free-336748).
- [Low Poly Forests Creation Kit](https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-forests-creation-kit-223495): free, 7/6/2022.
- [Pandazole - Nature Environment Low Poly Pack](https://assetstore.unity.com/packages/3d/environments/pandazole-nature-environment-low-poly-pack-212621): 732 models on one shared texture; the page says it works with Standard (Built-in), URP and HDRP. Price not shown.
- Asset Store lists: [Free Natural Environments](https://assetstore.unity.com/lists/free-natural-environments-11561),
  [Low Poly Assets](https://assetstore.unity.com/lists/low-poly-assets-11692),
  [Low Poly Landscape](https://assetstore.unity.com/lists/low-poly-landscape-49469),
  [Free Low-Poly and Voxel](https://assetstore.unity.com/lists/free-low-poly-and-voxel-10940).

**Monsters and heroes (Dungeon Mason's Polyart family)**
- [RPG Monster Couple PBR Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-couple-pbr-polyart-355988)
- [RPG Monster Buddy PBR Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-buddy-pbr-polyart-253961)
- [RPG Monster Wave 2 PolyArt](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-wave-2-polyart-169863)
- [Party Monster Duo Polyart PBR](https://assetstore.unity.com/packages/3d/characters/creatures/party-monster-duo-polyart-pbr-195698)
- [Monster Minion Survivor PBR Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/monster-minion-survivor-pbr-polyart-269515)
- [RPG Tiny Hero Duo PBR Polyart](https://assetstore.unity.com/packages/3d/characters/humanoids/rpg-tiny-hero-duo-pbr-polyart-225148) (small heroes)

**Animals**
- [Quirky Series - FREE Animals Pack](https://assetstore.unity.com/packages/3d/characters/animals/quirky-series-free-animals-pack-178235) (Omabuarts Studio): free, reptiles, mammals, birds, fish. A reviewer says the Animator controllers start on the attack animation (change the default state).
- [Animated Goat and Sheep - 3D low poly - FREE](https://assetstore.unity.com/packages/3d/characters/animals/animated-goat-and-sheep-3d-low-poly-free-251910) (UrsaAnimations): free, about 1.9 MB, June 2024.
- [Stylized Low Poly Animated Horse Pack](https://assetstore.unity.com/packages/3d/characters/animals/mammals/stylized-low-poly-animated-horse-pack-137631) (Imitation Studios): free, horses only, 2019.
- Paid: [Animated Low Poly Animals Pack](https://assetstore.unity.com/packages/3d/characters/animals/animated-low-poly-animals-pack-wolf-horse-shark-more-free-60-var-258476) ($9.99, 69 variants),
  [Low Poly Wild Animals Pack - Animated](https://assetstore.unity.com/packages/3d/characters/animals/mammals/low-poly-wild-animals-pack-animated-296252) ($4.99),
  [Cartoon Animals Pack](https://assetstore.unity.com/packages/3d/characters/animals/cartoon-animals-pack-low-poly-game-ready-animated-312824) ($599.99).

**Tools and weapons**
- [FREE Demo: Low Poly Post-Apo Survival Tools (4 Assets)](https://assetstore.unity.com/packages/3d/props/tools/free-demo-low-poly-post-apo-survival-tools-4-assets-331744) (PolynestLab): free, includes a crowbar and an axe.
- [FREE Low Poly Weapons Pack - Fantasy Dream](https://assetstore.unity.com/packages/3d/props/weapons/free-low-poly-weapons-pack-fantasy-dream-320869) (GanzSe): axes, swords, bows, shields.
- [Free Low Poly Modular Medieval Weapons 3D](https://assetstore.unity.com/packages/3d/props/weapons/free-low-poly-modular-medieval-weapons-3d-346702) (Black Rose Developers): axes and swords, November 2025.
- [Low Poly Stylized Pickaxe](https://assetstore.unity.com/packages/3d/props/tools/low-poly-stylized-pickaxe-hand-painted-game-ready-3d-asset-368916) (Propsybrush): free.
- [Low Poly RPG Axe](https://assetstore.unity.com/packages/3d/props/weapons/low-poly-rpg-axe-119905) (August3D): free, few ratings.
- [LowPoly - Free Tools Pack](https://marketplace.unity.com/packages/3d/props/tools/low-poly-tools-pack-84630): free, contents not shown.
- List: [Best hand-painted Weapons (Free)](https://assetstore.unity.com/lists/best-handpainted-weapons-free-|-list-33663).

**Building kits**
- [WOODLAND - FREE - Low Poly Modular Castle Kit](https://assetstore.unity.com/packages/3d/environments/fantasy/woodland-free-low-poly-modular-castle-kit-424964) (ForgeWorks)
- [HOLLOWS - FREE - Functional Horror Props & Modular Building Kit](https://assetstore.unity.com/packages/3d/props/hollows-free-functional-horror-props-modular-building-kit-426276) (ForgeWorks, horror style)
- [CITYBUILDER - FREE - Modular Low Poly City Kit](https://assetstore.unity.com/packages/3d/environments/urban/citybuilder-free-modular-low-poly-city-kit-414228) (ForgeWorks, city)
- [DOLLHOUSE - FREE - Modular Low Poly Interiors](https://assetstore.unity.com/packages/3d/props/interior/dollhouse-free-modular-low-poly-interiors-furniture-room-kit-411314) (ForgeWorks, furniture and rooms)
- [3D Free Modular Kit](https://assetstore.unity.com/packages/3d/environments/3d-free-modular-kit-85732) (Barking Dog, sci-fi)
- [Lowpoly Platformer Kit](https://assetstore.unity.com/packages/3d/environments/lowpoly-platformer-kit-free-modular-stylized-blocks-319018) (blocks)
- List: [Free Buildings](https://assetstore.unity.com/lists/free-buildings-28800)

**Survival props**
- [Survival Low Poly Asset Pack](https://assetstore.unity.com/packages/3d/props/survival-low-poly-asset-pack-409452) (naroxks3D): free, 16.7 MB, axe, pickaxe, canned food, lighter, tent, campfire, backpack.
- [Low Poly Survival Game Props](https://assetstore.unity.com/packages/3d/props/low-poly-survival-game-props-107984) (Timothy Porter): free, 653 KB, 2018.
- [GanzSe FREE Camping - Fantasy Low Poly Props](https://assetstore.unity.com/packages/3d/props/ganzse-free-camping-fantasy-low-poly-props-370804): free, 8.6 MB.
- [Low-Poly Survival Asset Pack (Props & Items)](https://assetstore.unity.com/packages/3d/props/low-poly-survival-asset-pack-props-items-386898) (simon3tortora): free, 6.7 MB.
- [POLY - Lite Survival Collection](https://assetstore.unity.com/packages/3d/props/poly-lite-survival-collection-220452) (AnimPic Studio): free, 3.3 MB, food, weapons, medkit.
- [Stylised Reality: Stylized Camping Pack](https://assetstore.unity.com/packages/3d/props/stylised-reality-stylized-camping-pack-low-poly-survival-pack-ur-322112) (Resonant Mesh): free, 9.9 MB, listed as URP (probably not for us).
- [3D Low-Poly Chest](https://assetstore.unity.com/packages/3d/props/3d-low-poly-chest-240360): free, fantasy / gold chest.
- Pandazole: [Simple Game Low Poly Pack](https://assetstore.unity.com/packages/3d/props/pandazole-simple-game-low-poly-pack-210274) (free),
  [Farm Ranch Pack](https://assetstore.unity.com/packages/3d/props/pandazole-farm-ranch-pack-206756) (Built-in / URP / HDRP, price not shown).
- Not opened: [Survival Lowpoly Tools and Props Pack](https://assetstore.unity.com/packages/3d/props/survival-lowpoly-tools-and-props-pack-277233),
  [Low Poly Survival Tools Mega Pack](https://assetstore.unity.com/packages/3d/props/tools/low-poly-survival-tools-mega-pack-269568),
  [Low Poly Survival Props](https://assetstore.unity.com/packages/3d/props/low-poly-survival-props-240383).
- 2D inventory icons (not 3D, could suit our inventory): [Survival, crafting, fishing, farming, weapons, tools, resources icons](https://assetstore.unity.com/packages/2d/survival-crafting-fishing-farming-weapons-tools-resources-icons--404616).

**Ores and mining**
- [DEMO Low Poly Mines](https://assetstore.unity.com/packages/3d/environments/dungeons/demo-low-poly-mines-327689) (Emilulz): free, gold ore, iron crystals, pickaxes, helmets. The full version: [Low Poly Mines](https://assetstore.unity.com/packages/3d/environments/dungeons/low-poly-mines-327302).
- [Low-Poly Resource Rocks](https://assetstore.unity.com/packages/3d/props/exterior/low-poly-resource-rocks-76150) (False Wisp Studios): free; a reviewer says it has no plain rock to match.
- Low Poly Gemstone Pack (Simply Poly Lab): crystals and jewels, in the [Free Assets - November 2025](https://assetstore.unity.com/lists/free-assets-november-2025-5773952565254) list.
- Paid: [Mining Tools, Ore, & Ingots](https://assetstore.unity.com/packages/3d/props/exterior/mining-ore-ingots-minerals-kit-26357) ($9.99),
  [Low Poly Fantasy Minerals - Rocks](https://assetstore.unity.com/packages/3d/props/low-poly-fantasy-minerals-rocks-313525) ($4.99).

---

## 7. Mixamo: free characters and animations (outside the Asset Store)

[mixamo.com](https://www.mixamo.com) is a free Adobe website (needs a free Adobe account). It is **both**:

- **Characters** tab: ready-made 3D people with bones inside.
- **Animations** tab: thousands of moves (idle, walk, run, jump, attack, death) that fit any human-shaped character.
- **Auto-rigging:** upload your own human-shaped model and it adds the bones.
- Only for human-shaped bodies (2 arms, 2 legs), not for four-legged animals.

**Download settings for Unity:**
- **Character:** Format **FBX for Unity (.fbx)**, Pose **T-pose**.
- **Animations:** Format **FBX for Unity**, Skin **Without Skin**.
- Tick **In Place** on walk / run if you see it: our ball does the moving, so the animation should stay on the spot.

**A slow walk for our character:** download Mixamo's "Walking" (In Place, Without Skin). It only looks right if the
walking speed is lowered (ours is 7.8 m/s, already a run).

**Licence** (from a community summary of the Mixamo FAQ): free to use inside games and videos; what's not allowed is
giving out or selling the raw character and animation files on their own.
[Mixamo FAQ discussion](https://community.adobe.com/t5/mixamo-discussions/mixamo-faq-licensing-royalties-ownership-eula-and-tos/td-p/13234775),
official FAQ: [helpx.adobe.com/creative-cloud/faq/mixamo-faq.html](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html).

**Will an Asset Store character work with Mixamo animations?**
- **Before downloading:** the page says Humanoid, Mecanim or Mixamo compatible, or **Rigging: Yes**, and the body is
  human-shaped. "Not rigged" or "static mesh" means no bones.
- **After importing:** click its .fbx, then Inspector > **Rig** > Animation Type **Humanoid** > **Apply**.
  A tick next to **Configure...** means it works.

---

## 8. How to get an asset from the Asset Store

1. Open [assetstore.unity.com](https://assetstore.unity.com) (or in Unity: **Window > Asset Store**). Sign in with the
   **same Unity account** as Unity Hub.
2. Search, and use the **Pricing** filter for free assets.
3. On the asset's page, check **Render pipeline compatibility**: Built-in must be **Compatible**.
4. Click **Add to My Assets** and **Accept** the Terms of Service popup. That popup is the licence agreement:
   accepting is normal and free assets stay free.
5. In Unity: **Window > Package Manager** > **My Assets** (left side) > the pack > **Download**, then **Import**.
6. In the Import window, untick demo scenes and anything you won't use. Untick a **ProjectSettings** folder if one
   is listed: it would replace our project settings.
7. Tell Tony the folder name. Our tools then turn models into game things:
   - **Tools > Items > Make Item From Selection** (items, see `Docs/Item Setup.md`).
   - **Tools > Building > Make Building From Selection** (building pieces, see `Docs/Build Menu Setup.md`).
   - Enemies and animals need hooking up to Marko's scripts.

---

## 9. Licence notes found while searching

- **Asset Store assets:** you can use them inside your game; you can't share or sell the asset files themselves as
  downloadable resources ([Unity support](https://support.unity.com/hc/en-us/articles/34387186019988-Can-I-use-assets-from-the-Asset-Store-with-other-engines),
  [EULA FAQ](https://assetstore.unity.com/browse/eula-faq), [Terms of Service and EULA](https://unity.com/de/legal/as-terms)).
- **Editor tools** ("Extension Assets") need one licence per person; other assets (models, animations) are
  licensed per company or team ([Unity support: Extension Assets](https://support.unity.com/hc/en-us/articles/360013083452-How-do-I-know-if-my-Asset-is-an-Extension-Asset-which-requires-a-Per-Seat-license)).
- **Asset files in a public repository:** forum users count it as sharing the files
  ([forum thread](https://discussions.unity.com/t/what-kind-of-license-can-be-used-to-protect-assets-purchased-from-the-asset-store-in-a-public-repo/860104)).
  Some teams keep the pack folders out of git with `.gitignore` and each person imports them
  ([forum thread](https://discussions.unity.com/t/question-regarding-github-and-asset-store/767483)).
