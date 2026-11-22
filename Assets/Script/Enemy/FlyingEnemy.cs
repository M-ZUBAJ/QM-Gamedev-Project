/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: A flying enemy's top object, next to its Rigidbody and Marko's EnemyMovement / EnemyAttack (the Beholder:
        Assets/Prefabs/Enemy/BeholderEnemy). Tools > Enemies > Set Up Night Monsters puts it there.
Unity Editor: The numbers can be changed while playing. Select the enemy to see its heights in the Scene view (cyan =
              Cruise Height, red ring = Dive Distance).
              Testing: Play, select the sun (DaylightCycle), set Day to 4 and drag Time Of Day to 0.8 (night 4); or select
              the EnemySpawner and use its ⋮ > Test Spawn Enemy until a Beholder comes. It rises, flies to you high up,
              dives to bite, then climbs away and comes back.
Purpose: Makes an enemy fly. Marko's EnemyMovement still steers it toward the player over the ground (and turns it to
         face them) and his EnemyAttack still bites when it's close enough; this script only decides how high it is:
           - Cruise Height above the ground while it's away from the player: higher than a sword or a punch reaches
             (2.5 m from the eyes), so arrows and the fireball are the way to hit it up there;
           - within Dive Distance of the player it comes down to Bite Height (about the player's face): close enough for
             Marko's EnemyAttack to bite, and close enough for a sword;
           - after each bite (EnemyAnimator.LastAttackTime) it climbs back up and backs away for Climb Seconds, then
             comes down again;
           - it bobs gently up and down while it flies.
         It turns the Rigidbody's gravity off and flies over trees and rocks (they aren't ground to it). While a punch
         knocks it back (Knockback) it lets the push play out; once it dies or burns away at dawn (EnemyAnimator stops it)
         it does nothing more: it dissolves where it is, in the air.
Requirement: FlyingEnemy.cs x-> Knockback.cs, ResourceNode.cs, Rigidbody (Unity)
             FlyingEnemy.cs o-> EnemyMovement.cs, EnemyAnimator.cs
             FlyingEnemy.cs <-x EnemyBuilder.cs
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]   // after Marko's EnemyMovement in each physics step: it sets the speed over the ground, this the height
public class FlyingEnemy : MonoBehaviour
{
    // ===== Fields =====
    [Header("Height")]
    [Tooltip("How high it flies above the ground while it's away from the player, in meters (its lowest point, the tips " +
             "of its tentacles). A sword or a punch reaches about 2.5 m from the player's eyes: 3.5 is out of their reach, " +
             "not of an arrow's or the fireball's.")]
    [Range(1f, 10f)]
    [SerializeField] float cruiseHeight = 3.5f;

    [Tooltip("How high above the player's middle it comes down to bite, in meters (its lowest point). 0.4 = its eye " +
             "about at the player's face.")]
    [Range(-0.5f, 2f)]
    [SerializeField] float biteHeight = 0.4f;

    [Tooltip("The fastest it climbs or comes down, in meters per second.")]
    [Range(0.5f, 10f)]
    [SerializeField] float climbSpeed = 3f;

    [Header("Diving")]
    [Tooltip("It comes down at the player when it's this close over the ground, in meters. Farther away it stays at " +
             "Cruise Height.")]
    [Range(0f, 30f)]
    [SerializeField] float diveDistance = 7f;

    [Tooltip("Seconds it climbs back up and backs away after each bite, before it comes down again. 0 = it stays down " +
             "by the player.")]
    [Range(0f, 5f)]
    [SerializeField] float climbSeconds = 1.6f;

    [Tooltip("How fast it backs away from the player while climbing after a bite, in meters per second.")]
    [Range(0f, 10f)]
    [SerializeField] float backOffSpeed = 2.5f;

    [Header("Look")]
    [Tooltip("How far it bobs up and down while it flies, in meters. 0 = no bob.")]
    [Range(0f, 0.5f)]
    [SerializeField] float bobHeight = 0.15f;

    [Tooltip("Bobs per second.")]
    [Range(0.1f, 3f)]
    [SerializeField] float bobSpeed = 0.8f;

    const float Lift = 4f;               // how hard it heads for the height it wants: 4 = 4 m/s for every meter off (up to Climb Speed)
    const float MinClearance = 0.3f;     // never lower than this above the ground under it
    const float GroundCheckEvery = 0.2f; // seconds between two looks for the ground under it
    const float LookDown = 60f;          // how far below itself it looks for the ground, in meters
    const float Drift = 0.9f;            // with nobody to chase it slows down to this share of its speed every physics step
    static readonly RaycastHit[] hits = new RaycastHit[16];

    Rigidbody body;
    EnemyMovement movement;    // Marko's: its target is the player (can be missing: then it just hovers)
    EnemyAnimator animator;    // Tony's: when it last bit (can be missing: then it never climbs away)
    float groundY;             // the height of the ground under it (the last found)
    float nextGroundCheck;
    float bobStart;            // a random start, so several of them don't bob together

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        movement = GetComponent<EnemyMovement>();
        animator = GetComponent<EnemyAnimator>();
        body.useGravity = false;   // it flies: this script holds it up
        bobStart = Random.value * 10f;
        groundY = transform.position.y;
    }

    void FixedUpdate()
    {
        if (body.isKinematic) return;             // dying or burning away (EnemyAnimator stopped it): it stays where it is
        if (Knockback.IsPushed(body)) return;     // a punch is knocking it back: let the push play out
        if (Time.time >= nextGroundCheck)
        {
            nextGroundCheck = Time.time + GroundCheckEvery;
            FindGround();
        }

        Vector3 velocity = body.velocity;          // EnemyMovement has just set its speed over the ground
        float wantY = groundY + cruiseHeight;
        Transform target = movement != null ? movement.target : null;
        if (target != null)
        {
            Vector3 flat = target.position - body.position;
            flat.y = 0f;
            if (JustBit())
            {
                // Up and away for a moment, still facing the player (EnemyMovement keeps turning it).
                Vector3 away = flat.sqrMagnitude > 0.01f ? -flat.normalized : -transform.forward;
                velocity.x = away.x * backOffSpeed;
                velocity.z = away.z * backOffSpeed;
            }
            else if (flat.magnitude <= diveDistance)
            {
                wantY = target.position.y + biteHeight;   // down to the player's face
            }
        }
        else
        {
            velocity.x *= Drift;   // nobody to chase (EnemyMovement doesn't move it then): slow down and hover
            velocity.z *= Drift;
        }

        wantY = Mathf.Max(wantY, groundY + MinClearance);
        wantY += Mathf.Sin((Time.time + bobStart) * bobSpeed * Mathf.PI * 2f) * bobHeight;
        velocity.y = Mathf.Clamp((wantY - body.position.y) * Lift, -climbSpeed, climbSpeed);
        body.velocity = velocity;
    }

    // Shows the heights in the Scene view while it's selected: cyan = Cruise Height, red ring = Dive Distance.
    void OnDrawGizmosSelected()
    {
        float ground = Application.isPlaying ? groundY : transform.position.y;
        Vector3 at = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(at.x, ground, at.z), new Vector3(at.x, ground + cruiseHeight, at.z));
        Gizmos.DrawWireSphere(new Vector3(at.x, ground + cruiseHeight, at.z), 0.2f);
        Gizmos.color = Color.red;
        const int Steps = 32;
        for (int i = 0; i < Steps; i++)
        {
            float a = i * Mathf.PI * 2f / Steps, b = (i + 1) * Mathf.PI * 2f / Steps;
            Gizmos.DrawLine(at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * diveDistance,
                            at + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * diveDistance);
        }
    }

    // ===== My methods =====

    // True for Climb Seconds after a bite (EnemyAnimator remembers when it last attacked).
    bool JustBit() => climbSeconds > 0f && animator != null && Time.time < animator.LastAttackTime + climbSeconds;

    // The ground under it: the nearest thing below that isn't itself, a moving body (the player, another monster) or a
    // tree or rock (it flies over those, not on them). Nothing found = the last ground stays.
    void FindGround()
    {
        int count = Physics.RaycastNonAlloc(body.position + Vector3.up, Vector3.down, hits, LookDown + 1f,
                                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider part = hits[i].collider;
            if (part == null || hits[i].distance >= nearest) continue;
            if (part.transform.IsChildOf(transform)) continue;                            // itself
            if (hits[i].rigidbody != null && !hits[i].rigidbody.isKinematic) continue;    // the player, another monster
            if (part.GetComponentInParent<ResourceNode>() != null) continue;              // a tree, a rock, an animal
            nearest = hits[i].distance;
            groundY = hits[i].point.y;
        }
    }
}
