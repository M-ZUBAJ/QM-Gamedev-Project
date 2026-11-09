/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). The menu effects use it for their soft shapes.
Unity Editor: Nothing to set. Tools > UI > Build Main Title Scene also saves these shapes as PNG files in
              Assets/UI/Title (so the title background looks right in the editor too). Replace a PNG with your own
              art if you like: the tool only draws the ones that are missing.
Purpose: Draws the soft white shapes the menu effects need, so no art is needed: a glowing dot (sparks, stars,
         ripples), a ring (the START GAME shockwave), a shine streak, the sky gradient, the dark vignette around the
         screen edges and diagonal stripes. They're white with see-through parts, so any color can tint them.
         While playing, each shape is drawn once and shared (Get); the builder saves them with Draw.
Requirement: UIFXSprites.cs <-x ButtonFX.cs, IncomingMessage.cs, TitleBackground.cs, TitleSceneBuilder.cs, UISparks.cs
*/

using System.Collections.Generic;
using UnityEngine;

public static class UIFXSprites
{
    // The shapes this can draw.
    public enum Shape
    {
        SoftDot,       // a round glow, bright in the middle (sparks, stars, ripples, glows)
        Ring,          // a soft circle line (the shockwave)
        Shine,         // a soft vertical streak (the light sweeping over a button)
        SkyGradient,   // white at the top, fading to nothing at the bottom (the sky's top color)
        Vignette,      // nothing in the middle, dark towards the edges
        Stripes,       // diagonal stripes that repeat (the "INCOMING" bar)
    }

    // ===== Fields =====
    static readonly Dictionary<Shape, Sprite> made = new Dictionary<Shape, Sprite>();   // drawn while playing

    // ===== My methods =====

    // The shape as a Sprite, drawn the first time it's asked for, then shared.
    // Made in code: never saved into a scene (use the builder's PNG files for scene objects).
    public static Sprite Get(Shape shape)
    {
        if (made.TryGetValue(shape, out Sprite sprite) && sprite != null) return sprite;

        Texture2D texture = Draw(shape);
        texture.hideFlags = HideFlags.DontSave;
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        made[shape] = sprite;
        return sprite;
    }

    // Draws the shape into a new texture (white, the shape's see-through amount in alpha).
    public static Texture2D Draw(Shape shape)
    {
        Vector2Int size = SizeOf(shape);
        Texture2D texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
        texture.wrapMode = shape == Shape.Stripes ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size.x * size.y];
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                // u, v = where this pixel is, from 0 (left / bottom) to 1 (right / top), measured at its middle.
                float u = (x + 0.5f) / size.x;
                float v = (y + 0.5f) / size.y;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(AlphaAt(shape, u, v)) * 255f);
                pixels[y * size.x + x] = new Color32(255, 255, 255, a);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // How big each shape is drawn, in pixels.
    static Vector2Int SizeOf(Shape shape)
    {
        switch (shape)
        {
            case Shape.Ring: return new Vector2Int(256, 256);
            case Shape.Shine: return new Vector2Int(64, 16);
            case Shape.SkyGradient: return new Vector2Int(8, 256);
            case Shape.Vignette: return new Vector2Int(256, 256);
            case Shape.Stripes: return new Vector2Int(64, 64);
            default: return new Vector2Int(64, 64);   // SoftDot
        }
    }

    // How solid the shape is at (u, v): 0 = see-through, 1 = solid white.
    static float AlphaAt(Shape shape, float u, float v)
    {
        // Distance from the middle: 0 in the middle, 1 at the edge of the square.
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;

        switch (shape)
        {
            case Shape.SoftDot:
                return Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f);                    // bright middle, soft edge

            case Shape.Ring:
                return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f), 2f);   // a line at 82% out

            case Shape.Shine:
                return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 2f), 2f);       // brightest down the middle

            case Shape.SkyGradient:
                return Mathf.Pow(v, 1.4f);                                         // solid at the top only

            case Shape.Vignette:
                return Mathf.SmoothStep(0f, 0.85f, Mathf.InverseLerp(0.55f, 1.35f, d));   // dark corners

            case Shape.Stripes:
                float band = Mathf.Repeat((u + v) * 2f, 1f);                       // 2 diagonal stripes per tile
                return band < 0.5f ? 0.9f : 0f;

            default:
                return 1f;
        }
    }
}
