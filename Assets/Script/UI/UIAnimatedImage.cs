/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Drag this script onto any UI Image (e.g. LeftClickImage). Tools > UI > Add Build Mode Scripts adds it for you.
Unity Editor: Animated off: drag a picture (PNG, imported as Sprite) into Static Image.
              Animated on:  drag an .aseprite file into Aseprite. Its frames and their timing (from Aseprite) are read
                            by themselves. Several animations (tags) in one file? Type the tag's name in Aseprite Tag.
              The .aseprite file needs Unity's Aseprite Importer (installed) with "Animation Clips" ticked in its
              import settings (the default).
              The picture shows in the editor right away, and the animation plays while playing.
Purpose: A UI picture that is either a still image or an Aseprite animation, chosen with one tick.
         Animations keep playing when the game is paused (they use real time).
         Code can also show an item's / building's icon on any Image with UIAnimatedImage.ShowIcon(...):
         it animates Aseprite icons (adding this script by itself) and shows still ones as they are.
Requirement: UIAnimatedImage.cs x-> AsepriteFrames.cs, IconSource.cs, Image (Unity)
             UIAnimatedImage.cs <-x BuildingButton.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, SlotUI.cs
*/

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;   // EditorUtility.SetDirty (editor only)
#endif

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class UIAnimatedImage : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Off: show Static Image. On: play the Aseprite animation.")]
    [SerializeField] bool animated = true;

    [Header("Static")]
    [Tooltip("The still picture (a PNG imported as Sprite). Used when Animated is off.")]
    [SerializeField] Sprite staticImage;

    [Header("Animation")]
    [Tooltip("The .aseprite file. Used when Animated is on.")]
    [SerializeField] Object aseprite;

    [Tooltip("Optional: the Aseprite tag (animation name) to play when the file has several. Empty = the first one.")]
    [SerializeField] string asepriteTag = "";

    [Range(1f, 60f)]
    [Tooltip("Frames per second, only used if the file has no timing (no animation clip). Aseprite timing wins.")]
    [SerializeField] float fallbackFps = 10f;

    [Tooltip("Ticked: start again after the last frame. Unticked: stop on the last frame.")]
    [SerializeField] bool loop = true;

    // Read from the .aseprite file in the editor and saved with the scene, so they also work in a built game.
    [SerializeField, HideInInspector] Sprite[] frames = new Sprite[0];
    [SerializeField, HideInInspector] float[] durations = new float[0];   // seconds each frame stays

    Image image;
    int frame;
    float timer;   // seconds spent on the current frame

    public bool Animated => animated;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void OnEnable()
    {
        frame = 0;
        timer = 0f;
        ShowCurrent();
    }

    void Update()
    {
        if (!animated || frames.Length < 2) return;   // a still image, or a one-frame "animation"
        if (image.canvas != null && !image.canvas.enabled) return;   // hidden (e.g. not in build mode): no work

        // unscaledDeltaTime: real seconds, so UI keeps moving when the game is paused (Time.timeScale = 0).
        timer += Time.unscaledDeltaTime;
        while (timer >= Duration(frame))
        {
            timer -= Duration(frame);
            if (frame + 1 < frames.Length) frame++;
            else if (loop) frame = 0;
            else { timer = 0f; break; }   // stay on the last frame
        }
        image.sprite = frames[frame];
    }

    // Unity calls this when a value is changed in the Inspector.
    void OnValidate()
    {
#if UNITY_EDITOR
        // Not while playing: icons set from code (ShowIcon) have no Aseprite field, and reading would wipe their frames.
        if (!Application.isPlaying) ReadAseprite();
#endif
        if (image == null) image = GetComponent<Image>();
        ShowCurrent();   // preview in the editor without playing
    }

    // ===== My methods =====

    // Shows an icon on any Image: animated if the IconSource is an Aseprite animation, otherwise the still picture.
    // Used by the inventory slots, the cursor stack and the build menu. source can be null (= just the sprite).
    // Adds this script to the Image only when something animated is shown there.
    public static void ShowIcon(Image target, Sprite still, IconSource source)
    {
        if (target == null) return;
        UIAnimatedImage anim = target.GetComponent<UIAnimatedImage>();

        if (source != null && source.IsAnimated)
        {
            if (anim == null) anim = target.gameObject.AddComponent<UIAnimatedImage>();
            anim.Play(source.frames, source.durations);
            return;
        }

        if (anim != null) anim.ShowStill(still);   // was animating something else before: stop
        target.sprite = still;
    }

    // Plays these frames (e.g. an item's Aseprite icon). Same frames as now = keeps playing smoothly.
    public void Play(Sprite[] newFrames, float[] newDurations)
    {
        if (image == null) image = GetComponent<Image>();
        bool same = animated && frames == newFrames;
        animated = true;
        frames = newFrames ?? new Sprite[0];
        durations = newDurations ?? new float[0];
        if (!same) { frame = 0; timer = 0f; }
        ShowCurrent();
    }

    // Stops animating and shows one still picture.
    public void ShowStill(Sprite still)
    {
        if (image == null) image = GetComponent<Image>();
        animated = false;
        staticImage = still;
        ShowCurrent();
    }

    // Switches between still and animated from code (e.g. a hint that starts moving).
    public void SetAnimated(bool on)
    {
        animated = on;
        frame = 0;
        timer = 0f;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        if (image == null) return;
        Sprite s = animated ? (frames.Length > 0 ? frames[Mathf.Min(frame, frames.Length - 1)] : null) : staticImage;
        if (s != null) image.sprite = s;   // nothing set yet: keep whatever the Image already shows
    }

    float Duration(int i)
    {
        float d = i < durations.Length ? durations[i] : 0f;
        return d > 0f ? d : 1f / fallbackFps;
    }

#if UNITY_EDITOR
    // For editor tools (e.g. BuildModeUIBuilder): read the .aseprite file now and show the first frame,
    // without waiting for someone to touch the Inspector.
    public void EditorRefresh()
    {
        if (image == null) image = GetComponent<Image>();
        ReadAseprite();
        frame = 0;
        ShowCurrent();
        EditorUtility.SetDirty(this);
    }

    // Fills frames and durations from the .aseprite file (see AsepriteFrames).
    void ReadAseprite()
    {
        if (aseprite == null)
        {
            frames = new Sprite[0];
            durations = new float[0];
            return;
        }

        if (!AsepriteFrames.IsAseprite(aseprite))
        {
            Debug.LogWarning(name + ": Aseprite must be an .aseprite file.", this);
            aseprite = null;
            return;
        }

        AsepriteFrames.Read(aseprite, asepriteTag, out frames, out durations);
    }
#endif
}
