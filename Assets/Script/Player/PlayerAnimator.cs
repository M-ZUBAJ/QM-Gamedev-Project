/*
First Date: 10/11/2026
Date: 13/11/2026
Author: Tony
Use by: The character model under the Player. Tools > Player > Add Character Model puts it there, already set up.
        By hand: put a Humanoid character on the Player as a child, give its Animator the PlayerCharacter controller
        and drag this script onto the same object as that Animator.
Unity Editor: Facing picks how the body turns (see its tooltip). The other numbers can be changed while playing.
              To change the look (clothes, armor, hair, beard), open Assets/Prefabs/Player/PlayerCharacter.prefab and
              switch its parts on or off (e.g. Armors > Plate1 > PlateSet1_Helmet).
              Testing: Play, then walk, sprint (Left Shift), jump (Space) and press F5 (first person: only the shadow
              shows). On the Player's PlayerHealth, the ⋮ menu > "Test: Take Damage" makes the body flinch and
              "Test: Die" makes it fall down until the respawn.
Purpose: Makes the player's character move like the player: it stands, runs (forwards, backwards and sideways),
         sprints, jumps, falls off edges, flinches when hurt and lies down when dead.
         It only shows the player: the ball under it still does all the moving and bumping into things.
         Actions (attack, mining...) play on the upper body only, so the legs keep running.
         Any script can play one with one line, e.g. PlayerAnimator.Play(PlayerAction.Attack);
         Play() also fires ActionPlayed, so the first-person hands (FirstPersonHands) play the same action, at the same
         speed (ActionSpeed, used by both controllers), so in first person the hands and the body's shadow move together.
Requirement: PlayerAnimator.cs x-> Animator (Unity)
             PlayerAnimator.cs o-> PlayerController.cs, PlayerJump.cs, PlayerHealth.cs, CameraController.cs, Rigidbody (Unity)
             PlayerAnimator.cs <-x FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PlayerCombat.cs, PunchCombo.cs, PunchEffect.cs, PunchEffectBuilder.cs, TierToolBuilder.cs, ToolRules.cs
             PlayerAnimator.cs <-o FirstPersonHands.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode

// What the character can do with its upper body. Each name is also a trigger in the PlayerCharacter Animator
// Controller (Tools > Player > Add Character Model makes one per name and picks its animation).
// Punch = the right jab. PunchLeft, HeavyHook (left) and HeavyPunch (right) are PunchCombo's (Mixamo clips).
// Attack (sword, axe) and Mine (pickaxe) both play Mixamo's BotAxeAttackDownward.
public enum PlayerAction { Hit, Punch, Attack, Mine, Gather, Cast, PunchLeft, HeavyHook, HeavyPunch }

[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class PlayerAnimator : MonoBehaviour
{
    // Which way the body faces.
    public enum Facing
    {
        CameraDirection,   // where you aim, like Minecraft: walking sideways or backwards uses those animations
        MoveDirection      // the way it's going, like most adventure games: it turns around to run back
    }

    // The names in the Animator Controller. The builder tool makes the controller with these same names,
    // so changing one here changes both.
    public const string MoveXName = "MoveX";         // speed to the body's right (1 = walking speed, minus = left)
    public const string MoveZName = "MoveZ";         // speed forwards (1 = walking, about 2 = sprinting, minus = backwards)
    public const string RunSpeedName = "RunSpeed";   // how fast the running animations play (1 = normal)
    public const string FallingName = "Falling";
    public const string JumpName = "Jump";
    public const string DeadName = "Dead";
    public const string ActionLayerName = "Actions"; // the upper body layer
    public const string EmptyStateName = "Empty";    // its "doing nothing" state

    // How actions play, in the body's controller AND the first-person hands' one, so in first person the hands and the
    // body's shadow punch together. ActionSpeed 2 = twice as fast as the clips (Blink's punch takes 2 s): a click should
    // feel snappy. After changing these, run Tools > Player > Set Up Player Animations (it updates both controllers).
    public const float ActionSpeed = 2f;
    public const float ActionBlendSeconds = 0.05f;   // how quickly an action takes over from what was playing

    // ===== Fields =====
    [Header("Turning")]
    [Tooltip("Camera Direction: the body looks where you aim (like Minecraft) and runs sideways or backwards. " +
             "Move Direction: the body turns to run the way it's going.")]
    [SerializeField] Facing facing = Facing.CameraDirection;

    [Range(90f, 1440f)]
    [Tooltip("How fast the body turns, in degrees per second. 720 = turning around in a quarter of a second.")]
    [SerializeField] float turnSpeed = 720f;

    [Header("Moving")]
    [Range(0f, 0.5f)]
    [Tooltip("Seconds to blend between standing, running and sprinting. 0 = switch at once (looks jumpy).")]
    [SerializeField] float blendTime = 0.1f;

    [Range(0.5f, 2f)]
    [Tooltip("How fast the running animations play. Raise it if the feet look like they slide over the ground.")]
    [SerializeField] float runAnimationSpeed = 1f;

    [Range(0f, 1f)]
    [Tooltip("Seconds in the air before the falling animation starts, so small bumps don't count.")]
    [SerializeField] float fallDelay = 0.25f;

    [Header("Hurt")]
    [Tooltip("Flinch when hurt.")]
    [SerializeField] bool flinchWhenHurt = true;

    [Min(0f)]
    [Tooltip("Smallest health loss at once that makes the body flinch. Starving takes health away slowly, " +
             "a little every frame, so it doesn't make the body flinch.")]
    [SerializeField] float flinchMinDamage = 5f;

    [Header("First person")]
    [Tooltip("In first person (F5) hide the body but keep its shadow, so the camera doesn't see the inside of the head.")]
    [SerializeField] bool shadowOnlyInFirstPerson = true;

    static readonly int MoveX = Animator.StringToHash(MoveXName);
    static readonly int MoveZ = Animator.StringToHash(MoveZName);
    static readonly int RunSpeed = Animator.StringToHash(RunSpeedName);
    static readonly int Falling = Animator.StringToHash(FallingName);
    static readonly int Jump = Animator.StringToHash(JumpName);
    static readonly int Dead = Animator.StringToHash(DeadName);
    static readonly int EmptyState = Animator.StringToHash(EmptyStateName);
    // One trigger per action, in the same order as PlayerAction.
    static readonly int[] Actions = System.Array.ConvertAll(System.Enum.GetNames(typeof(PlayerAction)), Animator.StringToHash);

    Animator animator;
    Rigidbody body;                  // the Player's: its speed drives the running
    PlayerController controller;     // the walking speed and "on the ground" (can be missing)
    PlayerJump jump;                 // tells us when the player jumps (can be missing)
    PlayerHealth health;             // hurt and dead (can be missing)
    CameraController cameraControl;  // first / third person and where the camera looks (can be missing)

    readonly HashSet<int> parameters = new HashSet<int>();   // the controller's parameters (missing ones are skipped)
    Renderer[] parts;                  // every mesh of the character
    ShadowCastingMode[] partShadows;   // each one's own shadow setting, put back after first person
    bool shadowOnly;                   // the parts only draw their shadow right now (first person)
    int actionLayer = -1;              // the Actions layer's number (-1 = the controller has none)
    float yaw;                         // which way the body faces, in degrees (0 = north / +Z)
    float airTime;                     // seconds since the feet left the ground
    float lastHealth;                  // health in the last frame, to see how much was lost

    // The character in the scene right now, for Play(). Null if there isn't one.
    public static PlayerAnimator Current { get; private set; }

    // Fired by Play() with the action, even without a character, e.g. so the first-person hands punch too.
    public static event System.Action<PlayerAction> ActionPlayed;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;   // the ball moves the player: the animations stay on the spot

        // The Player's scripts sit on a parent object (the ball). Each one is optional.
        body = GetComponentInParent<Rigidbody>();
        controller = GetComponentInParent<PlayerController>();
        jump = GetComponentInParent<PlayerJump>();
        health = GetComponentInParent<PlayerHealth>();

        parts = GetComponentsInChildren<Renderer>(true);
        partShadows = new ShadowCastingMode[parts.Length];
        for (int i = 0; i < parts.Length; i++) partShadows[i] = parts[i].shadowCastingMode;

        yaw = transform.eulerAngles.y;
    }

    // In Start, not Awake: by now the Animator has loaded its controller, so its parameters and layers can be read.
    void Start()
    {
        if (animator.runtimeAnimatorController == null)
        {
            // Nothing to play: the character would stand in a T-pose. Say why, once, instead of Unity's
            // "Animator is not playing an AnimatorController" for every value set.
            Debug.LogWarning(name + "'s Animator has no Controller, so the character stands in a T-pose. Run Tools > Player > " +
                             "Add Character Model again, or drag Assets/Animation/Player/PlayerCharacter.controller into its " +
                             "Animator > Controller.", this);
        }
        else
        {
            foreach (AnimatorControllerParameter p in animator.parameters) parameters.Add(p.nameHash);
            actionLayer = animator.GetLayerIndex(ActionLayerName);
        }

        cameraControl = FindFirstObjectByType<CameraController>();
        if (health != null) lastHealth = health.Health;
    }

    void OnEnable()
    {
        Current = this;
        if (jump != null) jump.Jumped += OnJumped;
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (Current == this) Current = null;   // statics outlive the scene: don't keep pointing at a removed character
        if (jump != null) jump.Jumped -= OnJumped;
        if (health != null) health.Died -= OnDied;
    }

    void Update()
    {
        bool dead = health != null && health.IsDead;
        Vector3 velocity = body != null ? body.velocity : Vector3.zero;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);   // going up or down isn't running

        if (!dead) Turn(flat);

        // Running: the speed along the body's own directions, in walking speeds (1 = walking, about 2 = sprinting),
        // so moving to the body's left plays "run left". Dividing by the walking speed keeps it right after a speed perk.
        float walkSpeed = controller != null && controller.Speed > 0.1f ? controller.Speed : 5f;
        Vector3 move = dead ? Vector3.zero : Quaternion.Inverse(transform.rotation) * flat / walkSpeed;
        SetFloat(MoveX, move.x, blendTime);
        SetFloat(MoveZ, move.z, blendTime);
        SetFloat(RunSpeed, runAnimationSpeed, 0f);

        // Falling: only after a moment in the air. On the ground = PlayerController or PlayerJump says so. PlayerJump
        // looks below the feet itself, so it still knows when Unity stops sending PlayerController touch messages
        // (it can do that for a body that stands still).
        bool grounded = (controller == null && jump == null)
                        || (controller != null && controller.IsGrounded)
                        || (jump != null && jump.IsGrounded);
        airTime = grounded ? 0f : airTime + Time.deltaTime;
        SetBool(Falling, airTime > fallDelay);
        SetBool(Dead, dead);

        // Hurt: health dropped by enough since the last frame (e.g. an enemy's hit).
        if (health != null)
        {
            if (flinchWhenHurt && !dead && lastHealth - health.Health >= flinchMinDamage) PlayAction(PlayerAction.Hit);
            lastHealth = health.Health;
        }

        ShowForView();
    }

    // ===== My methods =====

    // Plays an action on the upper body from any script, e.g. PlayerAnimator.Play(PlayerAction.Attack);
    // Does nothing if there's no character in the scene, so it's safe to call anywhere.
    public static void Play(PlayerAction action)
    {
        ActionPlayed?.Invoke(action);   // the first-person hands (if any) play it too
        if (Current != null) Current.PlayAction(action);
    }

    // Static values outlive the scene (and Play sessions if domain reload is off): start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Current = null;
        ActionPlayed = null;
    }

    // The speed an action plays at, in both controllers: ActionSpeed, but Hit (the flinch, body only) as it was made,
    // and the heavy punches a bit slower than the jabs (HeavySpeed), so they look heavy.
    public const float HeavySpeed = 1.5f;
    public static float ActionSpeedOf(PlayerAction action)
    {
        if (action == PlayerAction.Hit) return 1f;
        if (action == PlayerAction.HeavyHook || action == PlayerAction.HeavyPunch) return HeavySpeed;
        return ActionSpeed;
    }

    // The same, for this character.
    public void PlayAction(PlayerAction action)
    {
        if (health != null && health.IsDead) return;   // no attacking while lying dead
        SetTrigger(Actions[(int)action]);
    }

    void OnJumped()
    {
        airTime = 0f;
        SetTrigger(Jump);
    }

    // Stops any action at once, so the body doesn't keep swinging while it falls down.
    void OnDied()
    {
        SetBool(Dead, true);
        foreach (int trigger in Actions)
        {
            if (parameters.Contains(trigger)) animator.ResetTrigger(trigger);
        }
        if (actionLayer >= 0 && animator.HasState(actionLayer, EmptyState)) animator.Play(EmptyState, actionLayer, 0f);
    }

    // Turns the body (only around the up axis) towards where the camera looks, or the way it's moving.
    void Turn(Vector3 flatVelocity)
    {
        float target = yaw;
        if (facing == Facing.CameraDirection)
        {
            Transform cam = cameraControl != null ? cameraControl.transform
                          : Camera.main != null ? Camera.main.transform : null;
            if (cam != null) target = cam.eulerAngles.y;
        }
        else if (flatVelocity.sqrMagnitude > 0.25f)   // faster than 0.5 m/s. Standing still keeps the last direction.
        {
            target = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
        }

        yaw = Mathf.MoveTowardsAngle(yaw, target, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // First person: the camera sits inside the head, so only the shadow is drawn. Third person: drawn as normal.
    void ShowForView()
    {
        bool want = shadowOnlyInFirstPerson && cameraControl != null && cameraControl.IsFirstPerson;
        if (want == shadowOnly) return;   // nothing changed since the last frame
        shadowOnly = want;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null) parts[i].shadowCastingMode = want ? ShadowCastingMode.ShadowsOnly : partShadows[i];
        }
    }

    // These skip parameters the controller doesn't have (e.g. a hand-made controller), instead of filling the
    // Console with warnings every frame.
    void SetFloat(int id, float value, float smoothing)
    {
        if (!parameters.Contains(id)) return;
        if (smoothing > 0f) animator.SetFloat(id, value, smoothing, Time.deltaTime);   // eases towards the value
        else animator.SetFloat(id, value);
    }

    void SetBool(int id, bool value)
    {
        if (parameters.Contains(id)) animator.SetBool(id, value);
    }

    void SetTrigger(int id)
    {
        if (parameters.Contains(id)) animator.SetTrigger(id);
    }
}
