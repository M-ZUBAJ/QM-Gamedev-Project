/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory).
Unity Editor: Tune it WHILE PLAYING: hold an item (pick one up, put it in the hotbar, press its number key),
              then change Offset / Rotation / Item Size in the Inspector and watch the Game view.
              Right-click the component title > Copy Component, stop playing, right-click > Paste Component Values
              to keep the numbers (Play mode changes are lost otherwise).
Purpose: Shows the selected hotbar item at the bottom right of the screen, like the player is holding it.
         It is a look-only copy (no scripts or physics), made by IconCapture when the item was picked up.
         It follows the camera; turning makes it lag a little (sway) and walking makes it bob.
         Nothing selected, or an empty hotbar slot = empty hand.
Requirement: HeldItemView.cs x-> PlayerInventory.cs, IconCapture.cs, MenuState.cs
             HeldItemView.cs o-> PlayerController.cs, CameraController.cs
*/

using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]   // move the item AFTER the camera has moved this frame, so it never shakes
public class HeldItemView : MonoBehaviour
{
    // ===== Fields =====
    [Header("Where (from the camera)")]
    [Tooltip("x = right, y = up (negative = down), z = forward, in meters from the camera. " +
             "Bigger z = farther from the player's eyes.")]
    [SerializeField] Vector3 offset = new Vector3(0.35f, -0.3f, 0.6f);

    [Tooltip("Turn of the item in degrees: x = tip forward, y = turn left/right, z = roll.")]
    [SerializeField] Vector3 rotation = new Vector3(10f, -25f, 0f);

    [Range(0.05f, 1f)]
    [Tooltip("How big the item looks: its longest side, in meters. Every item is scaled to this, big or small.")]
    [SerializeField] float itemSize = 0.25f;

    [Header("Feel")]
    [Range(0f, 30f)]
    [Tooltip("How fast the item catches up when you turn. Lower = more sway. 0 = no sway (glued to the camera).")]
    [SerializeField] float swaySpeed = 12f;

    [Range(0f, 0.1f)]
    [Tooltip("How far the item bobs up and down while walking, in meters. 0 = no bob.")]
    [SerializeField] float bobAmount = 0.015f;

    [Range(0f, 20f)]
    [Tooltip("How fast it bobs while walking (steps).")]
    [SerializeField] float bobSpeed = 9f;

    [Header("When to show")]
    [Tooltip("Off: also show it in third person (F5). On: only in first person.")]
    [SerializeField] bool firstPersonOnly = false;

    [Tooltip("Hide it while a menu is open (inventory, build menu...).")]
    [SerializeField] bool hideInMenus = false;

    [Tooltip("Ticked: the item casts a shadow on the ground. Usually off for items in hand.")]
    [SerializeField] bool castShadows = false;

    [Tooltip("The camera the item follows. Empty = the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    const int IGNORE_RAYCAST_LAYER = 2;   // the crosshair looks straight through the item in hand

    PlayerInventory inv;
    PlayerController controller;     // for the walking bob (can be missing)
    CameraController cameraControl;  // for first / third person (can be missing)

    Transform hand;                  // moves with the camera; the item model sits in it
    GameObject model;                // the item shown right now (null = empty hand)
    Vector3 modelScale;              // the model's own scale (keeps its proportions, e.g. a long thin stick)
    string shownId;                  // its Item Id
    Quaternion swayRotation;         // the camera's turn, followed a bit late (sway)
    float bobTime;
    float bobWeight;                 // 0 = standing still, 1 = walking (eases between them)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null)
        {
            cameraControl = cameraTransform.GetComponent<CameraController>();
            swayRotation = cameraTransform.rotation;
        }

        hand = new GameObject("HeldItem (hand)").transform;   // not a child of the player: it follows the camera
        Refresh();
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // also fired when the selected hotbar slot changes
        if (hand != null) Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
        if (hand != null) hand.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (hand != null) Destroy(hand.gameObject);
    }

    void LateUpdate()
    {
        if (hand == null || cameraTransform == null) return;

        bool show = model != null
                    && !(hideInMenus && MenuState.AnyOpen)
                    && !(firstPersonOnly && cameraControl != null && !cameraControl.IsFirstPerson);
        if (hand.gameObject.activeSelf != show) hand.gameObject.SetActive(show);
        if (!show)
        {
            swayRotation = cameraTransform.rotation;   // appear in place next time, not swinging in
            return;
        }

        // Sway: the item's turn follows the camera's turn a little late. 0 = glued.
        // (1 - e^(-speed x time) is the same smooth catch-up at any frame rate.)
        swayRotation = swaySpeed <= 0f
            ? cameraTransform.rotation
            : Quaternion.Slerp(swayRotation, cameraTransform.rotation, 1f - Mathf.Exp(-swaySpeed * Time.deltaTime));

        // Position follows the camera exactly (no lag while walking), turned by the swaying rotation.
        hand.position = cameraTransform.position + swayRotation * (offset + Bob());
        hand.rotation = swayRotation * Quaternion.Euler(rotation);
    }

    // Unity calls this when a value is changed in the Inspector, so Item Size updates while testing.
    void OnValidate()
    {
        if (Application.isPlaying && model != null && hand != null) FitModel();
    }

    // ===== My methods =====

    // Makes the hand show the selected hotbar item. Called whenever the inventory changes.
    void Refresh()
    {
        PlayerInventory.Slot active = inv.ActiveItem;
        string id = active != null && !active.IsEmpty ? active.id : null;
        if (id == shownId && (id == null || model != null)) return;   // same item as before (e.g. only the count changed)

        if (model != null) Destroy(model);
        model = null;
        shownId = id;
        if (id == null || hand == null) return;

        model = IconCapture.MakeModel(id);   // null if this item was never seen this play session
        if (model == null) return;

        model.transform.SetParent(hand, false);
        modelScale = model.transform.localScale;
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IGNORE_RAYCAST_LAYER;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        FitModel();
    }

    // Scales the model so its longest side is itemSize, and centers it in the hand.
    void FitModel()
    {
        // Measure it in the hand's own space: put it back to its own size, measure, then scale and center.
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = modelScale;

        Bounds box = LocalBounds(model);
        float longest = Mathf.Max(box.size.x, Mathf.Max(box.size.y, box.size.z));
        float scale = longest > 0.0001f ? itemSize / longest : 1f;

        model.transform.localScale = modelScale * scale;          // same proportions, just bigger or smaller
        model.transform.localPosition = -box.center * scale;   // the middle of the item sits on the hand point
    }

    // The box around every mesh of the model, in the hand's directions.
    Bounds LocalBounds(GameObject go)
    {
        Bounds box = new Bounds();
        bool first = true;

        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Bounds b = mf.sharedMesh.bounds;   // the mesh's own box

            // Its 8 corners, moved into the hand's space.
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                    (c & 1) == 0 ? -1f : 1f,     // the 3 bits of c pick - or + on each axis
                    (c & 2) == 0 ? -1f : 1f,
                    (c & 4) == 0 ? -1f : 1f));
                Vector3 p = hand.InverseTransformPoint(mf.transform.TransformPoint(corner));

                if (first) { box = new Bounds(p, Vector3.zero); first = false; }
                else box.Encapsulate(p);
            }
        }
        return box;
    }

    // Up-and-down (and a little side-to-side) while walking, easing in and out.
    Vector3 Bob()
    {
        bool walking = controller != null && controller.IsMoving;
        bobWeight = Mathf.MoveTowards(bobWeight, walking ? 1f : 0f, Time.deltaTime * 4f);   // a quarter second to start / stop
        if (bobWeight <= 0f || bobAmount <= 0f) return Vector3.zero;

        bobTime += Time.deltaTime * bobSpeed;
        float up = Mathf.Abs(Mathf.Sin(bobTime)) * bobAmount;   // one bounce per step
        float side = Mathf.Sin(bobTime) * bobAmount * 0.5f;     // swing left and right between steps
        return new Vector3(side, -up, 0f) * bobWeight;
    }
}
