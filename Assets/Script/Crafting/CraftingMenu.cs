using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenu : MonoBehaviour
{
    [Tooltip("The items listed in the menu. Drag in Item Data files that have a recipe.")]
    [SerializeField] List<ItemData> craftables = new List<ItemData>();

    // From Tony I move and edit Turki code to KeyBindings.cs .The key that opens and closes the menu is KeyBindings.Crafting (default C), set on the KeyBindings object.

    [Header("UI (already filled in on the prefab)")]
    [SerializeField] GameObject panel;    // the dark box that shows and hides
    [SerializeField] Transform rows;      // the list the buttons go into
    [SerializeField] TMP_Text message;    // the line at the bottom
    [SerializeField] Button rowPrefab;    // one button, copied for each item

    bool isOpen;

    // [Tony] added (20/10/2026): this menu can also live inside the inventory's Crafting page
    // (InventoryCanvas > InventoryWindow > Pages > CraftingPage). There the TAB shows / hides it (Tab key, then click
    // the Crafting tab), and the inventory already frees the mouse, so C, Esc and the cursor are left to the inventory.
    // Anywhere else (e.g. its own CraftingCanvas in the scene) it works exactly as before: C opens it.
    bool insideInventory;                                                                     // [Tony] added

    void Awake()                                                                              // [Tony] added
    {                                                                                         // [Tony] added
        insideInventory = GetComponentInParent<InventoryWindow>(true) != null;               // [Tony] added
        if (insideInventory) FillParent();                                                    // [Tony] added
    }                                                                                         // [Tony] added

    // [Tony] added: the Crafting page was shown (its tab was clicked): fill the list and show the panel.
    void OnEnable()                                                                           // [Tony] added
    {                                                                                         // [Tony] added
        if (!insideInventory) return;                                                         // [Tony] added
        FillRows();                                                                           // [Tony] added
        panel.SetActive(true);                                                                // [Tony] added
        message.text = "Click an item to craft it.";                                          // [Tony] added
    }                                                                                         // [Tony] added

    // [Tony] added: inside the page, cover the whole page so the panel sits in its middle
    // (the prefab's own size is for a full-screen canvas).
    void FillParent()                                                                         // [Tony] added
    {                                                                                         // [Tony] added
        RectTransform rt = transform as RectTransform;                                        // [Tony] added
        if (rt == null) return;                                                               // [Tony] added
        rt.anchorMin = Vector2.zero;                                                          // [Tony] added
        rt.anchorMax = Vector2.one;                                                           // [Tony] added
        rt.offsetMin = Vector2.zero;                                                          // [Tony] added
        rt.offsetMax = Vector2.zero;                                                          // [Tony] added
        // The prefab saves its scale as 0 (Unity sizes a top-level canvas itself). Inside another canvas nothing
        // does that, so at scale 0 the whole menu would be invisible.
        rt.localScale = Vector3.one;                                                          // [Tony] added
    }                                                                                         // [Tony] added

    void Start()
    {
        // [Tony] edited: inside the inventory the panel is not hidden (the page shows / hides it).
        // Original line: panel.SetActive(false);   // hidden until C is pressed
        if (!insideInventory) panel.SetActive(false);   // hidden until C is pressed
    }

    void Update()
    {
        if (insideInventory) return;   // [Tony] added: inside the inventory, the inventory's Tab key and tabs open it, not C
        if (MenuState.KeysCaptured) return;

        if (Input.GetKeyDown(KeyBindings.Crafting)) //[Tony] see Crafting in KeyBindings.cs 
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

    // Rebuilt on every open so the list always matches the current Craftables.
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