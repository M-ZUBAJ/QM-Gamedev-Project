/*
First Date: 2/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto the Player (PlayerInventory is added automatically).
Unity Editor: Nothing to set. Which items it collects is decided by each item's config file:
              Item Data > Pickup = AutoPickup. (No tags.)
Purpose: Collects AutoPickup items into the inventory as soon as the player touches them.
         Works with normal colliders (bump into it) and "Is Trigger" colliders (walk through it).
         A just-dropped item waits a moment first, so it isn't collected straight back.
Requirement: AutoCollector.cs x-> PlayerInventory.cs, ItemPickup.cs, ItemData.cs
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class AutoCollector : MonoBehaviour
{
    // ===== Fields =====
    PlayerInventory inv;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    // The player walked into a collider marked "Is Trigger".
    void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    // The player bumped into a solid collider.
    void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.collider);
    }

    // Still touching: a just-dropped item's wait may end while the player is already touching it.
    void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    void OnCollisionStay(Collision collision)
    {
        TryCollect(collision.collider);
    }

    // ===== My methods =====

    void TryCollect(Collider hit)
    {
        // The player may touch a child part (e.g. a sword's blade), so look up through the parents.
        ItemPickup item = hit.GetComponentInParent<ItemPickup>();
        if (item == null || item.Pickup != PickupType.AutoPickup) return;   // not collected by touch
        if (!item.gameObject.activeSelf || !item.CanAutoCollect) return;     // already collected, or just dropped

        Sprite icon = item.GetIcon();   // take the picture before the object disappears

        if (inv.Add(item.ItemId, item.DisplayName, icon, item.Amount, item.Data))
        {
            item.gameObject.SetActive(false);   // hide it right now so it can't be collected twice
            Destroy(item.gameObject);           // Destroy happens at the end of the frame
        }
        else
        {
            item.BlockAutoCollect(1f);   // inventory full: try again in a second, not every physics step
            Debug.Log("Inventory is full.");
        }
    }
}
