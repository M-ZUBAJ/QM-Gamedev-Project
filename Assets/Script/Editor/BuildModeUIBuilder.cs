/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want it in, then menu Tools > UI > Build Build Mode UI.
              It makes Assets/Prefabs/UI/Building/BuildMode/BuildModeCanvas.prefab and puts it in the open scene, with
              BuildModeHUD (shown only while placing) and every hint's UIAnimatedImage already filled in.
              A BuildModeCanvas already in the scene (e.g. one made by hand) can be replaced in the same step.
              To change a hint (text, picture, position), edit the Hints table below and run it again.
              Needs TMP Essentials (it tells you if they are missing). Undo (Ctrl + Z) works in the scene.
Purpose: Builds the build mode hints in one click, the same layout as the one first made by hand:
         BuildModeCanvas > LeftClickImage (+ LeftClickText "Build"), RightClickImage ("Cancel"),
         MiddleClickImage ("Auto / Free"), MouseWheelImage ("Rotate"), each an Aseprite mouse animation.
Requirement: BuildModeUIBuilder.cs x-> UIBuildHelper.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class BuildModeUIBuilder
{
    // One hint: a mouse picture with a word above it.
    struct Hint
    {
        public string name;       // object names: name + "Image" and name + "Text"
        public string text;       // the word above the picture
        public Vector2 position;  // from the middle of the screen (1920 x 1080), in pixels
        public string aseprite;   // the animation file
        public float textWidth;   // wide enough for the word

        public Hint(string name, string text, float x, float y, string aseprite, float textWidth = 200f)
        {
            this.name = name;
            this.text = text;
            position = new Vector2(x, y);
            this.aseprite = aseprite;
            this.textWidth = textWidth;
        }
    }

    // ===== Fields =====
    const string ToolName = "Build Build Mode UI";
    // Where a new prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    const string PrefabFolder = "Assets/Prefabs/UI/Building/BuildMode";
    static string CanvasPath => FindPrefabPath(PrefabFolder + "/BuildModeCanvas.prefab");
    const string CanvasName = "BuildModeCanvas";

    // The hints, left to right along the bottom of the screen. Change them here and run the tool again.
    static readonly Hint[] Hints =
    {
        new Hint("LeftClick",   "Build",       -471f, -270f, "Assets/UI/key/MouseLeftClick.aseprite"),
        new Hint("RightClick",  "Cancel",      -171f, -270f, "Assets/UI/key/MouseRightClick.aseprite"),
        new Hint("MiddleClick", "Auto / Free",  156f, -270f, "Assets/UI/key/MouseMiddleClick.aseprite", 250f),
        new Hint("MouseWheel",  "Rotate",       483f, -270f, "Assets/UI/key/MouseScrolling.aseprite"),
    };

    static readonly Vector2 ImageSize = new Vector2(100f, 100f);
    const float TextAbove = 79f;       // how far above the picture's middle the word sits
    const float TextHeight = 50f;
    const float FontSize = 24f;
    const int SortingOrder = 0;        // same layer as the HUD (the build menu uses 10, the held item 100)

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Build Mode UI", false, MenuOrder.BuildBuildModeUI)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) != null &&
            !EditorUtility.DisplayDialog(ToolName, "BuildModeCanvas.prefab already exists. Build a new one and replace it?",
                                         "Replace", "Cancel"))
        {
            return;
        }

        // A BuildModeCanvas already in the scene (e.g. made by hand): offer to swap it for the built one.
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
        Debug.Log("Build mode hints built: " + CanvasPath + " (in the scene too). " +
                  "They show while placing a building. Save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);   // scales with the screen (1920 x 1080 reference)
        canvas.AddComponent<BuildModeHUD>();                         // shown only while placing

        foreach (Hint hint in Hints) MakeHint(hint, canvas.transform);
        return canvas;
    }

    // A mouse picture (animated) with its word above it.
    static void MakeHint(Hint hint, Transform parent)
    {
        GameObject image = NewUI(hint.name + "Image", parent);
        RectTransform rt = (RectTransform)image.transform;
        rt.sizeDelta = ImageSize;              // anchored to the middle of the screen (the default)
        rt.anchoredPosition = hint.position;

        Image img = image.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;             // hints never block clicks

        UIAnimatedImage anim = image.AddComponent<UIAnimatedImage>();
        Object file = AssetDatabase.LoadMainAssetAtPath(hint.aseprite);
        if (file == null) Debug.LogWarning(ToolName + ": " + hint.aseprite + " not found, " + hint.name + " has no picture.");

        // The names in quotes must match the field names in UIAnimatedImage.
        SerializedObject so = new SerializedObject(anim);
        so.FindProperty("animated").boolValue = file != null;
        so.FindProperty("aseprite").objectReferenceValue = file;
        so.ApplyModifiedPropertiesWithoutUndo();
        anim.EditorRefresh();                  // read the frames now, so the first one shows straight away

        GameObject text = NewUI(hint.name + "Text", image.transform);
        RectTransform textRt = (RectTransform)text.transform;
        textRt.sizeDelta = new Vector2(hint.textWidth, TextHeight);
        textRt.anchoredPosition = new Vector2(0f, TextAbove);
        AddText(text, hint.text, FontSize, TextAlignmentOptions.Center);
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
