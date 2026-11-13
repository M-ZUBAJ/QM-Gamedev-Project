/*
First Date: 10/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Player > Add Character Model.
              Character: the one selected in the Project window (any Humanoid character prefab or model), or Blink's
              HumanMale_Character_FREE when nothing is selected.
              It makes Assets/Animation/Player/PlayerCharacter.controller (+ UpperBody.mask) and
              Assets/Prefabs/Player/PlayerCharacter.prefab, puts that on the Player as "Character" (1.8 m tall, feet
              on the ground) and hides the ball. It asks before replacing anything. Undo (Ctrl + Z) works in the scene.
              To change the height for every future build, edit Height in Fields below.
              Hand-made steps: Docs/Player Character Setup.md
Purpose: Gives the player a real body in one click. The ball keeps doing the moving and colliding; the character on
         top of it only shows the player, animated by PlayerAnimator. The controller it builds:
         - Base layer (whole body): Locomotion = a blend of Idle, Run forward / back / left / right / back-left /
           back-right and Sprint, picked by the speed. Falling (in the air for a moment), Jump (then Falling),
           Dead (from anywhere, back to Locomotion when alive again).
         - Actions layer (upper body only, UpperBody.mask): Empty (shows the legs' layer) and Hit, Punch, Attack,
           Mine, Gather, Cast, each played once by its trigger, at PlayerAnimator.ActionSpeed like the first-person
           hands; a new trigger starts it again at once.
         The prefab is a Prefab Variant of the character, so Blink's own files are never changed.
Requirement: PlayerCharacterBuilder.cs x-> UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, PlayerAnimator.cs, PlayerController.cs, ToolInHand.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AnimatorBuildHelper;   // EmptyController, AddState, Instant, FindHumanoidClips
using static UIBuildHelper;         // FindPrefabPath, FolderOf, MakeFolders

public static class PlayerCharacterBuilder
{
    // ===== Fields =====
    const string ToolName = "Add Character Model";
    const string DefaultCharacter = "HumanMale_Character_FREE";   // Blink's free human, used when nothing is selected
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/Player/PlayerCharacter.prefab");
    const string ControllerPath = "Assets/Animation/Player/PlayerCharacter.controller";
    const string MaskPath = "Assets/Animation/Player/UpperBody.mask";
    const string ChildName = "Character";
    const float Height = 1.8f;   // meters from the feet to the top of the head: a grown-up person (the ball is 1 m)

    // Which animation plays where: the clip names in Blink's Animations_Starter_Pack.
    // Running: x = to the right, y = forwards, in walking speeds (1 = walking). Sprint goes at the Player's Sprint
    // Multiplier (e.g. 2.11 = sprinting is 2.11 times walking), so full sprinting speed plays the whole Sprint clip.
    static readonly (string clip, Vector2 at)[] RunClips =
    {
        ("Idle", new Vector2(0f, 0f)),
        ("RunForward", new Vector2(0f, 1f)),
        ("RunBackward", new Vector2(0f, -1f)),
        ("RunLeft", new Vector2(-1f, 0f)),
        ("RunRight", new Vector2(1f, 0f)),
        ("RunBackwardLeft", new Vector2(-0.7f, -0.7f)),
        ("RunBackwardRight", new Vector2(0.7f, -0.7f)),
    };
    const string SprintClip = "Sprint";
    const string JumpClip = "Jump_Up";        // the take-off part of Blink's jump
    const string FallClip = "FallingLoop";
    const string DeathClip = "Death";

    // The animation for each PlayerAction (upper body).
    static readonly Dictionary<PlayerAction, string> ActionClips = new Dictionary<PlayerAction, string>
    {
        { PlayerAction.Hit, "BotGetHitBody|GetHit" },
        { PlayerAction.Punch, "BotPunchingRight|PunchRight" },
        { PlayerAction.Attack, "BotAxeAttackDownward|MeleeAttack_OneHanded" },   // sword, axe
        { PlayerAction.Mine, "BotAxeAttackDownward|MiningLoop" },                 // pickaxe
        { PlayerAction.Gather, "Gathering" },
        { PlayerAction.Cast, "SpellCast" },
        { PlayerAction.PunchLeft, "BotPunchingLeft|PunchLeft" },
        { PlayerAction.HeavyHook, "BotHeavyLeftHook|PunchLeft" },
        { PlayerAction.HeavyPunch, "BotHeavyPunchRight|PunchRight" },
    };

    // Blink's human only wears underwear: these clothes are switched on in our prefab (other characters: ignored).
    static readonly string[] Clothes = { "Starter_Boots", "Starter_Chest", "Starter_Pants" };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Player/Add Character Model", false, MenuOrder.AddCharacterModel)]
    static void Build()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no Player (an object with PlayerController) in the open scene.\n\n" +
                                        "Open GameScene and run this again.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        // Made before: build them again, or keep them (e.g. the clothes were changed in the prefab).
        bool rebuild = true;
        if (prefab != null || controller != null)
        {
            int choice = EditorUtility.DisplayDialogComplex(ToolName,
                "The PlayerCharacter prefab and its animation controller already exist.\n\n" +
                "Replace: build both again (changes made to them, like the clothes, are lost).\n" +
                "Keep: use them as they are and only put the character on the Player.",
                "Replace", "Cancel", "Keep");
            if (choice == 1) return;   // 0 = Replace, 1 = Cancel, 2 = Keep
            rebuild = choice == 0;
        }

        // The Player already has a character: swap it for the new one.
        Transform old = OldCharacter(player.transform);
        if (old != null && !EditorUtility.DisplayDialog(ToolName,
                "The Player already has a character (" + old.name + "). Replace it?\n(Ctrl + Z brings the old one back.)",
                "Replace", "Cancel"))
        {
            return;
        }

        if (rebuild || controller == null || prefab == null)
        {
            GameObject source = ChosenCharacter();
            string problem = Problem(source);
            if (problem != null)
            {
                EditorUtility.DisplayDialog(ToolName, problem, "OK");
                return;
            }

            List<string> missing = new List<string>();
            if (rebuild || controller == null) controller = BuildController(FindHumanoidClips(source), SprintAt(player), missing);
            if (rebuild || prefab == null) prefab = BuildPrefab(source, controller);
            if (missing.Count > 0)
            {
                Debug.LogWarning(ToolName + ": these animations weren't found, so their states play nothing: " +
                                 string.Join(", ", missing) + ". Drag a clip into each state's Motion in the Animator window.");
            }
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(ToolName, "Couldn't save " + PrefabPath + ". See the Console for why.", "OK");
                return;
            }
        }
        FillMissingController(prefab, controller);

        GameObject character = PutOnPlayer(player.transform, prefab, old, controller);
        Selection.activeGameObject = character;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log("Character added: " + player.name + " > " + ChildName + " (" + PrefabPath + "). The ball is hidden but " +
                  "still does the moving. Save the scene (Ctrl + S) and press Play.");
    }

    // ===== My methods =====

    // The character to use: a Humanoid prefab or model selected in the Project window, else Blink's free human.
    static GameObject ChosenCharacter()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected != null && EditorUtility.IsPersistent(selected)                 // a file, not a scene object
            && selected.GetComponentInChildren<Animator>(true) != null
            && selected.GetComponentInChildren<PlayerAnimator>(true) == null)        // not our own PlayerCharacter
        {
            return selected;
        }

        foreach (string guid in AssetDatabase.FindAssets(DefaultCharacter + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == DefaultCharacter)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // Why this character can't be used (null = it can).
    static string Problem(GameObject source)
    {
        if (source == null)
        {
            return "No character found.\n\nImport Blink's \"FREE Low Poly Human - RPG Character\" (Window > Package Manager > " +
                   "My Assets), or select a Humanoid character prefab in the Project window, then run this again.";
        }

        Animator animator = source.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
        {
            return source.name + " isn't a Humanoid character, so the animations can't play on it.\n\n" +
                   "Click its model file (.fbx), then in the Inspector: Rig > Animation Type = Humanoid > Apply. " +
                   "Run this again afterwards.";
        }
        return null;
    }

    // Where Sprint goes in the running blend: the Player's Sprint Multiplier (sprinting speed / walking speed).
    static float SprintAt(PlayerController player)
    {
        // The name in quotes must match the field name in PlayerController.
        SerializedProperty multiplier = new SerializedObject(player).FindProperty("sprintMultiplier");
        return multiplier != null && multiplier.floatValue > 1.05f ? multiplier.floatValue : 2f;
    }

    // Builds the Animator Controller (and the upper body mask) from scratch, inside the same files.
    static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, float sprintAt, List<string> missing)
    {
        MakeFolders(FolderOf(ControllerPath));
        AvatarMask upperBody = UpperBodyMask();
        AnimatorController c = EmptyController(ControllerPath);   // emptied in place, never deleted (see AnimatorBuildHelper)

        c.AddParameter(PlayerAnimator.MoveXName, AnimatorControllerParameterType.Float);
        c.AddParameter(PlayerAnimator.MoveZName, AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter
        {
            name = PlayerAnimator.RunSpeedName, type = AnimatorControllerParameterType.Float, defaultFloat = 1f
        });
        c.AddParameter(PlayerAnimator.FallingName, AnimatorControllerParameterType.Bool);
        c.AddParameter(PlayerAnimator.JumpName, AnimatorControllerParameterType.Trigger);
        c.AddParameter(PlayerAnimator.DeadName, AnimatorControllerParameterType.Bool);
        foreach (PlayerAction action in ActionClips.Keys) c.AddParameter(action.ToString(), AnimatorControllerParameterType.Trigger);

        // A clip by name (null + noted as missing if it isn't in the project).
        // "A|B" = clip A if the project has it (a Mixamo clip, Tools > Player > Set Up Player Animations), else clip B.
        AnimationClip Clip(string name)
        {
            foreach (string choice in name.Split('|'))
            {
                if (clips.TryGetValue(choice, out AnimationClip clip)) return clip;
            }
            missing.Add(name);
            return null;
        }

        // ----- Base layer: the whole body -----
        AnimatorStateMachine body = c.layers[0].stateMachine;
        AnimatorState locomotion = c.CreateBlendTreeInController("Locomotion", out BlendTree run, 0);
        run.blendType = BlendTreeType.FreeformDirectional2D;   // picks the clips around the direction and speed
        run.blendParameter = PlayerAnimator.MoveXName;
        run.blendParameterY = PlayerAnimator.MoveZName;
        foreach (var (clipName, at) in RunClips)
        {
            AnimationClip clip = Clip(clipName);
            if (clip != null) run.AddChild(clip, at);
        }
        AnimationClip sprint = Clip(SprintClip);
        if (sprint != null) run.AddChild(sprint, new Vector2(0f, sprintAt));
        locomotion.speedParameter = PlayerAnimator.RunSpeedName;   // PlayerAnimator's Run Animation Speed
        locomotion.speedParameterActive = true;
        body.defaultState = locomotion;

        AnimatorState jump = AddState(body, "Jump", Clip(JumpClip), new Vector3(560f, 230f));
        AnimatorState falling = AddState(body, "Falling", Clip(FallClip), new Vector3(560f, 110f));
        AnimatorState dead = AddState(body, "Dead", Clip(DeathClip), new Vector3(300f, 300f));

        // Jump: from anywhere (not while dead) when PlayerJump jumps, then Falling once the take-off has played.
        AnimatorStateTransition t = body.AddAnyStateTransition(jump);
        t.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.JumpName);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
        Instant(t, 0.05f);
        t = jump.AddTransition(falling);
        t.hasExitTime = true;
        t.exitTime = 0.9f;   // at 90 % of the take-off animation
        t.duration = 0.1f;

        // Walked off an edge, and landed again.
        t = locomotion.AddTransition(falling);
        t.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.FallingName);
        Instant(t, 0.2f);
        t = falling.AddTransition(locomotion);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.FallingName);
        Instant(t, 0.15f);

        // Died: from anywhere. Alive again (respawned): stand back up.
        t = body.AddAnyStateTransition(dead);
        t.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.DeadName);
        Instant(t, 0.1f);
        t = dead.AddTransition(locomotion);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
        Instant(t, 0.2f);

        // ----- Actions layer: only the upper body, on top of the base layer -----
        c.AddLayer(PlayerAnimator.ActionLayerName);
        AnimatorControllerLayer[] layers = c.layers;   // a copy: change it, then give it back
        layers[1].defaultWeight = 1f;
        layers[1].avatarMask = upperBody;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        c.layers = layers;

        AnimatorStateMachine actions = c.layers[1].stateMachine;
        AnimatorState empty = AddState(actions, PlayerAnimator.EmptyStateName, null, new Vector3(300f, 110f));
        actions.defaultState = empty;   // no animation here = the base layer shows through
        float y = 0f;
        foreach (KeyValuePair<PlayerAction, string> pair in ActionClips)
        {
            string trigger = pair.Key.ToString();
            AnimatorState state = AddState(actions, trigger, Clip(pair.Value), new Vector3(600f, y));
            state.speed = PlayerAnimator.ActionSpeedOf(pair.Key);   // the same as the first-person hands, so they move together
            y += 70f;

            t = actions.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
            Instant(t, PlayerAnimator.ActionBlendSeconds);
            t.canTransitionToSelf = true;   // a new click starts it again at once (else the click waits, and it plays late)
            t = state.AddTransition(empty);   // played once, then back to doing nothing
            t.hasExitTime = true;
            t.exitTime = 0.85f;
            t.duration = 0.15f;
        }

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    // Upper body = everything but the legs, the feet and the root (the root is where the body stands).
    // Like the controller, an existing mask is changed in place instead of being deleted and made again.
    static AvatarMask UpperBodyMask()
    {
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        bool isNew = mask == null;
        if (isNew) mask = new AvatarMask();

        for (AvatarMaskBodyPart part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
        {
            bool lower = part == AvatarMaskBodyPart.Root || part == AvatarMaskBodyPart.LeftLeg ||
                         part == AvatarMaskBodyPart.RightLeg || part == AvatarMaskBodyPart.LeftFootIK ||
                         part == AvatarMaskBodyPart.RightFootIK;
            mask.SetHumanoidBodyPartActive(part, !lower);
        }

        if (isNew) AssetDatabase.CreateAsset(mask, MaskPath);
        else EditorUtility.SetDirty(mask);
        return mask;
    }

    // The prefab's Animator must have the controller. An older version of this tool could leave it empty
    // (see EmptyController); this fills it in. A different controller put there by hand is kept.
    static void FillMissingController(GameObject prefab, AnimatorController controller)
    {
        Animator animator = prefab.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController != null) return;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);   // the prefab opened for changing, like Prefab Mode
        try
        {
            contents.GetComponentInChildren<Animator>(true).runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // A Prefab Variant of the character with our controller and PlayerAnimator on it.
    static GameObject BuildPrefab(GameObject source, AnimatorController controller)
    {
        MakeFolders(FolderOf(PrefabPath));
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            Animator animator = temp.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;                          // the ball moves the player
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;  // keeps moving in first person, where only its shadow shows
            if (animator.GetComponent<PlayerAnimator>() == null) animator.gameObject.AddComponent<PlayerAnimator>();
            if (animator.GetComponent<ToolInHand>() == null) animator.gameObject.AddComponent<ToolInHand>();   // holds the selected tool (the first-person hands copy gets it too)

            // A collider on the character would join the ball's Rigidbody and change how the player bumps into things.
            foreach (Collider part in temp.GetComponentsInChildren<Collider>(true)) part.enabled = false;

            foreach (Transform part in temp.GetComponentsInChildren<Transform>(true))
            {
                if (System.Array.IndexOf(Clothes, part.name) >= 0) part.gameObject.SetActive(true);
            }

            return PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);   // saved from a prefab copy = a Prefab Variant
        }
        finally
        {
            Object.DestroyImmediate(temp);   // only the saved prefab is kept
        }
    }

    // The child of the Player that holds a PlayerAnimator (a character put there before), or null.
    static Transform OldCharacter(Transform player)
    {
        foreach (Transform child in player)
        {
            if (child.GetComponentInChildren<PlayerAnimator>(true) != null) return child;
        }
        return null;
    }

    // Puts the character on the Player (replacing the old one), sized and standing on the ground, and hides the ball.
    static GameObject PutOnPlayer(Transform player, GameObject prefab, Transform old, AnimatorController controller)
    {
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(character, ToolName);
        character.name = ChildName;
        character.transform.SetParent(player, false);
        Fit(character.transform, player);

        // Last check: an Animator without a controller plays nothing and the character stands in a T-pose.
        Animator animator = character.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController == null) animator.runtimeAnimatorController = controller;

        // The character shows the player now. The ball's collider and scripts keep working; only its look is off.
        MeshRenderer ballLook = player.GetComponent<MeshRenderer>();
        if (ballLook != null && ballLook.enabled)
        {
            Undo.RecordObject(ballLook, ToolName);
            ballLook.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        return character;
    }

    // Scales the character to Height and stands its feet on the bottom of the ball.
    static void Fit(Transform character, Transform player)
    {
        character.localPosition = Vector3.zero;
        character.localRotation = Quaternion.identity;
        character.localScale = Vector3.one;

        Bounds look = MeshBounds.Of(character.gameObject);    // the size you see, as it comes
        if (look.size.y < 0.01f) return;                      // nothing to measure: leave it as it is

        float scale = Height / look.size.y;
        float feetBelow = look.min.y - character.position.y;  // how far the feet are below its own middle point
        character.localScale = Vector3.one * scale;

        Vector3 position = character.position;
        position.y = BottomOf(player) - feetBelow * scale;
        character.position = position;
    }

    // The lowest point of the Player's shape (the bottom of the ball), where the feet go.
    static float BottomOf(Transform player)
    {
        Collider shape = player.GetComponent<Collider>();
        if (shape != null && shape.enabled && shape.bounds.size.y > 0f) return shape.bounds.min.y;

        float scale = Mathf.Abs(player.lossyScale.y);   // the collider's box can be empty before physics runs: work it out
        if (shape is SphereCollider sphere) return player.TransformPoint(sphere.center).y - sphere.radius * scale;
        if (shape is CapsuleCollider capsule) return player.TransformPoint(capsule.center).y - capsule.height * 0.5f * scale;
        return player.position.y;
    }

}
