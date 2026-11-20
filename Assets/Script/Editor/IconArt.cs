/*
First Date: 19/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. Tools > UI > Build HUD and Tools > UI > Build Perk Panel use it for their pictures.
              A picture is only drawn if its file isn't there yet, so your own art saved under the same name is kept
              (delete a file to get the drawn one back).
Purpose: Draws the small game pictures in code and saves them as PNG sprites, so nothing has to be drawn by hand:
           Assets/UI/Common/Coin.png          a gold coin (the HUD's coins, a chest's price)
           Assets/UI/Common/RoundedBox.png    a white box with round corners (9-sliced: any size, tinted any color)
           Assets/UI/Common/Glow.png          a soft round glow (behind the perk icons)
           Assets/UI/Perks/PerkHealth.png     a red heart          (Max Health)
           Assets/UI/Perks/PerkStamina.png    a yellow lightning   (Max Stamina)
           Assets/UI/Perks/PerkSpeed.png      blue double arrows   (Move Speed)
           Assets/UI/Perks/PerkFireball.png   an orange flame      (Fireball)
           Assets/UI/Stats/Hunger.png         a drumstick          (the inventory's Info tab: hunger)
           Assets/UI/Stats/Jump.png           a green up arrow     (the inventory's Info tab: jump height)
           Assets/UI/Stats/Armor.png          a steel breastplate  (the inventory's Info tab: armor)
           Assets/UI/Armor/<Slot>.png         a white outline of each armor piece (helmet, shoulders, chestplate, gloves,
                                              leggings, boots): shown faintly in an empty slot on the Gear page
         Each pixel is the average of 4 x 4 points, so the edges come out smooth, not jagged.
         GivePerkIcons puts the perk pictures in the player's PlayerPerks (its Icons list), for the HUD's perk icons
         and the Info tab.
Requirement: IconArt.cs x-> UIBuildHelper.cs, UIFXSprites.cs, PlayerPerks.cs, ArmorStats.cs
             IconArt.cs <-x GearPageBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, PerkPanelBuilder.cs
*/

using System.IO;
using UnityEditor;
using UnityEngine;
using static UIBuildHelper;   // MakeFolders, FolderOf

public static class IconArt
{
    // ===== Fields =====
    public const string CommonFolder = "Assets/UI/Common";
    public const string PerkFolder = "Assets/UI/Perks";
    public const string StatFolder = "Assets/UI/Stats";
    public const string ArmorFolder = "Assets/UI/Armor";

    const int IconSize = 128;    // pixels: sharp up to about 128 pixels on screen
    const int BoxSize = 64;      // the round box is small: 9-slicing stretches it to any size
    const int BoxRadius = 20;    // its corners, in pixels
    const int Samples = 4;       // 4 x 4 points per pixel

    static readonly Color Shadow = new Color(0f, 0f, 0f, 0.35f);

    // The shapes, with x and y from -1 to 1 (y up).
    static readonly Vector2[] Bolt =
    {
        new Vector2(0.16f, 0.84f), new Vector2(-0.46f, -0.04f), new Vector2(-0.05f, -0.04f), new Vector2(-0.21f, -0.84f),
        new Vector2(0.49f, 0.11f), new Vector2(0.07f, 0.11f), new Vector2(0.32f, 0.84f),
    };
    static readonly Vector2[] ChevronBack = Chevron(-0.24f);
    static readonly Vector2[] ChevronFront = Chevron(0.26f);
    static readonly Vector2[] FlameTip = { new Vector2(-0.5f, -0.22f), new Vector2(0.5f, -0.22f), new Vector2(0.06f, 0.92f) };
    static readonly Vector2[] FlameCoreTip = { new Vector2(-0.27f, -0.38f), new Vector2(0.27f, -0.38f), new Vector2(0.03f, 0.36f) };
    static readonly Vector2[] Breastplate =
    {
        new Vector2(-0.72f, 0.5f), new Vector2(-0.34f, 0.72f), new Vector2(-0.16f, 0.5f), new Vector2(0.16f, 0.5f),
        new Vector2(0.34f, 0.72f), new Vector2(0.72f, 0.5f), new Vector2(0.56f, 0.1f), new Vector2(0.5f, -0.72f),
        new Vector2(-0.5f, -0.72f), new Vector2(-0.56f, 0.1f),
    };
    static readonly Vector2[] Palm = { new Vector2(-0.36f, -0.7f), new Vector2(0.36f, -0.7f), new Vector2(0.38f, 0.12f), new Vector2(-0.38f, 0.12f) };
    static readonly Vector2[] Cuff = { new Vector2(-0.44f, -0.92f), new Vector2(0.44f, -0.92f), new Vector2(0.4f, -0.62f), new Vector2(-0.4f, -0.62f) };
    static readonly Vector2[] Trousers =
    {
        new Vector2(-0.52f, 0.72f), new Vector2(0.52f, 0.72f), new Vector2(0.56f, -0.82f), new Vector2(0.14f, -0.82f),
        new Vector2(0f, 0.08f), new Vector2(-0.14f, -0.82f), new Vector2(-0.56f, -0.82f),
    };
    static readonly Vector2[] Boot =
    {
        new Vector2(-0.42f, 0.76f), new Vector2(0.12f, 0.76f), new Vector2(0.14f, -0.22f), new Vector2(0.56f, -0.34f),
        new Vector2(0.72f, -0.54f), new Vector2(0.7f, -0.78f), new Vector2(-0.44f, -0.78f),
    };
    static readonly Vector2[] UpArrow =
    {
        new Vector2(0f, 0.86f), new Vector2(0.62f, 0.18f), new Vector2(0.24f, 0.18f), new Vector2(0.24f, -0.82f),
        new Vector2(-0.24f, -0.82f), new Vector2(-0.24f, 0.18f), new Vector2(-0.62f, 0.18f),
    };

    // ===== My methods =====

    public static Sprite Coin() => Save(CommonFolder + "/Coin.png", () => Draw(IconSize, CoinPaint), Vector4.zero);
    public static Sprite RoundedBox() => Save(CommonFolder + "/RoundedBox.png", () => Draw(BoxSize, BoxPaint), Vector4.one * (BoxRadius + 4));
    public static Sprite Glow() => Save(CommonFolder + "/Glow.png", () => UIFXSprites.Draw(UIFXSprites.Shape.SoftDot), Vector4.zero);
    public static Sprite PerkHealth() => Save(PerkFolder + "/PerkHealth.png", () => Draw(IconSize, HeartPaint), Vector4.zero);
    public static Sprite PerkStamina() => Save(PerkFolder + "/PerkStamina.png", () => Draw(IconSize, BoltPaint), Vector4.zero);
    public static Sprite PerkSpeed() => Save(PerkFolder + "/PerkSpeed.png", () => Draw(IconSize, SpeedPaint), Vector4.zero);
    public static Sprite PerkFireball() => Save(PerkFolder + "/PerkFireball.png", () => Draw(IconSize, FlamePaint), Vector4.zero);
    public static Sprite StatHunger() => Save(StatFolder + "/Hunger.png", () => Draw(IconSize, HungerPaint), Vector4.zero);
    public static Sprite StatJump() => Save(StatFolder + "/Jump.png", () => Draw(IconSize, JumpPaint), Vector4.zero);
    public static Sprite StatArmor() => Save(StatFolder + "/Armor.png", () => Draw(IconSize, ArmorPaint), Vector4.zero);

    // A white outline of an armor piece, shown faintly in an empty slot on the Gear page (tinted by the slot).
    public static Sprite ArmorSlotIcon(ArmorSlot slot) =>
        Save(ArmorFolder + "/" + slot + ".png", () => Draw(IconSize, (x, y) => InArmor(slot, x, y) ? Color.white : Color.clear), Vector4.zero);

    // The drawn picture of one of the player's perks (PlayerPerks' PerkType).
    public static Sprite ForPerk(PerkType type)
    {
        switch (type)
        {
            case PerkType.MaxHealth:  return PerkHealth();
            case PerkType.MaxStamina: return PerkStamina();
            case PerkType.MoveSpeed:  return PerkSpeed();
            case PerkType.Fireball:   return PerkFireball();
            default:                  return null;
        }
    }

    // Gives the player's PlayerPerks a picture for every perk that has none yet (its Icons list), so the HUD's perk
    // icons and the inventory's Info tab can show them. Pictures you chose stay. Ctrl + Z undoes it.
    public static void GivePerkIcons(PlayerPerks perks)
    {
        if (perks == null) return;
        SerializedObject so = new SerializedObject(perks);
        SerializedProperty list = so.FindProperty("icons");   // the names in quotes must match PlayerPerks' fields
        if (list == null) return;

        foreach (PerkType type in System.Enum.GetValues(typeof(PerkType)))
        {
            SerializedProperty entry = null;
            for (int i = 0; i < list.arraySize && entry == null; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("type").enumValueIndex == (int)type) entry = e;
            }
            if (entry == null)   // a perk with no line yet: add one
            {
                list.arraySize++;
                entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                entry.FindPropertyRelative("type").enumValueIndex = (int)type;
                entry.FindPropertyRelative("icon").objectReferenceValue = null;
            }
            SerializedProperty icon = entry.FindPropertyRelative("icon");
            if (icon.objectReferenceValue == null) icon.objectReferenceValue = ForPerk(type);
        }
        so.ApplyModifiedProperties();
    }

    // ----- The pictures: each gives the color of one point (x and y from -1 to 1, y up) -----

    static Color CoinPaint(float x, float y)
    {
        float d = Mathf.Sqrt(x * x + y * y);   // how far from the middle
        Color c = Color.clear;
        if (Circle(x - 0.05f, y + 0.07f, 0.9f)) c = Over(c, Shadow);                                 // a soft shadow, down right
        if (d <= 0.88f) c = Over(c, new Color(0.6f, 0.37f, 0.05f));                                   // the dark rim
        if (d <= 0.74f) c = Over(c, Color.Lerp(new Color(0.93f, 0.6f, 0.1f), new Color(1f, 0.89f, 0.38f), (y + 0.74f) / 1.48f));   // gold, lighter at the top
        if (d > 0.5f && d < 0.58f) c = Over(c, new Color(0.8f, 0.52f, 0.08f, 0.9f));                // a ring pressed into it
        if (d <= 0.74f && Circle(x + 0.3f, y - 0.32f, 0.16f)) c = Over(c, new Color(1f, 1f, 0.9f, 0.6f));   // a shine, top left
        return c;
    }

    // White inside a box with round corners (the whole picture), clear outside the corners.
    static Color BoxPaint(float x, float y)
    {
        float r = BoxRadius / (BoxSize * 0.5f);   // the corners' radius, on the -1 to 1 scale
        float cornerX = Mathf.Abs(x) - (1f - r);
        float cornerY = Mathf.Abs(y) - (1f - r);
        bool inside = cornerX <= 0f || cornerY <= 0f || cornerX * cornerX + cornerY * cornerY <= r * r;
        return inside ? Color.white : Color.clear;
    }

    static Color HeartPaint(float x, float y) =>
        Icon(x, y, InHeart, new Color(0.75f, 0.1f, 0.16f), new Color(1f, 0.45f, 0.48f), new Color(0.42f, 0.04f, 0.08f));

    static Color BoltPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, Bolt), new Color(1f, 0.6f, 0.05f), new Color(1f, 0.94f, 0.4f), new Color(0.55f, 0.3f, 0.02f));

    static Color SpeedPaint(float x, float y) =>
        Icon(x, y, InChevrons, new Color(0.1f, 0.5f, 0.9f), new Color(0.55f, 0.95f, 1f), new Color(0.04f, 0.22f, 0.45f));

    static Color FlamePaint(float x, float y)
    {
        Color c = Icon(x, y, InFlame, new Color(0.85f, 0.15f, 0.05f), new Color(1f, 0.58f, 0.1f), new Color(0.45f, 0.06f, 0.02f));
        if (InFlameCore(x, y)) c = Over(c, Color.Lerp(new Color(1f, 0.78f, 0.2f), new Color(1f, 0.98f, 0.75f), (y + 1f) * 0.5f));   // the hot middle
        return c;
    }

    // A drumstick: a bone behind a piece of meat.
    static Color HungerPaint(float x, float y)
    {
        Color bone = Icon(x, y, InBone, new Color(0.8f, 0.76f, 0.68f), new Color(1f, 0.98f, 0.92f), new Color(0.45f, 0.4f, 0.32f));
        Color meat = Icon(x, y, InMeat, new Color(0.6f, 0.28f, 0.1f), new Color(0.92f, 0.55f, 0.27f), new Color(0.35f, 0.14f, 0.04f));
        return Over(bone, meat);
    }

    static Color JumpPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, UpArrow), new Color(0.18f, 0.58f, 0.28f), new Color(0.55f, 0.92f, 0.5f), new Color(0.07f, 0.28f, 0.11f));

    // A steel breastplate (the same shape as the Gear page's chestplate outline).
    static Color ArmorPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, Breastplate), new Color(0.42f, 0.47f, 0.55f), new Color(0.86f, 0.89f, 0.94f), new Color(0.18f, 0.2f, 0.26f));

    // A shape with a shadow (down right), a darker edge around it, and a fill that is lighter at the top.
    static Color Icon(float x, float y, System.Func<float, float, bool> shape, Color bottom, Color top, Color edge)
    {
        Color c = Color.clear;
        if (shape(x - 0.06f, y + 0.08f)) c = Over(c, Shadow);
        if (Grown(shape, x, y, 0.06f)) c = Over(c, edge);
        if (shape(x, y)) c = Over(c, Color.Lerp(bottom, top, (y + 1f) * 0.5f));
        return c;
    }

    // ----- Shapes: true = the point is inside -----

    // A circle of radius r, where dx / dy = how far the point is from its middle.
    static bool Circle(float dx, float dy, float r) => dx * dx + dy * dy <= r * r;

    // The heart curve (x² + y² - 1)³ - x² y³ <= 0, sized to fit the picture.
    static bool InHeart(float x, float y)
    {
        float hx = x * 1.4f;
        float hy = y * 1.4f + 0.1f;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy <= 0f;
    }

    static bool InChevrons(float x, float y) => InPolygon(x, y, ChevronBack) || InPolygon(x, y, ChevronFront);

    // The armor outlines: a helmet with a T-shaped visor, two round shoulder plates, a breastplate, a glove, leggings, a boot.
    static bool InArmor(ArmorSlot slot, float x, float y)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet:
                bool shell = (Circle(x, y + 0.02f, 0.66f) && y > -0.12f) || (Mathf.Abs(x) <= 0.66f && y <= -0.12f && y >= -0.7f);
                bool eyes = y > -0.26f && y < -0.1f && Mathf.Abs(x) < 0.48f;   // the eye slit
                bool nose = Mathf.Abs(x) < 0.09f && y < -0.1f;                  // the slit down the middle
                return shell && !eyes && !nose;
            case ArmorSlot.Shoulders:
                bool plates = (Circle(x + 0.42f, y, 0.44f) || Circle(x - 0.42f, y, 0.44f)) && y > -0.18f;
                return plates && !Circle(x, y - 0.42f, 0.24f) && Mathf.Abs(x) > 0.04f;   // a gap for the neck
            case ArmorSlot.Chest:
                return InPolygon(x, y, Breastplate);
            case ArmorSlot.Gloves:
                bool fingers = Segment(x, y, -0.27f, 0.05f, -0.29f, 0.66f, 0.085f) || Segment(x, y, -0.09f, 0.05f, -0.09f, 0.76f, 0.085f) ||
                               Segment(x, y, 0.09f, 0.05f, 0.1f, 0.74f, 0.085f) || Segment(x, y, 0.27f, 0.05f, 0.3f, 0.6f, 0.08f);
                return InPolygon(x, y, Palm) || InPolygon(x, y, Cuff) || fingers || Segment(x, y, -0.34f, -0.12f, -0.66f, 0.24f, 0.1f);
            case ArmorSlot.Legs:
                return InPolygon(x, y, Trousers);
            case ArmorSlot.Boots:
                return InPolygon(x, y, Boot);
            default:
                return Circle(x, y, 0.6f);
        }
    }
    static bool InMeat(float x, float y) => Circle(x - 0.18f, y - 0.2f, 0.52f) || Circle(x + 0.06f, y + 0.06f, 0.32f);
    static bool InBone(float x, float y) =>
        Segment(x, y, -0.1f, -0.1f, -0.6f, -0.6f, 0.08f) || Circle(x + 0.72f, y + 0.5f, 0.13f) || Circle(x + 0.5f, y + 0.72f, 0.13f);

    // Within r of the straight line from (ax, ay) to (bx, by): a thick line with round ends.
    static bool Segment(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));   // the nearest point on the line
        float px = ax + dx * t - x, py = ay + dy * t - y;
        return px * px + py * py <= r * r;
    }
    static bool InFlame(float x, float y) => Circle(x, y + 0.32f, 0.55f) || InPolygon(x, y, FlameTip);
    static bool InFlameCore(float x, float y) => Circle(x, y + 0.42f, 0.3f) || InPolygon(x, y, FlameCoreTip);

    // One thick ">" pointing right, its middle at x = cx.
    static Vector2[] Chevron(float cx) => new[]
    {
        new Vector2(cx - 0.32f, 0.62f), new Vector2(cx - 0.06f, 0.62f), new Vector2(cx + 0.3f, 0f),
        new Vector2(cx - 0.06f, -0.62f), new Vector2(cx - 0.32f, -0.62f), new Vector2(cx + 0.04f, 0f),
    };

    // Inside a polygon (corners in order): a line from the point to the right crosses its edges an odd number of times.
    static bool InPolygon(float x, float y, Vector2[] p)
    {
        bool inside = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                inside = !inside;
        }
        return inside;
    }

    // Inside the shape made a little bigger (by d): this point, or one of 8 points around it, is inside.
    static bool Grown(System.Func<float, float, bool> shape, float x, float y, float d)
    {
        if (shape(x, y)) return true;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            if (shape(x + Mathf.Cos(angle) * d, y + Mathf.Sin(angle) * d)) return true;
        }
        return false;
    }

    // ----- Drawing and saving -----

    // Puts "top" over "bottom" like layers of paint (see-through colors mix).
    static Color Over(Color bottom, Color top)
    {
        float a = top.a + bottom.a * (1f - top.a);
        if (a <= 0f) return Color.clear;
        float keep = bottom.a * (1f - top.a);
        return new Color((top.r * top.a + bottom.r * keep) / a, (top.g * top.a + bottom.g * keep) / a,
                         (top.b * top.a + bottom.b * keep) / a, a);
    }

    // A size x size picture. Each pixel is the average of Samples x Samples points of "paint", so edges are smooth.
    static Texture2D Draw(int size, System.Func<float, float, Color> paint)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float step = 1f / Samples;
        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;   // the colors, each counted as much as it is solid
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = (px + (sx + 0.5f) * step) / size * 2f - 1f;
                        float y = (py + (sy + 0.5f) * step) / size * 2f - 1f;
                        Color c = paint(x, y);
                        r += c.r * c.a;
                        g += c.g * c.a;
                        b += c.b * c.a;
                        a += c.a;
                    }
                }
                pixels[py * size + px] = a > 0f ? new Color(r / a, g / a, b / a, a / (Samples * Samples)) : Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Saves a picture as a PNG sprite, only if the file isn't there yet. border = the 9-slice edges in pixels (0 = none).
    static Sprite Save(string path, System.Func<Texture2D> draw, Vector4 border)
    {
        if (!File.Exists(path))
        {
            MakeFolders(FolderOf(path));
            Texture2D texture = draw();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;   // sharp edges, no blocky compression
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
