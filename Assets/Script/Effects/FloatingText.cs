/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to put on an object: FloatingText.Show(...) makes one by itself (e.g. the turtle's "BLOCKED!" and the
        damage numbers, DamageNumbers).
Unity Editor: Nothing to set. Testing: punch a turtle while it's in its shell (ShellDefense > ⋮ > Test: Hide In Shell).
Purpose: A short word in the world that pops up, rises and fades away, always facing the camera and drawn over
         everything (a TextMeshPro text, overlay), e.g. "BLOCKED!" over a turtle whose shell a hit bounced off. It
         removes itself when it has faded. It runs on real time, so one shown just before the game freezes (the
         victory page, the pause menu) still fades away instead of hanging in the middle of the screen.
Requirement: FloatingText.cs x-> TextMeshPro (Unity)
             FloatingText.cs <-x DamageNumbers.cs
             FloatingText.cs <-o ShellDefense.cs
*/

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class FloatingText : MonoBehaviour
{
    // ===== Fields =====
    const float PopSeconds = 0.12f;   // it starts bigger and shrinks to its size this fast: a little pop
    const float FadeFrom = 0.6f;      // it starts fading after this share of its time

    TextMeshPro text;
    float seconds;    // how long it shows
    float age;        // how long it has shown
    Color color;
    Vector3 start;
    float pop = 1.6f;   // how much bigger it starts (1.6 = 60 % bigger)
    float rise = 0.7f;  // meters it floats up while it fades

    // ===== Unity messages (Unity calls these) =====

    // LateUpdate: after the camera has moved this frame, so it faces the camera exactly.
    void LateUpdate()
    {
        age += Time.unscaledDeltaTime;   // real time: it still fades while the game is frozen (Time Scale 0)
        float k = seconds > 0f ? age / seconds : 1f;
        if (k >= 1f || text == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = start + Vector3.up * (rise * (1f - (1f - k) * (1f - k)));   // fast at first, then slower
        transform.localScale = Vector3.one * (age < PopSeconds ? Mathf.Lerp(pop, 1f, age / PopSeconds) : 1f);
        Color c = color;
        c.a *= k < FadeFrom ? 1f : 1f - (k - FadeFrom) / (1f - FadeFrom);
        text.color = c;
        FaceCamera();
    }

    // ===== My methods =====

    // Shows "words" at "point" in the world for "seconds". size = the letters' size (3 = about a quarter of a meter),
    // pop = how much bigger it starts (a critical hit's number pops harder), rise = meters it floats up.
    public static void Show(Vector3 point, string words, Color color, float size = 3f, float seconds = 0.9f,
                            float pop = 1.6f, float rise = 0.7f)
    {
        if (string.IsNullOrEmpty(words)) return;

        GameObject go = new GameObject("Floating Text");
        go.transform.position = point;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = words;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.color = color;
        tmp.isOverlay = true;        // drawn over the turtle (and everything else), so it's never hidden inside it
        tmp.outlineWidth = 0.25f;    // a dark edge: readable on the bright desert and at night
        tmp.outlineColor = new Color32(0, 0, 0, 220);
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

        FloatingText floating = go.AddComponent<FloatingText>();
        floating.text = tmp;
        floating.seconds = seconds;
        floating.color = color;
        floating.start = point;
        floating.pop = Mathf.Max(1f, pop);
        floating.rise = rise;
        floating.FaceCamera();
    }

    // Turned toward the camera, so the words read the right way round from wherever the player looks.
    void FaceCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
