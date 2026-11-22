/*
First Date: 20/10/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Build Game End Screen (it asks first).
              It builds Assets/Prefabs/UI/GameEnd/GameEndCanvas.prefab, the victory page. When the prefab is already
              there it's rebuilt in place, so GameScene's and the new world's (ProceduralGenerateScene) victory pages
              both change and nothing in the scenes needs saving (its VictoryScreen settings, e.g. Days To Win, stay).
              The first time (no prefab yet) it also puts one in the open scene (Ctrl + Z works there; save the scene).
              A game scene open without a GameEndCanvas gets one too.
              The trophy and its light rays are drawn the first time, into Assets/UI/GameEnd (IconArt).
              To change texts, sizes or colors for every future build, edit the numbers in Fields below.
              Needs TMP Essentials (it tells you if they are missing).
              Hand-made steps: Docs/Game End Setup.md
Purpose: Builds the victory page in one click:
           GameEndCanvas > BackgroundPanel > TrophyRays, TrophyGlow, TrophyIcon  a gold trophy, middle top
                                            TitleText                          "YOU SURVIVED!"
                                            DaysText                           "5 DAYS AND NIGHTS", when they won
                                            CoinsBox > CoinIcon, CoinsText     coins left, earned and spent
                                            StatsToggle                        MINIMISE / SHOW ALL STATS
                                            Status (FitToText) > StatusPanel, StatusText   the short numbers
                                            StatsPanel                         all stats, like the inventory's Info
                                                                               tab: THIS LIFE, STATUS and PERKS
                                            PlayAgainButton, MainMenuButton
         The stats panel and the toggle are the death page's (DeathScreenBuilder.MakeStatsPanel / MakeStatsToggle), so
         both pages look alike. The page follows the screen's height (Canvas Scaler Match 1), so it fits top to bottom
         on any screen shape.
Requirement: GameEndUIBuilder.cs x-> UIBuildHelper.cs, VictoryScreen.cs, FitToText.cs, DeathScreenBuilder.cs, IconArt.cs, ButtonFX.cs, MenuOrder.cs
             GameEndUIBuilder.cs o-> PlayerHealth.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class GameEndUIBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Game End Screen";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string CanvasPath => FindPrefabPath("Assets/Prefabs/UI/GameEnd/GameEndCanvas.prefab");
    const string CanvasName = "GameEndCanvas";
    const int SortingOrder = 30;   // above the HUD (0), menus (10) and the death screen: nothing covers it

    // Where things go, in the canvas's units (1920 x 1080; the page follows the screen's height), up from the middle of
    // the screen (the buttons: up from the bottom).
    const float TrophyY = 425f;
    const float TrophySize = 150f;
    const float TitleY = 308f;
    const float DaysY = 238f;
    const float HeaderY = 166f;       // the coins and the toggle, side by side over the stats
    const float HeaderHeight = 52f;
    const float ContentTop = 124f;    // the stats panel and the short numbers hang down from here
    const float PanelScale = 0.76f;   // the stats panel (1530 x 700) drawn at 76 %: 1163 x 532, down to just over the buttons
    const float PanelWidth = 1530f;   // DeathScreenBuilder's panel at full size
    const float ContentWidth = PanelWidth * PanelScale;
    static readonly Vector2 ToggleSize = new Vector2(330f, 52f);
    const float ButtonY = 70f;        // the buttons' middles, up from the bottom of the screen
    const float ButtonsApart = 340f;  // from one button's middle to the other's
    static readonly Vector2 ButtonSize = new Vector2(300f, 80f);

    // The look.
    static readonly Color BackgroundColor = new Color(0.02f, 0.06f, 0.03f, 0.92f);   // almost black, a hint of green
    static readonly Color RaysColor = new Color(1f, 0.82f, 0.3f, 0.35f);             // soft gold light behind the trophy
    static readonly Color GlowColor = new Color(1f, 0.78f, 0.25f, 0.55f);
    const string Title = "YOU SURVIVED!";
    const float TitleSize = 68f;
    static readonly Color TitleColor = new Color(1f, 0.85f, 0.2f);                    // gold
    const float DaysFontSize = 34f;
    static readonly Color DaysColor = new Color(1f, 0.93f, 0.7f);                     // pale gold
    static readonly Color CoinsBoxColor = new Color(1f, 0.84f, 0.35f, 0.16f);         // a faint gold pill
    const float CoinsFontSize = 26f;
    const float StatusFontSize = 32f;
    const float StatusPadding = 15f;
    static readonly Color StatusPanelColor = new Color(1f, 1f, 1f, 0.392f);         // see-through white, like the death page
    const string NoPerksText = "You won without a single perk!";
    static readonly Color ButtonColor = Color.white;
    const float ButtonFontSize = 34f;
    static readonly Color ButtonTextColor = new Color(0.2f, 0.2f, 0.2f);

    static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Game End Screen", false, MenuOrder.BuildGameEndUI)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;

        string path = CanvasPath;
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        string what = exists
            ? "This rebuilds the victory page, " + path + ", in place: GameScene's and the new world's both change.\n\n"
            : "This builds the victory page, " + path + ", and puts it in the open scene.\n\n";
        if (!EditorUtility.DisplayDialog(ToolName, what +
                "- a gold trophy in the middle, then YOU SURVIVED! and the days\n" +
                "- the coins: how many are left, earned and spent\n" +
                "- all stats like the inventory's Info tab (this life's numbers, health, armor, speeds, perks...), " +
                "with MINIMISE / SHOW ALL STATS\n" +
                "- Play Again and Main Menu buttons\n\n" +
                (exists ? "Changes made by hand to the page are replaced (its VictoryScreen settings stay).\n" : "") +
                "Continue?", exists ? "Rebuild" : "Build", "Cancel"))
            return;

        if (exists) RebuildInPlace(path);
        else BuildNew(path);
    }

    // ===== My methods =====

    // The prefab exists: empty it and build the page again inside it, keeping its top object (and its VictoryScreen
    // settings), so the scenes' copies follow without being touched.
    static void RebuildInPlace(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Fill(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log(ToolName + ": " + path + " rebuilt (both game scenes use it). " + PutInOpenScene(path) +
                  "Test: Play, then GameEndCanvas's VictoryScreen ⋮ menu > Test: Win.");
    }

    // No prefab yet: build the page in the open scene and save it as the prefab.
    static void BuildNew(string path)
    {
        // A GameEndCanvas already in the scene (made by hand): offer to swap it for the built one.
        GameObject old = FindInScene(CanvasName);
        bool replaceOld = old != null && EditorUtility.DisplayDialog(ToolName,
            "The scene already has a " + CanvasName + ". Replace it with the built one?\n" +
            "(Ctrl + Z brings the old one back.)", "Replace it", "Keep both");

        MakeFolders(FolderOf(path));
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);
        Fill(canvas);
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, path, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);

        if (replaceOld)
        {
            canvas.transform.SetSiblingIndex(old.transform.GetSiblingIndex());   // same place in the Hierarchy
            Undo.DestroyObjectImmediate(old);
        }

        MakeEventSystemIfMissing(ToolName);   // the buttons need one to be clicked

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log("Game end screen built: " + path + " (in the scene too). Test: Play, then GameEndCanvas's " +
                  "VictoryScreen ⋮ menu > Test: Win. Save the scene (Ctrl + S).");
    }

    // An open game scene (it has a player) without a GameEndCanvas gets one. Returns what it did, for the Console.
    static string PutInOpenScene(string path)
    {
        if (FindInScene(CanvasName) != null || Object.FindFirstObjectByType<PlayerHealth>() == null) return "";
        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        Undo.RegisterCreatedObjectUndo(placed, ToolName);
        MakeEventSystemIfMissing(ToolName);
        return "The open scene had none, so one was added: save the scene (Ctrl + S). ";
    }

    // Everything inside the canvas, made again from nothing (the canvas's old children are deleted first).
    static void Fill(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        Canvas canvas = Need<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = Need<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;   // follow the height: the page fills it top to bottom, on any screen shape
        Need<GraphicRaycaster>(root);     // the mouse can click the buttons

        // The dark page over the whole screen. It also blocks clicks on anything behind it.
        GameObject background = NewUI("BackgroundPanel", root.transform);
        Stretch(background, 0f, 0f, 0f, 0f);
        background.AddComponent<Image>().color = BackgroundColor;
        Transform page = background.transform;

        MakeTrophy(page);
        MakeTitle(page);
        MakeDays(page);
        MakeCoins(page);
        DeathScreenBuilder.MakeStatsToggle(page, page.childCount, null, new Vector2(ContentWidth / 2f, HeaderY),
                                           new Vector2(1f, 0.5f), ToggleSize);   // its right edge lines up with the panel's
        MakeStatus(page);
        DeathScreenBuilder.MakeStatsPanel(page, page.childCount, ContentTop, PanelScale, false, NoPerksText);
        MakeButton(page, "PlayAgainButton", "PlayAgainText", "Play Again", -ButtonsApart / 2f);
        MakeButton(page, "MainMenuButton", "MainMenuText", "Main Menu", ButtonsApart / 2f);

        VictoryScreen screen = Need<VictoryScreen>(root);   // hides the page at the start, shows it on winning
        SerializedObject so = new SerializedObject(screen);  // the names in quotes must match VictoryScreen's fields
        so.FindProperty("startShowingAll").boolValue = true;
        so.FindProperty("twoColumns").boolValue = true;       // the short numbers' box is wide enough for two columns
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The trophy in the middle at the top, with a soft glow and light rays behind it (VictoryScreen moves them).
    static void MakeTrophy(Transform page)
    {
        Picture(page, "TrophyRays", IconArt.Rays(), RaysColor, TrophySize * 2.2f);
        Picture(page, "TrophyGlow", IconArt.Glow(), GlowColor, TrophySize * 1.9f);
        Picture(page, "TrophyIcon", IconArt.Trophy(), Color.white, TrophySize).preserveAspect = true;
    }

    static Image Picture(Transform page, string objectName, Sprite sprite, Color color, float size)
    {
        GameObject go = NewUI(objectName, page);
        DeathScreenBuilder.Place(go, new Vector2(0f, TrophyY), new Vector2(size, size), Middle);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void MakeTitle(Transform page)
    {
        GameObject title = NewUI("TitleText", page);
        DeathScreenBuilder.Place(title, new Vector2(0f, TitleY), new Vector2(1100f, 90f), Middle);
        TextMeshProUGUI text = AddText(title, Title, TitleSize, TextAlignmentOptions.Center);
        text.color = TitleColor;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
        text.enableWordWrapping = false;
    }

    // "5 DAYS AND NIGHTS" and, smaller, when they won (VictoryScreen writes the real words).
    static void MakeDays(Transform page)
    {
        GameObject days = NewUI("DaysText", page);
        DeathScreenBuilder.Place(days, new Vector2(0f, DaysY), new Vector2(1100f, 72f), Middle);
        TextMeshProUGUI text = AddText(days, "5 DAYS AND NIGHTS\n<size=60%>won on Day 6 at 05:31</size>",
                                       DaysFontSize, TextAlignmentOptions.Center);
        text.color = DaysColor;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3f;
        text.enableWordWrapping = false;
    }

    // A faint gold pill over the stats, on the left: [coin] "45 coins left   earned 120   spent 95". It's as wide as
    // its words (Content Size Fitter), its left edge in line with the stats panel's.
    static void MakeCoins(Transform page)
    {
        GameObject box = NewUI("CoinsBox", page);
        DeathScreenBuilder.Place(box, new Vector2(-ContentWidth / 2f, HeaderY), new Vector2(400f, HeaderHeight), new Vector2(0f, 0.5f));
        DeathScreenBuilder.Rounded(box, CoinsBoxColor);
        HorizontalLayoutGroup row = box.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(14, 24, 6, 6);
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        box.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image coin = NewUI("CoinIcon", box.transform).AddComponent<Image>();
        coin.sprite = IconArt.Coin();
        coin.preserveAspect = true;
        coin.raycastTarget = false;
        LayoutElement coinSize = coin.gameObject.AddComponent<LayoutElement>();
        coinSize.preferredWidth = coinSize.preferredHeight = 38f;

        TextMeshProUGUI words = AddText(NewUI("CoinsText", box.transform), "<b>45</b> coins left    earned 120    spent 95",
                                        CoinsFontSize, TextAlignmentOptions.MidlineLeft);
        words.color = Color.white;
        words.enableWordWrapping = false;
    }

    // Status > StatusPanel (the see-through box) + StatusText (the short numbers, in two columns). FitToText makes the
    // box as tall as the text. Shown after MINIMISE.
    static void MakeStatus(Transform page)
    {
        GameObject status = NewUI("Status", page);
        DeathScreenBuilder.Place(status, new Vector2(0f, ContentTop), new Vector2(ContentWidth, 100f), new Vector2(0.5f, 1f));   // top: grows downward

        GameObject panel = NewUI("StatusPanel", status.transform);
        Stretch(panel, 0f, 0f, 0f, 0f);
        Image panelImage = DeathScreenBuilder.Rounded(panel, StatusPanelColor);

        GameObject textGo = NewUI("StatusText", status.transform);
        Stretch(textGo, StatusPadding, StatusPadding, StatusPadding, StatusPadding);
        TextMeshProUGUI text = AddText(textGo, "Monsters killed: 0<pos=50%>Stamina used: 0", StatusFontSize, TextAlignmentOptions.TopLeft);
        text.color = Color.white;
        text.enableWordWrapping = false;   // a column's line never breaks into the other column

        FitToText fit = status.AddComponent<FitToText>();
        SerializedObject so = new SerializedObject(fit);   // the names in quotes must match the field names in FitToText
        so.FindProperty("padding").floatValue = StatusPadding;
        so.FindProperty("text").objectReferenceValue = text;
        so.FindProperty("panel").objectReferenceValue = panelImage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A white button with round corners at the bottom of the screen, "x" from the middle; gold under the mouse
    // (ButtonFX). VictoryScreen adds its click by itself.
    static void MakeButton(Transform page, string objectName, string labelName, string words, float x)
    {
        GameObject button = NewUI(objectName, page);
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // bottom middle of the screen
        rt.sizeDelta = ButtonSize;
        rt.anchoredPosition = new Vector2(x, ButtonY);

        Image image = DeathScreenBuilder.Rounded(button, ButtonColor);
        image.raycastTarget = true;   // it's clicked
        button.AddComponent<Button>().targetGraphic = image;

        GameObject label = NewUI(labelName, button.transform);
        Stretch(label, 0f, 0f, 0f, 0f);
        TextMeshProUGUI text = AddText(label, words, ButtonFontSize, TextAlignmentOptions.Center);
        text.color = ButtonTextColor;
        text.fontStyle = FontStyles.Bold;

        ButtonFX fx = button.AddComponent<ButtonFX>();
        SerializedObject so = new SerializedObject(fx);   // the names in quotes must match ButtonFX's fields
        so.FindProperty("hoverScale").floatValue = 1.06f;
        so.FindProperty("dimOthers").boolValue = false;   // the rest of the page stays as it is
        so.FindProperty("sparks").intValue = 10;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The component, added if it isn't there yet.
    static T Need<T>(GameObject go) where T : Component
    {
        T found = go.GetComponent<T>();
        return found != null ? found : go.AddComponent<T>();
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
