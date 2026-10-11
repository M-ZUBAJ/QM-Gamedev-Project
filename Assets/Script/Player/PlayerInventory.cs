/*
First Date: 30/9/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere.
Purpose: Stores the player's items. Items with the same Item Id stack in one slot.
         Every slot has a fixed place (slot 0, 1, 2 ...) and can be empty.
         The hotbar slots come after the inventory slots (HotbarStart). One of them can be selected (ActiveItem),
         or none (ActiveHotbar = NoHotbar).
         New items go to the inventory first, then to the hotbar when the inventory is full.
         Like Minecraft, clicking a slot picks its stack onto the cursor (Held) and clicking again puts it down.
         Other scripts add items with PlayerInventory.Instance.Add(...)
         and listen to PlayerInventory.Changed to update the UI.
Requirement: PlayerInventory.cs x-> IHand.cs, ItemData.cs
             PlayerInventory.cs <-x AutoCollector.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingData.cs, DroppedStack.cs, HeldItemUI.cs, HeldItemView.cs, HotbarSelector.cs, ItemConsumer.cs, ItemData.cs, ItemDropper.cs, ItemGrabber.cs, Recipe.cs, SlotGroup.cs, SlotUI.cs, UIBuildHelper.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour, IHand
{
    [System.Serializable]
    public class Slot
    {
        public string id;       // Item Id = the Item Data file name, e.g. "Stone"
        public string name;     // name shown to the player
        public Sprite icon;
        public int count;
        public ItemData data;   // the item's config file (prefab, pickup type...). Can be empty for old items.

        public bool IsEmpty => count <= 0;   // an empty slot has nothing in it

        // True if both hold the same item, so they can stack.
        public bool SameItemAs(Slot other)
        {
            return id == other.id;
        }

        // Copies which item it is (not the count) from another slot.
        public void CopyItemFrom(Slot other)
        {
            id = other.id;
            name = other.name;
            icon = other.icon;
            data = other.data;
        }

        public void Clear()
        {
            id = "";
            name = "";
            icon = null;
            count = 0;
            data = null;
        }
    }

    // ===== Fields =====
    [Range(1, 100)]
    [Tooltip("How many slots the inventory has. Match it to the number of slots in your UI.")]
    [SerializeField] int maxSlots = 20;

    [Range(1, 10)]
    [Tooltip("How many hotbar slots there are. Match it to the number of slots in the Hotbar UI.")]
    [SerializeField] int hotbarSize = 8;

    [Tooltip("The items currently held: first the inventory slots, then the hotbar slots. Shown here for checking while playing.")]
    [SerializeField] List<Slot> slots = new List<Slot>();

    [Tooltip("The stack held on the mouse cursor while the inventory is open. Shown here for checking while playing.")]
    [SerializeField] Slot held = new Slot();

    public const int NoHotbar = -1;   // activeHotbar value when no hotbar slot is selected
    int activeHotbar = NoHotbar;      // which hotbar slot is selected (0 = the first one, -1 = none)

    public static PlayerInventory Instance { get; private set; }   // any script can reach the inventory
    public static event System.Action Changed;                     // fired whenever items change (or the hotbar selection)

    public IReadOnlyList<Slot> Slots => slots;
    public Slot Held => held;   // the stack on the cursor (Held.IsEmpty = holding nothing)

    // The hotbar is the last hotbarSize slots of the list, so all the clicking rules work on it too.
    public int HotbarStart => maxSlots;                        // index of the first hotbar slot in Slots
    public int HotbarSize => hotbarSize;
    public int ActiveHotbar => activeHotbar;                   // 0 - (HotbarSize - 1), or NoHotbar (-1)
    // The item "in hand". null = no slot selected; ActiveItem.IsEmpty = the selected slot is empty.
    public Slot ActiveItem => activeHotbar == NoHotbar ? null : slots[maxSlots + activeHotbar];

    // IHand: the hand is empty when no hotbar slot is selected, or the selected slot has no item.
    // (Lets e.g. ObjectLevitator check the hand without knowing this inventory.)
    public bool IsEmpty => ActiveItem == null || ActiveItem.IsEmpty;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Instance = this;

        // Make exactly maxSlots + hotbarSize slots, so every slot has its own place.
        int total = maxSlots + hotbarSize;
        while (slots.Count < total) slots.Add(new Slot());
        if (slots.Count > total) slots.RemoveRange(total, slots.Count - total);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;   // static values outlive the scene, so clear it
    }

    // ===== My methods =====

    // Adds items to the inventory. Returns false if a new slot is needed but all are full.
    // data = the item's config file (Item Data), so it can be dropped / shown later. Can be null.
    public bool Add(string id, string name, Sprite icon, int amount, ItemData data = null)
    {
        Slot slot = FindSlot(id);     // first try to stack on the same item
        if (slot == null)
        {
            slot = FindEmptySlot();   // otherwise use the first empty slot
            if (slot == null) return false;

            slot.id = id;
            slot.name = name;
            slot.icon = icon;
            slot.data = data;
        }
        else if (slot.data == null)
        {
            slot.data = data;   // an older stack learns its config file
        }

        slot.count += amount;
        Changed?.Invoke();
        return true;
    }

    // Left click on a slot (like Minecraft):
    // holding nothing = pick up the whole stack, holding an item = put it all down,
    // same item = add it to the stack, different item = swap with what is held.
    public void LeftClick(int i)
    {
        if (!IsSlot(i)) return;
        Slot slot = slots[i];

        if (!held.IsEmpty && !slot.IsEmpty && held.SameItemAs(slot))
        {
            slot.count += held.count;   // same item: put the whole held stack on it
            held.Clear();
        }
        else
        {
            // Pick up, put down and swap are all the same thing: swap the slot with the cursor.
            slots[i] = held;
            held = slot;
        }

        Changed?.Invoke();
    }

    // Right click on a slot (like Minecraft):
    // holding nothing = pick up half the stack, holding an item = put down just 1,
    // different item = swap with what is held.
    public void RightClick(int i)
    {
        if (!IsSlot(i)) return;
        Slot slot = slots[i];

        if (held.IsEmpty)
        {
            if (slot.IsEmpty) return;              // nothing to pick up

            int half = (slot.count + 1) / 2;       // the bigger half goes to the cursor (5 -> 3)
            held.CopyItemFrom(slot);
            held.count = half;
            slot.count -= half;
            if (slot.IsEmpty) slot.Clear();
        }
        else if (slot.IsEmpty || slot.SameItemAs(held))
        {
            PlaceOne(i);   // fires Changed itself
            return;
        }
        else
        {
            slots[i] = held;   // different item: swap, same as left click
            held = slot;
        }

        Changed?.Invoke();
    }

    // Double click: pulls every stack of the same item (and the held stack) into slot i.
    public void GatherSame(int i)
    {
        if (!IsSlot(i)) return;

        // The first click of the double click picked the stack up, so put it back down first.
        if (slots[i].IsEmpty && !held.IsEmpty)
        {
            Slot empty = slots[i];
            slots[i] = held;
            held = empty;
        }

        Slot target = slots[i];
        if (target.IsEmpty) return;   // nothing here to gather

        if (!held.IsEmpty && held.SameItemAs(target))
        {
            target.count += held.count;   // also take the same item from the cursor
            held.Clear();
        }

        for (int j = 0; j < slots.Count; j++)
        {
            if (j == i || slots[j].IsEmpty || !slots[j].SameItemAs(target)) continue;   // skip other items

            target.count += slots[j].count;
            slots[j].Clear();
        }

        Changed?.Invoke();
    }

    // Puts down 1 of the held item in slot i, if the slot is empty or has the same item.
    // Returns false if nothing was placed (holding nothing, or a different item is there).
    public bool PlaceOne(int i)
    {
        if (!IsSlot(i) || held.IsEmpty) return false;

        Slot slot = slots[i];
        if (!slot.IsEmpty && !slot.SameItemAs(held)) return false;   // never swap here

        if (slot.IsEmpty) slot.CopyItemFrom(held);   // empty slot becomes this item
        slot.count += 1;
        held.count -= 1;
        if (held.IsEmpty) held.Clear();

        Changed?.Invoke();
        return true;
    }

    // Takes 1 item out of the selected hotbar slot (the item in hand), e.g. when it is eaten.
    // Returns false if nothing is in hand.
    public bool TakeOneFromHand()
    {
        Slot hand = ActiveItem;
        if (hand == null || hand.IsEmpty) return false;

        hand.count -= 1;
        if (hand.IsEmpty) hand.Clear();
        Changed?.Invoke();
        return true;
    }

    // Empties every slot and the cursor stack at once (e.g. everything was dropped on death).
    public void ClearAll()
    {
        foreach (Slot slot in slots) slot.Clear();
        held.Clear();
        Changed?.Invoke();
    }

    // Takes items away from the held stack (e.g. they were dropped into the world).
    public void RemoveFromHeld(int amount)
    {
        if (held.IsEmpty || amount <= 0) return;

        held.count -= amount;
        if (held.IsEmpty) held.Clear();
        Changed?.Invoke();
    }

    // Puts the held stack back into the inventory (call it when the inventory closes).
    public void ReturnHeld()
    {
        if (held.IsEmpty) return;

        // Add() stacks it on the same item or uses the first empty slot.
        // If there is no room, it stays on the cursor and shows again next time the inventory opens.
        if (Add(held.id, held.name, held.icon, held.count, held.data))
        {
            held.Clear();
            Changed?.Invoke();
        }
    }

    // Number key: selects hotbar slot i (0 = first). Pressing the selected slot again selects nothing.
    public void ToggleHotbar(int i)
    {
        if (i < 0 || i >= hotbarSize) return;

        activeHotbar = i == activeHotbar ? NoHotbar : i;
        Changed?.Invoke();
    }

    // Mouse wheel: step = +1 next slot, -1 previous slot, wrapping around the ends.
    // With nothing selected, +1 starts at the first slot and -1 at the last.
    public void ScrollHotbar(int step)
    {
        if (step == 0) return;

        if (activeHotbar == NoHotbar)
        {
            activeHotbar = step > 0 ? 0 : hotbarSize - 1;
        }
        else
        {
            // % wraps forward (8 -> 0); adding hotbarSize first makes -1 wrap to the last slot.
            activeHotbar = ((activeHotbar + step) % hotbarSize + hotbarSize) % hotbarSize;
        }
        Changed?.Invoke();
    }

    // How many of this item the player has, in all slots together (any properties).
    public int Count(string id)
    {
        int total = 0;
        foreach (Slot slot in slots)
        {
            if (!slot.IsEmpty && slot.id == id) total += slot.count;
        }
        return total;
    }

    // The first slot with this item (to show its name and icon), or null if the player has none.
    public Slot FindAny(string id)
    {
        foreach (Slot slot in slots)
        {
            if (!slot.IsEmpty && slot.id == id) return slot;
        }
        return null;
    }

    // Takes "amount" of this item out of the inventory, from as many slots as needed (inventory first, then hotbar).
    // Takes nothing (and returns false) if the player doesn't have that many.
    public bool Remove(string id, int amount)
    {
        if (amount <= 0) return true;
        if (Count(id) < amount) return false;

        foreach (Slot slot in slots)
        {
            if (amount == 0) break;
            if (slot.IsEmpty || slot.id != id) continue;

            int take = Mathf.Min(slot.count, amount);
            slot.count -= take;
            amount -= take;
            if (slot.IsEmpty) slot.Clear();
        }

        Changed?.Invoke();
        return true;
    }

    // Takes 1 of this item out of the inventory (inventory slots first, then the hotbar).
    // Returns a copy of what was taken (count 1), so it can be given back later, or null if there is none.
    public Slot TakeOne(string id)
    {
        Slot slot = FindAny(id);
        if (slot == null) return null;

        Slot taken = new Slot();
        taken.CopyItemFrom(slot);
        taken.count = 1;

        slot.count -= 1;
        if (slot.IsEmpty) slot.Clear();

        Changed?.Invoke();
        return taken;
    }

    // True if ALL these stacks would fit (e.g. before giving back a building's materials).
    // Stacks never get full, so only items that can't stack on anything need an empty slot each.
    public bool CanAddAll(List<Slot> stacks)
    {
        int emptySlots = 0;
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty) emptySlots++;
        }

        // Different new items need their own slot; the same new item twice shares one.
        HashSet<string> newKinds = new HashSet<string>();
        foreach (Slot s in stacks)
        {
            if (FindSlot(s.id) != null) continue;   // stacks on an item already there
            newKinds.Add(s.id);
        }

        return newKinds.Count <= emptySlots;
    }

    bool IsSlot(int i)
    {
        return i >= 0 && i < slots.Count;
    }

    Slot FindSlot(string id)
    {
        foreach (Slot slot in slots)
        {
            if (!slot.IsEmpty && slot.id == id) return slot;
        }
        return null;
    }

    Slot FindEmptySlot()
    {
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty) return slot;
        }
        return null;
    }
}
