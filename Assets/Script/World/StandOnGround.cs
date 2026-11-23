/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: The top object of anything that should rest on the floor where it's spawned or placed, e.g. the Chest prefab.
        Drag it on. Tools > World > Set Up Chests puts it on chests and measures Foot Offset.
Unity Editor: Testing: Play: every chest stands on the grass (tilted a little on a slope), whatever height
              ObjectScatter spawned it at or someone placed it at. ⋮ (top right of this component) > Settle Now puts it
              on the floor right away, also while not playing (Ctrl + Z undoes it).
Purpose: Puts the object on the floor under it when the game starts, so it never floats or sinks. It looks straight
         down for the nearest solid thing (the terrain, a built floor, a rock; not itself, and not the player or
         anything else physics moves), stands its bottom there, sinks a hair in so no gap shows on bumpy ground, and on
         a slope tilts to lie flat (up to Max Tilt) while still facing the same way.
Requirement: StandOnGround.cs <-x BerryBushBuilder.cs, ChestSetup.cs, LootChestBuilder.cs, RockBuilder.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class StandOnGround : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Meters from this object's pivot down to the bottom of its model (0 = the pivot is at the bottom). " +
             "Tools > World > Set Up Chests measures it. Below 0 = measured from the model when the game starts.")]
    [SerializeField] float footOffset = -1f;

    [Tooltip("Tilts up to this many degrees to lie flat on a slope. 0 = always stands straight up.")]
    [Range(0f, 45f)]
    [SerializeField] float maxTilt = 15f;

    [Tooltip("How far it sinks into the floor (meters), so no gap shows under a corner on bumpy ground.")]
    [Range(0f, 0.2f)]
    [SerializeField] float sink = 0.02f;

    [Tooltip("How far above its pivot it starts looking for the floor (meters). Keep it lower than a roof or shelf " +
             "over it, or it lands on top of that.")]
    [Range(0.2f, 5f)]
    [SerializeField] float lookAbove = 1f;

    const float LookBelow = 3f;   // meters below its bottom it still looks for a floor
    static readonly RaycastHit[] hits = new RaycastHit[16];

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        Settle();
    }

    [ContextMenu("Settle Now")]
    void SettleNow()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Settle On The Floor");
#endif
        if (!Settle()) Debug.Log(name + ": no floor found under it, so it stays where it is.");
    }

    // ===== My methods =====

    // Puts it on the floor right below it. False = no floor found (it stays where it is).
    public bool Settle()
    {
        Physics.SyncTransforms();   // things spawned this frame (trees, rocks) are really where they look, before looking
        float foot = FootOffset();
        float above = Mathf.Max(lookAbove, 0.1f);
        Vector3 from = transform.position + Vector3.up * above;
        int count = Physics.RaycastNonAlloc(from, Vector3.down, hits, above + foot + LookBelow, ~0, QueryTriggerInteraction.Ignore);

        bool found = false;
        RaycastHit floor = default;
        for (int i = 0; i < count; i++)
        {
            if (!IsFloor(hits[i].collider)) continue;
            if (!found || hits[i].distance < floor.distance) floor = hits[i];   // the first floor under it
            found = true;
        }
        if (!found) return false;

        // Lie flat on the slope, up to Max Tilt, still facing the same way.
        Vector3 up = floor.normal;
        float slope = Vector3.Angle(Vector3.up, up);
        if (slope > maxTilt) up = Vector3.Slerp(Vector3.up, up, maxTilt / slope);
        transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.position = floor.point + transform.up * (foot - sink);
        return true;
    }

    // Solid and still: not part of this object, and nothing physics moves around (the player, an enemy, a dropped item).
    bool IsFloor(Collider other)
    {
        if (other.transform == transform || other.transform.IsChildOf(transform)) return false;
        Rigidbody body = other.attachedRigidbody;
        return body == null || body.isKinematic;
    }

    // Meters from the pivot down to the model's bottom: Foot Offset, else measured from its meshes' box.
    float FootOffset()
    {
        if (footOffset >= 0f) return footOffset * transform.lossyScale.y;

        bool any = false;
        Bounds box = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer look in GetComponentsInChildren<Renderer>())
        {
            if (!(look is MeshRenderer) && !(look is SkinnedMeshRenderer)) continue;
            if (any) box.Encapsulate(look.bounds);
            else box = look.bounds;
            any = true;
        }
        return any ? Mathf.Max(0f, transform.position.y - box.min.y) : 0f;
    }
}
