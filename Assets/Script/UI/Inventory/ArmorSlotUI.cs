/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Each armor slot on the inventory's Gear page (HelmetSlot, ShouldersSlot...), next to its Button and ButtonFX.
        Tools > UI > Build Gear Page makes the six of them.
Unity Editor: Slot = which armor it holds. Picker = the page's pop-up (ArmorPicker), found by itself if empty.
              It finds its parts by name: Icon (the worn piece's picture), Empty (the faint outline shown while
              nothing is worn) and Defense (the corner text: how much the worn piece protects, e.g. "+1.5%").
              Testing: Play, Tab > Gear: move the mouse over a slot (it grows and turns gold, and its name shows next to
              the mouse), click it to choose armor, right-click it to take the armor off. Or pick a piece up on the
              Inventory page (it sticks to the cursor), open Gear and click its slot: it goes straight on.
Purpose: One armor slot: shows what the player wears there (PlayerArmor), or a faint outline of the piece if nothing.
         The hover effect (grow, gold, shine, sparks on click) comes from ButtonFX; this adds the name box next to the
         mouse (ItemTooltip). Left click opens the pop-up to choose armor from the inventory; right click takes the
         worn piece off and puts it back in the inventory. While a stack is held on the cursor (picked up on the
         Inventory page), a click puts that piece on instead (PlayerArmor.EquipFromCursor); if it doesn't go in this
         slot, the Gear page says why (GearPage.Say).
Requirement: ArmorSlotUI.cs x-> PlayerArmor.cs, PlayerInventory.cs, ArmorPicker.cs, GearPage.cs, ItemData.cs, ArmorStats.cs, ItemTooltip.cs, UIAnimatedImage.cs, Button (Unity)
             ArmorSlotUI.cs <-x GearPageBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ArmorSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // ===== Fields =====
    [Tooltip("Which armor this slot holds.")]
    [SerializeField] ArmorSlot slot = ArmorSlot.Helmet;

    [Tooltip("The Gear page's pop-up for choosing armor. Empty = found on the page by itself.")]
    [SerializeField] ArmorPicker picker;

    Image icon;          // the worn piece's picture
    Image empty;         // the faint outline of the piece, while nothing is worn
    TMP_Text defense;    // "+1.5%"
    GearPage gearPage;   // says why a click didn't work (can be missing)
    bool hovered;

    public ArmorSlot Slot => slot;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        icon = FindPart<Image>("Icon");
        empty = FindPart<Image>("Empty");
        defense = FindPart<TMP_Text>("Defense");
        if (picker == null)
        {
            Transform page = transform.parent != null && transform.parent.parent != null ? transform.parent.parent : transform.parent;
            if (page != null) picker = page.GetComponentInChildren<ArmorPicker>(true);
        }
        gearPage = GetComponentInParent<GearPage>(true);
        // The Button is only there for ButtonFX's hover and press looks: every click is handled in OnPointerClick.
    }

    void OnEnable()
    {
        PlayerArmor.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerArmor.Changed -= Refresh;
        if (hovered) ItemTooltip.Hide();
        hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        ItemTooltip.Show(TooltipText());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        ItemTooltip.Hide();
    }

    // Holding a stack on the cursor: put that piece on. Else left click = the pop-up, right click = take it off.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle) return;
        PlayerArmor armor = PlayerArmor.Current;
        if (armor == null) return;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory != null && !inventory.Held.IsEmpty)
        {
            if (!armor.EquipFromCursor(slot)) Say(WhyNot(inventory.Held.data));
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left) OpenPicker();
        else if (armor.WornIn(slot) != null && !armor.TakeOff(slot)) Say("No room in the inventory to take it off.");
    }

    // ===== My methods =====

    // Shows the worn piece (or the outline), and how much it protects.
    void Refresh()
    {
        ItemData worn = Worn();
        if (icon != null)
        {
            icon.enabled = worn != null;
            if (worn != null) UIAnimatedImage.ShowIcon(icon, worn.GetIcon(), worn.Icon);
        }
        if (empty != null) empty.enabled = worn == null;
        if (defense != null) defense.text = worn != null ? "+" + ArmorStats.Percent(worn.Armor.protection) : "";
        if (hovered) ItemTooltip.Show(TooltipText());
    }

    // Why the item on the cursor couldn't be put on here.
    string WhyNot(ItemData held)
    {
        if (held == null || !held.IsArmor) return "Only armor goes in the " + ArmorStats.SlotName(slot) + " slot.";
        if (held.Armor.slot != slot) return held.DisplayName + " goes in the " + ArmorStats.SlotName(held.Armor.slot) + " slot.";
        return "No room in the inventory for the piece you're wearing.";
    }

    void Say(string message)
    {
        if (gearPage != null) gearPage.Say(message);
        else Debug.Log(message);
    }

    void OpenPicker()
    {
        ItemTooltip.Hide();
        if (picker != null) picker.Open(slot);
        else Debug.LogWarning(name + ": no ArmorPicker on the Gear page. Run Tools > UI > Build Gear Page.");
    }

    // "Helmet: Iron Helmet (+1.5% protection, Iron set)" + what the clicks do, or "Helmet: empty" + how to fill it.
    string TooltipText()
    {
        ItemData worn = Worn();
        string slotName = ArmorStats.SlotName(slot);
        if (worn == null) return slotName + ": empty\nClick to put armor on";
        string set = string.IsNullOrEmpty(worn.Armor.set) ? "" : ", " + worn.Armor.set + " set";
        return slotName + ": " + worn.DisplayName + " (+" + ArmorStats.Percent(worn.Armor.protection) + " protection" + set + ")\n" +
               "Click to change, right-click to take off";
    }

    ItemData Worn()
    {
        PlayerArmor armor = PlayerArmor.Current;
        ItemData worn = armor != null ? armor.WornIn(slot) : null;
        return worn != null && worn.IsArmor ? worn : null;
    }

    // A part of this slot found by its name (one of its children), or null.
    T FindPart<T>(string partName) where T : Component
    {
        Transform part = transform.Find(partName);
        return part != null ? part.GetComponent<T>() : null;
    }
}
