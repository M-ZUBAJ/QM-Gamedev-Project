/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Build Gear Page (Tools > Items > Build Plate Armor runs it too). It asks before building
              a Gear page that's already built. Every copy of the inventory follows (it changes the InventoryCanvas
              prefab). Guide: Docs/Armor Setup.md
Purpose: Builds the inventory's Gear page (InventoryCanvas > InventoryWindow > Pages > GearPage, the "coming soon"
         placeholder before), with GearPage on it:
           - Preview (left): a dark panel with the player's character in it (Character = a Raw Image with GearPreview,
             showing Assets/Prefabs/Player/PlayerCharacter), a soft glow on its floor and "Drag to turn";
           - Armor (right): ARMOR, then the six slots laid out like the body (Helmet on top; Shoulders, Chestplate and
             Gloves across; Leggings; Boots at the bottom), each a Button with ButtonFX (grows, turns gold and shines
             under the mouse) and ArmorSlotUI, its name under it and a faint outline of the piece in it; then Total (how
             much the worn armor protects) and SetBonus (right of the leggings and boots: "Iron set 4/6", gold once
             the whole set is worn);
           - Picker: the pop-up window for choosing armor (ArmorPicker), switched off: a dark cover, a window with a title,
             an X, a list of choices (from a switched-off Choice), the "none" text, a red message line and Take Off.
         The outline pictures are drawn by IconArt (Assets/UI/Armor).
Requirement: GearPageBuilder.cs x-> GearPage.cs, GearPreview.cs, ArmorSlotUI.cs, ArmorPicker.cs, ArmorStats.cs, ButtonFX.cs, IconArt.cs, UIBuildHelper.cs, MenuOrder.cs
             GearPageBuilder.cs <-x ArmorBuilder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;    // NewUI, Stretch, AddText, FindPrefabPath, CheckTMP

public static class GearPageBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Gear Page";
    static string CanvasPath => FindPrefabPath("Assets/Prefabs/UI/Inventory/InventoryCanvas.prefab");
    static string CharacterPath => FindPrefabPath("Assets/Prefabs/Player/PlayerCharacter.prefab");
    const string PagePath = "InventoryWindow/Pages/GearPage";

    // Sizes, in pixels on a 1920 x 1080 screen. The Gear page is about 1200 x 720 (the window under its tab bar).
    const float Margin = 30f;
    const float PreviewWidth = 470f;
    const float SlotSize = 116f;

    static readonly Color PanelColor = new Color(0.16f, 0.17f, 0.21f);      // behind the character
    static readonly Color SlotColor = new Color(0.22f, 0.24f, 0.29f);       // the slots and choices (ButtonFX turns them gold)
    static readonly Color TitleColor = new Color(0.42f, 0.43f, 0.48f);
    static readonly Color DarkText = new Color(0.13f, 0.13f, 0.15f);
    static readonly Color DimText = new Color(0.6f, 0.62f, 0.68f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color WindowColor = new Color(0.95f, 0.95f, 0.97f);
    static readonly Color TakeOffColor = new Color(0.72f, 0.22f, 0.2f);
    static readonly Color MessageColor = new Color(0.8f, 0.15f, 0.12f);

    // Where each slot sits on the armor side, like a body: x and y from the middle of that side.
    static readonly (ArmorSlot slot, Vector2 at)[] SlotPlaces =
    {
        (ArmorSlot.Helmet,    new Vector2(0f, 205f)),
        (ArmorSlot.Shoulders, new Vector2(-180f, 65f)),
        (ArmorSlot.Chest,     new Vector2(0f, 65f)),
        (ArmorSlot.Gloves,    new Vector2(180f, 65f)),
        (ArmorSlot.Legs,      new Vector2(0f, -75f)),
        (ArmorSlot.Boots,     new Vector2(0f, -215f)),
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Gear Page", false, MenuOrder.BuildGearPage)]
    static void BuildMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no InventoryCanvas prefab yet. Run Tools > UI > Build Inventory UI first.", "OK");
            return;
        }
        if (PageBuilt() && !EditorUtility.DisplayDialog(ToolName, "The Gear page is built already. Build it again " +
                "(changes you made to it by hand are lost)?", "Build again", "Cancel"))
            return;

        Debug.Log(ToolName + ": " + BuildPage() + " Test: Play, Tab > Gear.");
    }

    // ===== My methods =====

    // True if the Gear page has been built (it has a GearPage script).
    static bool PageBuilt()
    {
        GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
        Transform page = canvas != null ? canvas.transform.Find(PagePath) : null;
        return page != null && page.GetComponent<GearPage>() != null;
    }

    // Empties the Gear page and builds it (also called by Build Plate Armor). Returns what it did, for the Console.
    public static string BuildPage()
    {
        string path = CanvasPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "no InventoryCanvas prefab (Tools > UI > Build Inventory UI).";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform page = root.transform.Find(PagePath);
            if (page == null) return "no " + PagePath + " in " + path + " (built by Tools > UI > Build Inventory UI).";

            for (int i = page.childCount - 1; i >= 0; i--) Object.DestroyImmediate(page.GetChild(i).gameObject);   // the placeholder
            if (page.GetComponent<GearPage>() == null) page.gameObject.AddComponent<GearPage>();

            MakePreview(page);
            ArmorPicker picker = MakePicker(page);
            MakeArmorSide(page, picker);
            picker.transform.SetAsLastSibling();   // the pop-up covers everything else

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path + " > GearPage built (character preview, 6 armor slots, the choosing pop-up).";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ----- Left: the character -----

    static void MakePreview(Transform page)
    {
        GameObject panel = NewUI("Preview", page);
        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(Margin, Margin);
        rt.offsetMax = new Vector2(Margin + PreviewWidth, -Margin);
        Rounded(panel, PanelColor);

        GameObject floor = NewUI("Floor", panel.transform);   // a soft light pool under the feet
        RectTransform f = (RectTransform)floor.transform;
        f.anchorMin = f.anchorMax = new Vector2(0.5f, 0f);
        f.anchoredPosition = new Vector2(0f, 78f);
        f.sizeDelta = new Vector2(320f, 70f);
        Image glow = floor.AddComponent<Image>();
        glow.sprite = IconArt.Glow();
        glow.color = new Color(1f, 1f, 1f, 0.16f);
        glow.raycastTarget = false;

        GameObject character = NewUI("Character", panel.transform);
        Stretch(character, 10f, 16f, 10f, 56f);
        RawImage view = character.AddComponent<RawImage>();
        view.color = Color.white;
        view.raycastTarget = true;   // dragging it turns the character
        GearPreview preview = character.AddComponent<GearPreview>();
        SerializedObject so = new SerializedObject(preview);   // the names in quotes must match GearPreview's fields
        so.FindProperty("characterPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject hint = NewUI("Hint", panel.transform);
        Band(hint, 0f, 14f, 36f);
        TextMeshProUGUI hintText = AddText(hint, "Drag to turn", 19f, TextAlignmentOptions.Center);
        hintText.color = DimText;
    }

    // ----- Right: the armor slots -----

    static void MakeArmorSide(Transform page, ArmorPicker picker)
    {
        GameObject side = NewUI("Armor", page);
        Stretch(side, Margin + PreviewWidth + Margin, Margin, Margin, Margin);

        GameObject title = NewUI("Title", side.transform);
        TopLine(title, 0f, 44f);
        TextMeshProUGUI titleText = AddText(title, "ARMOR", 26f, TextAlignmentOptions.Center);
        titleText.color = TitleColor;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 6f;

        foreach ((ArmorSlot slot, Vector2 at) in SlotPlaces) MakeSlot(side.transform, slot, at, picker);

        GameObject total = NewUI("Total", side.transform);
        Band(total, 0f, 4f, 40f);
        TextMeshProUGUI totalText = AddText(total, "No armor yet: click a slot to put some on", 22f, TextAlignmentOptions.Center);
        totalText.color = DarkText;

        // The set line, in the free space right of the leggings and boots: "Iron set 4/6" (GearPage turns it gold with all six).
        GameObject setLine = NewUI("SetBonus", side.transform);
        RectTransform s = (RectTransform)setLine.transform;
        s.anchorMin = s.anchorMax = new Vector2(0.5f, 0.5f);
        s.anchoredPosition = new Vector2(195f, -150f);
        s.sizeDelta = new Vector2(240f, 130f);
        TextMeshProUGUI setText = AddText(setLine, "", 20f, TextAlignmentOptions.Center);
        setText.color = TitleColor;
        setText.enableWordWrapping = true;
        setText.raycastTarget = false;
    }

    // One slot: [outline / worn piece] with "+1.5%" in its corner, its name under it.
    static void MakeSlot(Transform side, ArmorSlot slot, Vector2 at, ArmorPicker picker)
    {
        GameObject go = NewUI(slot + "Slot", side);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = new Vector2(SlotSize, SlotSize);
        Image box = Rounded(go, SlotColor);
        box.raycastTarget = true;   // the mouse can hover and click it
        Button button = go.AddComponent<Button>();
        button.targetGraphic = box;

        Image empty = Picture(go.transform, "Empty", IconArt.ArmorSlotIcon(slot), new Color(1f, 1f, 1f, 0.28f), 72f);
        empty.preserveAspect = true;
        Image icon = Picture(go.transform, "Icon", null, Color.white, 90f);
        icon.preserveAspect = true;
        icon.enabled = false;   // ArmorSlotUI shows it while a piece is worn

        GameObject defense = NewUI("Defense", go.transform);
        RectTransform d = (RectTransform)defense.transform;
        d.anchorMin = d.anchorMax = d.pivot = new Vector2(1f, 1f);
        d.anchoredPosition = new Vector2(-8f, -4f);
        d.sizeDelta = new Vector2(80f, 28f);   // room for "+2.5%"
        TextMeshProUGUI defenseText = AddText(defense, "", 19f, TextAlignmentOptions.TopRight);
        defenseText.color = Gold;
        defenseText.fontStyle = FontStyles.Bold;

        GameObject label = NewUI("Label", go.transform);   // ButtonFX darkens it under the mouse
        RectTransform l = (RectTransform)label.transform;
        l.anchorMin = l.anchorMax = new Vector2(0.5f, 0f);
        l.pivot = new Vector2(0.5f, 1f);
        l.anchoredPosition = new Vector2(0f, -4f);
        l.sizeDelta = new Vector2(190f, 30f);
        TextMeshProUGUI labelText = AddText(label, ArmorStats.SlotName(slot), 21f, TextAlignmentOptions.Top);
        labelText.color = TitleColor;

        HoverEffect(go, 1.08f, 8);
        SerializedObject so = new SerializedObject(go.AddComponent<ArmorSlotUI>());   // must match ArmorSlotUI's fields
        so.FindProperty("slot").enumValueIndex = (int)slot;
        so.FindProperty("picker").objectReferenceValue = picker;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ----- The pop-up -----

    static ArmorPicker MakePicker(Transform page)
    {
        GameObject picker = NewUI("Picker", page);
        Stretch(picker, 0f, 0f, 0f, 0f);

        GameObject backdrop = NewUI("Backdrop", picker.transform);   // a click outside the window closes it
        Stretch(backdrop, 0f, 0f, 0f, 0f);
        Image cover = backdrop.AddComponent<Image>();
        cover.color = new Color(0f, 0f, 0f, 0.55f);
        Button coverButton = backdrop.AddComponent<Button>();
        coverButton.transition = Selectable.Transition.None;
        coverButton.targetGraphic = cover;

        GameObject window = NewUI("Window", picker.transform);
        RectTransform w = (RectTransform)window.transform;
        w.anchorMin = w.anchorMax = new Vector2(0.5f, 0.5f);
        w.sizeDelta = new Vector2(700f, 520f);
        Image windowBox = Rounded(window, WindowColor);
        windowBox.raycastTarget = true;   // clicks on the window don't reach the cover behind it

        GameObject title = NewUI("Title", window.transform);
        TopLine(title, 24f, 44f);
        TextMeshProUGUI titleText = AddText(title, "Choose armor", 32f, TextAlignmentOptions.Center);
        titleText.color = DarkText;
        titleText.fontStyle = FontStyles.Bold;

        GameObject close = SmallButton(window.transform, "Close", "X", SlotColor, new Vector2(56f, 56f));
        RectTransform c = (RectTransform)close.transform;
        c.anchorMin = c.anchorMax = c.pivot = new Vector2(1f, 1f);
        c.anchoredPosition = new Vector2(-18f, -18f);
        HoverEffect(close, 1.1f, 0);

        GameObject list = NewUI("List", window.transform);
        Stretch(list, 30f, 96f, 30f, 130f);
        GridLayoutGroup grid = list.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 190f);
        grid.spacing = new Vector2(18f, 18f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        MakeChoice(list.transform);

        GameObject empty = NewUI("Empty", window.transform);
        Stretch(empty, 30f, 150f, 30f, 170f);
        TextMeshProUGUI emptyText = AddText(empty, "No armor of this kind in your inventory.", 24f, TextAlignmentOptions.Center);
        emptyText.color = TitleColor;
        emptyText.fontStyle = FontStyles.Italic;

        GameObject message = NewUI("Message", window.transform);
        Band(message, 0f, 86f, 34f);
        TextMeshProUGUI messageText = AddText(message, "", 21f, TextAlignmentOptions.Center);
        messageText.color = MessageColor;

        GameObject takeOff = SmallButton(window.transform, "TakeOff", "Take off", TakeOffColor, new Vector2(380f, 58f));
        RectTransform t = (RectTransform)takeOff.transform;
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0f);
        t.anchoredPosition = new Vector2(0f, 24f);
        HoverEffect(takeOff, 1.06f, 8);

        ArmorPicker script = picker.AddComponent<ArmorPicker>();
        picker.SetActive(false);   // until a slot is clicked
        return script;
    }

    // The switched-off choice ArmorPicker copies for each armor kind: [picture] / name / "+1.5% protection", "x2" in a corner.
    static void MakeChoice(Transform list)
    {
        GameObject choice = NewUI("Choice", list);
        Image box = Rounded(choice, SlotColor);
        box.raycastTarget = true;
        Button button = choice.AddComponent<Button>();
        button.targetGraphic = box;

        Image icon = Picture(choice.transform, "Icon", null, Color.white, 96f);
        icon.preserveAspect = true;
        ((RectTransform)icon.transform).anchoredPosition = new Vector2(0f, 28f);

        GameObject label = NewUI("Label", choice.transform);   // ButtonFX darkens it under the mouse
        Band(label, 6f, 40f, 30f);
        TextMeshProUGUI labelText = AddText(label, "Armor", 19f, TextAlignmentOptions.Center);
        labelText.color = Color.white;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 13f;
        labelText.fontSizeMax = 19f;

        GameObject defense = NewUI("Defense", choice.transform);
        Band(defense, 6f, 12f, 26f);
        TextMeshProUGUI defenseText = AddText(defense, "+1.5% protection", 17f, TextAlignmentOptions.Center);
        defenseText.color = Gold;

        GameObject count = NewUI("Count", choice.transform);
        RectTransform n = (RectTransform)count.transform;
        n.anchorMin = n.anchorMax = n.pivot = new Vector2(1f, 1f);
        n.anchoredPosition = new Vector2(-8f, -4f);
        n.sizeDelta = new Vector2(60f, 26f);
        TextMeshProUGUI countText = AddText(count, "", 18f, TextAlignmentOptions.TopRight);
        countText.color = Color.white;

        HoverEffect(choice, 1.06f, 10);
        choice.SetActive(false);   // a template: ArmorPicker switches on its copies
    }

    // A rounded button with a text child named Label (ButtonFX's text).
    static GameObject SmallButton(Transform parent, string objectName, string text, Color color, Vector2 size)
    {
        GameObject go = NewUI(objectName, parent);
        ((RectTransform)go.transform).sizeDelta = size;
        Image box = Rounded(go, color);
        box.raycastTarget = true;
        go.AddComponent<Button>().targetGraphic = box;

        GameObject label = NewUI("Label", go.transform);
        Stretch(label, 0f, 0f, 0f, 0f);
        TextMeshProUGUI labelText = AddText(label, text, 24f, TextAlignmentOptions.Center);
        labelText.color = Color.white;
        labelText.fontStyle = FontStyles.Bold;
        return go;
    }

    // ButtonFX: grows a little and turns gold under the mouse, a light streak sweeps over it, sparks on click.
    static void HoverEffect(GameObject button, float grow, int sparks)
    {
        SerializedObject so = new SerializedObject(button.AddComponent<ButtonFX>());   // must match ButtonFX's fields
        so.FindProperty("hoverScale").floatValue = grow;
        so.FindProperty("dimOthers").boolValue = false;   // the other slots stay as they are
        so.FindProperty("sparks").intValue = sparks;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ----- Small helpers -----

    // A box with round corners (IconArt's 9-sliced picture), in this color, that doesn't catch the mouse.
    static Image Rounded(GameObject go, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = IconArt.RoundedBox();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // A centered square picture this big.
    static Image Picture(Transform parent, string objectName, Sprite sprite, Color color, float size)
    {
        GameObject go = NewUI(objectName, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Stretched across its parent at the top, "fromTop" pixels down, this high.
    static void TopLine(GameObject go, float fromTop, float height)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -fromTop);
        rt.sizeDelta = new Vector2(0f, height);
    }

    // Stretched across its parent at the bottom ("fromBottom" pixels up), this high, "sides" pixels in from each side.
    static void Band(GameObject go, float sides, float fromBottom, float height)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, fromBottom);
        rt.sizeDelta = new Vector2(-2f * sides, height);
    }
}
