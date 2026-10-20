/*
First Date: 7/10/2026
Date: 20/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene you want the inventory in, then menu Tools > UI > Build Inventory UI.
              It makes Assets/Prefabs/UI/Inventory/Slot.prefab and .../InventoryCanvas.prefab (found by name if you
              moved them), puts InventoryCanvas in the open scene (and an EventSystem if there is none), all connected.
              An InventoryCanvas already in the scene can be replaced in the same step (Ctrl + Z brings it back).
              The Crafting page gets Turki's CraftingCanvas prefab (the crafting list), if it's in the project.
              Needs TMP Essentials (it tells you if they are missing).
              Already built the inventory before? Use Tools > UI > Add Inventory Scripts instead: it adds the
              newer pieces (SlotUI, SlotGroup, HeldItem, ItemName texts, the crafting list, HotbarSelector,
              ItemDropper) to what you have, without rebuilding (your changes are kept).
Purpose: Builds the inventory window from Docs/Inventory Setup.md in one click:
         InventoryCanvas > InventoryWindow > TabBar (3 tabs) + Pages (Inventory with 20 slots, Gear, Crafting
         with the crafting list), the HeldItem on the mouse, and the 8-slot Hotbar. Every script is added and filled
         in for you, and HotbarSelector + ItemDropper are added to the player.
         An existing Slot prefab is reused (your changes to it are kept), so only InventoryCanvas is rebuilt.
Requirement: InventoryUIBuilder.cs x-> UIBuildHelper.cs, TabGroup.cs, InventoryWindow.cs, SlotUI.cs, SlotGroup.cs, HeldItemUI.cs, HotbarSelector.cs, ItemDropper.cs, CraftingMenu.cs, MenuOrder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, Stretch, AddBox, AddText... can be called directly

public static class InventoryUIBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Inventory UI";
    // Where new prefabs go. If you move them, the tool still finds them by file name (FindPrefabPath).
    const string PrefabFolder = "Assets/Prefabs/UI/Inventory";
    static string SlotPath => FindPrefabPath(PrefabFolder + "/Slot.prefab");
    static string CanvasPath => FindPrefabPath(PrefabFolder + "/InventoryCanvas.prefab");
    // Turki's crafting list, put into the Crafting page (CraftingMenu shows it there by itself).
    static string CraftingPath => FindPrefabPath("Assets/Prefabs/UI/CraftingCanvas.prefab");
    const string CanvasName = "InventoryCanvas";

    // Sizes, the same numbers as in Docs/Inventory Setup.md.
    static readonly Vector2 WindowSize = new Vector2(1200f, 800f);   // width, height of InventoryWindow
    const float SlotSize = 120f;
    const int InventorySlots = 20;   // match PlayerInventory's Max Slots
    const int Columns = 5;
    const int HotbarSlots = 8;       // match PlayerInventory's Hotbar Size
    const float TabBarHeight = 80f;

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Inventory UI", false, MenuOrder.BuildInventoryUI)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;   // TextMeshPro text is invisible without its essential files

        string canvasPath = CanvasPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath) != null &&
            !EditorUtility.DisplayDialog(ToolName,
                "InventoryCanvas.prefab already exists. Build a new one and replace it?\n" +
                "(The Slot prefab is kept as it is.)", "Replace", "Cancel"))
        {
            return;
        }

        // An InventoryCanvas already in the scene: offer to swap it for the built one (else there would be two).
        GameObject old = FindInScene(CanvasName);
        bool replaceOld = old != null && EditorUtility.DisplayDialog(ToolName,
            "The scene already has an " + CanvasName + ". Replace it with the built one?\n" +
            "(Ctrl + Z brings the old one back.)", "Replace it", "Keep both");

        MakeFolders(FolderOf(canvasPath));

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath);
        if (slotPrefab == null) slotPrefab = MakeSlotPrefab();
        else AddSlotScript();   // an older Slot prefab may not have SlotUI / ItemName yet

        GameObject canvas = MakeCanvas(slotPrefab);
        GameObject canvasPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, canvasPath, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);

        if (replaceOld)
        {
            canvas.transform.SetSiblingIndex(old.transform.GetSiblingIndex());   // same place in the Hierarchy
            Undo.DestroyObjectImmediate(old);
        }

        MakeEventSystemIfMissing(ToolName);
        AddHotbarSelectorToPlayer();

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(canvasPrefab);   // flashes it in the Project window
        Debug.Log("Inventory UI built: " + canvasPath + " (in the scene too). Press Play and Tab to test. " +
                  "Save the scene (Ctrl + S).");
    }

    // For an inventory built before the slots could show items: adds the scripts to the prefabs
    // you already have, without rebuilding them (your changes are kept).
    [MenuItem("Tools/UI/Add Inventory Scripts", false, MenuOrder.AddInventoryScripts)]
    static void AddScripts()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) == null)
        {
            EditorUtility.DisplayDialog("Add Inventory Scripts",
                "There is no inventory to add scripts to yet. Use Tools > UI > Build Inventory UI instead.", "OK");
            return;
        }

        AddSlotScript();

        // Open the prefab file, change it, save it. Every copy in a scene follows.
        GameObject root = PrefabUtility.LoadPrefabContents(CanvasPath);
        bool ok = AddCanvasScripts(root);
        if (ok) PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
        PrefabUtility.UnloadPrefabContents(root);
        if (!ok) return;

        AddHotbarSelectorToPlayer();
        Debug.Log("Inventory scripts added (only what was missing): SlotUI + ItemName (Slot prefab), " +
                  "SlotGroup (InventoryPage, Hotbar), HeldItem + its ItemName (InventoryWindow), the crafting list " +
                  "(Crafting page), HotbarSelector + ItemDropper (Player). " +
                  "Press Play, pick up items and press Tab to test. Save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    // Adds what an older Slot prefab file may be missing: SlotUI, and the ItemName text (Label mode, tool names).
    static void AddSlotScript()
    {
        string path = SlotPath;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        bool changed = false;

        if (root.GetComponent<SlotUI>() == null) { root.AddComponent<SlotUI>(); changed = true; }
        if (root.transform.Find("ItemName") == null) { MakeItemName(root.transform); changed = true; }

        if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // SlotGroup on InventoryPage and Hotbar, and the HeldItem that follows the mouse.
    // Returns false (and says why) if the hierarchy isn't the one from Docs/Inventory Setup.md.
    static bool AddCanvasScripts(GameObject canvas)
    {
        Transform window = canvas.transform.Find("InventoryWindow");
        Transform inventoryPage = canvas.transform.Find("InventoryWindow/Pages/InventoryPage");
        Transform hotbar = canvas.transform.Find("Hotbar");

        if (window == null || inventoryPage == null || hotbar == null)
        {
            Debug.LogError("Add Inventory Scripts: couldn't find InventoryWindow, InventoryWindow/Pages/InventoryPage " +
                           "or Hotbar in InventoryCanvas. Were they renamed or moved? (See Docs/Inventory Setup.md.)");
            return false;
        }

        AddSlotGroup(inventoryPage.gameObject, false);
        AddSlotGroup(hotbar.gameObject, true);

        Transform held = window.Find("HeldItem");
        if (held == null) MakeHeldItem(window);
        else if (held.Find("ItemName") == null) MakeItemName(held);   // an older HeldItem: add its name text

        Transform craftingPage = window.Find("Pages/CraftingPage");
        if (craftingPage != null) AddCraftingList(craftingPage);
        return true;
    }

    // Puts Turki's CraftingCanvas (the crafting list) into the Crafting page, if the page doesn't have one yet.
    // CraftingMenu notices it's inside the inventory and shows itself whenever the Crafting tab is open.
    // Returns false if the CraftingCanvas prefab isn't in the project (the page then keeps a placeholder text).
    static bool AddCraftingList(Transform craftingPage)
    {
        if (craftingPage.GetComponentInChildren<CraftingMenu>(true) != null) return true;   // already there

        GameObject craftingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CraftingPath);
        if (craftingPrefab == null)
        {
            Debug.LogWarning(ToolName + ": CraftingCanvas.prefab not found, the Crafting page stays a placeholder.");
            return false;
        }

        // The page's "coming soon" text isn't needed any more.
        Transform placeholder = craftingPage.Find("Text (TMP)");
        if (placeholder != null) Object.DestroyImmediate(placeholder.gameObject);

        GameObject list = (GameObject)PrefabUtility.InstantiatePrefab(craftingPrefab, craftingPage);
        // Fill the page and scale 1, so it looks right in the editor too (the prefab saves a top-level canvas
        // at scale 0; CraftingMenu also fixes both when the game runs).
        Stretch(list, 0f, 0f, 0f, 0f);
        list.transform.localScale = Vector3.one;
        return true;
    }

    static void AddSlotGroup(GameObject go, bool isHotbar)
    {
        SlotGroup group = go.GetComponent<SlotGroup>();
        if (group == null) group = go.AddComponent<SlotGroup>();

        SerializedObject so = new SerializedObject(group);   // "isHotbar" must match the field name in SlotGroup
        so.FindProperty("isHotbar").boolValue = isHotbar;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The picture + amount that follows the mouse while a stack is picked up. Last child = drawn on top.
    static void MakeHeldItem(Transform window)
    {
        GameObject held = NewUI("HeldItem", window);
        ((RectTransform)held.transform).sizeDelta = new Vector2(SlotSize - 20f, SlotSize - 20f);
        held.transform.SetAsLastSibling();

        GameObject icon = NewUI("Icon", held.transform);
        Stretch(icon, 0f, 0f, 0f, 0f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;   // hidden until something is held

        GameObject count = NewUI("Count", held.transform);
        RectTransform rt = (RectTransform)count.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);   // bottom right corner
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(80f, 40f);
        AddText(count, "", 28f, TextAlignmentOptions.BottomRight);

        MakeItemName(held.transform);

        held.AddComponent<HeldItemUI>();
    }

    // "ItemName": the item's name in the middle of a slot, e.g. "SWORD". Hidden until needed: Marko's tools without
    // a picture use it, and every item does when GameRules > Item Icon Style = Label.
    static void MakeItemName(Transform parent)
    {
        GameObject name = NewUI("ItemName", parent);
        Stretch(name, 4f, 4f, 4f, 4f);
        TextMeshProUGUI text = AddText(name, "", 18f, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;   // long names shrink to fit the slot
        text.fontSizeMin = 10f;
        text.fontSizeMax = 18f;
        name.SetActive(false);
    }

    // HotbarSelector and ItemDropper go on the object with PlayerInventory (the player) in the open scene.
    static void AddHotbarSelectorToPlayer()
    {
        AddToPlayer<HotbarSelector>();
        AddToPlayer<ItemDropper>();
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

    // One slot: the box, the item picture (hidden until there is an item) and the amount.
    static GameObject MakeSlotPrefab()
    {
        GameObject slot = NewUI("Slot", null);
        ((RectTransform)slot.transform).sizeDelta = new Vector2(SlotSize, SlotSize);
        AddBox(slot);

        GameObject icon = NewUI("Icon", slot.transform);
        Stretch(icon, 15f, 15f, 15f, 15f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;   // clicks go to the slot, not the picture
        iconImage.enabled = false;         // hidden until an item is in the slot

        GameObject count = NewUI("Count", slot.transform);
        RectTransform rt = (RectTransform)count.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);   // bottom right corner
        rt.anchoredPosition = new Vector2(-8f, 4f);
        rt.sizeDelta = new Vector2(80f, 40f);
        AddText(count, "", 28f, TextAlignmentOptions.BottomRight);

        MakeItemName(slot.transform);

        slot.AddComponent<SlotUI>();   // shows the item and handles clicks

        MakeFolders(PrefabFolder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, PrefabFolder + "/Slot.prefab");
        Object.DestroyImmediate(slot);   // the prefab file is saved, the temporary object isn't needed
        return prefab;
    }

    static GameObject MakeCanvas(GameObject slotPrefab)
    {
        // --- InventoryCanvas ---
        GameObject canvas = NewUI("InventoryCanvas", null);
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;   // same size on every screen
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();   // lets the mouse click this canvas

        // --- InventoryWindow (shown / hidden with Tab) ---
        GameObject window = NewUI("InventoryWindow", canvas.transform);
        RectTransform windowRt = (RectTransform)window.transform;
        windowRt.sizeDelta = WindowSize;
        windowRt.anchoredPosition = new Vector2(0f, 60f);

        // --- TabBar with 3 tabs ---
        GameObject tabBar = NewUI("TabBar", window.transform);
        RectTransform barRt = (RectTransform)tabBar.transform;
        barRt.anchorMin = new Vector2(0f, 1f);   // stretched along the top
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0f, TabBarHeight);
        AddBox(tabBar);
        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childControlWidth = tabLayout.childControlHeight = true;         // buttons share the bar equally
        tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;

        Button inventoryTab = MakeTab("InventoryTab", "Inventory", tabBar.transform);
        Button gearTab = MakeTab("GearTab", "Gear", tabBar.transform);
        Button craftingTab = MakeTab("CraftingTab", "Crafting", tabBar.transform);

        // --- Pages (the big frame under the tabs) ---
        GameObject pages = NewUI("Pages", window.transform);
        Stretch(pages, 0f, TabBarHeight, 0f, 0f);
        AddBox(pages);

        GameObject inventoryPage = NewUI("InventoryPage", pages.transform);
        Stretch(inventoryPage, 40f, 40f, 40f, 40f);
        GridLayoutGroup grid = inventoryPage.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(20f, 20f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        AddSlots(slotPrefab, inventoryPage.transform, InventorySlots);

        GameObject gearPage = MakePlaceholderPage("GearPage", "Gear (coming soon)", pages.transform);
        GameObject craftingPage = MakePlaceholderPage("CraftingPage", "Crafting (coming soon)", pages.transform);
        // (AddCanvasScripts below swaps the Crafting placeholder for Turki's crafting list.)

        // --- Hotbar (outside the window, so it stays on screen) ---
        GameObject hotbar = NewUI("Hotbar", canvas.transform);
        RectTransform hotbarRt = (RectTransform)hotbar.transform;
        hotbarRt.anchorMin = hotbarRt.anchorMax = hotbarRt.pivot = new Vector2(0.5f, 0f);   // bottom center
        hotbarRt.anchoredPosition = new Vector2(0f, 20f);
        hotbarRt.sizeDelta = new Vector2(1100f, 130f);
        HorizontalLayoutGroup hotbarLayout = hotbar.AddComponent<HorizontalLayoutGroup>();
        hotbarLayout.spacing = 15f;
        hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
        hotbarLayout.childControlWidth = hotbarLayout.childControlHeight = false;         // slots keep their own size
        hotbarLayout.childForceExpandWidth = hotbarLayout.childForceExpandHeight = false;
        AddSlots(slotPrefab, hotbar.transform, HotbarSlots);

        // --- Connect the scripts (the same as dragging things into their fields) ---
        // The names in quotes must match the field names in TabGroup and InventoryWindow.
        TabGroup tabGroup = tabBar.AddComponent<TabGroup>();
        SerializedObject tabsSo = new SerializedObject(tabGroup);
        SerializedProperty tabs = tabsSo.FindProperty("tabs");
        Button[] buttons = { inventoryTab, gearTab, craftingTab };
        GameObject[] pageList = { inventoryPage, gearPage, craftingPage };
        tabs.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
        {
            SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
            tab.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            tab.FindPropertyRelative("page").objectReferenceValue = pageList[i];
        }
        tabsSo.ApplyModifiedPropertiesWithoutUndo();

        InventoryWindow inventoryWindow = canvas.AddComponent<InventoryWindow>();
        SerializedObject windowSo = new SerializedObject(inventoryWindow);
        windowSo.FindProperty("window").objectReferenceValue = window;
        windowSo.ApplyModifiedPropertiesWithoutUndo();

        AddCanvasScripts(canvas);   // slot numbering and the held item

        // Only the Inventory page shows in the editor (TabGroup does the same when playing).
        gearPage.SetActive(false);
        craftingPage.SetActive(false);

        return canvas;
    }

    static Button MakeTab(string name, string label, Transform parent)
    {
        GameObject tab = NewUI(name, parent);
        Image image = AddBox(tab);
        Button button = tab.AddComponent<Button>();
        button.targetGraphic = image;   // TabGroup colors this to show the open tab

        GameObject text = NewUI("Text (TMP)", tab.transform);
        Stretch(text, 0f, 0f, 0f, 0f);
        AddText(text, label, 32f, TextAlignmentOptions.Center);
        return button;
    }

    static GameObject MakePlaceholderPage(string name, string label, Transform parent)
    {
        GameObject page = NewUI(name, parent);
        Stretch(page, 0f, 0f, 0f, 0f);

        GameObject text = NewUI("Text (TMP)", page.transform);
        Stretch(text, 0f, 0f, 0f, 0f);
        AddText(text, label, 36f, TextAlignmentOptions.Center);
        return page;
    }

    // Copies of the Slot prefab, still linked to it (change the prefab = every slot changes).
    static void AddSlots(GameObject slotPrefab, Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            PrefabUtility.InstantiatePrefab(slotPrefab, parent);
        }
    }

    // NewUI, Stretch, AddBox, AddText, MakeEventSystemIfMissing... live in UIBuildHelper (shared by every UI builder).
}
