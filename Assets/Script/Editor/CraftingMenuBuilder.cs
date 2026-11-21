/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Build Crafting Menu. It asks first, because it changes Turki's CraftingCanvas prefab.
              Run it again any time (e.g. after new tools got recipes): it builds the row again and sorts the list again.
              Every crafting list follows (the inventory's Crafting page and the C menu are both that prefab).
              Guide: Docs/Crafting Menu Setup.md
Purpose: Gives Turki's crafting list pictures and the tier tools, in one click:
           - makes Assets/Prefabs/UI/Crafting/CraftingRow.prefab: a row with the item's picture and name and its recipe
             as pictures with "have / need" amounts (Tony's CraftingRow), and puts it in Turki's CraftingMenu > Row
             Prefab (his old CraftRow prefab is kept, just not used); the picture has a switched-off "Owned" badge (a
             green tick, IconArt.Tick) that shows while the player already has that armor piece;
           - makes Turki's panel bigger (Panel Size) and puts its rows in a scroll list with a scroll bar, so any number
             of items fits (the mouse wheel scrolls it);
           - tabs over the list (Panel > Tabs: All, Materials, Food, Tools, Armor; Tony's CraftingTabs on the panel)
             and a "Nothing to craft here yet." text in the list (Scroll > Empty), built again every run;
           - armor set panels: the look of the box each armor set's rows are grouped in (Scroll > SetPanel, switched
             off: a rounded box with a Header of Stripe, Title, Info and Progress) and Tony's CraftingSetPanels on the
             panel, built again every run;
           - sorts the list (Craftables): the items that aren't tools first, in their order (Plank, Stew...), then every
             tool with a recipe made at the Crafting page, Wooden > Stone > Iron, each Pickaxe, Axe, Sword, then every
             armor piece with a recipe, set by set (the weakest set first: Copper, then Iron), each helmet to boots. Tools without a recipe (Gold, Diamond) are left out.
         SortList sorts the list again and makes the set panels (Tools > Items > Build Plate Armor uses it to add the
         armor).
         Turki's CraftingMenu script gets [Tony] lines (it calls CraftingRow.Show; upgraded tools keep their slot; a
         worn armor piece can be upgraded). Logs: Docs/TonyEditMate/Turki/19-11-2026 Crafting Menu Icons And Tier
         Tools.md, .../20-11-2026 Reinforced Iron Armor And Set Panels.md
Requirement: CraftingMenuBuilder.cs x-> CraftingMenu.cs, CraftingRow.cs, CraftingTabs.cs, CraftingSetPanels.cs, ItemData.cs, ToolStats.cs, IconArt.cs, UIBuildHelper.cs, MenuOrder.cs
             CraftingMenuBuilder.cs <-x ArmorBuilder.cs, TierToolBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddBox, AddText, FindPrefabPath, MakeFolders...

public static class CraftingMenuBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Crafting Menu";
    // Turki's crafting list, found by name if it was moved.
    static string CraftingPath => FindPrefabPath("Assets/Prefabs/UI/CraftingCanvas.prefab");
    const string RowPath = "Assets/Prefabs/UI/Crafting/CraftingRow.prefab";

    // Sizes (pixels on a 1920 x 1080 screen). The inventory's Crafting page is about 1200 x 720.
    static readonly Vector2 PanelSize = new Vector2(960f, 640f);   // Turki's panel was 700 x 600 (about 8 rows)
    const float RowHeight = 76f;
    const float IconSize = 58f;
    const float TextLeft = 92f;      // where the name and the recipe start, after the picture
    const float StatusWidth = 150f;  // the CRAFT text on the right
    const float OwnedSize = 26f;     // the green tick on the picture's corner (an armor piece the player has)
    const float TabHeight = 40f;     // the tabs over the list
    const float TabWidth = 150f;     // each tab (the five sit in the middle, not stretched across the panel)
    const string SetPanelName = "SetPanel";   // Scroll > SetPanel: the look of an armor set's box (CraftingSetPanels)
    const float SetHeaderHeight = 58f;        // its header: the set's name over a line of what it gives

    static readonly Color NameColor = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color StatusColor = new Color(0.1f, 0.55f, 0.25f);
    static readonly Color TabIdle = new Color(0.86f, 0.87f, 0.9f);     // CraftingTabs makes the picked one gold
    static readonly Color EmptyColor = new Color(0.45f, 0.46f, 0.5f);
    static readonly Color InfoColor = new Color(0.25f, 0.26f, 0.3f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Crafting Menu", false, MenuOrder.BuildCraftingMenu)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;

        string canvasPath = CraftingPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "Turki's CraftingCanvas.prefab wasn't found (Assets/Prefabs/UI).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "This changes Turki's CraftingCanvas prefab:\n\n" +
                "- rows with pictures (the item and its recipe, green / red for enough / missing; a green tick on armor " +
                "you already have)\n" +
                "- a bigger panel with a scroll list\n" +
                "- tabs over the list: All, Materials, Food, Tools, Armor\n" +
                "- each armor set's pieces grouped in one panel (Copper, Iron)\n" +
                "- the list sorted, with every Wooden, Stone and Iron tool that has a recipe\n\n" +
                "The inventory's Crafting page and the C menu both follow. Continue?", "Build", "Cancel"))
            return;

        GameObject rowPrefab = MakeRowPrefab();
        string report = UpgradeCraftingList(canvasPath, rowPrefab);
        AssetDatabase.SaveAssets();
        Debug.Log(ToolName + ": " + RowPath + " made; " + report +
                  " Test: Play, Tab > Crafting (or press C).");
    }

    // ===== My methods =====

    // The row prefab: [picture] Name / recipe pictures with "have/need Name", and CRAFT on the right.
    static GameObject MakeRowPrefab()
    {
        GameObject row = NewUI("CraftingRow", null);
        ((RectTransform)row.transform).sizeDelta = new Vector2(880f, RowHeight);
        Image back = AddBox(row);   // a white box like the rest of the inventory
        back.color = Color.white;
        Button button = row.AddComponent<Button>();
        button.targetGraphic = back;
        LayoutElement size = row.AddComponent<LayoutElement>();
        size.preferredHeight = size.minHeight = RowHeight;
        row.AddComponent<CanvasGroup>();   // CraftingRow fades the row while something is missing

        Image icon = NewUI("Icon", row.transform).AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform i = icon.rectTransform;
        i.anchorMin = i.anchorMax = new Vector2(0f, 0.5f);
        i.anchoredPosition = new Vector2(TextLeft / 2f, 0f);
        i.sizeDelta = new Vector2(IconSize, IconSize);

        // A green tick on the picture's top right corner: CraftingRow shows it while the player already has that armor
        // piece (in the inventory or worn), so it isn't crafted twice by mistake.
        Image owned = NewUI("Owned", icon.transform).AddComponent<Image>();
        owned.sprite = IconArt.Tick();
        owned.raycastTarget = false;
        RectTransform o = owned.rectTransform;
        o.anchorMin = o.anchorMax = new Vector2(1f, 1f);
        o.anchoredPosition = new Vector2(-2f, -2f);   // its middle a little inside the corner
        o.sizeDelta = new Vector2(OwnedSize, OwnedSize);
        owned.gameObject.SetActive(false);

        TextMeshProUGUI name = AddText(NewUI("Name", row.transform), "Item", 26f, TextAlignmentOptions.MidlineLeft);
        name.color = NameColor;
        name.fontStyle = FontStyles.Bold;
        name.enableWordWrapping = false;
        Band(name.gameObject, 14f, 32f);

        GameObject ingredients = NewUI("Ingredients", row.transform);
        Band(ingredients, -16f, 30f);
        HorizontalLayoutGroup line = ingredients.AddComponent<HorizontalLayoutGroup>();
        line.spacing = 22f;
        line.childAlignment = TextAnchor.MiddleLeft;
        line.childControlWidth = line.childControlHeight = true;
        line.childForceExpandWidth = line.childForceExpandHeight = false;
        MakeIngredientTemplate(ingredients.transform);

        TextMeshProUGUI status = AddText(NewUI("Status", row.transform), "CRAFT", 22f, TextAlignmentOptions.MidlineRight);
        status.color = StatusColor;
        status.fontStyle = FontStyles.Bold;
        status.characterSpacing = 4f;
        RectTransform s = status.rectTransform;
        s.anchorMin = s.anchorMax = s.pivot = new Vector2(1f, 0.5f);   // on the right edge
        s.anchoredPosition = new Vector2(-22f, 0f);
        s.sizeDelta = new Vector2(StatusWidth - 22f, 32f);

        row.AddComponent<CraftingRow>();

        MakeFolders(FolderOf(RowPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, RowPath);
        Object.DestroyImmediate(row);   // the prefab file is saved; the temporary object isn't needed
        return prefab;
    }

    // One ingredient: [small picture] "1/2 Plank". Switched off: CraftingRow copies it per ingredient.
    static void MakeIngredientTemplate(Transform parent)
    {
        GameObject chip = NewUI("Ingredient", parent);
        HorizontalLayoutGroup line = chip.AddComponent<HorizontalLayoutGroup>();
        line.spacing = 6f;
        line.childAlignment = TextAnchor.MiddleLeft;
        line.childControlWidth = line.childControlHeight = true;
        line.childForceExpandWidth = line.childForceExpandHeight = false;

        Image icon = NewUI("Icon", chip.transform).AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        LayoutElement iconSize = icon.gameObject.AddComponent<LayoutElement>();
        iconSize.preferredWidth = iconSize.preferredHeight = 26f;

        TextMeshProUGUI amount = AddText(NewUI("Amount", chip.transform), "1/2 Plank", 20f, TextAlignmentOptions.MidlineLeft);
        amount.enableWordWrapping = false;

        chip.SetActive(false);
    }

    // A strip across the row (between the picture and the CRAFT text), its middle "y" above the row's middle.
    static void Band(GameObject go, float y, float height)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(TextLeft, y - height / 2f);
        rt.offsetMax = new Vector2(-StatusWidth, y + height / 2f);
    }

    // Turki's CraftingCanvas: the new row, a bigger panel, the scroll list and the sorted list. Returns a report.
    static string UpgradeCraftingList(string path, GameObject rowPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            CraftingMenu menu = root.GetComponentInChildren<CraftingMenu>(true);
            if (menu == null) return "no CraftingMenu in " + path + ", nothing changed.";

            SerializedObject so = new SerializedObject(menu);   // the names in quotes must match Turki's fields
            GameObject panel = so.FindProperty("panel").objectReferenceValue as GameObject;
            Transform rows = so.FindProperty("rows").objectReferenceValue as Transform;
            if (panel == null || rows == null) return "its Panel or Rows is empty, nothing changed.";

            ((RectTransform)panel.transform).sizeDelta = PanelSize;
            bool scroll = MakeScrollList(panel.transform, rows);
            bool tabs = MakeTabs(panel.transform);
            bool sets = MakeSetPanels(panel.transform);
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab.GetComponent<Button>();
            int tools = SortCraftables(so.FindProperty("craftables"));
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path + ": rows with pictures, panel " + PanelSize.x + " x " + PanelSize.y +
                   (scroll ? ", rows in a new scroll list" : ", scroll list already there") +
                   (tabs ? ", tabs All / Materials / Food / Tools / Armor" : ", no tabs (no scroll list)") +
                   (sets ? ", armor grouped by set" : "") +
                   ", " + tools + " tools in the list (Wooden > Stone > Iron).";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Puts the rows in a scroll list where they were (between Turki's title and message): Scroll (fills the space
    // that's left) > Viewport (cuts off what's outside) > Rows, plus a scroll bar on the right. False = already done.
    static bool MakeScrollList(Transform panel, Transform rows)
    {
        if (rows.parent != null && rows.parent.name == "Viewport") return false;

        GameObject scroll = NewUI("Scroll", panel);
        scroll.transform.SetSiblingIndex(rows.GetSiblingIndex());
        LayoutElement fill = scroll.AddComponent<LayoutElement>();
        fill.flexibleHeight = 1f;   // takes all the height the title and the message leave
        fill.minHeight = 150f;

        GameObject viewport = NewUI("Viewport", scroll.transform);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        Image catcher = viewport.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0f);   // see-through, but the mouse wheel works over the gaps too
        viewport.AddComponent<RectMask2D>();

        rows.SetParent(viewport.transform, false);
        RectTransform r = (RectTransform)rows;
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.zero;
        ContentSizeFitter grow = rows.GetComponent<ContentSizeFitter>();
        if (grow == null) grow = rows.gameObject.AddComponent<ContentSizeFitter>();
        grow.verticalFit = ContentSizeFitter.FitMode.PreferredSize;   // as tall as all its rows

        Scrollbar bar = MakeScrollbar(scroll.transform);
        ScrollRect list = scroll.AddComponent<ScrollRect>();
        list.horizontal = false;
        list.vertical = true;
        list.movementType = ScrollRect.MovementType.Clamped;
        list.scrollSensitivity = 40f;
        list.viewport = (RectTransform)viewport.transform;
        list.content = r;
        list.verticalScrollbar = bar;
        list.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        list.verticalScrollbarSpacing = 6f;
        return true;
    }

    // The tabs over the list, between Turki's title and the scroll list (built again every run): one rounded button per
    // CraftingTabs.Kind, sharing the panel's width; a "Nothing to craft here yet." text over the list (hidden); and
    // CraftingTabs on the panel, which picks a tab and hides the other rows. False = there's no scroll list to put them on.
    static bool MakeTabs(Transform panel)
    {
        Transform scroll = panel.Find("Scroll");
        if (scroll == null) return false;
        Transform oldTabs = panel.Find("Tabs");
        if (oldTabs != null) Object.DestroyImmediate(oldTabs.gameObject);

        GameObject tabs = NewUI("Tabs", panel);
        tabs.transform.SetSiblingIndex(scroll.GetSiblingIndex());   // just above the list
        ((RectTransform)tabs.transform).sizeDelta = new Vector2(0f, TabHeight);
        LayoutElement size = tabs.AddComponent<LayoutElement>();
        size.preferredHeight = size.minHeight = TabHeight;
        // Flexible Height 0 on purpose: left empty, Unity asks the row's Horizontal Layout Group instead, which says 1
        // (its tabs fill its height), and the panel then gave the tabs half of its spare height: huge tabs, a short list.
        size.flexibleHeight = 0f;
        HorizontalLayoutGroup row = tabs.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 8f;
        row.childAlignment = TextAnchor.MiddleCenter;   // the tabs sit in the middle
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = false;              // each tab its own width (Tab Width), not stretched
        row.childForceExpandHeight = true;              // as tall as the row
        foreach (CraftingTabs.Kind kind in System.Enum.GetValues(typeof(CraftingTabs.Kind))) MakeTab(tabs.transform, kind);

        Transform oldEmpty = scroll.Find("Empty");
        if (oldEmpty != null) Object.DestroyImmediate(oldEmpty.gameObject);
        TextMeshProUGUI empty = AddText(NewUI("Empty", scroll), "Nothing to craft here yet.", 24f, TextAlignmentOptions.Center);
        Stretch(empty.gameObject, 0f, 0f, 0f, 0f);
        empty.color = EmptyColor;
        empty.fontStyle = FontStyles.Italic;
        empty.raycastTarget = false;   // never in the way of the scroll wheel
        empty.gameObject.SetActive(false);

        if (panel.GetComponent<CraftingTabs>() == null) panel.gameObject.AddComponent<CraftingTabs>();
        return true;
    }

    // One tab: a rounded button with its name ("Tools"). CraftingTabs colors it (gold when picked) and fades it when
    // there's nothing in it (its CanvasGroup); the Button only darkens it a little under the mouse.
    static void MakeTab(Transform parent, CraftingTabs.Kind kind)
    {
        GameObject tab = NewUI(kind.ToString(), parent);
        LayoutElement width = tab.AddComponent<LayoutElement>();
        width.preferredWidth = width.minWidth = TabWidth;
        width.flexibleWidth = 0f;
        Image back = tab.AddComponent<Image>();
        back.sprite = IconArt.RoundedBox();
        back.type = Image.Type.Sliced;
        back.color = TabIdle;
        Button button = tab.AddComponent<Button>();
        button.targetGraphic = back;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        tab.AddComponent<CanvasGroup>();

        TextMeshProUGUI label = AddText(NewUI("Label", tab.transform), kind.ToString(), 20f, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 0f, 0f, 0f, 0f);
        label.color = NameColor;
        label.raycastTarget = false;
    }

    // The look of an armor set's panel (Scroll > SetPanel, switched off: CraftingSetPanels copies it around each set's
    // rows while playing) and CraftingSetPanels on the panel. Built again every run. False = no scroll list to put it in.
    //   SetPanel: a rounded box (tinted in the set's color) stacking its children: Header, then the set's rows
    //   Header:   Stripe (a bar in the set's color) | Title "IRON SET"                    Progress "Worn 2/6"
    //                                              | Info "10% protection - all 6: +1 coin for every monster killed"
    static bool MakeSetPanels(Transform panel)
    {
        Transform scroll = panel.Find("Scroll");
        if (scroll == null) return false;
        Transform old = scroll.Find(SetPanelName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject box = NewUI(SetPanelName, scroll);
        ((RectTransform)box.transform).sizeDelta = new Vector2(880f, 200f);   // the list sizes each copy itself
        Image back = box.AddComponent<Image>();
        back.sprite = IconArt.RoundedBox();
        back.type = Image.Type.Sliced;
        back.color = Color.white;      // CraftingSetPanels tints it in the set's color
        back.raycastTarget = false;    // never in the way of the rows' clicks or the scroll wheel
        VerticalLayoutGroup column = box.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(12, 12, 8, 12);
        column.spacing = 8f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;     // the rows as wide as the box
        column.childForceExpandHeight = false;   // each row its own height

        GameObject header = NewUI("Header", box.transform);
        LayoutElement size = header.AddComponent<LayoutElement>();
        size.preferredHeight = size.minHeight = SetHeaderHeight;
        size.flexibleHeight = 0f;

        Image stripe = NewUI("Stripe", header.transform).AddComponent<Image>();
        stripe.sprite = IconArt.RoundedBox();
        stripe.type = Image.Type.Sliced;
        stripe.raycastTarget = false;
        RectTransform st = stripe.rectTransform;
        st.anchorMin = Vector2.zero;
        st.anchorMax = new Vector2(0f, 1f);
        st.pivot = new Vector2(0f, 0.5f);
        st.offsetMin = new Vector2(2f, 6f);
        st.offsetMax = new Vector2(10f, -6f);    // an 8 px bar down the left

        TextMeshProUGUI title = AddText(NewUI("Title", header.transform), "IRON SET", 24f, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3f;
        title.enableWordWrapping = false;
        title.raycastTarget = false;
        HeaderBand(title.gameObject, true, 220f);

        TextMeshProUGUI info = AddText(NewUI("Info", header.transform), "10% protection", 18f, TextAlignmentOptions.MidlineLeft);
        info.color = InfoColor;
        info.enableWordWrapping = false;
        info.enableAutoSizing = true;            // a long bonus gets a little smaller instead of running off the box
        info.fontSizeMin = 13f;
        info.fontSizeMax = 18f;
        info.raycastTarget = false;
        HeaderBand(info.gameObject, false, 12f);

        TextMeshProUGUI progress = AddText(NewUI("Progress", header.transform), "Worn 0/6", 20f, TextAlignmentOptions.MidlineRight);
        progress.fontStyle = FontStyles.Bold;
        progress.enableWordWrapping = false;
        progress.raycastTarget = false;
        RectTransform p = progress.rectTransform;
        p.anchorMin = new Vector2(1f, 0.5f);
        p.anchorMax = Vector2.one;
        p.pivot = new Vector2(1f, 0.5f);
        p.offsetMin = new Vector2(-210f, 0f);
        p.offsetMax = new Vector2(-10f, 0f);     // the top right, next to the title

        box.SetActive(false);
        if (panel.GetComponent<CraftingSetPanels>() == null) panel.gameObject.AddComponent<CraftingSetPanels>();
        return true;
    }

    // The top or bottom half of the set panel's header, after its stripe, "right" pixels short of the right edge.
    static void HeaderBand(GameObject go, bool top, float right)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, top ? 0.5f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(22f, 0f);
        rt.offsetMax = new Vector2(-right, 0f);
    }

    // A slim scroll bar down the right side.
    static Scrollbar MakeScrollbar(Transform parent)
    {
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        };
        GameObject go = DefaultControls.CreateScrollbar(res);
        go.name = "Scrollbar";
        go.transform.SetParent(parent, false);
        foreach (Transform part in go.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = UILayer;

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(14f, 0f);

        Scrollbar bar = go.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
        if (bar.targetGraphic != null) bar.targetGraphic.color = new Color(0.45f, 0.47f, 0.52f);
        return bar;
    }

    // Sorts Turki's crafting list again (see SortCraftables), e.g. after new armor got recipes, and makes the armor set
    // panels (MakeSetPanels). Returns a report.
    public static string SortList()
    {
        string path = CraftingPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "no CraftingCanvas prefab, the list wasn't sorted.";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            CraftingMenu menu = root.GetComponentInChildren<CraftingMenu>(true);
            if (menu == null) return "no CraftingMenu in " + path + ", the list wasn't sorted.";
            SerializedObject so = new SerializedObject(menu);   // the names in quotes must match Turki's fields
            SerializedProperty list = so.FindProperty("craftables");
            int tools = SortCraftables(list);
            int total = list.arraySize;
            GameObject panel = so.FindProperty("panel").objectReferenceValue as GameObject;
            bool sets = panel != null && MakeSetPanels(panel.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path + " sorted: " + total + " items (" + tools + " tools, then the armor set by set)" +
                   (sets ? ", armor grouped by set" : ", no set panels (run Tools > UI > Build Crafting Menu)") + ".";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Sorts Craftables: everything that isn't a tool or armor first (in its order), then every tool with a recipe made
    // at the Crafting page, weakest tier first (Wooden > Stone > Iron), each Pickaxe, Axe, Sword, then every armor piece
    // with a recipe made there, set by set (the weakest set first: all its pieces' Protection added up, Copper 10 %
    // before Iron 16 %), each helmet to boots, so each set's rows are together for its panel. Returns how many tools.
    static int SortCraftables(SerializedProperty list)
    {
        List<ItemData> others = new List<ItemData>();
        for (int i = 0; i < list.arraySize; i++)
        {
            ItemData item = list.GetArrayElementAtIndex(i).objectReferenceValue as ItemData;
            if (item != null && !item.IsTool && !item.IsArmor && !others.Contains(item)) others.Add(item);
        }

        List<ItemData> tools = new List<ItemData>();
        List<ItemData> armor = new List<ItemData>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || !item.IsCraftable || item.CraftedAt != CraftStation.Crafting) continue;
            if (item.IsTool) tools.Add(item);
            else if (item.IsArmor) armor.Add(item);
        }
        tools.Sort((a, b) =>
        {
            int byTier = ((int)a.Tool.tier).CompareTo((int)b.Tool.tier);
            return byTier != 0 ? byTier : ((int)a.Tool.kind).CompareTo((int)b.Tool.kind);
        });
        Dictionary<string, float> setTotal = new Dictionary<string, float>();   // each set's pieces' Protection added up
        foreach (ItemData piece in armor)
        {
            string set = piece.Armor.set ?? "";
            setTotal[set] = (setTotal.TryGetValue(set, out float sum) ? sum : 0f) + piece.Armor.protection;
        }
        armor.Sort((a, b) =>
        {
            string setA = a.Armor.set ?? "", setB = b.Armor.set ?? "";
            int bySet = setTotal[setA].CompareTo(setTotal[setB]);
            if (bySet == 0) bySet = string.CompareOrdinal(setA, setB);
            if (bySet != 0) return bySet;
            int bySlot = ((int)a.Armor.slot).CompareTo((int)b.Armor.slot);
            return bySlot != 0 ? bySlot : string.CompareOrdinal(a.Id, b.Id);
        });

        List<ItemData> sorted = new List<ItemData>(others);
        sorted.AddRange(tools);
        sorted.AddRange(armor);
        list.arraySize = sorted.Count;
        for (int i = 0; i < sorted.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sorted[i];
        return tools.Count;
    }
}
