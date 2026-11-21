/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The crafting list's Panel (Turki's CraftingCanvas > Panel), next to CraftingTabs. Tools > UI > Build Crafting
        Menu (and Tools > Items > Build Plate Armor) put it there, with the panel's look: the switched-off Scroll >
        SetPanel (a tinted box with Header > Stripe, Title, Info and Progress).
Unity Editor: Nothing to connect: it finds the list (the scroll list's Content) and the SetPanel look by name. Each
              set's name, color and bonus come from PlayerArmor's Set Bonuses (on the Player).
              Testing: Play, Tab > Crafting > Armor (or All): the Copper pieces sit in one copper box and the Iron
              pieces in a steel-grey one, each with how much the whole set protects, what wearing all six
              gives, and how many are worn ("BONUS ON" in gold while the whole set is worn). Pick Tools: the boxes go.
Purpose: Groups the crafting list's armor by set, so a player sees at a glance which pieces make a set and what the
         set is worth: every run of rows whose items are armor of the same set (Armor > Set) is put inside one panel
         in the set's color, with a header:
           IRON SET                                                    Worn 2/6
           10% protection - all 6: stamina recharges 50% faster
         Turki's CraftingMenu makes the list again every time it opens; its new rows ask for a new grouping
         (CraftingRow.Show calls MarkDirty), done in the same frame, before anything is drawn. A panel hides while the
         picked tab hides all its rows. The list is sorted set by set, helmet to boots (Build Crafting Menu's sorting).
Requirement: CraftingSetPanels.cs x-> CraftingRow.cs, ItemData.cs, ArmorStats.cs, PlayerArmor.cs, PlayerInventory.cs
             CraftingSetPanels.cs <-x CraftingMenuBuilder.cs
             CraftingSetPanels.cs <-o CraftingRow.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CraftingSetPanels : MonoBehaviour
{
    // One set's panel and the rows inside it.
    class Group
    {
        public GameObject panel;
        public string set;
        public Image back, stripe;
        public TMP_Text title, info, progress;
        public readonly List<CraftingRow> rows = new List<CraftingRow>();
    }

    // ===== Fields =====
    [Tooltip("How strongly a panel's background takes its set's color (0 = white, 1 = the set's color).")]
    [Range(0f, 1f)]
    [SerializeField] float tint = 0.3f;

    [Tooltip("What the panel says on the right while the whole set is worn and its bonus is on.")]
    [SerializeField] string bonusOnText = "BONUS ON";

    [Tooltip("That text's color (gold, like the Gear page's set bonus).")]
    [SerializeField] Color bonusColor = new Color(0.8f, 0.5f, 0.02f);

    [Tooltip("The worn count's color before that (\"Worn 2/6\").")]
    [SerializeField] Color countColor = new Color(0.36f, 0.37f, 0.42f);

    const string TemplateName = "SetPanel";   // Scroll > SetPanel: the look each panel is copied from

    RectTransform content;   // the list the rows are in (Turki's Rows)
    GameObject template;
    readonly List<Group> groups = new List<Group>();
    bool regroup;            // new rows came: group them (in LateUpdate, once they're all there)
    bool textsChanged;       // the worn armor or the inventory changed: the headers say it again

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
        content = scroll != null ? scroll.content : null;
        Transform look = scroll != null ? scroll.transform.Find(TemplateName) : null;
        template = look != null ? look.gameObject : null;
        if (template != null) template.SetActive(false);
    }

    void OnEnable()
    {
        PlayerArmor.Changed += TextsChanged;
        PlayerInventory.Changed += TextsChanged;
        regroup = true;   // the list may have been made again while this was hidden
    }

    void OnDisable()
    {
        PlayerArmor.Changed -= TextsChanged;
        PlayerInventory.Changed -= TextsChanged;
    }

    void LateUpdate()
    {
        if (regroup)
        {
            regroup = false;
            Regroup();
        }
        ShowPanels();
        if (!textsChanged) return;
        textsChanged = false;
        foreach (Group group in groups)
        {
            if (group.panel != null) Fill(group);
        }
    }

    // ===== My methods =====

    // A new row asks this (CraftingRow.Show): the list was made again, so its armor is grouped again.
    public void MarkDirty()
    {
        regroup = true;
    }

    void TextsChanged()
    {
        textsChanged = true;
    }

    // Puts every run of rows next to each other whose items are armor of the same set into one panel. Turki's list
    // removes all its old rows (and with them the old panels) when it's made again, so only rows straight in the list
    // are grouped; rows already in a panel stay there.
    void Regroup()
    {
        groups.RemoveAll(group => group.panel == null);   // panels the list removed
        if (content == null || template == null) return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in content) children.Add(child);

        List<CraftingRow> run = new List<CraftingRow>();
        string runSet = null;
        foreach (Transform child in children)
        {
            CraftingRow row = child.GetComponent<CraftingRow>();
            string set = SetOf(row);
            if (set == null || !string.Equals(set, runSet, System.StringComparison.OrdinalIgnoreCase))
            {
                Wrap(run, runSet);
                run.Clear();
                runSet = set;
            }
            if (set != null) run.Add(row);
        }
        Wrap(run, runSet);
        textsChanged = true;
    }

    // One panel (a copy of the template) where the run's first row was, with the run's rows inside it.
    void Wrap(List<CraftingRow> run, string set)
    {
        if (run.Count == 0 || set == null) return;

        GameObject panel = Instantiate(template, content);
        panel.name = "Set " + set;
        panel.transform.SetSiblingIndex(run[0].transform.GetSiblingIndex());
        Group group = new Group
        {
            panel = panel,
            set = set,
            back = panel.GetComponent<Image>(),
            stripe = Part<Image>(panel, "Header/Stripe"),
            title = Part<TMP_Text>(panel, "Header/Title"),
            info = Part<TMP_Text>(panel, "Header/Info"),
            progress = Part<TMP_Text>(panel, "Header/Progress"),
        };
        foreach (CraftingRow row in run)
        {
            row.transform.SetParent(panel.transform, false);
            group.rows.Add(row);
        }
        panel.SetActive(true);
        groups.Add(group);
    }

    // A panel shows only while one of its rows does (a tab hides the rows of other kinds).
    void ShowPanels()
    {
        foreach (Group group in groups)
        {
            if (group.panel == null) continue;
            bool any = false;
            foreach (CraftingRow row in group.rows) any |= row != null && row.gameObject.activeSelf;
            if (group.panel.activeSelf != any) group.panel.SetActive(any);
        }
    }

    // The panel's color and header: the set's name, how much all its pieces protect, what wearing all of them gives
    // (and the set it counts as), and how many are worn, or BONUS ON.
    void Fill(Group group)
    {
        PlayerArmor armor = PlayerArmor.Current;
        PlayerArmor.SetBonus bonus = armor != null ? armor.BonusFor(group.set) : null;
        Color color = bonus != null && bonus.color.a > 0f ? bonus.color : PlayerArmor.IronColor;   // no color set: steel grey
        int size = armor != null ? armor.SetSize : PlayerArmor.Slots.Length;

        if (group.back != null) group.back.color = Color.Lerp(Color.white, color, tint);
        if (group.stripe != null) group.stripe.color = color;
        if (group.title != null)
        {
            group.title.text = group.set.ToUpper() + " SET";
            group.title.color = Color.Lerp(color, Color.black, 0.45f);   // the set's color, dark enough to read
        }

        if (group.info != null)
        {
            float protection = 0f;
            foreach (CraftingRow row in group.rows)
            {
                ItemData item = row != null ? row.Item : null;
                if (item != null && item.IsArmor) protection += item.Armor.protection;
            }
            string text = ArmorStats.Percent(protection) + " protection";
            if (bonus != null) text += "  -  all " + size + ": " + bonus.Describe();
            if (bonus != null && !string.IsNullOrEmpty(bonus.countsAs)) text += "  -  counts as " + bonus.countsAs;
            group.info.text = text;
        }

        if (group.progress != null)
        {
            bool on = bonus != null && armor.ActiveBonus == bonus;
            group.progress.text = on ? bonusOnText : "Worn " + (armor != null ? armor.PiecesFor(group.set) : 0) + "/" + size;
            group.progress.color = on ? bonusColor : countColor;
        }
    }

    // The armor set a row's item belongs to, or null (not a row, not armor, or no set).
    static string SetOf(CraftingRow row)
    {
        ItemData item = row != null ? row.Item : null;
        if (item == null || !item.IsArmor || string.IsNullOrEmpty(item.Armor.set)) return null;
        return item.Armor.set;
    }

    static T Part<T>(GameObject panel, string path) where T : Component
    {
        Transform part = panel.transform.Find(path);
        return part != null ? part.GetComponent<T>() : null;
    }
}
