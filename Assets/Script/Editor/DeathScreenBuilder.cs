/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Upgrade Death Screen (it asks first). It changes Assets/Prefabs/UI/Dead/DeadCanvas.prefab,
              so the death page in GameScene and in the new world (ProceduralGenerateScene) both change; nothing to
              save in the scenes. Run it again any time: the parts it adds are made again, the parts made by hand
              (BackgroundPanel, TitleText, Status, RespawnButton) keep their look.
              Guide: Docs/Death Screen Setup.md
Purpose: Adds to the GAME OVER page (the death page made by hand, DeathScreen on it):
           - DaysText under GAME OVER: how long the player survived ("YOU SURVIVED 2 DAYS", and smaller the hours and
             the day and time they died), in gold;
           - StatsToggle under the stats: a pill button, SHOW ALL STATS / MINIMISE;
           - StatsPanel (hidden until SHOW ALL STATS): everything about the character at the moment of death, like the
             inventory's Info tab: THIS LIFE (what they did: LifeText), STATUS (health, hunger, stamina, armor, speeds,
             jump, fireball, coins, with what perks and armor added) and PERKS (every perk with its picture and
             stats). STATUS and PERKS are made by the Info tab's own builder (InventoryUIBuilder.FillInfoPage), with
             its script PlayerInfoPage set to Last Life;
           - MainMenuButton: a copy of RespawnButton beside it, "Main Menu" (back to the title screen).
         So that it all fits a 1920 x 1080 screen, the stats box (Status) now hangs down from under the days (it grows
         with its text), and the two buttons sit side by side a little lower.
Requirement: DeathScreenBuilder.cs x-> DeathScreen.cs, PlayerInfoPage.cs, InventoryUIBuilder.cs, IconArt.cs, ButtonFX.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildHelper;    // NewUI, Stretch, AddText, CheckTMP, FindPrefabPath

public static class DeathScreenBuilder
{
    // ===== Fields =====
    const string ToolName = "Upgrade Death Screen";
    const string PrefabPath = "Assets/Prefabs/UI/Dead/DeadCanvas.prefab";

    // The names DeathScreen finds (keep them the same as its own).
    const string PageName = "BackgroundPanel";
    const string TitleName = "TitleText";
    const string StatusTextName = "StatusText";
    const string RespawnName = "RespawnButton";
    const string DaysName = "DaysText";
    const string ToggleName = "StatsToggle";
    const string PanelName = "StatsPanel";
    const string LifeName = "LifeText";
    const string MainMenuName = "MainMenuButton";

    // Where things go, in the canvas's units (its Canvas Scaler: 1920 x 1080), up from the middle of the screen. The page
    // made by hand has GAME OVER at y 410 and the buttons anchored to the bottom.
    const float DaysY = 318f;
    static readonly Vector2 DaysSize = new Vector2(1100f, 86f);
    const float ContentTop = 262f;                                    // the stats box and the panel hang down from here
    static readonly Vector2 PanelSize = new Vector2(1530f, 700f);    // THIS LIFE 340 + STATUS 640 + PERKS 430 + gaps
    const float PanelScale = 0.75f;                                   // drawn at 75 %: 1148 x 525, between the days and the toggle
    const float LifeWidth = 340f;
    const float Gap = 30f;                                            // the panel's margins and the gap between its columns
    const float ToggleY = -305f;
    static readonly Vector2 ToggleSize = new Vector2(380f, 54f);
    const float ButtonsY = 105f;      // the buttons' middles, up from the bottom of the screen
    const float ButtonsApart = 220f;  // from one button's middle to the other's

    static readonly Color DaysColor = new Color(1f, 0.84f, 0.35f);           // gold
    static readonly Color PanelColor = new Color(0.96f, 0.96f, 0.97f);       // like the inventory window
    static readonly Color RowColor = new Color(0.92f, 0.93f, 0.95f);         // like the Info tab's rows
    static readonly Color DarkText = new Color(0.13f, 0.13f, 0.15f);
    static readonly Color DimText = new Color(0.42f, 0.43f, 0.48f);
    static readonly Color ToggleColor = new Color(1f, 1f, 1f, 0.16f);         // a see-through pill (gold under the mouse)

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Upgrade Death Screen", false, MenuOrder.UpgradeDeathScreen)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        string path = FindPrefabPath(PrefabPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no DeadCanvas prefab (" + PrefabPath + "): make the death page " +
                                        "first (Docs/Death Screen Setup.md).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "This changes the death page, " + path + " (GameScene's and the new world's):\n\n" +
                "- under GAME OVER: how long the player survived (\"YOU SURVIVED 2 DAYS\")\n" +
                "- a SHOW ALL STATS / MINIMISE button, and the panel it opens: this life's numbers, the character's stats " +
                "and perks at the moment of death, like the inventory's Info tab\n" +
                "- a Main Menu button beside Respawn (a copy of it)\n" +
                "- the stats box now hangs down from under the days, and the buttons sit side by side a little lower\n\n" +
                "Your title, stats box and Respawn button keep their look. Continue?", "Upgrade", "Cancel"))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        string report;
        try
        {
            report = Upgrade(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log(ToolName + ": " + path + " upgraded (" + report + "). Test: Play, PlayerHealth > ⋮ > Test: Die.");
    }

    // ===== My methods =====

    // Adds (or makes again) the new parts and moves the stats box and the buttons. Returns what it did.
    static string Upgrade(GameObject root)
    {
        if (root.GetComponent<DeathScreen>() == null) root.AddComponent<DeathScreen>();
        Transform page = FindDeep(root.transform, PageName);
        if (page == null) page = root.transform;
        foreach (string made in new[] { DaysName, ToggleName, PanelName, MainMenuName }) Remove(root.transform, made);

        TMP_Text title = FindDeep(root.transform, TitleName)?.GetComponent<TMP_Text>();
        TMP_FontAsset font = title != null ? title.font : null;   // the page's own letters
        float middleX = title != null ? ((RectTransform)title.transform).anchoredPosition.x : 0f;

        MakeDays(page, title != null ? title.transform.GetSiblingIndex() + 1 : page.childCount, font, middleX);
        Transform box = MoveStatus(root.transform);
        int afterBox = box != null && box.parent == page ? box.GetSiblingIndex() + 1 : page.childCount;
        MakePanel(page, afterBox);
        MakeToggle(page, afterBox + 1, font);
        string buttons = MakeMainMenu(root.transform);
        return "days, SHOW ALL STATS panel, " + (box != null ? "stats box moved, " : "no stats box found, ") + buttons;
    }

    // "YOU SURVIVED 2 DAYS" under GAME OVER (DeathScreen writes the real words when the player dies).
    static void MakeDays(Transform page, int index, TMP_FontAsset font, float middleX)
    {
        GameObject go = NewUI(DaysName, page);
        go.transform.SetSiblingIndex(Mathf.Min(index, page.childCount - 1));
        Place(go, new Vector2(middleX, DaysY), DaysSize, new Vector2(0.5f, 0.5f));
        TextMeshProUGUI text = AddText(go, "YOU SURVIVED 2 DAYS\n<size=60%>and 6 hours  ·  died on Day 3 at 14:30</size>",
                                       44f, TextAlignmentOptions.Center);
        if (font != null) text.font = font;
        text.color = DaysColor;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3f;
        text.enableWordWrapping = false;
    }

    // The stats box (StatusText's parent, named Status) hangs down from ContentTop, under the days: it grows with its
    // text, so it can't run into them. Returns it (null if the page has none).
    static Transform MoveStatus(Transform root)
    {
        Transform text = FindDeep(root, StatusTextName);
        if (text == null || text.parent == null || text.parent.name != "Status") return null;
        RectTransform box = (RectTransform)text.parent;
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 1f);   // its top stays put; FitToText changes its height
        box.anchoredPosition = new Vector2(box.anchoredPosition.x, ContentTop);
        return box;
    }

    // SHOW ALL STATS' panel: a light box like the inventory window with THIS LIFE on the left and the Info tab's STATUS
    // and PERKS (InventoryUIBuilder.FillInfoPage, PlayerInfoPage set to Last Life). Hidden until the toggle shows it.
    static void MakePanel(Transform page, int index)
    {
        GameObject panel = NewUI(PanelName, page);
        panel.transform.SetSiblingIndex(Mathf.Min(index, page.childCount - 1));
        Place(panel, new Vector2(0f, ContentTop), PanelSize, new Vector2(0.5f, 1f));
        panel.transform.localScale = Vector3.one * PanelScale;
        Rounded(panel, PanelColor);
        panel.AddComponent<CanvasGroup>();   // DeathScreen fades it in

        // THIS LIFE: what the player did (DeathScreen writes PlayerStats' numbers into LifeText).
        GameObject life = NewUI("LifeColumn", panel.transform);
        RectTransform lifeRect = (RectTransform)life.transform;
        lifeRect.anchorMin = Vector2.zero;
        lifeRect.anchorMax = new Vector2(0f, 1f);
        lifeRect.pivot = new Vector2(0f, 1f);
        lifeRect.offsetMin = new Vector2(Gap, Gap);
        lifeRect.offsetMax = new Vector2(Gap + LifeWidth, -Gap);
        VerticalLayoutGroup column = life.AddComponent<VerticalLayoutGroup>();
        column.spacing = 6f;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        Title(life.transform, "LifeTitle", "THIS LIFE");
        GameObject lifeBox = NewUI("LifeBox", life.transform);
        lifeBox.AddComponent<LayoutElement>().flexibleHeight = 1f;
        Rounded(lifeBox, RowColor);
        TextMeshProUGUI lifeText = AddText(NewUI(LifeName, lifeBox.transform), "Distance walked: 0 m", 24f, TextAlignmentOptions.TopLeft);
        Stretch(lifeText.gameObject, 18f, 16f, 14f, 16f);
        lifeText.color = DarkText;
        lifeText.lineSpacing = 18f;
        lifeText.enableAutoSizing = true;   // a long line shrinks to fit the column
        lifeText.fontSizeMin = 14f;
        lifeText.fontSizeMax = 24f;

        // STATUS and PERKS: the Info tab's own, its script kept at the moment of death (Last Life).
        GameObject info = NewUI("Info", panel.transform);
        Stretch(info, Gap + LifeWidth + Gap, Gap, Gap, Gap);
        PlayerInfoPage stats = InventoryUIBuilder.FillInfoPage(info.transform);
        SerializedObject so = new SerializedObject(stats);   // the name in quotes must match PlayerInfoPage's field
        so.FindProperty("lastLife").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        Transform none = info.transform.Find("PerkList/NoPerks");
        TMP_Text noneText = none != null ? none.GetComponent<TMP_Text>() : null;
        if (noneText != null) noneText.text = "No perks this life.";
        ScrollPerks(info.transform);

        panel.SetActive(false);   // SHOW ALL STATS shows it
    }

    // The PERKS column inside a scroll view (the mouse wheel scrolls it), so a long life's many perks stay inside the
    // panel instead of running out under it. PlayerInfoPage still finds PerkList by its name.
    static void ScrollPerks(Transform info)
    {
        RectTransform list = info.Find("PerkList") as RectTransform;
        if (list == null) return;

        GameObject view = NewUI("PerkScroll", info);
        RectTransform viewRect = (RectTransform)view.transform;
        viewRect.anchorMin = list.anchorMin;   // where the column was
        viewRect.anchorMax = list.anchorMax;
        viewRect.pivot = list.pivot;
        viewRect.anchoredPosition = list.anchoredPosition;
        viewRect.sizeDelta = list.sizeDelta;
        Image catcher = view.AddComponent<Image>();
        catcher.color = Color.clear;   // invisible, but it catches the mouse wheel for the scroll view
        view.AddComponent<RectMask2D>();

        list.SetParent(viewRect, false);
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.anchoredPosition = Vector2.zero;
        list.sizeDelta = Vector2.zero;
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = view.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.viewport = viewRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
    }

    // The pill under the stats: SHOW ALL STATS / MINIMISE (DeathScreen changes the words), gold under the mouse.
    static void MakeToggle(Transform page, int index, TMP_FontAsset font)
    {
        GameObject toggle = NewUI(ToggleName, page);
        toggle.transform.SetSiblingIndex(Mathf.Min(index, page.childCount - 1));
        Place(toggle, new Vector2(0f, ToggleY), ToggleSize, new Vector2(0.5f, 0.5f));
        Image back = Rounded(toggle, ToggleColor);
        back.raycastTarget = true;   // it's clicked
        Button button = toggle.AddComponent<Button>();
        button.targetGraphic = back;

        TextMeshProUGUI label = AddText(NewUI("Label", toggle.transform), "SHOW ALL STATS", 26f, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 0f, 0f, 0f, 0f);
        if (font != null) label.font = font;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 4f;

        ButtonFX fx = toggle.AddComponent<ButtonFX>();
        SerializedObject so = new SerializedObject(fx);   // the names in quotes must match ButtonFX's fields
        so.FindProperty("hoverScale").floatValue = 1.06f;
        so.FindProperty("dimOthers").boolValue = false;   // the rest of the page stays as it is
        so.FindProperty("sparks").intValue = 8;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Main Menu: a copy of RespawnButton (its look, its size), beside it; the two sit side by side near the bottom.
    // Returns what it did.
    static string MakeMainMenu(Transform root)
    {
        Transform respawn = FindDeep(root, RespawnName);
        if (respawn == null) return "no RespawnButton, so no Main Menu button";

        RectTransform respawnRect = (RectTransform)respawn;
        respawnRect.anchoredPosition = new Vector2(-ButtonsApart / 2f, ButtonsY);

        GameObject menu = Object.Instantiate(respawn.gameObject, respawn.parent);
        menu.name = MainMenuName;
        menu.transform.SetSiblingIndex(respawn.GetSiblingIndex() + 1);
        ((RectTransform)menu.transform).anchoredPosition = new Vector2(ButtonsApart / 2f, ButtonsY);
        TMP_Text label = menu.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "Main Menu";
            label.gameObject.name = "MainMenuText";
        }
        else
        {
            Text legacy = menu.GetComponentInChildren<Text>(true);
            if (legacy != null) legacy.text = "Main Menu";
        }
        return "Main Menu button added beside Respawn";
    }

    // A column title like the Info tab's ("THIS LIFE"): small, bold, spaced out, grey.
    static void Title(Transform column, string objectName, string words)
    {
        GameObject go = NewUI(objectName, column);
        go.AddComponent<LayoutElement>().preferredHeight = 40f;
        TextMeshProUGUI text = AddText(go, words, 26f, TextAlignmentOptions.BottomLeft);
        text.color = DimText;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
    }

    // Puts a UI object with its pivot at "at" from the middle of its parent, this big.
    static void Place(GameObject go, Vector2 at, Vector2 size, Vector2 pivot)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
    }

    // A box with round corners (IconArt's 9-sliced picture), in this color.
    static Image Rounded(GameObject go, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = IconArt.RoundedBox();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Deletes every child with this name anywhere under "parent" (a part this tool made before).
    static void Remove(Transform parent, string childName)
    {
        for (Transform found = FindDeep(parent, childName); found != null; found = FindDeep(parent, childName))
        {
            Object.DestroyImmediate(found.gameObject);
        }
    }

    // A child (or grandchild...) with this name, or null.
    static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
