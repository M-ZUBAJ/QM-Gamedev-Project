/*
First Date: 10/11/2026
Date: 22/11/2026
Author: Tony
Use by: An enemy's top object, next to Marko's EnemyCombat, EnemyMovement and EnemyAttack, with the monster model
        (and its Animator) as a child. Tools > Enemies > Build Monster Enemy makes such enemies (e.g. SlimeEnemy).
Unity Editor: The numbers can be changed while playing. The Animator's controller needs the parameters
              Speed, Attack, Hit, Dead and Victory (the tool's controller has them; a monster with a shell, the turtle,
              also Shell and ShellHit).
              Testing: Play, select the EnemySpawner, click the ⋮ on its EnemySpawner component > Test Spawn Enemy.
              The enemy walks to the player and attacks; hit it and it flinches; kill it and it falls over, then
              disappears (with a DeathEffect: its body turns into red particles during the fall).
              ⋮ > Test: Burn Away Now (or the EnemySpawner's ⋮ > Test Despawn Enemies) shows what happens at dawn.
Purpose: Makes a monster act like a real enemy: it stands, walks or runs as fast as it really moves, attacks when
         Marko's EnemyAttack attacks, flinches when it loses health, plays its death animation before it
         disappears, and cheers when the player dies.
         When day comes, Marko's EnemySpawner calls Vanish for the night's enemies: each one stops (at a random moment
         within Vanish Spread seconds) and burns away into orange embers (its DeathEffect), then it's gone. No coins:
         nobody killed it.
         A monster with a shell (ShellDefense, the turtle) plays Defend while it's in its shell (SetShell), and
         DefendGetHit instead of a flinch when it's hit there (PlayShellHit, or a fireball hurting it inside).
         LastAttackTime = when it last attacked (a flying monster, FlyingEnemy, climbs away after each bite).
         It only shows the enemy: Marko's scripts still decide where it goes, when it attacks and its health.
Requirement: EnemyAnimator.cs o-> EnemyCombat.cs, EnemyMovement.cs, EnemyAttack.cs, PlayerHealth.cs, DeathEffect.cs, Animator (Unity), Rigidbody (Unity)
             EnemyAnimator.cs <-x DeathEffectBuilder.cs, EnemyAttack.cs, EnemyBuilder.cs, EnemyCombat.cs, EnemySpawner.cs
             EnemyAnimator.cs <-o ArrowProjectile.cs, DesertMonsterSpawner.cs, FlyingEnemy.cs, NightMonsters.cs, ShellDefense.cs
*/

using System.Collections;
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
    public const string ShellName = "Shell";         // a monster with a shell (the turtle): on = it's in its shell (Defend)
    public const string ShellHitName = "ShellHit";   // a hit on its shell (DefendGetHit)

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

    [Header("When day comes")]
    [Range(0f, 5f)]
    [Tooltip("The night's enemies burn away when day comes (Vanish), each at a random moment within this many seconds, so they don't all go at once.")]
    [SerializeField] float vanishSpread = 1.5f;

    static readonly int Speed = Animator.StringToHash(SpeedName);
    static readonly int Attack = Animator.StringToHash(AttackName);
    static readonly int Hit = Animator.StringToHash(HitName);
    static readonly int Dead = Animator.StringToHash(DeadName);
    static readonly int Victory = Animator.StringToHash(VictoryName);
    static readonly int Shell = Animator.StringToHash(ShellName);
    static readonly int ShellHit = Animator.StringToHash(ShellHitName);

    Animator animator;          // on the model (a child object)
    Rigidbody body;             // can be missing (Marko's old ball enemy has none)
    EnemyCombat combat;         // Marko's health script (can be missing)
    EnemyMovement movement;     // Marko's movement (can be missing)
    EnemyAttack attack;         // Marko's attack (can be missing)
    PlayerHealth playerHealth;  // to cheer when the player dies (can be missing)

    readonly HashSet<int> parameters = new HashSet<int>();   // the controller's parameters (missing ones are skipped)
    float lastHealth;           // health in the last frame, to see when it went down
    Vector3 lastPosition;       // to measure the speed when there's no Rigidbody
    bool dead;                  // stopped for good: dying, or burning away
    bool vanishing;             // burning away (or about to) because day came
    bool inShell;               // ShellDefense says it's in its shell: a hit shakes the shell instead of a flinch
    float lastAttack = -100f;   // Time.time of its last attack

    public bool IsDead => dead;
    public bool IsVanishing => vanishing;
    public float LastAttackTime => lastAttack;   // when it last attacked (FlyingEnemy climbs away after a bite)

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
            if (flinchWhenHurt && combat.currentHealth < lastHealth && combat.currentHealth > 0f)
                SetTrigger(inShell && parameters.Contains(ShellHit) ? ShellHit : Hit);   // in its shell: the shell shakes
            lastHealth = combat.currentHealth;
        }
    }

    // ===== My methods =====

    // ShellDefense calls this when the monster pulls into its shell (true) or comes out (false): the Defend animation.
    public void SetShell(bool hidden)
    {
        inShell = hidden;
        SetBool(Shell, hidden);
    }

    // ShellDefense calls this when a melee hit bounces off the shell: the shell's hit animation (DefendGetHit).
    public void PlayShellHit()
    {
        if (!dead) SetTrigger(ShellHit);
    }

    // Marko's EnemyAttack calls this when it attacks.
    public void PlayAttack()
    {
        if (dead || (playerHealth != null && playerHealth.IsDead)) return;   // a dead player gets a cheer, not more hits
        lastAttack = Time.time;
        SetTrigger(Attack);
    }

    // Marko's EnemyCombat calls this when the enemy dies. Plays the death animation and stops the enemy: no more
    // moving, attacking or being hit. Returns how many seconds to wait before removing it.
    public float PlayDeath()
    {
        if (!dead)
        {
            Stop();
            SetBool(Dead, true);
        }
        if (!parameters.Contains(Dead)) return 0f;   // no death animation = removed at once, like before (a DeathEffect bursts then)

        // A DeathEffect turns the body into particles during the death animation; it's removed once they've taken it.
        DeathEffect effect = GetComponent<DeathEffect>();
        return effect != null ? effect.Dissolve() : bodyStaysFor;
    }

    // Removes an enemy that wasn't killed: Marko's EnemySpawner calls this for the night's enemies when day comes.
    // It stops and burns away into orange embers (its DeathEffect), then it's removed. No coins: nobody killed it.
    // An enemy without an EnemyAnimator (Marko's old ball enemy) is removed at once, like before.
    public static void Vanish(GameObject enemy)
    {
        if (enemy == null) return;
        EnemyAnimator shown = enemy.GetComponent<EnemyAnimator>();
        if (shown != null && shown.gameObject.activeInHierarchy) shown.StartVanishing();
        else Destroy(enemy);
    }

    void StartVanishing()
    {
        if (dead || vanishing) return;   // dying already: dying removes it
        vanishing = true;
        StartCoroutine(Vanishing());
    }

    IEnumerator Vanishing()
    {
        yield return new WaitForSeconds(Random.Range(0f, vanishSpread));   // not all at the same moment
        if (dead) yield break;   // killed while waiting: dying removes it

        Stop();
        if (parameters.Contains(Speed)) animator.SetFloat(Speed, 0f);   // stands still while it burns
        DeathEffect effect = GetComponent<DeathEffect>();
        Destroy(gameObject, effect != null ? effect.Vanish() : 0f);
    }

    // Stops the enemy for good (dying or burning away): no more moving, attacking or being hit.
    void Stop()
    {
        dead = true;
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (body != null)
        {
            body.velocity = Vector3.zero;   // before it turns kinematic (a kinematic body has no velocity)
            body.isKinematic = true;        // stays where it is
        }
        foreach (Collider part in GetComponentsInChildren<Collider>()) part.enabled = false;   // walk and swing through it
    }

    [ContextMenu("Test: Burn Away Now")]
    void TestVanish()
    {
        if (Application.isPlaying) Vanish(gameObject);
        else Debug.Log("Press Play first.");
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
