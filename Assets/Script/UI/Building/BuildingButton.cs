/*
First Date: 7/10/2026
Date: 23/11/2026
Author: Tony
Use by: On the BuildingButton prefab. BuildMenu makes one per building, so don't put it in the scene yourself.
        Tools > UI > Build Build Menu UI makes the prefab for you.
Unity Editor: The parts are found by name: children "Icon" (Image), "Name" (Text) and "Recipe",
              and inside Recipe a hidden "Ingredient" (with its own "Icon" and "Count") that is copied once per material.
              Enough / Missing Color = the color of a material's amount when you have enough / not enough.
Purpose: One building in the build menu: its picture, name and recipe ("Stone 2/3" = have 2, need 3).
         The amounts update as the inventory changes. The button only works when you have every material.
         It tells the build menu when the mouse comes onto it and leaves (23/11/2026), so the menu can show the
         building's info box after the mouse rests on it (also on a grey button you can't afford yet).
         An upgrade (Building Data with Upgrade Of, e.g. the Iron Bear Trap) gets a first line with the building it
         goes on: "on a Copper Bear Trap", "on an Iron Bear Trap" (with its picture), before its materials.
Requirement: BuildingButton.cs x-> BuildingData.cs, Recipe.cs, PlayerInventory.cs, UIAnimatedImage.cs, ItemIconRules.cs, Button (Unity)
             BuildingButton.cs <-x BuildMenu.cs, BuildMenuUIBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;   // the mouse coming onto / leaving the button
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // One recipe line on screen, and the material it shows.
    class Row
    {
        public Ingredient ingredient;
        public TMP_Text count;
    }

    // ===== Fields =====
    [Header("Colors")]
    [Tooltip("Color of a material's amount when you have enough.")]
    [SerializeField] Color enoughColor = Color.black;

    [Tooltip("Color of a material's amount when you don't have enough.")]
    [SerializeField] Color missingColor = new Color(0.8f, 0.1f, 0.1f);

    [Header("Upgrades")]
    [Tooltip("The first line of an upgrade's recipe (e.g. the Gold Bear Trap). {0} = the building it's built on, with " +
             "a / an in front (\"an Iron Bear Trap\").")]
    [SerializeField] string upgradeText = "on {0}";

    Button button;
    Image icon;
    TMP_Text title;
    Transform recipe;
    GameObject ingredientTemplate;   // the hidden "Ingredient" copied for every material

    BuildingData data;
    readonly List<Row> rows = new List<Row>();
    System.Action<BuildingButton> onHover;   // the build menu: the mouse came onto this button
    System.Action<BuildingButton> onLeave;   // ...and left it

    public BuildingData Data => data;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        FindParts();
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // amounts change as items are picked up or used
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
        onLeave?.Invoke(this);   // hidden (another tab, the menu closed) while the mouse was on it
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onLeave?.Invoke(this);
    }

    // ===== My methods =====

    // Finds the Icon, Name, Recipe... children. Safe to call twice.
    // (Setup can run before Awake: Unity waits with Awake while the build window is hidden.)
    void FindParts()
    {
        if (button != null) return;   // already done

        button = GetComponent<Button>();
        icon = FindChild<Image>(transform, "Icon");
        title = FindChild<TMP_Text>(transform, "Name");
        recipe = transform.Find("Recipe");

        Transform template = recipe != null ? recipe.Find("Ingredient") : null;
        if (template != null)
        {
            ingredientTemplate = template.gameObject;
            ingredientTemplate.SetActive(false);   // only the copies show
        }
    }

    // Called by BuildMenu: which building this shows, who to tell when it's clicked, and (optional) who to tell when
    // the mouse comes onto it and leaves (the info box).
    public void Setup(BuildingData building, System.Action<BuildingData> onChosen,
                      System.Action<BuildingButton> hover = null, System.Action<BuildingButton> leave = null)
    {
        FindParts();
        data = building;
        onHover = hover;
        onLeave = leave;

        if (title != null) title.text = data.DisplayName;
        if (icon != null)
        {
            UIAnimatedImage.ShowIcon(icon, data.GetIcon(), data.Icon);   // plays it if the icon is an Aseprite animation
            icon.enabled = icon.sprite != null;
        }

        MakeRows();
        button.onClick.AddListener(() => onChosen(data));
        Refresh();
    }

    // One line per material: its picture and "name have/need".
    void MakeRows()
    {
        if (recipe == null || ingredientTemplate == null) return;

        if (data.UpgradeOf != null) MakeUpgradeRow(data.UpgradeOf);   // first: the building it goes on

        foreach (Ingredient ing in data.Recipe)
        {
            if (ing == null || ing.item == null) continue;   // an empty recipe line

            GameObject row = Instantiate(ingredientTemplate, recipe);
            row.name = ing.DisplayName;
            row.SetActive(true);

            Image rowIcon = FindChild<Image>(row.transform, "Icon");
            if (rowIcon != null)
            {
                // The same picture the inventory uses for this item (a photo is remembered by Item Id),
                // or the one GameRules > Item Icons says. Label: no picture (the row's text already has the name).
                ItemIconRules.Resolve(ing.item, ing.item.GetIcon(), out Sprite still, out IconSource animation);
                UIAnimatedImage.ShowIcon(rowIcon, still, animation);
                rowIcon.enabled = rowIcon.sprite != null;
            }

            rows.Add(new Row { ingredient = ing, count = FindChild<TMP_Text>(row.transform, "Count") });
        }
    }

    // An upgrade's first line: the placed building it's built on (and replaces), e.g. "on a Copper Bear Trap".
    // It isn't counted: that building is in the world, not in the inventory.
    void MakeUpgradeRow(BuildingData builtOn)
    {
        GameObject row = Instantiate(ingredientTemplate, recipe);
        row.name = "Upgrade Of";
        row.SetActive(true);

        Image rowIcon = FindChild<Image>(row.transform, "Icon");
        if (rowIcon != null)
        {
            UIAnimatedImage.ShowIcon(rowIcon, builtOn.GetIcon(), builtOn.Icon);
            rowIcon.enabled = rowIcon.sprite != null;
        }

        TMP_Text words = FindChild<TMP_Text>(row.transform, "Count");
        if (words != null)
        {
            words.text = upgradeText.Replace("{0}", builtOn.NameWithArticle);
            words.color = enoughColor;
        }
    }

    // Updates the amounts and colors, and lets the button work only if everything is there.
    void Refresh()
    {
        if (data == null) return;
        PlayerInventory inv = PlayerInventory.Instance;

        foreach (Row row in rows)
        {
            if (row.count == null) continue;

            int have = inv != null ? inv.Count(row.ingredient.ItemId) : 0;
            int need = row.ingredient.amount;
            row.count.text = row.ingredient.DisplayName + "  " + have + "/" + need;
            row.count.color = have >= need ? enoughColor : missingColor;
        }

        button.interactable = data.CanAfford(inv);
    }

    // A direct child with this name and component, or null.
    static T FindChild<T>(Transform parent, string childName) where T : Component
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }
}
