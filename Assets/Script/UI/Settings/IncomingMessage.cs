/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Put this on a "coming soon" page (Tools > UI > Build Main Title Scene puts it on the Controls, Display and Audio
        tabs). It animates its child "Message" (a TextMeshPro text) and, if there is one, "Stripes" (a Raw Image).
Unity Editor: Word = the text before the dots. Testing: Play, open Settings, click one of those tabs.
Purpose: A placeholder that looks alive while the real settings are still being made: "INCOMING" with dots that count
         up (INCOMING. .. ...) and a striped bar sliding along underneath, like a construction sign.
         (The glow pulse on the text is a PulseFX next to it.)
Requirement: IncomingMessage.cs x-> UITween.cs, UIFXSprites.cs
             IncomingMessage.cs <-x TitleSceneBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IncomingMessage : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The text before the dots.")]
    [SerializeField] string word = "INCOMING";

    [Range(0.5f, 10f)]
    [Tooltip("How many dots appear per second.")]
    [SerializeField] float dotsPerSecond = 2.5f;

    [Range(0f, 2f)]
    [Tooltip("How fast the stripes slide.")]
    [SerializeField] float stripeSpeed = 0.35f;

    TMP_Text message;
    RawImage stripes;
    float time;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Transform t = transform.Find("Message");
        message = t != null ? t.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        Transform s = transform.Find("Stripes");
        stripes = s != null ? s.GetComponent<RawImage>() : null;
        if (stripes != null && stripes.texture == null) stripes.texture = UIFXSprites.Get(UIFXSprites.Shape.Stripes).texture;
    }

    void Update()
    {
        time += UITween.DeltaTime;

        if (message != null)
        {
            // The dots count 0, 1, 2, 3, 0... The missing ones are see-through, so the word never jumps sideways.
            int dots = Mathf.FloorToInt(time * dotsPerSecond) % 4;
            message.text = word + new string('.', dots) + "<alpha=#00>" + new string('.', 3 - dots);
        }

        if (stripes != null)
        {
            // Keep the stripes square whatever the bar's size, and slide them along.
            Rect r = stripes.rectTransform.rect;
            float across = r.height > 0f ? r.width / r.height : 1f;
            stripes.uvRect = new Rect(-time * stripeSpeed, 0f, across, 1f);
        }
    }
}
