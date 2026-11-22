/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Enemies > Set Up Night Monsters. It asks first, saves your open scenes, changes both game scenes
              and saves them, then opens your scenes again. Run it again any time: the monsters and the sparks are rebuilt
              in place, and numbers you changed (ShellDefense on the turtle, FlyingEnemy on the Beholder, NightMonsters in a
              scene) stay. Needs RPG Monster DUO PBR Polyart (the TurtleShell) and RPG Monster Partners PBR Polyart (the
              Chest Monster and the Beholder; without it only the turtle is set up). Guide: Docs/Night Monsters Setup.md
Purpose: A new monster on each of the first nights, in one click: the turtle from night 2, the Chest Monster from night 3
         and the flying Beholder from night 4 (22/11/2026), so night 3 has 3 kinds of monster and night 4 on 4:
           - Assets/Prefabs/Enemy/TurtleShellEnemy (EnemyBuilder.BuildMonster: its controller gets the shell states)
             made a night monster: no EnemyAggro (it hunts the player like the slime), burning away into orange embers
             at dawn like the other night monsters (DeathEffect's Vanish Color / Rise back to the night's), and
             ShellDefense (it fights 6 s, hides 3 s, 1 in 3 when hurt; a melee hit on the shell = 15 damage to the
             player) with the sparks below;
           - Assets/Prefabs/Effects/ShellDeflect.prefab: bright sparks bouncing off and a white flash where a hit bounces
             off the shell (refilled in place; its material in Assets/Effects/ShellDeflect, made only if missing);
           - Assets/Prefabs/Enemy/ChestMonsterEnemy (RPG Monster Partners' mimic, 1.1 m, a little faster than the slime):
             a night monster like the slime (it walks up and bites), burning away at dawn;
           - Assets/Prefabs/Enemy/BeholderEnemy (Partners' floating eye, 1.3 m, flies at 3.5 m/s): a flying night
             monster (FlyingEnemy: high up out of sword reach, it dives to bite, then climbs away), burning away at dawn;
           - each game scene (the Build Settings scenes with Marko's EnemySpawner: GameScene, ProceduralGenerateScene):
             NightMonsters on the EnemySpawner's object with the Turtle (night 2), the Chest Monster (night 3) and the
             Beholder (night 4), each added only if it isn't there, so your numbers stay, and the desert's day monster
             spawner (Desert Monster Spawner) taken out. Then saved.
         BuildTurtle is also what Tools > World > Build Desert Content uses for the turtle.
Requirement: NightMonsterBuilder.cs x-> EnemyBuilder.cs, NightMonsters.cs, ShellDefense.cs, EnemyAggro.cs, EnemyMovement.cs, DeathEffect.cs, DesertMonsterSpawner.cs, EnemySpawner.cs, UIBuildHelper.cs, MenuOrder.cs
             NightMonsterBuilder.cs <-x DesertBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using UnityEngine.SceneManagement;
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class NightMonsterBuilder
{
    // ===== Fields =====
    const string ToolName = "Set Up Night Monsters";
    const string Monster = "TurtleShellPolyart";   // RPG Monster DUO's second monster (the first is the slime)
    const string ChestMonster = "ChestMonsterPolyartDefault";   // RPG Monster Partners' mimic: night 3
    const string FlyingMonster = "BeholderPolyartDefault";      // RPG Monster Partners' floating eye: night 4, it flies
    const float ChestHeight = 1.1f;      // meters tall: a big chest on legs (the slime and the turtle are 1 m)
    const float ChestSpeed = 2.5f;       // walks a bit faster than the slime and the turtle (2 m/s; the player walks 7.8)
    const float BeholderHeight = 1.3f;   // the eye with its tentacles
    const float BeholderSpeed = 3.5f;    // flies faster than the others walk
    const string EffectPath = "Assets/Prefabs/Effects/ShellDeflect.prefab";
    const string ArtFolder = "Assets/Effects/ShellDeflect";
    const string SparkTexture = "Assets/Effects/PunchWindBurst/WindDot.png";   // the punch burst's soft dot (else Unity's)

    static readonly Color NightEmbers = new Color(1f, 0.5f, 0.08f);   // DeathEffect's own Vanish Color: the night's embers
    const float NightRise = 0.35f;                                    // and its Vanish Rise: embers float up
    static readonly Color SparkHot = new Color(1f, 0.97f, 0.8f);      // the sparks: white-hot to warm yellow
    static readonly Color SparkWarm = new Color(1f, 0.72f, 0.25f);

    // A monster's line in NightMonsters, used only when the tool adds it (your numbers stay after that): its name, first
    // night, share of the spawns (+ per night after its first), most alive at once (+ per night), health and damage (+ per
    // night).
    struct NightLine
    {
        public string name;
        public int firstNight, maxAlive, maxAlivePerNight;
        public float share, sharePerNight, health, healthPerNight, damage, damagePerNight;
        public NightLine(string name, int firstNight, float share, float sharePerNight, int maxAlive, int maxAlivePerNight,
                         float health, float healthPerNight, float damage, float damagePerNight)
        {
            this.name = name;
            this.firstNight = firstNight;
            this.share = share;
            this.sharePerNight = sharePerNight;
            this.maxAlive = maxAlive;
            this.maxAlivePerNight = maxAlivePerNight;
            this.health = health;
            this.healthPerNight = healthPerNight;
            this.damage = damage;
            this.damagePerNight = damagePerNight;
        }
    }
    // The turtle: 3 in 10 of night 2's monsters (+5 % a night), at most 3 at once (+1 a night), 90 health (the slime has
    // 60 on night 2) and the slime's damage, 12.
    static readonly NightLine TurtleLine = new NightLine("Turtle", 2, 0.3f, 0.05f, 3, 1, 90f, 15f, 12f, 2f);
    // The Chest Monster: 2 in 10 of night 3's (+5 % a night), at most 2 at once (+1), tough (120; the turtle has 105 on
    // night 3) and it bites hard (15; the slime 14).
    static readonly NightLine ChestLine = new NightLine("Chest Monster", 3, 0.2f, 0.05f, 2, 1, 120f, 20f, 15f, 2f);
    // The Beholder: 15 in 100 of night 4's (+5 % a night), at most 2 at once (+1), less health (70: it's hard to reach up
    // there) and a lighter bite (12), but it keeps coming back. On night 4 the slimes still get 2 in 10 of the spawns.
    static readonly NightLine BeholderLine = new NightLine("Beholder", 4, 0.15f, 0.05f, 2, 1, 70f, 15f, 12f, 2f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Enemies/Set Up Night Monsters", false, MenuOrder.SetUpNightMonsters)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        List<string> scenes = GameScenes();
        if (!EditorUtility.DisplayDialog(ToolName,
                "This sets up a new monster for each of the first nights:\n\n" +
                "- night 1: slimes; night 2: + turtles; night 3: + chest monsters; night 4 on: + beholders (flying)\n" +
                "- the turtle hides in its shell now and then (fights 6 s, hides 3 s): a melee hit on the shell does nothing " +
                "to it and hurts you (15), with sparks and \"BLOCKED!\"; the fireball still hurts it\n" +
                "- the chest monster: a mimic on four legs, tougher and a bit faster than the slime\n" +
                "- the beholder flies 3.5 m up, out of sword reach (shoot it), and dives down to bite, then climbs away\n" +
                "- night monsters appear 24 - 34 m away where you can't see them (never right in front of you)\n" +
                "- the desert has no day monster any more\n\n" +
                "It rebuilds Assets/Prefabs/Enemy/TurtleShellEnemy, ChestMonsterEnemy and BeholderEnemy, makes " + EffectPath +
                ", then changes and SAVES these " +
                "scenes: " + (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "(none found)") +
                ". Your open scenes are saved first. Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> report = new List<string>();
        string turtle = PathOf(BuildTurtle(report));
        string chest = PathOf(BuildChestMonster(report));
        string beholder = PathOf(BuildBeholder(report));
        AssetDatabase.SaveAssets();

        // Loaded again from their files right before the scenes, not kept from when they were built: a model reimported
        // in between (EnemyBuilder makes meshes readable) could leave an old link empty (Build Bow And Arrows lost its
        // flying arrow that way, 22/11/2026).
        List<(GameObject prefab, NightLine line)> lines = new List<(GameObject prefab, NightLine line)>
        {
            (Load(turtle), TurtleLine), (Load(chest), ChestLine), (Load(beholder), BeholderLine),   // in order of their first night
        };
        List<string> sceneReports = new List<string>();
        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenes) sceneReports.Add(SetUpScene(path, lines));
        }
        finally
        {
            if (before != null && before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        }

        Debug.Log(ToolName + ": " + string.Join("; ", report) + ". Scenes: " +
                  (sceneReports.Count > 0 ? string.Join(" ", sceneReports) : "none found (no Build Settings scene has an EnemySpawner)") +
                  " Test: Play, select the sun (DaylightCycle), set Day to 4 and drag Time Of Day to 0.8.");
    }

    // ===== My methods =====

    static string PathOf(GameObject prefab) => prefab != null ? AssetDatabase.GetAssetPath(prefab) : null;

    static GameObject Load(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);

    // The turtle as a night monster (Tools > World > Build Desert Content uses this too): the enemy (EnemyBuilder), then
    // no EnemyAggro, the night's embers at dawn, and ShellDefense with the sparks. Returns the prefab (null = no turtle).
    public static GameObject BuildTurtle(List<string> report)
    {
        GameObject sparks = BuildDeflectEffect(report);
        GameObject prefab = EnemyBuilder.BuildMonster(Monster, out string built);
        report.Add("turtle: " + built);
        if (prefab == null) return null;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            MakeNightMonster(root);

            ShellDefense shell = root.GetComponent<ShellDefense>();   // kept with its numbers through EnemyBuilder's rebuild
            if (shell == null) shell = root.AddComponent<ShellDefense>();
            SerializedObject s = new SerializedObject(shell);   // the names in quotes must match ShellDefense's fields
            SerializedProperty fx = s.FindProperty("deflectEffect");
            if (fx.objectReferenceValue == null && sparks != null) fx.objectReferenceValue = sparks;
            s.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The Chest Monster (RPG Monster Partners' mimic) as night 3's new monster: the enemy (EnemyBuilder, ChestHeight
    // tall), the night's embers at dawn, a bit faster than the slime. Returns the prefab (null = the pack isn't there).
    public static GameObject BuildChestMonster(List<string> report)
    {
        GameObject prefab = EnemyBuilder.BuildMonster(ChestMonster, out string built, ChestHeight);
        report.Add("chest monster: " + built);
        return prefab != null ? Finish(prefab, ChestSpeed) : null;
    }

    // The Beholder (RPG Monster Partners' floating eye) as night 4's new monster: a flying enemy (EnemyBuilder with
    // flying: no gravity, its collider around its body, FlyingEnemy), the night's embers at dawn, flying at
    // BeholderSpeed. Returns the prefab (null = the pack isn't there).
    public static GameObject BuildBeholder(List<string> report)
    {
        GameObject prefab = EnemyBuilder.BuildMonster(FlyingMonster, out string built, BeholderHeight, true);
        report.Add("beholder: " + built);
        return prefab != null ? Finish(prefab, BeholderSpeed) : null;
    }

    // A built enemy made a night monster (MakeNightMonster) with its own speed (Marko's EnemyMovement > Move Speed).
    // Saved in place. Returns the prefab.
    static GameObject Finish(GameObject prefab, float speed)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            MakeNightMonster(root);
            EnemyMovement movement = root.GetComponent<EnemyMovement>();
            if (movement != null) movement.moveSpeed = speed;   // Marko's public field, on Tony's prefab (his script isn't changed)
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // What makes an enemy a night monster: no EnemyAggro (it comes for the player from the start) and the night's
    // orange embers when it burns away at dawn (DeathEffect's Vanish Color / Rise).
    static void MakeNightMonster(GameObject root)
    {
        EnemyAggro waits = root.GetComponent<EnemyAggro>();
        if (waits != null) Object.DestroyImmediate(waits);

        DeathEffect effect = root.GetComponent<DeathEffect>();
        if (effect == null) return;
        SerializedObject so = new SerializedObject(effect);   // the names in quotes must match DeathEffect's fields
        so.FindProperty("vanishColor").colorValue = NightEmbers;
        so.FindProperty("vanishRise").floatValue = NightRise;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The sparks where a hit bounces off a shell: a quick spray of bright, stretched sparks out of the shell (toward the
    // player) that drop a little, and a white flash. Refilled in place. Returns the prefab.
    static GameObject BuildDeflectEffect(List<string> report)
    {
        Material material = MakeMaterial("ShellSpark");
        string path = FindPrefabPath(EffectPath);
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("ShellDeflect");
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            ParticleSystem sparks = root.GetComponent<ParticleSystem>();
            if (sparks == null) sparks = root.AddComponent<ParticleSystem>();
            MakeSparks(sparks, material);

            GameObject flash = new GameObject("Flash");
            flash.transform.SetParent(root.transform, false);
            MakeFlash(flash.AddComponent<ParticleSystem>(), material);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            report.Add("sparks: " + path);
            return saved;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // 16 - 22 sparks thrown out of the shell in a cone, fast, stretched by their speed into short streaks, dropping a
    // little and shrinking: like a blade glancing off metal.
    static void MakeSparks(ParticleSystem ps, Material material)
    {
        Prepare(ps, material, 0.3f, 40);
        ParticleSystem.MainModule main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // they keep flying where they were thrown
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(SparkHot, SparkWarm);
        main.gravityModifier = 1.2f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16, (short)22) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;   // its front = out of the shell (the prefab is turned to the hit)
        shape.angle = 55f;
        shape.radius = 0.05f;
        shape.radiusThickness = 1f;

        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;
        slow.enabled = true;
        slow.limit = 100f;   // no speed limit, only the air slowing them
        slow.dampen = 0f;
        slow.drag = 2f;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f)));

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;   // 6 m/s = a 0.3 m streak
        renderer.lengthScale = 3f;
        renderer.cameraVelocityScale = 0f;
    }

    // One white flash right on the hit: grows a little and fades in a moment.
    static void MakeFlash(ParticleSystem ps, Material material)
    {
        Prepare(ps, material, 0.2f, 2);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.14f;
        main.startSpeed = 0f;
        main.startSize = 0.8f;
        main.startColor = new Color(1f, 1f, 1f, 0.9f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;   // right at the hit

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.3f)));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
    }

    // Settings both parts share: play once by themselves when made, no shadows.
    static void Prepare(ParticleSystem ps, Material material, float duration, int max)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // Unity only lets Duration change while stopped
        ParticleSystem.MainModule main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = max;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // A glowing (additive) particle material with the punch burst's soft dot: bright even at night. Made once, so
    // changes made to it stay.
    static Material MakeMaterial(string name)
    {
        MakeFolders(ArtFolder);
        string path = ArtFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) shader = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat").shader;
        Texture2D dot = AssetDatabase.LoadAssetAtPath<Texture2D>(SparkTexture);
        if (dot == null) dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        material = new Material(shader) { name = name, mainTexture = dot };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // The Build Settings scenes with Marko's EnemySpawner in them (found in the scene files, without opening them).
    static List<string> GameScenes()
    {
        List<string> found = new List<string>();
        string script = null;
        foreach (string guid in AssetDatabase.FindAssets("EnemySpawner t:MonoScript"))
        {
            if (Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == "EnemySpawner") script = guid;
        }
        if (script == null) return found;
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;
            if (File.ReadAllText(entry.path).Contains(script)) found.Add(entry.path);
        }
        return found;
    }

    // Opens one game scene: NightMonsters on the EnemySpawner's object with each monster's line (the turtle, the Chest
    // Monster, the Beholder: the ones that were built), no day monster spawner. Saves it.
    static string SetUpScene(string path, List<(GameObject prefab, NightLine line)> lines)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        List<string> done = new List<string>();

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner == null) done.Add("no EnemySpawner");
        else
        {
            NightMonsters monsters = spawner.GetComponent<NightMonsters>();
            if (monsters == null)
            {
                monsters = spawner.gameObject.AddComponent<NightMonsters>();
                done.Add("NightMonsters added to " + spawner.name);
            }
            foreach ((GameObject prefab, NightLine line) in lines)
            {
                if (prefab != null && AddLine(monsters, prefab, line)) done.Add("the " + line.name + " from night " + line.firstNight);
            }
        }

        int removed = 0;
        foreach (DesertMonsterSpawner day in Object.FindObjectsByType<DesertMonsterSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject holder = day.gameObject;
            if (holder.GetComponents<Component>().Length <= 2) Object.DestroyImmediate(holder);   // its own object (Transform + it)
            else Object.DestroyImmediate(day);
            removed++;
        }
        if (removed > 0) done.Add("the desert's day monster spawner removed");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + " (" + (done.Count > 0 ? string.Join(", ", done) : "already set up") + ", saved).";
    }

    // A monster's line in NightMonsters > Monsters (at the end: the lines are in order of their first night), unless
    // its prefab has one already (then your numbers stay). True = added.
    static bool AddLine(NightMonsters monsters, GameObject prefab, NightLine numbers)
    {
        SerializedObject so = new SerializedObject(monsters);   // the names in quotes must match NightMonsters' fields
        SerializedProperty list = so.FindProperty("monsters");
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == prefab) return false;
        }

        list.arraySize++;   // a new line copies the one before it (or starts at zeros), so every number is set
        SerializedProperty line = list.GetArrayElementAtIndex(list.arraySize - 1);
        line.FindPropertyRelative("name").stringValue = numbers.name;
        line.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        line.FindPropertyRelative("firstNight").intValue = numbers.firstNight;
        line.FindPropertyRelative("share").floatValue = numbers.share;
        line.FindPropertyRelative("sharePerNight").floatValue = numbers.sharePerNight;
        line.FindPropertyRelative("maxAlive").intValue = numbers.maxAlive;
        line.FindPropertyRelative("maxAlivePerNight").intValue = numbers.maxAlivePerNight;
        line.FindPropertyRelative("health").floatValue = numbers.health;
        line.FindPropertyRelative("healthPerNight").floatValue = numbers.healthPerNight;
        line.FindPropertyRelative("damage").floatValue = numbers.damage;
        line.FindPropertyRelative("damagePerNight").floatValue = numbers.damagePerNight;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
