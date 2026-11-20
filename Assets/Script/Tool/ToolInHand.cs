/*
First Date: 12/11/2026
Date: 19/11/2026
Author: Tony
Use by: An animated Humanoid character: the root of Assets/Prefabs/Player/PlayerCharacter.prefab. The Player's
        "Character" and the first-person hands copy (Main Camera > FirstPersonHands, a Prefab Variant of it) both get it
        from there. Tools > Items > Build Tier Tools adds it to that prefab.
Unity Editor: Testing: Play and select a tool in the hotbar: it's in the right hand, in first and third person.
              To fit it better, change Held Size, Grip Nudge and Grip Turn WHILE PLAYING with a tool in the hand (on the
              Character for third person, on FirstPersonHands for first person), then right-click the component title >
              Copy Component, stop playing, and right-click > Paste Component Values.
Purpose: When the selected hotbar item is a tool (Item Data > Item Type = Tool), a copy of it sits in this character's
         right hand: the handle through the fist near its bottom, the blade facing the way the fingers point (forward in
         a swing). The grip is worked out from the hand's own bones (wrist, knuckles, index and little finger), so it fits
         any Humanoid character. On the body it only casts a shadow in first person, like the body; on the first-person
         hands it's drawn with the hands, on top of the world.
         Every other item (wood, food...) is held in the same fist by HeldItemView; both get the fist from HandGrip.
Requirement: ToolInHand.cs x-> Animator (Unity), PlayerInventory.cs, ItemData.cs, ToolStats.cs, HandGrip.cs
             ToolInHand.cs o-> CameraController.cs, FirstPersonHands.cs, IconCapture.cs
             ToolInHand.cs <-x PlayerCharacterBuilder.cs, TierToolBuilder.cs
             ToolInHand.cs <-o GearPreview.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode

[DisallowMultipleComponent]
public class ToolInHand : MonoBehaviour
{
    // ===== Fields =====
    [Header("Held tool")]
    [Tooltip("How big a held tool is: 1 = the tool pack's own size (about 1 m long), 0.8 = 80 %.")]
    [Range(0.2f, 2f)]
    [SerializeField] float heldSize = 0.8f;

    [Tooltip("Moves the tool in the hand, in meters: y = along the handle (more = the hand holds it lower down), " +
             "x = the way the blade faces, z = out of the palm.")]
    [SerializeField] Vector3 gripNudge = Vector3.zero;

    [Tooltip("Turns the tool in the hand (degrees around its own x / y / z), after the grip worked out from the hand. " +
             "y 180 = the blade faces the other way.")]
    [SerializeField] Vector3 gripTurn = Vector3.zero;

    [Tooltip("Spins a held axe around its handle, in degrees (after Grip Turn): 180 = its sharp edge faces the other way, " +
             "toward what it chops. 0 = the way the pack made it. Only axes; change it while playing to see it.")]
    [SerializeField] float axeTurn = 180f;

    // Where the fist holds each kind, in meters up the handle from its bottom (the pack's tools are about 1 m long).
    const float SwordGrip = 0.13f;   // the middle of the 30 cm hilt
    const float ToolGrip = 0.1f;     // near the end of an axe or pickaxe handle

    Animator animator;
    Transform hand;                  // the right hand bone (the wrist)
    bool handsCopy;                  // the first-person hands (no shadow) or the body (shadow only in first person)
    CameraController cameraControl;  // first or third person (can be missing)
    Vector3 gripCenter;              // where the handle goes through the fist, in the hand's own space
    Vector3 thumbSide;               // the way the handle points out of the fist (the thumb's side), in the hand's space
    Quaternion gripRotation = Quaternion.identity;   // turns a tool (handle = its y, blade = its x) into the fist

    GameObject held;                 // the tool copy in the hand (null = empty hand)
    ItemData heldData;               // what it is a copy of
    readonly List<Renderer> heldParts = new List<Renderer>();
    bool shownFirstPerson;           // the view its shadows were last set for

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (hand == null)
        {
            Debug.LogWarning(name + ": ToolInHand needs a Humanoid Animator with a right hand, so no tool shows in it.");
            enabled = false;
            return;
        }
        handsCopy = GetComponentInParent<FirstPersonHands>() != null;
        cameraControl = FindFirstObjectByType<CameraController>();

        MeasureGrip();
        PlayerInventory.Changed += Refresh;   // also fired when the selected hotbar slot changes
        Refresh();
    }

    void OnDestroy()
    {
        PlayerInventory.Changed -= Refresh;
    }

    // The body's tool casts only a shadow in first person (where the body is just a shadow too).
    void LateUpdate()
    {
        if (held == null || handsCopy) return;
        bool firstPerson = cameraControl != null && cameraControl.IsFirstPerson;
        if (firstPerson != shownFirstPerson) SetShadows(firstPerson);
    }

    // Unity calls this when a value is changed in the Inspector: move the held tool while testing.
    void OnValidate()
    {
        if (Application.isPlaying && held != null && hand != null) Place();
    }

    // ===== My methods =====

    // Shows the selected hotbar item in the hand if it's a tool, else nothing.
    void Refresh()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        PlayerInventory.Slot slot = inventory != null ? inventory.ActiveItem : null;
        ItemData data = slot != null && !slot.IsEmpty && slot.data != null && slot.data.IsTool ? slot.data : null;
        if (data == heldData && (data == null || held != null)) return;   // the same tool as before (or still none)

        if (held != null) Destroy(held);
        held = null;
        heldParts.Clear();
        heldData = data;
        if (data == null) return;

        held = MakeCopy(data);
        if (held == null) return;
        held.name = "Held " + data.DisplayName;
        held.transform.SetParent(hand, false);

        int layer = hand.gameObject.layer;   // the first-person hands are drawn on their own layer: the tool joins them
        foreach (Transform part in held.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
        heldParts.AddRange(held.GetComponentsInChildren<Renderer>(true));
        Place();
        SetShadows(cameraControl != null && cameraControl.IsFirstPerson);
    }

    // A look-only copy of the tool: its prefab's "Model" child (Tools > Items > Build Tier Tools makes it, with no scripts
    // or colliders), else IconCapture's copy of the whole item.
    GameObject MakeCopy(ItemData data)
    {
        Transform model = data.Prefab != null ? data.Prefab.transform.Find("Model") : null;
        if (model != null) return Instantiate(model.gameObject);
        return IconCapture.MakeModel(data.Id);   // null if the item was never shown in the inventory this session
    }

    // Puts the tool in the fist: turned by the grip, its size in meters whatever the character's scale, and the point
    // of the handle the fist holds (SwordGrip / ToolGrip up from its bottom) right in the fist.
    void Place()
    {
        float toHand = 1f / Mathf.Max(0.0001f, hand.lossyScale.x);   // world meters -> the hand's own units
        float scale = heldSize * toHand;
        float grip = (heldData != null && heldData.Tool.kind == ToolKind.Sword ? SwordGrip : ToolGrip) * scale;

        bool axe = heldData != null && heldData.Tool.kind == ToolKind.Axe;
        Quaternion spin = axe ? Quaternion.Euler(0f, axeTurn, 0f) : Quaternion.identity;   // around the handle only: the grip stays
        held.transform.localRotation = gripRotation * Quaternion.Euler(gripTurn) * spin;
        held.transform.localScale = Vector3.one * scale;
        held.transform.localPosition = gripCenter - thumbSide * grip + gripRotation * (gripNudge * toHand);
    }

    // The fist's directions in the hand's own space, worked out by HandGrip from where the knuckles and fingers are
    // (HeldItemView holds other items the same way). Those spots don't move in the hand's space whatever the pose, so
    // this is measured once.
    void MeasureGrip()
    {
        HandGrip grip = HandGrip.Measure(animator, hand);
        gripCenter = grip.center;         // inside the closed fist
        thumbSide = grip.thumbSide;       // the way the handle sticks out of the fist
        gripRotation = grip.rotation;     // tool z -> palm, y (handle) -> thumb side, x (blade) -> fingers
    }

    void SetShadows(bool firstPerson)
    {
        shownFirstPerson = firstPerson;
        ShadowCastingMode mode = handsCopy ? ShadowCastingMode.Off
                               : firstPerson ? ShadowCastingMode.ShadowsOnly
                               : ShadowCastingMode.On;
        foreach (Renderer part in heldParts)
        {
            if (part != null) part.shadowCastingMode = mode;
        }
    }
}
