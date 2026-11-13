/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Player > Add First Person Hands.
              It uses the player's look: Assets/Prefabs/Player/PlayerCharacter.prefab (made by Tools > Player > Add
              Character Model), or Blink's HumanMale_Character_FREE if that doesn't exist yet.
              It makes Assets/Animation/Player/FirstPersonHands.controller and Assets/Prefabs/Player/FirstPersonHands.prefab,
              and puts that on the Main Camera as "FirstPersonHands", the same size as the player's character.
              It asks before replacing anything. Undo (Ctrl + Z) works in the scene.
              Hand-made steps: Docs/Player Character Setup.md (First-person hands).
Purpose: Gives the first-person view Minecraft-style hands in one click:
         - a Prefab Variant of the player's look, with only its arms and hands drawn and no shadows
           (PlayerCharacter's own PlayerAnimator is switched off: these hands must not act as the player's body);
         - its own controller: Idle = IdleCombat (a fighting guard, fists up, so they're in view), and Punch, Attack,
           Mine, Gather, Cast at PlayerAnimator.ActionSpeed (the body's speed too, so the shadow punches with the hands),
           each click starting them again;
         - FirstPersonHands, which puts its head on the camera and plays what PlayerAnimator.Play plays;
         - FirstPersonArmsHelper's arm meshes: cut at the elbow and drawn from both sides.
Requirement: FirstPersonHandsBuilder.cs x-> UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, FirstPersonArmsHelper.cs, FirstPersonHands.cs, PlayerAnimator.cs, CameraController.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using static AnimatorBuildHelper;   // EmptyController, AddState, Instant, AfterPlaying, FindHumanoidClips
using static UIBuildHelper;         // FindPrefabPath, FolderOf, MakeFolders

public static class FirstPersonHandsBuilder
{
    // ===== Fields =====
    const string ToolName = "Add First Person Hands";
    const string CharacterPrefab = "Assets/Prefabs/Player/PlayerCharacter.prefab";   // the player's look (found by name if moved)
    const string FallbackCharacter = "HumanMale_Character_FREE";                      // Blink's human, if there's no PlayerCharacter yet
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/Player/FirstPersonHands.prefab");
    const string ControllerPath = "Assets/Animation/Player/FirstPersonHands.controller";
    const string ChildName = "FirstPersonHands";
    const float Height = 1.8f;      // only used when the scene has no player character to copy the size from

    const string IdleClip = "IdleCombat";   // Blink's fighting guard: the fists are up in front of the face, so they're in view
    static readonly Dictionary<PlayerAction, string> ActionClips = new Dictionary<PlayerAction, string>
    {
        { PlayerAction.Punch, "BotPunchingRight|PunchRight" },
        { PlayerAction.Attack, "BotAxeAttackDownward|MeleeAttack_OneHanded" },   // sword, axe
        { PlayerAction.Mine, "BotAxeAttackDownward|MiningLoop" },                 // pickaxe
        { PlayerAction.Gather, "Gathering" },
        { PlayerAction.Cast, "SpellCast" },
        { PlayerAction.PunchLeft, "BotPunchingLeft|PunchLeft" },
        { PlayerAction.HeavyHook, "BotHeavyLeftHook|PunchLeft" },
        { PlayerAction.HeavyPunch, "BotHeavyPunchRight|PunchRight" },
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Player/Add First Person Hands", false, MenuOrder.AddFirstPersonHands)]
    static void Build()
    {
        CameraController cam = Object.FindFirstObjectByType<CameraController>();
        if (cam == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no camera with CameraController (the Main Camera) in the open scene.\n\n" +
                                        "Open GameScene and run this again.", "OK");
            return;
        }

        GameObject source = SourceCharacter();
        string problem = Problem(source);
        if (problem != null)
        {
            EditorUtility.DisplayDialog(ToolName, problem, "OK");
            return;
        }

        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null ||
                      AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null;
        if (exists && !EditorUtility.DisplayDialog(ToolName,
                "FirstPersonHands already exists. Build it again and replace it?\n(Changes made to its prefab or controller are lost.)",
                "Replace", "Cancel"))
        {
            return;
        }

        Transform old = OldHands(cam.transform);
        if (old != null && !EditorUtility.DisplayDialog(ToolName,
                "The camera already has first-person hands (" + old.name + "). Replace them?\n(Ctrl + Z brings the old ones back.)",
                "Replace", "Cancel"))
        {
            return;
        }

        List<string> missing = new List<string>();
        AnimatorController controller = BuildController(FindHumanoidClips(source), missing);
        GameObject prefab = BuildPrefab(source, controller);
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

        GameObject hands = PutOnCamera(cam.transform, prefab, old);
        Selection.activeGameObject = hands;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log("First-person hands added: " + cam.name + " > " + ChildName + ". Save the scene (Ctrl + S), press Play, " +
                  "F5 for first person, pick an empty hotbar slot and left click to punch. Frame them with FirstPersonHands > Offset.");
    }

    // ===== My methods =====

    // The player's look: the PlayerCharacter prefab, else Blink's free human.
    static GameObject SourceCharacter()
    {
        GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(CharacterPrefab));
        if (character != null) return character;

        foreach (string guid in AssetDatabase.FindAssets(FallbackCharacter + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == FallbackCharacter)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // Why this character can't be used (null = it can).
    static string Problem(GameObject source)
    {
        if (source == null)
        {
            return "No character found.\n\nRun Tools > Player > Add Character Model first (or import Blink's \"FREE Low Poly " +
                   "Human - RPG Character\"), then run this again.";
        }
        Animator animator = source.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
        {
            return source.name + " isn't a Humanoid character, so the hand animations can't play on it.\n\n" +
                   "Click its model file (.fbx), then in the Inspector: Rig > Animation Type = Humanoid > Apply.";
        }
        return null;
    }

    // The hands' controller, built from scratch inside the same file if it already exists.
    static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, List<string> missing)
    {
        MakeFolders(FolderOf(ControllerPath));
        AnimatorController c = EmptyController(ControllerPath);
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

        AnimatorStateMachine sm = c.layers[0].stateMachine;
        AnimatorState idle = AddState(sm, "Idle", Clip(IdleClip), new Vector3(300f, 120f));
        sm.defaultState = idle;

        float y = 0f;
        foreach (KeyValuePair<PlayerAction, string> pair in ActionClips)
        {
            string trigger = pair.Key.ToString();   // the same names as PlayerAction, so FirstPersonHands can pass them on
            AnimatorState state = AddState(sm, trigger, Clip(pair.Value), new Vector3(560f, y));
            state.speed = PlayerAnimator.ActionSpeedOf(pair.Key);   // the same as the body, so hands and shadow move together
            y += 70f;

            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            Instant(t, PlayerAnimator.ActionBlendSeconds);
            t.canTransitionToSelf = true;   // a new click starts the punch again, even if the last one hasn't finished
            AfterPlaying(state, idle, 0.85f, 0.15f);
        }

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    // A Prefab Variant of the player's look with the hands' controller and FirstPersonHands on it.
    static GameObject BuildPrefab(GameObject source, AnimatorController controller)
    {
        MakeFolders(FolderOf(PrefabPath));
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            Animator animator = temp.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;                          // the camera holds the hands in place
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // PlayerCharacter's PlayerAnimator: switched off, or these hands would act as the player's body.
            foreach (PlayerAnimator body in temp.GetComponentsInChildren<PlayerAnimator>(true)) body.enabled = false;
            foreach (Collider part in temp.GetComponentsInChildren<Collider>(true)) part.enabled = false;

            // Only the arms and hands are drawn (in the editor too), and nothing casts a shadow.
            foreach (Renderer part in temp.GetComponentsInChildren<Renderer>(true))
            {
                part.shadowCastingMode = ShadowCastingMode.Off;
                if (!FirstPersonHands.IsHandPart(part.name, FirstPersonHands.DefaultParts)) part.enabled = false;
            }

            if (animator.GetComponent<FirstPersonHands>() == null) animator.gameObject.AddComponent<FirstPersonHands>();
            FirstPersonArmsHelper.Apply(temp);   // arms cut at the elbow and drawn from both sides (no open shoulder, no holes)
            return PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);   // saved from a prefab copy = a Prefab Variant
        }
        finally
        {
            Object.DestroyImmediate(temp);   // only the saved prefab is kept
        }
    }

    // The camera's child that holds FirstPersonHands (hands put there before), or null.
    static Transform OldHands(Transform cam)
    {
        foreach (Transform child in cam)
        {
            if (child.GetComponentInChildren<FirstPersonHands>(true) != null) return child;
        }
        return null;
    }

    // Puts the hands on the camera (replacing the old ones), the player's size, with their head on the camera.
    static GameObject PutOnCamera(Transform cam, GameObject prefab, Transform old)
    {
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject hands = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cam.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(hands, ToolName);
        hands.name = ChildName;
        hands.transform.SetParent(cam, false);
        hands.transform.localRotation = Quaternion.identity;   // looks the way the camera looks
        hands.transform.localScale = Vector3.one * Scale(hands.transform, cam);
        PutHeadOnCamera(hands.transform);   // in the editor too; FirstPersonHands does it again when the game starts

        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        return hands;
    }

    // The same size as the player's character in the scene (a PlayerAnimator that's switched on), else Height tall.
    static float Scale(Transform hands, Transform cam)
    {
        float camScale = Mathf.Abs(cam.lossyScale.y) > 0.0001f ? cam.lossyScale.y : 1f;
        foreach (PlayerAnimator body in Object.FindObjectsByType<PlayerAnimator>(FindObjectsSortMode.None))
        {
            if (body.enabled && body.GetComponentInParent<FirstPersonHands>() == null) return body.transform.lossyScale.y / camScale;
        }

        hands.localScale = Vector3.one;
        Bounds look = MeshBounds.Of(hands.gameObject);   // the whole body (hidden parts count: only switched-off objects don't)
        return look.size.y > 0.01f ? Height / look.size.y : 1f;
    }

    // Moves the copy so its head bone is where the camera is: its eyes see what the camera sees.
    static void PutHeadOnCamera(Transform hands)
    {
        hands.localPosition = Vector3.zero;
        Animator animator = hands.GetComponentInChildren<Animator>();
        Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (head != null) hands.localPosition = -hands.parent.InverseTransformPoint(head.position);
    }
}
