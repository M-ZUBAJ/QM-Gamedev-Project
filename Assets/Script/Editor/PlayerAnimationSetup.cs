/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: The one tool for the player's animations. For a new character: Tools > Player > Add Character Model, then
              Tools > Player > Add First Person Hands, then Tools > Player > Set Up Player Animations (GameScene open).
              Run it again whenever you add Mixamo files (to Assets/Imported3D/Mixamo, e.g. mining), rebuild the character
              or the hands, or the animations look out of step. It only puts things right: running it twice changes nothing.
              Hand-made steps: Docs/Mixamo Animations.md
Purpose: Puts the player's animations into their correct setting, in one click:
         1. Mixamo files (picked by words in their names, the Uses table, e.g. BotPunchingLeft = the left jab): Humanoid
            import (Avatar: Create From This Model), the clip named after the file, played once, root motion baked in.
         2. Both controllers, the body's (PlayerCharacter, Actions layer) and the first-person hands' (FirstPersonHands):
            each action state gets its Mixamo clip (new actions get their state, trigger and arrows), retired states go
            (HeavyUppercut), and EVERY action plays at PlayerAnimator.ActionSpeedOf, takes over in ActionBlendSeconds and
            starts again on a new click, so the hands and the body's shadow move together.
         3. The first-person arms: cut at the elbow and drawn from both sides (FirstPersonArmsHelper): no open shoulder,
            no see-through holes.
         4. The Player: PunchCombo (left click jabs, right click heavy punches), and PunchEffect's delays measured from
            the clips (when each fist or tool lands). Components whose script was deleted (e.g. the old ToolEquip) are
            taken off the Player.
         One file can play several actions: BotAxeAttackDownward is the swing of every tool (Attack and Mine).
Requirement: PlayerAnimationSetup.cs x-> AnimatorBuildHelper.cs, FirstPersonArmsHelper.cs, PunchEffectBuilder.cs, PlayerAnimator.cs, PunchEffect.cs, PunchCombo.cs, PlayerInventory.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AnimatorBuildHelper;   // AddState, Instant, AfterPlaying
using static UIBuildHelper;         // AddToPlayer, FindPrefabPath

public static class PlayerAnimationSetup
{
    // ===== Fields =====
    const string ToolName = "Set Up Player Animations";
    static readonly string[] SearchFolders = { "Assets/Imported3D", "Assets/Animation" };   // Mixamo files: Assets/Imported3D/Mixamo
    const string BodyControllerPath = "Assets/Animation/Player/PlayerCharacter.controller";    // Add Character Model
    const string HandsControllerPath = "Assets/Animation/Player/FirstPersonHands.controller";  // Add First Person Hands
    static string HandsPrefabPath => FindPrefabPath("Assets/Prefabs/Player/FirstPersonHands.prefab");

    // A word in a Mixamo file's name -> the actions it plays. Read top to bottom: an earlier word wins an action, so a
    // clip made for one tool (a future "Mining" file) comes before the shared one (AxeAttack).
    static readonly (string word, PlayerAction[] actions)[] Uses =
    {
        ("LeftHook", new[] { PlayerAction.HeavyHook }),                   // BotHeavyLeftHook: right click (1st, 3rd...)
        ("HeavyPunch", new[] { PlayerAction.HeavyPunch }),                // BotHeavyPunchRight: right click (2nd, 4th...)
        ("PunchingLeft", new[] { PlayerAction.PunchLeft }),               // BotPunchingLeft: left click (1st, 3rd...)
        ("PunchingRight", new[] { PlayerAction.Punch }),                  // BotPunchingRight: left click (2nd, 4th...)
        ("GetHit", new[] { PlayerAction.Hit }),                           // BotGetHitBody: the flinch when hurt (body only)
        ("Mining", new[] { PlayerAction.Mine }),                          // a pickaxe-only swing (none yet)
        ("Slash", new[] { PlayerAction.Attack }),                         // a sword / axe-only swing (none yet)
        ("SwordSwing", new[] { PlayerAction.Attack }),
        ("AxeAttack", new[] { PlayerAction.Attack, PlayerAction.Mine }),  // BotAxeAttackDownward: every tool's swing
    };

    // Actions that were dropped: their states and triggers are taken out of the controllers.
    // 13/11/2026: right click's left punch is now the left hook; tools no longer play the Unarmed equip animations.
    static readonly string[] Retired = { "HeavyUppercut", "EquipUnderarm", "EquipOverShoulder" };

    // PunchEffect's delay field for each action it times.
    static readonly (PlayerAction action, string field)[] Delays =
    {
        (PlayerAction.Punch, "impactDelay"), (PlayerAction.PunchLeft, "leftJabDelay"),
        (PlayerAction.HeavyHook, "hookDelay"), (PlayerAction.HeavyPunch, "heavyPunchDelay"),
        (PlayerAction.Attack, "swingDelay"), (PlayerAction.Mine, "mineDelay"),
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Player/Set Up Player Animations", false, MenuOrder.SetUpPlayerAnimations)]
    static void Setup()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(BodyControllerPath) == null &&
            AssetDatabase.LoadAssetAtPath<AnimatorController>(HandsControllerPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no player character yet.\n\nRun Tools > Player > Add Character Model (and " +
                                        "Add First Person Hands) first, then this.", "OK");
            return;
        }

        List<string> report = new List<string>();
        Dictionary<PlayerAction, AnimationClip> clips = PrepareMixamoClips(report);

        report.Add("body controller: " + FixController(BodyControllerPath, clips, true));
        report.Add("hands controller: " + FixController(HandsControllerPath, clips, false));
        AssetDatabase.SaveAssets();

        report.Add(FixArms());
        report.Add(SetUpPlayer(clips));
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ":\n- " + string.Join("\n- ", report.FindAll(line => !string.IsNullOrEmpty(line))) +
                  "\nSave the scene, then Play: left click jabs, right click heavy punches; with a tool, click to swing it (every hit bursts wind).");
    }

    // ===== My methods =====

    // ----- 1. Mixamo files -----

    // Every Mixamo file the Uses table knows, made Humanoid with its clip named after the file. Action -> clip.
    static Dictionary<PlayerAction, AnimationClip> PrepareMixamoClips(List<string> report)
    {
        Dictionary<PlayerAction, AnimationClip> clips = new Dictionary<PlayerAction, AnimationClip>();
        List<string> used = new List<string>();
        List<string> problems = new List<string>();
        Dictionary<string, AnimationClip> prepared = new Dictionary<string, AnimationClip>();   // each file only once
        foreach (KeyValuePair<PlayerAction, string> file in FindFiles())
        {
            if (!prepared.TryGetValue(file.Value, out AnimationClip clip))
            {
                clip = PrepareClip(file.Value);
                prepared[file.Value] = clip;
                if (clip == null || !clip.humanMotion) problems.Add(System.IO.Path.GetFileName(file.Value));
            }
            if (clip == null || !clip.humanMotion) continue;
            clips[file.Key] = clip;
            used.Add(clip.name + " = " + file.Key);
        }
        report.Add(used.Count > 0 ? "Mixamo clips: " + string.Join(", ", used) : "no Mixamo files in " + string.Join(" or ", SearchFolders) + " (Blink's clips stay)");
        if (problems.Count > 0)
        {
            Debug.LogWarning(ToolName + ": " + string.Join(", ", problems) + " couldn't be made Humanoid, so they're skipped. Select " +
                             "the file > Rig > Animation Type Humanoid, Avatar Definition Create From This Model > Apply, then run this again.");
        }
        return clips;
    }

    // The model file for each action: the first file whose name has a Uses word, taking the words top to bottom (an
    // action already taken by an earlier word keeps that file).
    static Dictionary<PlayerAction, string> FindFiles()
    {
        Dictionary<PlayerAction, string> files = new Dictionary<PlayerAction, string>();
        string[] folders = System.Array.FindAll(SearchFolders, AssetDatabase.IsValidFolder);
        if (folders.Length == 0) return files;
        List<string> paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", folders)) paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach ((string word, PlayerAction[] actions) in Uses)
        {
            string path = paths.Find(p => System.IO.Path.GetFileNameWithoutExtension(p).ToLower().Contains(word.ToLower()));
            if (path == null) continue;
            foreach (PlayerAction action in actions)
            {
                if (!files.ContainsKey(action)) files[action] = path;
            }
        }
        return files;
    }

    // Sets a Mixamo file to Humanoid with one clip named after the file, played once, root motion baked into the pose
    // (In Place: the body stays where the ball is). Returns that clip (null = no animation in the file).
    static AnimationClip PrepareClip(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return null;
        string name = System.IO.Path.GetFileNameWithoutExtension(path);

        if (importer.animationType != ModelImporterAnimationType.Human || !importer.importAnimation)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();   // read the file again as Humanoid before choosing its clip
        }

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0) return null;
        ModelImporterClipAnimation clip = importer.clipAnimations.Length > 0 ? importer.clipAnimations[0] : defaults[0];
        bool alreadyRight = importer.clipAnimations.Length == 1 && clip.name == name && !clip.loopTime &&
                            clip.lockRootRotation && clip.lockRootHeightY && clip.lockRootPositionXZ;
        if (!alreadyRight)
        {
            clip.name = name;                 // "mixamo.com" in every file otherwise: the file's name tells them apart
            clip.loopTime = false;            // a punch plays once
            clip.lockRootRotation = true;     // "Bake Into Pose": the clip can't turn or move the character's root
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
        }

        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (part is AnimationClip found && found.name == name) return found;
        }
        return null;
    }

    // ----- 2. The controllers -----

    // One controller put right: retired states out, Mixamo clips in (new actions get their state), then every action
    // state's speed and arrows set to the shared timing. body = PlayerCharacter (Actions layer, back to Empty, not while
    // dead); else the hands (base layer, back to Idle, no flinch). Returns a short report.
    static string FixController(string path, Dictionary<PlayerAction, AnimationClip> clips, bool body)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) return "not found (" + (body ? "Add Character Model" : "Add First Person Hands") + " makes it)";

        AnimatorStateMachine machine = null;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (!body || layer.name == PlayerAnimator.ActionLayerName)
            {
                machine = layer.stateMachine;
                break;
            }
        }
        if (machine == null) return "no Actions layer: rebuild it with Add Character Model";

        int removed = RemoveRetired(controller, machine);
        AnimatorState back = FindState(machine, body ? PlayerAnimator.EmptyStateName : "Idle");
        int filled = 0;
        foreach (KeyValuePair<PlayerAction, AnimationClip> pair in clips)
        {
            if (!body && pair.Key == PlayerAction.Hit) continue;
            string name = pair.Key.ToString();
            AnimatorState state = FindState(machine, name);
            if (state == null) state = AddAction(controller, machine, name, body, back);
            state.motion = pair.Value;
            EditorUtility.SetDirty(state);
            filled++;
        }

        int timed = 0;
        foreach (AnimatorControllerLayer layer in controller.layers) timed += SyncTiming(layer.stateMachine);
        EditorUtility.SetDirty(controller);
        return filled + " Mixamo clips, " + timed + " timing fixes" + (removed > 0 ? ", " + removed + " retired states removed" : "");
    }

    // A new action state like the builders make: its trigger, an arrow from Any State and an arrow back once it played.
    static AnimatorState AddAction(AnimatorController controller, AnimatorStateMachine machine, string name, bool body, AnimatorState back)
    {
        bool hasTrigger = false;
        foreach (AnimatorControllerParameter p in controller.parameters) hasTrigger |= p.name == name;
        if (!hasTrigger) controller.AddParameter(name, AnimatorControllerParameterType.Trigger);

        AnimatorState state = AddState(machine, name, null, new Vector3(body ? 600f : 560f, 70f * machine.states.Length));
        AnimatorStateTransition arrow = machine.AddAnyStateTransition(state);
        arrow.AddCondition(AnimatorConditionMode.If, 0f, name);
        if (body) arrow.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
        Instant(arrow, PlayerAnimator.ActionBlendSeconds);
        if (back != null) AfterPlaying(state, back, 0.85f, 0.15f);
        return state;   // its speed and restarting are set by SyncTiming
    }

    // Every action state (named like a PlayerAction) plays at PlayerAnimator.ActionSpeedOf, and its Any State arrow
    // takes over in ActionBlendSeconds and may start it again (a new click). Returns how many parts it changed.
    static int SyncTiming(AnimatorStateMachine machine)
    {
        int changed = 0;
        foreach (ChildAnimatorState child in machine.states)
        {
            if (!IsAction(child.state, out PlayerAction action)) continue;
            float speed = PlayerAnimator.ActionSpeedOf(action);
            if (Mathf.Approximately(child.state.speed, speed)) continue;
            child.state.speed = speed;
            EditorUtility.SetDirty(child.state);
            changed++;
        }
        foreach (AnimatorStateTransition arrow in machine.anyStateTransitions)
        {
            if (!IsAction(arrow.destinationState, out _)) continue;
            if (arrow.canTransitionToSelf && !arrow.hasExitTime && arrow.hasFixedDuration &&
                Mathf.Approximately(arrow.duration, PlayerAnimator.ActionBlendSeconds)) continue;
            arrow.hasExitTime = false;
            arrow.hasFixedDuration = true;   // the blend time in seconds, not a share of the animation
            arrow.duration = PlayerAnimator.ActionBlendSeconds;
            arrow.canTransitionToSelf = true;
            EditorUtility.SetDirty(arrow);
            changed++;
        }
        foreach (ChildAnimatorStateMachine inner in machine.stateMachines) changed += SyncTiming(inner.stateMachine);   // state groups
        return changed;
    }

    // Takes dropped actions' states (with the arrows to them) and triggers out. Returns how many states went.
    static int RemoveRetired(AnimatorController controller, AnimatorStateMachine machine)
    {
        int removed = 0;
        foreach (string name in Retired)
        {
            AnimatorState state = FindState(machine, name);
            if (state != null)
            {
                machine.RemoveState(state);
                removed++;
            }
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name != name) continue;
                controller.RemoveParameter(p);
                break;
            }
        }
        return removed;
    }

    static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == name) return child.state;
        }
        return null;
    }

    // True if the state is named exactly like one of the PlayerActions (e.g. "Punch").
    static bool IsAction(AnimatorState state, out PlayerAction action)
    {
        action = PlayerAction.Hit;
        if (state == null) return false;
        foreach (PlayerAction a in System.Enum.GetValues(typeof(PlayerAction)))
        {
            if (state.name != a.ToString()) continue;
            action = a;
            return true;
        }
        return false;
    }

    // ----- 3. The first-person arms -----

    static string FixArms()
    {
        string path = HandsPrefabPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "first-person arms: no hands yet (Tools > Player > Add First Person Hands)";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            string result = FirstPersonArmsHelper.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return "first-person arms: " + result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ----- 4. The Player -----

    // PunchCombo on the Player, and PunchEffect's delays measured from the clips.
    static string SetUpPlayer(Dictionary<PlayerAction, AnimationClip> clips)
    {
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return "no Player in the open scene: open GameScene and run this again for PunchCombo and the punch timing";

        AddToPlayer<PunchCombo>();
        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(player.gameObject);
        if (missing > 0)
        {
            Undo.RegisterCompleteObjectUndo(player.gameObject, ToolName);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player.gameObject);   // e.g. the old ToolEquip
        }
        string timing = "";
        PunchEffect effect = player.GetComponent<PunchEffect>();
        if (effect != null)
        {
            SerializedObject so = new SerializedObject(effect);
            List<string> measured = new List<string>();
            foreach ((PlayerAction action, string field) in Delays)
            {
                if (!clips.ContainsKey(action)) continue;
                float? delay = PunchEffectBuilder.MeasureAction(action, out string how);
                SerializedProperty property = so.FindProperty(field);
                if (delay.HasValue && property != null)
                {
                    property.floatValue = delay.Value;
                    measured.Add(action + " " + delay.Value.ToString("0.00") + " s");
                }
                else
                {
                    measured.Add(action + ": " + how);
                }
            }
            so.ApplyModifiedProperties();   // recorded for Undo
            if (measured.Count > 0) timing = "; punches land after: " + string.Join(", ", measured);
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        return "the Player has PunchCombo" + timing + (missing > 0 ? "; " + missing + " component(s) with a deleted script taken off it" : "");
    }
}
