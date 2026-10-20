/*
First Date: 7/10/2026
Date: 20/10/2026
Author: Tony
Use by: On the Slot prefab (Assets/Prefabs/UI/Inventory/Slot.prefab). Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: Icon, Count and ItemName are found by themselves (children named "Icon", "Count" and "ItemName").
              How items are shown can be switched for every item at once in GameRules > Item Icons (ItemIconRules):
              e.g. Label = each slot shows the item's name instead of a picture.
              Which inventory slot it shows is set by the SlotGroup it is in, not here.
              Testing: Play, pick up items, press Tab: they show in the slots. Click them to move them.
Purpose: Shows one inventory slot: the item's picture and how many (no number for just 1).
         A selected hotbar slot gets the Selected Color; the others keep the color set on the slot's Image.
         Clicks (like Minecraft): left = pick up / put down / swap the whole stack,
         right = pick up half / put down one, double left click = gather all of that item here.
         Holding a stack, hold right click and drag over slots: one item goes into each slot you pass (once per slot).
Requirement: SlotUI.cs x-> PlayerInventory.cs, UIAnimatedImage.cs, IconSource.cs, ItemIconRules.cs, GameRules.cs, Image (Unity)
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

    // Where a text sits and how it's lined up, so Label mode can move it and put it back exactly afterwards.
    // Public: HeldItemUI uses it too.
    public struct TextLayout
    {
        public Vector2 anchorMin, anchorMax, pivot, position, size;
        public TextAlignmentOptions alignment;
        public bool autoSize, wrap;
        public float fontSize, fontSizeMin, fontSizeMax;

        public static TextLayout Of(TMP_Text t)
        {
            RectTransform rt = t.rectTransform;
            return new TextLayout
            {
                anchorMin = rt.anchorMin, anchorMax = rt.anchorMax, pivot = rt.pivot,
                position = rt.anchoredPosition, size = rt.sizeDelta,
                alignment = t.alignment, autoSize = t.enableAutoSizing, wrap = t.enableWordWrapping,
                fontSize = t.fontSize, fontSizeMin = t.fontSizeMin, fontSizeMax = t.fontSizeMax,
            };
        }

        public void ApplyTo(TMP_Text t)
        {
            RectTransform rt = t.rectTransform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = position; rt.sizeDelta = size;
            t.alignment = alignment; t.enableAutoSizing = autoSize; t.enableWordWrapping = wrap;
            t.fontSize = fontSize; t.fontSizeMin = fontSizeMin; t.fontSizeMax = fontSizeMax;
        }
    }

    // Label mode (GameRules > Item Icon Style = Label): the name fills the top of the box, the amount the bottom left.
    const float LabelPadding = 6f;          // gap between the slot's edge and the texts, in pixels
    const float LabelCountHeight = 0.3f;    // the bottom 30% of the box is kept for the amount
    const float LabelFontMax = 26f;         // the name is at most this big, and shrinks to fit long names
    const float LabelFontMin = 10f;

    Image frame;         // the slot's own box
    Color normalColor;   // the box color set on the Image in the editor, put back when not selected
    int index = -1;      // which slot of PlayerInventory this shows (-1 = none yet)

    TextLayout nameLayout, countLayout;   // how ItemName and Count were set up (Marko's / the prefab's layout)
    bool labelLayout;                     // ItemName and Count are moved for Label mode right now

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

        // Remember the texts' own layout before Label mode ever moves them.
        if (itemName != null) nameLayout = TextLayout.Of(itemName);
        if (count != null) countLayout = TextLayout.Of(count);
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // redraw whenever items move
        GameRules.Changed += Refresh;         // and when GameRules > Item Icons is switched
        Refresh();                            // and right away (the window may have been closed for a while)
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
        GameRules.Changed -= Refresh;
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

        Show(slot, icon, count, itemName);   // [Tony] itemName added: in Label mode the picture is hidden for the name (was: Show(slot, icon, count);)

        //////////////////////////////////////////////////////////
        /// added by marko to make Inventory slot UI display text 
        /// for items that you dont pick up and that have no icon yet


        bool hasItem = slot != null && !slot.IsEmpty;

        string label = "";

        if (hasItem)
        {
            if (slot.id == "Sword")
                label = "SWORD";
            else if (slot.id == "Axe")
                label = "AXE";
            else if (slot.id == "PickAxe")
                label = "PICKAXE";
        }

        if (itemName != null)
        {
            itemName.text = label;
            itemName.gameObject.SetActive(label != "");
        }

        //////////////////////////// end of code added

        // [Tony] added: GameRules > Overwrite Item Icon = Label: EVERY item shows its name here (not only the tools above),
        // so it runs after Marko's lines and wins over them while Label is on.
        ShowLabel(slot, itemName);
        UseLabelLayout(ItemIconRules.UseLabel);   // [Tony] added: Label mode lays the name and amount out to fit the box

        bool selected = inv != null && inv.ActiveHotbar != PlayerInventory.NoHotbar
                        && index == inv.HotbarStart + inv.ActiveHotbar;
        if (frame != null) frame.color = selected ? selectedColor : normalColor;
    }

    // Puts a stack's picture and amount into an Icon and a Count. Also used by HeldItemUI.
    // label (optional): where the item's name goes when GameRules > Item Icon Style = Label.
    // Without a label, Label mode keeps showing a picture (so the item never becomes invisible).
    public static void Show(PlayerInventory.Slot slot, Image icon, TMP_Text count, TMP_Text label = null)
    {
        bool hasItem = slot != null && !slot.IsEmpty;
        bool asLabel = hasItem && label != null && ItemIconRules.UseLabel;

        if (icon != null)
        {
            Sprite still = null;
            IconSource animation = null;
            if (hasItem && !asLabel)
            {
                // The item's own icon, or the one GameRules > Item Icons says (ItemIconRules).
                ItemIconRules.Resolve(slot.data, slot.icon, out still, out animation);
                if (still == null && animation == null) still = slot.icon;   // Label mode but nowhere to write the name
            }

            // An animated (Aseprite) icon plays; everything else shows its still picture.
            UIAnimatedImage.ShowIcon(icon, still, animation);
            icon.enabled = hasItem && !asLabel && icon.sprite != null;   // no item = no picture (not a white box)
        }

        ShowLabel(slot, label);

        // Like Minecraft: no number for a single item.
        if (count != null) count.text = hasItem && slot.count > 1 ? slot.count.ToString() : "";
    }

    // Label mode on: the name fills the top of the box (centered, shrinking to fit), the amount sits bottom left.
    // Off: both go back exactly to how they were set up (e.g. Marko's "SWORD" text keeps its own place).
    void UseLabelLayout(bool on)
    {
        if (on == labelLayout) return;   // already laid out that way
        labelLayout = on;

        if (!on)
        {
            if (itemName != null) nameLayout.ApplyTo(itemName);
            if (count != null) countLayout.ApplyTo(count);
            return;
        }

        ApplyLabelLayout(itemName, count, countLayout.size);
    }

    // The Label layout, inside whatever box the texts belong to (a slot, or the stack on the cursor):
    // the name fills the top of the box, centered and shrinking to fit; the amount sits in the bottom left corner.
    // Wherever the texts were placed in the editor doesn't matter: they are always put inside their own box.
    public static void ApplyLabelLayout(TMP_Text name, TMP_Text count, Vector2 countSize)
    {
        if (name != null)
        {
            RectTransform rt = name.rectTransform;
            rt.anchorMin = new Vector2(0f, LabelCountHeight);   // above the amount's row
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(LabelPadding, 0f);
            rt.offsetMax = new Vector2(-LabelPadding, -LabelPadding);
            name.alignment = TextAlignmentOptions.Center;
            name.enableWordWrapping = true;    // "IRON ORE" can break into two lines
            name.enableAutoSizing = true;      // long names shrink instead of spilling out of the box
            name.fontSizeMin = LabelFontMin;
            name.fontSizeMax = LabelFontMax;
        }

        if (count != null)
        {
            RectTransform rt = count.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;   // bottom left corner
            rt.anchoredPosition = new Vector2(LabelPadding + 2f, 4f);
            rt.sizeDelta = countSize;
            count.alignment = TextAlignmentOptions.BottomLeft;
        }
    }

    // GameRules > Item Icon Style = Label: writes the item's name (e.g. "SWORD") into the label and shows it.
    // Not in Label mode: leaves the label alone (the slot's own code decides, e.g. Marko's tool names).
    public static void ShowLabel(PlayerInventory.Slot slot, TMP_Text label)
    {
        if (label == null || !ItemIconRules.UseLabel) return;

        string text = ItemIconRules.Label(slot);   // "" for an empty slot
        label.text = text;
        label.gameObject.SetActive(text != "");
    }

    // A direct child with this name and component, or null.
    T FindChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }
}
