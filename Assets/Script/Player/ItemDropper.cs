/*
First Date: 9/10/2026
Date: 12/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory).
        Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: Drop Style = how dropped items look: Icon (the item's picture floating on the floor, like Minecraft)
              or Bundle (a small box with the picture and amount on its sides). Tune the sizes / color below it.
              Needs an EventSystem in the scene (the inventory has one), so it can tell clicks on the UI from clicks outside.
              Testing: Play, Tab, click a stack so it sticks to the cursor, then click outside the inventory window.
              Death drop: with the GameRule Keep Inventory off (the default), PlayerHealth's ⋮ menu > "Test: Die".
Purpose: Like Minecraft: while the inventory is open and a stack is on the cursor, clicking outside the window
         throws it in front of the player. Left click = the whole stack, right click = just one.
         The stack lands as ONE DroppedStack (no lag from 64 objects) that gives the whole amount back when picked up,
         with F, or by walking near it when the GameRule Auto Pickup Drops is on (after a short wait, Pickup Delay).
         When the player dies (PlayerHealth) and the GameRule Keep Inventory is off, EVERY stack (inventory, hotbar,
         cursor) is thrown out around the body, and the inventory is emptied.
Requirement: ItemDropper.cs x-> PlayerInventory.cs, DroppedStack.cs, ItemPickup.cs, MenuState.cs, GameRules.cs
             ItemDropper.cs o-> PlayerHealth.cs
             ItemDropper.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: is the mouse over UI?

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class ItemDropper : MonoBehaviour
{
    // ===== Fields =====
    [Header("Look")]
    [Tooltip("Icon: the item's picture floating on the floor with the amount (like Minecraft). " +
             "Bundle: a small box with the picture and amount on its sides.")]
    [SerializeField] DropStyle dropStyle = DropStyle.Icon;

    [Range(0.1f, 2f)]
    [Tooltip("Icon: how wide the floating picture is, in meters.")]
    [SerializeField] float iconSize = 0.4f;

    [Range(0.1f, 2f)]
    [Tooltip("Bundle: how wide the box is, in meters.")]
    [SerializeField] float bundleSize = 0.4f;

    [Tooltip("Bundle: the box's color.")]
    [SerializeField] Color bundleColor = new Color(0.62f, 0.45f, 0.28f);   // cardboard brown

    [Header("Throwing")]
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

    [Header("On the floor")]
    [Range(0f, 3f)]
    [Tooltip("Stacks of the same item this close together merge into one, in meters. 0 = never merge.")]
    [SerializeField] float mergeRadius = 1f;

    [Range(0f, 30f)]
    [Tooltip("Minutes before a dropped stack disappears. Minecraft = 5. 0 = never.")]
    [SerializeField] float despawnMinutes = 5f;

    [Tooltip("The camera whose facing is \"in front\". Empty = the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    [Header("Dying (GameRule Keep Inventory off)")]
    [Range(0f, 10f)]
    [Tooltip("How fast the items fly out in every direction when the player dies. 0 = just fall where the player was.")]
    [SerializeField] float deathScatterSpeed = 3f;

    PlayerInventory inv;
    PlayerHealth health;   // can be missing: then nothing drops on death

    public DropStyle Style => dropStyle;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
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

    // Throws "count" of the stack on the cursor in front of the player.
    void Drop(int count)
    {
        // In front of the player, the way the camera looks (flat, so it isn't thrown into the ground).
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        Vector3 dropPoint = transform.position + forward * dropDistance + Vector3.up * 0.5f;

        DroppedStack dropped = DroppedStack.Spawn(inv.Held, count, dropPoint, CurrentLook());
        if (dropped == null) return;

        dropped.GetComponent<ItemPickup>().BlockAutoCollect(pickupDelay);
        dropped.GetComponent<Rigidbody>().velocity = forward * throwSpeed + Vector3.up;   // forward and a little up

        inv.RemoveFromHeld(count);
    }

    // The player died: drop everything, unless the GameRule Keep Inventory is on.
    void OnDied()
    {
        if (GameRules.KeepInventory) return;
        DropEverything();
    }

    // Throws every stack (inventory, hotbar and the one on the cursor) out around the player, then empties the inventory.
    public void DropEverything()
    {
        Vector3 center = transform.position + Vector3.up * 0.5f;
        DroppedStack.Look look = CurrentLook();
        int dropped = 0;

        foreach (PlayerInventory.Slot slot in inv.Slots)
        {
            if (DropAround(slot, center, look)) dropped++;
        }
        if (DropAround(inv.Held, center, look)) dropped++;

        inv.ClearAll();
        if (dropped > 0) Debug.Log("Dropped " + dropped + " stack(s) on death (GameRule Keep Inventory is off).");
    }

    // Throws one whole stack in a random direction around "center". False if the slot was empty.
    bool DropAround(PlayerInventory.Slot stack, Vector3 center, DroppedStack.Look look)
    {
        if (stack.IsEmpty) return false;

        // A random flat direction, so the stacks spread out in a ring instead of piling up on one spot.
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 flat = new Vector3(dir.x, 0f, dir.y);

        // 0.8 m out: just outside a 1 m wide player, so it doesn't start inside the body and get shoved.
        DroppedStack dropped = DroppedStack.Spawn(stack, stack.count, center + flat * 0.8f, look);
        if (dropped == null) return false;

        dropped.GetComponent<ItemPickup>().BlockAutoCollect(pickupDelay);
        dropped.GetComponent<Rigidbody>().velocity = flat * deathScatterSpeed + Vector3.up * 2f;   // out and a little up
        return true;
    }

    // The Inspector settings, packed for DroppedStack.
    DroppedStack.Look CurrentLook()
    {
        return new DroppedStack.Look
        {
            style = dropStyle,
            iconSize = iconSize,
            bundleSize = bundleSize,
            bundleColor = bundleColor,
            mergeRadius = mergeRadius,
            despawnSeconds = despawnMinutes * 60f,
        };
    }

    // True if the mouse is over any UI (the inventory window, a slot, the hotbar...).
    static bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
