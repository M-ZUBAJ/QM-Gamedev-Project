/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: The Player (next to PlayerJump). Switched off until the epic box's Glide perk switches it on (PerkEffects).
        Tools > World > Build Loot Chests puts it there switched off, so its settings can be changed in the Inspector;
        without it, PerkEffects adds one when Glide is picked.
Unity Editor: Glide Fall Speed (how slowly you float down), Dive Speed, Double Tap Seconds.
              Testing: Play, tick this component on (or get Glide), jump and hold Space: you float down. In the air,
              tap Space twice quickly: you shoot down to the ground.
Purpose: The Glide perk:
           - Hold the jump key (Space) while falling: you fall no faster than Glide Fall Speed (like a parachute).
             Going up after a jump is unchanged; the float starts once you fall.
           - Tap the jump key twice quickly while in the air: you dive straight down at Dive Speed until you land.
         It runs after PlayerController (which keeps the falling speed in the air), so its slower fall isn't undone.
         The dive tells PlayerJump to forget the tap, so landing doesn't jump again by itself.
Requirement: PlayerGlide.cs x-> KeyBindings.cs, MenuState.cs, Rigidbody (Unity)
             PlayerGlide.cs o-> PlayerJump.cs, PlayerController.cs
             PlayerGlide.cs <-x LootChestBuilder.cs
             PlayerGlide.cs <-o PerkEffects.cs
*/

using UnityEngine;

[DefaultExecutionOrder(100)]   // after PlayerController's FixedUpdate, which sets the speed in the air
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class PlayerGlide : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The fastest you fall while holding Space, in meters per second. 2 = a slow float (falling freely reaches about 15).")]
    [Range(0.5f, 10f)]
    [SerializeField] float glideFallSpeed = 2f;

    [Tooltip("How fast the dive (Space twice in the air) shoots you down, in meters per second.")]
    [Range(5f, 60f)]
    [SerializeField] float diveSpeed = 22f;

    [Tooltip("Two taps of Space within this many seconds (in the air) make a dive.")]
    [Range(0.1f, 0.6f)]
    [SerializeField] float doubleTapSeconds = 0.3f;

    [Tooltip("Taps this soon after leaving the ground don't count toward a dive (the jump's own press).")]
    [Range(0f, 0.5f)]
    [SerializeField] float ignoreAfterTakeOff = 0.1f;

    Rigidbody rb;
    PlayerJump jump;               // its ground check, and forgetting a tap after a dive (can be missing)
    PlayerController controller;   // its ground check when there's no PlayerJump (can be missing)
    float lastGroundedTime;
    float lastAirTap = -10f;
    bool diving;

    public bool IsGliding { get; private set; }   // floating down right now (e.g. for an animation later)
    public bool IsDiving => diving;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jump = GetComponent<PlayerJump>();
        controller = GetComponent<PlayerController>();
    }

    void OnDisable()
    {
        IsGliding = false;
        diving = false;
    }

    // Update: read the taps every frame, so a quick double tap is never missed.
    void Update()
    {
        if (Grounded())
        {
            lastGroundedTime = Time.time;
            lastAirTap = -10f;
            return;
        }
        if (MenuState.AnyOpen || MenuState.KeysCaptured) return;   // a menu has the keyboard
        if (!Input.GetKeyDown(KeyBindings.Jump) || Time.time - lastGroundedTime < ignoreAfterTakeOff) return;

        if (Time.time - lastAirTap <= doubleTapSeconds) StartDive();
        else lastAirTap = Time.time;
    }

    // FixedUpdate: physics changes go here (Unity's physics step).
    void FixedUpdate()
    {
        if (Grounded())
        {
            diving = false;
            IsGliding = false;
            return;
        }

        Vector3 v = rb.velocity;
        if (diving)
        {
            IsGliding = false;
            if (v.y > -diveSpeed) rb.velocity = new Vector3(v.x, -diveSpeed, v.z);
            return;
        }

        bool holding = Input.GetKey(KeyBindings.Jump) && !MenuState.AnyOpen && !MenuState.KeysCaptured;
        IsGliding = holding && v.y < 0f;   // only on the way down
        if (IsGliding && v.y < -glideFallSpeed) rb.velocity = new Vector3(v.x, -glideFallSpeed, v.z);
    }

    // ===== My methods =====

    void StartDive()
    {
        diving = true;
        lastAirTap = -10f;
        if (jump != null) jump.ForgetPress();   // the tap mustn't jump again the moment we land
    }

    bool Grounded()
    {
        return (jump != null && jump.IsGrounded) || (controller != null && controller.IsGrounded);
    }
}
