/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: The top object of anything the player can use (interact with), e.g. the Chest prefab. Drag it on.
        Tools > World > Set Up Chests puts a yellow one on every chest.
Unity Editor: Choose the Color and when it shows (Show When). Testing: Play, walk up to a chest and look at it from
              closer than Range (3 m, where E opens it): a yellow outline glows around it and fades when you look away.
              Show When = Always shows every outline at once, handy to check the look.
Purpose: A signal that you can use something: a glowing outline around it (plus an optional light tint over it),
         drawn on top of the picture and the same thickness on any screen. By default it shows while you look at the
         thing from within Range (LooksAt: the nearest thing along the camera's ray, skipping the player's own ball;
         Marko's PerkChest opens with E by the same test), or while you're near it (Near), or always. It hides while a
         menu is open, while the player is dead, and once a chest is opened (PerkChest.IsOpened). For anything else
         that gets used up, switch this component off.
         All outlines on screen are drawn together, by one command buffer on the main camera (no extra objects).
Requirement: InteractOutline.cs x-> MenuState.cs
             InteractOutline.cs o-> PerkChest.cs, PlayerHealth.cs, PlayerController.cs, Camera (Unity)
             InteractOutline.cs <-x ChestPrompt.cs, ChestSetup.cs, LootChestBuilder.cs, PerkChest.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // CommandBuffer, CameraEvent

public enum OutlineShowWhen { LookAt, Near, Always }

[DisallowMultipleComponent]
public class InteractOutline : MonoBehaviour
{
    // ===== Fields =====
    [Header("Look")]
    [Tooltip("The outline's color. Yellow = something you can open or use (a chest).")]
    [SerializeField] Color color = new Color(1f, 0.82f, 0.1f);

    [Tooltip("How thick the outline is, in pixels on a screen 1080 pixels high (it keeps that size on any screen).")]
    [Range(1f, 12f)]
    [SerializeField] float width = 4f;

    [Tooltip("A light tint of the color over the thing itself: 0 = only the outline, 0.15 = a soft glow over it too.")]
    [Range(0f, 0.6f)]
    [SerializeField] float fill = 0f;

    [Tooltip("How much it pulses (gets fainter and stronger) to catch the eye. 0 = steady.")]
    [Range(0f, 1f)]
    [SerializeField] float pulse = 0.3f;

    [Header("When it shows")]
    [Tooltip("Look At = while you look at it from within Range (when E would work). Near = while the player is within Range. Always = all the time.")]
    [SerializeField] OutlineShowWhen showWhen = OutlineShowWhen.LookAt;

    [Tooltip("Meters. Look At: from the camera; match the thing's own reach (a chest's Interaction Range is 3). Near: from the player.")]
    [Min(0.5f)]
    [SerializeField] float range = 3f;

    [Tooltip("Seconds to fade in and out.")]
    [Range(0f, 1f)]
    [SerializeField] float fadeSeconds = 0.12f;

    const float PulseSpeed = 4f;   // radians per second: one gentle pulse about every 1.6 s

    readonly List<Renderer> looks = new List<Renderer>();   // the meshes the outline goes around
    PerkChest chest;               // Marko's chest (can be missing): no outline once it's opened
    PlayerHealth playerHealth;     // no outlines while the player is dead (can be missing)
    float size;                    // about half the thing's size, in meters (for the quick "too far" check)
    float strength;                // 0 = hidden, 1 = fully shown (fades in between)

    static readonly RaycastHit[] rayHits = new RaycastHit[16];   // shared by LooksAt (no garbage every frame)

    // Every outline showing right now (or fading out), drawn together on the main camera.
    static readonly List<InteractOutline> showing = new List<InteractOutline>();
    const CameraEvent DrawAfter = CameraEvent.AfterImageEffects;   // on top of the finished picture
    static CommandBuffer buffer;
    static Camera bufferCamera;        // the camera the buffer is on (null = none)
    static Material maskMaterial;      // draws a shape in flat color into the hidden mask
    static Material outlineMaterial;   // turns the mask into outlines on screen
    static bool missingShaders;        // told the Console once
    static readonly int MaskId = Shader.PropertyToID("_InteractOutlineMask");
    static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
    static readonly int PulseId = Shader.PropertyToID("_OutlinePulse");
    static readonly int FillId = Shader.PropertyToID("_FillAlpha");

    // True while this thing's outline shows (or is still fading out).
    public bool IsShowing => strength > 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        bool any = false;
        Bounds box = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer look in GetComponentsInChildren<Renderer>(true))
        {
            if (!(look is MeshRenderer) && !(look is SkinnedMeshRenderer)) continue;   // no particles, lines or text
            looks.Add(look);
            if (any) box.Encapsulate(look.bounds);
            else box = look.bounds;
            any = true;
        }
        size = box.extents.magnitude;
        chest = GetComponent<PerkChest>();
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void OnDisable()
    {
        strength = 0f;
        showing.Remove(this);
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        showing.Clear();
        buffer = null;
        bufferCamera = null;
        missingShaders = false;
        Camera.onPreCull -= BeforeCameraDraws;
        Camera.onPreCull += BeforeCameraDraws;
    }

    // LateUpdate: after the player and the camera have moved this frame.
    void LateUpdate()
    {
        float target = Wanted() ? 1f : 0f;
        strength = fadeSeconds > 0f ? Mathf.MoveTowards(strength, target, Time.unscaledDeltaTime / fadeSeconds) : target;

        bool listed = showing.Contains(this);
        if (strength > 0f && !listed) showing.Add(this);
        else if (strength <= 0f && listed) showing.Remove(this);
    }

    // ===== My methods =====

    // Should the outline show now?
    bool Wanted()
    {
        if (looks.Count == 0 || MenuState.AnyOpen) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (chest != null && chest.IsOpened) return false;

        switch (showWhen)
        {
            case OutlineShowWhen.Always:
                return true;
            case OutlineShowWhen.Near:
                return playerHealth != null && (playerHealth.transform.position - transform.position).sqrMagnitude <= range * range;
            default:
                return LookedAt();
        }
    }

    // The same test as PerkChest's E key: a ray straight out of the camera hits this thing within Range.
    bool LookedAt()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 eye = cam.transform.position;
        float reach = range + size + 1f;
        if ((transform.position - eye).sqrMagnitude > reach * reach) return false;   // far away: no ray needed
        return LooksAt(new Ray(eye, cam.transform.forward), range, transform);
    }

    // True if the nearest thing along the ray (within range) is target or part of it, not counting the player's own
    // ball: the first-person camera sits on its edge, so a plain Raycast often hit the player first. Marko's PerkChest
    // uses this for its E key too ([Tony] line), so the outline shows exactly when E works.
    public static bool LooksAt(Ray ray, float range, Transform target)
    {
        if (!FirstHit(ray, range, out RaycastHit hit)) return false;
        Transform other = hit.collider.transform;
        return other == target || other.IsChildOf(target);
    }

    // The nearest thing along the ray within range, not counting the player's own body. False = nothing there.
    // E.g. the prompt under the crosshair (ChestPrompt) finds the chest being looked at with it.
    public static bool FirstHit(Ray ray, float range, out RaycastHit nearest)
    {
        int count = Physics.RaycastNonAlloc(ray, rayHits, range);
        nearest = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            if (rayHits[i].collider.GetComponentInParent<PlayerController>() != null) continue;   // the player's own body
            if (found && rayHits[i].distance >= nearest.distance) continue;
            nearest = rayHits[i];
            found = true;
        }
        return found;
    }

    // ----- Drawing (shared by every InteractOutline) -----

    // Unity calls this before each camera draws. On the main game camera, while any outline shows: (re)fills the
    // command buffer and makes sure it's on that camera; with none showing, takes it off again (costs nothing then).
    static void BeforeCameraDraws(Camera cam)
    {
        if (cam.cameraType != CameraType.Game || !cam.CompareTag("MainCamera")) return;

        bool draw = showing.Count > 0 && MakeMaterials();
        if (bufferCamera != null && (bufferCamera != cam || !draw))
        {
            bufferCamera.RemoveCommandBuffer(DrawAfter, buffer);
            bufferCamera = null;
        }
        if (!draw) return;

        if (buffer == null) buffer = new CommandBuffer { name = "Interact Outline" };
        Record(buffer);
        if (bufferCamera == null)
        {
            cam.AddCommandBuffer(DrawAfter, buffer);
            bufferCamera = cam;
        }
    }

    // The drawing steps: every showing thing's shape, in its color, into a hidden mask the size of the screen; then
    // the mask turned into outlines, see-through, on top of the camera's picture.
    static void Record(CommandBuffer cmd)
    {
        cmd.Clear();
        float widest = 1f;
        float tint = 0f;
        float wobble = 0f;
        foreach (InteractOutline outline in showing)
        {
            widest = Mathf.Max(widest, outline.width);
            tint = Mathf.Max(tint, outline.fill);
            wobble = Mathf.Max(wobble, outline.pulse);
        }

        cmd.GetTemporaryRT(MaskId, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);   // -1 = the camera's size
        cmd.SetRenderTarget(MaskId);
        cmd.ClearRenderTarget(false, true, Color.clear);
        foreach (InteractOutline outline in showing)
        {
            Color shade = outline.color;
            shade.a = outline.strength;   // fading in or out
            cmd.SetGlobalColor(ColorId, shade);
            foreach (Renderer look in outline.looks)
            {
                if (look == null || !look.enabled || !look.gameObject.activeInHierarchy) continue;
                int parts = PartsOf(look);
                for (int part = 0; part < parts; part++) cmd.DrawRenderer(look, maskMaterial, part, 0);
            }
        }

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
        outlineMaterial.SetFloat(WidthId, Mathf.Max(1f, widest * Screen.height / 1080f));
        outlineMaterial.SetFloat(PulseId, Mathf.Lerp(1f - wobble, 1f, wave));
        outlineMaterial.SetFloat(FillId, tint);
        cmd.Blit(MaskId, BuiltinRenderTextureType.CameraTarget, outlineMaterial);
        cmd.ReleaseTemporaryRT(MaskId);
    }

    // How many parts (sub-meshes, one per material) a mesh renderer draws.
    static int PartsOf(Renderer look)
    {
        Mesh mesh = null;
        if (look is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
        else if (look.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
        return mesh != null ? mesh.subMeshCount : 0;
    }

    // The two materials, made once from the shaders in Assets/Effects/InteractOutline/Resources (a Resources folder is
    // always part of the game, so nothing needs dragging in). False = missing or not supported on this computer.
    static bool MakeMaterials()
    {
        if (maskMaterial != null && outlineMaterial != null) return true;
        if (missingShaders) return false;

        Shader mask = Resources.Load<Shader>("InteractOutlineMask");
        Shader outline = Resources.Load<Shader>("InteractOutlineComposite");
        if (mask == null || outline == null || !mask.isSupported || !outline.isSupported)
        {
            missingShaders = true;
            Debug.LogWarning("InteractOutline: its shaders (Assets/Effects/InteractOutline/Resources) are missing or not " +
                             "supported here, so no outlines show.");
            return false;
        }
        maskMaterial = new Material(mask) { hideFlags = HideFlags.HideAndDontSave };
        outlineMaterial = new Material(outline) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }
}
