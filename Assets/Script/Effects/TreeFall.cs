/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: A tree's top object: Turki's TreeNode prefab (its looks in Prefabs/ResourceNodes/Trees get it from there).
        Tools > Effects > Build Tree Fall puts it on every tree prefab and fills in Chips, Closed Meshes and Cut Wood.
Unity Editor: Testing: Play and chop a tree down (punch it, or hit it with an axe): it tips over away from you, slams
              into the ground in a burst of brown wood chips, and breaks apart into more of them.
              ⋮ (top right of this component) > Test: Fall shows it while playing without chopping (the tree stays).
Purpose: When a tree is chopped down (Turki's ResourceNode reaches 0 health and removes it at once), a look-only copy
         of it (its meshes and materials, no scripts or colliders) falls over from the foot of its trunk, away from the
         player, slowly at first and then faster, like a real tree. Chips fly from the cut as it starts, it lands in a
         burst of brown chips along the trunk, then breaks apart into chips and is gone (FallingTree does the falling).
         The wood is already in the inventory (ResourceNode gives it at once); this is only the show.
         What the tree looks like is remembered when the game starts, because by the time it's removed its parts may
         already be gone. A tree removed without being chopped (the scene closing) shows nothing.
         The pack's trunks are open tubes at the bottom (a standing tree hides that end in the ground), and a falling
         tree turns that end toward the player, who would see straight through the trunk. So the copy uses the meshes
         in Closed Meshes instead, where that end is closed with light cut wood (Build Tree Fall makes them).
Requirement: TreeFall.cs x-> FallingTree.cs
             TreeFall.cs o-> ResourceNode.cs, PlayerController.cs
             TreeFall.cs <-x TreeFallBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TreeFall : MonoBehaviour
{
    // One mesh of the tree, remembered: what it is, its materials, and where it sits on the tree.
    struct Part
    {
        public Mesh mesh;
        public Material[] materials;
        public Matrix4x4 onTree;              // its place, turn and size in the tree's own space
        public MaterialPropertyBlock colors;  // color overrides (e.g. ShapeColor), or null
    }

    // One of the tree's meshes and the same mesh with the bottom of its trunk closed (made by Build Tree Fall).
    [System.Serializable]
    public struct ClosedMesh
    {
        [Tooltip("The tree's own mesh (open at the bottom of the trunk).")]
        public Mesh open;

        [Tooltip("The same mesh with that end closed: it has one more sub-mesh, the cut, which gets Cut Wood.")]
        public Mesh closed;
    }

    // ===== Fields =====
    [Header("Look")]
    [Tooltip("The wood chips: Assets/Prefabs/Effects/WoodChips (made by Tools > Effects > Build Tree Fall).")]
    [SerializeField] ParticleSystem chips;

    [Tooltip("The darkest brown a chip can be (bark).")]
    [SerializeField] Color darkWood = new Color(0.33f, 0.2f, 0.09f);

    [Tooltip("The lightest brown a chip can be (fresh wood).")]
    [SerializeField] Color lightWood = new Color(0.7f, 0.49f, 0.27f);

    [Header("Falling")]
    [Tooltip("Seconds the tree takes to fall over.")]
    [Range(0.3f, 3f)]
    [SerializeField] float fallSeconds = 1.2f;

    [Tooltip("How far it tips over, in degrees (90 = flat on the ground; a little less looks like it rests on its branches).")]
    [Range(30f, 95f)]
    [SerializeField] float fallAngle = 86f;

    [Tooltip("Seconds the fallen tree lies there before it breaks apart.")]
    [Range(0f, 2f)]
    [SerializeField] float lieSeconds = 0.25f;

    [Tooltip("Seconds it takes to break apart into chips and be gone.")]
    [Range(0.2f, 3f)]
    [SerializeField] float breakSeconds = 0.9f;

    [Header("Chips")]
    [Tooltip("Chips flying from the cut when it starts to fall.")]
    [Min(0)]
    [SerializeField] int cutChips = 30;

    [Tooltip("Chips bursting up along the trunk when it lands.")]
    [Min(0)]
    [SerializeField] int landChips = 160;

    [Tooltip("Chips per second while it breaks apart.")]
    [Min(0f)]
    [SerializeField] float breakRate = 220f;

    [Tooltip("How fast the chips fly, in meters per second.")]
    [Min(0f)]
    [SerializeField] float chipSpeed = 2.6f;

    [Header("Closed trunk")]
    [Tooltip("The pack's trunks are open tubes at the bottom (hidden in the ground while the tree stands). The falling copy uses these closed versions, so the player can't see through the trunk. Filled by Tools > Effects > Build Tree Fall.")]
    [SerializeField] List<ClosedMesh> closedMeshes = new List<ClosedMesh>();

    [Tooltip("The closed end of the trunk: light, fresh-cut wood with rings (Assets/Effects/TreeFall/CutWood). Empty = the tree's own material.")]
    [SerializeField] Material cutWood;

    ResourceNode node;   // Turki's: the tree's health (missing = never falls by itself)
    bool played;         // the fall already happened (so it never happens twice)
    readonly List<Part> parts = new List<Part>();
    float height = 5f;   // meters, from the foot of the trunk to the top of the leaves
    static bool quitting;   // the game is closing: removed trees show nothing

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        node = GetComponent<ResourceNode>();
        Remember();
    }

    // Turki's ResourceNode removes a chopped tree at once: this is the last moment it's still here.
    void OnDestroy()
    {
        if (played || quitting || !gameObject.scene.isLoaded) return;
        if (node == null || node.currentHealth > 0f) return;   // not chopped (e.g. removed by something else)
        played = true;
        Fall();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    static void OnQuitting()
    {
        quitting = true;
    }

    [ContextMenu("Test: Fall")]
    void TestFall()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Press Play first.");
            return;
        }
        Fall();   // a copy falls next to the tree, which stays
    }

    // ===== My methods =====

    // A copy of the tree falls away from the player and breaks into chips.
    public void Fall()
    {
        GameObject copy = MakeCopy();
        if (copy == null) return;

        ParticleSystem chipCopy = null;
        if (chips != null)
        {
            chipCopy = Instantiate(chips, transform.position, Quaternion.identity);   // not a child: it stays when the tree goes
            chipCopy.Play();   // its own emission is off: only FallingTree's Emit makes chips
        }
        else Debug.LogWarning(name + ": TreeFall has no Chips. Run Tools > Effects > Build Tree Fall.");

        FallingTree.Settings how = new FallingTree.Settings
        {
            fallSeconds = fallSeconds,
            fallAngle = fallAngle,
            lieSeconds = lieSeconds,
            breakSeconds = breakSeconds,
            cutChips = cutChips,
            landChips = landChips,
            breakRate = breakRate,
            chipSpeed = chipSpeed,
            darkWood = darkWood,
            lightWood = lightWood,
        };
        copy.AddComponent<FallingTree>().Begin(how, AwayFromPlayer(), height, chipCopy);
    }

    // What the tree looks like: every mesh with its materials and its place on the tree, and how tall it is.
    void Remember()
    {
        parts.Clear();
        Matrix4x4 toTree = transform.worldToLocalMatrix;
        bool found = false;
        Bounds box = new Bounds(transform.position, Vector3.zero);
        foreach (MeshRenderer look in GetComponentsInChildren<MeshRenderer>())
        {
            MeshFilter filter = look.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;

            Mesh mesh = filter.sharedMesh;
            Material[] materials = look.sharedMaterials;
            Mesh closed = ClosedVersionOf(mesh);   // the same mesh with the bottom of the trunk closed, if there is one
            if (closed != null)
            {
                materials = WithCutWood(materials, mesh.subMeshCount, closed.subMeshCount);
                mesh = closed;
            }

            Part part = new Part
            {
                mesh = mesh,
                materials = materials,
                onTree = toTree * look.transform.localToWorldMatrix,
            };
            if (look.HasPropertyBlock())
            {
                part.colors = new MaterialPropertyBlock();
                look.GetPropertyBlock(part.colors);
            }
            parts.Add(part);

            if (found) box.Encapsulate(look.bounds);
            else box = look.bounds;
            found = true;
        }
        if (found) height = Mathf.Max(1f, box.max.y - transform.position.y);
    }

    // The closed version of one of the tree's meshes (from Closed Meshes), or null if it has none.
    Mesh ClosedVersionOf(Mesh open)
    {
        foreach (ClosedMesh pair in closedMeshes)
        {
            if (pair.open == open && pair.closed != null) return pair.closed;
        }
        return null;
    }

    // The materials for a closed mesh: the tree's own ones for its own sub-meshes (the parts of a mesh that each get a
    // material), then Cut Wood for the closed ends (the tree's last material if Cut Wood is empty).
    Material[] WithCutWood(Material[] own, int ownSubMeshes, int allSubMeshes)
    {
        if (own.Length == 0) return own;
        Material[] all = new Material[allSubMeshes];
        for (int i = 0; i < allSubMeshes; i++)
        {
            if (i < ownSubMeshes) all[i] = own[Mathf.Min(i, own.Length - 1)];
            else all[i] = cutWood != null ? cutWood : own[own.Length - 1];
        }
        return all;
    }

    // A look-only copy built from what was remembered. Its top object sits where the tree's does (the foot of the
    // trunk), so it tips over from there.
    GameObject MakeCopy()
    {
        if (parts.Count == 0) return null;

        GameObject pivot = new GameObject(name + " (falling)");
        pivot.transform.SetPositionAndRotation(transform.position, transform.rotation);
        pivot.transform.localScale = transform.lossyScale;
        foreach (Part part in parts)
        {
            GameObject look = new GameObject("Look");
            look.transform.SetParent(pivot.transform, false);
            look.transform.localPosition = part.onTree.GetColumn(3);
            look.transform.localRotation = part.onTree.rotation;
            look.transform.localScale = part.onTree.lossyScale;
            look.AddComponent<MeshFilter>().sharedMesh = part.mesh;
            MeshRenderer drawn = look.AddComponent<MeshRenderer>();
            drawn.sharedMaterials = part.materials;
            if (part.colors != null) drawn.SetPropertyBlock(part.colors);
        }
        return pivot;
    }

    // The way it falls: straight away from the player (flat on the ground), or any way if there's no player.
    Vector3 AwayFromPlayer()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector3 away = player != null ? transform.position - player.transform.position : Vector3.zero;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
        {
            Vector2 any = Random.insideUnitCircle.normalized;
            away = new Vector3(any.x, 0f, any.y);
        }
        return away.normalized;
    }
}
