/*
First Date: 12/11/2026
Date: 22/11/2026
Author: Tony
Use by: An animated Humanoid character: the root of Assets/Prefabs/Player/PlayerCharacter.prefab. The Player's
        "Character" and the first-person hands copy (Main Camera > FirstPersonHands, a Prefab Variant of it) both get it
        from there. Tools > Items > Build Tier Tools adds it to that prefab.
Unity Editor: Testing: Play and select a tool in the hotbar: it's in the right hand, in first and third person. A bow is in
              the left hand (in first person it shows while you draw it), with an arrow on its string while it's drawn.
              To fit them better, change Held Size, Grip Nudge and Grip Turn (Bow Size, Bow Nudge, Bow Turn, Arrow Size
              for the bow) WHILE PLAYING with it in the hand (on the Character for third person, on FirstPersonHands for
              first person), then right-click the component title > Copy Component, stop playing, and right-click >
              Paste Component Values.
Purpose: When the selected hotbar item is a tool (Item Data > Item Type = Tool), a copy of it sits in this character's
         right hand: the handle through the fist near its bottom, the blade facing the way the fingers point (forward in
         a swing). The grip is worked out from the hand's own bones (wrist, knuckles, index and little finger), so it fits
         any Humanoid character. On the body it only casts a shadow in first person, like the body; on the first-person
         hands it's drawn with the hands, on top of the world.
         A bow (Tool Kind Bow) sits in the LEFT hand instead: upright through the fist at its grip, shooting the way the
         fingers point. While it's drawn (BowShooter) an arrow of the kind it will shoot lies on the string: its back end
         in the right hand, pointing at the bow, so it moves with the draw animation.
         Every other item (wood, food...) is held in the same fist by HeldItemView; both get the fist from HandGrip.
Requirement: ToolInHand.cs x-> Animator (Unity), PlayerInventory.cs, ItemData.cs, ToolStats.cs, HandGrip.cs
             ToolInHand.cs o-> CameraController.cs, FirstPersonHands.cs, IconCapture.cs, BowShooter.cs, AmmoStats.cs
             ToolInHand.cs <-x BowBuilder.cs, PlayerCharacterBuilder.cs, TierToolBuilder.cs
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

    [Header("Held bow (left hand)")]
    [Tooltip("How big a held bow is: 1 = the pack's own size (about 1.5 m tall), 0.85 = 85 %.")]
    [Range(0.2f, 2f)]
    [SerializeField] float bowSize = 0.85f;

    [Tooltip("Moves the bow in the left hand, in meters: y = along the bow (more = the hand holds it lower down), " +
             "x = the way it shoots, z = out of the back of the hand.")]
    [SerializeField] Vector3 bowNudge = Vector3.zero;

    [Tooltip("Turns the bow in the left hand (degrees around its own x / y / z), after the grip worked out from the hand.")]
    [SerializeField] Vector3 bowTurn = Vector3.zero;

    [Tooltip("How big the arrow on the string is while the bow is drawn: 1 = the arrow's own size (about 80 cm, like " +
             "the ones it shoots).")]
    [Range(0.2f, 2f)]
    [SerializeField] float arrowSize = 1f;

    // Where the fist holds each kind, in meters up the handle from its bottom (the pack's tools are about 1 m long).
    const float SwordGrip = 0.13f;   // the middle of the 30 cm hilt
    const float ToolGrip = 0.1f;     // near the end of an axe or pickaxe handle

    Animator animator;
    Transform hand;                  // the right hand bone (the wrist): tools, and the bow string
    Transform leftHand;              // the left hand bone: a bow (missing = the bow goes in the right hand too)
    bool handsCopy;                  // the first-person hands (no shadow) or the body (shadow only in first person)
    CameraController cameraControl;  // first or third person (can be missing)
    HandGrip rightGrip;              // where the right fist holds things, in that hand's own space
    HandGrip leftGrip;               // where the left fist holds a bow, in that hand's own space

    GameObject held;                 // the tool copy in the hand (null = empty hand)
    ItemData heldData;               // what it is a copy of
    readonly List<Renderer> heldParts = new List<Renderer>();
    bool shownFirstPerson;           // the view its shadows were last set for

    GameObject nocked;               // the arrow on the drawn bow's string (null = none made yet)
    ItemData nockedKind;             // which arrow it looks like (its head)
    readonly List<Renderer> nockedParts = new List<Renderer>();

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
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
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

    // The body's tool casts only a shadow in first person (where the body is just a shadow too). The arrow on a drawn
    // bow follows the hands (after the animation has moved them this frame).
    void LateUpdate()
    {
        ShowArrowOnString();
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
        held.transform.SetParent(HandFor(data), false);

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

    static bool IsBow(ItemData data) => data != null && data.IsTool && data.Tool.kind == ToolKind.Bow;

    // A bow goes in the left fist, every other tool in the right one.
    Transform HandFor(ItemData data) => IsBow(data) && leftHand != null ? leftHand : hand;

    // Puts the tool in the fist: turned by the grip, its size in meters whatever the character's scale, and the point
    // of the handle the fist holds (SwordGrip / ToolGrip up from its bottom) right in the fist. A bow: its grip (the
    // middle of the bow, Build Bow And Arrows puts it at the Model's middle) right in the left fist.
    void Place()
    {
        Transform holder = held.transform.parent != null ? held.transform.parent : hand;
        float toHand = 1f / Mathf.Max(0.0001f, holder.lossyScale.x);   // world meters -> the hand's own units

        if (IsBow(heldData))
        {
            HandGrip bowGrip = holder == leftHand ? leftGrip : rightGrip;
            held.transform.localRotation = bowGrip.rotation * Quaternion.Euler(bowTurn);
            held.transform.localScale = Vector3.one * (bowSize * toHand);
            held.transform.localPosition = bowGrip.center + bowGrip.rotation * (bowNudge * toHand);
            return;
        }

        float scale = heldSize * toHand;
        float grip = (heldData != null && heldData.Tool.kind == ToolKind.Sword ? SwordGrip : ToolGrip) * scale;

        bool axe = heldData != null && heldData.Tool.kind == ToolKind.Axe;
        Quaternion spin = axe ? Quaternion.Euler(0f, axeTurn, 0f) : Quaternion.identity;   // around the handle only: the grip stays
        held.transform.localRotation = rightGrip.rotation * Quaternion.Euler(gripTurn) * spin;
        held.transform.localScale = Vector3.one * scale;
        held.transform.localPosition = rightGrip.center - rightGrip.thumbSide * grip + rightGrip.rotation * (gripNudge * toHand);
    }

    // The fists' directions in each hand's own space, worked out by HandGrip from where the knuckles and fingers are
    // (HeldItemView holds other items the same way). Those spots don't move in the hand's space whatever the pose, so
    // this is measured once.
    void MeasureGrip()
    {
        rightGrip = HandGrip.Measure(animator, hand);   // tool z -> palm, y (handle) -> thumb side, x (blade) -> fingers
        leftGrip = leftHand != null ? HandGrip.Measure(animator, leftHand, true) : rightGrip;   // bow y -> thumb side, x (its shot) -> fingers
    }

    // While the held bow is drawn, an arrow of the kind it will shoot lies on the string: its back end in the right fist
    // (on the string), pointing at the bow in the left fist. Hidden the rest of the time.
    void ShowArrowOnString()
    {
        ItemData arrow = BowShooter.NextArrow;
        bool want = held != null && IsBow(heldData) && BowShooter.Drawing && arrow != null && BowShooter.ArrowLook != null;
        if (!want)
        {
            if (nocked != null && nocked.activeSelf) nocked.SetActive(false);
            return;
        }

        if (nocked == null) MakeArrowOnString();
        if (nocked == null) return;
        if (!nocked.activeSelf) nocked.SetActive(true);
        if (nockedKind != arrow) DressArrow(arrow);

        Vector3 back = hand.TransformPoint(rightGrip.center);   // the string, in the right fist
        Vector3 front = leftHand != null ? leftHand.TransformPoint(leftGrip.center) : back + transform.forward;
        Vector3 along = front - back;
        if (along.sqrMagnitude < 0.0001f) along = transform.forward;
        along.Normalize();

        // The look's middle is the arrow's tip (it points along its own z), so the tip goes an arrow's length ahead.
        float length = BowShooter.ArrowLength * arrowSize;
        nocked.transform.SetPositionAndRotation(back + along * length, Quaternion.LookRotation(along, transform.up));
        nocked.transform.localScale = Vector3.one * (arrowSize / Mathf.Max(0.0001f, hand.lossyScale.x));
    }

    // A copy of the flying arrow's look (no script: it only shows), in the right hand so it moves with it, drawn like
    // the bow (the hands' layer and shadows).
    void MakeArrowOnString()
    {
        nocked = Instantiate(BowShooter.ArrowLook, hand, false);
        nocked.name = "Arrow On String";
        int layer = hand.gameObject.layer;
        foreach (Transform part in nocked.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
        nockedParts.Clear();
        nockedParts.AddRange(nocked.GetComponentsInChildren<Renderer>(true));
        nockedKind = null;
        SetShadows(cameraControl != null && cameraControl.IsFirstPerson);
    }

    // The arrowhead of this kind (Ammo > Head): the arrow model's second part is its head.
    void DressArrow(ItemData arrow)
    {
        nockedKind = arrow;
        Material head = arrow.IsAmmo ? arrow.Ammo.head : null;
        if (head == null) return;
        foreach (Renderer part in nockedParts)
        {
            if (part == null) continue;
            Material[] materials = part.sharedMaterials;
            if (materials.Length < 2) continue;
            materials[1] = head;
            part.sharedMaterials = materials;
        }
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
        foreach (Renderer part in nockedParts)
        {
            if (part != null) part.shadowCastingMode = mode;
        }
    }
}
