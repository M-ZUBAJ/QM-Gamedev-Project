/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: An enemy's top object, next to Marko's EnemyMovement and EnemyAttack. Tools > World > Build Desert Content puts it
        on the desert's TurtleShellEnemy.
Unity Editor: Select the enemy to see its ranges in the Scene view: red = Notice Range, grey = Give Up Range.
              Testing: Play the new world in the day and find a turtle in the desert: it stands still until you come
              close (or hit it), then comes for you; run far away and it stops.
Purpose: An enemy that minds its own business until the player comes near: it stands still (its idle animation) and
         only chases and attacks once the player is within Notice Range, or once it's hurt (e.g. a fireball from far
         away); it gives up when the player gets farther than Give Up Range, or dies, and stops where it is.
         It does this by giving Marko's EnemyMovement and EnemyAttack the player as their target, or none (his
         scripts do nothing without a target; his movement keeps its last speed then, so it's stopped here).
         Without this script an enemy chases the player from anywhere as soon as its spawner gives it the target
         (Marko's night slimes).
Requirement: EnemyAggro.cs x-> EnemyMovement.cs, EnemyAttack.cs
             EnemyAggro.cs o-> EnemyCombat.cs, PlayerController.cs, PlayerHealth.cs, Rigidbody (Unity)
             EnemyAggro.cs <-x DesertBuilder.cs, EnemyBuilder.cs
             EnemyAggro.cs <-o DesertMonsterSpawner.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAggro : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("It notices the player (and starts chasing) this close, in meters.")]
    [Min(0f)]
    [SerializeField] float noticeRange = 14f;

    [Tooltip("It gives up the chase when the player is farther away than this, in meters (bigger than Notice Range, so " +
             "it doesn't keep starting and stopping at the edge).")]
    [Min(0f)]
    [SerializeField] float giveUpRange = 32f;

    const float CheckEvery = 0.2f;   // seconds between looks (no need to measure every frame)

    EnemyMovement movement;   // Marko's
    EnemyAttack attack;       // Marko's
    EnemyCombat combat;       // Marko's health: being hurt starts the chase (can be missing)
    Rigidbody body;           // can be missing (Marko's old ball enemy)
    Transform player;
    PlayerHealth playerHealth;
    bool chasing;
    float lastHealth;
    float nextCheck;

    public bool IsChasing => chasing;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
        combat = GetComponent<EnemyCombat>();
        body = GetComponent<Rigidbody>();
    }

    // In Start, not Awake: by then a spawner has set the targets (they're taken away again until the player is near).
    void Start()
    {
        FindPlayer();
        if (combat != null) lastHealth = combat.currentHealth;
        Chase(false);
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + CheckEvery;
        if (player == null && !FindPlayer()) return;

        bool hurt = combat != null && combat.currentHealth < lastHealth;
        if (combat != null) lastHealth = combat.currentHealth;
        bool playerAlive = playerHealth == null || !playerHealth.IsDead;
        Vector3 between = player.position - transform.position;
        between.y = 0f;
        float distance = between.magnitude;

        if (!chasing && playerAlive && (distance <= noticeRange || hurt)) Chase(true);
        else if (chasing && (!playerAlive || distance > giveUpRange)) Chase(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, noticeRange);
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);
    }

    // ===== My methods =====

    // Gives Marko's movement and attack the player as their target (chase) or none (stand still).
    void Chase(bool on)
    {
        chasing = on;
        Transform target = on ? player : null;
        if (movement != null) movement.target = target;
        if (attack != null) attack.target = target;
        // Marko's movement leaves its last speed when it has no target (and the enemy has no friction): stop it here.
        if (!on && body != null && !body.isKinematic) body.velocity = new Vector3(0f, body.velocity.y, 0f);
    }

    bool FindPlayer()
    {
        PlayerController found = FindFirstObjectByType<PlayerController>();
        if (found == null) return false;
        player = found.transform;
        playerHealth = found.GetComponent<PlayerHealth>();
        return true;
    }
}
