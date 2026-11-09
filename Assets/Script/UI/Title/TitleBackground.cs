/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Put this on the title screen's background canvas (Tools > UI > Build Main Title Scene makes BackgroundCanvas with
        it), a canvas with the children "SkyBottom" (a plain Image filling the screen), "SkyTop" (an Image filling the
        screen with the sky gradient picture) and "Stars" (an empty object filling the screen).
Unity Editor: Sky Top / Sky Bottom = the colors over one cycle (left = start, middle = the dawn glow, right = back to the
              start). Click a gradient to change it. Testing: Play and watch; set Cycle Seconds to 5 to see it quickly.
Purpose: A living sky behind the menu: it slowly goes from night blue to purple to an orange dawn glow and back
         (the game is about surviving the nights), with twinkling stars that fade away as dawn comes.
         Glowing embers rise in front of it (a Particle System in the scene), and the menu sits on top.
Requirement: TitleBackground.cs x-> UITween.cs, UIFXSprites.cs
             TitleBackground.cs <-x TitleSceneBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleBackground : MonoBehaviour
{
    // One star.
    class Star
    {
        public Image image;
        public float phase, speed, brightness;
    }

    // ===== Fields =====
    [Header("Sky")]
    [Tooltip("The color at the top of the sky over one cycle.")]
    [SerializeField] Gradient skyTop = DefaultTop();

    [Tooltip("The color near the horizon (bottom of the screen) over one cycle.")]
    [SerializeField] Gradient skyBottom = DefaultBottom();

    [Range(5f, 300f)]
    [Tooltip("Seconds for one whole night -> dawn -> night cycle.")]
    [SerializeField] float cycleSeconds = 40f;

    [Header("Stars")]
    [Range(0, 300)]
    [Tooltip("How many stars.")]
    [SerializeField] int starCount = 90;

    [Tooltip("The stars' color.")]
    [SerializeField] Color starColor = new Color(1f, 0.97f, 0.85f);

    [Tooltip("The star picture. Empty = a soft glowing dot.")]
    [SerializeField] Sprite starSprite;

    Image top, bottom;
    RectTransform starHolder;
    readonly List<Star> stars = new List<Star>();
    float time;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        // A "Screen Space - Camera" canvas needs the camera (a prefab can't remember the scene's camera).
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        top = FindImage("SkyTop");
        bottom = FindImage("SkyBottom");
        starHolder = transform.Find("Stars") as RectTransform;
        MakeStars();
    }

    void Update()
    {
        time += UITween.DeltaTime;
        float phase = Mathf.Repeat(time / cycleSeconds, 1f);

        if (top != null) top.color = skyTop.Evaluate(phase);
        if (bottom != null) bottom.color = skyBottom.Evaluate(phase);

        // Stars show at night and fade as the sky gets lighter.
        float night = top != null ? 1f - Mathf.Clamp01((top.color.grayscale - 0.05f) / 0.12f) : 1f;
        foreach (Star s in stars)
        {
            float twinkle = 0.55f + 0.45f * Mathf.Sin(time * s.speed + s.phase);
            s.image.color = new Color(starColor.r, starColor.g, starColor.b, starColor.a * s.brightness * twinkle * night);
        }
    }

    // ===== My methods =====

    // Scatters the stars over the top three quarters of the sky, each with its own size and twinkle.
    void MakeStars()
    {
        if (starHolder == null) return;
        Sprite sprite = starSprite != null ? starSprite : UIFXSprites.Get(UIFXSprites.Shape.SoftDot);

        for (int i = 0; i < starCount; i++)
        {
            GameObject go = new GameObject("Star", typeof(RectTransform));
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(starHolder, false);
            Vector2 at = new Vector2(Random.value, Random.Range(0.25f, 1f));
            rt.anchorMin = rt.anchorMax = at;
            rt.sizeDelta = Vector2.one * Random.Range(3f, 9f);

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            stars.Add(new Star { image = image, phase = Random.Range(0f, 10f), speed = Random.Range(0.8f, 2.6f),
                                 brightness = Random.Range(0.35f, 1f) });
        }
    }

    Image FindImage(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    // Night -> purple -> orange dawn glow -> purple -> night. The same color at both ends, so it loops smoothly.
    static Gradient DefaultTop()
    {
        return MakeGradient(new Color(0.03f, 0.04f, 0.11f), new Color(0.10f, 0.07f, 0.22f), new Color(0.27f, 0.15f, 0.33f));
    }

    static Gradient DefaultBottom()
    {
        return MakeGradient(new Color(0.06f, 0.08f, 0.18f), new Color(0.25f, 0.12f, 0.26f), new Color(0.82f, 0.38f, 0.17f));
    }

    static Gradient MakeGradient(Color night, Color purple, Color dawn)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(night, 0f), new GradientColorKey(purple, 0.35f), new GradientColorKey(dawn, 0.5f),
                new GradientColorKey(purple, 0.65f), new GradientColorKey(night, 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
