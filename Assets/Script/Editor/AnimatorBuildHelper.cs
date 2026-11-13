/*
First Date: 10/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. The tools that build Animator Controllers use it (Tools > Player > Add Character
              Model, Tools > Player > Add First Person Hands, Tools > Enemies > Build Monster Enemy).
Purpose: The small building blocks for making an Animator Controller in code, kept in one place:
         - empty an existing controller IN PLACE. Never delete it and make a new one: everything linked to the
           deleted one (prefabs, scenes) loses its animations and the character stands in a T-pose.
         - add a state, a transition that happens at once, and one that waits for the animation to finish.
         - find the project's Humanoid animation clips by name.
         A tool writes "using static AnimatorBuildHelper;" and can then call e.g. EmptyController(path) directly.
Requirement: AnimatorBuildHelper.cs <-x EnemyBuilder.cs, FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, WildAnimalBuilder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorBuildHelper
{
    // ===== My methods =====

    // The controller at this path, emptied (or made the first time, with a Base Layer).
    // It keeps its file and object, so prefabs and scenes stay linked to it.
    public static AnimatorController EmptyController(string path)
    {
        AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (c == null) return AnimatorController.CreateAnimatorControllerAtPath(path);

        c.layers = new AnimatorControllerLayer[0];
        c.parameters = new AnimatorControllerParameter[0];
        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (part != null && part != c) Object.DestroyImmediate(part, true);   // old states, transitions, blend trees
        }
        c.AddLayer("Base Layer");
        return c;
    }

    // A state that plays one animation (null = plays nothing, e.g. an empty state or a missing clip).
    public static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);   // position = where it shows in the Animator window
        state.motion = motion;
        return state;
    }

    // Makes a transition happen as soon as its conditions are true, without waiting for the animation to end.
    public static void Instant(AnimatorStateTransition t, float blendSeconds)
    {
        t.hasExitTime = false;
        t.duration = blendSeconds;
        t.canTransitionToSelf = false;   // for Any State ones: don't restart the same state every frame
    }

    // A transition from one state to another once the animation has played to exitTime (0.9 = 90 % of it).
    public static AnimatorStateTransition AfterPlaying(AnimatorState from, AnimatorState to, float exitTime, float blendSeconds)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.duration = blendSeconds;
        return t;
    }

    // Every Humanoid animation clip in the project, by name. When two share a name, the one in the character's own
    // pack folder wins (e.g. Assets/Imported3D/Blink), so another pack's "Idle" can't sneak in. For a Prefab Variant (like
    // PlayerCharacter) the original character's folder counts.
    public static Dictionary<string, AnimationClip> FindHumanoidClips(GameObject character)
    {
        GameObject original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(character);
        string home = TopFolder(AssetDatabase.GetAssetPath(original != null ? original : character));
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        HashSet<string> fromHome = new HashSet<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool atHome = TopFolder(path) == home;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is AnimationClip clip) || !clip.humanMotion || clip.name.StartsWith("__preview__")) continue;
                if (clips.ContainsKey(clip.name) && (fromHome.Contains(clip.name) || !atHome)) continue;   // keep the first one
                clips[clip.name] = clip;
                if (atHome) fromHome.Add(clip.name);
            }
        }
        return clips;
    }

    // The pack a file belongs to: "Assets/Blink/Art/X.fbx" -> "Assets/Blink". Imported packs live one folder deeper
    // ("Assets/Imported3D/Blink/Art/X.fbx" -> "Assets/Imported3D/Blink"), so each pack still counts as its own home.
    static string TopFolder(string path)
    {
        string[] parts = path.Split('/');
        if (parts.Length > 3 && parts[1] == ImportedFolderName) return parts[0] + "/" + parts[1] + "/" + parts[2];
        return parts.Length > 1 ? parts[0] + "/" + parts[1] : path;
    }

    // The folder for imported 3D packs (Asset Store packs, Mixamo clips): Assets/Imported3D.
    public const string ImportedFolderName = "Imported3D";
}
