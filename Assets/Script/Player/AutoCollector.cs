/*
First Date: 2/10/2026
Date: 12/10/2026
Author: Tony
Use by: Drag this script onto the Player (PlayerInventory is added automatically).
Unity Editor: Which world items it collects is decided by each item's config file:
              Item Data > Pickup = AutoPickup. (No tags.)
              Dropped items: GameRules > Auto Pickup Drops (off by default). On = walking within Drop Pickup Radius
              collects them. Testing: tick the rule, drop a stack (Tab, click it, click outside), walk back to it.
Purpose: Collects items into the inventory without pressing F:
         - AutoPickup items placed in the world, as soon as the player touches them
           (normal colliders: bump into it; "Is Trigger" colliders: walk through it).
         - Dropped stacks, when the player is near them, but only while the GameRule Auto Pickup Drops is on.
         A just-dropped item waits a moment first, so it isn't collected straight back.
         Nothing is collected while the player is dead (PlayerHealth), so death drops stay on the ground.
Requirement: AutoCollector.cs x-> PlayerInventory.cs, ItemPickup.cs, ItemData.cs, GameRules.cs
             AutoCollector.cs o-> PlayerHealth.cs
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class AutoCollector : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.5f, 5f)]
    [Tooltip("GameRule Auto Pickup Drops on: dropped stacks this close (meters, from the player's middle) are collected. " +
             "Minecraft is about 1 block + a bit, so 1.5.")]
    [SerializeField] float dropPickupRadius = 1.5f;

    PlayerInventory inv;
    PlayerHealth health;   // can be missing: then the player is never "dead" here

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
    }

    // Dropped stacks nearby (only with the GameRule Auto Pickup Drops on).
    void FixedUpdate()
    {
        if (!GameRules.AutoPickupDrops || IsDead()) return;

        // Every collider within the radius, triggers too (a dropped stack may use one).
        Collider[] near = Physics.OverlapSphere(transform.position, dropPickupRadius,
                                                Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider hit in near)
        {
            ItemPickup item = hit.GetComponentInParent<ItemPickup>();
            if (item != null && item.IsDrop) Collect(item);
        }
    }

    // The player walked into a collider marked "Is Trigger".
    void OnTriggerEnter(Collider other)
    {
        TryCollectTouched(other);
    }

    // The player bumped into a solid collider.
    void OnCollisionEnter(Collision collision)
    {
        TryCollectTouched(collision.collider);
    }

    // Still touching: a just-dropped item's wait may end while the player is already touching it.
    void OnTriggerStay(Collider other)
    {
        TryCollectTouched(other);
    }

    void OnCollisionStay(Collision collision)
    {
        TryCollectTouched(collision.collider);
    }

    // ===== My methods =====

    // Touched something: collect it if it's an AutoPickup item (Item Data > Pickup).
    void TryCollectTouched(Collider hit)
    {
        if (IsDead()) return;   // a dead body doesn't pick up the items it just dropped

        // The player may touch a child part (e.g. a sword's blade), so look up through the parents.
        ItemPickup item = hit.GetComponentInParent<ItemPickup>();
        if (item == null || item.Pickup != PickupType.AutoPickup) return;   // not collected by touch
        Collect(item);
    }

    // Puts the item in the inventory and removes it from the world.
    void Collect(ItemPickup item)
    {
        if (!item.gameObject.activeSelf || !item.CanAutoCollect) return;   // already collected, or just dropped

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

    bool IsDead()
    {
        return health != null && health.IsDead;
    }

    // Shows the Drop Pickup Radius as a green ball in the Scene view when the player is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dropPickupRadius);
    }
}
