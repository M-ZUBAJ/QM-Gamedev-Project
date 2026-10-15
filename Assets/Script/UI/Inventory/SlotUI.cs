/*
First Date: 7/10/2026
Date: 8/10/2026
Author: Tony
Use by: On the Slot prefab (Assets/Prefab/UI/Slot.prefab). Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: Icon and Count are found by themselves (children named "Icon" and "Count").
              Which inventory slot it shows is set by the SlotGroup it is in, not here.
              Testing: Play, pick up items, press Tab: they show in the slots. Click them to move them.
Purpose: Shows one inventory slot: the item's picture and how many (no number for just 1).
         A selected hotbar slot gets the Selected Color; the others keep the color set on the slot's Image.
         Clicks (like Minecraft): left = pick up / put down / swap the whole stack,
         right = pick up half / put down one, double left click = gather all of that item here.
         Holding a stack, hold right click and drag over slots: one item goes into each slot you pass (once per slot).
Requirement: SlotUI.cs x-> PlayerInventory.cs, UIAnimatedImage.cs, IconSource.cs, Image (Unity)
             SlotUI.cs <-x HeldItemUI.cs, InventoryUIBuilder.cs, SlotGroup.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;   // IPointer...Handler: lets UI clicks reach this script
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class SlotUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler
{
    // ===== Fields =====
    [Tooltip("The item's picture. Empty = the child named \"Icon\".")]
    [SerializeField] Image icon;

    [Tooltip("The amount text. Empty = the child named \"Count\".")]
    [SerializeField] TMP_Text count;

    [SerializeField] TMP_Text itemName; /// displays text with the name of item if no icon is used

    [Tooltip("Box color of the selected hotbar slot (the item in hand). " +
             "The normal color is the slot Image's own Color, so set that on the Image as usual.")]
    [SerializeField] Color selectedColor = new Color(1f, 0.85f, 0.4f);

    Image frame;         // the slot's own box
    Color normalColor;   // the box color set on the Image in the editor, put back when not selected
    int index = -1;      // which slot of PlayerInventory this shows (-1 = none yet)

    // Right-click drag, shared by every slot (only one mouse): is a drag placing items, and which slots got one.
    static bool rightDragPlacing;
    static readonly HashSet<int> draggedOver = new HashSet<int>();

    public int Index => index;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        frame = GetComponent<Image>();
        normalColor = frame.color;   // remember the color you chose, before this script ever changes it
        if (icon == null) icon = FindChild<Image>("Icon");
        if (count == null) count = FindChild<TMP_Text>("Count");

        if (itemName == null)
            itemName = FindChild<TMP_Text>("ItemName");
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // redraw whenever items move
        Refresh();                            // and right away (the window may have been closed for a while)
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
    }

    // Unity calls this when the slot is clicked: pressed AND let go on this slot (needs an EventSystem in the scene).
    public void OnPointerClick(PointerEventData click)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || index < 0) return;
        if (click.button != PointerEventData.InputButton.Left) return;   // right is handled on press, below

        // The first click of a double click already picked the stack up; GatherSame handles that.
        if (click.clickCount == 2) inv.GatherSame(index);
        else inv.LeftClick(index);
    }

    // Unity calls this the moment a mouse button is pressed on the slot.
    // Right click acts on press (not on let go), so holding it can start a drag.
    public void OnPointerDown(PointerEventData press)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || index < 0) return;
        if (press.button != PointerEventData.InputButton.Right) return;

        // Like Minecraft: only a press while already holding a stack starts placing.
        // (A press on an empty hand picks up half; dragging after that doesn't spread it.)
        rightDragPlacing = !inv.Held.IsEmpty;
        draggedOver.Clear();
        draggedOver.Add(index);   // this slot gets its one item from RightClick right now

        inv.RightClick(index);
    }

    // Unity calls this when the mouse moves onto the slot, also while a button is held.
    public void OnPointerEnter(PointerEventData pointer)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || index < 0) return;
        if (!rightDragPlacing || !Input.GetMouseButton(1)) return;   // 1 = right button, still held
        if (inv.Held.IsEmpty || draggedOver.Contains(index)) return;   // nothing left, or this slot already got one

        draggedOver.Add(index);
        inv.PlaceOne(index);   // does nothing if a different item is in the slot
    }

    // Static values outlive the scene (and Play sessions if domain reload is off): start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        rightDragPlacing = false;
        draggedOver.Clear();
    }

    // ===== My methods =====

    // Called by SlotGroup: which inventory slot this box shows.
    public void Setup(int slotIndex)
    {
        index = slotIndex;
        Refresh();
    }

    public void Refresh()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        PlayerInventory.Slot slot = inv != null && index >= 0 && index < inv.Slots.Count ? inv.Slots[index] : null;

        Show(slot, icon, count);

        //////////////////////////////////////////////////////////
        /// added by marko to make Inventory slot UI display text 
        /// for items that you dont pick up and that have no icon yet

        bool isSword = slot != null && !slot.IsEmpty
                    && slot.id == "Sword";

        if (itemName != null)
        {
            itemName.text = isSword ? "SWORD" : "";
            itemName.gameObject.SetActive(isSword);
        }
        //////////////////////////// end of code added 

        bool selected = inv != null && inv.ActiveHotbar != PlayerInventory.NoHotbar
                        && index == inv.HotbarStart + inv.ActiveHotbar;
        if (frame != null) frame.color = selected ? selectedColor : normalColor;
    }

    // Puts a stack's picture and amount into an Icon and a Count. Also used by HeldItemUI.
    public static void Show(PlayerInventory.Slot slot, Image icon, TMP_Text count)
    {
        bool hasItem = slot != null && !slot.IsEmpty;

        if (icon != null)
        {
            // An item with an animated (Aseprite) icon plays it; everything else shows its still picture.
            IconSource source = hasItem && slot.data != null ? slot.data.Icon : null;
            UIAnimatedImage.ShowIcon(icon, hasItem ? slot.icon : null, source);
            icon.enabled = hasItem && icon.sprite != null;   // no item = no picture (not a white box)
        }

        // Like Minecraft: no number for a single item.
        if (count != null) count.text = hasItem && slot.count > 1 ? slot.count.ToString() : "";
    }

    // A direct child with this name and component, or null.
    T FindChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }
}
