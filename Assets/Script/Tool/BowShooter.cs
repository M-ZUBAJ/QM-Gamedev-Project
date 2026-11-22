/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory). Tools > Items > Build Bow And Arrows puts it there and fills Arrow Prefab.
Unity Editor: Testing: tick GameRules > Tool Test (both bows and 32 of each arrow), Play, select a bow in the hotbar,
              hold the left mouse button (the bow draws, the ring around the crosshair fills, gold when full) and let
              go: an arrow flies where the crosshair points. The numbers below can be changed while playing. Each bow's
              own numbers (Damage, Draw Seconds, Arrow Speed) are in its Item Data's Bow section (BowStats).
Purpose: Shooting a bow. With a bow (Item Data > Tool > Kind Bow) in the selected hotbar slot:
           - hold the left mouse button (KeyBindings' Swing) to draw: the character draws (PlayerAnimator's BowDraw, then
             it holds at full draw) and the longer you hold, up to the bow's Draw Seconds, the harder and farther the
             arrow goes. A better bow draws faster, so it shoots faster (Wooden Bow 1 s, Spruce Bow 0.6 s), and the
             draw animation keeps up (DrawSpeed);
           - let go to shoot: one arrow leaves the inventory and flies (ArrowProjectile, at the bow's Arrow Speed)
             toward what the crosshair points at, falling a little on the way. It hurts by the bow's Damage + the
             arrow's Damage (Wooden Bow 5, Spruce Bow 15; Stone Arrow 20 ... Diamond Arrow 50) x how far the bow was
             drawn (half = half). Only a fully drawn shot can be a critical hit (the Critical Strike perk: double
             damage, a big orange number);
           - let go before Min Draw Seconds, change the hotbar slot (to another bow too), open a menu or die: nothing is
             shot, no arrow used.
         It shoots the strongest arrow you carry (the most Damage). With none it says so and doesn't draw.
         Holding the bow drawn tires the arms: stamina drains while the button is held (Stamina Per Second: 4, sprinting
         uses 20), and the shot itself costs nothing. If the bar runs out mid-draw the string is let go without
         shooting (no arrow used), and like sprinting, a tired player can't draw again until stamina is back to
         PlayerStamina's Recover To (20).
         Marko's PlayerCombat doesn't punch while a bow is in the hand (its [Tony] line), so a click only shoots.
         Other scripts read: BowShooter.HoldingBow, Drawing, Power, DrawSpeed, InAimPose, NextArrow, ArrowCount,
         ArrowLook, ArrowLength (BowHUD, ToolInHand, FirstPersonHands, PlayerAnimator).
Requirement: BowShooter.cs x-> PlayerInventory.cs, ItemData.cs, AmmoStats.cs, BowStats.cs, ToolRules.cs, ArrowProjectile.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs
             BowShooter.cs o-> PlayerAnimator.cs, PlayerHealth.cs, CameraController.cs, PlayerController.cs, NotificationFeed.cs, PerkEffects.cs, PlayerStamina.cs
             BowShooter.cs <-x BowBuilder.cs, BowHUD.cs, PlayerAnimationSetup.cs
             BowShooter.cs <-o FirstPersonHands.cs, PlayerAnimator.cs, ToolInHand.cs
*/

using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: is the free mouse over a menu?

[DisallowMultipleComponent]
public class BowShooter : MonoBehaviour
{
    // ===== Fields =====
    // The draw animation's own length: Set Up Player Animations makes BowDraw last this long, and a bow with other Draw
    // Seconds plays it faster or slower (DrawSpeed: the Spruce Bow's 0.6 s = 1.67 times as fast).
    public const float DefaultDrawSeconds = 1f;

    [Header("Drawing")]
    [Tooltip("Let go sooner than this (seconds) and nothing is shot and no arrow is used: a quick click wastes nothing. " +
             "(How long a full draw takes is each bow's own Draw Seconds: its Item Data > Bow.)")]
    [Range(0f, 1f)]
    [SerializeField] float minDrawSeconds = 0.25f;

    [Header("The arrow")]
    [Tooltip("The flying arrow (Tools > Items > Build Bow And Arrows makes Assets/Prefabs/Item/Arrows/Arrow Projectile).")]
    [SerializeField] ArrowProjectile arrowPrefab;

    [Tooltip("How fast the weakest shot flies (the least draw that still shoots), as a share of the bow's Arrow Speed " +
             "(its Item Data > Bow: the speed of a fully drawn shot).")]
    [Range(0.1f, 1f)]
    [SerializeField] float slowestShare = 0.35f;

    [Tooltip("How far away the crosshair can pick what the arrow flies to, in meters (nothing that close: straight ahead).")]
    [Range(20f, 500f)]
    [SerializeField] float aimDistance = 250f;

    [Header("Stamina")]
    [Tooltip("Stamina used per second while the bow is held drawn (the button held), from the start of the draw. Sprinting " +
             "uses 20, out of 100: 4 = a full bar lasts 25 seconds of holding. The shot itself is free. 0 = holding is free " +
             "too. If the bar runs out mid-draw the string is let go without shooting (no arrow used).")]
    [Range(0f, 20f)]
    [SerializeField] float staminaPerSecond = 4f;

    [Header("Messages")]
    [Tooltip("Said when you try to draw with no arrows.")]
    [SerializeField] string noArrowsText = "No arrows! Craft some: 2 Wood + 2 Stone = 4 Stone Arrows";

    [Tooltip("Said when stamina runs out mid-draw (the string is let go), or a draw is tried while still tired. " +
             "Empty = say nothing.")]
    [SerializeField] string tiredText = "Too tired to hold the bow! Rest a moment.";

    const float MessageGap = 2f;           // seconds between two messages ("No arrows!", "Too tired...")
    static readonly BowStats PlainBow = new BowStats();   // the numbers of a bow without its own (the Wooden Bow's)
    const float AimPoseAfterShot = 0.6f;   // seconds the arms stay up after a shot (the release animation)
    static readonly RaycastHit[] hits = new RaycastHit[16];

    PlayerInventory inventory;
    PlayerHealth health;               // no shooting while dead (can be missing)
    PlayerStamina stamina;             // holding the draw uses it up (can be missing = free)
    CameraController cameraControl;    // first or third person (can be missing)
    bool holdingBow;                   // a bow is in the selected hotbar slot right now
    ItemData bow;                      // that bow (its Item Data, with its Bow numbers), null = none
    ItemData drawnBow;                 // the bow being drawn (swapping to another one puts it away)
    bool drawing;
    float drawStart;                   // Time.time the draw began
    ItemData loaded;                   // the arrow picked when the draw began
    ItemData bestArrow;                // the strongest arrow carried (found every frame while a bow is held)
    float releasedAt = -100f;          // Time.time of the last shot
    float nextMessage;
    bool warnedNoPrefab;

    // The one on the Player (null = none).
    public static BowShooter Current { get; private set; }

    // A bow is in the hand (and the player is alive).
    public static bool HoldingBow => Current != null && Current.holdingBow;

    // The bow is being drawn (the button is held).
    public static bool Drawing => Current != null && Current.drawing;

    // How far it's drawn: 0 = not drawing, 1 = fully drawn (full damage and speed).
    public static float Power => Current != null && Current.drawing ? Current.PowerNow() : 0f;

    // How fast the draw animation plays for the bow in the hand: 1 = it lasts Default Draw Seconds (the Wooden Bow), 1.67 =
    // 0.6 s (the Spruce Bow). PlayerAnimator and FirstPersonHands give it to their controllers (BowDrawSpeed). 1 = no bow.
    public static float DrawSpeed => Current != null && Current.bow != null ? DefaultDrawSeconds / DrawSecondsOf(Current.bow) : 1f;

    // The arms are in the bow's pose: drawing, or just shot (FirstPersonHands shows the archer's own view then).
    public static bool InAimPose => Current != null && (Current.drawing || Time.time < Current.releasedAt + AimPoseAfterShot);

    // The arrow the next shot uses (the one on the string while drawing), or null with none.
    public static ItemData NextArrow => Current == null ? null : Current.drawing && Current.loaded != null ? Current.loaded : Current.bestArrow;

    // How many of that arrow the player has.
    public static int ArrowCount
    {
        get
        {
            ItemData arrow = NextArrow;
            return arrow != null && Current.inventory != null ? Current.inventory.Count(arrow.Id) : 0;
        }
    }

    // The flying arrow's look (ToolInHand copies it onto the string) and its length in meters.
    public static GameObject ArrowLook => Current != null && Current.arrowPrefab != null ? Current.arrowPrefab.Look : null;
    public static float ArrowLength => Current != null && Current.arrowPrefab != null ? Current.arrowPrefab.Length : 0.8f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();
        stamina = GetComponent<PlayerStamina>();
    }

    void Start()
    {
        cameraControl = FindFirstObjectByType<CameraController>();
        if (inventory == null) inventory = PlayerInventory.Instance;
    }

    void OnEnable()
    {
        Current = this;
    }

    void OnDisable()
    {
        drawing = false;   // (no animation call: the scene may be closing, the character already gone)
        loaded = null;
        drawnBow = null;
        if (Current == this) Current = null;   // statics outlive the scene
    }

    void Update()
    {
        if (inventory == null) inventory = PlayerInventory.Instance;
        holdingBow = ToolRules.BowInHand && !(health != null && health.IsDead);
        bow = holdingBow ? BowInSlot() : null;
        if (!holdingBow)
        {
            if (drawing) Cancel();   // the slot changed, or the player died
            return;
        }
        if (drawing && bow != drawnBow) Cancel();   // swapped to another bow mid-draw: this one is put away
        bestArrow = StrongestArrow();

        bool blocked = MenuState.AnyOpen || ClickClaim.Claimed;   // a menu, or placing a building, owns the mouse
        if (drawing)
        {
            if (blocked) Cancel();
            else if (!Input.GetKey(KeyBindings.Swing)) Release();   // let go
            else TireArms();                                        // still held: stamina drains
            return;
        }
        if (blocked || PointerOverMenu()) return;
        if (Input.GetKeyDown(KeyBindings.Swing)) StartDrawing();
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Current = null;
    }

    // ===== My methods =====

    float PowerNow() => Mathf.Clamp01((Time.time - drawStart) / DrawSecondsOf(drawnBow));

    // A bow's numbers (its Item Data > Bow), or the plain ones (the Wooden Bow's) when it has none.
    static BowStats StatsOf(ItemData bowItem) => bowItem != null && bowItem.IsBow ? bowItem.Bow : PlainBow;

    // How long a bow takes to draw all the way, in seconds (its Draw Seconds).
    static float DrawSecondsOf(ItemData bowItem) => Mathf.Max(0.1f, StatsOf(bowItem).drawSeconds);

    // The bow in the selected hotbar slot (its Item Data), or null.
    ItemData BowInSlot()
    {
        PlayerInventory.Slot slot = inventory != null ? inventory.ActiveItem : null;
        return slot != null && !slot.IsEmpty && slot.data != null && slot.data.IsBow ? slot.data : null;
    }

    void StartDrawing()
    {
        if (bestArrow == null)
        {
            Tell(noArrowsText);
            return;
        }
        if (TooTired())   // ran out and hasn't got its breath back yet (like sprinting)
        {
            Tell(tiredText);
            return;
        }
        loaded = bestArrow;
        drawnBow = bow;
        drawing = true;
        drawStart = Time.time;
        PlayerAnimator.Play(PlayerAction.BowDraw);
    }

    // Let go: shoots, unless the bow was hardly drawn (then nothing happens and no arrow is used).
    void Release()
    {
        drawing = false;
        float held = Time.time - drawStart;
        ItemData arrow = loaded != null && inventory != null && inventory.Count(loaded.Id) > 0 ? loaded : StrongestArrow();
        ItemData shotFrom = drawnBow;
        loaded = null;
        drawnBow = null;

        if (arrowPrefab == null && !warnedNoPrefab)
        {
            warnedNoPrefab = true;
            Debug.LogWarning(name + ": BowShooter has no Arrow Prefab, so the bow can't shoot. Run Tools > Items > Build Bow " +
                             "And Arrows, or drag Assets/Prefabs/Item/Arrows/Arrow Projectile into it.", this);
        }
        if (held < minDrawSeconds || arrow == null || arrowPrefab == null || inventory == null || !inventory.Remove(arrow.Id, 1))
        {
            PlayerAnimator.Play(PlayerAction.BowCancel);
            return;
        }

        Shoot(arrow, shotFrom, Mathf.Clamp01(held / DrawSecondsOf(shotFrom)));
        releasedAt = Time.time;
        PlayerAnimator.Play(PlayerAction.BowRelease);
    }

    // While the bow is held drawn the arms tire: Stamina Per Second, every frame (no resting = it doesn't fill back up
    // meanwhile). When the bar is empty the string is let go without shooting: the arrow stays in the inventory.
    void TireArms()
    {
        if (staminaPerSecond <= 0f || stamina == null) return;
        stamina.Use(staminaPerSecond * Time.deltaTime);
        if (stamina.Stamina > 0f) return;
        Cancel();
        Tell(tiredText);
    }

    // Too tired to draw: the stamina bar ran out and hasn't refilled to PlayerStamina's Recover To yet (the same rule as
    // sprinting, so a draw doesn't stutter on and off at an empty bar). Never with Stamina Per Second 0.
    bool TooTired() => staminaPerSecond > 0f && stamina != null && (stamina.IsTired || stamina.Stamina <= 0f);

    // Stops drawing without shooting (the arms lower).
    void Cancel()
    {
        drawing = false;
        loaded = null;
        drawnBow = null;
        PlayerAnimator.Play(PlayerAction.BowCancel);
    }

    // One arrow on its way: from the bow toward what the crosshair points at, power = how far the bow was drawn (0 - 1).
    // It hurts by the bow's Damage + the arrow's, x power, and flies at the bow's Arrow Speed (slower when half drawn).
    void Shoot(ItemData arrow, ItemData bowItem, float power)
    {
        Camera cam = Camera.main;
        Vector3 target = AimPoint(cam);
        Vector3 from = StartPoint(cam);
        Vector3 toward = target - from;
        if (toward.sqrMagnitude < 2f) toward = cam != null ? cam.transform.forward : transform.forward;   // aimed right in front: straight on
        toward.Normalize();

        BowStats stats = StatsOf(bowItem);
        float damage = (stats.damage + arrow.Ammo.damage) * power;   // the bow's + the arrow's (since 22/11/2026: no tier bonus)
        bool critical = power >= 1f && PerkEffects.RollCritical();   // only a fully drawn shot
        if (critical) damage *= PerkEffects.CriticalMultiplier;
        float speed = stats.arrowSpeed * Mathf.Lerp(slowestShare, 1f, power);

        ArrowProjectile shot = Instantiate(arrowPrefab, from, Quaternion.LookRotation(toward));
        shot.name = arrow.DisplayName;
        shot.Launch(toward * speed, damage, critical, arrow);   // its head's look, and what it gives back (Arrow Recovery)
    }

    // What the crosshair points at: the first thing along the camera's middle that isn't the player, else a point
    // Aim Distance away.
    Vector3 AimPoint(Camera cam)
    {
        if (cam == null) return transform.position + transform.forward * aimDistance;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, hits, aimDistance, ArrowProjectile.HitLayers, QueryTriggerInteraction.Ignore);
        float nearest = aimDistance;
        Vector3 point = ray.GetPoint(aimDistance);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance >= nearest || IsPlayer(hits[i].collider)) continue;
            nearest = hits[i].distance;
            point = hits[i].point;
        }
        return point;
    }

    // Where the arrow starts: in first person just in front of the eyes (where the drawn bow is), in third person at the
    // character's bow hand (else the chest). Never inside a wall the player stands against: checked from the eyes in
    // first person, from the player's middle in third (the camera behind could have a tree between it and the player).
    Vector3 StartPoint(Camera cam)
    {
        bool firstPerson = cameraControl != null && cameraControl.IsFirstPerson && cam != null;
        Transform bowHand = firstPerson ? null : BowHand();
        Vector3 from = firstPerson ? cam.transform.position + cam.transform.forward * 0.5f - cam.transform.up * 0.05f
                     : bowHand != null ? bowHand.position
                     : transform.position + Vector3.up * 0.9f;

        Vector3 safe = firstPerson ? cam.transform.position : transform.position;
        Vector3 gap = from - safe;
        float length = gap.magnitude;
        if (length < 0.001f) return from;
        int count = Physics.RaycastNonAlloc(safe, gap / length, hits, length, ArrowProjectile.HitLayers, QueryTriggerInteraction.Ignore);
        float nearest = length;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance < nearest && !IsPlayer(hits[i].collider)) nearest = hits[i].distance;
        }
        return nearest < length ? safe + gap / length * Mathf.Max(0f, nearest - 0.05f) : from;
    }

    // The body's left hand (it holds the bow), or null.
    static Transform BowHand()
    {
        Animator body = PlayerAnimator.Current != null ? PlayerAnimator.Current.GetComponent<Animator>() : null;
        return body != null && body.isHuman ? body.GetBoneTransform(HumanBodyBones.LeftHand) : null;
    }

    // The arrow with the most Damage in the inventory (any slot), or null.
    ItemData StrongestArrow()
    {
        if (inventory == null) return null;
        ItemData best = null;
        foreach (PlayerInventory.Slot slot in inventory.Slots)
        {
            if (slot.IsEmpty || slot.data == null || !slot.data.IsAmmo) continue;
            if (best == null || slot.data.Ammo.damage > best.Ammo.damage) best = slot.data;
        }
        return best;
    }

    static bool IsPlayer(Collider part) => part != null && part.GetComponentInParent<PlayerController>() != null;

    // While the mouse is free (a menu), it may be over a button: a click there isn't a shot (like PlayerCombat).
    static bool PointerOverMenu()
    {
        return Cursor.lockState != CursorLockMode.Locked && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // A message in the corner (NotificationFeed), at most one every Message Gap seconds. Empty words = nothing.
    void Tell(string words)
    {
        if (string.IsNullOrEmpty(words) || Time.time < nextMessage) return;
        nextMessage = Time.time + MessageGap;
        NotificationFeed.Show(words);
    }
}
