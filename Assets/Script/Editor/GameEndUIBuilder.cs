/*
First Date: 20/10/2026
Date: 20/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want it in, then menu Tools > UI > Build Game End Screen.
              It makes Assets/Prefabs/UI/GameEnd/GameEndCanvas.prefab and puts it in the open scene, with
              VictoryScreen (shown when the player wins) and FitToText (the stats box grows with the text).
              If the prefab or a GameEndCanvas in the scene already exists, it asks before replacing it.
              To change texts, sizes or colors for every future build, edit the numbers in Fields below.
              Needs TMP Essentials (it tells you if they are missing). Undo (Ctrl + Z) works in the scene.
              Hand-made steps: Docs/Game End Setup.md
Purpose: Builds the victory screen in one click, laid out like the death screen (DeadCanvas):
         GameEndCanvas > BackgroundPanel > TitleText ("YOU SURVIVED!"),
                                          Status (FitToText) > StatusPanel + StatusText (the stats),
                                          PlayAgainButton > PlayAgainText ("Play Again")
Requirement: GameEndUIBuilder.cs x-> UIBuildHelper.cs, VictoryScreen.cs, FitToText.cs, MenuOrder.cs
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

    // The look (1920 x 1080 screen, same layout as DeadCanvas).
    static readonly Color BackgroundColor = new Color(0.02f, 0.06f, 0.03f, 0.92f);   // almost black, a hint of green
    const string Title = "YOU SURVIVED!";
    const float TitleSize = 72f;
    static readonly Color TitleColor = new Color(1f, 0.85f, 0.2f);                   // gold
    const float TitleY = 410f;                                                        // from the middle of the screen

    const float StatusTop = 330f;          // the stats box's top edge, from the middle of the screen
    const float StatusWidth = 1000f;
    const float StatusFontSize = 36f;
    const float StatusPadding = 15f;
    static readonly Color StatusPanelColor = new Color(1f, 1f, 1f, 0.392f);         // see-through white, like DeadCanvas

    const string ButtonText = "Play Again";
    static readonly Vector2 ButtonSize = new Vector2(320f, 100f);
    const float ButtonY = 217f;            // from the bottom of the screen
    const float ButtonFontSize = 36f;
    static readonly Color ButtonTextColor = new Color(0.2f, 0.2f, 0.2f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Game End Screen", false, MenuOrder.BuildGameEndUI)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;

        string path = CanvasPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null &&
            !EditorUtility.DisplayDialog(ToolName, "GameEndCanvas.prefab already exists. Build a new one and replace it?",
                                         "Replace", "Cancel"))
        {
            return;
        }

        // A GameEndCanvas already in the scene: offer to swap it for the built one.
        GameObject old = FindInScene(CanvasName);
        bool replaceOld = old != null && EditorUtility.DisplayDialog(ToolName,
            "The scene already has a " + CanvasName + ". Replace it with the built one?\n" +
            "(Ctrl + Z brings the old one back.)", "Replace it", "Keep both");

        MakeFolders(FolderOf(path));
        GameObject canvas = MakeCanvas();
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, path, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);

        if (replaceOld)
        {
            canvas.transform.SetSiblingIndex(old.transform.GetSiblingIndex());   // same place in the Hierarchy
            Undo.DestroyObjectImmediate(old);
        }

        MakeEventSystemIfMissing(ToolName);   // the button needs one to be clicked

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log("Game end screen built: " + path + " (in the scene too). Test: Play, then GameEndCanvas's " +
                  "VictoryScreen ⋮ menu > Test: Win. Save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;   // same as the other screens: follow the width

        // The dark page over the whole screen. It also blocks clicks on anything behind it.
        GameObject background = NewUI("BackgroundPanel", canvas.transform);
        Stretch(background, 0f, 0f, 0f, 0f);
        background.AddComponent<Image>().color = BackgroundColor;

        MakeTitle(background.transform);
        MakeStatus(background.transform);
        MakeButton(background.transform);

        canvas.AddComponent<VictoryScreen>();   // hides the page at the start, shows it on winning
        return canvas;
    }

    static void MakeTitle(Transform parent)
    {
        GameObject title = NewUI("TitleText", parent);
        RectTransform rt = (RectTransform)title.transform;
        rt.sizeDelta = new Vector2(1000f, 100f);   // anchored to the middle of the screen (the default)
        rt.anchoredPosition = new Vector2(0f, TitleY);
        TextMeshProUGUI text = AddText(title, Title, TitleSize, TextAlignmentOptions.Center);
        text.color = TitleColor;
        text.fontStyle = FontStyles.Bold;
    }

    // Status > StatusPanel (the see-through box) + StatusText (the stats). FitToText makes the box as tall as the text.
    static void MakeStatus(Transform parent)
    {
        GameObject status = NewUI("Status", parent);
        RectTransform rt = (RectTransform)status.transform;
        rt.pivot = new Vector2(0.5f, 1f);          // top: grows downward as lines are added
        rt.sizeDelta = new Vector2(StatusWidth, 100f);
        rt.anchoredPosition = new Vector2(0f, StatusTop);

        GameObject panel = NewUI("StatusPanel", status.transform);
        Stretch(panel, 0f, 0f, 0f, 0f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = StatusPanelColor;
        panelImage.raycastTarget = false;

        GameObject textGo = NewUI("StatusText", status.transform);
        Stretch(textGo, StatusPadding, StatusPadding, StatusPadding, StatusPadding);
        TextMeshProUGUI text = AddText(textGo, "You survived 5 days and nights!", StatusFontSize, TextAlignmentOptions.TopLeft);
        text.color = Color.white;

        FitToText fit = status.AddComponent<FitToText>();
        // The names in quotes must match the field names in FitToText.
        SerializedObject so = new SerializedObject(fit);
        so.FindProperty("padding").floatValue = StatusPadding;
        so.FindProperty("text").objectReferenceValue = text;
        so.FindProperty("panel").objectReferenceValue = panelImage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void MakeButton(Transform parent)
    {
        GameObject button = NewUI("PlayAgainButton", parent);
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // bottom middle of the screen
        rt.sizeDelta = ButtonSize;
        rt.anchoredPosition = new Vector2(0f, ButtonY);

        Image image = AddBox(button);
        button.AddComponent<Button>().targetGraphic = image;   // VictoryScreen adds the click by itself

        GameObject label = NewUI("PlayAgainText", button.transform);
        Stretch(label, 0f, 0f, 0f, 0f);
        TextMeshProUGUI text = AddText(label, ButtonText, ButtonFontSize, TextAlignmentOptions.Center);
        text.color = ButtonTextColor;
        text.fontStyle = FontStyles.Bold;
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
