/*
First Date: 10/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Enemies > Build Monster Enemy.
              Monster: the one selected in the Project window (a monster prefab with an Animator, e.g. TurtleShellPolyart),
              or RPG Monster Duo's SlimePolyart when nothing is selected.
              It makes Assets/Animation/Enemy/<Name>Enemy.controller and Assets/Prefabs/Enemy/<Name>Enemy.prefab
              (e.g. SlimeEnemy), asks before replacing them, then offers to make the scene's EnemySpawner spawn it.
              Undo (Ctrl + Z) works for the EnemySpawner change. To change the size or the starting numbers for every
              future build, edit Fields below. Hand-made steps: Docs/Enemy Setup.md
Purpose: Turns a monster model into a working enemy in one click, using Marko's enemy scripts:
         <Name>Enemy: Rigidbody + Sphere Collider (walls stop it, it walks on the ground), Marko's EnemyCombat,
                      EnemyMovement and EnemyAttack, and Tony's EnemyAnimator
           > Model:   the monster prefab, 1 m tall, feet at the bottom, using the new controller
         The controller: Locomotion (IdleBattle / WalkFWD / RunFWD, picked by Speed), Attack (Attack01), Hit (GetHit),
         Victory, Dead (Die). The clips are taken from the monster's own demo controller, by state name. A monster with
         a shell (the turtle: Defend / DefendGetHit) also gets Shell (Defend, held while the Shell parameter is on) and
         Shell Hit (DefendGetHit, the ShellHit trigger, back to the shell's inside pose), for ShellDefense.
         BuildMonster does the same for a monster by name, without asking (Tools > Enemies > Set Up Night Monsters and
         Tools > World > Build Desert Content: the TurtleShell, the Chest Monster, the Beholder), at its own height, and
         for a flying monster (the Beholder) without gravity, with its collider around its body in the air and
         FlyingEnemy. A rebuild keeps an EnemyAggro, a ShellDefense, a FlyingEnemy (with their settings) and
         DeathEffect's Vanish Color that other tools gave the enemy.
         Works with RPG Monster DUO and RPG Monster Partners (22/11/2026): Partners calls its run "Run" (not RunFWD),
         and its prefabs end in "Default" (ChestMonsterPolyartDefault -> ChestMonsterEnemy).
Requirement: EnemyBuilder.cs x-> UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, EnemyAnimator.cs, EnemyAggro.cs, ShellDefense.cs, FlyingEnemy.cs, EnemyCombat.cs, EnemyMovement.cs, EnemyAttack.cs, EnemySpawner.cs, DeathEffect.cs, DeathEffectBuilder.cs, MenuOrder.cs
             EnemyBuilder.cs <-x NightMonsterBuilder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AnimatorBuildHelper;   // EmptyController, AddState, Instant, AfterPlaying
using static UIBuildHelper;         // FindPrefabPath, FolderOf, MakeFolders

public static class EnemyBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Monster Enemy";
    const string DefaultMonster = "SlimePolyart";   // RPG Monster Duo PBR Polyart's slime, used when nothing is selected
    const string PrefabFolder = "Assets/Prefabs/Enemy";
    const string ControllerFolder = "Assets/Animation/Enemy";
    const string NoFrictionPath = "Assets/Prefabs/Enemy/EnemyNoFriction.physicMaterial";
    const float Height = 1f;   // meters from the ground to the top of the monster (Marko's ball enemy was 1 m too); BuildMonster can pick another
    const float FlyerBall = 0.8f;   // a flying monster's collider: this share of the ball that fits it, around the middle of its body

    // Starting numbers on the new prefab. Marko's EnemySpawner still raises health and damage each day.
    const float MoveSpeed = 2f;       // meters per second (the player walks at 7.8)
    const float StopDistance = 1.5f;  // stops this close to the player: inside the attack range, so its attacks reach
    const float AttackRange = 2f;
    const float Mass = 5f;            // heavier than the player's ball (1), so the player can't shove it away easily

    // Speeds (m/s) where the walk and the run animation play fully; speeds in between blend them.
    const float WalkAt = 2f;
    const float RunAt = 4f;

    // Which of the monster's demo states plays for what (the names RPG Monster Duo uses).
    const string IdleState = "IdleBattle";
    const string WalkState = "WalkFWD";
    const string RunState = "RunFWD";
    const string RunStateToo = "Run";              // RPG Monster Partners' name for it (the Chest Monster, the Beholder)
    const string AttackState = "Attack01";
    const string HitState = "GetHit";
    const string DieState = "Die";
    const string VictoryState = "Victory";
    const string ShellState = "Defend";            // a monster with a shell (the turtle): pulls into it (plays once, then holds)
    const string ShellHitState = "DefendGetHit";   // hit while in its shell

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Enemies/Build Monster Enemy", false, MenuOrder.BuildMonsterEnemy)]
    static void Build()
    {
        GameObject monster = ChosenMonster();
        string problem = Problem(monster);
        if (problem != null)
        {
            EditorUtility.DisplayDialog(ToolName, problem, "OK");
            return;
        }

        string name = EnemyName(monster);   // "SlimePolyart" -> "SlimeEnemy"
        string prefabPath = FindPrefabPath(PrefabFolder + "/" + name + ".prefab");
        string controllerPath = ControllerFolder + "/" + name + ".controller";

        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null ||
                      AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null;
        if (exists && !EditorUtility.DisplayDialog(ToolName,
                name + " already exists. Build it again and replace it?\n(Changes made to its prefab or controller are lost.)",
                "Replace", "Cancel"))
        {
            return;
        }

        List<string> missing = new List<string>();
        AnimatorController controller = BuildController(controllerPath, ClipsByState(monster), missing);
        GameObject prefab = BuildPrefab(monster, name, prefabPath, controller);
        DeathEffectBuilder.MakeMeshesReadable();   // now that the prefab is closed: the particles may read its mesh
        if (missing.Count > 0)
        {
            Debug.LogWarning(ToolName + ": " + monster.name + " has no state called " + string.Join(", ", missing) +
                             ", so those states play nothing. Drag a clip into each one's Motion in the Animator window.");
        }
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(ToolName, "Couldn't save " + prefabPath + ". See the Console for why.", "OK");
            return;
        }

        UseInSpawner(prefab);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log(name + " built: " + prefabPath + ". Test it in Play mode: select the EnemySpawner, click the ⋮ on its " +
                  "EnemySpawner component > Test Spawn Enemy.");
    }

    // ===== My methods =====

    // Builds (refills) the enemy for one of the monster prefabs by its file name (e.g. "TurtleShellPolyart"), without
    // asking and without touching the EnemySpawner: its controller and prefab, like the menu. Tools > Enemies > Set Up
    // Night Monsters uses it for the night monsters. height = meters from the ground to its top (1 = like the slime);
    // flying = it flies (no gravity, its collider around its body, FlyingEnemy). Returns the prefab (null = no such
    // monster, or it couldn't be saved); "report" says what happened.
    public static GameObject BuildMonster(string monsterName, out string report, float height = Height, bool flying = false)
    {
        GameObject monster = FindMonster(monsterName);
        if (monster == null)
        {
            report = "no monster prefab called " + monsterName + " in the project (import its pack)";
            return null;
        }
        string problem = Problem(monster);
        if (problem != null)
        {
            report = problem.Replace("\n\n", " ");
            return null;
        }

        string name = EnemyName(monster);
        string prefabPath = FindPrefabPath(PrefabFolder + "/" + name + ".prefab");
        List<string> missing = new List<string>();
        AnimatorController controller = BuildController(ControllerFolder + "/" + name + ".controller", ClipsByState(monster), missing);
        GameObject prefab = BuildPrefab(monster, name, prefabPath, controller, height, flying);
        DeathEffectBuilder.MakeMeshesReadable();
        report = prefab == null ? "couldn't save " + prefabPath
                                : prefabPath + (missing.Count > 0 ? " (no " + string.Join(", ", missing) + " animation)" : "");
        return prefab;
    }

    // The monster to use: a prefab with an Animator selected in the Project window, else RPG Monster Duo's slime.
    static GameObject ChosenMonster()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected != null && EditorUtility.IsPersistent(selected)          // a file, not a scene object
            && selected.GetComponentInChildren<Animator>(true) != null
            && selected.GetComponent<EnemyCombat>() == null)                  // not an enemy prefab already
        {
            return selected;
        }
        return FindMonster(DefaultMonster);
    }

    // A monster prefab by its file name, anywhere in Assets, or null.
    static GameObject FindMonster(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == fileName)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // Why this monster can't be used (null = it can).
    static string Problem(GameObject monster)
    {
        if (monster == null)
        {
            return "No monster found.\n\nImport \"RPG Monster Duo PBR Polyart\" (Window > Package Manager > My Assets), or select " +
                   "a monster prefab with an Animator in the Project window, then run this again.";
        }
        Animator animator = monster.GetComponentInChildren<Animator>(true);
        if (!(animator.runtimeAnimatorController is AnimatorController))
        {
            return monster.name + "'s Animator has no Animator Controller, so the tool can't find its animations.\n\n" +
                   "Give it the controller that came with it (e.g. Animators/Slime.controller), then run this again.";
        }
        return null;
    }

    // "SlimePolyart" -> "SlimeEnemy", "TurtleShellPBR" -> "TurtleShellEnemy", "ChestMonsterPolyartDefault" ->
    // "ChestMonsterEnemy" (RPG Monster Partners' prefabs end in "Default").
    static string EnemyName(GameObject monster)
    {
        string name = monster.name.Replace("Polyart", "").Replace("PBR", "").Replace("Default", "").Replace(" ", "");
        return (name.Length > 0 ? name : "Monster") + "Enemy";
    }

    // The monster's demo controller: each state's name -> its animation (e.g. "WalkFWD" -> the slime's walk).
    static Dictionary<string, Motion> ClipsByState(GameObject monster)
    {
        Dictionary<string, Motion> clips = new Dictionary<string, Motion>();
        AnimatorController demo = (AnimatorController)monster.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
        foreach (AnimatorControllerLayer layer in demo.layers) Collect(layer.stateMachine, clips);
        return clips;
    }

    static void Collect(AnimatorStateMachine machine, Dictionary<string, Motion> clips)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.motion != null && !clips.ContainsKey(child.state.name)) clips[child.state.name] = child.state.motion;
        }
        foreach (ChildAnimatorStateMachine inner in machine.stateMachines) Collect(inner.stateMachine, clips);   // state groups
    }

    // Builds the enemy's controller from scratch, inside the same file if it already exists.
    static AnimatorController BuildController(string path, Dictionary<string, Motion> clips, List<string> missing)
    {
        MakeFolders(FolderOf(path));
        AnimatorController c = EmptyController(path);
        c.AddParameter(EnemyAnimator.SpeedName, AnimatorControllerParameterType.Float);
        c.AddParameter(EnemyAnimator.AttackName, AnimatorControllerParameterType.Trigger);
        c.AddParameter(EnemyAnimator.HitName, AnimatorControllerParameterType.Trigger);
        c.AddParameter(EnemyAnimator.DeadName, AnimatorControllerParameterType.Bool);
        c.AddParameter(EnemyAnimator.VictoryName, AnimatorControllerParameterType.Trigger);

        // A clip by its demo state name, or by the first of several names the monster has (the run: RunFWD, or Run in
        // RPG Monster Partners); null + noted as missing if it has none of them.
        Motion Clip(params string[] states)
        {
            foreach (string state in states)
            {
                if (clips.TryGetValue(state, out Motion motion)) return motion;
            }
            missing.Add(states[0]);
            return null;
        }

        AnimatorStateMachine sm = c.layers[0].stateMachine;

        // Standing, walking, running: picked by how fast the enemy moves (Speed, in m/s).
        AnimatorState locomotion = c.CreateBlendTreeInController("Locomotion", out BlendTree move, 0);
        move.blendType = BlendTreeType.Simple1D;
        move.blendParameter = EnemyAnimator.SpeedName;
        move.useAutomaticThresholds = false;
        AddClip(move, Clip(IdleState), 0f);
        AddClip(move, Clip(WalkState), WalkAt);
        AddClip(move, Clip(RunState, RunStateToo), RunAt);
        sm.defaultState = locomotion;

        AnimatorState dead = AddState(sm, "Dead", Clip(DieState), new Vector3(300f, 260f));
        AnimatorState hit = AddState(sm, "Hit", Clip(HitState), new Vector3(560f, 60f));
        AnimatorState attack = AddState(sm, "Attack", Clip(AttackState), new Vector3(560f, 140f));
        AnimatorState victory = AddState(sm, "Victory", Clip(VictoryState), new Vector3(560f, 220f));

        // Any State arrows are checked in the order they're made: dying first, then being hit, then attacking.
        AnimatorStateTransition t = sm.AddAnyStateTransition(dead);
        t.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.DeadName);
        Instant(t, 0.1f);
        PlayOnce(sm, hit, EnemyAnimator.HitName, locomotion);
        PlayOnce(sm, attack, EnemyAnimator.AttackName, locomotion);
        PlayOnce(sm, victory, EnemyAnimator.VictoryName, locomotion);
        AddShell(c, sm, clips, locomotion, new[] { locomotion, hit, attack, victory });

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    // A monster with a shell (its demo controller has Defend; the turtle): ShellDefense pulls it in with the Shell
    // parameter (Defend plays once and holds its last pose, inside the shell) and a hit on the shell plays DefendGetHit
    // (the ShellHit trigger), then goes back to the inside pose (offset 0.99, so it doesn't pull in again). Shell off =
    // back to moving. Not for other monsters (no Defend clip): nothing is added.
    static void AddShell(AnimatorController c, AnimatorStateMachine sm, Dictionary<string, Motion> clips,
                         AnimatorState locomotion, AnimatorState[] from)
    {
        if (!clips.TryGetValue(ShellState, out Motion hide) || hide == null) return;

        c.AddParameter(EnemyAnimator.ShellName, AnimatorControllerParameterType.Bool);
        c.AddParameter(EnemyAnimator.ShellHitName, AnimatorControllerParameterType.Trigger);
        AnimatorState shell = AddState(sm, "Shell", hide, new Vector3(300f, 340f));
        foreach (AnimatorState state in from)   // from these, not Any State (that would restart it every frame)
        {
            AnimatorStateTransition inside = state.AddTransition(shell);
            inside.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.ShellName);
            inside.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
            Instant(inside, 0.1f);
        }
        AnimatorStateTransition outside = shell.AddTransition(locomotion);
        outside.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.ShellName);
        Instant(outside, 0.2f);

        if (!clips.TryGetValue(ShellHitState, out Motion knock) || knock == null) return;
        AnimatorState shellHit = AddState(sm, "Shell Hit", knock, new Vector3(560f, 340f));
        AnimatorStateTransition hitIt = sm.AddAnyStateTransition(shellHit);
        hitIt.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimator.ShellHitName);
        hitIt.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
        Instant(hitIt, 0.05f);
        AnimatorStateTransition back = AfterPlaying(shellHit, shell, 0.9f, 0.1f);
        back.offset = 0.99f;   // straight to the shell's last pose: still inside
    }

    // A state that plays once when its trigger is set (not while dead), then goes back to moving.
    // (Some of the monster's clips are set to loop; going back at 90 % plays them just once.)
    static void PlayOnce(AnimatorStateMachine sm, AnimatorState state, string trigger, AnimatorState back)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimator.DeadName);
        Instant(t, 0.05f);
        AfterPlaying(state, back, 0.9f, 0.15f);
    }

    static void AddClip(BlendTree tree, Motion clip, float speed)
    {
        if (clip != null) tree.AddChild(clip, speed);
    }

    // The enemy prefab: physics, Marko's scripts, EnemyAnimator, and the monster as its Model child, "height" meters tall.
    // A flying one (or one that flew before: it had FlyingEnemy) has no gravity, its collider around the middle of its
    // body and FlyingEnemy.
    // An existing prefab is emptied and refilled IN PLACE: its top object stays the same, so links to it (the
    // EnemySpawner's Enemy Prefab) keep working. A brand-new object saved over it could break those links.
    static GameObject BuildPrefab(GameObject monster, string name, string path, AnimatorController controller,
                                  float height = Height, bool flying = false)
    {
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        try
        {
            // What other tools gave it, kept through the rebuild: EnemyAggro (it waits until the player comes near, e.g.
            // the desert's turtle) and DeathEffect's Vanish Color / Rise (the turtle dissolves into falling sand).
            bool waits = existed && root.GetComponent<EnemyAggro>() != null;
            ShellDefense oldShell = existed ? root.GetComponent<ShellDefense>() : null;   // the turtle's shell, with its settings
            string shellSettings = oldShell != null ? EditorJsonUtility.ToJson(oldShell) : null;
            FlyingEnemy oldFlyer = existed ? root.GetComponent<FlyingEnemy>() : null;     // a flying monster, with its settings
            string flyerSettings = oldFlyer != null ? EditorJsonUtility.ToJson(oldFlyer) : null;
            bool flies = flying || oldFlyer != null;
            DeathEffect oldEffect = existed ? root.GetComponent<DeathEffect>() : null;
            SerializedObject oldLook = oldEffect != null ? new SerializedObject(oldEffect) : null;
            Color? vanishColor = oldLook != null ? oldLook.FindProperty("vanishColor").colorValue : (Color?)null;
            float vanishRise = oldLook != null ? oldLook.FindProperty("vanishRise").floatValue : 0f;
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the world's middle, so Fit's measuring is simple
            root.transform.localScale = Vector3.one;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(monster, root.scene);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            Animator animator = model.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;   // the movement script moves the enemy
            float radius = Fit(model.transform, height);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = Mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;              // smooth between physics steps
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // can't slip through thin walls
            body.constraints = RigidbodyConstraints.FreezeRotation;              // never tips over; EnemyMovement turns it
            body.useGravity = !flies;                                            // a flying one holds itself up (FlyingEnemy)

            SphereCollider shape = root.AddComponent<SphereCollider>();
            shape.radius = flies ? radius * FlyerBall : radius;
            shape.center = new Vector3(0f, flies ? height * 0.5f : radius, 0f);   // on the ground: its bottom under the feet; flying: its body
            shape.sharedMaterial = NoFriction();

            // Marko's scripts (public fields, so set directly). The EnemySpawner sets their targets.
            root.AddComponent<EnemyCombat>();
            EnemyMovement movement = root.AddComponent<EnemyMovement>();
            movement.moveSpeed = MoveSpeed;
            movement.stopDistance = StopDistance;
            EnemyAttack attack = root.AddComponent<EnemyAttack>();
            attack.attackRange = AttackRange;

            root.AddComponent<EnemyAnimator>();
            DeathEffectBuilder.AddTo(root);   // its body turns into red particles during the death animation
            if (waits) root.AddComponent<EnemyAggro>();
            if (shellSettings != null) EditorJsonUtility.FromJsonOverwrite(shellSettings, root.AddComponent<ShellDefense>());
            if (flies)
            {
                FlyingEnemy flyer = root.AddComponent<FlyingEnemy>();
                if (flyerSettings != null) EditorJsonUtility.FromJsonOverwrite(flyerSettings, flyer);
            }
            DeathEffect effect = root.GetComponent<DeathEffect>();
            if (vanishColor.HasValue && effect != null)
            {
                SerializedObject so = new SerializedObject(effect);   // the names in quotes must match DeathEffect's fields
                so.FindProperty("vanishColor").colorValue = vanishColor.Value;
                so.FindProperty("vanishRise").floatValue = vanishRise;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // Removes everything from an opened prefab except its top object's Transform: children first, then components
    // (scripts before physics, so nothing is removed while another part still needs it).
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(script);
        foreach (Component part in root.GetComponents<Component>())
        {
            if (!(part is Transform)) Object.DestroyImmediate(part);
        }
    }

    // Scales the model to "height" meters, stands its feet on the root (ground level), centers it over the root, and
    // returns the radius of a ball that fits inside it (for the collider).
    static float Fit(Transform model, float height)
    {
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        Bounds look = MeshBounds.Of(model.gameObject);    // the size you see, as it comes (the root is at the world's middle)
        if (look.size.y < 0.01f) return height * 0.5f;    // nothing to measure: leave it as it is

        float scale = height / look.size.y;
        model.localScale = Vector3.one * scale;
        model.localPosition = new Vector3(-look.center.x, -look.min.y, -look.center.z) * scale;

        float width = Mathf.Max(look.size.x, look.size.z) * scale;
        return 0.5f * Mathf.Min(height, width);
    }

    // A physics material without friction, so the enemy slides along walls instead of sticking to them.
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

    // Offers to make the open scene's EnemySpawner spawn the new enemy at night.
    static void UseInSpawner(GameObject prefab)
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.Log("No EnemySpawner in the open scene: drag " + prefab.name + " into an EnemySpawner's Enemy Prefab yourself.");
            return;
        }
        if (spawner.enemyPrefab == prefab) return;   // already spawns it

        string now = spawner.enemyPrefab != null ? spawner.enemyPrefab.name : "nothing";
        if (!EditorUtility.DisplayDialog(ToolName,
                "The EnemySpawner spawns " + now + " at night. Make it spawn " + prefab.name + " instead?\n" +
                "(Ctrl + Z undoes it. Save the scene to keep it.)", "Use " + prefab.name, "Keep " + now))
        {
            return;
        }
        Undo.RecordObject(spawner, ToolName);
        spawner.enemyPrefab = prefab;
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
    }
}
