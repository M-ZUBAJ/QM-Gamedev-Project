/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: The name box of the inventory: InventoryCanvas > ItemTooltip (with a child "Box" holding a "Text").
        Tools > UI > Build Inventory UI, or Tools > UI > Add Inventory Scripts for an inventory built before, makes it.
Unity Editor: Nothing to connect: it finds its "Box" and "Text" children by name.
              Testing: Play, press Tab and move the mouse over an item (inventory or hotbar): its name shows next to
              the mouse. It hides on an empty slot, while a stack is on the mouse, and when the inventory closes.
Purpose: Shows the name of the item under the mouse, next to the mouse, like Minecraft. The slots (SlotUI) say what to
         show (Show / Hide). The name = the item's Item Data > Display Name, or its file name when that's empty
         (PlayerInventory.Slot.ShownName), so "Meat.asset" shows "Meat" and the StonePickaxe file shows "Stone Pickaxe".
         It sits up and to the right of the mouse, on the other side near the screen's edges, on its own canvas on top
         of every window, and never blocks clicks.
Requirement: ItemTooltip.cs x-> PlayerInventory.cs
             ItemTooltip.cs <-x ArmorPicker.cs, ArmorSlotUI.cs, InventoryUIBuilder.cs, SlotUI.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;   // Graphic, LayoutRebuilder

[DisallowMultipleComponent]
public class ItemTooltip : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("How far from the mouse the box sits, in pixels on a 1920 x 1080 screen: x = to the right, y = up.")]
    [SerializeField] Vector2 offset = new Vector2(22f, 18f);

    [Tooltip("Sorting Order of its own canvas: higher = drawn over more (the stack on the mouse, HeldItemUI, uses 100).")]
    [SerializeField] int sortingOrder = 110;

    static ItemTooltip current;   // the one in the scene (the slots talk to it)

    RectTransform box;
    TMP_Text text;
    CanvasGroup group;
    Canvas ownCanvas;
    string wanted;                // the name a slot asked for (null = nothing)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        box = transform.Find("Box") as RectTransform;
        if (box == null) box = (RectTransform)transform;
        Transform textPart = box.Find("Text");
        text = textPart != null ? textPart.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;   // never in the way: the mouse keeps "hovering" the slot under it
        group.interactable = false;
        foreach (Graphic part in GetComponentsInChildren<Graphic>(true)) part.raycastTarget = false;

        // A Canvas of its own with a high Sorting Order puts it on top of every window (like the held stack).
        ownCanvas = GetComponent<Canvas>();
        if (ownCanvas == null) ownCanvas = gameObject.AddComponent<Canvas>();
        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = sortingOrder;
    }

    void OnDestroy()
    {
        if (current == this) current = null;   // static values outlive the scene, so clear it
    }

    // LateUpdate: after the slots said what's under the mouse this frame.
    void LateUpdate()
    {
        bool show = wanted != null && text != null && Cursor.lockState != CursorLockMode.Locked && !HoldingStack();
        group.alpha = show ? 1f : 0f;
        if (!show) return;

        if (text.text != wanted)
        {
            text.text = wanted;
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);   // the box fits the new name before it's placed
        }
        Place();
    }

    // ===== My methods =====

    // A slot under the mouse shows its item's name (empty text = nothing).
    public static void Show(string itemName)
    {
        if (current != null) current.wanted = string.IsNullOrEmpty(itemName) ? null : itemName;
    }

    // The mouse left the slot, or the slot became empty.
    public static void Hide()
    {
        if (current != null) current.wanted = null;
    }

    // Like Minecraft: no name while a stack is on the mouse (it's about to be put down).
    static bool HoldingStack()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        return inventory != null && inventory.Held != null && !inventory.Held.IsEmpty;
    }

    // Up and to the right of the mouse; to the left / below it when it would go past the screen's right / top edge.
    void Place()
    {
        Canvas root = ownCanvas.rootCanvas;
        float scale = root != null ? root.scaleFactor : 1f;   // pixels on this screen per pixel of the 1920 x 1080 layout
        Vector2 mouse = Input.mousePosition;
        Vector2 size = box.rect.size * scale;
        bool left = mouse.x + (offset.x * scale) + size.x > Screen.width;
        bool below = mouse.y + (offset.y * scale) + size.y > Screen.height;

        box.pivot = new Vector2(left ? 1f : 0f, below ? 1f : 0f);
        box.position = mouse + new Vector2(left ? -offset.x : offset.x, below ? -offset.y : offset.y) * scale;
    }
}
