/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory).
        Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: Needs an EventSystem in the scene (the inventory has one), so it can tell clicks on the UI from clicks outside.
              Testing: Play, Tab, click a stack so it sticks to the cursor, then click outside the inventory window.
Purpose: Like Minecraft: while the inventory is open and a stack is on the cursor, clicking outside the window
         throws it in front of the player. Left click = the whole stack, right click = just one.
         A whole stack drops as ONE object that gives the whole amount back when picked up (no lag from 64 objects).
         The dropped object is the item's Prefab from its Item Data (real shape and collider). Items without
         Item Data use their look-only copy (IconCapture.MakeModel) with a box collider instead.
         It is picked up again the way its Item Data says (AutoPickup items wait a moment first).
Requirement: ItemDropper.cs x-> PlayerInventory.cs, ItemPickup.cs, ItemData.cs, IconCapture.cs, MenuState.cs
             ItemDropper.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: is the mouse over UI?

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class ItemDropper : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.5f, 5f)]
    [Tooltip("How far in front of the player dropped items appear, in meters.")]
    [SerializeField] float dropDistance = 1.5f;

    [Range(0f, 10f)]
    [Tooltip("How fast dropped items are thrown forward. 0 = just fall.")]
    [SerializeField] float throwSpeed = 3f;

    [Range(0f, 5f)]
    [Tooltip("Seconds a dropped AutoPickup item waits before touching collects it again " +
             "(else it comes straight back the moment it lands).")]
    [SerializeField] float pickupDelay = 1.5f;

    [Tooltip("The camera whose facing is \"in front\". Empty = the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    PlayerInventory inv;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Only while a menu (the inventory) is open, a stack is on the cursor, and the mouse is NOT over any UI
        // (clicks on slots and the window belong to them).
        if (!MenuState.AnyOpen || MenuState.KeysCaptured) return;
        if (inv.Held.IsEmpty || PointerOverUI()) return;

        if (Input.GetMouseButtonDown(0)) Drop(inv.Held.count);   // 0 = left: the whole stack
        else if (Input.GetMouseButtonDown(1)) Drop(1);           // 1 = right: just one
    }

    // ===== My methods =====

    void Drop(int count)
    {
        PlayerInventory.Slot held = inv.Held;

        // In front of the player, the way the camera looks (flat, so it isn't thrown into the ground).
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        Vector3 dropPoint = transform.position + forward * dropDistance + Vector3.up * 0.5f;

        GameObject item = held.data != null && held.data.Prefab != null
            ? FromPrefab(held.data, dropPoint)
            : FromModel(held, dropPoint);
        if (item == null)
        {
            Debug.Log("Can't drop " + held.name + ": it has no Item Data prefab, and its look wasn't saved.");
            return;
        }

        ItemPickup pickup = item.GetComponent<ItemPickup>();
        pickup.Setup(held.data, count);
        pickup.BlockAutoCollect(pickupDelay);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null) rb = item.AddComponent<Rigidbody>();   // it needs physics to fall and be thrown
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;   // smooth on screen
        rb.velocity = forward * throwSpeed + Vector3.up;         // thrown forward and a little up

        inv.RemoveFromHeld(count);
    }

    // The real prefab from the item's config file.
    static GameObject FromPrefab(ItemData data, Vector3 point)
    {
        GameObject item = Instantiate(data.Prefab, point, data.Prefab.transform.rotation);
        item.name = data.Id;   // without "(Clone)"

        if (item.GetComponentInChildren<Collider>() == null) AddBoxCollider(item);   // so it can land and be aimed at
        if (item.GetComponent<ItemPickup>() == null) item.AddComponent<ItemPickup>();
        return item;
    }

    // No config file (an older item): its look-only copy, made solid.
    static GameObject FromModel(PlayerInventory.Slot held, Vector3 point)
    {
        GameObject item = IconCapture.MakeModel(held.id);   // kept when the item was first picked up
        if (item == null) return null;

        item.name = held.id;   // ItemPickup without Data uses the object name as its Item Id
        item.transform.SetPositionAndRotation(point, Quaternion.identity);
        foreach (Transform t in item.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;   // Default layer

        AddBoxCollider(item);
        item.AddComponent<ItemPickup>();   // after the collider: its Awake thickens a too-thin collider
        return item;
    }

    // One box around all its visible parts.
    static void AddBoxCollider(GameObject item)
    {
        BoxCollider box = item.AddComponent<BoxCollider>();
        Bounds world = RendererBounds(item);
        Vector3 scale = item.transform.lossyScale;
        box.center = item.transform.InverseTransformPoint(world.center);
        box.size = new Vector3(world.size.x / Mathf.Abs(scale.x), world.size.y / Mathf.Abs(scale.y),
                               world.size.z / Mathf.Abs(scale.z));   // the box is in the item's own (scaled) units
    }

    // The box around every visible part, in world space.
    static Bounds RendererBounds(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    // True if the mouse is over any UI (the inventory window, a slot, the hotbar...).
    static bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
