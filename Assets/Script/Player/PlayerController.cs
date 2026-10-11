/*
First Date: 29/9/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the player shape (any shape: Capsule, Cube, Sphere...). A Rigidbody is added automatically.
Unity Editor: The shape needs a Collider (3D Objects come with one).
              Testing sprint: Play, hold W and Left Shift (the Sprint key, set in KeyBindings).
Purpose: Move the player with W A S D (or the arrow keys, both set in KeyBindings), relative to the camera:
         W always moves the way the camera is facing.
         Hold Sprint (Left Shift) to run faster (Sprint Multiplier), forward only like Minecraft (Sprint Forward Only).
         With a PlayerStamina on the player, sprinting uses stamina and stops when the player is tired.
         No speed-up or slow-down: full speed the moment a key is pressed, stops the moment it is let go.
         The shape never tips over or rolls. Gravity still works (it falls off edges).
         The player's collider has no friction (Slippery Body), so it never sticks to walls in the air.
Requirement: PlayerController.cs x-> KeyBindings.cs, MenuState.cs, Rigidbody (Unity)
             PlayerController.cs o-> PlayerStamina.cs
             PlayerController.cs <-o HeldItemView.cs, PlayerHunger.cs
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]   // adds a Rigidbody for you
public class PlayerController : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Walking speed in meters per second. Minecraft walking is about 4.3.")]
    [Range(0f, 20f)]
    [SerializeField] float speed = 5f;

    [Header("Sprint")]
    [Tooltip("Walking speed is multiplied by this while sprinting (the Sprint key, Left Shift). " +
             "Minecraft: 5.6 / 4.3 = about 1.3.")]
    [Range(1f, 3f)]
    [SerializeField] float sprintMultiplier = 1.3f;

    [Tooltip("Ticked (like Minecraft): sprinting only works while moving forward (W). " +
             "Unticked: any direction.")]
    [SerializeField] bool sprintForwardOnly = true;

    [Header("Other")]
    [Tooltip("W moves the way this camera faces. Leave empty to use the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    [Tooltip("Ticked: the player's collider gets zero friction, so walking into a wall or object while jumping " +
             "slides down instead of sticking to it. Walking doesn't need friction (the speed is set directly).")]
    [SerializeField] bool slipperyBody = true;

    Rigidbody rb;          // the physics component: keeps gravity and collisions working
    PlayerStamina stamina; // sprinting uses it up (can be missing = sprint forever)
    Vector2 moveInput;     // x = left/right (A/D), y = forward/back (W/S)
    bool sprintHeld;       // the Sprint key is held down

    // True while the player is pressing a move key (PlayerHunger uses it: walking burns more food).
    public bool IsMoving => moveInput.sqrMagnitude > 0f;

    // True while really sprinting: the key is held, the player moves (forward, if Sprint Forward Only)
    // and isn't too tired (only if there's a PlayerStamina).
    public bool IsSprinting => sprintHeld && IsMoving && (!sprintForwardOnly || moveInput.y > 0f)
                               && (stamina == null || stamina.CanSprint);

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();   // cache it once
        stamina = GetComponent<PlayerStamina>();
        rb.freezeRotation = true;         // physics never turns the shape, so it can't tip over or roll
        if (slipperyBody) MakeSlippery();

        // No camera dragged in? Use the one tagged "MainCamera".
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Update runs every frame: the best place to READ input.
    void Update()
    {
        // Keys pressed in the key binding menu shouldn't move the player.
        moveInput = MenuState.KeysCaptured ? Vector2.zero : ReadMoveInput();   // a menu reads every key (e.g. rebinding)
        sprintHeld = !MenuState.KeysCaptured && Input.GetKey(KeyBindings.Sprint);
    }

    // FixedUpdate runs at the physics rate: the best place to move physics objects.
    void FixedUpdate()
    {
        // Default: W = world forward. With a camera: W = the camera's forward.
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            forward.y = 0f;          // ignore that the camera looks down at the player
            forward.Normalize();     // make its length 1 again

            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        // Set the speed directly (no pushing), so there is no speed-up or slow-down.
        // Only the sideways speed is set: the up/down speed is kept, so gravity and falling still work.
        bool sprinting = IsSprinting;
        if (sprinting && stamina != null) stamina.SpendSprint(Time.fixedDeltaTime);   // per second, not per step

        float currentSpeed = sprinting ? speed * sprintMultiplier : speed;
        Vector3 move = (forward * moveInput.y + right * moveInput.x) * currentSpeed;
        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
    }

    // ===== My methods =====

    // Moving sets the speed toward a wall every physics step, which presses the player against it.
    // With normal friction that press is enough to hold the player up in the air ("sticking" to the wall).
    // Zero friction (and "Minimum", so the other object's friction doesn't count either) lets it slide down.
    void MakeSlippery()
    {
        PhysicMaterial noFriction = new PhysicMaterial("Player (no friction)")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,   // the lower friction of the two touching things wins: 0
        };

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.isTrigger) c.sharedMaterial = noFriction;   // triggers don't touch anything, leave them
        }
    }

    Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        // The keys are chosen in KeyBindings. Each direction has a main key and a second key.
        if (Held(KeyBindings.MoveForward, KeyBindings.MoveForwardAlt)) input.y += 1f;
        if (Held(KeyBindings.MoveBack,    KeyBindings.MoveBackAlt))    input.y -= 1f;
        if (Held(KeyBindings.MoveRight,   KeyBindings.MoveRightAlt))   input.x += 1f;
        if (Held(KeyBindings.MoveLeft,    KeyBindings.MoveLeftAlt))    input.x -= 1f;

        // Stop diagonal movement (e.g. W + D) from being faster than straight movement.
        return Vector2.ClampMagnitude(input, 1f);
    }

    // True while either of the two keys is held down.
    bool Held(KeyCode key, KeyCode altKey)
    {
        return Input.GetKey(key) || Input.GetKey(altKey);
    }
}
