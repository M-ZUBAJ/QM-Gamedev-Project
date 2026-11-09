/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto a TextMeshPro text, e.g. the game title on the title screen.
Unity Editor: Nothing to connect. TitleMenu plays the falling letters in its opening; on its own the text just floats
              and shines. Testing: Play and watch the title; change the numbers while playing.
Purpose: Brings a title to life, letter by letter:
           Falling in: each letter drops from above and bounces into place, one after another, flashing as it lands.
           Floating:   the letters bob up and down in a slow wave.
           Shine:      a band of light sweeps across the letters every few seconds.
         Only moves the letters' shapes (no new objects), so any font and size works.
Requirement: TitleTextFX.cs x-> UITween.cs, TMP_Text (Unity)
             TitleTextFX.cs <-x TitleMenu.cs, TitleSceneBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class TitleTextFX : MonoBehaviour
{
    enum State { Shown, Hidden, Falling }

    // ===== Fields =====
    [Header("Falling in")]
    [Tooltip("How far above its place each letter starts falling from, in pixels.")]
    [SerializeField] float dropHeight = 300f;

    [Range(0f, 0.3f)]
    [Tooltip("Seconds between one letter starting to fall and the next.")]
    [SerializeField] float letterDelay = 0.05f;

    [Range(0.1f, 2f)]
    [Tooltip("Seconds each letter takes to fall and bounce.")]
    [SerializeField] float fallTime = 0.7f;

    [Header("Floating")]
    [Tooltip("How far the letters bob up and down, in pixels (0 = still).")]
    [SerializeField] float waveHeight = 7f;

    [Tooltip("Waves per second.")]
    [SerializeField] float waveSpeed = 0.5f;

    [Tooltip("How far behind each letter is in the wave (bigger = more letters per wave).")]
    [SerializeField] float waveSpread = 0.45f;

    [Header("Shine")]
    [Tooltip("Seconds between two shines (0 = only when a script asks).")]
    [SerializeField] float shineEvery = 6f;

    [Range(0.1f, 3f)]
    [Tooltip("Seconds one shine takes to cross the whole title.")]
    [SerializeField] float shineTime = 0.9f;

    [Tooltip("The color of the light band and of the flash when a letter lands.")]
    [SerializeField] Color shineColor = new Color(1f, 0.95f, 0.75f);

    const float ShineWidth = 0.12f;   // how wide the light band is, as a part of the title's width
    const float FlashTime = 0.35f;    // how long a landing letter flashes (as a part of Fall Time)
    const float FirstLanding = 0.364f;// when a bouncing letter first touches its place (as a part of Fall Time)

    TMP_Text text;
    State state = State.Shown;
    float time, fallClock, shineClock = float.MaxValue, nextShine;
    float wave = 1f;   // how much of the floating wave shows: 0 while falling, back to 1 after

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        nextShine = shineEvery;
    }

    void LateUpdate()
    {
        float dt = UITween.DeltaTime;
        time += dt;
        fallClock += dt;
        shineClock += dt;

        if (state == State.Falling && fallClock >= FallDuration()) state = State.Shown;
        wave = state == State.Shown ? Mathf.MoveTowards(wave, 1f, dt) : 0f;   // the wave fades back in after falling

        if (state == State.Shown && shineEvery > 0f && time >= nextShine)
        {
            Shine();
            nextShine = time + shineEvery;
        }

        Animate();
    }

    // ===== My methods =====

    // Makes the letters invisible (before the opening drops them in).
    public void HideLetters()
    {
        state = State.Hidden;
    }

    // Drops the letters in, one after another.
    public void PlayIntro()
    {
        state = State.Falling;
        fallClock = 0f;
    }

    // Skips to the end: every letter in place.
    public void FinishIntro()
    {
        state = State.Shown;
    }

    // Sends one band of light across the title now.
    public void Shine()
    {
        shineClock = 0f;
        nextShine = time + shineEvery;
    }

    // How long the whole fall takes, for the last letter too.
    float FallDuration()
    {
        return Mathf.Max(0, text.textInfo.characterCount - 1) * letterDelay + fallTime;
    }

    // Moves and colors each letter for this frame.
    void Animate()
    {
        text.ForceMeshUpdate();   // fresh letters in their normal places
        TMP_TextInfo info = text.textInfo;
        if (info.characterCount == 0) return;

        // The title's left and right edge, to know where each letter is along it (0 = left, 1 = right).
        float left = float.MaxValue, right = float.MinValue;
        for (int i = 0; i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            left = Mathf.Min(left, info.characterInfo[i].bottomLeft.x);
            right = Mathf.Max(right, info.characterInfo[i].topRight.x);
        }
        float width = Mathf.Max(1f, right - left);
        float shineAt = Mathf.Lerp(-0.2f, 1.2f, shineClock / shineTime);   // where the light band is now
        bool shining = shineClock < shineTime;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            float lift = 0f, alpha = 1f, light = 0f;

            if (state == State.Hidden) alpha = 0f;
            else if (state == State.Falling)
            {
                float part = (fallClock - i * letterDelay) / fallTime;   // this letter's own progress
                if (part <= 0f) alpha = 0f;
                else
                {
                    lift = (1f - UITween.Evaluate(UITween.Ease.OutBounce, part)) * dropHeight;
                    alpha = Mathf.Clamp01(part * 5f);
                    if (part >= FirstLanding) light = Mathf.Clamp01(1f - (part - FirstLanding) / FlashTime);
                }
            }

            lift += Mathf.Sin((time * waveSpeed * Mathf.PI * 2f) - i * waveSpread) * waveHeight * wave;

            if (shining)
            {
                float x = ((c.bottomLeft.x + c.topRight.x) / 2f - left) / width;
                light = Mathf.Max(light, Mathf.Clamp01(1f - Mathf.Abs(x - shineAt) / ShineWidth));
            }

            int m = c.materialReferenceIndex, v = c.vertexIndex;
            Vector3[] vertices = info.meshInfo[m].vertices;
            Color32[] colors = info.meshInfo[m].colors32;
            for (int k = 0; k < 4; k++)   // the 4 corners of the letter
            {
                vertices[v + k].y += lift;
                Color lit = Color.Lerp(colors[v + k], shineColor, light);
                lit.a = colors[v + k].a / 255f * alpha;
                colors[v + k] = lit;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}
