/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Enemies > Set Up Night Monsters. It asks first, saves your open scenes, changes both game scenes
              and saves them, then opens your scenes again. Run it again any time: the turtle and its sparks are rebuilt in
              place, and numbers you changed (ShellDefense on the turtle, NightMonsters in a scene) stay. Needs RPG Monster
              DUO PBR Polyart (the TurtleShell). Guide: Docs/Night Monsters Setup.md
Purpose: The turtle as night 2's new monster, in one click:
           - Assets/Prefabs/Enemy/TurtleShellEnemy (EnemyBuilder.BuildMonster: its controller gets the shell states)
             made a night monster: no EnemyAggro (it hunts the player like the slime), burning away into orange embers
             at dawn like the other night monsters (DeathEffect's Vanish Color / Rise back to the night's), and
             ShellDefense (it fights 6 s, hides 3 s, 1 in 3 when hurt; a melee hit on the shell = 15 damage to the
             player) with the sparks below;
           - Assets/Prefabs/Effects/ShellDeflect.prefab: bright sparks bouncing off and a white flash where a hit bounces
             off the shell (refilled in place; its material in Assets/Effects/ShellDeflect, made only if missing);
           - each game scene (the Build Settings scenes with Marko's EnemySpawner: GameScene, ProceduralGenerateScene):
             NightMonsters on the EnemySpawner's object with the Turtle from night 2 (added only if it isn't there, so
             your numbers stay), and the desert's day monster spawner (Desert Monster Spawner) taken out. Then saved.
         BuildTurtle is also what Tools > World > Build Desert Content uses for the turtle.
Requirement: NightMonsterBuilder.cs x-> EnemyBuilder.cs, NightMonsters.cs, ShellDefense.cs, EnemyAggro.cs, DeathEffect.cs, DesertMonsterSpawner.cs, EnemySpawner.cs, UIBuildHelper.cs, MenuOrder.cs
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
    const string EffectPath = "Assets/Prefabs/Effects/ShellDeflect.prefab";
    const string ArtFolder = "Assets/Effects/ShellDeflect";
    const string SparkTexture = "Assets/Effects/PunchWindBurst/WindDot.png";   // the punch burst's soft dot (else Unity's)

    static readonly Color NightEmbers = new Color(1f, 0.5f, 0.08f);   // DeathEffect's own Vanish Color: the night's embers
    const float NightRise = 0.35f;                                    // and its Vanish Rise: embers float up
    static readonly Color SparkHot = new Color(1f, 0.97f, 0.8f);      // the sparks: white-hot to warm yellow
    static readonly Color SparkWarm = new Color(1f, 0.72f, 0.25f);

    // The turtle's line in NightMonsters, used only when the tool adds it (your numbers stay after that).
    const int TurtleFirstNight = 2;
    const float TurtleShare = 0.3f;          // 3 in 10 of night 2's monsters
    const float TurtleSharePerNight = 0.05f; // 5 % more each night after
    const int TurtleMaxAlive = 3;            // at most 3 at once on night 2
    const int TurtleMaxAlivePerNight = 1;    // one more each night after
    const float TurtleHealth = 90f;          // the slime has 60 on night 2
    const float TurtleHealthPerNight = 15f;
    const float TurtleDamage = 12f;          // the slime's on night 2
    const float TurtleDamagePerNight = 2f;

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
                "This makes the turtle night 2's new monster:\n\n" +
                "- night 1: slimes; from night 2: slimes and turtles (about 3 in 10, at most 3 turtles at once, more each night)\n" +
                "- the turtle hides in its shell now and then (fights 6 s, hides 3 s): a melee hit on the shell does nothing " +
                "to it and hurts you (15), with sparks and \"BLOCKED!\"; the fireball still hurts it\n" +
                "- night monsters appear 24 - 34 m away where you can't see them (never right in front of you)\n" +
                "- the desert has no day monster any more\n\n" +
                "It rebuilds Assets/Prefabs/Enemy/TurtleShellEnemy, makes " + EffectPath + ", then changes and SAVES these " +
                "scenes: " + (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "(none found)") +
                ". Your open scenes are saved first. Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> report = new List<string>();
        GameObject turtle = BuildTurtle(report);
        AssetDatabase.SaveAssets();

        List<string> sceneReports = new List<string>();
        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenes) sceneReports.Add(SetUpScene(path, turtle));
        }
        finally
        {
            if (before != null && before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        }

        Debug.Log(ToolName + ": " + string.Join("; ", report) + ". Scenes: " +
                  (sceneReports.Count > 0 ? string.Join(" ", sceneReports) : "none found (no Build Settings scene has an EnemySpawner)") +
                  " Test: Play, select the sun (DaylightCycle), set Day to 2 and drag Time Of Day to 0.8.");
    }

    // ===== My methods =====

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
            EnemyAggro waits = root.GetComponent<EnemyAggro>();
            if (waits != null) Object.DestroyImmediate(waits);   // a night monster comes for the player from the start

            DeathEffect effect = root.GetComponent<DeathEffect>();
            if (effect != null)
            {
                SerializedObject so = new SerializedObject(effect);   // the names in quotes must match DeathEffect's fields
                so.FindProperty("vanishColor").colorValue = NightEmbers;
                so.FindProperty("vanishRise").floatValue = NightRise;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

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

    // Opens one game scene: NightMonsters with the turtle on the EnemySpawner's object, no day monster spawner. Saves it.
    static string SetUpScene(string path, GameObject turtle)
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
            if (turtle != null && AddTurtle(monsters, turtle)) done.Add("the turtle from night " + TurtleFirstNight);
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

    // The turtle's line in NightMonsters > Monsters, unless it's there already (then your numbers stay). True = added.
    static bool AddTurtle(NightMonsters monsters, GameObject turtle)
    {
        SerializedObject so = new SerializedObject(monsters);   // the names in quotes must match NightMonsters' fields
        SerializedProperty list = so.FindProperty("monsters");
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == turtle) return false;
        }

        list.arraySize++;   // a new line starts empty (zeros), so every number is set
        SerializedProperty line = list.GetArrayElementAtIndex(list.arraySize - 1);
        line.FindPropertyRelative("name").stringValue = "Turtle";
        line.FindPropertyRelative("prefab").objectReferenceValue = turtle;
        line.FindPropertyRelative("firstNight").intValue = TurtleFirstNight;
        line.FindPropertyRelative("share").floatValue = TurtleShare;
        line.FindPropertyRelative("sharePerNight").floatValue = TurtleSharePerNight;
        line.FindPropertyRelative("maxAlive").intValue = TurtleMaxAlive;
        line.FindPropertyRelative("maxAlivePerNight").intValue = TurtleMaxAlivePerNight;
        line.FindPropertyRelative("health").floatValue = TurtleHealth;
        line.FindPropertyRelative("healthPerNight").floatValue = TurtleHealthPerNight;
        line.FindPropertyRelative("damage").floatValue = TurtleDamage;
        line.FindPropertyRelative("damagePerNight").floatValue = TurtleDamagePerNight;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
