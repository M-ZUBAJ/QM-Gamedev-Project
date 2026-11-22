/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > UI > Build Perk Panel, then save the scene. It asks before replacing the
              prefab or the perk panel already in the scene (Ctrl + Z brings the old one back in the scene).
              To change sizes or colors for every future build, edit Fields below.
              Needs TMP Essentials (it tells you if they are missing). Guide: Docs/Perk Panel Setup.md
Purpose: Builds the "CHOOSE YOUR REWARD" panel of Marko's perk chests in one click, everything connected:
           PerkSelectCanvas (Marko's PerkSelectionUI + Tony's PerkPanelInfo)
             > PerkSelectionPanel (the dark backdrop: Marko's script switches it on and off)
               > Window > Title, Line, PriceRow (the price + your coins), Cards > PerkOption1..3 (Glow, Icon, PerkName,
                          PerkDescription, ChooseButton > Label), NeedText, Hint, RerollButton > Label
         It saves Assets/Prefabs/UI/Perks/PerkSelectCanvas.prefab and puts it in the open scene instead of the old
         panel (there can only be one PerkSelectionUI). Marko's script isn't changed: its fields are filled in here.
         It also draws the pictures (IconArt) and gives each of Marko's Perk Data without an icon the matching one:
         a heart (Max Health), lightning (Max Stamina), double arrows (Move Speed), a flame (Fireball), and the loot
         boxes' perks theirs (IconArt.ForPerk).
         The cards keep their width when a box shows only 1 or 2 perks (centered). REROLL (bottom right) only shows
         for a box that allows rerolls (PerkPanelInfo: the epic box in the old chest system, every box in the new one).
         The new chest system's random boxes use their own pop-up instead (Tools > UI > Build Perk Roll Popup).
         UpdatePrefab brings a panel built before the loot boxes up to date in place (Tools > World > Build Loot Chests
         calls it): the cards' width, the REROLL button, the title and reroll fields of PerkPanelInfo.
Requirement: PerkPanelBuilder.cs x-> UIBuildHelper.cs, IconArt.cs, PerkSelectionUI.cs, PerkPanelInfo.cs, PerkData.cs, ButtonFX.cs, MenuOrder.cs
             PerkPanelBuilder.cs <-x LootChestBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class PerkPanelBuilder
{
    // One perk card's parts, to connect to the scripts.
    struct Card
    {
        public Button choose;
        public Image icon;
        public TextMeshProUGUI name;
        public TextMeshProUGUI description;
        public TextMeshProUGUI chooseLabel;
    }

    // ===== Fields =====
    const string ToolName = "Build Perk Panel";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/UI/Perks/PerkSelectCanvas.prefab");
    const string CanvasName = "PerkSelectCanvas";
    const int SortingOrder = 20;   // over the HUD (0) and the messages (5), under the victory screen (30)

    // The look: like the title screen (dark see-through boxes, white text, gold).
    static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color WindowColor = new Color(0.07f, 0.08f, 0.11f, 0.95f);
    static readonly Color CardColor = new Color(0.14f, 0.16f, 0.21f, 1f);
    static readonly Color ButtonColor = new Color(0.07f, 0.08f, 0.11f, 0.9f);   // ButtonFX turns it gold under the mouse
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color SoftWhite = new Color(0.86f, 0.87f, 0.92f);
    static readonly Color Dim = new Color(0.62f, 0.64f, 0.7f);
    static readonly Color Red = new Color(1f, 0.38f, 0.32f);

    // Sizes (pixels on a 1920 x 1080 screen; y = from the top or bottom of the box it's in).
    static readonly Vector2 WindowSize = new Vector2(1260f, 740f);
    static readonly Vector2 CardsSize = new Vector2(1170f, 420f);
    const float CardsY = -25f;      // the cards' middle, below the window's middle
    const float CardSpacing = 30f;  // between the cards
    const float IconSize = 128f;
    static float CardWidth => (CardsSize.x - CardSpacing * 2f) / 3f;   // three cards fill the row; fewer stay this wide

    static Sprite coin, box, glow;  // the pictures for this build (IconArt)

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Perk Panel", false, MenuOrder.BuildPerkPanel)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog(ToolName, "PerkSelectCanvas.prefab already exists. Build a new one and replace it?",
                                         "Replace", "Cancel"))
            return;

        // There can only be one PerkSelectionUI: the old panel (Marko's hand-made one, or an older build) goes.
        PerkSelectionUI old = Object.FindFirstObjectByType<PerkSelectionUI>(FindObjectsInactive.Include);
        GameObject oldRoot = old == null ? null
                           : PrefabUtility.IsPartOfPrefabInstance(old.gameObject) ? PrefabUtility.GetOutermostPrefabInstanceRoot(old.gameObject)
                           : old.gameObject;
        if (oldRoot != null && !EditorUtility.DisplayDialog(ToolName,
                "The scene already has a perk panel (\"" + oldRoot.name + "\"). Replace it with the built one?\n\n" +
                "There can only be one. Ctrl + Z brings the old one back.", "Replace it", "Cancel"))
            return;

        coin = IconArt.Coin();
        box = IconArt.RoundedBox();
        glow = IconArt.Glow();
        string icons = GiveIconsToPerks();

        GameObject canvas = MakeCanvas();
        MakeFolders(FolderOf(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, PrefabPath, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, ToolName);
        if (oldRoot != null)
        {
            canvas.transform.SetSiblingIndex(oldRoot.transform.GetSiblingIndex());   // same place in the Hierarchy
            Undo.DestroyObjectImmediate(oldRoot);
        }
        MakeEventSystemIfMissing(ToolName);   // the CHOOSE buttons need one

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log(ToolName + ": " + PrefabPath + " (in the scene too" + (oldRoot != null ? ", in place of the old panel" : "") +
                  "). " + icons + " Save the scene (Ctrl + S), then Play and press E at a chest.");
    }

    // ===== My methods =====

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);

        // The backdrop: dims the game behind the panel and catches the clicks around it.
        GameObject panel = NewUI("PerkSelectionPanel", canvas.transform);
        Stretch(panel, 0f, 0f, 0f, 0f);
        panel.AddComponent<Image>().color = Backdrop;

        GameObject window = NewUI("Window", panel.transform);
        Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
        Rounded(window, WindowColor);

        TextMeshProUGUI title = MakeText(window.transform, "Title", "CHOOSE YOUR REWARD", 54f, Color.white, TextAlignmentOptions.Center);
        Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1100f, 70f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;

        Image line = NewImage(window.transform, "Line", null, new Color(Gold.r, Gold.g, Gold.b, 0.8f));
        Place(line.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(360f, 3f));

        MakePriceRow(window.transform, out TextMeshProUGUI price, out TextMeshProUGUI coins);

        GameObject cards = NewUI("Cards", window.transform);
        Place(cards, new Vector2(0.5f, 0.5f), new Vector2(0f, CardsY), CardsSize);
        cards.AddComponent<HorizontalLayoutGroup>();
        Card[] made = new Card[3];
        for (int i = 0; i < made.Length; i++) made[i] = MakeCard(cards.transform, i + 1);
        FitCards(cards.transform);   // three cards fill the row; one or two stay as wide, in the middle

        TextMeshProUGUI need = MakeText(window.transform, "NeedText", "Not enough coins", 28f, Red, TextAlignmentOptions.Center);
        Place(need.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(1100f, 40f));
        need.gameObject.SetActive(false);   // PerkPanelInfo shows it while the player can't pay

        TextMeshProUGUI hint = MakeText(window.transform, "Hint", "E: close, or walk away", 24f, Dim, TextAlignmentOptions.Center);
        Place(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(1100f, 36f));

        MakeReroll(window.transform, out Button reroll, out TextMeshProUGUI rerollLabel);

        // Marko's script. The names in quotes must match its field names.
        PerkSelectionUI selection = canvas.AddComponent<PerkSelectionUI>();
        SerializedObject s = new SerializedObject(selection);
        s.FindProperty("selectionPanel").objectReferenceValue = panel;
        SetArray(s, "perkButtons", made, c => c.choose);
        SetArray(s, "perkIcons", made, c => c.icon);
        SetArray(s, "perkNames", made, c => c.name);
        SetArray(s, "perkDescriptions", made, c => c.description);
        s.ApplyModifiedPropertiesWithoutUndo();

        // Tony's extras: the price, your coins, the faded CHOOSE buttons, the pop.
        PerkPanelInfo info = canvas.AddComponent<PerkPanelInfo>();
        SerializedObject p = new SerializedObject(info);
        p.FindProperty("priceText").objectReferenceValue = price;
        p.FindProperty("coinsText").objectReferenceValue = coins;
        p.FindProperty("needText").objectReferenceValue = need;
        p.FindProperty("hintText").objectReferenceValue = hint;
        p.FindProperty("window").objectReferenceValue = window.transform;
        p.FindProperty("titleText").objectReferenceValue = title;
        p.FindProperty("titleLine").objectReferenceValue = line;
        p.FindProperty("rerollButton").objectReferenceValue = reroll;
        p.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
        SetArray(p, "chooseButtons", made, c => c.choose);
        SetArray(p, "chooseLabels", made, c => c.chooseLabel);
        p.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);   // hidden until a chest opens it (Marko's script shows it)
        return canvas;
    }

    // Price (coin) 2 coins          You have (coin) 20
    static void MakePriceRow(Transform window, out TextMeshProUGUI price, out TextMeshProUGUI coins)
    {
        GameObject row = NewUI("PriceRow", window);
        Place(row, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1100f, 46f));
        HorizontalLayoutGroup line = row.AddComponent<HorizontalLayoutGroup>();
        line.childAlignment = TextAnchor.MiddleCenter;
        line.spacing = 10f;
        line.childControlWidth = line.childControlHeight = true;
        line.childForceExpandWidth = line.childForceExpandHeight = false;

        RowText(row.transform, "PriceLabel", "Price", 28f, Dim);
        RowCoin(row.transform, "PriceCoin");
        price = RowText(row.transform, "PriceText", "2 coins", 30f, Gold);
        price.fontStyle = FontStyles.Bold;
        NewUI("Gap", row.transform).AddComponent<LayoutElement>().preferredWidth = 50f;
        RowText(row.transform, "HaveLabel", "You have", 28f, Dim);
        RowCoin(row.transform, "HaveCoin");
        coins = RowText(row.transform, "CoinsText", "20", 30f, Color.white);
        coins.fontStyle = FontStyles.Bold;
    }

    // One perk: a box with a glowing icon, the perk's name and what it does, and a CHOOSE button at the bottom.
    static Card MakeCard(Transform parent, int number)
    {
        GameObject card = NewUI("PerkOption" + number, parent);
        Rounded(card, CardColor);

        Image halo = NewImage(card.transform, "Glow", glow, new Color(Gold.r, Gold.g, Gold.b, 0.35f));
        Place(halo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(210f, 210f));

        Image icon = NewImage(card.transform, "Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(IconSize, IconSize));

        TextMeshProUGUI name = MakeText(card.transform, "PerkName", "Perk", 36f, Gold, TextAlignmentOptions.Center);
        Place(name.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(330f, 48f));
        name.fontStyle = FontStyles.Bold;

        TextMeshProUGUI description = MakeText(card.transform, "PerkDescription", "What it does", 25f, SoftWhite,
                                               TextAlignmentOptions.Top);
        RectTransform d = description.rectTransform;
        d.anchorMin = d.anchorMax = d.pivot = new Vector2(0.5f, 1f);   // hangs down from under the name
        d.anchoredPosition = new Vector2(0f, -232f);
        d.sizeDelta = new Vector2(320f, 100f);
        description.enableAutoSizing = true;   // a long description shrinks to fit
        description.fontSizeMin = 18f;
        description.fontSizeMax = 25f;

        // The button Marko's script clicks: ButtonFX makes it grow, turn gold and spark (like the title screen).
        GameObject button = NewUI("ChooseButton", card.transform);
        Place(button, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(250f, 62f));
        Image back = Rounded(button, ButtonColor);
        Button choose = button.AddComponent<Button>();
        choose.targetGraphic = back;
        TextMeshProUGUI label = MakeText(button.transform, "Label", "CHOOSE", 28f, Color.white, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 0f, 0f, 0f, 0f);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 4f;
        ButtonFX fx = button.AddComponent<ButtonFX>();
        SerializedObject so = new SerializedObject(fx);   // the names in quotes must match ButtonFX's fields
        SerializedProperty sparks = so.FindProperty("sparks");
        if (sparks != null) sparks.intValue = 14;
        SerializedProperty dimOthers = so.FindProperty("dimOthers");
        if (dimOthers != null) dimOthers.boolValue = false;   // the buttons are in different cards
        so.ApplyModifiedPropertiesWithoutUndo();

        return new Card { choose = choose, icon = icon, name = name, description = description, chooseLabel = label };
    }

    // REROLL, bottom right of the window (PerkPanelInfo shows it only for a box that allows rerolls).
    static void MakeReroll(Transform window, out Button button, out TextMeshProUGUI label)
    {
        GameObject go = NewUI("RerollButton", window);
        Place(go, new Vector2(1f, 0f), new Vector2(-175f, 62f), new Vector2(290f, 56f));
        Image back = Rounded(go, ButtonColor);
        button = go.AddComponent<Button>();
        button.targetGraphic = back;
        label = MakeText(go.transform, "Label", "REROLL", 24f, Color.white, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 8f, 0f, 8f, 0f);
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;   // "NOTHING NEW TO ROLL" shrinks to fit
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;
        ButtonFX fx = go.AddComponent<ButtonFX>();
        SerializedObject so = new SerializedObject(fx);   // the names in quotes must match ButtonFX's fields
        SerializedProperty dimOthers = so.FindProperty("dimOthers");
        if (dimOthers != null) dimOthers.boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);   // PerkPanelInfo shows it at a box that can reroll
    }

    // The cards' row: three cards fill it, one or two keep the same width in the middle (a box with fewer perks left).
    static void FitCards(Transform cards)
    {
        HorizontalLayoutGroup row = cards.GetComponent<HorizontalLayoutGroup>();
        if (row == null) row = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = CardSpacing;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        foreach (Transform card in cards)
        {
            LayoutElement size = card.GetComponent<LayoutElement>();
            if (size == null) size = card.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = CardWidth;
            size.flexibleWidth = 0f;
        }
    }

    // Brings a PerkSelectCanvas prefab built before the loot boxes up to date, in place (so every scene's copy follows):
    // the cards keep their width when fewer show, the REROLL button, and PerkPanelInfo's title / line / reroll fields.
    // Returns a report for the Console.
    public static string UpdatePrefab()
    {
        string path = PrefabPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            return "no perk panel yet: run Tools > UI > Build Perk Panel (it comes with the REROLL button)";

        coin = IconArt.Coin();
        box = IconArt.RoundedBox();
        glow = IconArt.Glow();
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            PerkPanelInfo info = root.GetComponentInChildren<PerkPanelInfo>(true);
            Transform window = FindDeep(root.transform, "Window");
            if (info == null || window == null) return "the perk panel has no PerkPanelInfo or Window: run Tools > UI > Build Perk Panel";

            Transform cards = FindDeep(window, "Cards");
            if (cards != null) FitCards(cards);

            Button reroll;
            TextMeshProUGUI rerollLabel;
            Transform old = window.Find("RerollButton");
            if (old == null) MakeReroll(window, out reroll, out rerollLabel);
            else
            {
                reroll = old.GetComponent<Button>();
                rerollLabel = old.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            Transform title = FindDeep(window, "Title");
            Transform line = FindDeep(window, "Line");
            SerializedObject p = new SerializedObject(info);   // the names in quotes must match PerkPanelInfo's fields
            p.FindProperty("titleText").objectReferenceValue = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            p.FindProperty("titleLine").objectReferenceValue = line != null ? line.GetComponent<Image>() : null;
            p.FindProperty("rerollButton").objectReferenceValue = reroll;
            p.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
            p.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return "perk panel updated (" + path + "): REROLL button, the box's name as the title, cards keep their width";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The first object with this name under "parent" (any depth), or null.
    static Transform FindDeep(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName) return child;
            Transform found = FindDeep(child, objectName);
            if (found != null) return found;
        }
        return null;
    }

    // Gives each Perk Data (Marko's, e.g. Assets/Config/Perks) without an icon the matching picture. Returns a report.
    static string GiveIconsToPerks()
    {
        List<string> given = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:PerkData"))
        {
            PerkData perk = AssetDatabase.LoadAssetAtPath<PerkData>(AssetDatabase.GUIDToAssetPath(guid));
            if (perk == null || perk.icon != null) continue;   // an icon already chosen stays
            Sprite icon = IconFor(perk.perkType);
            if (icon == null) continue;

            Undo.RecordObject(perk, ToolName);
            perk.icon = icon;
            EditorUtility.SetDirty(perk);
            given.Add(perk.name);
        }
        AssetDatabase.SaveAssets();
        return given.Count > 0 ? "Icons given to " + string.Join(", ", given) + "." : "Every Perk Data already had an icon.";
    }

    static Sprite IconFor(ChestPerkType type) => IconArt.ForPerk(type);

    // ----- Pieces -----

    // Puts a UI object with its middle at "position" from the "anchor" point of its parent, this big.
    static void Place(GameObject go, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    // A box with round corners (IconArt's 9-sliced picture), in this color.
    static Image Rounded(GameObject go, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = box;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    static Image NewImage(Transform parent, string objectName, Sprite sprite, Color color)
    {
        Image image = NewUI(objectName, parent).AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;   // pictures never block clicks
        return image;
    }

    static TextMeshProUGUI MakeText(Transform parent, string objectName, string text, float size, Color color,
                                    TextAlignmentOptions alignment)
    {
        TextMeshProUGUI t = AddText(NewUI(objectName, parent), text, size, alignment);
        t.color = color;
        return t;
    }

    // A one-line text in the price row (as wide as its words).
    static TextMeshProUGUI RowText(Transform parent, string objectName, string text, float size, Color color)
    {
        TextMeshProUGUI t = MakeText(parent, objectName, text, size, color, TextAlignmentOptions.Center);
        t.enableWordWrapping = false;
        return t;
    }

    static void RowCoin(Transform parent, string objectName)
    {
        Image icon = NewImage(parent, objectName, coin, Color.white);
        icon.preserveAspect = true;
        LayoutElement size = icon.gameObject.AddComponent<LayoutElement>();
        size.preferredWidth = size.preferredHeight = 36f;
    }

    // Fills a 3-slot list field (e.g. Marko's perkButtons) with one part from each card.
    static void SetArray(SerializedObject so, string field, Card[] cards, System.Func<Card, Object> part)
    {
        SerializedProperty list = so.FindProperty(field);
        list.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = part(cards[i]);
    }
}
