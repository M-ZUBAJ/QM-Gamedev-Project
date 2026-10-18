/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
Unity Editor: Works in the Unity Editor only (it reads files). The frames it reads are saved into the scene / asset,
              so the game still plays them after it is built.
              The .aseprite file needs Unity's Aseprite Importer (installed) with "Animation Clips" ticked (the default).
Purpose: Reads the frames of an .aseprite file and how long each one shows, in ONE place,
         for everything that plays Aseprite animations (UIAnimatedImage, IconSource on Item / Building Data).
         - From the importer's animation clip (Aseprite's own timing), the tag asked for or the first one.
         - No clip: all its sprites in name order, with no timing (0 = the player's fallback speed).
Requirement: AsepriteFrames.cs <-x CoinCounter.cs, IconSource.cs, UIAnimatedImage.cs
*/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;   // AssetDatabase, AnimationUtility: read the .aseprite file (editor only)
#endif

public static class AsepriteFrames
{
#if UNITY_EDITOR
    // ===== My methods =====

    // True if this asset is an .aseprite (or .ase) file.
    public static bool IsAseprite(Object file)
    {
        if (file == null) return false;
        string path = AssetDatabase.GetAssetPath(file);
        return path.EndsWith(".aseprite") || path.EndsWith(".ase");
    }

    // Reads the frames and their durations (seconds). Returns false (and empty arrays) if there are none.
    public static bool Read(Object file, string tag, out Sprite[] frames, out float[] durations)
    {
        frames = new Sprite[0];
        durations = new float[0];
        if (!IsAseprite(file)) return false;

        Object[] parts = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(file));   // everything the importer made

        // 1. The animation clip: the tag asked for, or the first one.
        AnimationClip clip = null;
        foreach (Object o in parts)
        {
            if (!(o is AnimationClip c)) continue;
            if (clip == null) clip = c;
            if (!string.IsNullOrEmpty(tag) && c.name.EndsWith(tag)) { clip = c; break; }
        }
        if (clip != null && ReadClip(clip, out frames, out durations)) return true;

        // 2. No clip: every sprite, in name order (Frame_0, Frame_1, ...), no timing.
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object o in parts)
        {
            if (o is Sprite s) sprites.Add(s);
        }
        sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));   // "Frame_2" before "Frame_10"
        frames = sprites.ToArray();
        durations = new float[frames.Length];   // 0 = use the fallback speed
        return frames.Length > 0;
    }

    // The sprite keys of the clip: which sprite shows from which time. Returns false if it has none.
    static bool ReadClip(AnimationClip clip, out Sprite[] frames, out float[] durations)
    {
        frames = new Sprite[0];
        durations = new float[0];

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.propertyName != "m_Sprite") continue;

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys.Length == 0) continue;

            frames = new Sprite[keys.Length];
            durations = new float[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                frames[i] = keys[i].value as Sprite;
                float end = i + 1 < keys.Length ? keys[i + 1].time : clip.length;   // the next key, or the clip's end
                durations[i] = end - keys[i].time;
            }
            return true;
        }
        return false;
    }
#endif
}
