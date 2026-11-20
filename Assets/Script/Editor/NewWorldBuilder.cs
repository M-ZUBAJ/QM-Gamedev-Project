/*
First Date: 20/11/2026
Date: 20/11/2026
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
             by hand, building pieces, and its walls (Boundaries) and helper objects (Spawn, Ore Testing);
           - plus the World Generator (WorldGenerator), given the old terrain's ground textures (same order, so
             ObjectScatter's rules work the same) and material, the rock caves (Tools > World > Build Rock Caves,
             run first) and the Stone / Copper / Iron rocks for them, and the grass picture (Polytope's grass blades,
             copied once to Assets/Effects/Grass/GrassBlades.png with clamped edges; Polytope's own file isn't changed);
             GameScene has two terrains (its painted Terrain and an unpainted one inside Sanyam's WorldMap): the
             textures come from the one with the most of them;
           - ObjectScatter (Sanyam's, his script unchanged) gets one more rule in this scene, Hill Rocks (stone rocks
             on the rocky hills), and no terrain of its own: the World Generator gives it the new one;
           - its light from the sky is saved (like Tools > World > Set Up Sky Lighting), and it's added to Build
             Settings, so the title screen can load it;
           - Unity's terrain grass shaders go into Project Settings > Graphics > Always Included Shaders (no saved
             scene has terrain grass, so a built game would leave them out).
Requirement: NewWorldBuilder.cs x-> WorldGenerator.cs, RockCaveBuilder.cs, SkyLightingSetup.cs, ObjectScatter.cs, AnimalSpawner.cs, PlayerController.cs, ResourceNode.cs, PerkChest.cs, ItemPickup.cs, WildAnimal.cs, UIBuildHelper.cs, MenuOrder.cs
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
                "- with the World Generator: a new random 1000 x 1000 m world every game (grassland start, desert, " +
                "rocky hills, rock caves with stone, copper and iron)\n" +
                "- the rock caves (Assets/Prefabs/World/Caves), its sky light, and a place in Build Settings\n\n" +
                (exists ? "It replaces the ProceduralGenerateScene you have (changes made by hand there are lost).\n\n" : "") +
                "Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // unsaved changes: save them, or stop
        string reopen = SceneManager.GetActiveScene().path;

        List<GameObject> caves = RockCaveBuilder.BuildAll(out string caveReport);
        CopyScene(exists);
        Scene scene = EditorSceneManager.OpenScene(NewScenePath, OpenSceneMode.Single);

        Terrain oldTerrain = PaintedTerrain(scene);
        WorldGenerator generator = AddGenerator(oldTerrain, caves, scene, out string layerReport);   // before the old map goes
        List<string> removed = RemoveOldMap(scene);
        string scatter = SetUpScatter(scene, generator);
        foreach (AnimalSpawner spawner in FindAll<AnimalSpawner>(scene)) SetField(spawner, "terrain", null);   // whichever terrain is there

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string light = SkyLightingSetup.SaveSkyLight(scene);
        AddToBuildSettings();
        IncludeGrassShaders();

        if (!string.IsNullOrEmpty(reopen) && reopen != NewScenePath) EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);

        Debug.Log(ToolName + ": " + NewScenePath + " made from GameScene. Removed the old map: " +
                  (removed.Count > 0 ? string.Join(", ", removed) : "nothing") + ". " + layerReport + " Caves: " + caveReport +
                  ". " + scatter + " Sky light: " + light + ". It's in Build Settings. Test: MainTitleScene > Play > " +
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
    static WorldGenerator AddGenerator(Terrain oldTerrain, List<GameObject> caves, Scene scene, out string report)
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
        so.FindProperty("scatter").objectReferenceValue = Find<ObjectScatter>(scene);
        PlayerController player = Find<PlayerController>(scene);
        so.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        report = "Ground textures: " + layers.Length + " from " + (oldTerrain != null ? oldTerrain.name : "the project") +
                 " (" + string.Join(", ", found) + "). Grass: " + (grass != null ? GrassPath : "none (no " + GrassSource + " picture found)") + ".";
        return generator;
    }

    // GameScene's painted terrain: the one with the most ground textures. (GameScene also has Sanyam's WorldMap, with
    // an unpainted Terrain (1) inside it; taking the first terrain found gave the new world no textures at all: a white
    // checkerboard ground, and so no trees and no deer.) Null = no terrain has any.
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
        string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        return !string.IsNullOrEmpty(source) && source.StartsWith(BuildingFolder);   // a building piece (e.g. a floor)
    }

    // ObjectScatter (Sanyam's): no terrain of its own (the World Generator gives it the new one when the game starts),
    // and a Hill Rocks rule (stone rocks on the hills' rock texture) if it hasn't got one. Its other rules stay as in
    // GameScene. Returns a sentence for the Console.
    static string SetUpScatter(Scene scene, WorldGenerator generator)
    {
        ObjectScatter scatter = Find<ObjectScatter>(scene);
        if (scatter == null) return "No ObjectScatter in GameScene: the new world gets no trees, bushes, rocks or chests.";

        SerializedObject so = new SerializedObject(scatter);   // the names in quotes must match ObjectScatter's fields
        so.FindProperty("terrain").objectReferenceValue = null;
        int rockLayer = new SerializedObject(generator).FindProperty("rockLayer").intValue;
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));

        SerializedProperty rules = so.FindProperty("rules");
        bool hasHillRule = false;
        for (int i = 0; i < rules.arraySize; i++) hasHillRule |= rules.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == HillRule;
        string added = "";
        if (!hasHillRule && rockLayer >= 0 && stone != null)
        {
            rules.InsertArrayElementAtIndex(rules.arraySize);
            SerializedProperty rule = rules.GetArrayElementAtIndex(rules.arraySize - 1);
            rule.FindPropertyRelative("name").stringValue = HillRule;
            rule.FindPropertyRelative("prefab").objectReferenceValue = stone;
            rule.FindPropertyRelative("count").intValue = 60;
            rule.FindPropertyRelative("yOffset").floatValue = 0f;
            rule.FindPropertyRelative("terrainLayer").intValue = rockLayer;
            rule.FindPropertyRelative("minStrength").floatValue = 0.5f;
            rule.FindPropertyRelative("maxSlope").floatValue = 40f;
            rule.FindPropertyRelative("minSpacing").floatValue = 12f;
            rule.FindPropertyRelative("spacingGroup").stringValue = "";
            rule.FindPropertyRelative("clearance").floatValue = 3f;
            rule.FindPropertyRelative("patchSize").floatValue = 0f;
            rule.FindPropertyRelative("openGround").floatValue = 0.35f;
            rule.FindPropertyRelative("minSize").floatValue = 0.8f;
            rule.FindPropertyRelative("maxSize").floatValue = 1.6f;
            added = " + " + HillRule + " (60 stone rocks on the hills)";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return "ObjectScatter: " + rules.arraySize + " rules" + added + ".";
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
