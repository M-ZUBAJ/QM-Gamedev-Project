/*
First Date: 10/11/2026
Date: 13/11/2026
Author: Tony
Use by: An enemy's top object, next to Marko's EnemyCombat, EnemyMovement and EnemyAttack, with the monster model
        (and its Animator) as a child. Tools > Enemies > Build Monster Enemy makes such enemies (e.g. SlimeEnemy).
Unity Editor: The numbers can be changed while playing. The Animator's controller needs the parameters
              Speed, Attack, Hit, Dead and Victory (the tool's controller has them).
              Testing: Play, select the EnemySpawner, click the ⋮ on its EnemySpawner component > Test Spawn Enemy.
              The enemy walks to the player and attacks; hit it and it flinches; kill it and it falls over, then
              disappears (with a DeathEffect: its body turns into red particles during the fall).
Purpose: Makes a monster act like a real enemy: it stands, walks or runs as fast as it really moves, attacks when
         Marko's EnemyAttack attacks, flinches when it loses health, plays its death animation before it
         disappears, and cheers when the player dies.
         It only shows the enemy: Marko's scripts still decide where it goes, when it attacks and its health.
Requirement: EnemyAnimator.cs o-> EnemyCombat.cs, EnemyMovement.cs, EnemyAttack.cs, PlayerHealth.cs, DeathEffect.cs, Animator (Unity), Rigidbody (Unity)
             EnemyAnimator.cs <-x DeathEffectBuilder.cs, EnemyAttack.cs, EnemyBuilder.cs, EnemyCombat.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAnimator : MonoBehaviour
{
    // The names in the Animator Controller. The builder tool makes the controller with these same names.
    public const string SpeedName = "Speed";       // how fast the enemy moves over the ground, in meters per second
    public const string AttackName = "Attack";
    public const string HitName = "Hit";
    public const string DeadName = "Dead";
    public const string VictoryName = "Victory";

    // ===== Fields =====
    [Header("Moving")]
    [Range(0f, 0.5f)]
    [Tooltip("Seconds to blend between standing, walking and running. 0 = switch at once (looks jumpy).")]
    [SerializeField] float blendTime = 0.15f;

    [Header("Hurt and dying")]
    [Tooltip("Flinch when it loses health.")]
    [SerializeField] bool flinchWhenHurt = true;

    [Range(0f, 10f)]
    [Tooltip("Seconds the body stays after dying (the death animation plays in this time), then it disappears. With a DeathEffect on the enemy, its dissolve times decide instead.")]
    [SerializeField] float bodyStaysFor = 2.5f;

    [Header("Player")]
    [Tooltip("Cheer (the Victory animation) when the player dies.")]
    [SerializeField] bool cheerWhenPlayerDies = true;

    static readonly int Speed = Animator.StringToHash(SpeedName);
    static readonly int Attack = Animator.StringToHash(AttackName);
    static readonly int Hit = Animator.StringToHash(HitName);
    static readonly int Dead = Animator.StringToHash(DeadName);
    static readonly int Victory = Animator.StringToHash(VictoryName);

    Animator animator;          // on the model (a child object)
    Rigidbody body;             // can be missing (Marko's old ball enemy has none)
    EnemyCombat combat;         // Marko's health script (can be missing)
    EnemyMovement movement;     // Marko's movement (can be missing)
    EnemyAttack attack;         // Marko's attack (can be missing)
    PlayerHealth playerHealth;  // to cheer when the player dies (can be missing)

    readonly HashSet<int> parameters = new HashSet<int>();   // the controller's parameters (missing ones are skipped)
    float lastHealth;           // health in the last frame, to see when it went down
    Vector3 lastPosition;       // to measure the speed when there's no Rigidbody
    bool dead;

    public bool IsDead => dead;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;   // Marko's movement moves the enemy: animations stay on the spot

        body = GetComponent<Rigidbody>();
        combat = GetComponent<EnemyCombat>();
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
        lastPosition = transform.position;
    }

    // In Start, not Awake: by now the Animator has loaded its controller, and the spawner has set the health.
    void Start()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters) parameters.Add(p.nameHash);
        }
        if (combat != null) lastHealth = combat.currentHealth;

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.Died += OnPlayerDied;
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;   // the enemy is gone: stop listening
    }

    void Update()
    {
        if (dead) return;

        // Speed: how fast it really moves over the ground (up and down don't count).
        Vector3 velocity = body != null && !body.isKinematic
            ? body.velocity
            : (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        velocity.y = 0f;
        if (parameters.Contains(Speed))
        {
            if (blendTime > 0f) animator.SetFloat(Speed, velocity.magnitude, blendTime, Time.deltaTime);   // eases towards it
            else animator.SetFloat(Speed, velocity.magnitude);
        }

        // Hurt: health went down since the last frame, and it's still alive (dying has its own animation).
        if (combat != null)
        {
            if (flinchWhenHurt && combat.currentHealth < lastHealth && combat.currentHealth > 0f) SetTrigger(Hit);
            lastHealth = combat.currentHealth;
        }
    }

    // ===== My methods =====

    // Marko's EnemyAttack calls this when it attacks.
    public void PlayAttack()
    {
        if (dead || (playerHealth != null && playerHealth.IsDead)) return;   // a dead player gets a cheer, not more hits
        SetTrigger(Attack);
    }

    // Marko's EnemyCombat calls this when the enemy dies. Plays the death animation and stops the enemy: no more
    // moving, attacking or being hit. Returns how many seconds to wait before removing it.
    public float PlayDeath()
    {
        if (!dead)
        {
            dead = true;
            SetBool(Dead, true);
            if (movement != null) movement.enabled = false;
            if (attack != null) attack.enabled = false;
            if (body != null)
            {
                body.velocity = Vector3.zero;   // before it turns kinematic (a kinematic body has no velocity)
                body.isKinematic = true;        // stays where it fell
            }
            foreach (Collider part in GetComponentsInChildren<Collider>()) part.enabled = false;   // walk and swing through it
        }
        if (!parameters.Contains(Dead)) return 0f;   // no death animation = removed at once, like before (a DeathEffect bursts then)

        // A DeathEffect turns the body into particles during the death animation; it's removed once they've taken it.
        DeathEffect effect = GetComponent<DeathEffect>();
        return effect != null ? effect.Dissolve() : bodyStaysFor;
    }

    void OnPlayerDied()
    {
        if (!dead && cheerWhenPlayerDies) SetTrigger(Victory);
    }

    // These skip parameters the controller doesn't have, instead of filling the Console with warnings.
    void SetBool(int id, bool value)
    {
        if (parameters.Contains(id)) animator.SetBool(id, value);
    }

    void SetTrigger(int id)
    {
        if (parameters.Contains(id)) animator.SetTrigger(id);
    }
}
