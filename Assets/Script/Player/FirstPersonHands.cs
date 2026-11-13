/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: A copy of the player's character under the Main Camera, on the same object as that copy's Animator.
        Tools > Player > Add First Person Hands puts one there, already set up.
Unity Editor: Fists On Screen picks where the fists sit on the screen (0.3 high = just above the hotbar). Change it
              WHILE PLAYING in first person (F5) until it looks right, then right-click the component title > Copy
              Component, stop playing, right-click > Paste Component Values (Play mode changes are lost otherwise).
              Layer 30 must stay unused by anything else: the hands are drawn on it (see Hands Layer).
              Testing: Play in first person: the fists show. Left click: punch (also right next to a tree or a rock).
              Select a tool in the hotbar: it's in the right fist (ToolInHand), and left click swings it.
Purpose: Minecraft-style hands for the first-person view. A copy of the player's character sits on the camera with its
         head where the camera is, standing in a fighting guard. The first time the hands show, the copy is moved so
         the middle of its two fists lands on Fists On Screen, whatever the character's size, the camera's field of view
         or the window's shape. Only its arms and hands are drawn, measured every frame so Unity never skips them
         mid-punch, by a hands-only camera on top of the world: the tree, rock or enemy being punched can't hide the
         fist. When the player punches (PlayerAnimator.Play, e.g. a bare-handed attack), the hands punch too.
         They show in first person while alive. A selected tool sits in the right fist (ToolInHand, on the same copy)
         and swings with it (PlayerAnimator.Play: Attack for a sword or axe, Mine for a pickaxe); while a tool is in the
         fist the hands sit lower (Tool Lower, a share of the screen), so it doesn't cover the view.
Requirement: FirstPersonHands.cs x-> Animator (Unity)
             FirstPersonHands.cs o-> PlayerAnimator.cs, CameraController.cs, PlayerInventory.cs, PlayerHealth.cs, PlayerController.cs, Camera (Unity)
             FirstPersonHands.cs <-x FirstPersonArmsHelper.cs, FirstPersonHandsBuilder.cs, TierToolBuilder.cs
             FirstPersonHands.cs <-o ToolInHand.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode

[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class FirstPersonHands : MonoBehaviour
{
    // The meshes drawn by default: the copy's arms and hands (Blink's "Arms" and "Hands", a "Gloves" armor piece).
    public static readonly string[] DefaultParts = { "Arm", "Hand", "Glove" };

    // ===== Fields =====
    [Header("Where on screen")]
    [Tooltip("Where the middle of the two fists sits on the screen: x 0 = left edge, 0.5 = middle, 1 = right edge; " +
             "y 0 = bottom, 1 = top. 0.3 high = a little above the hotbar. Change it while playing.")]
    [SerializeField] Vector2 fistsOnScreen = new Vector2(0.5f, 0.3f);

    [Tooltip("An extra small move after that, in meters from the camera: x = right, y = up, z = forward " +
             "(e.g. z 0.1 = a bit further away, so the fists look smaller).")]
    [SerializeField] Vector3 nudge = Vector3.zero;

    [Tooltip("How much lower the fists sit while a tool is in the hand, as a share of the screen's height: 0.15 = 15 % " +
             "of the screen lower than Fists On Screen, so the tool doesn't cover the view. 0 = where empty hands are. " +
             "They glide there in a fifth of a second. Change it while playing.")]
    [Range(0f, 0.5f)]
    [SerializeField] float toolLower = 0.15f;

    [Header("What shows")]
    [Tooltip("Only meshes whose name contains one of these words are drawn (the arms and hands). The rest of the body stays hidden.")]
    [SerializeField] string[] shownParts = { "Arm", "Hand", "Glove" };

    [Tooltip("Item Ids that hide the hands while held. Empty by default: the hands always show, holding the selected " +
             "tool (ToolInHand). Add an id here for something held without hands (e.g. a future bow).")]
    [SerializeField] string[] hideWhileHolding = { };

    [Header("Feel")]
    [Range(0f, 0.1f)]
    [Tooltip("How far the hands bob up and down while walking, in meters. 0 = no bob.")]
    [SerializeField] float bobAmount = 0.015f;

    [Range(0f, 20f)]
    [Tooltip("How fast they bob while walking (steps).")]
    [SerializeField] float bobSpeed = 9f;

    [Header("Drawing on top")]
    [Range(8, 31)]
    [Tooltip("A layer only the hands use (30 is free; 31 is IconCapture's). The main camera stops drawing it and a " +
             "hands-only camera draws the hands on top of everything, so a punch never disappears into the tree, rock " +
             "or enemy being hit, or into a wall.")]
    [SerializeField] int handsLayer = 30;

    [Range(0.001f, 0.1f)]
    [Tooltip("How close to the eyes the hands camera still draws, in meters. Smaller = the fists can come closer before " +
             "they're cut off. (The main camera's own near clip plane isn't changed.)")]
    [SerializeField] float handsNearClip = 0.01f;

    Animator animator;
    CameraController cameraControl;   // first / third person (can be missing)
    Camera cam;                       // the main camera: its view is copied, and it stops drawing the hands' layer
    Camera handsCamera;               // made in Start: draws only the hands, after the main camera
    bool mainDrewHandsLayer;          // the main camera drew that layer before (given back in OnDestroy)
    PlayerHealth health;              // no hands while dead (can be missing)
    PlayerController controller;      // for the walking bob (can be missing)

    readonly HashSet<int> parameters = new HashSet<int>();   // the controller's parameters (missing ones are skipped)
    readonly List<Renderer> handParts = new List<Renderer>();
    Vector3 eyesAt;        // the position (in the camera's space) that puts the character's head on the camera
    Vector3 framing;       // the move from there that puts the fists on Fists On Screen
    bool needsFraming = true;   // measure the fists again (the first time they show, or after a change)
    float framedAspect;    // the screen's width / height when they were placed (a resized window places them again)
    bool shown;
    float bobTime;
    float bobWeight;       // 0 = standing still, 1 = walking (eases between them)
    float toolWeight;      // 0 = empty hands (at Fists On Screen), 1 = a tool in the fist (Tool Lower further down)
    float fistsDepth = 0.4f;   // how far in front of the camera the fists are, in meters (measured by FrameFists)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;                          // the hands stay where the camera puts them
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;  // keep the guard moving even while hidden

        // Only the arms and hands are ever drawn, and nothing casts a shadow (the real character casts the player's shadow).
        foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
        {
            part.shadowCastingMode = ShadowCastingMode.Off;
            if (IsHandPart(part.name, shownParts))
            {
                handParts.Add(part);

                // Unity skips drawing an animated mesh whose box is out of view. That box is stored for arms held out to
                // the sides and fixed to one bone (Blink's arms: the upper spine, hands: the hips), so when a punch twists
                // the body the box swings out of view and the punching arm vanished. Measuring the real arm every frame
                // instead keeps it drawn; for two small meshes it costs almost nothing.
                if (part is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            part.enabled = false;   // the hand parts switch on in first person (Show)
        }
    }

    void Start()
    {
        cameraControl = GetComponentInParent<CameraController>();
        if (cameraControl == null) cameraControl = FindFirstObjectByType<CameraController>();
        cam = GetComponentInParent<Camera>();
        if (cam == null) cam = Camera.main;
        MakeHandsCamera();
        health = FindFirstObjectByType<PlayerHealth>();
        controller = FindFirstObjectByType<PlayerController>();

        if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters) parameters.Add(p.nameHash);
        }

        // Head on the camera: move the copy so its head bone is where the camera is (the copy is the camera's child).
        Transform head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        transform.localRotation = Quaternion.identity;   // looks the way the camera looks
        eyesAt = transform.localPosition;
        if (head != null && transform.parent != null) eyesAt -= transform.parent.InverseTransformPoint(head.position);
        transform.localPosition = eyesAt + nudge;
    }

    // Unity calls this when a value is changed in the Inspector: place the fists again with the new numbers.
    void OnValidate()
    {
        needsFraming = true;
    }

    void OnEnable()
    {
        PlayerAnimator.ActionPlayed += OnAction;   // e.g. a bare-handed attack: punch
    }

    void OnDisable()
    {
        PlayerAnimator.ActionPlayed -= OnAction;
        if (shown) Show(false);   // switches the hands camera off too
    }

    void OnDestroy()
    {
        if (cam != null && mainDrewHandsLayer) cam.cullingMask |= 1 << handsLayer;   // give the main camera its whole view back
        if (handsCamera != null) Destroy(handsCamera.gameObject);
    }

    // LateUpdate: after the camera has switched view and moved this frame.
    void LateUpdate()
    {
        bool want = cameraControl != null && cameraControl.IsFirstPerson
                    && !(health != null && health.IsDead)
                    && !HoldingATool();
        if (want != shown) Show(want);
        if (!shown) return;

        if (handsCamera != null) handsCamera.fieldOfView = cam.fieldOfView;   // follows the main camera if it ever zooms
        if (cam != null && !Mathf.Approximately(cam.aspect, framedAspect)) needsFraming = true;   // the window was resized
        if (needsFraming) FrameFists();
        toolWeight = Mathf.MoveTowards(toolWeight, ToolOut() ? 1f : 0f, Time.deltaTime * 5f);
        float drop = ToolDrop() * Mathf.SmoothStep(0f, 1f, toolWeight);   // eases in and out, no jerk
        transform.localPosition = eyesAt + framing + nudge + Bob() + Vector3.down * drop;
    }

    // ===== My methods =====

    // True if a mesh with this name is one of the parts that show (its name contains one of the words).
    public static bool IsHandPart(string partName, string[] words)
    {
        foreach (string word in words ?? DefaultParts)
        {
            if (!string.IsNullOrEmpty(word) && partName.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    void Show(bool show)
    {
        shown = show;
        foreach (Renderer part in handParts)
        {
            if (part != null) part.enabled = show;
        }
        if (handsCamera != null) handsCamera.enabled = show;   // nothing to draw while hidden
    }

    // A second camera that draws only the hands, after the main camera and with a fresh depth (it forgets how far away
    // the world was), so nothing in the world can cover them: not the tree, rock or enemy being punched, not a wall.
    // Its near clip plane is tiny, so the fists can come very close to the eyes without being cut off.
    void MakeHandsCamera()
    {
        if (cam == null) return;

        foreach (Transform part in GetComponentsInChildren<Transform>(true)) part.gameObject.layer = handsLayer;
        mainDrewHandsLayer = (cam.cullingMask & (1 << handsLayer)) != 0;
        cam.cullingMask &= ~(1 << handsLayer);   // the main camera stops drawing the hands (no double, no hiding in the world)

        handsCamera = new GameObject("HandsCamera").AddComponent<Camera>();
        handsCamera.transform.SetParent(cam.transform, false);
        handsCamera.CopyFrom(cam);                          // the same view: field of view, screen area, HDR, position...
        handsCamera.clearFlags = CameraClearFlags.Depth;    // keep the world's picture, only start a fresh depth
        handsCamera.cullingMask = 1 << handsLayer;          // sees only the hands
        handsCamera.depth = cam.depth + 1f;                 // drawn after the main camera (the HUD still goes on top)
        handsCamera.nearClipPlane = handsNearClip;
        handsCamera.farClipPlane = 10f;                     // the hands are always close
        handsCamera.useOcclusionCulling = false;
        handsCamera.enabled = false;                        // on only while the hands show (Show)
    }

    // Moves the hands so the middle of the two fists lands on Fists On Screen. It measures where the hand bones are
    // right now (in the fighting guard), works out where that screen spot is at the same distance from the camera,
    // and adds the difference to Framing.
    void FrameFists()
    {
        needsFraming = false;
        if (cam == null || transform.parent == null || !animator.isHuman) return;
        framedAspect = cam.aspect;

        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (left == null || right == null) return;

        // Measure from the plain spot, without the walking bob or the tool drop: else placing them again (a resized
        // window, an Inspector change) would count those in and undo them (the tool drop vanished that way).
        transform.localPosition = eyesAt + framing + nudge;

        // The middle of the fists, in the camera's own directions (x = right, y = up, z = forward).
        Vector3 fists = transform.parent.InverseTransformPoint((left.position + right.position) * 0.5f);
        float depth = Mathf.Max(fists.z, 0.1f);
        fistsDepth = depth;

        // At that distance, the screen is this many meters from its middle to its top edge (and to its side edge).
        float halfHeight = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * cam.aspect;
        float wantX = (fistsOnScreen.x - 0.5f) * 2f * halfWidth;
        float wantY = (fistsOnScreen.y - 0.5f) * 2f * halfHeight;

        framing += new Vector3(wantX - fists.x, wantY - fists.y, 0f);
    }

    // Tool Lower turned into meters at the fists' distance: the same share of the screen whatever the character's size
    // or the camera's field of view.
    float ToolDrop()
    {
        if (cam == null) return toolLower * 0.5f;
        return toolLower * 2f * fistsDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // True while a tool (Item Data > Item Type = Tool) is in the selected hotbar slot: the hands sit lower then.
    bool ToolOut()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        PlayerInventory.Slot slot = inventory != null ? inventory.ActiveItem : null;
        return slot != null && !slot.IsEmpty && slot.data != null && slot.data.IsTool;
    }

    // True while the hand holds one of Hide While Holding (a tool or weapon: it swings instead of punching).
    bool HoldingATool()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null || inventory.ActiveItem == null || inventory.ActiveItem.IsEmpty) return false;

        string held = inventory.ActiveItem.id;
        foreach (string tool in hideWhileHolding)
        {
            if (tool == held) return true;
        }
        return false;
    }

    // PlayerAnimator.Play was called (e.g. Punch): play the same action, if this controller has it.
    void OnAction(PlayerAction action)
    {
        int trigger = Animator.StringToHash(action.ToString());
        if (parameters.Contains(trigger)) animator.SetTrigger(trigger);
    }

    // Up-and-down (and a little side-to-side) while walking, easing in and out. Like HeldItemView's bob.
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
