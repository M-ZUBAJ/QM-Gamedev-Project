/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: On "HeldItem", a child of InventoryWindow. Tools > UI > Add Inventory Scripts makes it for you.
Unity Editor: Icon and Count are found by themselves (children named "Icon" and "Count").
              It adds its own Canvas when the game runs, so it draws on top of the hotbar and every other UI
              (raise "Sorting Order" if something still covers it).
              Testing: Play, Tab, click a slot with items: the stack follows the mouse. Click another slot to put it down.
Purpose: Shows the stack picked up with the mouse (PlayerInventory.Held) at the mouse pointer, above all other UI.
         It never blocks clicks, so the slot under it still gets them.
         When the inventory closes, a stack still on the mouse goes back into the inventory.
Requirement: HeldItemUI.cs x-> PlayerInventory.cs, SlotUI.cs
             HeldItemUI.cs <-x InventoryUIBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeldItemUI : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The item's picture. Empty = the child named \"Icon\".")]
    [SerializeField] Image icon;

    [Tooltip("The amount text. Empty = the child named \"Count\".")]
    [SerializeField] TMP_Text count;

    [Tooltip("Draw order: higher = on top. Must be higher than every other canvas (HUD, hotbar...).")]
    [SerializeField] int sortingOrder = 100;

    Canvas ownCanvas;   // its own layer, so it draws above things that come later in the Hierarchy (e.g. the Hotbar)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (icon == null) icon = FindChild<Image>("Icon");
        if (count == null) count = FindChild<TMP_Text>("Count");

        // Clicks must go through to the slot under the mouse, not hit the held picture.
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        // UI is drawn in Hierarchy order, and the Hotbar comes after InventoryWindow, so it would cover this.
        // A Canvas of its own with a high Sorting Order puts it on top of everything instead.
        ownCanvas = GetComponent<Canvas>();
        if (ownCanvas == null) ownCanvas = gameObject.AddComponent<Canvas>();
    }

    void OnEnable()
    {
        // Set every time it shows: Unity can forget Override Sorting while the object is hidden.
        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = sortingOrder;

        PlayerInventory.Changed += Refresh;
        Refresh();
    }

    // The inventory window closed (this is inside it): put the held stack back.
    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;

        PlayerInventory inv = PlayerInventory.Instance;
        if (inv != null) inv.ReturnHeld();
    }

    // LateUpdate: after the mouse moved this frame.
    void LateUpdate()
    {
        transform.position = Input.mousePosition;   // on a Screen Space - Overlay canvas, position = screen pixels
    }

    // ===== My methods =====

    void Refresh()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        SlotUI.Show(inv != null ? inv.Held : null, icon, count);
    }

    T FindChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }
}
