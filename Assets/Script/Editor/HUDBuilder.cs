/*
First Date: 18/10/2026
Date: 18/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want it in, then menu Tools > UI > Build HUD.
              It makes Assets/Prefabs/UI/HUD/HUDCanvas.prefab and puts it in the open scene, everything connected.
              If the prefab or a HUDCanvas in the scene already exists, it asks before replacing it
              (use it to bring the HUD back if it was deleted or broken). Undo (Ctrl + Z) works in the scene.
              To change sizes, positions or colors for every future build, edit the numbers in Fields below.
              Needs TMP Essentials (it tells you if they are missing). Hand-made steps: Docs/HUD Setup.md
Purpose: Builds the whole HUD in one click, the same as the one first made by hand, plus the top-right counters:
         HUDCanvas > Crosshair (middle)
                   > HealthBar, HungerBar, StaminaBar (top left; each Background + Fill + Frame, with StatBar)
                   > TopRight > DayText (DayCounter), TimeText (TimeCounter), CoinCounter (Icon + Text)
Requirement: HUDBuilder.cs x-> UIBuildHelper.cs, StatBar.cs, Crosshair.cs, DayCounter.cs, TimeCounter.cs, CoinCounter.cs, MenuOrder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class HUDBuilder
{
    // One stat bar in the top left.
    struct Bar
    {
        public string name;
        public StatBar.Stat stat;
        public float y;        // from the top of the screen, in pixels (1920 x 1080)
        public Color color;    // the Fill color

        public Bar(string name, StatBar.Stat stat, float y, Color color)
        {
            this.name = name;
            this.stat = stat;
            this.y = y;
            this.color = color;
        }
    }

    // ===== Fields =====
    const string ToolName = "Build HUD";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string CanvasPath => FindPrefabPath("Assets/Prefabs/UI/HUD/HUDCanvas.prefab");
    const string CanvasName = "HUDCanvas";
    const int SortingOrder = 0;   // the bottom layer: menus (10+) and the death page draw over it

    // The art (same as the hand-made HUD). Missing files only leave that picture empty.
    const string BarSprite = "Assets/UI/Bar/fill_bar.png";
    const string CrosshairSprite = "Assets/UI/Cursor/CursorPlus.png";

    // The bars, top to bottom (same places and colors as the hand-made HUD).
    static readonly Bar[] Bars =
    {
        new Bar("HealthBar",  StatBar.Stat.Health,  -37f,  new Color(0f, 1f, 0f)),
        new Bar("HungerBar",  StatBar.Stat.Hunger,  -90f,  new Color(1f, 0.735f, 0f)),
        new Bar("StaminaBar", StatBar.Stat.Stamina, -144f, new Color(1f, 1f, 0f)),
    };
    const float BarX = 182f;                                     // middle of the bar, from the left edge
    static readonly Vector2 BarSize = new Vector2(288f, 69f);
    static readonly Color BarBackground = new Color(1f, 0f, 0f); // red behind the fill = what's been lost
    const int BarPixelWidth = 96;                                // StatBar: the fill moves one pixel at a time

    // The top-right box with the day, time and coins.
    static readonly Vector2 TopRightMargin = new Vector2(30f, 25f);   // from the right and top edges
    const float TopRightWidth = 460f;
    const float DayFontSize = 36f;
    const float TimeFontSize = 26f;
    const float CoinFontSize = 34f;
    const float CoinIconSize = 36f;
    static readonly Color DayColor = Color.white;
    static readonly Color TimeColor = new Color(0.85f, 0.85f, 0.85f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build HUD", false, MenuOrder.BuildHUD)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) != null &&
            !EditorUtility.DisplayDialog(ToolName, "HUDCanvas.prefab already exists. Build a new one and replace it?",
                                         "Replace", "Cancel"))
        {
            return;
        }

        // A HUDCanvas already in the scene: offer to swap it for the built one.
        GameObject old = FindInScene(CanvasName);
        bool replaceOld = old != null && EditorUtility.DisplayDialog(ToolName,
            "The scene already has a " + CanvasName + ". Replace it with the built one?\n" +
            "(Ctrl + Z brings the old one back.)", "Replace it", "Keep both");

        MakeFolders(FolderOf(CanvasPath));
        GameObject canvas = MakeCanvas();
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, CanvasPath, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);

        if (replaceOld)
        {
            canvas.transform.SetSiblingIndex(old.transform.GetSiblingIndex());   // same place in the Hierarchy
            Undo.DestroyObjectImmediate(old);
        }

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log("HUD built: " + CanvasPath + " (in the scene too). Save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;   // same as the hand-made HUD: follow the width

        MakeCrosshair(canvas.transform);
        foreach (Bar bar in Bars) MakeBar(bar, canvas.transform);
        MakeTopRight(canvas.transform);
        return canvas;
    }

    // The aiming mark in the middle of the screen, with its colors (red = nothing, green = an item to grab).
    static void MakeCrosshair(Transform parent)
    {
        GameObject go = NewUI("Crosshair", parent);
        ((RectTransform)go.transform).sizeDelta = new Vector2(32f, 32f);   // anchored to the middle (the default)

        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite(CrosshairSprite);
        image.raycastTarget = false;   // never blocks clicks

        Crosshair crosshair = go.AddComponent<Crosshair>();
        // The names in quotes must match the field names in Crosshair.
        SerializedObject so = new SerializedObject(crosshair);
        so.FindProperty("normalColor").colorValue = new Color(1f, 0f, 0f);
        so.FindProperty("targetColor").colorValue = new Color(0.137f, 1f, 0.016f);
        so.FindProperty("autoColor").colorValue = new Color(0f, 1f, 0f);
        so.FindProperty("autoRange").floatValue = 3f;
        so.FindProperty("maxDist").floatValue = 30f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // One bar: red Background, colored Fill on top (StatBar fills it), and an empty Frame slot for border art.
    static void MakeBar(Bar bar, Transform parent)
    {
        GameObject go = NewUI(bar.name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // top left corner
        rt.sizeDelta = BarSize;
        rt.anchoredPosition = new Vector2(BarX, bar.y);

        Sprite sprite = LoadSprite(BarSprite);

        Image background = AddImage(NewUI("Background", go.transform), sprite, BarBackground);
        Stretch(background.gameObject, 0f, 0f, 0f, 0f);

        Image fill = AddImage(NewUI("Fill", go.transform), sprite, bar.color);
        Stretch(fill.gameObject, 0f, 0f, 0f, 0f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;

        // Frame: switched off until it has a picture (drag a border sprite in, then tick the Image on).
        Image frame = AddImage(NewUI("Frame", go.transform), null, Color.white);
        Stretch(frame.gameObject, 0f, 0f, 0f, 0f);
        frame.enabled = false;

        StatBar statBar = go.AddComponent<StatBar>();
        // The names in quotes must match the field names in StatBar.
        SerializedObject so = new SerializedObject(statBar);
        so.FindProperty("stat").enumValueIndex = (int)bar.stat;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("pixelWidth").intValue = BarPixelWidth;
        so.FindProperty("color").colorValue = bar.color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The box in the top right: day, time, coins, stacked and lined up on the right.
    static void MakeTopRight(Transform parent)
    {
        GameObject box = NewUI("TopRight", parent);
        RectTransform rt = (RectTransform)box.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);   // top right corner, grows downward
        rt.anchoredPosition = new Vector2(-TopRightMargin.x, -TopRightMargin.y);
        rt.sizeDelta = new Vector2(TopRightWidth, 0f);

        VerticalLayoutGroup column = box.AddComponent<VerticalLayoutGroup>();
        column.childAlignment = TextAnchor.UpperRight;
        column.spacing = 4f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;    // every row as wide as the box, text lined up on the right
        column.childForceExpandHeight = false;
        box.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI day = AddRowText(box.transform, "DayText", "DAY 1 - Morning", DayFontSize, DayColor);
        day.fontStyle = FontStyles.Bold;
        day.gameObject.AddComponent<DayCounter>();

        TextMeshProUGUI time = AddRowText(box.transform, "TimeText", "07:12 - Night in 4:30", TimeFontSize, TimeColor);
        time.gameObject.AddComponent<TimeCounter>();

        MakeCoinCounter(box.transform);
    }

    // CoinCounter > Icon + Text, side by side, lined up on the right.
    static void MakeCoinCounter(Transform parent)
    {
        GameObject row = NewUI("CoinCounter", parent);
        row.AddComponent<LayoutElement>().preferredHeight = CoinIconSize + 6f;

        HorizontalLayoutGroup line = row.AddComponent<HorizontalLayoutGroup>();
        line.childAlignment = TextAnchor.MiddleRight;
        line.spacing = 8f;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = false;

        GameObject iconGo = NewUI("Icon", row.transform);
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        LayoutElement iconSize = iconGo.AddComponent<LayoutElement>();
        iconSize.preferredWidth = iconSize.preferredHeight = CoinIconSize;

        GameObject textGo = NewUI("Text", row.transform);
        TextMeshProUGUI text = AddText(textGo, "0", CoinFontSize, TextAlignmentOptions.Right);
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;

        CoinCounter counter = row.AddComponent<CoinCounter>();
        // The names in quotes must match the field names in CoinCounter.
        SerializedObject so = new SerializedObject(counter);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("text").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A one-line text row, lined up on the right.
    static TextMeshProUGUI AddRowText(Transform parent, string name, string sample, float size, Color color)
    {
        GameObject go = NewUI(name, parent);
        TextMeshProUGUI text = AddText(go, sample, size, TextAlignmentOptions.Right);
        text.color = color;
        text.enableWordWrapping = false;
        go.AddComponent<LayoutElement>().preferredHeight = size + 8f;
        return text;
    }

    static Image AddImage(GameObject go, Sprite sprite, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;   // the HUD never blocks clicks
        return image;
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning(ToolName + ": " + path + " not found, that picture stays empty.");
        return sprite;
    }

    // A root object with this name in the open scene (even if switched off), or null.
    static GameObject FindInScene(string objectName)
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.name == objectName && c.transform.parent == null) return c.gameObject;
        }
        return null;
    }
}
