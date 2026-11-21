/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open a game scene (GameScene or ProceduralGenerateScene), then menu Tools > UI > Build Perk Roll Popup, then
              save the scene. It asks before replacing the prefab. Tools > World > Build Loot Chests runs it too and puts
              the pop-up in every game scene. To change sizes or colors for every future build, edit Fields below.
              Needs TMP Essentials (it tells you if they are missing). Guide: Docs/Loot Chests Setup.md
Purpose: Builds the new chest system's roll pop-up (PerkRollUI: the random common and epic boxes) in one click, everything
         connected:
           PerkRollCanvas (PerkRollUI)
             > RollPanel (the dark backdrop: PerkRollUI switches it on and off)
               > Window > Glow, Title, Line, Reel > Icon, PerkName, PerkDescription, PriceRow (the price + your coins),
                          NeedText, BuyButton > Label, RerollButton > Label, SkipButton > Label, Hint
         It saves Assets/Prefabs/UI/Perks/PerkRollCanvas.prefab (replaced in place, so every scene's copy follows) and
         puts one in the open scene if it has none.
Requirement: PerkRollBuilder.cs x-> UIBuildHelper.cs, IconArt.cs, PerkRollUI.cs, ButtonFX.cs, MenuOrder.cs
             PerkRollBuilder.cs <-x LootChestBuilder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class PerkRollBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Perk Roll Popup";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/UI/Perks/PerkRollCanvas.prefab");
    const string CanvasName = "PerkRollCanvas";
    const int SortingOrder = 21;   // like the perk panel (20): over the HUD and the messages, under the victory screen (30)

    // The look: the same as the perk panel (dark see-through boxes, white text, gold).
    static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color WindowColor = new Color(0.07f, 0.08f, 0.11f, 0.95f);
    static readonly Color ReelColor = new Color(0.14f, 0.16f, 0.21f, 1f);
    static readonly Color ButtonColor = new Color(0.07f, 0.08f, 0.11f, 0.9f);   // ButtonFX turns it gold under the mouse
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color SoftWhite = new Color(0.86f, 0.87f, 0.92f);
    static readonly Color Dim = new Color(0.62f, 0.64f, 0.7f);
    static readonly Color Red = new Color(1f, 0.38f, 0.32f);

    // Sizes (pixels on a 1920 x 1080 screen; y = from the top or bottom of the window).
    static readonly Vector2 WindowSize = new Vector2(760f, 690f);
    const float ReelY = -215f;      // the middle of the frame the pictures roll in
    const float ReelSize = 230f;
    const float IconSize = 150f;
    const float ButtonsY = -592f;   // BUY / REROLL / SKIP
    static readonly Vector2 ButtonSize = new Vector2(290f, 62f);

    static Sprite coin, box, glow;  // the pictures for this build (IconArt)

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Perk Roll Popup", false, MenuOrder.BuildPerkRollPopup)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog(ToolName, "PerkRollCanvas.prefab already exists. Build it again and replace it?\n\n" +
                                         "Every scene's copy follows (changes made to a copy in a scene are lost).",
                                         "Replace", "Cancel"))
            return;

        GameObject prefab = SavePrefab();
        string scene = PutInOpenScene(prefab, true);
        MakeEventSystemIfMissing(ToolName);   // the buttons need one

        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log(ToolName + ": " + PrefabPath + "; " + scene + ". Save the scene (Ctrl + S). It shows in the new chest " +
                  "system (GameRules > New Chests) at a common or epic box.");
    }

    // ===== My methods =====

    // Builds the pop-up and saves it as the prefab (an existing one is replaced in place: same file, so every scene's
    // copy follows). Returns the prefab. Tools > World > Build Loot Chests calls it too.
    public static GameObject SavePrefab()
    {
        coin = IconArt.Coin();
        box = IconArt.RoundedBox();
        glow = IconArt.Glow();

        GameObject canvas = MakeCanvas();
        MakeFolders(FolderOf(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
        Object.DestroyImmediate(canvas);
        return prefab;
    }

    // Puts one copy of the prefab in the open scene if it has no PerkRollUI yet (undo = Ctrl + Z can take it out again).
    // A pop-up that isn't a copy of the prefab (made by hand) is replaced. Returns a report for the Console.
    public static string PutInOpenScene(GameObject prefab, bool undo)
    {
        if (prefab == null) return "no prefab to put in the scene";

        PerkRollUI old = Object.FindFirstObjectByType<PerkRollUI>(FindObjectsInactive.Include);
        if (old != null)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(old.gameObject);
            if (source != null && AssetDatabase.GetAssetPath(source) == AssetDatabase.GetAssetPath(prefab))
                return "the scene already has it (" + old.name + ")";

            GameObject oldRoot = PrefabUtility.IsPartOfPrefabInstance(old.gameObject)
                               ? PrefabUtility.GetOutermostPrefabInstanceRoot(old.gameObject) : old.gameObject;
            if (undo) Undo.DestroyObjectImmediate(oldRoot);
            else Object.DestroyImmediate(oldRoot);
        }

        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (undo) Undo.RegisterCreatedObjectUndo(copy, ToolName);
        return old != null ? "put in the scene in place of the old one" : "put in the scene";
    }

    static GameObject MakeCanvas()
    {
        GameObject canvas = NewCanvas(CanvasName, SortingOrder);

        // The backdrop: dims the game behind the pop-up and catches the clicks around it (a click there skips the roll).
        GameObject panel = NewUI("RollPanel", canvas.transform);
        Stretch(panel, 0f, 0f, 0f, 0f);
        panel.AddComponent<Image>().color = Backdrop;

        GameObject window = NewUI("Window", panel.transform);
        Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
        Rounded(window, WindowColor);

        // The soft light behind the frame: first in the window, so everything else is drawn over it (and not inside the
        // frame, which cuts off what sticks out).
        Image halo = NewImage(window.transform, "Glow", glow, new Color(Gold.r, Gold.g, Gold.b, 0.35f));
        Place(halo.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, ReelY), new Vector2(ReelSize * 1.5f, ReelSize * 1.5f));

        TextMeshProUGUI title = MakeText(window.transform, "Title", "COMMON BOX", 46f, Gold, TextAlignmentOptions.Center);
        Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(700f, 60f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;

        Image line = NewImage(window.transform, "Line", null, new Color(Gold.r, Gold.g, Gold.b, 0.8f));
        Place(line.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(300f, 3f));

        // The frame the pictures roll in: each picture slides down from above, and RectMask2D hides it until it's inside.
        GameObject reel = NewUI("Reel", window.transform);
        Place(reel, new Vector2(0.5f, 1f), new Vector2(0f, ReelY), new Vector2(ReelSize, ReelSize));
        Rounded(reel, ReelColor).raycastTarget = false;
        reel.AddComponent<RectMask2D>();

        Image icon = NewImage(reel.transform, "Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(IconSize, IconSize));

        TextMeshProUGUI name = MakeText(window.transform, "PerkName", "Perk", 38f, Gold, TextAlignmentOptions.Center);
        Place(name.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -362f), new Vector2(700f, 50f));
        name.fontStyle = FontStyles.Bold;

        TextMeshProUGUI description = MakeText(window.transform, "PerkDescription", "What it does", 24f, SoftWhite,
                                               TextAlignmentOptions.Top);
        RectTransform d = description.rectTransform;
        d.anchorMin = d.anchorMax = d.pivot = new Vector2(0.5f, 1f);   // hangs down from under the name
        d.anchoredPosition = new Vector2(0f, -392f);
        d.sizeDelta = new Vector2(640f, 72f);
        description.enableAutoSizing = true;   // a long description shrinks to fit
        description.fontSizeMin = 16f;
        description.fontSizeMax = 24f;

        MakePriceRow(window.transform, out TextMeshProUGUI price, out TextMeshProUGUI coins);

        TextMeshProUGUI need = MakeText(window.transform, "NeedText", "Not enough coins", 22f, Red, TextAlignmentOptions.Center);
        Place(need.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -534f), new Vector2(700f, 32f));
        need.gameObject.SetActive(false);   // PerkRollUI shows it while the player can't pay

        Button buy = MakeButton(window.transform, "BuyButton", "BUY", new Vector2(-160f, ButtonsY), ButtonSize, 14, out TextMeshProUGUI buyLabel);
        Button reroll = MakeButton(window.transform, "RerollButton", "REROLL", new Vector2(160f, ButtonsY), ButtonSize, 0, out TextMeshProUGUI rerollLabel);
        Button skip = MakeButton(window.transform, "SkipButton", "SKIP", new Vector2(0f, ButtonsY), new Vector2(240f, 62f), 0, out _);

        TextMeshProUGUI hint = MakeText(window.transform, "Hint", "E: close, or walk away", 22f, Dim, TextAlignmentOptions.Center);
        Place(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(700f, 32f));

        // The script. The names in quotes must match PerkRollUI's fields.
        PerkRollUI ui = canvas.AddComponent<PerkRollUI>();
        SerializedObject s = new SerializedObject(ui);
        s.FindProperty("panel").objectReferenceValue = panel;
        s.FindProperty("window").objectReferenceValue = window.transform;
        s.FindProperty("titleText").objectReferenceValue = title;
        s.FindProperty("titleLine").objectReferenceValue = line;
        s.FindProperty("reel").objectReferenceValue = reel.transform;
        s.FindProperty("icon").objectReferenceValue = icon;
        s.FindProperty("glow").objectReferenceValue = halo;
        s.FindProperty("perkName").objectReferenceValue = name;
        s.FindProperty("perkDescription").objectReferenceValue = description;
        s.FindProperty("priceText").objectReferenceValue = price;
        s.FindProperty("coinsText").objectReferenceValue = coins;
        s.FindProperty("needText").objectReferenceValue = need;
        s.FindProperty("hintText").objectReferenceValue = hint;
        s.FindProperty("buyButton").objectReferenceValue = buy;
        s.FindProperty("buyLabel").objectReferenceValue = buyLabel;
        s.FindProperty("rerollButton").objectReferenceValue = reroll;
        s.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
        s.FindProperty("skipButton").objectReferenceValue = skip;
        s.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);   // hidden until a random box opens it
        return canvas;
    }

    // Price (coin) 2 coins          You have (coin) 20
    static void MakePriceRow(Transform window, out TextMeshProUGUI price, out TextMeshProUGUI coins)
    {
        GameObject row = NewUI("PriceRow", window);
        Place(row, new Vector2(0.5f, 1f), new Vector2(0f, -496f), new Vector2(700f, 42f));
        HorizontalLayoutGroup line = row.AddComponent<HorizontalLayoutGroup>();
        line.childAlignment = TextAnchor.MiddleCenter;
        line.spacing = 10f;
        line.childControlWidth = line.childControlHeight = true;
        line.childForceExpandWidth = line.childForceExpandHeight = false;

        RowText(row.transform, "PriceLabel", "Price", 26f, Dim);
        RowCoin(row.transform, "PriceCoin");
        price = RowText(row.transform, "PriceText", "2 coins", 28f, Gold);
        price.fontStyle = FontStyles.Bold;
        NewUI("Gap", row.transform).AddComponent<LayoutElement>().preferredWidth = 46f;
        RowText(row.transform, "HaveLabel", "You have", 26f, Dim);
        RowCoin(row.transform, "HaveCoin");
        coins = RowText(row.transform, "CoinsText", "20", 28f, Color.white);
        coins.fontStyle = FontStyles.Bold;
    }

    // A dark rounded button with a bold label; ButtonFX makes it grow, turn gold and (sparks > 0) spark when clicked.
    static Button MakeButton(Transform parent, string objectName, string text, Vector2 position, Vector2 size, int sparks,
                             out TextMeshProUGUI label)
    {
        GameObject go = NewUI(objectName, parent);
        Place(go, new Vector2(0.5f, 1f), position, size);
        Image back = Rounded(go, ButtonColor);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = back;
        label = MakeText(go.transform, "Label", text, 26f, Color.white, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 8f, 0f, 8f, 0f);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 3f;
        label.enableAutoSizing = true;   // "NOTHING NEW TO ROLL" shrinks to fit
        label.fontSizeMin = 14f;
        label.fontSizeMax = 26f;

        ButtonFX fx = go.AddComponent<ButtonFX>();
        SerializedObject so = new SerializedObject(fx);   // the names in quotes must match ButtonFX's fields
        SerializedProperty sparkCount = so.FindProperty("sparks");
        if (sparkCount != null) sparkCount.intValue = sparks;
        SerializedProperty dimOthers = so.FindProperty("dimOthers");
        if (dimOthers != null) dimOthers.boolValue = false;   // BUY and REROLL are both important
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // ----- Pieces (the same as the perk panel's) -----

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
        size.preferredWidth = size.preferredHeight = 34f;
    }
}
