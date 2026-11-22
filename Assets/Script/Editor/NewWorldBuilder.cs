/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build New World Scene (it asks first). Run it again whenever GameScene changes
              (new menus, player scripts, spawner settings...): the new world is made from GameScene each time, so
              changes made by hand in ProceduralGenerateScene are lost then (put them in GameScene instead).
              Needs the rock shapes from Tools > World > Use Low Poly Rocks (for the caves).
              Guide: Docs/New World Setup.md
Purpose: Makes the "New world" of the title screen's NEW GAME page: Assets/Scenes/ProceduralGenerateScene.unity.
           - It's a copy of GameScene with everything the game needs (the player, camera, sun and day / night,
             HUD and menus, GameRules, key bindings, enemy, deer and tree spawners, Marko's fireball...),
           - without GameScene's hand-made map: its terrains, the trees, rocks, ores, chests, apples and deer placed
             by hand, building pieces, its walls (Boundaries) and World Barrier, and helper objects (Spawn, Ore
             Testing). The new world is endless (since 21/11/2026): it has no barrier;
           - a view distance on its Main Camera (ViewDistance, 100 m: nothing further is drawn, a mist hides the edge;
             faster, most of all in the web build), and the scene saved with its fog on (Linear): a built game keeps
             fog only if a scene in it has fog on, and the mist is the fog;
           - plus the World Generator (WorldGenerator), given the old terrain's ground textures (same order, so
             ObjectScatter's rules work the same) and material, the rock caves (Tools > World > Build Rock Caves,
             run first) and the Stone / Copper / Iron rocks for them, and the grass picture (Polytope's grass blades,
             copied once to Assets/Effects/Grass/GrassBlades.png with clamped edges; Polytope's own file isn't changed);
             GameScene has two terrains (the painted Terrain (1) inside Sanyam's WorldMap and an unpainted top-level
             Terrain): the textures come from the one with the most of them;
           - ObjectScatter (Sanyam's; his script has one [Tony] line: nothing spawns outside a barrier) gets more
             rules in this scene: Hill Rocks (stone rocks on the rocky hills), Sandstone Rocks (the desert's hard
             sandstone, DesertBuilder), and for the snow (SnowBuilder) the snowy conifers (Trees: Snow Pine...), Snow
             Rocks and the snow's chests: copies of the desert's chest rules on the snow texture with more boxes (40
             common, 20 rare, 8 epic, 4 legendary; the desert has 30, 12, 4, 2). It loses the rules that put plain stone
             rocks on the sand (his Desert Rocks; GameScene keeps it) and has no terrain of its own: the World Generator
             switches it off and uses its rules chunk by chunk as the land is made;
           - the desert's content first (Tools > World > Build Desert Content: Gold, GoldNode, SandstoneNode, the
             sandstone caves, the turtle), given to the World Generator (desert caves, gold, sandstone). The turtle is
             a night monster (since 21/11/2026): it comes with GameScene's EnemySpawner and its NightMonsters (Tools >
             Enemies > Set Up Night Monsters), copied with the scene; no day monster spawner is added;
           - then the snow's content (Tools > World > Build Snow Content: Diamond, DiamondNode, StoneNode Snow, the snow
             caves, the snowy conifers, the Diamond tools' recipes), given to the World Generator (snow caves, diamonds);
           - its light from the sky is saved (like Tools > World > Set Up Sky Lighting), and it's added to Build
             Settings, so the title screen can load it;
           - Unity's terrain grass shaders go into Project Settings > Graphics > Always Included Shaders (no saved
             scene has terrain grass, so a built game would leave them out).
Requirement: NewWorldBuilder.cs x-> WorldGenerator.cs, RockCaveBuilder.cs, DesertBuilder.cs, SnowBuilder.cs, GrasslandTreeBuilder.cs, SkyLightingSetup.cs, ObjectScatter.cs, AnimalSpawner.cs, PlayerController.cs, ResourceNode.cs, PerkChest.cs, ItemPickup.cs, WildAnimal.cs, WorldBarrier.cs, ViewDistance.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class NewWorldBuilder
{
    // ===== Fields =====
    const string ToolName = "Build New World Scene";
    const string OldScenePath = "Assets/Scenes/GameScene.unity";
    public const string NewSceneName = "ProceduralGenerateScene";
    const string NewScenePath = "Assets/Scenes/" + NewSceneName + ".unity";
    const string BuildingFolder = "Assets/Prefabs/Building";   // building pieces placed by hand are part of the old map
    const string StonePath = "Assets/Prefabs/ResourceNodes/StoneNode.prefab";
    const string CopperPath = "Assets/Prefabs/ResourceNodes/CopperNode.prefab";
    const string IronPath = "Assets/Prefabs/ResourceNodes/IronNode.prefab";
    const string HillRule = "Hill Rocks";
    const string SandstoneRule = "Sandstone Rocks";   // the desert's hard rock (DesertBuilder's SandstoneNode)
    const string SnowRockRule = "Snow Rocks";         // frosty stone rocks on the snow (SnowBuilder's StoneNode Snow)
    const string SnowTreeRule = "Trees: ";            // + the look, e.g. "Trees: Snow Pine" (like the grassland's tree rules)
    const int SnowTrees = 480;                        // snowy conifers for every Rules Area of snow, shared out by the looks
    const string ChestGroup = "Chests";               // every box keeps Min Spacing from every other box

    // The snow's chest rules: a copy of the desert's rule, its name and count for the snow. More (and more of the better
    // boxes) than the desert's 30 / 12 / 4 / 2: the snow is the farthest and best place.
    static readonly (string desert, string snow, int count)[] SnowChests =
    {
        ("ChestsDesert", "ChestsSnow", 40),
        ("Rare Chests Desert", "Rare Chests Snow", 20),
        ("Epic Chests Desert", "Epic Chests Snow", 8),
        ("Legendary Chests Desert", "Legendary Chests Snow", 4),
    };
    const string GrassSource = "PT_Grass_01";                              // Polytope's grass blades picture (found by name)
    const string GrassPath = "Assets/Effects/Grass/GrassBlades.png";       // our copy, with clamped edges

    // GameScene's own map helpers, by name (its walls are sized for its terrain; the others only mark places on it).
    static readonly string[] OldMapObjects = { "Boundaries", "Spawn", "Ore Testing" };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build New World Scene", false, MenuOrder.BuildNewWorldScene)]
    static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldScenePath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no " + OldScenePath + " to make the new world from.", "OK");
            return;
        }
        bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(NewScenePath) != null;
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes " + NewScenePath + ", the title screen's \"New world\":\n\n" +
                "- a copy of GameScene's game (player, HUD, menus, sun, spawners)\n" +
                "- without its hand-made map (terrain, placed trees, rocks, ores, chests, walls)\n" +
                "- with the World Generator: a new random world every game that never ends (made in squares as you " +
                "walk: a grassland start, rocky hills, farther out the desert and farthest the snow; rock caves with " +
                "stone, copper and iron), with no barrier\n" +
                "- the desert's content (Tools > World > Build Desert Content): sandstone caves with gold and hard " +
                "sandstone rocks (the turtles come at night from night 2, with GameScene's night spawner)\n" +
                "- the snow's content (Tools > World > Build Snow Content): snowy pines, frosty rocks, snow caves with " +
                "diamonds (Diamond tools can then be crafted) and more and better chests than the desert\n" +
                "- a 100 m view distance (ViewDistance on the Main Camera, with a mist)\n" +
                "- the rock caves (Assets/Prefabs/World/Caves), its sky light, and a place in Build Settings\n\n" +
                (exists ? "It replaces the ProceduralGenerateScene you have (changes made by hand there are lost).\n\n" : "") +
                "Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // unsaved changes: save them, or stop
        string reopen = SceneManager.GetActiveScene().path;

        List<GameObject> caves = RockCaveBuilder.BuildAll(out string caveReport);
        DesertBuilder.Parts desert = DesertBuilder.BuildAll();
        SnowBuilder.Parts snow = SnowBuilder.BuildAll();
        CopyScene(exists);
        Scene scene = EditorSceneManager.OpenScene(NewScenePath, OpenSceneMode.Single);

        Terrain oldTerrain = PaintedTerrain(scene);
        WorldGenerator generator = AddGenerator(oldTerrain, caves, desert, snow, scene, out string layerReport);   // before the old map goes
        List<string> removed = RemoveOldMap(scene);
        string scatter = SetUpScatter(scene, generator, desert.sandstone, snow);
        foreach (AnimalSpawner spawner in FindAll<AnimalSpawner>(scene)) SetField(spawner, "terrain", null);   // whichever terrain is there
        string monsters = "Night monsters: GameScene's EnemySpawner (and its NightMonsters: the turtle from night 2) came with the copy.";
        string view = AddViewDistance(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string light = SkyLightingSetup.SaveSkyLight(scene);
        AddToBuildSettings();
        IncludeGrassShaders();

        if (!string.IsNullOrEmpty(reopen) && reopen != NewScenePath) EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);

        Debug.Log(ToolName + ": " + NewScenePath + " made from GameScene. Removed the old map: " +
                  (removed.Count > 0 ? string.Join(", ", removed) : "nothing") + ". " + layerReport + " Caves: " + caveReport +
                  ". Desert: " + desert.report + ". Snow: " + snow.report + ". " + scatter + " " + monsters + " " + view +
                  " Sky light: " + light +
                  ". It's in Build Settings. Test: MainTitleScene > Play > " +
                  "NEW GAME > Map: New world > START GAME (rebuild the title screen first: Tools > UI > Build Main Title Scene).");
        if (caves.Count == 0)
        {
            EditorUtility.DisplayDialog(ToolName, "The new world has no caves: " + caveReport, "OK");
        }
    }

    // ===== My methods =====

    // GameScene copied to the new scene's file. An existing file is overwritten but keeps its ID (.meta), so Build
    // Settings and anything else linked to it stay linked.
    static void CopyScene(bool exists)
    {
        if (exists)
        {
            File.Copy(OldScenePath, NewScenePath, true);
            AssetDatabase.ImportAsset(NewScenePath, ImportAssetOptions.ForceUpdate);
        }
        else AssetDatabase.CopyAsset(OldScenePath, NewScenePath);
    }

    // The World Generator object, filled in: the old terrain's textures (same order) and material, the caves, the ores,
    // ObjectScatter and the player. "report" says which texture is which, for the Console.
    static WorldGenerator AddGenerator(Terrain oldTerrain, List<GameObject> caves, DesertBuilder.Parts desert, SnowBuilder.Parts snow,
                                       Scene scene, out string report)
    {
        GameObject go = new GameObject("World Generator");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetAsFirstSibling();
        WorldGenerator generator = go.AddComponent<WorldGenerator>();

        TerrainLayer[] layers = oldTerrain != null && oldTerrain.terrainData != null ? oldTerrain.terrainData.terrainLayers : FindLayers();
        SerializedObject so = new SerializedObject(generator);   // the names in quotes must match WorldGenerator's fields
        SerializedProperty list = so.FindProperty("layers");
        list.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];

        List<string> found = new List<string>();
        foreach ((string field, string word) in new[] { ("grassLayer", "grass"), ("sandLayer", "sand"), ("rockLayer", "rock"),
                                                       ("snowLayer", "snow"), ("mudLayer", "mud") })
        {
            int index = LayerIndex(layers, word);
            so.FindProperty(field).intValue = index;
            found.Add(word + " " + (index >= 0 ? index.ToString() : "missing"));
        }
        if (oldTerrain != null) so.FindProperty("terrainMaterial").objectReferenceValue = oldTerrain.materialTemplate;
        Texture2D grass = GrassTexture();
        so.FindProperty("grassTexture").objectReferenceValue = grass;

        SerializedProperty caveList = so.FindProperty("cavePrefabs");
        caveList.arraySize = caves.Count;
        for (int i = 0; i < caves.Count; i++) caveList.GetArrayElementAtIndex(i).objectReferenceValue = caves[i];
        so.FindProperty("stoneOre").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));
        so.FindProperty("copperOre").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(CopperPath));
        so.FindProperty("ironOre").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(IronPath));
        SerializedProperty desertCaves = so.FindProperty("desertCavePrefabs");   // the desert's (DesertBuilder)
        desertCaves.arraySize = desert.caves.Count;
        for (int i = 0; i < desert.caves.Count; i++) desertCaves.GetArrayElementAtIndex(i).objectReferenceValue = desert.caves[i];
        so.FindProperty("goldOre").objectReferenceValue = desert.goldOre;
        so.FindProperty("sandstoneOre").objectReferenceValue = desert.sandstone;
        SerializedProperty snowCaves = so.FindProperty("snowCavePrefabs");   // the snow's (SnowBuilder)
        snowCaves.arraySize = snow.caves.Count;
        for (int i = 0; i < snow.caves.Count; i++) snowCaves.GetArrayElementAtIndex(i).objectReferenceValue = snow.caves[i];
        so.FindProperty("diamondOre").objectReferenceValue = snow.diamondOre;
        so.FindProperty("scatter").objectReferenceValue = Find<ObjectScatter>(scene);
        PlayerController player = Find<PlayerController>(scene);
        so.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        report = "Ground textures: " + layers.Length + " from " + (oldTerrain != null ? oldTerrain.name : "the project") +
                 " (" + string.Join(", ", found) + "). Grass: " + (grass != null ? GrassPath : "none (no " + GrassSource + " picture found)") + ".";
        return generator;
    }

    // GameScene's painted terrain: the one with the most ground textures (the Terrain (1) inside Sanyam's WorldMap).
    // (GameScene also has an unpainted top-level Terrain; taking the first terrain found gave the new world no textures
    // at all: a white checkerboard ground, and so no trees and no deer.) Null = no terrain has any.
    static Terrain PaintedTerrain(Scene scene)
    {
        Terrain best = null;
        int most = 0;
        foreach (Terrain terrain in FindAll<Terrain>(scene))
        {
            if (terrain.terrainData == null) continue;
            int count = 0;
            foreach (TerrainLayer layer in terrain.terrainData.terrainLayers) if (layer != null) count++;
            if (count <= most) continue;
            most = count;
            best = terrain;
        }
        return best;
    }

    // The grass blades picture for the terrain grass: our copy of Polytope's PT_Grass_01 (made once, with clamped edges,
    // so the blades' bottoms don't show as a line along the top of each tuft). Null = the pack isn't in the project.
    static Texture2D GrassTexture()
    {
        Texture2D copy = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassPath);
        if (copy != null) return copy;

        string source = null;
        foreach (string guid in AssetDatabase.FindAssets(GrassSource + " t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == GrassSource) source = path;
        }
        if (source == null) return null;

        MakeFolders(FolderOf(GrassPath));
        if (!AssetDatabase.CopyAsset(source, GrassPath)) return null;
        TextureImporter importer = AssetImporter.GetAtPath(GrassPath) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(GrassPath);
    }

    // The ground textures when no terrain in GameScene has any: the project's own Terrain Layers (not the imported
    // packs'), in file name order (NewLayer = grass, NewLayer 1 = sand ... the order GameScene's terrain had them).
    static TerrainLayer[] FindLayers()
    {
        List<string> paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/Imported3D/")) paths.Add(path);
        }
        paths.Sort(System.StringComparer.OrdinalIgnoreCase);
        List<TerrainLayer> layers = new List<TerrainLayer>();
        foreach (string path in paths)
        {
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer != null) layers.Add(layer);
        }
        return layers.ToArray();
    }

    // The place in the list of the texture whose picture (or name) has this word in it (e.g. "Grass"), or -1.
    static int LayerIndex(TerrainLayer[] layers, string word)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            string picture = layers[i].diffuseTexture != null ? layers[i].diffuseTexture.name : "";
            if (picture.ToLower().Contains(word) || layers[i].name.ToLower().Contains(word)) return i;
        }
        return -1;
    }

    // Removes GameScene's hand-made map from the copy: every top object that holds a terrain, a resource (tree, rock,
    // ore, bush), a chest, an item lying around or an animal, building pieces, and the old map's helpers by name.
    // Returns their names.
    static List<string> RemoveOldMap(Scene scene)
    {
        List<string> removed = new List<string>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!IsOldMap(root)) continue;
            removed.Add(root.name);
            Object.DestroyImmediate(root);
        }
        return removed;
    }

    static bool IsOldMap(GameObject root)
    {
        if (root.GetComponent<WorldGenerator>() != null) return false;
        if (System.Array.IndexOf(OldMapObjects, root.name) >= 0) return true;
        if (root.GetComponentInChildren<Terrain>(true) != null) return true;
        if (root.GetComponentInChildren<ResourceNode>(true) != null) return true;    // trees, rocks, ores, bushes, deer
        if (root.GetComponentInChildren<PerkChest>(true) != null) return true;       // Marko's chests
        if (root.GetComponentInChildren<ItemPickup>(true) != null) return true;      // apples and items lying around
        if (root.GetComponentInChildren<WildAnimal>(true) != null) return true;
        if (root.GetComponentInChildren<WorldBarrier>(true) != null) return true;    // the old map's barrier (Add World Barrier)
        string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        return !string.IsNullOrEmpty(source) && source.StartsWith(BuildingFolder);   // a building piece (e.g. a floor)
    }

    // ObjectScatter (Sanyam's): no terrain of its own (the World Generator switches it off and uses its rules chunk by
    // chunk), no plain stone rocks in the desert (its Desert Rocks rule is left out of this scene; GameScene keeps it),
    // a Hill Rocks rule (stone rocks on the hills' rock texture), a Sandstone Rocks rule (the desert's own hard rock, on
    // the sand) and the snow's rules (snowy conifers, Snow Rocks, the snow's chests) if it hasn't got them. Its other
    // rules stay as in GameScene. Returns a sentence for the Console.
    static string SetUpScatter(Scene scene, WorldGenerator generator, GameObject sandstone, SnowBuilder.Parts snow)
    {
        ObjectScatter scatter = Find<ObjectScatter>(scene);
        if (scatter == null) return "No ObjectScatter in GameScene: the new world gets no trees, bushes, rocks or chests.";

        SerializedObject so = new SerializedObject(scatter);   // the names in quotes must match ObjectScatter's fields
        so.FindProperty("terrain").objectReferenceValue = null;
        SerializedObject layers = new SerializedObject(generator);
        int rockLayer = layers.FindProperty("rockLayer").intValue;
        int sandLayer = layers.FindProperty("sandLayer").intValue;
        int snowLayer = layers.FindProperty("snowLayer").intValue;
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));

        SerializedProperty rules = so.FindProperty("rules");
        List<string> leftOut = new List<string>();
        for (int i = rules.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty rule = rules.GetArrayElementAtIndex(i);
            bool onSand = sandLayer >= 0 && rule.FindPropertyRelative("terrainLayer").intValue == sandLayer;
            if (!onSand || !IsStone(rule.FindPropertyRelative("prefab").objectReferenceValue as GameObject)) continue;
            leftOut.Add(rule.FindPropertyRelative("name").stringValue);
            rules.DeleteArrayElementAtIndex(i);
        }

        string added = "";
        if (rockLayer >= 0 && stone != null && AddRule(rules, HillRule, stone, 60, rockLayer, 0.5f, 40f, 12f, 0.8f, 1.6f))
            added += " + " + HillRule + " (60 stone rocks on the hills)";
        if (sandLayer >= 0 && sandstone != null && AddRule(rules, SandstoneRule, sandstone, 40, sandLayer, 0.6f, 30f, 16f, 1f, 1.6f))
            added += " + " + SandstoneRule + " (40 hard sandstone rocks in the desert)";
        if (snowLayer >= 0) added += AddSnowRules(rules, snow, snowLayer);
        so.ApplyModifiedPropertiesWithoutUndo();
        string removed = leftOut.Count > 0 ? ", left out " + string.Join(", ", leftOut) + " (plain stones in the desert)" : "";
        return "ObjectScatter: " + rules.arraySize + " rules" + added + removed + ".";
    }

    // The snow's rules (on the snow texture; the World Generator grows them only in the snow biome): the chests first
    // (copies of the desert's chest rules, right behind them, with the snow's counts), then the snowy conifers (one rule
    // per look, one forest: spacing group Trees, in patches like the grassland's) and Snow Rocks. Rules already there
    // are left as they are. Returns what was added, for the Console.
    static string AddSnowRules(SerializedProperty rules, SnowBuilder.Parts snow, int snowLayer)
    {
        List<string> added = new List<string>();
        string after = LastChestRule(rules);
        foreach ((string desert, string name, int count) in SnowChests)
        {
            string from = FindRule(rules, desert) >= 0 ? desert : desert == "ChestsDesert" ? "ChestsForest" : null;
            if (from != null && after != null && CopyRule(rules, from, after, name, count, snowLayer)) added.Add(name + " " + count);
            if (FindRule(rules, name) >= 0) after = name;
        }
        if (FindRule(rules, "Rare Chests Desert") < 0)
            added.Add("no rare / epic / legendary boxes in the snow: run Tools > World > Build Loot Chests, then this again");

        int shares = 0;
        foreach (GameObject tree in snow.trees) shares += GrasslandTreeBuilder.SnowShare(tree);
        foreach (GameObject tree in snow.trees)
        {
            int count = Mathf.RoundToInt(SnowTrees * (float)GrasslandTreeBuilder.SnowShare(tree) / Mathf.Max(1, shares));
            string name = SnowTreeRule + tree.name.Replace("TreeNode ", "");
            if (AddRule(rules, name, tree, count, snowLayer, 0.7f, 25f, 9f, 0.85f, 1.2f, "Trees", 120f)) added.Add(name + " " + count);
        }
        if (snow.snowStone != null && AddRule(rules, SnowRockRule, snow.snowStone, 40, snowLayer, 0.6f, 30f, 14f, 0.8f, 1.5f))
            added.Add(SnowRockRule + " 40");
        return added.Count > 0 ? " + snow: " + string.Join(", ", added) : "";
    }

    // The last box rule in the list (the snow's chest rules go right behind it), or null when there's none.
    static string LastChestRule(SerializedProperty rules)
    {
        string last = null;
        int at = -1;
        foreach (string name in new[] { "ChestsForest", "Rare Chests Grass", "ChestsDesert", "Rare Chests Desert", "Epic Chests Desert", "Legendary Chests Desert" })
        {
            int i = FindRule(rules, name);
            if (i <= at) continue;
            at = i;
            last = name;
        }
        return last;
    }

    // A copy of the rule "from" (its prefab, spacing, height and the rest) right behind the rule "after", renamed, on
    // another texture, with its own count, in the boxes' spacing group. Only if no rule has that name yet. True = added.
    static bool CopyRule(SerializedProperty rules, string from, string after, string name, int count, int layer)
    {
        if (FindRule(rules, name) >= 0) return false;
        int source = FindRule(rules, from);
        int behind = FindRule(rules, after);
        if (source < 0 || behind < 0) return false;
        rules.InsertArrayElementAtIndex(source);               // the copy goes right behind its source...
        rules.MoveArrayElement(source + 1, behind + 1);        // ...then right behind "after"
        SerializedProperty rule = rules.GetArrayElementAtIndex(behind + 1);
        rule.FindPropertyRelative("name").stringValue = name;
        rule.FindPropertyRelative("count").intValue = count;
        rule.FindPropertyRelative("terrainLayer").intValue = layer;
        rule.FindPropertyRelative("spacingGroup").stringValue = ChestGroup;
        return true;
    }

    static int FindRule(SerializedProperty rules, string name)
    {
        for (int i = 0; i < rules.arraySize; i++)
        {
            if (rules.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return i;
        }
        return -1;
    }

    // A new rule at the end of ObjectScatter's list (the names in quotes must match its ScatterRule's fields), unless one
    // with that name is there already. group = its Spacing Group (empty = its prefab), patch = its Patch Size (0 = no
    // patches). True = added.
    static bool AddRule(SerializedProperty rules, string name, GameObject prefab, int count, int layer, float minStrength,
                        float maxSlope, float spacing, float minSize, float maxSize, string group = "", float patch = 0f)
    {
        if (FindRule(rules, name) >= 0) return false;
        rules.InsertArrayElementAtIndex(rules.arraySize);
        SerializedProperty rule = rules.GetArrayElementAtIndex(rules.arraySize - 1);
        rule.FindPropertyRelative("name").stringValue = name;
        rule.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        rule.FindPropertyRelative("count").intValue = count;
        rule.FindPropertyRelative("yOffset").floatValue = 0f;
        rule.FindPropertyRelative("terrainLayer").intValue = layer;
        rule.FindPropertyRelative("minStrength").floatValue = minStrength;
        rule.FindPropertyRelative("maxSlope").floatValue = maxSlope;
        rule.FindPropertyRelative("minSpacing").floatValue = spacing;
        rule.FindPropertyRelative("spacingGroup").stringValue = group;
        rule.FindPropertyRelative("clearance").floatValue = 3f;
        rule.FindPropertyRelative("patchSize").floatValue = patch;
        rule.FindPropertyRelative("openGround").floatValue = 0.35f;
        rule.FindPropertyRelative("minSize").floatValue = minSize;
        rule.FindPropertyRelative("maxSize").floatValue = maxSize;
        return true;
    }

    // The new world's line of sight: ViewDistance on the Main Camera (100 m: nothing further is drawn, a mist hides the
    // edge), and the scene's fog saved on, Linear, with the same distances. Unity leaves fog out of a built game when no
    // scene in it has fog on (GameScene's is off), and the mist is that fog. Returns a sentence for the Console.
    static string AddViewDistance(Scene scene)
    {
        Camera main = null;
        foreach (Camera cam in FindAll<Camera>(scene))
        {
            if (main == null && cam.CompareTag("MainCamera")) main = cam;
        }
        if (main == null) return "No Main Camera found, so no view distance (everything far away is drawn too).";

        ViewDistance view = main.GetComponent<ViewDistance>();
        if (view == null) view = main.gameObject.AddComponent<ViewDistance>();
        SerializedObject so = new SerializedObject(view);   // the names in quotes must match ViewDistance's fields
        float distance = so.FindProperty("distance").floatValue;
        float clear = so.FindProperty("clearShare").floatValue;

        RenderSettings.fog = true;   // the open scene's settings: the new world (it's saved right after this)
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = distance * clear;
        RenderSettings.fogEndDistance = distance;
        return "View distance: " + distance.ToString("0") + " m (ViewDistance on " + main.name + ", fog saved on).";
    }

    // Is this prefab a stone rock (a ResourceNode of type Stone, e.g. StoneNode or StoneNode Desert)?
    static bool IsStone(GameObject prefab)
    {
        ResourceNode node = prefab != null ? prefab.GetComponentInChildren<ResourceNode>(true) : null;
        return node != null && node.resourceType == ResourceNode.ResourceType.Stone;
    }

    // Unity draws terrain grass with its own hidden shaders. A built game only keeps the shaders its scenes use, and no
    // saved scene has terrain grass (the World Generator makes it while playing), so they're added to Edit > Project
    // Settings > Graphics > Always Included Shaders (once). Without this the grass would be missing in a build.
    static void IncludeGrassShaders()
    {
        Object graphics = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (graphics == null) return;
        SerializedObject so = new SerializedObject(graphics);
        SerializedProperty list = so.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) return;
        foreach (string name in new[] { "Hidden/TerrainEngine/Details/WavingDoublePass", "Hidden/TerrainEngine/Details/BillboardWavingDoublePass" })
        {
            Shader shader = Shader.Find(name);
            if (shader == null) continue;
            bool there = false;
            for (int i = 0; i < list.arraySize; i++) there |= list.GetArrayElementAtIndex(i).objectReferenceValue == shader;
            if (there) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == NewScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(NewScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // The first T in the scene (switched off ones too), or null.
    static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> all = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) all.AddRange(root.GetComponentsInChildren<T>(true));
        return all;
    }

    static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) return;
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
