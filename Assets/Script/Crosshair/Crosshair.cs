/*
First Date: 30/9/2026
Date: 22/11/2026
Author: Tony
Use by: Drag this script onto the Crosshair Image in the HUD Canvas.
        Set the Player sphere's Layer to "Ignore Raycast" so the crosshair looks past the ball.
Unity Editor: Colors: Normal, Target (an item F can grab), Auto (an AutoPickup item), Reach (Keen Eye perk).
              Testing Keen Eye: Play, Player > PerkEffects > ⋮ > "Test: Every New Perk Once" (or buy Keen Eye from an
              epic / legendary box), then walk up to a tree: the crosshair turns yellow once a punch reaches it.
Purpose: Shows the crosshair in the center of the screen while the mouse turns the camera.
         Changes color when it points at an item (ItemPickup on it or a parent),
         but only within the Grab Range of the player's ItemGrabber.
         Turns the Auto Color (green) for AutoPickup items (see their Item Data) within Auto Range. No tags.
         With the Keen Eye perk (PerkEffects) it turns the Reach Color (yellow) when the player's attack would hit what
         it points at: a monster, an animal, a tree or a rock. It asks the attack's own question (Marko's PlayerCombat: a
         ray from the middle of its camera, as far as its AttackReach, which the Long Reach perk makes longer), so
         yellow means a click hits.
         Other scripts can read Crosshair.Target to know what the player is aiming at,
         and Crosshair.HitPoint for the exact point on it.
Requirement: Crosshair.cs <-> ItemGrabber.cs
             Crosshair.cs x-> ItemPickup.cs, ItemData.cs, BoundsUtil.cs, ToolRules.cs, Image (Unity)
             Crosshair.cs o-> PlayerCombat.cs, PerkEffects.cs, EnemyCombat.cs, ResourceNode.cs
             Crosshair.cs <-x HUDBuilder.cs, ObjectLevitator.cs
*/

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class Crosshair : MonoBehaviour
{
    // ===== Fields =====
    [Header("Colors")]
    [Tooltip("Color when the crosshair is not pointing at a target.")]
    [SerializeField] Color normalColor = Color.white;

    [Tooltip("Color when the crosshair points at an item F can grab (ItemPickup, within Grab Range).")]
    [SerializeField] Color targetColor = Color.yellow;

    [Tooltip("Color when the crosshair points at an AutoPickup item (collected by touch, see its Item Data).")]
    [SerializeField] Color autoColor = Color.green;

    [Tooltip("Color while the player has the Keen Eye perk and the attack can reach what the crosshair points at (a " +
             "monster, an animal, a tree or a rock): a click will hit it.")]
    [SerializeField] Color reachColor = Color.yellow;

    [Header("Aiming")]
    [Range(1f, 20f)]
    [Tooltip("How far from the player an AutoPickup item turns the crosshair the Auto Color (measured to its surface).")]
    [SerializeField] float autoRange = 3f;

    [Range(1f, 100f)]
    [Tooltip("How far from the camera the crosshair can detect objects.")]
    [SerializeField] float maxDist = 30f;

    // What the crosshair is pointing at right now (null = nothing). Any script can read it.
    public static GameObject Target { get; private set; }

    // The exact point on Target the ray hit (only meaningful while Target isn't null).
    public static Vector3 HitPoint { get; private set; }

    [Tooltip("The player's ItemGrabber. The crosshair only changes color within its Grab Range. Leave empty to find it automatically.")]
    [SerializeField] ItemGrabber grabber;

    Image img;
    Camera cam;
    PlayerCombat combat;      // Marko's attack: where it aims and how far it reaches (Keen Eye)
    float nextCombatSearch;   // when to look for it again if the scene has none

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false;   // the crosshair must never block clicks on other UI
    }

    void Start()
    {
        cam = Camera.main;

        // Nothing dragged in? Look for the ItemGrabber in the scene (on the player).
        if (grabber == null) grabber = FindFirstObjectByType<ItemGrabber>();
        combat = FindFirstObjectByType<PlayerCombat>();
    }

    void Update()
    {
        // Show the crosshair only while the mouse turns the camera (cursor hidden).
        bool aiming = Cursor.lockState == CursorLockMode.Locked;
        img.enabled = aiming;   // hides the picture, but this script keeps running

        Target = aiming ? FindTarget() : null;
        img.color = PickColor(Target, aiming && InReach());
    }

    void OnDestroy()
    {
        Target = null;   // static values outlive the scene, so clear it
    }

    // ===== My methods =====

    // Shoots an invisible ray from the center of the screen and returns the first object it hits.
    GameObject FindTarget()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // (0.5, 0.5) = screen center

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDist,
                             Physics.DefaultRaycastLayers,          // skips the "Ignore Raycast" layer
                             QueryTriggerInteraction.Collide))      // also hit triggers, like pick-ups
        {
            return null;
        }

        HitPoint = hit.point;   // e.g. ObjectLevitator holds the object by this point
        return hit.collider.gameObject;
    }

    // Chooses the crosshair color for what it is pointing at. reach = the attack would hit it (Keen Eye perk).
    Color PickColor(GameObject go, bool reach)
    {
        if (IsAutoPickUp(go)) return autoColor;      // checked first, so auto items are always green
        if (IsHighlighted(go)) return targetColor;
        if (reach) return reachColor;
        return normalColor;
    }

    // Keen Eye perk: true when a click would hit something the attack hurts. It asks the attack's own question (Marko's
    // PlayerCombat: a ray from the middle of its camera, as far as AttackReach, the first thing it meets), so yellow
    // means a hit: a monster (EnemyCombat), or an animal, a tree or a rock (ResourceNode). Without the perk it does
    // nothing (no extra ray).
    bool InReach()
    {
        if (ToolRules.BowInHand) return false;   // a bow doesn't hit up close: there's no reach to show
        if (!PerkEffects.KeenEye) return false;
        if (combat == null && Time.unscaledTime >= nextCombatSearch)
        {
            combat = FindFirstObjectByType<PlayerCombat>();
            nextCombatSearch = Time.unscaledTime + 1f;   // not every frame: searching the whole scene is slow
        }
        if (combat == null) return false;

        Camera from = combat.playerCamera != null ? combat.playerCamera : cam;
        if (from == null) return false;
        Ray ray = from.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // the attack's ray: the screen's middle
        if (!Physics.Raycast(ray, out RaycastHit hit, combat.AttackReach)) return false;   // the same test as the attack
        return hit.collider.GetComponentInParent<EnemyCombat>() != null ||
               hit.collider.GetComponentInParent<ResourceNode>() != null;
    }

    // True if the object (or a parent) is an AutoPickup item (see its Item Data) within Auto Range of the player.
    bool IsAutoPickUp(GameObject go)
    {
        if (go == null) return false;

        ItemPickup item = go.GetComponentInParent<ItemPickup>();   // the ray may hit a child part
        if (item == null || item.Pickup != PickupType.AutoPickup) return false;

        // Measure from the player (the object with ItemGrabber), or from the camera if there is none.
        Vector3 from = grabber != null ? grabber.transform.position : cam.transform.position;

        // Measure to the nearest point on the object, so big objects aren't harder to reach.
        Collider col = go.GetComponent<Collider>();
        Vector3 nearest = col != null ? BoundsUtil.NearestPoint(col, from) : go.transform.position;   // works for any collider
        return Vector3.Distance(from, nearest) <= autoRange;
    }

    // True if the crosshair should change color for this object.
    bool IsHighlighted(GameObject go)
    {
        if (go == null) return false;
        if (grabber != null && !grabber.CanGrab(go)) return false;   // F wouldn't work (too far / no machine), so don't highlight

        // The ray may hit a child part (e.g. the sword's blade), so also look up through the parents.
        // This is the same check ItemGrabber uses, so if the crosshair turns color, F can grab it.
        return go.GetComponentInParent<ItemPickup>() != null;
    }
}
