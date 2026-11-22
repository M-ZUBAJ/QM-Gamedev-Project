/*
First Date: 19/11/2026
Date: 22/11/2026
Author: Tony
Use by: The crafting list's row prefab (Assets/Prefabs/UI/Crafting/CraftingRow.prefab, made by Tools > UI > Build Crafting
        Menu). Turki's CraftingMenu copies it for every item in its list and calls Show(item) (a [Tony] line).
Unity Editor: Nothing to connect: it finds its parts by NAME: "Icon" (Image), "Name" (text), "Ingredients" (a row with a
              switched-off "Ingredient" template inside: an "Icon" image and an "Amount" text), "Status" (text),
              "Owned" (the green tick inside Icon; a row built before 21/11/2026 has none) and "Better" (the blue up
              arrow inside Icon; a row built before 22/11/2026 has none, but still gets the words): run Tools > UI >
              Build Crafting Menu to get them.
              Testing: Play, open the inventory's Crafting tab (or press C): each item shows its picture and its recipe
              as pictures with amounts, green when you have enough and red when not. Craft something: they update.
Purpose: One row of the crafting list, with pictures: the item's icon and name ("x2" when one craft makes two), and each
         ingredient as its icon with "have / need" and its name, e.g. "1/2 Plank", green when the inventory has enough
         and red when not. While something is missing the whole row is faded; when everything is there it says CRAFT.
         It updates by itself whenever the inventory or the worn armor changes (e.g. right after crafting). Clicking it
         still crafts (Turki's CraftingMenu does that). With tabs on the list (CraftingTabs), it hides itself when the
         picked tab isn't its item's kind; with set panels (CraftingSetPanels), a new list gets its armor grouped by set.
         An armor upgrade's recipe counts the piece the player wears, e.g. "1/1 Iron Helmet (worn)": crafting takes it
         off and puts the new piece on in its place (PlayerArmor).
         An armor piece the player already has gets a green tick on its picture and "(owned)" after its name ("(worn)"
         while it's worn), so it isn't crafted twice by mistake; it can still be crafted.
         A weaker armor piece, while the player has a better one for the same slot (more Protection, worn or in the
         inventory: PlayerArmor.BetterOwned), gets a blue up arrow on its picture instead and, in blue, "(wearing Iron
         Helmet)" or "(you have Iron Helmet)" after its name; it can still be crafted too.
Requirement: CraftingRow.cs x-> ItemData.cs, Recipe.cs, PlayerInventory.cs
             CraftingRow.cs o-> CraftingTabs.cs, CraftingSetPanels.cs, PlayerArmor.cs
             CraftingRow.cs <-x CraftingMenu.cs, CraftingMenuBuilder.cs, CraftingSetPanels.cs, CraftingTabs.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   // Image, Graphic

[DisallowMultipleComponent]
public class CraftingRow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("An ingredient's text while the inventory has enough of it.")]
    [SerializeField] Color haveColor = new Color(0.1f, 0.55f, 0.25f);

    [Tooltip("An ingredient's text while the inventory has too few.")]
    [SerializeField] Color missingColor = new Color(0.82f, 0.2f, 0.18f);

    [Tooltip("How see-through the row is while something is missing (1 = not faded).")]
    [Range(0.2f, 1f)]
    [SerializeField] float missingAlpha = 0.6f;

    [Tooltip("The words on the right when everything is there.")]
    [SerializeField] string craftText = "CRAFT";

    [Tooltip("After an armor piece's name while the player has it in the inventory (with the green tick).")]
    [SerializeField] string ownedText = "(owned)";

    [Tooltip("After an armor piece's name while the player wears it (with the green tick).")]
    [SerializeField] string wornText = "(worn)";

    [Tooltip("After a weaker armor piece's name while the player wears a better one for that slot (with the blue arrow). " +
             "{0} = the better piece's name.")]
    [SerializeField] string betterWornText = "(wearing {0})";

    [Tooltip("After a weaker armor piece's name while the inventory has a better one for that slot (with the blue arrow). " +
             "{0} = the better piece's name.")]
    [SerializeField] string betterOwnedText = "(you have {0})";

    [Tooltip("The color of those words (the blue arrow's blue).")]
    [SerializeField] Color betterColor = new Color(0.14f, 0.4f, 0.82f);

    ItemData item;               // what this row makes (null = nothing yet)
    Image icon;
    TMP_Text nameText;
    Transform ingredients;
    GameObject template;         // the switched-off "Ingredient" copied for each ingredient
    TMP_Text status;
    GameObject ownedMark;        // the green tick on the picture (an armor piece the player has)
    GameObject betterMark;       // the blue up arrow on the picture (the player has a better piece for that slot)
    string nameWords = "";       // the item's name (and "x2"), before "(owned)"
    CanvasGroup group;
    bool partsFound;
    readonly List<Ingredient> shownIngredients = new List<Ingredient>();
    readonly List<TMP_Text> amounts = new List<TMP_Text>();   // one per ingredient, in the same order

    public ItemData Item => item;   // what this row makes (CraftingTabs sorts the rows into tabs by it)

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // e.g. right after crafting, or picking something up
        PlayerArmor.Changed += Refresh;       // a worn piece counts toward an armor upgrade's recipe
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
        PlayerArmor.Changed -= Refresh;
    }

    // ===== My methods =====

    // Turki's CraftingMenu calls this for each item in its list: the picture, the name and the recipe.
    public void Show(ItemData shown)
    {
        item = shown;
        FindParts();
        if (item == null) return;

        if (icon != null)
        {
            Sprite picture = item.GetIcon();
            icon.sprite = picture;
            icon.enabled = picture != null;
        }
        nameWords = item.DisplayName + (item.CraftAmount > 1 ? " x" + item.CraftAmount : "");
        if (nameText != null) nameText.text = nameWords;
        MakeIngredients();
        Refresh();

        // The list's tabs (All, Materials, Food, Tools, Armor): hidden if the picked tab isn't this item's kind.
        CraftingTabs tabs = GetComponentInParent<CraftingTabs>(true);   // true: the panel may still be hidden
        if (tabs != null) tabs.Filter(this);

        // The armor set panels: the list was made again, so its armor is grouped again.
        CraftingSetPanels sets = GetComponentInParent<CraftingSetPanels>(true);
        if (sets != null) sets.MarkDirty();
    }

    // One small picture + text for every ingredient of the recipe, copied from the template.
    void MakeIngredients()
    {
        shownIngredients.Clear();
        amounts.Clear();
        if (ingredients == null || template == null) return;

        foreach (Transform old in ingredients)
        {
            if (old.gameObject != template) Destroy(old.gameObject);
        }
        foreach (Ingredient ingredient in item.Recipe)
        {
            if (ingredient == null || ingredient.item == null) continue;

            GameObject chip = Instantiate(template, ingredients);
            chip.name = "Ingredient " + ingredient.DisplayName;
            chip.SetActive(true);

            Transform iconPart = chip.transform.Find("Icon");
            Image chipIcon = iconPart != null ? iconPart.GetComponent<Image>() : null;
            if (chipIcon != null)
            {
                Sprite picture = ingredient.item.GetIcon();
                chipIcon.sprite = picture;
                chipIcon.enabled = picture != null;
            }
            Transform amountPart = chip.transform.Find("Amount");
            shownIngredients.Add(ingredient);
            amounts.Add(amountPart != null ? amountPart.GetComponent<TMP_Text>() : null);
        }
    }

    // How much of each ingredient the inventory has: green / red texts, CRAFT, and the fade.
    void Refresh()
    {
        if (item == null) return;
        PlayerInventory inventory = PlayerInventory.Instance;
        PlayerArmor armor = item.IsArmor ? PlayerArmor.Current : null;   // only armor upgrades use a worn piece

        bool everything = shownIngredients.Count > 0;
        for (int i = 0; i < shownIngredients.Count; i++)
        {
            Ingredient ingredient = shownIngredients[i];
            int have = inventory != null ? inventory.Count(ingredient.ItemId) : 0;
            // The armor piece the player wears counts too: crafting takes it off and puts the new piece on in its place.
            bool worn = have < ingredient.amount && armor != null && ingredient.item.IsArmor && armor.IsWorn(ingredient.ItemId);
            if (worn) have++;
            bool enough = have >= ingredient.amount;
            everything &= enough;

            TMP_Text amount = amounts[i];
            if (amount == null) continue;
            amount.text = have + "/" + ingredient.amount + " " + ingredient.DisplayName + (worn ? " (worn)" : "");
            amount.color = enough ? haveColor : missingColor;
        }

        if (status != null)
        {
            status.text = everything ? craftText : "";
            status.color = haveColor;
        }
        if (group != null) group.alpha = everything ? 1f : missingAlpha;
        ShowOwned(inventory, armor);
    }

    // An armor piece the player already has, in the inventory or worn: the green tick, and "(owned)" / "(worn)" after
    // its name, in green. A weaker piece while the player has a better one for its slot: the blue arrow (instead of the
    // tick: it's the more useful thing to know) and "(wearing Iron Helmet)" / "(you have Iron Helmet)" in blue.
    // Anything else: no mark, just the name.
    void ShowOwned(PlayerInventory inventory, PlayerArmor armor)
    {
        bool worn = item.IsArmor && armor != null && armor.IsWorn(item.Id);
        bool owned = item.IsArmor && (worn || (inventory != null && inventory.Count(item.Id) > 0));
        bool betterWorn = false;
        ItemData better = item.IsArmor && armor != null ? armor.BetterOwned(item, out betterWorn) : null;

        if (betterMark != null) betterMark.SetActive(better != null);
        if (ownedMark != null) ownedMark.SetActive(owned && (better == null || betterMark == null));
        if (nameText == null) return;

        string words = "";
        Color color = haveColor;
        if (better != null)
        {
            words = (betterWorn ? betterWornText : betterOwnedText).Replace("{0}", better.DisplayName);
            color = betterColor;
        }
        else if (owned) words = worn ? wornText : ownedText;

        nameText.text = !string.IsNullOrEmpty(words)
            ? nameWords + "  <size=75%><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + words + "</color></size>"
            : nameWords;
    }

    // The row's parts, found once by name.
    void FindParts()
    {
        if (partsFound) return;
        partsFound = true;

        Transform part = transform.Find("Icon");
        icon = part != null ? part.GetComponent<Image>() : null;
        part = transform.Find("Name");
        nameText = part != null ? part.GetComponent<TMP_Text>() : null;
        part = transform.Find("Status");
        status = part != null ? part.GetComponent<TMP_Text>() : null;
        part = icon != null ? icon.transform.Find("Owned") : null;
        if (part == null) part = transform.Find("Owned");
        ownedMark = part != null ? part.gameObject : null;
        if (ownedMark != null) ownedMark.SetActive(false);
        part = icon != null ? icon.transform.Find("Better") : null;
        if (part == null) part = transform.Find("Better");
        betterMark = part != null ? part.gameObject : null;
        if (betterMark != null) betterMark.SetActive(false);
        ingredients = transform.Find("Ingredients");
        Transform templatePart = ingredients != null ? ingredients.Find("Ingredient") : null;
        template = templatePart != null ? templatePart.gameObject : null;
        if (template != null) template.SetActive(false);

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        // Only the row's own box catches the click (so the whole row is one button).
        Graphic box = GetComponent<Graphic>();
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic != box) graphic.raycastTarget = false;
        }
    }
}
