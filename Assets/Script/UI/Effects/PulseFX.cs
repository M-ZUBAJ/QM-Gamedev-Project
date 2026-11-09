/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto any UI picture or text that should gently pulse (e.g. the glow behind START GAME,
        the "INCOMING" text).
Unity Editor: Nothing to connect. Testing: Play and watch it breathe; change the numbers while playing.
Purpose: A slow "heartbeat": the object grows a little and fades a little, over and over, to catch the eye.
Requirement: PulseFX.cs x-> UITween.cs
             PulseFX.cs <-x TitleSceneBuilder.cs
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PulseFX : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.1f, 5f)]
    [Tooltip("Pulses per second.")]
    [SerializeField] float speed = 0.8f;

    [Range(0f, 0.5f)]
    [Tooltip("How much bigger it gets at the top of a pulse (0.06 = 6% bigger).")]
    [SerializeField] float growBy = 0.06f;

    [Range(0f, 1f)]
    [Tooltip("How see-through it gets at the bottom of a pulse (1 = never fades, 0.3 = fades to 30%).")]
    [SerializeField] float lowestAlpha = 0.45f;

    Graphic graphic;   // can be missing: then it only grows
    Vector3 baseScale;
    float baseAlpha = 1f;
    float time;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseScale = transform.localScale;
        if (graphic != null) baseAlpha = graphic.color.a;
    }

    void Update()
    {
        time += UITween.DeltaTime;
        float wave = 0.5f + 0.5f * Mathf.Sin(time * speed * Mathf.PI * 2f);   // 0 -> 1 -> 0, smoothly

        transform.localScale = baseScale * (1f + growBy * wave);
        if (graphic != null)
        {
            Color c = graphic.color;
            graphic.color = new Color(c.r, c.g, c.b, baseAlpha * Mathf.Lerp(lowestAlpha, 1f, wave));
        }
    }

    void OnDisable()
    {
        transform.localScale = baseScale;
        if (graphic != null)
        {
            Color c = graphic.color;
            graphic.color = new Color(c.r, c.g, c.b, baseAlpha);
        }
    }
}
