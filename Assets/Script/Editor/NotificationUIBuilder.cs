/*
First Date: 2/11/2026
Date: 2/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want it in, then menu Tools > UI > Build Notifications.
              It makes Assets/Prefabs/UI/Notifications/NotificationCanvas.prefab and puts it in the open scene,
              everything connected. If the prefab or a NotificationCanvas in the scene already exists, it asks before
              replacing it. Undo (Ctrl + Z) works in the scene.
              To change sizes, positions or colors for every future build, edit the numbers in Fields below.
              Needs TMP Essentials (it tells you if they are missing). Hand-made steps: Docs/Notifications Setup.md
Purpose: Builds the "+3 Wood" / "+5 Coins" messages in one click:
         NotificationCanvas (GainNotifier: turns item and coin gains into messages)
         > Feed (Vertical Layout Group + NotificationFeed): where the messages stack, on the left edge of the
           screen at middle height (GameRules > Notification Side = Right flips it to the right edge)
           > Message (switched off): the look every message is copied from, a see-through dark box
             (Horizontal Layout Group + CanvasGroup) with Icon (Image) + Text (TextMeshPro)
Requirement: NotificationUIBuilder.cs x-> UIBuildHelper.cs, NotificationFeed.cs, GainNotifier.cs, MenuOrder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class NotificationUIBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Notifications";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string CanvasPath => FindPrefabPath("Assets/Prefabs/UI/Notifications/NotificationCanvas.prefab");
    const string CanvasName = "NotificationCanvas";
    const int SortingOrder = 5;   // over the HUD (0), under the menus (10+) and the death / victory pages

    // Where the messages stack (in pixels on a 1920 x 1080 screen): an area on the left edge, at middle height,
    // clear of the bars at the top and the hotbar / build hints at the bottom. Messages start at its bottom.
    static readonly Vector2 FeedPosition = new Vector2(30f, 340f);   // its bottom left corner, from the left / bottom edges
    static readonly Vector2 FeedSize = new Vector2(600f, 380f);
    const float MessageSpacing = 6f;

    // One message: a see-through dark box with a picture and a white text.
    static readonly Color BoxColor = new Color(0f, 0f, 0f, 0.55f);
    const float FontSize = 30f;
    const float IconSize = 40f;
    const float IconGap = 10f;                     // between the picture and the text
    const int PadLeft = 12, PadRight = 18, PadTop = 6, PadBottom = 6;   // inside the box, around its contents

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Notifications", false, MenuOrder.BuildNotifications)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) != null &&
            !EditorUtility.DisplayDialog(ToolName, "NotificationCanvas.prefab already exists. Build a new one and replace it?",
                                         "Replace", "Cancel"))
        {
            return;
        }

        // A NotificationCanvas already in the scene: offer to swap it for the built one.
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
        Debug.Log("Notifications built: " + CanvasPath + " (in the scene too). Save the scene (Ctrl + S). " +
                  "GameRules > Notifications turns them on / off and picks the side.");
    }

    // ===== My methods =====

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);
        Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());   // nothing here is ever clicked
        canvas.AddComponent<GainNotifier>();

        GameObject feed = NewUI("Feed", canvas.transform);
        RectTransform rt = (RectTransform)feed.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;   // bottom left corner (NotificationFeed flips it for Right)
        rt.anchoredPosition = FeedPosition;
        rt.sizeDelta = FeedSize;

        VerticalLayoutGroup column = feed.AddComponent<VerticalLayoutGroup>();
        column.childAlignment = TextAnchor.LowerLeft;   // newest at the bottom, older ones pushed up
        column.spacing = MessageSpacing;
        column.childControlWidth = column.childControlHeight = true;             // each message as big as its text
        column.childForceExpandWidth = column.childForceExpandHeight = false;

        GameObject message = MakeMessage(feed.transform);

        NotificationFeed script = feed.AddComponent<NotificationFeed>();
        // The name in quotes must match the field name in NotificationFeed.
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("template").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();
        return canvas;
    }

    // The look of one message, switched off: NotificationFeed copies it for every new message.
    static GameObject MakeMessage(Transform parent)
    {
        GameObject message = NewUI("Message", parent);
        Image box = AddBox(message);
        box.color = BoxColor;
        box.raycastTarget = false;   // never blocks clicks

        HorizontalLayoutGroup line = message.AddComponent<HorizontalLayoutGroup>();
        line.padding = new RectOffset(PadLeft, PadRight, PadTop, PadBottom);
        line.spacing = IconGap;
        line.childAlignment = TextAnchor.MiddleLeft;
        line.childControlWidth = line.childControlHeight = true;
        line.childForceExpandWidth = line.childForceExpandHeight = false;

        CanvasGroup group = message.AddComponent<CanvasGroup>();   // NotificationFeed fades the whole message with it
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject iconGo = NewUI("Icon", message.transform);
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        LayoutElement iconSize = iconGo.AddComponent<LayoutElement>();
        iconSize.preferredWidth = iconSize.preferredHeight = IconSize;

        GameObject textGo = NewUI("Text", message.transform);
        TextMeshProUGUI text = AddText(textGo, "+3 Wood", FontSize, TextAlignmentOptions.Left);
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;

        message.SetActive(false);   // only its copies are shown
        return message;
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
