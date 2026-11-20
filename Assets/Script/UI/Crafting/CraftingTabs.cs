/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The crafting list's Panel (Turki's CraftingCanvas > Panel), with its tab buttons in its child "Tabs".
        Tools > UI > Build Crafting Menu makes the tabs and puts this there.
Unity Editor: Nothing to connect: it finds its tabs by NAME in Panel > Tabs (All, Materials, Food, Tools, Armor, each a
              Button with a "Label" text) and the "nothing here" text in Scroll > Empty. Both crafting lists have it (the
              inventory's Crafting tab and the C menu are the same prefab).
              Testing: Play, Tab > Crafting (or press C), click Tools: only the tools are left, scrolled to the top. All
              shows everything again. Close and open the list: the tab picked stays picked.
Purpose: Tabs on top of the crafting list, so a long list is quick to look through:
           All        everything (picked at the start)
           Materials  Item Type Material, e.g. Plank
           Food       Item Type Consumable, e.g. Stew
           Tools      pickaxes, axes and swords
           Armor      helmet to boots
         Picking a tab hides the rows of the other kinds (the list closes up around the rest) and scrolls to the top.
         A tab with nothing to craft in it is faded, and picking it says "Nothing to craft here yet."
         Turki's CraftingMenu makes the list again every time it opens; each new row asks this whether it shows
         (CraftingRow.Show), so the picked tab still counts.
Requirement: CraftingTabs.cs x-> CraftingRow.cs, ItemData.cs
             CraftingTabs.cs <-x CraftingMenuBuilder.cs
             CraftingTabs.cs <-o CraftingRow.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CraftingTabs : MonoBehaviour
{
    // The tabs, left to right (their objects in Panel > Tabs have these names).
    public enum Kind { All, Materials, Food, Tools, Armor }

    // ===== Fields =====
    [Tooltip("The picked tab's color (gold, like the inventory's own tabs).")]
    [SerializeField] Color pickedColor = new Color(0.97f, 0.83f, 0.42f);

    [Tooltip("The other tabs' color.")]
    [SerializeField] Color idleColor = new Color(0.86f, 0.87f, 0.9f);

    [Tooltip("How see-through a tab is while there's nothing to craft in it (1 = not faded).")]
    [Range(0.2f, 1f)]
    [SerializeField] float emptyAlpha = 0.45f;

    static readonly Kind[] Kinds = (Kind[])System.Enum.GetValues(typeof(Kind));

    Kind picked = Kind.All;
    readonly Dictionary<Kind, Image> backs = new Dictionary<Kind, Image>();
    readonly Dictionary<Kind, TMP_Text> labels = new Dictionary<Kind, TMP_Text>();
    readonly Dictionary<Kind, CanvasGroup> fades = new Dictionary<Kind, CanvasGroup>();
    ScrollRect scroll;
    GameObject empty;          // "Nothing to craft here yet."
    int recountAfter = -1;     // the frame the rows changed in: they're counted the frame after (-1 = no need)

    public Kind Picked => picked;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Transform holder = transform.Find("Tabs");
        foreach (Kind kind in Kinds)
        {
            Transform tab = holder != null ? holder.Find(kind.ToString()) : null;
            if (tab == null) continue;

            Image back = tab.GetComponent<Image>();
            if (back != null) backs[kind] = back;
            Transform label = tab.Find("Label");
            if (label != null && label.GetComponent<TMP_Text>() is TMP_Text text) labels[kind] = text;
            CanvasGroup fade = tab.GetComponent<CanvasGroup>();
            if (fade == null) fade = tab.gameObject.AddComponent<CanvasGroup>();
            fades[kind] = fade;

            Button button = tab.GetComponent<Button>();
            Kind thisKind = kind;   // a copy for the click (the loop's own variable changes)
            if (button != null) button.onClick.AddListener(() => Pick(thisKind));
        }

        scroll = GetComponentInChildren<ScrollRect>(true);
        Transform none = scroll != null ? scroll.transform.Find("Empty") : null;
        empty = none != null ? none.gameObject : null;
        ShowTabs();
    }

    // The list may have been made again while it was hidden: count it again.
    void OnEnable()
    {
        recountAfter = Time.frameCount;
    }

    void LateUpdate()
    {
        // A frame later: the rows CraftingMenu replaced are only really gone at the end of the frame they were removed in.
        if (recountAfter < 0 || Time.frameCount <= recountAfter) return;
        recountAfter = -1;
        Recount();
    }

    // ===== My methods =====

    // Shows only the rows of this kind (All = every row), from the top of the list.
    public void Pick(Kind kind)
    {
        picked = kind;
        foreach (CraftingRow row in GetComponentsInChildren<CraftingRow>(true)) row.gameObject.SetActive(Shows(row.Item));
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        ShowTabs();
        Recount();
    }

    // A new row asks this (CraftingRow.Show): it hides itself if its item isn't of the picked kind.
    public void Filter(CraftingRow row)
    {
        row.gameObject.SetActive(Shows(row.Item));
        recountAfter = Time.frameCount;
    }

    // Is this item in the picked tab?
    public bool Shows(ItemData item)
    {
        return item != null && (picked == Kind.All || KindOf(item) == picked);
    }

    // Which tab an item belongs in (besides All), by its Item Type.
    public static Kind KindOf(ItemData item)
    {
        switch (item.Type)
        {
            case ItemType.Consumable: return Kind.Food;
            case ItemType.Tool: return Kind.Tools;
            case ItemType.Armor: return Kind.Armor;
            default: return Kind.Materials;
        }
    }

    // The picked tab gold and bold, the others grey.
    void ShowTabs()
    {
        foreach (KeyValuePair<Kind, Image> back in backs) back.Value.color = back.Key == picked ? pickedColor : idleColor;
        foreach (KeyValuePair<Kind, TMP_Text> label in labels)
        {
            label.Value.fontStyle = label.Key == picked ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    // Fades the tabs with nothing to craft in them, and says so when the picked one is empty.
    void Recount()
    {
        Dictionary<Kind, int> counts = new Dictionary<Kind, int>();
        foreach (Kind kind in Kinds) counts[kind] = 0;
        foreach (CraftingRow row in GetComponentsInChildren<CraftingRow>(true))
        {
            if (row.Item == null) continue;
            counts[KindOf(row.Item)]++;
            counts[Kind.All]++;
        }

        foreach (KeyValuePair<Kind, CanvasGroup> fade in fades) fade.Value.alpha = counts[fade.Key] > 0 ? 1f : emptyAlpha;
        if (empty != null) empty.SetActive(counts[picked] == 0);
    }
}
