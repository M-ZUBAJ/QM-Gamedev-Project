/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The Gear page's pop-up (GearPage > Picker), switched off until an armor slot is clicked.
        Tools > UI > Build Gear Page makes it.
Unity Editor: It finds its parts by name: Backdrop (the dark see-through cover; clicking it closes), Window > Title,
              Close (the X button), List > Choice (switched off: copied once per armor kind), Empty (the "you have none"
              text), Message (red, e.g. "no room"), TakeOff (the button that takes the worn piece off).
              Testing: Play, Tab > Gear, click a slot: the armor in the inventory that fits shows; click one to wear it.
Purpose: The window for choosing armor: it lists the armor in the inventory that fits the clicked slot (each kind once,
         with "x2" when there are more), with its picture, name and protection. Clicking one puts it on (PlayerArmor.Equip)
         and closes the window; Take Off takes the worn piece off. Esc, the X or a click outside the window closes it.
Requirement: ArmorPicker.cs x-> PlayerArmor.cs, ItemData.cs, ArmorStats.cs, UIAnimatedImage.cs, UITween.cs, ItemTooltip.cs
             ArmorPicker.cs <-x ArmorSlotUI.cs, GearPage.cs, GearPageBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ArmorPicker : MonoBehaviour
{
    // ===== Fields =====
    TMP_Text title;
    Transform list;          // where the choices go
    GameObject template;     // the switched-off choice that's copied
    TMP_Text emptyText;
    TMP_Text message;
    Button takeOff;
    TMP_Text takeOffLabel;
    RectTransform window;
    bool ready;              // the parts were found

    ArmorSlot slot;          // the slot being filled
    readonly List<GameObject> choices = new List<GameObject>();

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        FindParts();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void OnDisable()
    {
        ItemTooltip.Hide();
    }

    // ===== My methods =====

    // Opens the window for one slot and fills it with the armor that fits.
    public void Open(ArmorSlot forSlot)
    {
        FindParts();
        slot = forSlot;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();   // on top of the slots and the preview
        Fill();
        if (window != null)
        {
            window.localScale = Vector3.one * 0.85f;
            UITween.Scale(this, window, Vector3.one, 0.18f, UITween.Ease.OutBack);   // a small pop
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // The choices: one per armor kind in the inventory that fits the slot.
    void Fill()
    {
        foreach (GameObject old in choices) Destroy(old);
        choices.Clear();
        if (message != null) message.text = "";

        string slotName = ArmorStats.SlotName(slot).ToLower();   // "helmet", "leggings"...
        bool pair = slot == ArmorSlot.Shoulders || slot == ArmorSlot.Gloves || slot == ArmorSlot.Legs || slot == ArmorSlot.Boots;
        if (title != null) title.text = "Choose " + (pair ? "" : "a ") + slotName;   // "Choose a helmet", "Choose gloves"

        PlayerArmor armor = PlayerArmor.Current;
        List<(ItemData item, int count)> found = armor != null ? armor.InInventory(slot) : new List<(ItemData, int)>();
        foreach ((ItemData item, int count) in found) AddChoice(item, count);

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(found.Count == 0);
            emptyText.text = "No " + slotName + " in your inventory.\nCraft " + (pair ? "some" : "one") + " in the Crafting tab.";
        }

        ItemData worn = armor != null ? armor.WornIn(slot) : null;
        if (takeOff != null)
        {
            takeOff.gameObject.SetActive(worn != null);
            if (takeOffLabel != null && worn != null) takeOffLabel.text = "Take off " + worn.DisplayName;
        }
    }

    // One choice: picture, name, protection ("+1.5% protection") and how many, copied from the template.
    void AddChoice(ItemData item, int count)
    {
        if (template == null || list == null) return;
        GameObject choice = Instantiate(template, list);
        choice.name = item.Id;
        choice.SetActive(true);
        choices.Add(choice);

        Image picture = Part<Image>(choice.transform, "Icon");
        if (picture != null) UIAnimatedImage.ShowIcon(picture, item.GetIcon(), item.Icon);
        TMP_Text label = Part<TMP_Text>(choice.transform, "Label");
        if (label != null) label.text = item.DisplayName;
        TMP_Text defense = Part<TMP_Text>(choice.transform, "Defense");
        if (defense != null) defense.text = "+" + ArmorStats.Percent(item.Armor.protection) + " protection";
        TMP_Text amount = Part<TMP_Text>(choice.transform, "Count");
        if (amount != null) amount.text = count > 1 ? "x" + count : "";

        Button button = choice.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => Choose(item));
    }

    void Choose(ItemData item)
    {
        PlayerArmor armor = PlayerArmor.Current;
        if (armor != null && armor.Equip(item)) Close();
        else if (message != null) message.text = "No room in your inventory for the piece you're wearing.";
    }

    void TakeOffWorn()
    {
        PlayerArmor armor = PlayerArmor.Current;
        if (armor != null && armor.TakeOff(slot)) Close();
        else if (message != null) message.text = "No room in your inventory.";
    }

    // Finds the window's parts by name and connects its buttons (once).
    void FindParts()
    {
        if (ready) return;
        ready = true;

        window = transform.Find("Window") as RectTransform;
        Transform w = window != null ? window : transform;
        title = Part<TMP_Text>(w, "Title");
        list = w.Find("List");
        Transform choice = list != null ? list.Find("Choice") : null;
        if (choice != null)
        {
            template = choice.gameObject;
            template.SetActive(false);
        }
        emptyText = Part<TMP_Text>(w, "Empty");
        message = Part<TMP_Text>(w, "Message");
        takeOff = Part<Button>(w, "TakeOff");
        if (takeOff != null)
        {
            takeOffLabel = Part<TMP_Text>(takeOff.transform, "Label");
            takeOff.onClick.AddListener(TakeOffWorn);
        }
        Button close = Part<Button>(w, "Close");
        if (close != null) close.onClick.AddListener(Close);
        Button backdrop = Part<Button>(transform, "Backdrop");
        if (backdrop != null) backdrop.onClick.AddListener(Close);
    }

    // The component T on the child with this name, or null.
    static T Part<T>(Transform parent, string partName) where T : Component
    {
        Transform part = parent.Find(partName);
        return part != null ? part.GetComponent<T>() : null;
    }
}
