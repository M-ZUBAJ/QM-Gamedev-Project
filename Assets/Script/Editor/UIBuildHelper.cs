/*
First Date: 7/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. The UI builder tools (Tools > UI > ...) use it.
Purpose: The small building blocks every UI builder tool needs, kept in one place:
         make a UI object, stretch it, give it a box or a text, check TextMeshPro, add an EventSystem,
         and put a script on the player.
         A builder writes "using static UIBuildHelper;" and can then call e.g. NewUI(...) directly.
Requirement: UIBuildHelper.cs x-> PlayerInventory.cs
             UIBuildHelper.cs <-x BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, GameEndUIBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIBuildHelper
{
    // ===== Fields =====
    public static readonly Color TextColor = Color.black;
    public const int UILayer = 5;   // Unity's built-in "UI" layer

    // ===== My methods =====

    // True if TextMeshPro is ready. If not, tells the user how to fix it (text would be invisible).
    public static bool CheckTMP(string toolName)
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

        EditorUtility.DisplayDialog(toolName,
            "TextMeshPro isn't set up yet.\n\nMenu Window > TextMeshPro > Import TMP Essential Resources, " +
            "click Import, then run this again.", "OK");
        return false;
    }

    // Where a tool's prefab is: the usual path if it's there, otherwise wherever a prefab with that file name is
    // (so moving or sorting prefabs into folders never breaks a tool). Not found anywhere = the usual path.
    public static string FindPrefabPath(string defaultPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(defaultPath) != null) return defaultPath;

        string fileName = System.IO.Path.GetFileNameWithoutExtension(defaultPath);
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == fileName) return path;   // exact name, not "XYZ (1)"
        }
        return defaultPath;
    }

    // The folder part of a path: "Assets/Prefabs/UI/X.prefab" -> "Assets/Prefabs/UI".
    public static string FolderOf(string path)
    {
        return System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
    }

    // Makes every missing folder in a path like "Assets/Prefabs/UI/Building".
    public static void MakeFolders(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];   // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // A new empty UI object (with a Rect Transform) on the UI layer.
    public static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    // Fills the parent, leaving these gaps (like the Left / Top / Right / Bottom fields in the Inspector).
    public static void Stretch(GameObject go, float left, float top, float right, float bottom)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    // Stuck to the top of the parent, full width minus side gaps, this tall, this far down from the top.
    public static void TopBand(GameObject go, float fromTop, float height, float sides)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(sides, -fromTop - height);
        rt.offsetMax = new Vector2(-sides, -fromTop);
    }

    // A white box with a thin border (Unity's built-in "UISprite"). Swap the sprite for your own art later.
    public static Image AddBox(GameObject go)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;   // sliced = the border stays thin whatever the size
        return image;
    }

    public static TextMeshProUGUI AddText(GameObject go, string text, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = TextColor;
        tmp.raycastTarget = false;   // text never blocks clicks
        return tmp;
    }

    // A screen-wide canvas that looks the same on every screen size. Higher sortingOrder = drawn on top.
    public static GameObject NewCanvas(string name, int sortingOrder)
    {
        GameObject canvas = NewUI(name, null);
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();   // lets the mouse click this canvas
        return canvas;
    }

    // UI clicks need one EventSystem in the scene.
    public static void MakeEventSystemIfMissing(string undoName = "Build UI")
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, undoName);
    }

    // Adds script T to the player (the object with PlayerInventory) in the open scene, if it isn't there yet.
    public static void AddToPlayer<T>() where T : Component
    {
        PlayerInventory inv = Object.FindFirstObjectByType<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogWarning("No PlayerInventory in the open scene. Add " + typeof(T).Name + " to the player yourself.");
            return;
        }
        if (inv.GetComponent<T>() == null) Undo.AddComponent<T>(inv.gameObject);
    }
}
