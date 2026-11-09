/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up. UISparks.Burst(...) adds this script to the canvas by itself the first time.
Unity Editor: Nothing to set. TitleMenu's Sparks tick box turns every burst on or off (UISparks.Enabled).
Purpose: A burst of small glowing sparks flying out of a UI object (e.g. a clicked button), falling a little and
         fading away, like a tiny firework. The sparks are reused, so bursts make no garbage.
           UISparks.Burst(buttonRect, Color.yellow, 14);
Requirement: UISparks.cs x-> UITween.cs, UIFXSprites.cs
             UISparks.cs <-x ButtonFX.cs, TitleMenu.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UISparks : MonoBehaviour
{
    // One spark.
    class Spark
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;   // pixels per second
        public Color color;
        public float age, life;    // seconds lived / seconds it lives
        public float size;
    }

    // ===== Fields =====
    public static bool Enabled = true;   // TitleMenu's Sparks tick box

    const float Gravity = -900f;   // pixels per second per second: the sparks curve down a little
    const float Drag = 2.5f;       // how quickly they slow down (higher = shorter flight)

    readonly List<Spark> sparks = new List<Spark>();   // every spark ever made here (alive or waiting)
    RectTransform layer;                               // the full-screen object the sparks fly in, on top

    // ===== Unity messages (Unity calls these) =====

    void Update()
    {
        float dt = UITween.DeltaTime;
        foreach (Spark s in sparks)
        {
            if (!s.image.enabled) continue;

            s.age += dt;
            if (s.age >= s.life) { s.image.enabled = false; continue; }

            s.velocity.y += Gravity * dt;
            s.velocity *= Mathf.Exp(-Drag * dt);
            s.rect.anchoredPosition += s.velocity * dt;

            float left = 1f - s.age / s.life;   // 1 = just born, 0 = gone
            s.rect.sizeDelta = Vector2.one * s.size * (0.4f + 0.6f * left);
            s.image.color = new Color(s.color.r, s.color.g, s.color.b, s.color.a * left);
        }
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Enabled = true;
    }

    // ===== My methods =====

    // Sends "count" sparks flying out from the middle of "from" (e.g. a clicked button), in this color.
    public static void Burst(RectTransform from, Color color, int count)
    {
        if (!Enabled || from == null || count <= 0) return;

        Canvas canvas = from.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        UISparks fx = canvas.GetComponent<UISparks>();
        if (fx == null) fx = canvas.gameObject.AddComponent<UISparks>();
        fx.Emit(from, color, count);
    }

    void Emit(RectTransform from, Color color, int count)
    {
        MakeLayer();
        layer.SetAsLastSibling();   // over everything else on this canvas

        // The middle of "from", in the layer's own coordinates.
        Vector3 world = from.TransformPoint(from.rect.center);
        Vector2 start = layer.InverseTransformPoint(world);

        for (int i = 0; i < count; i++)
        {
            Spark s = FreeSpark();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(350f, 950f);

            s.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            s.age = 0f;
            s.life = Random.Range(0.45f, 0.9f);
            s.size = Random.Range(8f, 18f);
            s.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.5f));   // some whiter, like real sparks
            s.rect.anchoredPosition = start;
            s.image.enabled = true;
        }
    }

    // A full-screen, click-through object to fly the sparks in.
    void MakeLayer()
    {
        if (layer != null) return;

        GameObject go = new GameObject("Sparks (fx)", typeof(RectTransform));
        go.layer = gameObject.layer;
        layer = (RectTransform)go.transform;
        layer.SetParent(transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }

    // A spark that isn't flying right now, or a new one.
    Spark FreeSpark()
    {
        foreach (Spark s in sparks)
        {
            if (!s.image.enabled) return s;
        }

        GameObject go = new GameObject("Spark", typeof(RectTransform));
        go.layer = gameObject.layer;
        Spark spark = new Spark { rect = (RectTransform)go.transform, image = go.AddComponent<Image>() };
        spark.rect.SetParent(layer, false);
        spark.image.sprite = UIFXSprites.Get(UIFXSprites.Shape.SoftDot);
        spark.image.raycastTarget = false;   // sparks never block clicks
        spark.image.enabled = false;
        sparks.Add(spark);
        return spark;
    }
}
