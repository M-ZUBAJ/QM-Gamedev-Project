/*
First Date: 29/9/2026
Date: 21/11/2026
Author: Tony
Use by: Drag this script onto the player shape (any shape: Capsule, Cube, Sphere...). A Rigidbody is added automatically.
Unity Editor: The shape needs a Collider (3D Objects come with one).
              Testing sprint: Play, hold W and Left Shift (the Sprint key, set in KeyBindings).
              Testing slopes: walk at a steep hill or cliff: the player stops at the bottom (or slides back down)
              instead of climbing it. Run over a hill top: the player stays on the ground instead of flying off.
              Slide into a narrow V between two steep slopes: the player stands in it (no falling pose), walks along
              it and can jump.
Purpose: Move the player with W A S D (or the arrow keys, both set in KeyBindings), relative to the camera:
         W always moves the way the camera is facing.
         Walking and running only work ON THE GROUND (a surface no steeper than Max Slope Angle):
         - On the ground: full speed at once, stops at once, follows the ground up and down gentle slopes,
           and sticks to the ground over bumps and hill tops (Ground Snap Distance) instead of being launched.
         - Too steep (a cliff, a mountain side): can't walk up it, slides back down.
         - Held up between too-steep slopes (slid into a narrow V between two hills, or a pit): that counts as standing
           (IsWedged), so the player walks along the V and can jump, instead of hanging in the falling pose forever,
           unable to move. Each slope still blocks walking into it, so they can't be climbed from there either.
         - In the air (jumping, falling): keeps its speed; the keys only steer a little (Air Control), never sprint.
           Only a jump can send the player upward: running off a hill top never launches them into the air.
         Collision checking is Continuous, so even a fast player can't pass through the terrain or a wall.
         Hold Sprint (Left Shift) to run faster (Sprint Multiplier), forward only like Minecraft (Sprint Forward Only).
         With a PlayerStamina on the player, sprinting uses stamina and stops when the player is tired.
         The Sprinter perk makes sprinting faster (SetSprintBoost, from PerkEffects).
         The shape never tips over or rolls. The player's collider has no friction (Slippery Body).
Requirement: PlayerController.cs x-> KeyBindings.cs, MenuState.cs, Rigidbody (Unity)
             PlayerController.cs o-> PlayerStamina.cs, PlayerJump.cs
             PlayerController.cs <-x NewWorldBuilder.cs, PerkChest.cs, PerkManager.cs, PlayerCharacterBuilder.cs, ResourceNode.cs
             PlayerController.cs <-o EnemyAggro.cs, FirstPersonHands.cs, InteractOutline.cs, PerkEffects.cs, PlayerAnimator.cs, PlayerGlide.cs, PlayerHunger.cs, PlayerInfoPage.cs, PlayerJump.cs, PlayerPerks.cs, PlayerStats.cs, TreeFall.cs, WildAnimal.cs, WorldBarrier.cs, WorldGenerator.cs
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]   // adds a Rigidbody for you
public class PlayerController : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Walking speed in meters per second. Minecraft walking is about 4.3.")]
    [Range(0f, 20f)]
    [SerializeField] float speed = 7f;

    [Header("Sprint")]
    [Tooltip("Walking speed is multiplied by this while sprinting (the Sprint key, Left Shift). " +
             "Minecraft: 5.6 / 4.3 = about 1.3.")]
    [Range(1f, 3f)]
    [SerializeField] float sprintMultiplier = 1.5f;

    [Tooltip("Ticked (like Minecraft): sprinting only works while moving forward (W). " +
             "Unticked: any direction.")]
    [SerializeField] bool sprintForwardOnly = true;

    [Header("Ground And Slopes")]
    [Tooltip("The steepest ground the player can walk up, in degrees. Steeper = a wall: the player can't climb it " +
             "and slides back down. 45 = a 1-block step every 1 block, like Minecraft stairs.")]
    [Range(0f, 89f)]
    [SerializeField] float maxSlopeAngle = 45f;

    [Tooltip("In the air, how fast the keys can change the player's direction, in meters per second, every second. " +
             "0 = no steering in the air at all. The keys never make the player faster than walking in the air.")]
    [Range(0f, 30f)]
    [SerializeField] float airControl = 4f;

    [Tooltip("Running over a bump or a hill top, the ground can be this far below (meters) and the player still " +
             "sticks to it instead of flying off. 0 = never stick.")]
    [Range(0f, 2f)]
    [SerializeField] float groundSnapDistance = 0.6f;

    [Header("Other")]
    [Tooltip("W moves the way this camera faces. Leave empty to use the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    [Tooltip("Ticked: the player's collider gets zero friction, so it slides down walls and steep slopes instead " +
             "of sticking to them. Walking doesn't need friction (the speed is set directly).")]
    [SerializeField] bool slipperyBody = true;

    // Right after a jump, ground rules (sticking to the ground, setting the speed) wait this long,
    // so they don't cancel the jump.
    const float JumpGrace = 0.2f;

    // Held up by too-steep slopes (see Wedged): touching them without moving faster than StillSpeed (meters per
    // second) for StuckSeconds means something holds the player up, because gravity can't pull them down.
    const float StillSpeed = 0.1f;
    const float StuckSeconds = 0.25f;
    const int MaxWalls = 8;   // walls / steep slopes remembered one by one each physics step (more still count in the sum)

    // How much a slope must face up to hold anything: 0.087 = steeper than 85 degrees is an upright wall, which can't
    // hold the player up, so falling down a gap between two upright walls is still falling.
    const float UprightWall = 0.087f;

    float startSpeed;      // walking speed before any perk
    float sprintBoost = 1f;    // perks: 1.15 = sprinting 15 % faster (SetSprintBoost)
    Rigidbody rb;          // the physics component: keeps gravity and collisions working
    Collider body;         // the player's shape (for sticking to the ground)
    PlayerStamina stamina; // sprinting uses it up (can be missing = sprint forever)
    PlayerJump jump;       // tells us when the player jumps (can be missing)
    Vector2 moveInput;     // x = left/right (A/D), y = forward/back (W/S)
    bool sprintHeld;       // the Sprint key is held down

    // What the player touched during the last physics step (filled in by OnCollisionStay).
    bool grounded;         // standing on walkable ground
    Vector3 groundNormal;  // which way the walkable ground faces (added up, then made length 1)
    bool touchingSteep;    // touching a wall or a too-steep slope
    Vector3 steepNormal;   // which way that wall faces (added up)
    int steepCount;        // how many wall / too-steep contacts (the two sides of a V give 2 or more)
    readonly Vector3[] steepNormals = new Vector3[MaxWalls];   // each one's direction, to block pushing into every one
    bool wedged;           // held up between too-steep slopes (a narrow V, a pit): counts as standing (see Wedged)
    float stillSince = -1f;   // when the player stopped moving while touching only too-steep slopes (-1 = not now)
    bool wasGrounded;      // grounded in the step before (for sticking over hill tops)
    bool jumpedSinceGrounded;   // in the air because of a jump (then going up is allowed)
    float lastJumpTime = -10f;

    // True while the player is pressing a move key (PlayerHunger uses it: walking burns more food).
    public bool IsMoving => moveInput.sqrMagnitude > 0f;

    // True while standing on walkable ground (not in the air, not on a too-steep slope), or held up between too-steep
    // slopes (IsWedged).
    public bool IsGrounded => grounded || wedged;

    // True while held up between too-steep slopes (a narrow V between two hills, a pit): that counts as standing, so the
    // player can walk along it and jump out instead of "falling" forever. PlayerJump reads it.
    public bool IsWedged => wedged;

    // Walking speed right now, in meters per second (perks can raise it: IncreaseMoveSpeed). PlayerPerks shows it.
    public float Speed => speed;

    // Walking speed before any perk (the inventory's Info tab shows how much perks added), and sprint = Speed x this.
    public float StartSpeed => startSpeed;
    public float SprintMultiplier => sprintMultiplier * sprintBoost;   // perks included
    public float BaseSprintMultiplier => sprintMultiplier;               // without perks

    // PlayerJump reads it: no jumping off a too-steep slope.
    public float MaxSlopeAngle => maxSlopeAngle;

    // True while really sprinting: the key is held, the player moves (forward, if Sprint Forward Only),
    // is on the ground (no sprinting in the air) and isn't too tired (only if there's a PlayerStamina).
    public bool IsSprinting => sprintHeld && IsMoving && IsGrounded && (!sprintForwardOnly || moveInput.y > 0f)
                               && (stamina == null || stamina.CanSprint);

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        startSpeed = speed;               // before any perk raises it (ResetMoveSpeed goes back to it)
        rb = GetComponent<Rigidbody>();   // cache it once
        body = GetComponentInChildren<Collider>();
        stamina = GetComponent<PlayerStamina>();
        jump = GetComponent<PlayerJump>();
        rb.freezeRotation = true;         // physics never turns the shape, so it can't tip over or roll

        // Continuous: physics checks the whole path the player moves each step, not just where it ends up.
        // Without it, a fast player can jump past a thin surface (the terrain) in one step and walk through
        // the mountain ("tunneling"). Interpolate: smooth on screen between physics steps.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Never sleep: a body that rests for a moment "falls asleep" and stops reporting what it touches, so standing
        // still in a V between two slopes would look like falling again. One body awake costs nothing.
        rb.sleepThreshold = 0f;

        if (slipperyBody) MakeSlippery();

        // No camera dragged in? Use the one tagged "MainCamera".
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        if (jump != null) jump.Jumped += OnJumped;
    }

    void OnDisable()
    {
        if (jump != null) jump.Jumped -= OnJumped;
    }

    // Update runs every frame: the best place to READ input.
    void Update()
    {
        // Keys pressed in the key binding menu shouldn't move the player.
        moveInput = MenuState.KeysCaptured ? Vector2.zero : ReadMoveInput();   // a menu reads every key (e.g. rebinding)
        sprintHeld = !MenuState.KeysCaptured && Input.GetKey(KeyBindings.Sprint);
    }

    // FixedUpdate runs at the physics rate: the best place to move physics objects.
    // Unity runs it BEFORE the physics step, so the ground info below is from the step before.
    void FixedUpdate()
    {
        bool justJumped = Time.time - lastJumpTime < JumpGrace;

        // Held up between too-steep slopes (a narrow V, a pit): stand there, instead of hanging in the falling pose
        // forever, unable to move. Their directions added up are the "floor" (in a V: straight up).
        wedged = false;
        if (grounded || justJumped) stillSince = -1f;
        else if (Wedged())
        {
            wedged = true;
            grounded = true;
            groundNormal = SteepFloor() ? steepNormal : Vector3.up;
        }

        // Lost the ground this step without jumping (e.g. ran over a hill top): stick back down if it's close.
        if (!grounded && wasGrounded && !justJumped) SnapToGround();

        if (grounded)
        {
            groundNormal.Normalize();
            // Landed. (Not in the first moment of a jump: the feet still touch the ground for a step,
            // and forgetting the jump then would cancel it.)
            if (!justJumped) jumpedSinceGrounded = false;
        }
        else groundNormal = Vector3.up;

        Vector3 wish = WishDirection();   // the way the keys point, flat, length 0 - 1
        bool sprinting = IsSprinting;
        if (sprinting && stamina != null) stamina.SpendSprint(Time.fixedDeltaTime);   // per second, not per step

        if (grounded && !justJumped) MoveOnGround(wish, sprinting);
        else MoveInAir(wish);

        // Start fresh: the collision messages after this physics step fill these in again.
        wasGrounded = grounded;
        grounded = false;
        touchingSteep = false;
        groundNormal = Vector3.zero;
        steepNormal = Vector3.zero;
        steepCount = 0;
    }

    // Unity calls these for every solid thing the player touches during a physics step.
    void OnCollisionEnter(Collision collision)
    {
        ReadContacts(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        ReadContacts(collision);
    }

    // ===== My methods =====

    // On walkable ground: set the speed directly (full speed at once, stops at once), ALONG the ground,
    // so walking up a gentle slope goes up and walking down goes down, without flying off.
    void MoveOnGround(Vector3 wish, bool sprinting)
    {
        float currentSpeed = sprinting ? speed * SprintMultiplier : speed;
        Vector3 move = wish * currentSpeed;

        move = BlockSteep(move);   // never push into a wall / too-steep slope (that's how it used to climb them)

        // Tilt the flat move to follow the ground, keeping the same speed.
        Vector3 alongGround = Vector3.ProjectOnPlane(move, groundNormal);
        if (alongGround.sqrMagnitude > 0.0001f) alongGround = alongGround.normalized * move.magnitude;

        rb.velocity = alongGround;   // standing still = 0, so the player doesn't slide down gentle slopes
    }

    // In the air (or on a too-steep slope): keep the speed the player already has (gravity does the falling),
    // the keys only steer a little and never make the player faster than walking. No sprinting.
    void MoveInAir(Vector3 wish)
    {
        Vector3 v = rb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);

        if (wish.sqrMagnitude > 0f)
        {
            Vector3 target = wish * speed;
            // Only steer if that doesn't speed the player up past walking speed (a running jump keeps its speed).
            Vector3 steered = Vector3.MoveTowards(flat, target, airControl * Time.fixedDeltaTime);
            if (steered.magnitude <= Mathf.Max(flat.magnitude, speed)) flat = steered;
        }

        flat = BlockSteep(flat);   // can't push up a cliff from the air either

        // Only a jump may send the player up. Leaving the ground any other way (running off a hill top, up a ramp)
        // keeps no upward speed, so the player can't be launched into the air like a ball: gravity takes over at once.
        float up = jumpedSinceGrounded ? v.y : Mathf.Min(v.y, 0f);
        rb.velocity = new Vector3(flat.x, up, flat.z);
    }

    // Removes the part of a move that pushes INTO a wall or too-steep slope (moving away or along it is fine).
    // Every wall touched counts on its own: in a V between two steep slopes only moving along the V is left.
    // (Added up, the two sides of a V cancel out, and the player could push into one of them.)
    Vector3 BlockSteep(Vector3 move)
    {
        if (!touchingSteep) return move;

        int walls = Mathf.Min(steepCount, steepNormals.Length);
        for (int pass = 0; pass < 2; pass++)   // twice: taking away the push into one wall can push a little into another
        {
            for (int i = 0; i < walls; i++)
            {
                Vector3 wallOut = new Vector3(steepNormals[i].x, 0f, steepNormals[i].z);   // the flat direction away from it
                if (wallOut.sqrMagnitude < 0.0001f) continue;
                wallOut.Normalize();

                float into = Vector3.Dot(move, wallOut);
                if (into < 0f) move -= wallOut * into;   // negative = pushing into the wall: take that part away
            }
        }
        return move;
    }

    // Is the player held up by too-steep slopes, which normally don't count as ground? Either:
    // - several of them together face up like a floor (the two sides of a V: their directions added up point up), or
    // - the player has stopped moving while touching them for StuckSeconds: they slide off a single steep slope, so if
    //   they don't, something holds them up.
    bool Wedged()
    {
        if (!touchingSteep)
        {
            stillSince = -1f;
            return false;
        }
        if (SteepFloor()) return true;

        if (rb.velocity.sqrMagnitude > StillSpeed * StillSpeed)
        {
            stillSince = -1f;   // still sliding: not stuck
            return false;
        }
        if (stillSince < 0f) stillSince = Time.time;
        return Time.time - stillSince >= StuckSeconds;
    }

    // Do the too-steep slopes touched face up like a floor when added up? The two sides of a V (or the walls of a pit)
    // hold the player up between them. Upright walls hold nothing (they hardly face up at all, see UprightWall), so
    // two of them facing each other don't count.
    bool SteepFloor()
    {
        return steepCount >= 2 && steepNormal.y >= UprightWall * steepCount &&
               Vector3.Angle(steepNormal, Vector3.up) <= maxSlopeAngle;
    }

    // Lost the ground for one step (hill top, bump): if walkable ground is just below, stay stuck to it
    // by turning the speed to run along the ground instead of up into the air.
    void SnapToGround()
    {
        if (groundSnapDistance <= 0f || body == null) return;

        Bounds b = body.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(b.extents.x, b.extents.z) * 0.9f);

        // Look further down the faster the player goes: at 10 m/s the player moves 0.2 m per physics step,
        // so over a hill top the ground can be that much further away by the next step.
        float flatSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        float distance = b.extents.y - radius + groundSnapDistance + flatSpeed * Time.fixedDeltaTime;

        RaycastHit[] hits = Physics.SphereCastAll(b.center, radius, Vector3.down, distance,
                                                  Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody == rb || hit.distance <= 0f) continue;   // our own shape, or overlapping
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle) continue;       // too steep to stand on

            grounded = true;
            groundNormal = hit.normal;

            Vector3 v = rb.velocity;
            float upward = Vector3.Dot(v, hit.normal);
            if (upward > 0f) rb.velocity = (v - hit.normal * upward).normalized * v.magnitude;   // along the ground
            return;
        }
    }

    // Sorts what the player touches: walkable ground (not too steep) or a wall / steep slope.
    void ReadContacts(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            float angle = Vector3.Angle(n, Vector3.up);   // 0 = flat floor, 90 = straight wall, 180 = ceiling

            if (angle <= maxSlopeAngle)
            {
                grounded = true;
                groundNormal += n;
            }
            else if (angle < 120f)   // a wall or steep slope (not a ceiling above the head)
            {
                touchingSteep = true;
                steepNormal += n;
                if (steepCount < steepNormals.Length) steepNormals[steepCount] = n;
                steepCount++;
            }
        }
    }

    // The way the keys point: W = the camera's forward, flat on the ground. Length 0 - 1.
    Vector3 WishDirection()
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

        return forward * moveInput.y + right * moveInput.x;
    }

    void OnJumped()
    {
        lastJumpTime = Time.time;
        jumpedSinceGrounded = true;
    }

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
        if (Held(KeyBindings.MoveBack, KeyBindings.MoveBackAlt)) input.y -= 1f;
        if (Held(KeyBindings.MoveRight, KeyBindings.MoveRightAlt)) input.x += 1f;
        if (Held(KeyBindings.MoveLeft, KeyBindings.MoveLeftAlt)) input.x -= 1f;

        // Stop diagonal movement (e.g. W + D) from being faster than straight movement.
        return Vector2.ClampMagnitude(input, 1f);
    }

    // True while either of the two keys is held down.
    bool Held(KeyCode key, KeyCode altKey)
    {
        return Input.GetKey(key) || Input.GetKey(altKey);
    }



    // method added by Marko to accomodate Perk System

    public void IncreaseMoveSpeed(float percentage)
    {
        if (percentage <= 0f) return;

        float speedIncrease = speed * percentage;
        speed += speedIncrease;

        Debug.Log("Movement speed increased to " + speed);
    }

    // [Tony] added: undoes every Move Speed perk (IncreaseMoveSpeed above). PlayerPerks calls it when the player dies.
    public void ResetMoveSpeed()
    {
        speed = startSpeed;
    }

    // Sprinting goes "times" as fast as it normally would (1 = normal, 1.15 = 15 % faster). The Sprinter perk
    // (PerkEffects) sets it; walking isn't changed.
    public void SetSprintBoost(float times)
    {
        sprintBoost = Mathf.Max(0.1f, times);
    }


}
