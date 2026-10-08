/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component). It is the "Icon" section of an Item Data and a Building Data.
Unity Editor: Choose ONE in the Icon dropdown; only that one's field shows (IconSourceDrawer):
                Capture  = a photo of the prefab, taken by itself (nothing to fill in)
                Static   = a still picture: drag a PNG (imported as Sprite) into Image
                Animated = drag an .aseprite file into Aseprite (frames and timing are read by themselves).
                           Several animations (tags) in one file? Type the tag's name in Aseprite Tag.
Purpose: Where an item's or building's picture comes from, as one choice instead of several tick boxes.
         Animated icons really move in the inventory, on the cursor and in the build menu (UIAnimatedImage).
Requirement: IconSource.cs x-> AsepriteFrames.cs
             IconSource.cs <-x BuildingData.cs, IconSourceDrawer.cs, ItemData.cs, SlotUI.cs, UIAnimatedImage.cs
*/

using UnityEngine;

// Where a picture comes from. Only one at a time.
public enum IconMode
{
    Capture,    // a photo of the prefab (IconCapture)
    Static,     // a still picture (PNG imported as Sprite)
    Animated,   // an .aseprite animation
}

[System.Serializable]
public class IconSource
{
    // ===== Fields =====
    [Tooltip("Where the picture comes from: a photo of the prefab, a still picture, or an Aseprite animation.")]
    public IconMode mode = IconMode.Capture;

    [Tooltip("The still picture (a PNG imported as Sprite).")]
    public Sprite image;

    [Tooltip("The .aseprite file.")]
    public Object aseprite;

    [Tooltip("Optional: the Aseprite tag (animation name) to play when the file has several. Empty = the first one.")]
    public string asepriteTag = "";

    // Read from the .aseprite file in the editor and saved with the asset, so they also work in a built game.
    [HideInInspector] public Sprite[] frames = new Sprite[0];
    [HideInInspector] public float[] durations = new float[0];   // seconds each frame stays (0 = default speed)

    public bool IsCapture => mode == IconMode.Capture;
    public bool IsAnimated => mode == IconMode.Animated && frames != null && frames.Length > 0;

    // The picture to show when nothing moves: the still picture, or the animation's first frame.
    // null for Capture (the caller takes a photo instead).
    public Sprite Still
    {
        get
        {
            if (mode == IconMode.Static) return image;
            if (mode == IconMode.Animated && frames != null && frames.Length > 0) return frames[0];
            return null;
        }
    }

    // ===== My methods =====

#if UNITY_EDITOR
    // Re-reads the .aseprite frames (call it from the owner's OnValidate). owner = only for the warning message.
    public void EditorRefresh(Object owner)
    {
        if (mode != IconMode.Animated || aseprite == null)
        {
            frames = new Sprite[0];
            durations = new float[0];
            return;
        }

        if (!AsepriteFrames.IsAseprite(aseprite))
        {
            Debug.LogWarning((owner != null ? owner.name + ": " : "") + "Aseprite must be an .aseprite file.", owner);
            aseprite = null;
            return;
        }

        AsepriteFrames.Read(aseprite, asepriteTag, out frames, out durations);
    }
#endif
}
