/*
First Date: 7/10/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want the build menu in, then menu Tools > UI > Build Build Menu UI.
              It makes Assets/Prefabs/UI/Building/BuildMenu/BuildingButton.prefab and BuildMenuCanvas.prefab,
              puts BuildMenuCanvas in the open scene (and an EventSystem if there is none), all connected,
              adds BlueprintPlacer to the player, and lists every Building Data in the project.
              Made a new Building Data later? Tools > Building > Update Build Menu List adds it to the menu.
              Run again when BuildMenuCanvas.prefab exists: it's rebuilt in the same file (every scene that has it
              follows, your building list is kept), nothing new is put in the scene.
              Tools > UI > Add Build Mode Scripts: BuildModeHUD on BuildModeCanvas, UIAnimatedImage on each Image under it.
              Needs TMP Essentials (it tells you if they are missing).
              Undo (Ctrl + Z) removes what it put in the scene; the prefab files stay.
Purpose: Builds the build menu from Docs/Build Menu Setup.md in one click:
         BuildMenuCanvas > BuildWindow > Title, Tabs (All, Building, Traps: one per Build Category, 23/11/2026),
         List (a grid the building buttons go in), Info (the box shown when the mouse rests on a building: InfoTitle,
         InfoText, InfoStats; switched off), Hint.
         An existing BuildingButton prefab is reused (your changes to it are kept), so only BuildMenuCanvas is rebuilt.
Requirement: BuildMenuUIBuilder.cs x-> UIBuildHelper.cs, BuildMenu.cs, BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs
             BuildMenuUIBuilder.cs <-x BuildMenuAutoList.cs
*/

using System.Collections.Generic;
using System.Linq;   // Where, Any
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, Stretch, AddBox, AddText... can be called directly

public static class BuildMenuUIBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Build Menu UI";
    // Where new prefabs go. If you move them, the tools still find them by name (FindPrefabPath).
    const string PrefabFolder = "Assets/Prefabs/UI/Building/BuildMenu";
    static string ButtonPath => FindPrefabPath(PrefabFolder + "/BuildingButton.prefab");
    static string CanvasPath => FindPrefabPath(PrefabFolder + "/BuildMenuCanvas.prefab");

    // Sizes. 4 columns x 2 rows of buttons fit in the window (more than 8 buildings would need scrolling).
    static readonly Vector2 WindowSize = new Vector2(1200f, 860f);   // width, height of BuildWindow (room for the tabs)
    static readonly Vector2 ButtonSize = new Vector2(250f, 300f);    // one building button
    const int Columns = 4;
    const int SortingOrder = 10;   // drawn above the HUD and inventory (the held item uses 100)
    const float TabHeight = 46f;
    const float TabWidth = 170f;
    const float InfoWidth = 400f;  // the info box (its height fits its words)

    // ===== Menu (Unity calls these) =====

    [MenuItem("Tools/UI/Build Build Menu UI", false, MenuOrder.BuildBuildMenuUI)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;

        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) != null;
        if (exists && !EditorUtility.DisplayDialog(ToolName,
                "BuildMenuCanvas.prefab already exists. Rebuild it in the same file?\n\n" +
                "Every scene that has it follows, and your building list is kept. " +
                "(The BuildingButton prefab is kept as it is.)", "Rebuild", "Cancel"))
        {
            return;
        }

        MakeFolders(FolderOf(CanvasPath));
        MakeFolders(FolderOf(ButtonPath));

        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
        if (buttonPrefab == null) buttonPrefab = MakeButtonPrefab();

        if (exists)
        {
            RebuildInPlace(buttonPrefab.GetComponent<BuildingButton>());
            return;
        }

        GameObject canvas = MakeCanvas(buttonPrefab.GetComponent<BuildingButton>(), out int buildingCount);
        GameObject canvasPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, CanvasPath, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);

        MakeEventSystemIfMissing(ToolName);
        AddToPlayer<BlueprintPlacer>();   // the menu starts placing through it

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(canvasPrefab);   // flashes it in the Project window
        Debug.Log("Build menu built: " + CanvasPath + " (in the scene too) with " + buildingCount + " building(s). " +
                  "Press Play and B to test." +
                  (buildingCount == 0 ? " No Building Data yet: see Docs/Build Menu Setup.md to make one." : ""));
    }

    // Puts every Building Data in the project into the build menu (keeps the order of the ones already there).
    // This also happens by itself (BuildMenuAutoList) whenever a Building Data is made, deleted or moved.
    [MenuItem("Tools/Building/Update Build Menu List", false, MenuOrder.UpdateBuildMenuList)]
    static void UpdateList()
    {
        UpdateListNow(true);
    }

    // fromMenu = false: no pop-ups and no Console line when nothing changed (used by the automatic update).
    public static void UpdateListNow(bool fromMenu)
    {
        string path = CanvasPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            if (fromMenu)
            {
                EditorUtility.DisplayDialog("Update Build Menu List",
                    "There is no build menu yet. Use Tools > UI > Build Build Menu UI first.", "OK");
            }
            return;
        }

        // Open the prefab file, change it, save it. Every copy in a scene follows.
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        BuildMenu menu = root.GetComponent<BuildMenu>();
        if (menu == null)
        {
            Debug.LogError("Update Build Menu List: " + path + " has no BuildMenu script.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        int count = FillBuildings(menu, out bool changed);
        if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);   // only save when the list really changed
        PrefabUtility.UnloadPrefabContents(root);

        if (changed || fromMenu) Debug.Log("Build menu now lists " + count + " building(s).");
    }

    // Puts BuildModeHUD on BuildModeCanvas (shown only while placing) and UIAnimatedImage on every Image under it.
    [MenuItem("Tools/UI/Add Build Mode Scripts", false, MenuOrder.AddBuildModeScripts)]
    static void AddBuildModeScripts()
    {
        // The selected canvas, or the one named BuildModeCanvas in the open scene (even if switched off).
        GameObject canvas = Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Canvas>() != null
            ? Selection.activeGameObject
            : null;
        if (canvas == null)
        {
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.name == "BuildModeCanvas") { canvas = c.gameObject; break; }
            }
        }
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Add Build Mode Scripts",
                "No canvas named BuildModeCanvas in the open scene. Select your build mode canvas and try again.", "OK");
            return;
        }

        if (canvas.GetComponent<BuildModeHUD>() == null) Undo.AddComponent<BuildModeHUD>(canvas);

        int added = 0;
        foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<UIAnimatedImage>() != null) continue;

            UIAnimatedImage anim = Undo.AddComponent<UIAnimatedImage>(image.gameObject);
            SerializedObject so = new SerializedObject(anim);   // "staticImage" must match the field name in UIAnimatedImage
            so.FindProperty("staticImage").objectReferenceValue = image.sprite;   // keep the picture it already shows
            so.ApplyModifiedProperties();
            added++;
        }

        Selection.activeGameObject = canvas;
        Debug.Log("Build mode scripts added: BuildModeHUD on " + canvas.name + ", UIAnimatedImage on " + added +
                  " image(s). Set each image's Static Image or Aseprite, then save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    // One button: picture, name, and the recipe lines (made from the hidden Ingredient template).
    static GameObject MakeButtonPrefab()
    {
        GameObject root = NewUI("BuildingButton", null);
        ((RectTransform)root.transform).sizeDelta = ButtonSize;
        Image box = AddBox(root);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = box;   // greys out when the player can't afford it

        GameObject icon = NewUI("Icon", root.transform);
        TopBand(icon, 12f, 120f, 12f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;   // turned on when there is a picture

        GameObject title = NewUI("Name", root.transform);
        TopBand(title, 135f, 40f, 8f);
        AddText(title, "Building", 26f, TextAlignmentOptions.Center);

        // The recipe: lines stacked from the top.
        GameObject recipe = NewUI("Recipe", root.transform);
        Stretch(recipe, 12f, 180f, 12f, 10f);
        VerticalLayoutGroup lines = recipe.AddComponent<VerticalLayoutGroup>();
        lines.spacing = 4f;
        lines.childAlignment = TextAnchor.UpperLeft;
        lines.childControlWidth = true;          // each line is as wide as the recipe area
        lines.childControlHeight = false;        // and keeps its own height
        lines.childForceExpandWidth = true;
        lines.childForceExpandHeight = false;

        // One recipe line: a small picture and "Stone  2/3". Hidden: BuildingButton copies it per material.
        GameObject ingredient = NewUI("Ingredient", recipe.transform);
        ((RectTransform)ingredient.transform).sizeDelta = new Vector2(0f, 30f);

        GameObject ingIcon = NewUI("Icon", ingredient.transform);
        RectTransform ingIconRt = (RectTransform)ingIcon.transform;
        ingIconRt.anchorMin = new Vector2(0f, 0f);   // stuck to the left, full height
        ingIconRt.anchorMax = new Vector2(0f, 1f);
        ingIconRt.pivot = new Vector2(0f, 0.5f);
        ingIconRt.sizeDelta = new Vector2(30f, 0f);
        ingIconRt.anchoredPosition = Vector2.zero;
        Image ingIconImage = ingIcon.AddComponent<Image>();
        ingIconImage.preserveAspect = true;
        ingIconImage.raycastTarget = false;

        GameObject ingCount = NewUI("Count", ingredient.transform);
        Stretch(ingCount, 38f, 0f, 0f, 0f);
        AddText(ingCount, "Stone  0/1", 20f, TextAlignmentOptions.MidlineLeft);

        ingredient.SetActive(false);

        root.AddComponent<BuildingButton>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPath);
        Object.DestroyImmediate(root);   // the prefab file is saved, the temporary object isn't needed
        return prefab;
    }

    static GameObject MakeCanvas(BuildingButton buttonPrefab, out int buildingCount)
    {
        GameObject canvas = NewCanvas("BuildMenuCanvas", SortingOrder);
        BuildMenu menu = canvas.AddComponent<BuildMenu>();
        FillWindow(canvas, menu, buttonPrefab);
        buildingCount = FillBuildings(menu, out _);
        return canvas;
    }

    // The existing BuildMenuCanvas.prefab rebuilt in the same file: its window made again (tabs, info box...), its
    // BuildMenu and building list kept. Every scene's copy follows the file.
    static void RebuildInPlace(BuildingButton buttonPrefab)
    {
        string path = CanvasPath;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            BuildMenu menu = root.GetComponent<BuildMenu>();
            if (menu == null) menu = root.AddComponent<BuildMenu>();
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            FillWindow(root, menu, buttonPrefab);
            int count = FillBuildings(menu, out _);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("Build menu rebuilt in " + path + " (every scene that has it follows) with " + count +
                      " building(s), tabs and the info box. Press Play and B to test.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // BuildWindow and everything in it, under the canvas, with BuildMenu's fields connected.
    static void FillWindow(GameObject canvas, BuildMenu menu, BuildingButton buttonPrefab)
    {
        // --- BuildWindow (shown / hidden with B) ---
        GameObject window = NewUI("BuildWindow", canvas.transform);
        RectTransform windowRt = (RectTransform)window.transform;
        windowRt.sizeDelta = WindowSize;
        windowRt.anchoredPosition = new Vector2(0f, 60f);   // a bit up, so the hotbar stays visible
        AddBox(window);

        GameObject title = NewUI("Title", window.transform);
        TopBand(title, 10f, 70f, 20f);
        AddText(title, "Build", 44f, TextAlignmentOptions.Center);

        // --- Tabs: All, then one per Build Category (BuildMenu finds them by name) ---
        GameObject tabs = NewUI("Tabs", window.transform);
        TopBand(tabs, 86f, TabHeight, 20f);
        HorizontalLayoutGroup row = tabs.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        foreach (string tabName in TabNames()) MakeTab(tabs.transform, tabName);

        // --- List: the building buttons are made in here when the game starts ---
        GameObject list = NewUI("List", window.transform);
        Stretch(list, 30f, 146f, 30f, 70f);
        GridLayoutGroup grid = list.AddComponent<GridLayoutGroup>();
        grid.cellSize = ButtonSize;
        grid.spacing = new Vector2(20f, 20f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        // --- Hint: the placing keys (the defaults from KeyBindings) ---
        GameObject hint = NewUI("Hint", window.transform);
        RectTransform hintRt = (RectTransform)hint.transform;
        hintRt.anchorMin = new Vector2(0f, 0f);   // stuck to the bottom, full width
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.offsetMin = new Vector2(20f, 10f);
        hintRt.offsetMax = new Vector2(-20f, 60f);
        AddText(hint, "Rest the mouse on a building to see what it does.   Building:  Left click = build (it carries on)   " +
                      "Right click = back to this menu   B / Esc = stop   Wheel = turn   Middle click = snap on / off",
                20f, TextAlignmentOptions.Center);

        // --- Info: the box shown when the mouse rests on a building (BuildMenu fills and moves it) ---
        MakeInfoBox(window.transform);

        // --- Connect the script (the same as dragging things into its fields) ---
        // The names in quotes must match the field names in BuildMenu.
        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("window").objectReferenceValue = window;
        so.FindProperty("list").objectReferenceValue = list.transform;
        so.FindProperty("buttonPrefab").objectReferenceValue = buttonPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // "All", then every Build Category except Auto (Building, Traps...).
    static List<string> TabNames()
    {
        List<string> names = new List<string> { "All" };
        foreach (string category in System.Enum.GetNames(typeof(BuildCategory)))
        {
            if (category != nameof(BuildCategory.Auto)) names.Add(category);
        }
        return names;
    }

    // One tab: a box button named after its category, with its name as the label.
    static void MakeTab(Transform row, string tabName)
    {
        GameObject tab = NewUI(tabName, row);
        LayoutElement size = tab.AddComponent<LayoutElement>();
        size.preferredWidth = TabWidth;
        size.flexibleWidth = 0f;
        Image box = AddBox(tab);
        Button button = tab.AddComponent<Button>();
        button.targetGraphic = box;
        tab.AddComponent<CanvasGroup>();   // a tab with nothing in it is faded

        GameObject label = NewUI("Label", tab.transform);
        Stretch(label, 0f, 0f, 0f, 0f);
        TextMeshProUGUI text = AddText(label, tabName, 24f, TextAlignmentOptions.Center);
        text.raycastTarget = false;
    }

    // The info box: name (bold), what it does, its numbers (bold blue). Its height fits its words. Nothing in it
    // catches the mouse, so the button under it keeps it. Switched off: BuildMenu shows it.
    static void MakeInfoBox(Transform window)
    {
        GameObject info = NewUI("Info", window);
        RectTransform rt = (RectTransform)info.transform;
        rt.sizeDelta = new Vector2(InfoWidth, 0f);
        rt.pivot = new Vector2(0f, 1f);
        Image box = AddBox(info);
        box.color = new Color(1f, 0.98f, 0.92f, 0.98f);
        box.raycastTarget = false;

        VerticalLayoutGroup lines = info.AddComponent<VerticalLayoutGroup>();
        lines.padding = new RectOffset(16, 16, 12, 14);
        lines.spacing = 6f;
        lines.childAlignment = TextAnchor.UpperLeft;
        lines.childControlWidth = true;
        lines.childControlHeight = true;
        lines.childForceExpandWidth = true;
        lines.childForceExpandHeight = false;
        ContentSizeFitter fit = info.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        InfoLine(info.transform, "InfoTitle", "Iron Trap", 28f, FontStyles.Bold, Color.black);
        InfoLine(info.transform, "InfoText", "What it does.", 21f, FontStyles.Normal, new Color(0.2f, 0.2f, 0.22f));
        InfoLine(info.transform, "InfoStats", "Damage 20", 21f, FontStyles.Bold, new Color(0.1f, 0.25f, 0.55f));
        info.SetActive(false);
    }

    static void InfoLine(Transform parent, string name, string words, float size, FontStyles style, Color color)
    {
        TextMeshProUGUI text = AddText(NewUI(name, parent), words, size, TextAlignmentOptions.TopLeft);
        text.fontStyle = style;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    // Keeps the buildings already listed (in their order), then adds every other Building Data in the project.
    // changed = the list is different from before (so the prefab needs saving).
    static int FillBuildings(BuildMenu menu, out bool changed)
    {
        SerializedObject so = new SerializedObject(menu);
        SerializedProperty prop = so.FindProperty("buildings");   // must match the field name in BuildMenu

        List<Object> before = new List<Object>();
        List<BuildingData> all = new List<BuildingData>();
        for (int i = 0; i < prop.arraySize; i++)
        {
            Object o = prop.GetArrayElementAtIndex(i).objectReferenceValue;
            before.Add(o);
            BuildingData kept = o as BuildingData;
            if (kept != null && !all.Contains(kept)) all.Add(kept);   // drop empty lines and deleted files
        }

        foreach (string guid in AssetDatabase.FindAssets("t:BuildingData"))
        {
            BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && !all.Contains(data)) all.Add(data);
        }

        changed = before.Count != all.Count || before.Where((o, i) => o != all[i]).Any();

        prop.arraySize = all.Count;
        for (int i = 0; i < all.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return all.Count;
    }
}
