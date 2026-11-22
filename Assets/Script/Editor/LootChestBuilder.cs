/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Loot Chests. It asks first, saves your open scenes, changes both game scenes and
              saves them, then opens your scenes again. Run it again any time: the boxes' models and the roll pop-up are
              rebuilt, and numbers you changed (Perk Data, prices, bigger spawn counts) stay. Guide: Docs/Loot Chests Setup.md
Purpose: Four kinds of loot box and both chest systems (GameRules > New Chests, picked on the New Game page), in one click:
           - Perk Data for the new perks in Assets/Config/Perks (only made if missing; pictures drawn by IconArt):
               common: Second Wind, Precision (needs Critical Strike), Spring Legs (needs Glide)
               rare:   Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler, Attack Speed, Long Reach
                       (needs Keen Eye)
               epic:   Last Stand, Critical Strike, Glide, Reroll, Keen Eye, Arrow Recovery (each one at a time),
                       Vampire, Thorns, Night Power (also the legendary box's)
           - the common box = Marko's Chest prefab: Tony's ChestTier (2 coins, +1 for every perk bought, gold) and the 3
             new common perks added to its list (his 4 stay);
           - Assets/Prefabs/WorldItems/Rare Chest.prefab (DuNguyn's blue loot box, 10 coins, x1.5), Epic Chest.prefab
             (the purple one, 100 coins, x2) and Legendary Chest.prefab (the orange one, 150 coins, x2, the new system
             only): Marko's PerkChest with their perks, ChestTier, standing on the ground (StandOnGround) and an outline
             in their color (InteractOutline). The model comes from DuNguyn's prefab (SM_LootBox_01 / 03 / 02), which has
             the pack's textured URP materials: Built-in copies of them, glow included, go in Assets/Materials/Chests (the
             .fbx's own materials have no pictures: grey boxes); the pack's 4096 px textures are imported at 1024 (a chest
             doesn't need more, and the web game loads faster);
           - each box's ChestTier: old system = choose 1 of 3, REROLL only at the epic box; new system = the common and
             epic boxes roll one random perk (the roll pop-up), the rare and legendary boxes choose 1 of 3, every box can
             REROLL, the common and rare boxes offer the old common and rare perks together; in both systems REROLL only
             works once the player has the Reroll perk (ChestTier > Reroll Needs Perk); the burst in the box's color
             when a perk is bought (Assets/Prefabs/Effects/DeathParticles.prefab). A box built before (Tool Version < 2)
             gets these new-system settings once;
           - the perk panel (PerkPanelBuilder.UpdatePrefab) and the roll pop-up (PerkRollBuilder);
           - each game scene (the Build Settings scenes with Sanyam's ObjectScatter: GameScene, ProceduralGenerateScene):
             its chest rules, twice the first build's boxes: grassland (ChestsForest) 20 common + 6 rare, desert
             (ChestsDesert, on the sand layer: it used the grass one) 30 common + 12 rare + 4 epic + 2 legendary, all in one
             spacing group (counts only go up); the roll pop-up; PerkEffects and PlayerGlide (switched off) on the Player;
             PlayerArmor's set bonuses brought up to date. Then it's saved.
Requirement: LootChestBuilder.cs x-> PerkData.cs, PerkChest.cs, ChestTier.cs, PerkEffects.cs, PlayerGlide.cs, PlayerArmor.cs, PlayerInventory.cs, StandOnGround.cs, InteractOutline.cs, ObjectScatter.cs, WorldGenerator.cs, IconArt.cs, PerkPanelBuilder.cs, PerkRollBuilder.cs, MaterialBuildHelper.cs, MeshBounds.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;   // FindPrefabPath, FolderOf, MakeFolders, CheckTMP, MakeEventSystemIfMissing

public static class LootChestBuilder
{
    // One new perk's Perk Data.
    struct PerkSpec
    {
        public string file;              // Assets/Config/Perks/<file>.asset
        public string name, description;
        public string oldDescription;    // what an earlier build wrote: an untouched old text is brought up to date
        public ChestPerkType kind;
        public float value;
        public string requires;          // another perk's file (empty = always offered)
        public int maxPicks;             // 1 = one at a time, 0 = no limit
        public System.Func<Sprite> icon;
    }

    // One loot box.
    struct ChestSpec
    {
        public string file;              // Assets/Prefabs/WorldItems/<file>.prefab
        public string model;             // DuNguyn's model
        public LootTier tier;
        public string boxName;
        public Color color;              // the panel's title, the outline and the burst
        public int startPrice;
        public PriceGrowth growth;
        public float growBy;
        public bool reroll;              // old system: REROLL at this box
        public bool randomNew;           // new system: one random perk in the roll pop-up
        public bool newOnly;             // only in the new system (the legendary box)
        public bool sharedPool;          // new system: offers the common and rare perks together
        public int burst;                // particles when a perk from it is bought
        public float width;              // meters across (the common chest is about 1 m)
        public string[] perks;           // Perk Data files
    }

    // ===== Fields =====
    const string ToolName = "Build Loot Chests";
    const string PerkFolder = "Assets/Config/Perks";
    const string ChestFolder = "Assets/Prefabs/WorldItems";
    const string CommonChestPath = ChestFolder + "/Chest.prefab";   // Marko's chest (found by name if moved)
    const string MaterialFolder = "Assets/Materials/Chests";
    const string BurstPath = "Assets/Prefabs/Effects/DeathParticles.prefab";   // Tools > Effects > Build Death Particles
    const string ModelName = "Model";
    const int TextureSize = 1024;        // the loot boxes' textures are imported at most this big
    const string SpacingGroup = "Chests";   // every box keeps Min Spacing from every other box
    const int ToolVersion = 2;           // ChestTier's settings: 2 = the new chest system (21/11/2026)

    static readonly Color CommonColor = new Color(1f, 0.78f, 0.25f);         // gold
    static readonly Color OldCommonColor = new Color(0.86f, 0.87f, 0.92f);   // the first build's grey-white

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
        new PerkSpec { file = "Perk_AttackSpeed", name = "Attack Speed", description = "Attack 10% faster.",
                       kind = ChestPerkType.AttackSpeed, value = 0.1f, icon = IconArt.PerkAttackSpeed },
        new PerkSpec { file = "Perk_LongReach", name = "Long Reach", description = "Your melee attacks reach 10% farther.",
                       kind = ChestPerkType.LongReach, value = 0.1f, requires = "Perk_KeenEye", icon = IconArt.PerkLongReach },
        // Epic box (and the legendary box)
        new PerkSpec { file = "Perk_LastStand", name = "Last Stand",
                       description = "A killing blow leaves you at 1 health, safe for 2 seconds. Then it's used up, and a box can offer it again.",
                       oldDescription = "Once a life, a killing blow leaves you at 1 health, safe for 2 seconds.",
                       kind = ChestPerkType.LastStand, value = 2f, maxPicks = 1, icon = IconArt.PerkLastStand },
        new PerkSpec { file = "Perk_CriticalStrike", name = "Critical Strike", description = "5% of your hits do double damage (a red burst). Common boxes then offer Precision.",
                       kind = ChestPerkType.CriticalStrike, value = 0.05f, maxPicks = 1, icon = IconArt.PerkCritical },
        new PerkSpec { file = "Perk_Glide", name = "Glide", description = "Hold Space in the air to float down; tap it twice to dive. Common boxes then offer Spring Legs.",
                       kind = ChestPerkType.Glide, value = 0f, maxPicks = 1, icon = IconArt.PerkGlide },
        new PerkSpec { file = "Perk_Reroll", name = "Reroll", description = "Unlocks the REROLL button at loot boxes: new perks for a tenth of the box's price.",
                       kind = ChestPerkType.Reroll, value = 0f, maxPicks = 1, icon = IconArt.PerkReroll },
        new PerkSpec { file = "Perk_KeenEye", name = "Keen Eye", description = "Your crosshair turns yellow when your attack will reach what you aim at. Boxes then offer Long Reach.",
                       kind = ChestPerkType.KeenEye, value = 0f, maxPicks = 1, icon = IconArt.PerkKeenEye },
        new PerkSpec { file = "Perk_ArrowRecovery", name = "Arrow Recovery",
                       description = "Your arrows stay in monsters and animals: walk up close to take them back. A dead one drops them, and missed arrows stay on the ground for you to pick up.",
                       kind = ChestPerkType.ArrowRecovery, value = 0f, maxPicks = 1, icon = IconArt.PerkArrowRecovery },
        new PerkSpec { file = "Perk_Vampire", name = "Vampire", description = "Heal 5% of the damage you do to monsters and animals.",
                       kind = ChestPerkType.Vampire, value = 0.05f, icon = IconArt.PerkVampire },
        new PerkSpec { file = "Perk_Thorns", name = "Thorns", description = "A monster that hits you takes 10% of that hit back.",
                       kind = ChestPerkType.Thorns, value = 0.1f, icon = IconArt.PerkThorns },
        new PerkSpec { file = "Perk_NightPower", name = "Night Power", description = "At night: +10% max health, max stamina, speed and melee damage.",
                       oldDescription = "At night: +10% max health, max stamina, speed and damage.",
                       kind = ChestPerkType.NightPower, value = 0.1f, icon = IconArt.PerkNightPower },
    };

    static readonly string[] CommonAdds = { "Perk_SecondWind", "Perk_Precision", "Perk_SpringLegs" };
    static readonly string[] RarePerks =
        { "Perk_BountyHunter", "Perk_Gourmet", "Perk_Lumberjack", "Perk_Prospector", "Perk_Sprinter", "Perk_Brawler", "Perk_AttackSpeed",
          "Perk_LongReach" };
    static readonly string[] EpicPerks =
        { "Perk_LastStand", "Perk_CriticalStrike", "Perk_Glide", "Perk_Vampire", "Perk_Thorns", "Perk_NightPower", "Perk_Reroll",
          "Perk_KeenEye", "Perk_ArrowRecovery" };

    // Marko's chest (its model and perks are his; only ChestTier comes from here).
    static readonly ChestSpec CommonSpec = new ChestSpec
    {
        file = "Chest", tier = LootTier.Common, boxName = "Common box", color = CommonColor, startPrice = 2,
        growth = PriceGrowth.Add, growBy = 1f, randomNew = true, sharedPool = true, burst = 110,
    };

    static readonly ChestSpec[] Chests =
    {
        new ChestSpec
        {
            file = "Rare Chest", model = "SM_LootBox_01", tier = LootTier.Rare, boxName = "Rare box",
            color = new Color(0.35f, 0.65f, 1f), startPrice = 10, growth = PriceGrowth.Multiply, growBy = 1.5f,
            sharedPool = true, burst = 140, width = 1f, perks = RarePerks,
        },
        new ChestSpec
        {
            file = "Epic Chest", model = "SM_LootBox_03", tier = LootTier.Epic, boxName = "Epic box",
            color = new Color(0.78f, 0.4f, 1f), startPrice = 100, growth = PriceGrowth.Multiply, growBy = 2f, reroll = true,
            randomNew = true, burst = 180, width = 1.2f, perks = EpicPerks,
        },
        new ChestSpec
        {
            file = "Legendary Chest", model = "SM_LootBox_02", tier = LootTier.Legendary, boxName = "Legendary box",
            color = new Color(1f, 0.55f, 0.12f), startPrice = 150, growth = PriceGrowth.Multiply, growBy = 2f, reroll = true,
            newOnly = true, burst = 230, width = 1.35f, perks = EpicPerks,
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
                "This makes four kinds of loot box:\n\n" +
                "- Common (Marko's chest, gold): 2 coins, +1; adds Second Wind, Precision and Spring Legs\n" +
                "- Rare (blue): 10 coins, x1.5; Bounty Hunter, Gourmet, Lumberjack, Prospector, Sprinter, Brawler, Attack Speed, " +
                "Long Reach (after Keen Eye)\n" +
                "- Epic (purple): 100 coins, x2; Last Stand, Critical Strike, Glide, Vampire, Thorns, Night Power, Reroll, Keen Eye\n" +
                "- Legendary (orange, new chest system only): 150 coins, x2; the epic perks, choose 1 of 3\n" +
                "- the new chest system's settings (random common / epic boxes, REROLL everywhere once the player has the " +
                "Reroll perk), the bursts, the perk panel and the roll pop-up\n\n" +
                "Then it changes and SAVES these scenes: " + (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "(none found)") +
                "\n(chest spawns doubled: grassland 20 common + 6 rare, desert 30 common + 12 rare + 4 epic + 2 legendary; " +
                "the roll pop-up; PerkEffects and PlayerGlide on the Player). Your open scenes are saved first. Continue?",
                "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Dictionary<string, PerkData> perks = MakePerks(out List<string> perksMade);
        ParticleSystem burst = AssetDatabase.LoadAssetAtPath<ParticleSystem>(FindPrefabPath(BurstPath));

        string common = SetUpCommonChest(perks, burst, out GameObject commonPrefab, out List<PerkData> sharedPool);
        List<string> chestReports = new List<string> { common };
        Dictionary<LootTier, GameObject> prefabs = new Dictionary<LootTier, GameObject>();
        foreach (ChestSpec spec in Chests)
        {
            GameObject prefab = BuildChest(spec, perks, sharedPool, burst, out string report);
            if (prefab != null) prefabs[spec.tier] = prefab;
            chestReports.Add(report);
        }
        string panel = PerkPanelBuilder.UpdatePrefab();
        GameObject roll = PerkRollBuilder.SavePrefab();
        AssetDatabase.SaveAssets();

        List<string> sceneReports = new List<string>();
        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenes) sceneReports.Add(SetUpScene(path, commonPrefab, prefabs, roll));
        }
        finally
        {
            if (before != null && before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        }

        Debug.Log(ToolName + ": perks " + (perksMade.Count > 0 ? "made: " + string.Join(", ", perksMade) : "already there") +
                  ". " + string.Join(" ", chestReports) + (burst == null ? " No burst: run Tools > Effects > Build Death " +
                  "Particles, then this tool again." : "") + " Perk panel: " + panel + ". Roll pop-up: " +
                  (roll != null ? AssetDatabase.GetAssetPath(roll) : "not saved") + ". Scenes: " +
                  (sceneReports.Count > 0 ? string.Join(" ", sceneReports) : "none found (no Build Settings scene has ObjectScatter)") +
                  " Play: the New Game page's Testing row picks Old chests / New chests.");
    }

    // ===== My methods =====

    // The Perk Data for every new perk: an existing one is kept as it is (only an empty icon or Requires is filled in, and
    // a description still as an earlier build wrote it is brought up to date), a missing one is made. Returns all of them
    // by file name; "made" lists the new ones.
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
            if (!string.IsNullOrEmpty(spec.oldDescription) && perk.description == spec.oldDescription)
                perk.description = spec.description;   // e.g. Last Stand isn't "once a life" any more
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
    // StandOnGround and InteractOutline get the tool's numbers only when they're new (yours stay), PerkChest's list gets
    // the box's perks it's missing, and ChestTier is set up by SetUpTier.
    static GameObject BuildChest(ChestSpec spec, Dictionary<string, PerkData> perks, List<PerkData> sharedPool,
                                 ParticleSystem burst, out string report)
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
            WarnIfPlain(spec.file, look);
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
            bool newTier = tier == null;
            if (newTier) tier = root.AddComponent<ChestTier>();
            string tierReport = SetUpTier(tier, spec, newTier, sharedPool, burst);

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
            report = spec.file + ": " + path + " (" + spec.width + " m " + spec.model + (added > 0 ? ", " + added + " perks added" : "") +
                     (tierReport.Length > 0 ? ", " + tierReport : "") + ").";
            return saved;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // DuNguyn's prefab of the model, by name, else the model (.fbx) itself. The prefab first: it carries the pack's
    // textured materials (LootBox_v01_M...); the .fbx's own materials are plain ones without any picture, which made
    // the first boxes grey (21/11/2026).
    static GameObject FindModel(string modelName)
    {
        foreach (string filter in new[] { " t:Prefab", " t:Model" })
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

    // A material with no picture (its Albedo empty) draws the box in one flat color: says so in the Console.
    static void WarnIfPlain(string chest, GameObject look)
    {
        List<string> plain = new List<string>();
        foreach (Renderer part in look.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in part.sharedMaterials)
            {
                if (m != null && m.HasProperty("_MainTex") && m.mainTexture == null && !plain.Contains(m.name)) plain.Add(m.name);
            }
        }
        if (plain.Count > 0)
            Debug.LogWarning(ToolName + ": " + chest + "'s material " + string.Join(", ", plain) + " has no picture, so the box looks " +
                             "plain grey. Check that DuNguyn's prefab (Assets/Imported3D/DuNguyn/Loot Box/Prefabs) uses its LootBox " +
                             "material, then run the tool again.");
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

    // Marko's common chest: the new common perks added to its list, and ChestTier (SetUpTier). "sharedPool" = its perks
    // and the rare box's together: what the common and rare boxes offer in the new system.
    static string SetUpCommonChest(Dictionary<string, PerkData> perks, ParticleSystem burst, out GameObject prefab,
                                   out List<PerkData> sharedPool)
    {
        prefab = null;
        sharedPool = new List<PerkData>();
        AddFiles(sharedPool, CommonAdds, perks);   // used as they are when Marko's chest is missing
        AddFiles(sharedPool, RarePerks, perks);

        string path = FindPrefabPath(CommonChestPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "Common box: Marko's Chest prefab wasn't found.";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            PerkChest chest = root.GetComponent<PerkChest>();
            if (chest == null) return "Common box: " + path + " has no PerkChest.";
            SerializedObject c = new SerializedObject(chest);
            SerializedProperty list = c.FindProperty("availablePerks");
            int added = AddPerks(list, CommonAdds, perks);
            c.ApplyModifiedPropertiesWithoutUndo();

            sharedPool.Clear();   // Marko's perks first (in his order), then the rare box's
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is PerkData perk && !sharedPool.Contains(perk)) sharedPool.Add(perk);
            }
            AddFiles(sharedPool, RarePerks, perks);

            ChestTier tier = root.GetComponent<ChestTier>();
            bool newTier = tier == null;
            if (newTier) tier = root.AddComponent<ChestTier>();
            string tierReport = SetUpTier(tier, CommonSpec, newTier, sharedPool, burst);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            return "Common box: " + path + (newTier ? " got ChestTier (2 coins, +1)" : " kept its prices") +
                   (added > 0 ? ", " + added + " perks added" : "") + (tierReport.Length > 0 ? ", " + tierReport : "") + ".";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // A box's ChestTier. A new one gets every number from the spec. One built before the new chest system (Tool Version
    // under 2) gets the new system's settings once (the common box's grey-white turns gold); after that your changes
    // stay. Every run adds the new-system perks the common / rare box is missing, and the burst particles if it has
    // none. Returns what changed (empty = nothing).
    static string SetUpTier(ChestTier tier, ChestSpec spec, bool isNew, List<PerkData> sharedPool, ParticleSystem burst)
    {
        List<string> changed = new List<string>();
        SerializedObject t = new SerializedObject(tier);   // the names in quotes must match ChestTier's fields
        if (isNew)
        {
            t.FindProperty("tier").enumValueIndex = (int)spec.tier;
            t.FindProperty("boxName").stringValue = spec.boxName;
            t.FindProperty("color").colorValue = spec.color;
            t.FindProperty("startPrice").intValue = spec.startPrice;
            t.FindProperty("growth").enumValueIndex = (int)spec.growth;
            t.FindProperty("growBy").floatValue = spec.growBy;
            t.FindProperty("canReroll").boolValue = spec.reroll;
        }

        SerializedProperty version = t.FindProperty("toolVersion");
        if (isNew || version.intValue < ToolVersion)
        {
            t.FindProperty("randomInNewSystem").boolValue = spec.randomNew;
            t.FindProperty("rerollInNewSystem").boolValue = true;
            t.FindProperty("newSystemOnly").boolValue = spec.newOnly;
            t.FindProperty("burstCount").intValue = spec.burst;
            SerializedProperty color = t.FindProperty("color");
            if (!isNew && Near(color.colorValue, OldCommonColor)) color.colorValue = spec.color;
            version.intValue = ToolVersion;
            if (!isNew) changed.Add("new chest system settings");
        }

        if (spec.sharedPool)
        {
            int added = AddPerkList(t.FindProperty("newSystemPerks"), sharedPool);
            if (added > 0) changed.Add(added + " new-system perks");
        }
        SerializedProperty particles = t.FindProperty("vanishParticles");
        if (particles.objectReferenceValue == null && burst != null)
        {
            particles.objectReferenceValue = burst;
            changed.Add("burst");
        }
        t.ApplyModifiedPropertiesWithoutUndo();
        return string.Join(", ", changed);
    }

    static bool Near(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

    // Adds the perks the list is missing (by file name) at its end. Returns how many.
    static int AddPerks(SerializedProperty list, string[] files, Dictionary<string, PerkData> perks)
    {
        List<PerkData> wanted = new List<PerkData>();
        AddFiles(wanted, files, perks);
        return AddPerkList(list, wanted);
    }

    // Adds the perks the list is missing at its end. Returns how many.
    static int AddPerkList(SerializedProperty list, List<PerkData> wanted)
    {
        int added = 0;
        foreach (PerkData perk in wanted)
        {
            if (perk == null) continue;
            bool there = false;
            for (int i = 0; i < list.arraySize && !there; i++) there = list.GetArrayElementAtIndex(i).objectReferenceValue == perk;
            if (there) continue;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = perk;
            added++;
        }
        return added;
    }

    // The Perk Data of these files, added to "into" (each once).
    static void AddFiles(List<PerkData> into, string[] files, Dictionary<string, PerkData> perks)
    {
        foreach (string file in files)
        {
            if (perks.TryGetValue(file, out PerkData perk) && perk != null && !into.Contains(perk)) into.Add(perk);
        }
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

    // Opens one game scene, sets up its chest rules, the roll pop-up and its Player, and saves it. Returns a report.
    static string SetUpScene(string path, GameObject commonPrefab, Dictionary<LootTier, GameObject> prefabs, GameObject roll)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        List<string> done = new List<string>();

        ObjectScatter scatter = Object.FindFirstObjectByType<ObjectScatter>(FindObjectsInactive.Include);
        if (scatter != null) done.Add(SetUpRules(scatter, commonPrefab, prefabs));
        else done.Add("no ObjectScatter");

        if (roll != null)
        {
            done.Add("roll pop-up: " + PerkRollBuilder.PutInOpenScene(roll, false));
            MakeEventSystemIfMissing(ToolName);   // its buttons need one
        }

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

    // Sanyam's ObjectScatter chest rules, twice the first build's boxes (counts only go up: a bigger count you set
    // stays): the desert rule on the sand layer, the rare, epic and legendary rules after the common ones (copies of
    // their settings), all in one spacing group.
    static string SetUpRules(ObjectScatter scatter, GameObject commonPrefab, Dictionary<LootTier, GameObject> prefabs)
    {
        SerializedObject so = new SerializedObject(scatter);
        SerializedProperty rules = so.FindProperty("rules");   // the names in quotes must match Sanyam's ObjectScatter fields
        int sand = SandLayer(scatter);
        int grassRule = FindRule(rules, "ChestsForest");
        int desertRule = FindRule(rules, "ChestsDesert");
        if (grassRule < 0 && desertRule < 0) return "no ChestsForest / ChestsDesert rule";

        if (grassRule >= 0)
        {
            SerializedProperty grass = rules.GetArrayElementAtIndex(grassRule);
            RaiseCount(grass, 20);
            grass.FindPropertyRelative("spacingGroup").stringValue = SpacingGroup;
        }
        if (desertRule >= 0)
        {
            SerializedProperty desert = rules.GetArrayElementAtIndex(desertRule);
            desert.FindPropertyRelative("terrainLayer").intValue = sand;
            RaiseCount(desert, 30);
            desert.FindPropertyRelative("spacingGroup").stringValue = SpacingGroup;
        }

        prefabs.TryGetValue(LootTier.Rare, out GameObject rare);
        prefabs.TryGetValue(LootTier.Epic, out GameObject epic);
        prefabs.TryGetValue(LootTier.Legendary, out GameObject legendary);
        List<string> rulesMade = new List<string>();
        if (rare != null && grassRule >= 0) AddRuleAfter(rules, "ChestsForest", "Rare Chests Grass", rare, 6, -1, rulesMade);
        if (rare != null && desertRule >= 0) AddRuleAfter(rules, "ChestsDesert", "Rare Chests Desert", rare, 12, sand, rulesMade);
        string after = FindRule(rules, "Rare Chests Desert") >= 0 ? "Rare Chests Desert" : "ChestsDesert";
        if (epic != null && desertRule >= 0) AddRuleAfter(rules, after, "Epic Chests Desert", epic, 4, sand, rulesMade);
        after = FindRule(rules, "Epic Chests Desert") >= 0 ? "Epic Chests Desert" : after;
        if (legendary != null && desertRule >= 0) AddRuleAfter(rules, after, "Legendary Chests Desert", legendary, 2, sand, rulesMade);
        so.ApplyModifiedPropertiesWithoutUndo();
        return "chest rules: desert on sand layer " + sand + ", counts doubled" +
               (rulesMade.Count > 0 ? ", added " + string.Join(", ", rulesMade) : "");
    }

    // A rule's count up to at least "count" (a bigger one stays).
    static void RaiseCount(SerializedProperty rule, int count)
    {
        SerializedProperty c = rule.FindPropertyRelative("count");
        c.intValue = Mathf.Max(c.intValue, count);
    }

    // A copy of the rule named "after", put right behind it, as a new box rule (only if none has that name yet; an
    // existing one gets its prefab, layer and group, and its count raised to "count"). layer -1 = keep the copied rule's.
    static void AddRuleAfter(SerializedProperty rules, string after, string name, GameObject prefab, int count, int layer, List<string> made)
    {
        int existing = FindRule(rules, name);
        SerializedProperty rule;
        if (existing >= 0)
        {
            rule = rules.GetArrayElementAtIndex(existing);
            RaiseCount(rule, count);
        }
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
