/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Loot Chests. It asks first, saves your open scenes, changes both game scenes and
              saves them, then opens your scenes again. Run it again any time: the boxes' models are rebuilt, and
              numbers you changed (Perk Data, prices, spawn counts) stay. Guide: Docs/Loot Chests Setup.md
Purpose: Three kinds of loot box, in one click:
           - Perk Data for the new perks in Assets/Config/Perks (only made if missing; pictures drawn by IconArt):
               common: Second Wind, Precision (needs Critical Strike), Spring Legs (needs Glide)
               rare:   Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler
               epic:   Last Stand, Critical Strike, Glide (each once a life)
           - the common box = Marko's Chest prefab: Tony's ChestTier (2 coins, +1 for every perk bought) and the 3 new
             common perks added to its list (his 4 stay);
           - Assets/Prefabs/WorldItems/Rare Chest.prefab (DuNguyn's blue loot box, 10 coins, x1.5) and Epic Chest.prefab
             (the purple one, 100 coins, x2, can reroll): Marko's PerkChest with their perks, ChestTier, standing on the
             ground (StandOnGround) and a blue / purple outline (InteractOutline); Built-in copies of the pack's URP
             materials, glow included, in Assets/Materials/Chests; the pack's 4096 px textures imported at 1024 (a chest
             doesn't need more, and the web game loads faster). The orange loot box isn't used yet;
           - the perk panel: the REROLL button and the box's name as the title (PerkPanelBuilder.UpdatePrefab);
           - each game scene (the Build Settings scenes with Sanyam's ObjectScatter: GameScene, ProceduralGenerateScene):
             its chest rules: grassland (ChestsForest) 10 common + 3 rare, desert (ChestsDesert, moved onto the sand
             layer: it used the grass one) 15 common + 6 rare + 2 epic, all in one spacing group; PerkEffects and
             PlayerGlide (switched off) on the Player; PlayerArmor's set bonuses brought up to date. Then it's saved.
Requirement: LootChestBuilder.cs x-> PerkData.cs, PerkChest.cs, ChestTier.cs, PerkEffects.cs, PlayerGlide.cs, PlayerArmor.cs, PlayerInventory.cs, StandOnGround.cs, InteractOutline.cs, ObjectScatter.cs, WorldGenerator.cs, IconArt.cs, PerkPanelBuilder.cs, MaterialBuildHelper.cs, MeshBounds.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;   // FindPrefabPath, FolderOf, MakeFolders, CheckTMP

public static class LootChestBuilder
{
    // One new perk's Perk Data.
    struct PerkSpec
    {
        public string file;              // Assets/Config/Perks/<file>.asset
        public string name, description;
        public ChestPerkType kind;
        public float value;
        public string requires;          // another perk's file (empty = always offered)
        public int maxPicks;             // 1 = once a life, 0 = no limit
        public System.Func<Sprite> icon;
    }

    // One loot box prefab.
    struct ChestSpec
    {
        public string file;              // Assets/Prefabs/WorldItems/<file>.prefab
        public string model;             // DuNguyn's model
        public LootTier tier;
        public string boxName;
        public Color color;              // the panel's title and the outline
        public int startPrice;
        public PriceGrowth growth;
        public float growBy;
        public bool reroll;
        public float width;              // meters across (the common chest is about 1 m)
        public string[] perks;           // Perk Data files
    }

    // ===== Fields =====
    const string ToolName = "Build Loot Chests";
    const string PerkFolder = "Assets/Config/Perks";
    const string ChestFolder = "Assets/Prefabs/WorldItems";
    const string CommonChestPath = ChestFolder + "/Chest.prefab";   // Marko's chest (found by name if moved)
    const string MaterialFolder = "Assets/Materials/Chests";
    const string ModelName = "Model";
    const int TextureSize = 1024;        // the loot boxes' textures are imported at most this big
    const string SpacingGroup = "Chests";   // every box keeps Min Spacing from every other box

    static readonly PerkSpec[] Perks =
    {
        // Common box (with Marko's Vitality, Stamina, Agility and Fireball)
        new PerkSpec { file = "Perk_SecondWind", name = "Second Wind", description = "Stamina recharges 30% faster.",
                       kind = ChestPerkType.SecondWind, value = 0.3f, icon = IconArt.PerkSecondWind },
        new PerkSpec { file = "Perk_Precision", name = "Precision", description = "+1.25% chance for a critical strike (double damage).",
                       kind = ChestPerkType.CriticalStrike, value = 0.0125f, requires = "Perk_CriticalStrike", icon = IconArt.PerkPrecision },
        new PerkSpec { file = "Perk_SpringLegs", name = "Spring Legs", description = "Jump 25% higher.",
                       kind = ChestPerkType.SpringLegs, value = 0.25f, requires = "Perk_Glide", icon = IconArt.PerkSpringLegs },
        // Rare box
        new PerkSpec { file = "Perk_BountyHunter", name = "Bounty Hunter", description = "+1 coin for every monster you kill.",
                       kind = ChestPerkType.BountyHunter, value = 1f, icon = IconArt.PerkBountyHunter },
        new PerkSpec { file = "Perk_Gourmet", name = "Gourmet", description = "Food heals, feeds and refreshes 10% more.",
                       kind = ChestPerkType.Gourmet, value = 0.1f, icon = IconArt.PerkGourmet },
        new PerkSpec { file = "Perk_Lumberjack", name = "Lumberjack", description = "+1 wood from every tree you chop down.",
                       kind = ChestPerkType.Lumberjack, value = 1f, icon = IconArt.PerkLumberjack },
        new PerkSpec { file = "Perk_Prospector", name = "Prospector", description = "+1 stone or ore from every rock you break.",
                       kind = ChestPerkType.Prospector, value = 1f, icon = IconArt.PerkProspector },
        new PerkSpec { file = "Perk_Sprinter", name = "Sprinter", description = "Sprint 15% faster, using 25% less stamina.",
                       kind = ChestPerkType.Sprinter, value = 0.15f, icon = IconArt.PerkSprinter },
        new PerkSpec { file = "Perk_Brawler", name = "Brawler", description = "Punches knock monsters back 10% farther.",
                       kind = ChestPerkType.Brawler, value = 0.1f, icon = IconArt.PerkBrawler },
        // Epic box
        new PerkSpec { file = "Perk_LastStand", name = "Last Stand", description = "Once a life, a killing blow leaves you at 1 health, safe for 2 seconds.",
                       kind = ChestPerkType.LastStand, value = 2f, maxPicks = 1, icon = IconArt.PerkLastStand },
        new PerkSpec { file = "Perk_CriticalStrike", name = "Critical Strike", description = "5% of your hits do double damage (a red burst). Common boxes then offer Precision.",
                       kind = ChestPerkType.CriticalStrike, value = 0.05f, maxPicks = 1, icon = IconArt.PerkCritical },
        new PerkSpec { file = "Perk_Glide", name = "Glide", description = "Hold Space in the air to float down; tap it twice to dive. Common boxes then offer Spring Legs.",
                       kind = ChestPerkType.Glide, value = 0f, maxPicks = 1, icon = IconArt.PerkGlide },
    };

    static readonly string[] CommonAdds = { "Perk_SecondWind", "Perk_Precision", "Perk_SpringLegs" };

    static readonly ChestSpec[] Chests =
    {
        new ChestSpec
        {
            file = "Rare Chest", model = "SM_LootBox_01", tier = LootTier.Rare, boxName = "Rare box",
            color = new Color(0.35f, 0.65f, 1f), startPrice = 10, growth = PriceGrowth.Multiply, growBy = 1.5f, width = 1f,
            perks = new[] { "Perk_BountyHunter", "Perk_Gourmet", "Perk_Lumberjack", "Perk_Prospector", "Perk_Sprinter", "Perk_Brawler" },
        },
        new ChestSpec
        {
            file = "Epic Chest", model = "SM_LootBox_03", tier = LootTier.Epic, boxName = "Epic box",
            color = new Color(0.78f, 0.4f, 1f), startPrice = 100, growth = PriceGrowth.Multiply, growBy = 2f, reroll = true,
            width = 1.2f, perks = new[] { "Perk_LastStand", "Perk_CriticalStrike", "Perk_Glide" },
        },
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Loot Chests", false, MenuOrder.BuildLootChests)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        List<string> scenes = GameScenes();
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes three kinds of loot box:\n\n" +
                "- Common (Marko's chest): 2 coins, +1 for every perk bought; adds Second Wind, Precision (after Critical " +
                "Strike) and Spring Legs (after Glide) to its perks\n" +
                "- Rare (blue loot box): 10 coins, x1.5; Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler\n" +
                "- Epic (purple loot box): 100 coins, x2, REROLL; Last Stand, Critical Strike, Glide\n" +
                "- the perk panel: REROLL button and the box's name as the title\n\n" +
                "Then it changes and SAVES these scenes: " + (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "(none found)") +
                "\n(chest spawns: grassland 10 common + 3 rare, desert 15 common + 6 rare + 2 epic; PerkEffects and " +
                "PlayerGlide on the Player). Your open scenes are saved first. Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Dictionary<string, PerkData> perks = MakePerks(out List<string> perksMade);
        List<string> chestReports = new List<string>();
        Dictionary<LootTier, GameObject> prefabs = new Dictionary<LootTier, GameObject>();
        foreach (ChestSpec spec in Chests)
        {
            GameObject prefab = BuildChest(spec, perks, out string report);
            if (prefab != null) prefabs[spec.tier] = prefab;
            chestReports.Add(report);
        }
        string common = SetUpCommonChest(perks, out GameObject commonPrefab);
        string panel = PerkPanelBuilder.UpdatePrefab();
        AssetDatabase.SaveAssets();

        List<string> sceneReports = new List<string>();
        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenes) sceneReports.Add(SetUpScene(path, commonPrefab, prefabs));
        }
        finally
        {
            if (before != null && before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        }

        Debug.Log(ToolName + ": perks " + (perksMade.Count > 0 ? "made: " + string.Join(", ", perksMade) : "already there") +
                  ". " + string.Join(" ", chestReports) + " " + common + " Perk panel: " + panel + ". Scenes: " +
                  (sceneReports.Count > 0 ? string.Join(" ", sceneReports) : "none found (no Build Settings scene has ObjectScatter)") +
                  " Play: grassland chests are common or rare, the desert has epic ones too.");
    }

    // ===== My methods =====

    // The Perk Data for every new perk: an existing one is kept as it is (only an empty icon or Requires is filled in),
    // a missing one is made. Returns all of them by file name; "made" lists the new ones.
    static Dictionary<string, PerkData> MakePerks(out List<string> made)
    {
        made = new List<string>();
        MakeFolders(PerkFolder);
        Dictionary<string, PerkData> perks = new Dictionary<string, PerkData>();
        foreach (PerkSpec spec in Perks)
        {
            PerkData perk = FindPerk(spec.file);
            if (perk == null)
            {
                perk = ScriptableObject.CreateInstance<PerkData>();
                perk.displayName = spec.name;
                perk.description = spec.description;
                perk.perkType = spec.kind;
                perk.value = spec.value;
                perk.maxPicks = spec.maxPicks;
                AssetDatabase.CreateAsset(perk, PerkFolder + "/" + spec.file + ".asset");
                made.Add(spec.name);
            }
            if (perk.icon == null) perk.icon = spec.icon();
            EditorUtility.SetDirty(perk);
            perks[spec.file] = perk;
        }
        foreach (PerkSpec spec in Perks)   // second pass: every perk exists now, so Requires can point at it
        {
            PerkData perk = perks[spec.file];
            if (perk.requires == null && !string.IsNullOrEmpty(spec.requires) && perks.TryGetValue(spec.requires, out PerkData needed))
            {
                perk.requires = needed;
                EditorUtility.SetDirty(perk);
            }
        }
        AssetDatabase.SaveAssets();
        return perks;
    }

    static PerkData FindPerk(string file)
    {
        PerkData perk = AssetDatabase.LoadAssetAtPath<PerkData>(PerkFolder + "/" + file + ".asset");
        if (perk != null) return perk;
        foreach (string guid in AssetDatabase.FindAssets(file + " t:PerkData"))   // moved somewhere else
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == file) return AssetDatabase.LoadAssetAtPath<PerkData>(path);
        }
        return null;
    }

    // One loot box prefab, refilled in place if it exists: the model and its box are rebuilt every time; PerkChest,
    // ChestTier, StandOnGround and InteractOutline get the tool's numbers only when they're new (yours stay), and
    // PerkChest's list gets the box's perks it's missing.
    static GameObject BuildChest(ChestSpec spec, Dictionary<string, PerkData> perks, out string report)
    {
        GameObject model = FindModel(spec.model);
        if (model == null)
        {
            report = spec.file + ": DuNguyn's " + spec.model + " wasn't found (Assets/Imported3D/DuNguyn).";
            return null;
        }

        string path = FindPrefabPath(ChestFolder + "/" + spec.file + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.file);
        try
        {
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            Transform old = root.transform.Find(ModelName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // The model, sized to "width" across, its bottom in the middle at the root (so the root stands on the ground).
            GameObject look = (GameObject)Object.Instantiate(model, root.transform);
            look.name = ModelName;
            look.transform.localPosition = Vector3.zero;
            look.transform.localRotation = Quaternion.identity;
            look.transform.localScale = Vector3.one;
            foreach (Renderer part in look.GetComponentsInChildren<Renderer>(true))
                part.sharedMaterials = DrawableMaterials(part.sharedMaterials);
            Bounds seen = MeshBounds.Of(look);
            float across = Mathf.Max(seen.size.x, seen.size.z);
            if (across > 0.0001f) look.transform.localScale = Vector3.one * (spec.width / across);
            seen = MeshBounds.Of(look);
            look.transform.localPosition = -new Vector3(seen.center.x, seen.min.y, seen.center.z);
            seen = MeshBounds.Of(look);

            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.center = seen.center;
            box.size = seen.size;

            PerkChest chest = root.GetComponent<PerkChest>();
            bool newChest = chest == null;
            if (newChest) chest = root.AddComponent<PerkChest>();
            SerializedObject c = new SerializedObject(chest);   // the names in quotes must match Marko's PerkChest fields
            if (newChest)
            {
                c.FindProperty("interactKey").intValue = (int)KeyCode.E;
                c.FindProperty("interactionRange").floatValue = 3f;
                c.FindProperty("despawnAfterOpened").boolValue = true;
            }
            int added = AddPerks(c.FindProperty("availablePerks"), spec.perks, perks);
            c.ApplyModifiedPropertiesWithoutUndo();

            ChestTier tier = root.GetComponent<ChestTier>();
            if (tier == null) FillTier(root.AddComponent<ChestTier>(), spec.tier, spec.boxName, spec.color, spec.startPrice, spec.growth, spec.growBy, spec.reroll);

            StandOnGround ground = root.GetComponent<StandOnGround>();
            if (ground == null) ground = root.AddComponent<StandOnGround>();
            SerializedObject g = new SerializedObject(ground);
            g.FindProperty("footOffset").floatValue = 0f;   // the model's bottom is at the root
            g.ApplyModifiedPropertiesWithoutUndo();

            InteractOutline outline = root.GetComponent<InteractOutline>();
            if (outline == null)
            {
                outline = root.AddComponent<InteractOutline>();
                SerializedObject o = new SerializedObject(outline);
                o.FindProperty("color").colorValue = spec.color;
                o.FindProperty("showWhen").enumValueIndex = (int)OutlineShowWhen.LookAt;
                o.FindProperty("range").floatValue = 3f;   // shows exactly when E opens it (PerkChest's Interaction Range)
                o.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            report = spec.file + ": " + path + " (" + spec.width + " m " + spec.model + (added > 0 ? ", " + added + " perks added" : "") + ").";
            return saved;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // DuNguyn's model (the .fbx), or its prefab, by name.
    static GameObject FindModel(string modelName)
    {
        foreach (string filter in new[] { " t:Model", " t:Prefab" })
        {
            foreach (string guid in AssetDatabase.FindAssets(modelName + filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == modelName) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        return null;
    }

    // Each material Built-in can't draw (the pack's URP ones) swapped for a Standard copy with its glow, and their
    // textures imported at most TextureSize big.
    static Material[] DrawableMaterials(Material[] originals)
    {
        Material[] result = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            Material m = originals[i];
            result[i] = MaterialBuildHelper.CanDraw(m) ? m : MaterialBuildHelper.BuiltInCopy(m, MaterialFolder, null, true);
            ShrinkTextures(result[i]);
        }
        return result;
    }

    static void ShrinkTextures(Material m)
    {
        if (m == null) return;
        foreach (string property in m.GetTexturePropertyNames())
        {
            Texture texture = m.GetTexture(property);
            string path = texture != null ? AssetDatabase.GetAssetPath(texture) : null;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool changed = false;
            if (importer.maxTextureSize > TextureSize)
            {
                importer.maxTextureSize = TextureSize;
                changed = true;
            }
            TextureImporterPlatformSettings web = importer.GetPlatformTextureSettings("WebGL");
            if (web.overridden && web.maxTextureSize > TextureSize)
            {
                web.maxTextureSize = TextureSize;
                importer.SetPlatformTextureSettings(web);
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }
    }

    // Marko's common chest: ChestTier (only when it has none) and the new common perks added to its list.
    static string SetUpCommonChest(Dictionary<string, PerkData> perks, out GameObject prefab)
    {
        prefab = null;
        string path = FindPrefabPath(CommonChestPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "Common box: Marko's Chest prefab wasn't found.";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            PerkChest chest = root.GetComponent<PerkChest>();
            if (chest == null) return "Common box: " + path + " has no PerkChest.";
            SerializedObject c = new SerializedObject(chest);
            int added = AddPerks(c.FindProperty("availablePerks"), CommonAdds, perks);
            c.ApplyModifiedPropertiesWithoutUndo();
            bool newTier = root.GetComponent<ChestTier>() == null;
            if (newTier) FillTier(root.AddComponent<ChestTier>(), LootTier.Common, "Common box", new Color(0.86f, 0.87f, 0.92f), 2, PriceGrowth.Add, 1f, false);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            return "Common box: " + path + (newTier ? " got ChestTier (2 coins, +1)" : " kept its ChestTier") +
                   (added > 0 ? ", " + added + " perks added." : ", its perks were there already.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Adds the perks the list is missing (by file name) at its end. Returns how many.
    static int AddPerks(SerializedProperty list, string[] files, Dictionary<string, PerkData> perks)
    {
        int added = 0;
        foreach (string file in files)
        {
            if (!perks.TryGetValue(file, out PerkData perk)) continue;
            bool there = false;
            for (int i = 0; i < list.arraySize && !there; i++) there = list.GetArrayElementAtIndex(i).objectReferenceValue == perk;
            if (there) continue;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = perk;
            added++;
        }
        return added;
    }

    static void FillTier(ChestTier tier, LootTier kind, string boxName, Color color, int startPrice, PriceGrowth growth, float growBy, bool reroll)
    {
        SerializedObject t = new SerializedObject(tier);   // the names in quotes must match ChestTier's fields
        t.FindProperty("tier").enumValueIndex = (int)kind;
        t.FindProperty("boxName").stringValue = boxName;
        t.FindProperty("color").colorValue = color;
        t.FindProperty("startPrice").intValue = startPrice;
        t.FindProperty("growth").enumValueIndex = (int)growth;
        t.FindProperty("growBy").floatValue = growBy;
        t.FindProperty("canReroll").boolValue = reroll;
        t.ApplyModifiedPropertiesWithoutUndo();
    }

    // The Build Settings scenes with Sanyam's ObjectScatter in them (found in the scene files, without opening them).
    static List<string> GameScenes()
    {
        List<string> found = new List<string>();
        string script = AssetDatabase.FindAssets("ObjectScatter t:MonoScript").Length > 0
            ? AssetDatabase.AssetPathToGUID(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("ObjectScatter t:MonoScript")[0]))
            : null;
        if (string.IsNullOrEmpty(script)) return found;
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;
            if (File.ReadAllText(entry.path).Contains(script)) found.Add(entry.path);
        }
        return found;
    }

    // Opens one game scene, sets up its chest rules and its Player, and saves it. Returns a report.
    static string SetUpScene(string path, GameObject commonPrefab, Dictionary<LootTier, GameObject> prefabs)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        List<string> done = new List<string>();

        ObjectScatter scatter = Object.FindFirstObjectByType<ObjectScatter>(FindObjectsInactive.Include);
        if (scatter != null) done.Add(SetUpRules(scatter, commonPrefab, prefabs));
        else done.Add("no ObjectScatter");

        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        if (player != null)
        {
            if (player.GetComponent<PerkEffects>() == null) player.gameObject.AddComponent<PerkEffects>();
            if (player.GetComponent<PlayerGlide>() == null) player.gameObject.AddComponent<PlayerGlide>().enabled = false;   // Glide switches it on
            PlayerArmor armor = player.GetComponent<PlayerArmor>();
            if (armor != null && armor.UpdateOldBonuses())
            {
                EditorUtility.SetDirty(armor);
                done.Add("armor set bonuses now give coins");
            }
            done.Add("PerkEffects + PlayerGlide on the Player");
        }
        else done.Add("no Player");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + " (" + string.Join(", ", done) + ", saved).";
    }

    // Sanyam's ObjectScatter chest rules: the desert rule onto the sand layer with 15 boxes, the rare and epic rules
    // added after the common ones (copies of their settings), all in one spacing group. Counts you changed stay.
    static string SetUpRules(ObjectScatter scatter, GameObject commonPrefab, Dictionary<LootTier, GameObject> prefabs)
    {
        SerializedObject so = new SerializedObject(scatter);
        SerializedProperty rules = so.FindProperty("rules");   // the names in quotes must match Sanyam's ObjectScatter fields
        int sand = SandLayer(scatter);
        int grassRule = FindRule(rules, "ChestsForest");
        int desertRule = FindRule(rules, "ChestsDesert");
        if (grassRule < 0 && desertRule < 0) return "no ChestsForest / ChestsDesert rule";

        if (grassRule >= 0) rules.GetArrayElementAtIndex(grassRule).FindPropertyRelative("spacingGroup").stringValue = SpacingGroup;
        if (desertRule >= 0)
        {
            SerializedProperty desert = rules.GetArrayElementAtIndex(desertRule);
            desert.FindPropertyRelative("terrainLayer").intValue = sand;
            SerializedProperty count = desert.FindPropertyRelative("count");
            count.intValue = Mathf.Max(count.intValue, 15);
            desert.FindPropertyRelative("spacingGroup").stringValue = SpacingGroup;
        }

        prefabs.TryGetValue(LootTier.Rare, out GameObject rare);
        prefabs.TryGetValue(LootTier.Epic, out GameObject epic);
        List<string> rulesMade = new List<string>();
        if (rare != null && grassRule >= 0) AddRuleAfter(rules, "ChestsForest", "Rare Chests Grass", rare, 3, -1, rulesMade);
        if (rare != null && desertRule >= 0) AddRuleAfter(rules, "ChestsDesert", "Rare Chests Desert", rare, 6, sand, rulesMade);
        string epicAfter = FindRule(rules, "Rare Chests Desert") >= 0 ? "Rare Chests Desert" : "ChestsDesert";
        if (epic != null && desertRule >= 0) AddRuleAfter(rules, epicAfter, "Epic Chests Desert", epic, 2, sand, rulesMade);
        so.ApplyModifiedPropertiesWithoutUndo();
        return "chest rules: desert on sand layer " + sand + (rulesMade.Count > 0 ? ", added " + string.Join(", ", rulesMade) : ", rare / epic rules were there");
    }

    // A copy of the rule named "after", put right behind it, as a new box rule (only if none has that name yet; an
    // existing one only gets its prefab, layer and group). layer -1 = keep the copied rule's layer.
    static void AddRuleAfter(SerializedProperty rules, string after, string name, GameObject prefab, int count, int layer, List<string> made)
    {
        int existing = FindRule(rules, name);
        SerializedProperty rule;
        if (existing >= 0) rule = rules.GetArrayElementAtIndex(existing);
        else
        {
            int template = FindRule(rules, after);
            if (template < 0) return;
            rules.InsertArrayElementAtIndex(template);   // the copy goes right behind it (both are the same for now)
            rule = rules.GetArrayElementAtIndex(template + 1);
            rule.FindPropertyRelative("name").stringValue = name;
            rule.FindPropertyRelative("count").intValue = count;
            made.Add(name + " x" + count);
        }
        rule.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        if (layer >= 0) rule.FindPropertyRelative("terrainLayer").intValue = layer;
        rule.FindPropertyRelative("spacingGroup").stringValue = SpacingGroup;
    }

    static int FindRule(SerializedProperty rules, string name)
    {
        for (int i = 0; i < rules.arraySize; i++)
        {
            if (rules.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return i;
        }
        return -1;
    }

    // The sand layer's number: from the scatter's terrain (a layer named sand), else the new world's generator (its
    // Sand Layer: the terrain is made when the game starts), else 1 (GameScene's order: grass, sand, snow, rock, mud).
    static int SandLayer(ObjectScatter scatter)
    {
        Terrain terrain = scatter.terrain;
        TerrainLayer[] layers = terrain != null && terrain.terrainData != null ? terrain.terrainData.terrainLayers : null;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] != null && layers[i].name.ToLowerInvariant().Contains("sand")) return i;
        }
        WorldGenerator generator = Object.FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Include);
        if (generator != null)
        {
            SerializedProperty sand = new SerializedObject(generator).FindProperty("sandLayer");
            if (sand != null) return sand.intValue;
        }
        return 1;
    }
}
