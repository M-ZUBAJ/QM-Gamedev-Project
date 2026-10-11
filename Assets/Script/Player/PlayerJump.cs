/*
First Date: 8/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerController). A Rigidbody is added automatically.
Unity Editor: The jump key is set in KeyBindings (Jump, default Space).
              Testing: Play and press Space. Change Jump Height while playing to find the feel you like.
Purpose: Jump with Space, only when standing on something (no jumping again in the air).
         Jump Height is how high the player's feet go, in meters, whatever the gravity is.
         Two small helpers make it feel fair:
         - Coyote Time: you can still jump a moment after walking off an edge.
         - Jump Buffer: pressing Space a moment before landing still jumps when you land.
         Doesn't jump while a menu is open.
         With a PlayerStamina on the player, each jump uses stamina and there's no jumping while tired.
Requirement: PlayerJump.cs x-> KeyBindings.cs, MenuState.cs, Rigidbody (Unity), Collider (Unity)
             PlayerJump.cs o-> PlayerStamina.cs
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class PlayerJump : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.1f, 5f)]
    [Tooltip("How high a jump goes, in meters. Minecraft is about 1.25 (just over one block).")]
    [SerializeField] float jumpHeight = 1.25f;

    [Range(0f, 0.5f)]
    [Tooltip("Seconds after walking off an edge in which a jump still works. 0 = only while touching the ground.")]
    [SerializeField] float coyoteTime = 0.1f;

    [Range(0f, 0.5f)]
    [Tooltip("Seconds before landing in which pressing Jump still counts (it jumps the moment you land).")]
    [SerializeField] float jumpBuffer = 0.1f;

    [Range(0.01f, 0.5f)]
    [Tooltip("How close below the feet the ground must be to count as standing on it, in meters.")]
    [SerializeField] float groundCheckDistance = 0.1f;

    Rigidbody rb;
    Collider body;           // the player's shape, to find where its feet are
    PlayerStamina stamina;   // each jump uses some (can be missing = jump forever)
    float lastGroundedTime = -10f;
    float lastPressedTime = -10f;

    public bool IsGrounded { get; private set; }   // other scripts can read it (e.g. a landing sound later)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        body = GetComponentInChildren<Collider>();
        stamina = GetComponent<PlayerStamina>();
    }

    // Update: read the key every frame, so a quick tap is never missed.
    void Update()
    {
        if (MenuState.AnyOpen || MenuState.KeysCaptured) return;   // a menu has the keyboard
        if (Input.GetKeyDown(KeyBindings.Jump)) lastPressedTime = Time.time;
    }

    // FixedUpdate: physics changes go here (Unity's physics step).
    void FixedUpdate()
    {
        IsGrounded = CheckGround();
        if (IsGrounded) lastGroundedTime = Time.time;

        bool pressedRecently = Time.time - lastPressedTime <= jumpBuffer;
        bool groundedRecently = Time.time - lastGroundedTime <= coyoteTime;
        bool enoughStamina = stamina == null || stamina.CanJump;
        if (pressedRecently && groundedRecently && enoughStamina) Jump();
    }

    // ===== My methods =====

    void Jump()
    {
        // The speed that reaches exactly jumpHeight against gravity: v = square root of (2 x gravity x height).
        float speed = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);

        Vector3 v = rb.velocity;
        v.y = speed;   // replace (not add), so jumping while still falling a little goes just as high
        rb.velocity = v;

        if (stamina != null) stamina.SpendJump();

        // Used up: no second jump from the same press, nor from the same bit of coyote time.
        lastPressedTime = -10f;
        lastGroundedTime = -10f;
        IsGrounded = false;
    }

    // True if something solid is just below the feet. A ball the width of the feet slides down a little way,
    // so standing on an edge or a slope still counts (a single thin ray would miss those).
    bool CheckGround()
    {
        if (body == null) return false;

        Bounds b = body.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(b.extents.x, b.extents.z) * 0.9f);   // a bit thinner than the body
        Vector3 start = b.center;
        float distance = b.extents.y - radius + groundCheckDistance;                      // down to just below the feet

        // Ignore triggers (pick-ups you walk through) and the player's own colliders.
        RaycastHit[] hits = Physics.SphereCastAll(start, radius, Vector3.down, distance,
                                                  Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody == rb) continue;   // our own shape
            if (hit.distance <= 0f) continue;                      // already overlapping at the start (not "below")
            return true;
        }
        return false;
    }
}
