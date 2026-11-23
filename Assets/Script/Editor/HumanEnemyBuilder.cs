/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Enemies > Build Human Enemies. It asks first, saves your open scenes, builds the human
              prefabs and their animations, puts the Human Spawner in the new world (ProceduralGenerateScene, saved),
              then opens your scenes again. Run it again any time: the prefabs are rebuilt in place, and the numbers you
              changed on their HumanBrain, HumanGear and HumanLoot (and the scene's Human Spawner) stay (HumanBrain's
              and HumanGear's only while their defaults haven't changed: their SettingsVersion, then they get the new
              ones once). It also gives every cave prefab its CaveTrail (RockCaveBuilder.AddTrails).
              Needs Polytope Studio's Lowpoly Modular Armors Free and Lowpoly Weapons (in Assets/Imported3D), Blink's
              animations, and the new world (Tools > World > Build New World Scene). Guide: Docs/Human Enemies Setup.md
Purpose: Step 1 of the human enemies (Docs/Human Enemies Plan.md): survivors that live in the new world like the player.
         It makes:
           - Assets/Animation/Enemy/Human/HumanEnemy.controller: Blink's Humanoid clips: the legs by direction like
             the player's (a 2D blend on MoveX / MoveZ: Idle, RunForward slowed down for starting, the runs forward /
             backward / left / right / back diagonals at the player's walking speed, StrafeLeft / Right for circling,
             Sprint at the player's sprint); MiningLoop for chopping and mining with a tool (the moment the tool lands
             measured: Swing At); the three attacks by what it holds (fists, one-handed, two-handed; measured when
             they land and end), which also serve as its punches at a tree or rock it has no tool for; Gathering for
             picking berries (measured when the hand picks); GetHit; Death;
           - for each human type so far (Type 1) a man and a woman, Assets/Prefabs/Enemy/Human/Human Type 1 Male /
             Female: the Polytope body as Model (1.8 m tall, like the player's character: the Human Spawner makes
             each one a little taller or shorter), a capsule and physics, Marko's EnemyCombat for health (so every attack
             of the player hurts it; its own coins off, Coin Multiplier 0: HumanLoot pays by its gear), EnemyAnimator,
             DeathEffect, HumanGear (every armor piece, head, hair, beard and cape of the body, its weapon and shield
             slots, Polytope's melee weapons and shields, the game's Wooden ... Diamond axes and pickaxes),
             HumanLoot (the Copper / Iron / Gold / Diamond / Wood / Stone items) and HumanBrain;
           - the new world's Human Spawner, with each type and where it lives (Type 1: grassland, desert, snow).
         SetUpScene is also what Tools > World > Build New World Scene uses, so a rebuilt new world keeps its humans.
Requirement: HumanEnemyBuilder.cs x-> HumanGear.cs, HumanBrain.cs, HumanLoot.cs, HumanSpawner.cs, EnemyCombat.cs, EnemyAnimator.cs, AnimatorBuildHelper.cs, DeathEffectBuilder.cs, RockCaveBuilder.cs, UIBuildHelper.cs, MenuOrder.cs, ItemData.cs
             HumanEnemyBuilder.cs <-x NewWorldBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static UIBuildHelper;   // MakeFolders, FolderOf

public static class HumanEnemyBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Human Enemies";
    const string PrefabFolder = "Assets/Prefabs/Enemy/Human";
    const string ControllerPath = "Assets/Animation/Enemy/Human/HumanEnemy.controller";
    const string MaskPath = "Assets/Animation/Enemy/Human/HumanUpperBody.mask";   // the attacks move only the upper body
    const string ActionLayerName = "Actions";
    const string NoFrictionPath = "Assets/Prefabs/Enemy/EnemyNoFriction.physicMaterial";   // the monsters' (EnemyBuilder): slides along walls
    const string NewWorldPath = "Assets/Scenes/ProceduralGenerateScene.unity";
    const string SpawnerName = "Human Spawner";

    // Polytope Studio's two modular bodies: every armor piece, head, hair and beard is a child of one model.
    static readonly (string file, string body)[] Bodies =
    {
        ("PT_Lowpoly_Armors_Male_Moduar_Free", "Male"),
        ("PT_Lowpoly_Armors_Female_Moduar_Free", "Female"),
    };
    // Bones in Polytope's bodies made for holding things. Only the man's prefab has them: the tool copies them onto the
    // woman's hands (AddMissingSlots), else she could hold nothing (no weapon, shield, axe or pickaxe).
    const string WeaponSlotName = "PT_Right_Hand_Weapon_slot";
    const string ShieldSlotName = "PT_Left_Hand_Shield_slot";

    // Polytope's melee weapons (the bow comes with the archers, a later step) and shields. Damage = one hit at copper
    // tier (each tier above +25 %): written into HumanGear > Weapons only where it's still 0, so numbers you change on a
    // prefab stay. The two-handed ones hit harder but swing slower (their attack takes about twice as long).
    static readonly (string file, bool twoHanded, float damage)[] Weapons =
    {
        ("PT_Sword_01_a", false, 10f),
        ("PT_Mace_01_a", false, 12f),
        ("PT_ShortWaraxe_01_a", false, 11f),
        ("PT_Longsword_01_a", true, 15f),
        ("PT_Pike_01_a", true, 14f),
    };
    static readonly string[] Shields = { "PT_Shield_01_a", "PT_Shield_06_a", "PT_Shield_19_a" };

    // What a piece of each tier drops (Item Ids) and HumanLoot's fields for them: copper, iron, gold, diamond, and the
    // wood and stone a wooden or stone tool drops.
    static readonly string[] TierItems = { "Copper", "Iron", "Gold", "Diamond", "Wood", "Stone" };
    static readonly string[] TierFields = { "copper", "iron", "gold", "diamond", "wood", "stone" };

    // The game's tier tools (Tools > Items > Build Tier Tools: Assets/Prefabs/Item/Tools), weakest first.
    static readonly string[] ToolTiers = { "Wooden", "Stone", "Iron", "Gold", "Diamond" };

    // Blink's Humanoid clips (Assets/Imported3D/Blink, Animations_Starter_Pack).
    const string IdleClip = "Idle";
    const string RunClip = "RunForward";
    const string SprintClip = "Sprint";
    const string WorkClip = "MiningLoop";   // a swing that loops: chopping and mining with a tool
    const string GatherClip = "Gathering";  // picking berries: bend down and pick (one go)
    // Its legs by direction (HumanBrain's MoveX / MoveZ: meters a second in its own space), like the player's: Blink's
    // directional runs at the player's walking speed, its sprint at the player's sprint, its strafes for slow sideways
    // steps (circling the player), and its run played slower for getting going. Idle and RunForward are a must.
    static readonly (string clip, Vector2 at, float playSpeed)[] Legs =
    {
        ("Idle", Vector2.zero, 1f),
        ("RunForward", new Vector2(0f, WalkAt), WalkPlaySpeed),
        ("RunForward", new Vector2(0f, RunAt), 1f),
        ("RunBackward", new Vector2(0f, -RunAt), 1f),
        ("RunLeft", new Vector2(-RunAt, 0f), 1f),
        ("RunRight", new Vector2(RunAt, 0f), 1f),
        ("RunBackwardLeft", new Vector2(-RunAt * 0.7f, -RunAt * 0.7f), 1f),
        ("RunBackwardRight", new Vector2(RunAt * 0.7f, -RunAt * 0.7f), 1f),
        ("StrafeLeft", new Vector2(-StrafeAt, 0f), 1f),
        ("StrafeRight", new Vector2(StrafeAt, 0f), 1f),
        ("Sprint", new Vector2(0f, SprintAt), 1f),
    };
    const float StrafeAt = 3.9f;            // m/s: circling the player (HumanBrain strafes at half its walk)
    // The attacks (fighting back), by what it holds (HumanBrain's AttackKind): the state, Blink's clip, the number,
    // and HumanBrain's timing field. The clips stand still for a long while after the move: each state leaves when
    // the hands stop (measured).
    static readonly (string state, string clip, int kind, string field)[] Attacks =
    {
        ("Attack Fists", "PunchRight", 0, "fistStrike"),                // its big punch is thrown with the left hand
        ("Attack One Hand", "MeleeAttack_OneHanded", 1, "oneHandStrike"),  // a downward slash: sword, mace, war axe
        ("Attack Two Hands", "MeleeAttack_TwoHanded", 2, "twoHandStrike"), // a big overhead blow with a step: longsword, pike
    };
    const float AttackPlaySpeed = 1.2f;     // the attacks play a bit faster than made (snappier)
    const float StillDistance = 0.004f;     // a hand moving less than this in 1/60 s counts as still
    const float BottomShare = 0.15f;        // a swing lands when the hand is this close to its lowest (a share of its drop)
    const string HitClip = "GetHit";
    const string DeathClip = "Death";       // the state is named Death: DeathEffectBuilder times the dissolve by it
    const float WalkAt = 2f;                // m/s: getting going (Blink's run, played slower)
    const float RunAt = 7.8f;               // the player's walking speed = HumanBrain's walk: Blink's run, as the player's own
    const float SprintAt = 16.5f;           // the player's sprint (7.8 x Sprint Multiplier 2.11): Blink's sprint
    const float WalkPlaySpeed = 0.45f;      // Blink's pack has no walk: its run, played slower

    // A normal human: Polytope's bodies are this tall, like the player's character (Tools > Player > Add Character
    // Model makes it 1.8 m too), so a Kind's size 1 = the player's size. The capsule.
    const float Height = 1.8f;
    const float Radius = 0.3f;
    const float Mass = 5f;       // like the monsters: heavier than the player's ball (1), so it isn't shoved around

    // One human type the tool makes. Size = times a normal human; Health = on day 1.
    struct Kind
    {
        public string name;
        public HumanBrain.Kind type;
        public float size;
        public float health;
        public bool grassland, hills, desert, snow;
        public int groupMin, groupMax;
        public float weight;

        public Kind(string name, HumanBrain.Kind type, float size, float health, bool grassland, bool hills, bool desert,
                    bool snow, int groupMin, int groupMax, float weight)
        {
            this.name = name;
            this.type = type;
            this.size = size;
            this.health = health;
            this.grassland = grassland;
            this.hills = hills;
            this.desert = desert;
            this.snow = snow;
            this.groupMin = groupMin;
            this.groupMax = groupMax;
            this.weight = weight;
        }
    }

    // The human types made so far (the plan's step 1).
    static readonly Kind[] Kinds =
    {
        // Type 1: a loner as big as the player that never attacks and runs away when hit; grassland, desert and snow.
        // (Sizes are times the player's size: type 3 will be 0.75, type 4 1.4.)
        new Kind("Type 1", HumanBrain.Kind.Type1, 1f, 80f, true, false, true, true, 1, 1, 1f),
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Enemies/Build Human Enemies", false, MenuOrder.BuildHumanEnemies)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        List<(GameObject prefab, string body)> bodies = FindBodies();
        if (bodies.Count == 0)
        {
            EditorUtility.DisplayDialog(ToolName, "No Polytope Studio body found (" + Bodies[0].file + "). Import Polytope's " +
                                        "Lowpoly Modular Armors Free into Assets/Imported3D first.", "OK");
            return;
        }
        bool hasWorld = AssetDatabase.LoadAssetAtPath<SceneAsset>(NewWorldPath) != null;
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes the first human enemies for the new world (Type 1: a survivor that never attacks, walks as " +
                "fast as you, chops trees, mines rocks and picks berries for real, and runs away when hit):\n\n" +
                "- " + ControllerPath + " (Blink's animations)\n" +
                "- " + PrefabFolder + "/Human Type 1 Male and Female: Polytope bodies with random armor, a weapon and " +
                "sometimes a shield, tinted by tier; killed, they give you what they wore (1 of its material a piece) " +
                "and what they gathered, straight into your inventory, and coins by tier\n" +
                (hasWorld
                    ? "- the Human Spawner in " + NewWorldPath + " (changed and SAVED)\n\n"
                    : "\n(No new world yet: run Tools > World > Build New World Scene, then this again, for the Human Spawner.)\n\n") +
                "Existing prefabs are rebuilt in place (your numbers on HumanBrain, HumanGear and HumanLoot stay). Your " +
                "open scenes are saved first. Continue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> report = new List<string>();
        List<string> missingClips = new List<string>();
        AnimatorController controller = BuildController(bodies[0].prefab, missingClips, out Strike[] strikes, out string attackReport,
                                                        out Strike gather);
        report.Add("Animations: " + ControllerPath + (missingClips.Count > 0 ? " (MISSING clips: " + string.Join(", ", missingClips) + ")" : ""));
        report.Add("Attacks: " + attackReport);
        report.Add("Berry picking: " + (gather.measured ? "the hand picks at " + gather.hit.ToString("0.00") + " s" : "no Gathering clip measured (HumanBrain keeps its numbers)"));
        report.Add("Caves: " + RockCaveBuilder.AddTrails());
        float? swingAt = MeasureSwing(bodies[0].prefab, WorkClipOf(controller), out string swingHow);
        report.Add("Work swing: " + swingHow);

        List<string> missing = new List<string>();
        List<(GameObject prefab, bool twoHanded, float damage)> weapons = FindWeapons(missing);
        List<GameObject> shields = FindShields(missing);
        if (missing.Count > 0) report.Add("Missing Polytope weapons: " + string.Join(", ", missing));
        ItemData[] items = FindTierItems(out List<string> missingItems);
        if (missingItems.Count > 0) report.Add("Missing items (gear of that tier drops only its coins): " + string.Join(", ", missingItems));
        (List<GameObject> axes, List<GameObject> pickaxes) tools = FindTools(out int toolsFound);
        if (toolsFound < ToolTiers.Length * 2)
            report.Add("Tools found: " + toolsFound + " of " + ToolTiers.Length * 2 + " (Tools > Items > Build Tier Tools makes them)");

        foreach (Kind kind in Kinds)
        {
            foreach ((GameObject prefab, string body) in bodies)
                report.Add(BuildPrefab(kind, prefab, body, controller, weapons, shields, tools, items, swingAt, strikes, gather));
        }
        AssetDatabase.SaveAssets();
        string readable = DeathEffectBuilder.MakeMeshesReadable();
        if (!string.IsNullOrEmpty(readable)) report.Add(readable);
        report.Add(hasWorld ? SetUpNewWorld() : "No " + NewWorldPath + " yet: run Tools > World > Build New World Scene, then this tool again.");

        string all = string.Join("\n", report);
        Debug.Log(ToolName + ":\n" + all);
        EditorUtility.DisplayDialog(ToolName, all + "\n\nTest: from the title screen, NEW GAME > Map: New world. Guide: " +
                                    "Docs/Human Enemies Setup.md", "OK");
    }

    // ===== My methods =====

    // Opens the new world, puts the Human Spawner in it, saves it, and opens the scenes you had again.
    static string SetUpNewWorld()
    {
        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(NewWorldPath, OpenSceneMode.Single);
            string report = SetUpScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return report + " " + Path.GetFileNameWithoutExtension(NewWorldPath) + " saved.";
        }
        finally
        {
            if (before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        }
    }

    // The scene's Human Spawner (made if it has none) with each human type this tool made. Types already on the list
    // keep your numbers (weight, biomes, group size); only their prefabs are filled in again. Tools > World > Build New
    // World Scene calls this too. Returns a sentence for the Console.
    public static string SetUpScene(Scene scene)
    {
        List<(Kind kind, List<GameObject> bodies)> made = new List<(Kind kind, List<GameObject> bodies)>();
        foreach (Kind kind in Kinds)
        {
            List<GameObject> bodies = new List<GameObject>();
            foreach ((string file, string body) in Bodies)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(kind, body));
                if (prefab != null) bodies.Add(prefab);
            }
            if (bodies.Count > 0) made.Add((kind, bodies));
        }
        if (made.Count == 0) return "Human enemies: none built yet (Tools > Enemies > Build Human Enemies).";

        HumanSpawner spawner = FindInScene<HumanSpawner>(scene);
        bool added = spawner == null;
        if (added)
        {
            GameObject holder = new GameObject(SpawnerName);
            SceneManager.MoveGameObjectToScene(holder, scene);
            spawner = holder.AddComponent<HumanSpawner>();
        }

        SerializedObject so = new SerializedObject(spawner);   // the names in quotes must match HumanSpawner's fields
        SerializedProperty list = so.FindProperty("kinds");
        List<string> names = new List<string>();
        foreach ((Kind kind, List<GameObject> bodies) in made)
        {
            SerializedProperty line = FindLine(list, kind.name);
            if (line == null)   // a new line copies the one before it (or starts empty), so every number is set
            {
                list.arraySize++;
                line = list.GetArrayElementAtIndex(list.arraySize - 1);
                line.FindPropertyRelative("name").stringValue = kind.name;
                line.FindPropertyRelative("weight").floatValue = kind.weight;
                line.FindPropertyRelative("grassland").boolValue = kind.grassland;
                line.FindPropertyRelative("hills").boolValue = kind.hills;
                line.FindPropertyRelative("desert").boolValue = kind.desert;
                line.FindPropertyRelative("snow").boolValue = kind.snow;
                line.FindPropertyRelative("groupMin").intValue = kind.groupMin;
                line.FindPropertyRelative("groupMax").intValue = kind.groupMax;
            }
            SerializedProperty prefabs = line.FindPropertyRelative("bodies");
            prefabs.arraySize = bodies.Count;
            for (int i = 0; i < bodies.Count; i++) prefabs.GetArrayElementAtIndex(i).objectReferenceValue = bodies[i];
            names.Add(kind.name);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return "Human enemies: " + SpawnerName + (added ? " added" : " updated") + " (" + string.Join(", ", names) + ").";
    }

    // The animations: moving by speed (Locomotion), working (Work on), a flinch (Hit) and dying (Dead).
    // An attack's measured timing at its play speed (measured = false: HumanBrain keeps its own numbers).
    struct Strike
    {
        public float hit;
        public float done;
        public bool measured;
    }

    static AnimatorController BuildController(GameObject body, List<string> missing, out Strike[] strikes, out string attackReport,
                                              out Strike gather)
    {
        MakeFolders(FolderOf(ControllerPath));
        Dictionary<string, AnimationClip> clips = AnimatorBuildHelper.FindHumanoidClips(body);
        AnimationClip Clip(string name)
        {
            if (clips.TryGetValue(name, out AnimationClip clip)) return clip;
            missing.Add(name);
            return null;
        }

        AnimatorController c = AnimatorBuildHelper.EmptyController(ControllerPath);
        c.AddParameter(EnemyAnimator.SpeedName, AnimatorControllerParameterType.Float);
        c.AddParameter(EnemyAnimator.HitName, AnimatorControllerParameterType.Trigger);
        c.AddParameter(EnemyAnimator.DeadName, AnimatorControllerParameterType.Bool);
        c.AddParameter(HumanBrain.WorkName, AnimatorControllerParameterType.Bool);
        c.AddParameter(EnemyAnimator.AttackName, AnimatorControllerParameterType.Trigger);   // EnemyAnimator.PlayAttack sets it
        c.AddParameter(HumanBrain.AttackKindName, AnimatorControllerParameterType.Int);      // 0 fists, 1 one-handed, 2 two-handed
        c.AddParameter(HumanBrain.AttackingName, AnimatorControllerParameterType.Bool);      // mid-swing: no flinch
        c.AddParameter(HumanBrain.MoveXName, AnimatorControllerParameterType.Float);         // its legs by direction (m/s)
        c.AddParameter(HumanBrain.MoveZName, AnimatorControllerParameterType.Float);
        c.AddParameter(HumanBrain.GatherName, AnimatorControllerParameterType.Trigger);      // picking berries
        c.AddParameter(new AnimatorControllerParameter   // HumanBrain sets it: a smaller body steps faster
        {
            name = HumanBrain.StrideName,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f,
        });
        AnimatorStateMachine sm = c.layers[0].stateMachine;

        // Standing, walking, running, running away: by how fast it moves (Speed, in m/s; EnemyAnimator sets it).
        // Standing, walking, running, strafing, backpedalling, sprinting: by which way and how fast it moves in its own
        // space (MoveX / MoveZ, m/s; HumanBrain sets them), like the player's legs.
        AnimatorState locomotion = c.CreateBlendTreeInController("Locomotion", out BlendTree move, 0);
        move.blendType = BlendTreeType.FreeformDirectional2D;
        move.blendParameter = HumanBrain.MoveXName;
        move.blendParameterY = HumanBrain.MoveZName;
        foreach ((string name, Vector2 at, float playSpeed) in Legs)
        {
            bool must = name == IdleClip || name == RunClip;
            AnimationClip clip = must ? Clip(name) : clips.TryGetValue(name, out AnimationClip found) ? found : null;
            AddClip(move, clip, at, playSpeed);
        }
        locomotion.speedParameterActive = true;   // its walk and run play StrideSpeed times as fast
        locomotion.speedParameter = HumanBrain.StrideName;
        sm.defaultState = locomotion;

        AnimatorState work = AnimatorBuildHelper.AddState(sm, HumanBrain.WorkStateName, Clip(WorkClip), new Vector3(300f, 140f));
        AnimatorStateTransition toWork = locomotion.AddTransition(work);
        toWork.AddCondition(AnimatorConditionMode.If, 0f, HumanBrain.WorkName);
        AnimatorBuildHelper.Instant(toWork, 0.2f);
        AnimatorStateTransition stopWork = work.AddTransition(locomotion);
        stopWork.AddCondition(AnimatorConditionMode.IfNot, 0f, HumanBrain.WorkName);
        AnimatorBuildHelper.Instant(stopWork, 0.2f);

        AnimatorState death = AnimatorBuildHelper.AddState(sm, "Death", Clip(DeathClip), new Vector3(300f, 260f));
        AnimatorState hit = AnimatorBuildHelper.AddState(sm, "Hit", Clip(HitClip), new Vector3(560f, 60f));

        // Any State arrows are checked in the order they're made: dying first, then a flinch.
        AnimatorStateTransition die = sm.AddAnyStateTransition(death);
        die.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.DeadName);
        AnimatorBuildHelper.Instant(die, 0.1f);
        AnimatorStateTransition flinch = sm.AddAnyStateTransition(hit);
        flinch.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.HitName);
        flinch.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
        flinch.AddCondition(AnimatorConditionMode.IfNot, 0f, HumanBrain.AttackingName);   // a swing isn't broken off by a hit
        AnimatorBuildHelper.Instant(flinch, 0.05f);
        AnimatorBuildHelper.AfterPlaying(hit, locomotion, 0.9f, 0.15f);

        // Fighting back (and punching a tree or rock it has no tool for): an Actions layer for the upper body only, on
        // top of the legs (like the player's), so it swings while it runs, strafes or steps in. Any State -> the attack
        // for what it holds (AttackKind) on Attack, back to Empty once the move is over (measured: the clips stand still
        // for a long while after it); dying stops it at once.
        c.AddLayer(ActionLayerName);
        AnimatorControllerLayer[] layers = c.layers;   // a copy: change it, then give it back
        layers[1].defaultWeight = 1f;
        layers[1].avatarMask = UpperBodyMask();
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        c.layers = layers;
        AnimatorStateMachine actions = c.layers[1].stateMachine;
        AnimatorState empty = AnimatorBuildHelper.AddState(actions, "Empty", null, new Vector3(300f, 110f));
        actions.defaultState = empty;   // no animation here = the legs' layer shows through
        AnimatorStateTransition stop = actions.AddAnyStateTransition(empty);
        stop.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.DeadName);
        AnimatorBuildHelper.Instant(stop, 0.1f);

        strikes = new Strike[Attacks.Length];
        List<string> timings = new List<string>();
        for (int i = 0; i < Attacks.Length; i++)
        {
            AnimationClip clip = Clip(Attacks[i].clip);
            AnimatorState attack = AnimatorBuildHelper.AddState(actions, Attacks[i].state, clip, new Vector3(560f, 160f + 70f * i));
            attack.speed = AttackPlaySpeed;
            AnimatorStateTransition go = actions.AddAnyStateTransition(attack);
            go.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.AttackName);
            go.AddCondition(AnimatorConditionMode.Equals, Attacks[i].kind, HumanBrain.AttackKindName);
            go.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
            AnimatorBuildHelper.Instant(go, 0.1f);

            float exit = 0.9f;
            if (clip != null && MeasureStrike(body, clip, out float blow, out float over))
            {
                strikes[i] = new Strike { hit = blow / AttackPlaySpeed, done = over / AttackPlaySpeed, measured = true };
                exit = Mathf.Clamp(over / clip.length, 0.1f, 1f);
                timings.Add(Attacks[i].state + " lands " + strikes[i].hit.ToString("0.00") + " s, over " + strikes[i].done.ToString("0.00") + " s");
            }
            else
            {
                timings.Add(Attacks[i].state + " couldn't be measured (HumanBrain keeps its numbers)");
            }
            AnimatorBuildHelper.AfterPlaying(attack, empty, exit, 0.15f);
        }
        attackReport = string.Join("; ", timings);

        // Picking berries: Any State -> Gather (Blink's Gathering: bend down and pick) on Gather, back when it's done.
        // The pick lands when the hand reaches the bottom (measured like the work swing).
        gather = new Strike();
        if (clips.TryGetValue(GatherClip, out AnimationClip gatherClip))
        {
            AnimatorState picking = AnimatorBuildHelper.AddState(sm, "Gather", gatherClip, new Vector3(300f, 380f));
            AnimatorStateTransition pick = sm.AddAnyStateTransition(picking);
            pick.AddCondition(AnimatorConditionMode.If, 0f, HumanBrain.GatherName);
            pick.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
            AnimatorBuildHelper.Instant(pick, 0.15f);
            AnimatorBuildHelper.AfterPlaying(picking, locomotion, 0.95f, 0.2f);
            float? pickAt = MeasureSwing(body, gatherClip, out string _);
            if (pickAt.HasValue)
                gather = new Strike { hit = pickAt.Value * gatherClip.length, done = gatherClip.length * 0.95f, measured = true };
        }

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    // One clip in the blend at "at" m/s, played at "playSpeed" (1 = as made).
    static void AddClip(BlendTree tree, Motion clip, Vector2 at, float playSpeed)
    {
        if (clip == null) return;
        tree.AddChild(clip, at);
        ChildMotion[] children = tree.children;
        children[children.Length - 1].timeScale = playSpeed;
        tree.children = children;
    }

    // One human prefab: the Polytope body as Model, physics, health, animations, gear, loot and behaviour. An existing
    // prefab is emptied and refilled IN PLACE (the Human Spawner's links to it keep working).
    static string BuildPrefab(Kind kind, GameObject bodyPrefab, string body, AnimatorController controller,
                              List<(GameObject prefab, bool twoHanded, float damage)> weapons, List<GameObject> shields,
                              (List<GameObject> axes, List<GameObject> pickaxes) tools, ItemData[] items, float? swingAt,
                              Strike[] strikes, Strike gather)
    {
        string path = PrefabPath(kind, body);
        string title = Path.GetFileNameWithoutExtension(path);
        MakeFolders(PrefabFolder);
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(title);
        try
        {
            // The numbers changed on the old prefab are kept (its lists are filled in again below).
            string brainSettings = Settings<HumanBrain>(root);
            if (!CurrentSettings(brainSettings, HumanBrain.SettingsVersion)) brainSettings = null;   // made with older defaults: it gets the new ones (once)
            string gearSettings = Settings<HumanGear>(root);
            if (!CurrentSettings(gearSettings, HumanGear.SettingsVersion)) gearSettings = null;
            string lootSettings = Settings<HumanLoot>(root);
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, root.scene);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;   // Polytope's bodies stand on their own pivot (feet at 0), 1.8 m tall
            model.transform.localRotation = Quaternion.identity;
            float size = kind.size;   // 1 = Polytope's own 1.8 m = the player's character
            model.transform.localScale = Vector3.one * size;
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null) return title + ": " + bodyPrefab.name + " has no Animator, skipped.";
            string slots = AddMissingSlots(model);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;   // HumanBrain moves it: the animations stay on the spot
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;   // off screen its bones aren't posed every frame

            Rigidbody physics = root.AddComponent<Rigidbody>();
            physics.mass = Mass;
            physics.interpolation = RigidbodyInterpolation.Interpolate;                 // smooth between physics steps
            physics.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;  // can't slip through thin walls
            physics.constraints = RigidbodyConstraints.FreezeRotation;                  // never tips over; HumanBrain turns it

            CapsuleCollider shape = root.AddComponent<CapsuleCollider>();
            shape.direction = 1;   // standing up
            shape.height = Height * size;
            shape.radius = Radius * size;
            shape.center = new Vector3(0f, Height * size * 0.5f, 0f);
            shape.sharedMaterial = NoFriction();

            // Marko's health script (public fields): every attack of the player hurts it. Its own coins are off:
            // HumanLoot pays by what it wore and gathered.
            EnemyCombat combat = root.AddComponent<EnemyCombat>();
            combat.maxHealth = kind.health;
            combat.currentHealth = kind.health;
            combat.coinMultiplier = 0f;

            root.AddComponent<EnemyAnimator>();
            HumanGear gear = root.AddComponent<HumanGear>();
            Restore(gearSettings, gear);
            string gearReport = FillGear(gear, model, weapons, shields, tools);
            HumanLoot loot = root.AddComponent<HumanLoot>();
            Restore(lootSettings, loot);
            FillLoot(loot, items);
            HumanBrain brain = root.AddComponent<HumanBrain>();
            Restore(brainSettings, brain);
            SerializedObject brainFields = new SerializedObject(brain);   // the names in quotes must match HumanBrain's fields
            brainFields.FindProperty("kind").enumValueIndex = (int)kind.type;
            if (swingAt.HasValue) brainFields.FindProperty("swingAt").floatValue = swingAt.Value;   // when its tool lands
            if (gather.measured)   // when its hand picks a berry bush
            {
                SerializedProperty picking = brainFields.FindProperty("gatherTiming");
                picking.FindPropertyRelative("hit").floatValue = gather.hit;
                picking.FindPropertyRelative("done").floatValue = gather.done;
            }
            for (int i = 0; i < Attacks.Length && i < strikes.Length; i++)   // when its attacks land and end
            {
                if (!strikes[i].measured) continue;
                SerializedProperty timing = brainFields.FindProperty(Attacks[i].field);
                timing.FindPropertyRelative("hit").floatValue = strikes[i].hit;
                timing.FindPropertyRelative("done").floatValue = strikes[i].done;
            }
            brainFields.ApplyModifiedPropertiesWithoutUndo();
            string effect = DeathEffectBuilder.AddTo(root);   // it turns into particles during its death animation

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return title + ": " + (slots.Length > 0 ? slots + "; " : "") + gearReport + "; " + effect + ".";
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // Lists the body's pieces for HumanGear by their names (PT_Male_Armor_01_A_helmet, ..._cloth_00_body,
    // ..._naked_00_boots, ..._hair_01...), its two slot bones, and Polytope's weapons and shields. Returns a short report.
    static string FillGear(HumanGear gear, GameObject model, List<(GameObject prefab, bool twoHanded, float damage)> weapons, List<GameObject> shields,
                           (List<GameObject> axes, List<GameObject> pickaxes) tools)
    {
        SerializedObject so = new SerializedObject(gear);   // the names in quotes must match HumanGear's fields
        SerializedProperty parts = so.FindProperty("parts");
        parts.ClearArray();
        List<GameObject> heads = new List<GameObject>();
        List<GameObject> hairs = new List<GameObject>();
        List<GameObject> beards = new List<GameObject>();
        List<GameObject> capes = new List<GameObject>();
        int armor = 0;
        int plain = 0;

        foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            GameObject part = skin.gameObject;
            string n = part.name.ToLower();
            if (n.Contains("_head")) { heads.Add(part); continue; }
            if (n.Contains("_hair")) { hairs.Add(part); continue; }
            if (n.Contains("_beard")) { beards.Add(part); continue; }
            if (n.Contains("_cape")) { capes.Add(part); continue; }
            if (!PieceOf(n, out HumanGear.Piece piece, out string word)) continue;

            string style = StyleOf(n, word);
            bool armored = style != "cloth" && style != "naked";
            if (armored) armor++;
            else plain++;
            parts.arraySize++;
            SerializedProperty entry = parts.GetArrayElementAtIndex(parts.arraySize - 1);
            entry.FindPropertyRelative("piece").enumValueIndex = (int)piece;
            entry.FindPropertyRelative("style").stringValue = style;
            entry.FindPropertyRelative("armored").boolValue = armored;
            entry.FindPropertyRelative("part").objectReferenceValue = part;
        }
        SetList(so.FindProperty("heads"), heads);
        SetList(so.FindProperty("hairs"), hairs);
        SetList(so.FindProperty("beards"), beards);
        SetList(so.FindProperty("capes"), capes);

        Transform weaponSlot = FindDeep(model.transform, WeaponSlotName);
        Transform shieldSlot = FindDeep(model.transform, ShieldSlotName);
        so.FindProperty("weaponSlot").objectReferenceValue = weaponSlot;
        so.FindProperty("shieldSlot").objectReferenceValue = shieldSlot;

        SerializedProperty held = so.FindProperty("weapons");
        Dictionary<Object, float> keptDamage = new Dictionary<Object, float>();   // damage numbers already on the prefab
        for (int i = 0; i < held.arraySize; i++)
        {
            SerializedProperty old = held.GetArrayElementAtIndex(i);
            Object oldPrefab = old.FindPropertyRelative("prefab").objectReferenceValue;
            float oldDamage = old.FindPropertyRelative("damage").floatValue;
            if (oldPrefab != null && oldDamage > 0f) keptDamage[oldPrefab] = oldDamage;
        }
        held.ClearArray();
        foreach ((GameObject prefab, bool twoHanded, float damage) in weapons)
        {
            held.arraySize++;
            SerializedProperty entry = held.GetArrayElementAtIndex(held.arraySize - 1);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("twoHanded").boolValue = twoHanded;
            entry.FindPropertyRelative("damage").floatValue = keptDamage.TryGetValue(prefab, out float kept) ? kept : damage;
            entry.FindPropertyRelative("isAxe").boolValue = prefab.name.ToLower().Contains("axe");   // Polytope's war axe chops like an axe
        }
        SetList(so.FindProperty("shields"), shields);
        SetList(so.FindProperty("axes"), tools.axes);
        SetList(so.FindProperty("pickaxes"), tools.pickaxes);
        so.ApplyModifiedPropertiesWithoutUndo();

        return armor + " armor pieces, " + plain + " plain, " + heads.Count + " heads, " + hairs.Count + " hairs, " +
               beards.Count + " beards, " + capes.Count + " capes, " + weapons.Count + " weapons, " + shields.Count + " shields" +
               (weaponSlot == null ? ", NO weapon slot found" : "") + (shieldSlot == null ? ", NO shield slot found" : "");
    }

    // Which armor piece a part is, by a word in its name.
    static bool PieceOf(string name, out HumanGear.Piece piece, out string word)
    {
        (string word, HumanGear.Piece piece)[] words =
        {
            ("_helmet", HumanGear.Piece.Helmet),
            ("_body", HumanGear.Piece.Chestplate),
            ("_legs", HumanGear.Piece.Leggings),
            ("_boots", HumanGear.Piece.Boots),
            ("_gauntlets", HumanGear.Piece.Gauntlets),
        };
        foreach ((string w, HumanGear.Piece p) in words)
        {
            if (!name.Contains(w)) continue;
            piece = p;
            word = w;
            return true;
        }
        piece = HumanGear.Piece.Helmet;
        word = null;
        return false;
    }

    // A part's set: "naked" (bare skin), "cloth" (plain clothes), else the armor's code ("pt_male_armor_01_a_body" -> "01_a").
    static string StyleOf(string name, string word)
    {
        if (name.Contains("naked")) return "naked";
        if (name.Contains("cloth")) return "cloth";
        const string Marker = "armor_";
        int start = name.IndexOf(Marker);
        string rest = start >= 0 ? name.Substring(start + Marker.Length) : name;
        int end = rest.IndexOf(word);
        return end > 0 ? rest.Substring(0, end) : rest;
    }

    static void FillLoot(HumanLoot loot, ItemData[] items)
    {
        SerializedObject so = new SerializedObject(loot);   // the names in quotes must match HumanLoot's fields
        for (int i = 0; i < TierFields.Length; i++) so.FindProperty(TierFields[i]).objectReferenceValue = items[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetList(SerializedProperty list, List<GameObject> items)
    {
        list.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    // A component's settings as text (null if the prefab doesn't have it), to put back after the rebuild.
    static string Settings<T>(GameObject root) where T : Component
    {
        T part = root.GetComponent<T>();
        return part != null ? EditorJsonUtility.ToJson(part) : null;
    }

    // True if a script's saved settings were made with the defaults it has now (its SettingsVersion).
    static bool CurrentSettings(string settings, int version)
    {
        return settings != null && settings.Contains("\"settingsVersion\":" + version);
    }

    // Polytope gave only the man's body the two bones that hold things (the weapon and shield slots), so a woman held
    // nothing: no weapon, no shield, no axe or pickaxe. Copies them from the man's prefab onto the same hand bones: the
    // two skeletons are made the same way, only her hands are a bit smaller (the slots' offsets shrink with them).
    // Returns what it did, for the report ("" = nothing to add).
    static string AddMissingSlots(GameObject model)
    {
        GameObject template = FindPrefab(Bodies[0].file);   // the man's body
        if (template == null) return "";
        List<string> added = new List<string>();
        foreach (string slotName in new[] { WeaponSlotName, ShieldSlotName })
        {
            if (FindDeep(model.transform, slotName) != null) continue;   // it has this one
            Transform from = FindDeep(template.transform, slotName);
            Transform hand = from != null && from.parent != null ? FindDeep(model.transform, from.parent.name) : null;
            if (hand == null) continue;

            float templateHand = HandSize(from.parent);
            float ratio = templateHand > 0f && HandSize(hand) > 0f ? HandSize(hand) / templateHand : 1f;
            GameObject slot = new GameObject(slotName);
            slot.transform.SetParent(hand, false);
            slot.transform.localPosition = from.localPosition * ratio;
            slot.transform.localRotation = from.localRotation;
            slot.transform.localScale = Vector3.one;
            added.Add(slotName);
        }
        return added.Count > 0 ? "added the man's hand slots (" + string.Join(", ", added) + ")" : "";
    }

    // How big a hand is: the longest offset of its finger bones (0 = no fingers found).
    static float HandSize(Transform hand)
    {
        float most = 0f;
        foreach (Transform child in hand)
        {
            if (child.name.EndsWith("_slot")) continue;   // a slot isn't a finger
            most = Mathf.Max(most, child.localPosition.magnitude);
        }
        return most;
    }

    // When an attack's blow lands and when its move is over, in seconds of the clip (false = couldn't tell). It plays
    // the clip frame by frame on a hidden copy of the body and watches both hands (Blink's PunchRight throws its big
    // punch with the left one): the blow lands when a hand is furthest in front of the body, and the move is over
    // when the hands stop for good (Blink's attacks stand still for a long while after: the one-handed slash lands at
    // 0.37 s and is over at 0.87 s of its 1.73 s).
    static bool MeasureStrike(GameObject body, AnimationClip clip, out float hit, out float done)
    {
        hit = 0f;
        done = clip != null ? clip.length : 0f;
        if (body == null || clip == null) return false;
        GameObject copy = Object.Instantiate(body);
        copy.hideFlags = HideFlags.HideAndDontSave;   // never shown, never saved
        PlayableGraph graph = PlayableGraph.Create("Measure " + clip.name);
        try
        {
            Animator animator = copy.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman) return false;
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (right == null || left == null) return false;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // it's hidden; animate it anyway

            AnimationClipPlayable play = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Attack", animator);
            output.SetSourcePlayable(play);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            int count = Mathf.Max(2, Mathf.RoundToInt(clip.length * 60f)) + 1;   // 60 looks per second, both ends
            Transform root = animator.transform;
            Vector3[] r = new Vector3[count];
            Vector3[] l = new Vector3[count];
            int front = 0;
            float most = float.MinValue;
            float least = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                play.SetTime(clip.length * i / (count - 1));
                graph.Evaluate();
                r[i] = root.InverseTransformPoint(right.position);   // in the body's own space: z = in front
                l[i] = root.InverseTransformPoint(left.position);
                float ahead = Mathf.Max(r[i].z, l[i].z);
                if (ahead > most)
                {
                    most = ahead;
                    front = i;
                }
                least = Mathf.Min(least, ahead);
            }
            if (most - least < 0.05f) return false;   // the hands didn't move: the clip couldn't be played here

            int still = count - 1;   // the last look where a hand still moved, after the blow
            while (still > front + 1 && Vector3.Distance(r[still], r[still - 1]) < StillDistance &&
                   Vector3.Distance(l[still], l[still - 1]) < StillDistance)
                still--;
            hit = clip.length * front / (count - 1);
            done = clip.length * Mathf.Min(count - 1, still + 1) / (count - 1);
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(copy);
        }
    }

    // The attacks' mask: the upper body only (no root, legs or feet), so the legs keep running under a swing. Refilled
    // in place if it exists (the controller's link to it stays).
    static AvatarMask UpperBodyMask()
    {
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        bool isNew = mask == null;
        if (isNew) mask = new AvatarMask();
        for (AvatarMaskBodyPart part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
        {
            bool lower = part == AvatarMaskBodyPart.Root || part == AvatarMaskBodyPart.LeftLeg || part == AvatarMaskBodyPart.RightLeg ||
                         part == AvatarMaskBodyPart.LeftFootIK || part == AvatarMaskBodyPart.RightFootIK;
            mask.SetHumanoidBodyPartActive(part, !lower);
        }
        if (isNew) AssetDatabase.CreateAsset(mask, MaskPath);
        else EditorUtility.SetDirty(mask);
        return mask;
    }

    // The clip of the controller's Work state (null = none).
    static AnimationClip WorkClipOf(AnimatorController controller)
    {
        if (controller == null) return null;
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == HumanBrain.WorkStateName) return child.state.motion as AnimationClip;
        }
        return null;
    }

    // When a human's tool lands in its work animation, as a share of one swing (null = couldn't tell; how = what
    // happened, for the report). It plays the clip frame by frame on a hidden copy of the body and watches the right
    // hand: the swing lands when the hand, coming down from its highest point, gets near the bottom. (Blink's MiningLoop:
    // the hand slams down at about 46 % and stays in the rock until 75 %; its lowest point alone would say 71 %, a dip
    // while the pick is stuck, about 0.3 s late.)
    static float? MeasureSwing(GameObject body, AnimationClip clip, out string how)
    {
        if (body == null || clip == null)
        {
            how = "no work animation to measure (HumanBrain > Swing At stays as it is)";
            return null;
        }
        how = clip.name + " couldn't be played here to measure it (HumanBrain > Swing At stays as it is)";
        GameObject copy = Object.Instantiate(body);
        copy.hideFlags = HideFlags.HideAndDontSave;   // never shown, never saved
        PlayableGraph graph = PlayableGraph.Create("Measure " + clip.name);
        try
        {
            Animator animator = copy.GetComponentInChildren<Animator>(true);
            Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (hand == null) return null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // it's hidden; animate it anyway

            AnimationClipPlayable swing = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Swing", animator);
            output.SetSourcePlayable(swing);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            int count = Mathf.Max(2, Mathf.RoundToInt(clip.length * 60f));   // 60 looks per second of the clip
            float[] height = new float[count];
            Transform root = animator.transform;
            for (int i = 0; i < count; i++)
            {
                swing.SetTime(clip.length * i / count);
                graph.Evaluate();
                height[i] = hand.position.y - root.position.y;
            }

            int top = 0;
            float lowest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (height[i] > height[top]) top = i;
                lowest = Mathf.Min(lowest, height[i]);
            }
            float drop = height[top] - lowest;
            if (drop < 0.05f) return null;   // the hand didn't move: the clip couldn't be played here
            for (int step = 1; step < count; step++)   // from the top on, round the loop: the first time it's near the bottom
            {
                int i = (top + step) % count;
                if (height[i] > lowest + drop * BottomShare) continue;
                how = "its tool lands at " + Mathf.RoundToInt(100f * i / count) + " % of each " + clip.name + " swing (HumanBrain > Swing At)";
                return (float)i / count;
            }
            return null;
        }
        catch (System.Exception e)
        {
            how = "couldn't measure " + clip.name + ": " + e.Message + " (HumanBrain > Swing At stays as it is)";
            return null;
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(copy);
        }
    }

    static void Restore(string settings, Object target)
    {
        if (!string.IsNullOrEmpty(settings)) EditorJsonUtility.FromJsonOverwrite(settings, target);
    }

    static string PrefabPath(Kind kind, string body) => PrefabFolder + "/Human " + kind.name + " " + body + ".prefab";

    static List<(GameObject prefab, string body)> FindBodies()
    {
        List<(GameObject prefab, string body)> found = new List<(GameObject prefab, string body)>();
        foreach ((string file, string body) in Bodies)
        {
            GameObject prefab = FindPrefab(file);
            if (prefab != null) found.Add((prefab, body));
        }
        return found;
    }

    static List<(GameObject prefab, bool twoHanded, float damage)> FindWeapons(List<string> missing)
    {
        List<(GameObject prefab, bool twoHanded, float damage)> found = new List<(GameObject prefab, bool twoHanded, float damage)>();
        foreach ((string file, bool twoHanded, float damage) in Weapons)
        {
            GameObject prefab = FindPrefab(file);
            if (prefab != null) found.Add((prefab, twoHanded, damage));
            else missing.Add(file);
        }
        return found;
    }

    static List<GameObject> FindShields(List<string> missing)
    {
        List<GameObject> found = new List<GameObject>();
        foreach (string file in Shields)
        {
            GameObject prefab = FindPrefab(file);
            if (prefab != null) found.Add(prefab);
            else missing.Add(file);
        }
        return found;
    }

    // The game's axes and pickaxes, Wooden ... Diamond (a missing one stays empty, that tier's humans just don't get it).
    static (List<GameObject> axes, List<GameObject> pickaxes) FindTools(out int found)
    {
        List<GameObject> axes = new List<GameObject>();
        List<GameObject> pickaxes = new List<GameObject>();
        found = 0;
        foreach (string tier in ToolTiers)
        {
            GameObject axe = FindPrefab(tier + "Axe");
            GameObject pickaxe = FindPrefab(tier + "Pickaxe");
            axes.Add(axe);
            pickaxes.Add(pickaxe);
            if (axe != null) found++;
            if (pickaxe != null) found++;
        }
        return (axes, pickaxes);
    }

    static ItemData[] FindTierItems(out List<string> missing)
    {
        missing = new List<string>();
        ItemData[] items = new ItemData[TierItems.Length];
        for (int i = 0; i < TierItems.Length; i++)
        {
            items[i] = FindItem(TierItems[i]);
            if (items[i] == null) missing.Add(TierItems[i]);
        }
        return items;
    }

    // A prefab anywhere in the project by its exact file name (null = not there).
    static GameObject FindPrefab(string file)
    {
        foreach (string guid in AssetDatabase.FindAssets(file + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == file) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // An item by its Item Id (the Item Data file's name).
    static ItemData FindItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets(id + " t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.Id == id) return item;
        }
        return null;
    }

    static SerializedProperty FindLine(SerializedProperty list, string name)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty line = list.GetArrayElementAtIndex(i);
            if (line.FindPropertyRelative("name").stringValue == name) return line;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Removes everything from an opened prefab except its top object's Transform: children first, then components, the
    // last added first (HumanBrain and HumanLoot need EnemyCombat, and scripts need the physics: Unity won't remove a
    // part while another part still needs it).
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        MonoBehaviour[] scripts = root.GetComponents<MonoBehaviour>();
        for (int i = scripts.Length - 1; i >= 0; i--) Object.DestroyImmediate(scripts[i]);
        Component[] parts = root.GetComponents<Component>();
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (!(parts[i] is Transform)) Object.DestroyImmediate(parts[i]);
        }
    }

    // A physics material without friction (the monsters' one), so a human slides along walls instead of sticking.
    static PhysicMaterial NoFriction()
    {
        PhysicMaterial material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(NoFrictionPath);
        if (material != null) return material;

        MakeFolders(FolderOf(NoFrictionPath));
        material = new PhysicMaterial("EnemyNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
        };
        AssetDatabase.CreateAsset(material, NoFrictionPath);
        return material;
    }
}
