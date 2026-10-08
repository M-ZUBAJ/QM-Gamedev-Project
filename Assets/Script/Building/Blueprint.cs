/*
First Date: 5/10/2026
Date: 7/10/2026
Author: Tony
Use by: Nothing to set up. BlueprintPlacer makes blueprints with Blueprint.Create(...).
Purpose: A building that is planned but not built yet: a see-through blue copy of the building's prefab.
         - While placing: no collisions, blue = can be placed, red = can't.
         - Placed: its colliders are triggers, so the crosshair can aim at it but nothing bumps into it.
         - Build() swaps the blueprint for the real building (e.g. once the recipe is paid).
         The copy's own scripts are removed, so e.g. a blueprint of an InteractionMachine
         doesn't already work as a machine.
Requirement: Blueprint.cs x-> BuildingData.cs
             Blueprint.cs <-x BlueprintPlacer.cs
*/

using UnityEngine;

public class Blueprint : MonoBehaviour
{
    // ===== Fields =====
    // Tuning: the hologram colors (A = see-through amount).
    static readonly Color ValidColor = new Color(0.3f, 0.65f, 1f, 0.35f);
    static readonly Color InvalidColor = new Color(1f, 0.25f, 0.25f, 0.35f);
    const float OverlapMargin = 0.02f;   // the blueprint may touch other things by this much (e.g. sitting on the ground)

    static Transform holder;    // switched-off parent: copies made under it don't run Awake
    static Material validMat;   // shared by every blueprint (made once)
    static Material invalidMat;
    static readonly Collider[] overlapBuffer = new Collider[32];   // reused, so checking makes no garbage

    BuildingData data;
    Renderer[] renderers;       // every shape, found once (the placing checks run every frame)
    Collider[] colliders;
    bool placed;

    public BuildingData Data => data;
    public bool IsPlaced => placed;

    // ===== My methods =====

    // Makes a new blueprint of this building (in "placing" mode). Move it, then call Place() or Destroy it.
    public static Blueprint Create(BuildingData data)
    {
        if (holder == null)
        {
            GameObject h = new GameObject("Blueprint Holder (inactive)");
            h.SetActive(false);   // children of a switched-off object don't run Awake
            holder = h.transform;
        }

        GameObject go = Instantiate(data.Prefab, holder);
        go.name = data.DisplayName + " (Blueprint)";
        RemoveScripts(go);

        Blueprint bp = go.AddComponent<Blueprint>();
        bp.Setup(data);

        go.transform.SetParent(null);   // out of the holder: now it shows in the world
        return bp;
    }

    // Takes out every script, Rigidbody and joint, so the copy only looks like the building.
    // DestroyImmediate: Destroy waits until the end of the frame, and the copy is switched on before that.
    static void RemoveScripts(GameObject go)
    {
        MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = scripts.Length - 1; i >= 0; i--)   // backwards: scripts that need others are usually added later
        {
            DestroyImmediate(scripts[i]);
        }

        foreach (Joint j in go.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
    }

    void Setup(BuildingData buildingData)
    {
        data = buildingData;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        SetCollidersOn(false);   // while placing: nothing to bump into or aim at
        SetValid(true);
    }

    // ----- Placing -----

    // Blue = can be placed here, red = can't.
    public void SetValid(bool valid)
    {
        Material mat = GetHologram(valid);
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    // Puts the blueprint down where it is: the crosshair can now aim at it.
    public void Place()
    {
        placed = true;
        SetCollidersOn(true);
        SetValid(true);
    }

    // True if any shape is inside something else (a wall, a tree, the player, another blueprint...).
    public bool Overlaps()
    {
        foreach (Renderer r in renderers)
        {
            GetBox(r, out Vector3 center, out Vector3 halfSize, out Quaternion turn);

            int count = Physics.OverlapBoxNonAlloc(center, halfSize, overlapBuffer, turn,
                                                   Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (Blocks(overlapBuffer[i])) return true;
            }
        }
        return false;
    }

    // True if something under the building holds it up.
    public bool IsSupported()
    {
        // Look a little way down from the bottom of every shape. One touching the ground is enough.
        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            Vector3 from = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);

            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 0.2f,
                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    // Should this collider stop the blueprint from being placed?
    bool Blocks(Collider other)
    {
        if (other.transform.IsChildOf(transform)) return false;                       // our own shape
        if (other.isTrigger) return other.GetComponentInParent<Blueprint>() != null; // only other blueprints' triggers
        return true;
    }

    // A box around one shape that turns with it (tighter than Unity's bounds, which never turn).
    static void GetBox(Renderer r, out Vector3 center, out Vector3 halfSize, out Quaternion turn)
    {
        MeshFilter mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds mb = mf.sharedMesh.bounds;   // the mesh's own box, before scaling and turning
            center = r.transform.TransformPoint(mb.center);
            halfSize = Vector3.Scale(mb.extents, r.transform.lossyScale);
            turn = r.transform.rotation;
        }
        else
        {
            center = r.bounds.center;
            halfSize = r.bounds.extents;
            turn = Quaternion.identity;
        }

        // A little smaller, so touching (e.g. standing on the ground) doesn't count as inside.
        halfSize = new Vector3(Mathf.Abs(halfSize.x), Mathf.Abs(halfSize.y), Mathf.Abs(halfSize.z));
        halfSize = Vector3.Max(halfSize - Vector3.one * OverlapMargin, Vector3.one * 0.001f);
    }

    // ----- Building it -----

    // Swaps the blueprint for the real building (with its scripts working). Returns the new building.
    public GameObject Build()
    {
        GameObject building = Instantiate(data.Prefab, transform.position, transform.rotation);
        Destroy(gameObject);
        return building;
    }

    // ----- Look -----

    // Placing: all off. Placed: on, as triggers (aimable, but nothing bumps into them).
    void SetCollidersOn(bool on)
    {
        foreach (Collider c in colliders)
        {
            c.enabled = on;
            SetTrigger(c, true);
        }
    }

    // Unity doesn't allow a non-convex Mesh Collider to be a trigger, so those stay solid.
    static void SetTrigger(Collider c, bool trigger)
    {
        if (trigger && c is MeshCollider mc && !mc.convex) return;
        c.isTrigger = trigger;
    }

    static Material GetHologram(bool valid)
    {
        // "Sprites/Default" is a simple see-through shader that is always in the game build.
        if (validMat == null) validMat = new Material(Shader.Find("Sprites/Default")) { color = ValidColor };
        if (invalidMat == null) invalidMat = new Material(Shader.Find("Sprites/Default")) { color = InvalidColor };
        return valid ? validMat : invalidMat;
    }
}
