using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenu : MonoBehaviour
{
    [Tooltip("The items listed in the menu. Drag in Item Data files that have a recipe.")]
    [SerializeField] List<ItemData> craftables = new List<ItemData>();
    [Tooltip("Key that opens and closes the menu.")]
    [SerializeField] KeyCode openKey = KeyCode.C;

    [Header("UI (already filled in on the prefab)")]
    [SerializeField] GameObject panel;    // the dark box that shows and hides
    [SerializeField] Transform rows;      // the list the buttons go into
    [SerializeField] TMP_Text message;    // the line at the bottom
    [SerializeField] Button rowPrefab;    // one button, copied for each item

    bool isOpen;

    void Start()
    {
        panel.SetActive(false);   // hidden until C is pressed
    }

    void Update()
    {
        if (MenuState.KeysCaptured) return;

        if (Input.GetKeyDown(openKey))
        {
            if (isOpen) Close();
            else if (!MenuState.OthersOpen(this)) Open();   // not on top of another menu
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void OnDestroy()
    {
        MenuState.SetOpen(this, false);
    }

    void Open()
    {
        FillRows();
        panel.SetActive(true);
        isOpen = true;
        MenuState.SetOpen(this, true);
        Cursor.lockState = CursorLockMode.None;     // free the mouse so you can click
        Cursor.visible = true;                      // show the pointer
        message.text = "Click an item to craft it.";
    }

    void Close()
    {
        panel.SetActive(false);
        isOpen = false;
        MenuState.SetOpen(this, false);
        Cursor.lockState = CursorLockMode.Locked;   // back to aiming
    }

    // Throws away the old buttons and makes one new button per craftable item.
    void FillRows()
    {
        foreach (Transform child in rows) Destroy(child.gameObject);

        foreach (ItemData item in craftables)
        {
            // Skip empty slots, items without a recipe, and items made somewhere else (like a Furnace).
            if (item == null || !item.IsCraftable || item.CraftedAt != CraftStation.Crafting) continue;

            Button row = Instantiate(rowPrefab, rows);
            row.GetComponentInChildren<TMP_Text>().text = RowText(item);
            row.onClick.AddListener(() => Craft(item));
        }
    }

    // "Plank x2   (needs 1 Wood)"
    string RowText(ItemData item)
    {
        List<string> parts = new List<string>();
        foreach (Ingredient ing in item.Recipe)
        {
            if (ing != null && ing.item != null) parts.Add(ing.amount + " " + ing.DisplayName);
        }
        string made = item.CraftAmount > 1 ? " x" + item.CraftAmount : "";
        return item.DisplayName + made + "   (needs " + string.Join(", ", parts) + ")";
    }

    void Craft(ItemData item)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) { message.text = "No inventory in the scene."; return; }

        if (!item.CanCraft(inv))
        {
            message.text = "Can't craft " + item.DisplayName + ". Missing: " + item.Missing(inv);
            return;
        }

        item.TakeRecipeFrom(inv);
        if (inv.Add(item.Id, item.DisplayName, item.GetIcon(), item.CraftAmount, item))
        {
            message.text = "Crafted " + item.DisplayName + "!";
        }
        else
        {
            foreach (Ingredient ing in item.Recipe)   // inventory full: give the materials back
            {
                if (ing != null && ing.item != null)
                    inv.Add(ing.ItemId, ing.DisplayName, ing.item.GetIcon(), ing.amount, ing.item);
            }
            message.text = "Inventory is full!";
        }
    }
}