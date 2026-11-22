/*
First Date: 7/10/2026
Date: 19/11/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory).
Unity Editor: Testing: Play, pick something up (wood, stone, food...) and select it in the hotbar: it's in the
              character's right fist, in first person (the hands) and third person (the body).
              To fit it better, change Size In Hand, Item Nudge and Item Turn WHILE PLAYING with an item in the hand,
              then right-click the component title > Copy Component, stop playing, and right-click > Paste Component
              Values (Play mode changes are lost otherwise).
Purpose: Shows the selected hotbar item in the character's right hand, like holding it (Minecraft-style): on the body
         (the Player's "Character") and on the first-person hands (Main Camera > FirstPersonHands). Each is a look-only
         copy (meshes and colors, no scripts or physics) from IconCapture, sized so its longest side is Size In Hand,
         with that side through the fist like a handle (the fist's grip comes from HandGrip, the same as a tool's).
         A tool (Item Data > Item Type = Tool) isn't shown here: ToolInHand holds it. Nothing selected, or an empty
         slot = empty fists.
         Before 19/11/2026 this floated the item at the bottom right of the screen, where the first-person fists
         (drawn on top of everything) hid it.
Requirement: HeldItemView.cs x-> PlayerInventory.cs, ItemData.cs, IconCapture.cs, HandGrip.cs
             HeldItemView.cs o-> FirstPersonHands.cs, CameraController.cs, Animator (Unity)
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class HeldItemView : MonoBehaviour
{
    // One hand that holds the item: the body's, or the first-person hands'.
    class Holder
    {
        public HandGrip grip;
        public bool handsCopy;                 // the first-person hands: no shadow. The body: only a shadow in first person.
        public GameObject held;                // the item copy in this hand (null = empty)
        public readonly List<Renderer> parts = new List<Renderer>();
        public bool shownFirstPerson;          // the view its shadows were last set for
    }

    // ===== Fields =====
    [Header("Held item")]
    [Tooltip("How big a held item is: its longest side, in meters. Every item is sized to this, big or small.")]
    [Range(0.05f, 0.6f)]
    [SerializeField] float sizeInHand = 0.2f;

    [Tooltip("Moves the item in the fist, in meters: y = toward the thumb (where a handle sticks out), " +
             "x = the way the fingers point, z = out of the palm.")]
    [SerializeField] Vector3 itemNudge = Vector3.zero;

    [Tooltip("Turns the item in the fist (degrees around its own x / y / z), after its longest side is put through the fist.")]
    [SerializeField] Vector3 itemTurn = Vector3.zero;

    const int MaxTries = 60;   // frames to keep trying when an item has no look-only copy yet (then wait for a change)

    readonly List<Holder> holders = new List<Holder>();
    PlayerInventory inventory;
    CameraController cameraControl;   // first or third person (can be missing)
    string shownId;                   // the Item Id in the hands now (null = empty hands)
    int triesLeft;                    // > 0 = the selected item had no copy yet: try again next frame

    // The item's own size, measured once per item: in meters at its own scale, around its pivot.
    Bounds itemBox;
    Vector3 itemScale;                            // the copy's own scale (keeps its proportions, e.g. a long thin plank)
    Quaternion toHandle = Quaternion.identity;    // turns its longest side onto y (through the fist)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        cameraControl = FindFirstObjectByType<CameraController>();

        // The body: the first Humanoid under the Player that isn't the first-person copy.
        foreach (Animator body in GetComponentsInChildren<Animator>(true))
        {
            if (body.GetComponent<FirstPersonHands>() == null && AddHolder(body, false)) break;
        }
        // The first-person hands (under the Main Camera).
        FirstPersonHands hands = FindFirstObjectByType<FirstPersonHands>(FindObjectsInactive.Include);
        if (hands != null) AddHolder(hands.GetComponent<Animator>(), true);

        if (holders.Count == 0)
            Debug.LogWarning("HeldItemView: no Humanoid character with a right hand (Tools > Player > Add Character Model), " +
                             "so held items don't show.", this);

        PlayerInventory.Changed += Refresh;   // also fired when the selected hotbar slot changes
        Refresh();
    }

    void OnDestroy()
    {
        PlayerInventory.Changed -= Refresh;
        Clear();
    }

    void LateUpdate()
    {
        if (triesLeft > 0) Refresh();   // the item's copy wasn't ready a moment ago

        // The body's item only casts a shadow in first person (where the body is just a shadow too).
        bool firstPerson = cameraControl != null && cameraControl.IsFirstPerson;
        foreach (Holder h in holders)
        {
            if (h.held != null && !h.handsCopy && h.shownFirstPerson != firstPerson) SetShadows(h, firstPerson);
        }
    }

    // Unity calls this when a value is changed in the Inspector: move the held item while testing.
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        foreach (Holder h in holders)
        {
            if (h.held != null) Place(h);
        }
    }

    // ===== My methods =====

    // A hand that can hold things. False = not a Humanoid with a right hand.
    bool AddHolder(Animator animator, bool handsCopy)
    {
        if (!HandGrip.TryMeasure(animator, out HandGrip grip)) return false;
        holders.Add(new Holder { grip = grip, handsCopy = handsCopy });
        return true;
    }

    // Puts the selected hotbar item in every hand (or empties them). Called whenever the inventory changes.
    void Refresh()
    {
        PlayerInventory.Slot slot = inventory != null ? inventory.ActiveItem : null;
        bool item = slot != null && !slot.IsEmpty && !(slot.data != null && slot.data.IsTool);   // a tool: ToolInHand
        string id = item ? slot.id : null;
        bool retrying = triesLeft > 0 && id == shownId;
        if (id == shownId && !retrying) return;   // the same item as before (e.g. only the count changed)

        int tries = retrying ? triesLeft - 1 : MaxTries;
        Clear();
        shownId = id;
        triesLeft = 0;
        if (id == null || holders.Count == 0) return;

        GameObject first = MakeCopy(slot);
        if (first == null)
        {
            triesLeft = tries;   // not photographed yet: try again next frame, for a moment
            return;
        }

        Measure(first);
        for (int i = 0; i < holders.Count; i++)
        {
            GameObject copy = i == 0 ? first : MakeCopy(slot);
            if (copy != null) Hold(holders[i], copy);
        }
    }

    // A look-only copy of the item. IconCapture photographs it first if it hasn't yet, which also keeps the copy.
    GameObject MakeCopy(PlayerInventory.Slot slot)
    {
        ItemData data = slot.data;
        if (data != null && data.Prefab != null) IconCapture.GetIcon(slot.id, data.Prefab);
        return IconCapture.MakeModel(slot.id);   // null if it was never photographed
    }

    // The item's own box (in meters, around its pivot) and the turn that puts its longest side through the fist.
    void Measure(GameObject copy)
    {
        Transform root = copy.transform;
        root.SetParent(null, false);
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // measured upright, at its own scale
        itemScale = root.localScale;

        bool any = false;
        Bounds box = new Bounds();
        foreach (MeshFilter mesh in copy.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            Bounds own = mesh.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)   // its 8 corners (the 3 bits of c pick - or + on each side)
            {
                Vector3 corner = own.center + Vector3.Scale(own.extents, new Vector3(
                    (c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                Vector3 point = mesh.transform.TransformPoint(corner);
                if (any) box.Encapsulate(point);
                else box = new Bounds(point, Vector3.zero);
                any = true;
            }
        }
        itemBox = any ? box : new Bounds(Vector3.zero, Vector3.one * 0.1f);

        Vector3 size = itemBox.size;
        Vector3 longest = size.x >= size.y && size.x >= size.z ? Vector3.right
                        : size.z >= size.y ? Vector3.forward
                        : Vector3.up;
        toHandle = Quaternion.FromToRotation(longest, Vector3.up);
    }

    // Puts a copy in this hand, drawn the same way as the hand.
    void Hold(Holder h, GameObject copy)
    {
        h.held = copy;
        copy.name = "Held " + shownId;
        copy.transform.SetParent(h.grip.hand, false);

        int layer = h.grip.hand.gameObject.layer;   // the first-person hands are drawn on their own layer: the item joins them
        foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
        h.parts.Clear();
        h.parts.AddRange(copy.GetComponentsInChildren<Renderer>(true));

        Place(h);
        SetShadows(h, cameraControl != null && cameraControl.IsFirstPerson);
    }

    // Its longest side through the fist, Size In Hand long in meters whatever the character's scale, its middle in the fist.
    void Place(Holder h)
    {
        Transform hand = h.grip.hand;
        float toHand = 1f / Mathf.Max(0.0001f, hand.lossyScale.x);   // world meters -> the hand's own units
        float longest = Mathf.Max(itemBox.size.x, Mathf.Max(itemBox.size.y, itemBox.size.z));
        float scale = (longest > 0.0001f ? sizeInHand / longest : 1f) * toHand;

        Quaternion turn = h.grip.rotation * Quaternion.Euler(itemTurn) * toHandle;
        Transform t = h.held.transform;
        t.localRotation = turn;
        t.localScale = itemScale * scale;
        t.localPosition = h.grip.center + h.grip.rotation * (itemNudge * toHand) - turn * (itemBox.center * scale);
    }

    void SetShadows(Holder h, bool firstPerson)
    {
        h.shownFirstPerson = firstPerson;
        ShadowCastingMode mode = h.handsCopy ? ShadowCastingMode.Off
                               : firstPerson ? ShadowCastingMode.ShadowsOnly
                               : ShadowCastingMode.On;
        foreach (Renderer part in h.parts)
        {
            if (part != null) part.shadowCastingMode = mode;
        }
    }

    // Empties every hand.
    void Clear()
    {
        foreach (Holder h in holders)
        {
            if (h.held != null) Destroy(h.held);
            h.held = null;
            h.parts.Clear();
        }
    }
}
