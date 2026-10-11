/*
First Date: 30/9/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto any object the player can pick up (best: onto the item's prefab).
        Tools > Items > Make Item From Selection adds it and makes the config file for you.
Unity Editor: Data = the item's config file (Item Data in Assets/Config/Items). It decides the item and how it is picked up.
              No tag needed. Amount = how many the player gets from this one object.
Purpose: Marks an object as an item. Its Item Data says which item it is and how it is picked up
         (Pickup = crosshair + F, AutoPickup = by touch).
         Very thin Box Colliders are made thicker so physics doesn't miss them.
         (No Data: it still works as a "Pickup" item named after the object, so old objects don't break.)
Requirement: ItemPickup.cs x-> ItemData.cs, IconCapture.cs
             ItemPickup.cs <-x AutoCollector.cs, BuildingTools.cs, Crosshair.cs, DroppedStack.cs, ItemDropper.cs, ItemGrabber.cs, ItemTools.cs
*/

using System.Text.RegularExpressions;   // Regex: find and replace text by a pattern
using UnityEngine;

[DisallowMultipleComponent]
public class ItemPickup : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The item's config file (Item Data, in Assets/Config/Items). Decides which item this is and how it is picked up.")]
    [SerializeField] ItemData data;

    [Range(1, 999)]
    [Tooltip("How many items the player gets from this object.")]
    [SerializeField] int amount = 1;

    const float MinColliderThickness = 0.01f;   // colliders thinner than this get missed by physics

    float blockedUntil;   // game time until which touching doesn't collect it (just dropped)
    Sprite iconOverride;  // a picture given by code (a dropped stack keeps its inventory picture), null = none

    public ItemData Data => data;
    public string ItemId => data != null ? data.Id : CleanName(gameObject.name);
    public string DisplayName => data != null ? data.DisplayName : ItemId;
    public PickupType Pickup => data != null ? data.Pickup : PickupType.Pickup;
    public int Amount => amount;

    // False for a moment after the player dropped it, so touching doesn't collect it straight back.
    public bool CanAutoCollect => Time.time >= blockedUntil;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        GrowThinColliders();
    }

    // ===== My methods =====

    // Changes how many items this object gives.
    public void SetAmount(int newAmount)
    {
        amount = Mathf.Max(1, newAmount);
    }

    // Fills in everything at once, for items made by code (e.g. ItemDropper drops a stack from the inventory).
    public void Setup(ItemData newData, int newAmount)
    {
        data = newData;
        SetAmount(newAmount);
    }

    // The inventory picture: the Item Data's still picture / first animation frame if its Icon isn't Capture,
    // else a photo of this object (taken once per item, then remembered). Call it before the object is destroyed.
    public Sprite GetIcon()
    {
        if (iconOverride != null) return iconOverride;   // e.g. a dropped stack: no need for a new photo
        if (data != null && !data.Icon.IsCapture) return data.Icon.Still;
        return IconCapture.GetIcon(ItemId, gameObject);
    }

    // Gives this object a ready-made inventory picture (e.g. a dropped stack keeps the one it had in the inventory).
    public void SetIcon(Sprite icon)
    {
        iconOverride = icon;
    }

    // Touching won't collect it for this many seconds (e.g. right after it was dropped).
    public void BlockAutoCollect(float seconds)
    {
        blockedUntil = Time.time + seconds;
    }

    // Physics misses colliders that are very thin (a fast object can pass right through).
    // Makes every Box Collider at least MinColliderThickness thick.
    void GrowThinColliders()
    {
        foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>())
        {
            Vector3 scale = box.transform.lossyScale;
            Vector3 size = box.size;

            for (int axis = 0; axis < 3; axis++)   // 0 = x, 1 = y, 2 = z
            {
                float s = Mathf.Abs(scale[axis]);
                if (s <= 0f) continue;   // scale 0 can't be fixed by size

                // World thickness = size x scale. Too thin? Make the size big enough.
                if (size[axis] * s < MinColliderThickness)
                {
                    size[axis] = MinColliderThickness / s;
                }
            }

            box.size = size;
        }
    }

    // Copies of an object get names like "Coin (1)" or "Coin(Clone)".
    // This removes those endings so every copy without Data still gets the same Item Id ("Coin") and stacks.
    public static string CleanName(string objectName)
    {
        return Regex.Replace(objectName, @"(\s*\((\d+|Clone)\))+$", "");
    }
}
