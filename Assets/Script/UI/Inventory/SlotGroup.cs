/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: On the object whose children are the slots: InventoryPage (Is Hotbar off) and Hotbar (Is Hotbar on).
        Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: The slots are numbered in Hierarchy order (top to bottom = left to right, row by row).
              The number of slots should match PlayerInventory (Max Slots 20, Hotbar Size 8); the Console warns if not.
Purpose: Tells every SlotUI under it which inventory slot to show, so you never number 28 slots by hand.
Requirement: SlotGroup.cs x-> SlotUI.cs, PlayerInventory.cs
             SlotGroup.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class SlotGroup : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("On: these are the hotbar slots. Off: inventory slots.")]
    [SerializeField] bool isHotbar = false;

    [Min(0)]
    [Tooltip("Inventory slots only: the inventory slot the first box shows (0 = the first). " +
             "Use it if the inventory is ever split over two pages.")]
    [SerializeField] int firstSlot = 0;

    // ===== Unity messages (Unity calls these) =====

    // Start (not Awake): PlayerInventory has made its slots by then.
    void Start()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning(name + ": no PlayerInventory in the scene, so the slots stay empty.");
            return;
        }

        // The hotbar slots come after the inventory slots in PlayerInventory.
        int first = isHotbar ? inv.HotbarStart : firstSlot;
        int available = isHotbar ? inv.HotbarSize : Mathf.Max(0, inv.HotbarStart - firstSlot);

        SlotUI[] boxes = GetComponentsInChildren<SlotUI>(true);
        if (boxes.Length != available)
        {
            Debug.LogWarning(name + " has " + boxes.Length + " slots, but PlayerInventory has " + available +
                             ". Add or remove slots, or change PlayerInventory's sizes.");
        }

        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].Setup(i < available ? first + i : -1);   // extra boxes stay empty
        }
    }
}
