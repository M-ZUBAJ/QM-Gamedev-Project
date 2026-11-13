/*
First Date: 12/11/2026
Date: 12/11/2026
Author: Tony
Use by: The top object of a wild animal, e.g. DeerNode. Tools > World > Build Wild Animal puts it there, set up.
        By hand: drag it onto the animal's top object (the one with its collider). The Animator on a child needs a
        controller with two floats: "Speed" (0 = standing, 1 = walking, 2 = running) and "AnimSpeed" (default 1).
Unity Editor: Select an animal to see its circles in the Scene view: yellow = where it wanders, red = how close the
              player can come before it runs, green = how far away the player must be before it calms down.
              Testing: Play and walk up to a deer: it runs away. Hit it: it keeps running for a while even if you stop.
Purpose: A shy wild animal. It grazes and walks around its home, and runs away when the player comes near or hurts it.
         It stays on the ground (follows the terrain), steers around trees, rocks, buildings and steep slopes, and
         plays idle / walk / run on its Animator by itself. Once the player is far enough away it calms down and that
         spot becomes its new home. It moves itself (no physics), so walls never get pushed and it never tips over.
Requirement: WildAnimal.cs o-> Animator (Unity), ResourceNode.cs, PlayerController.cs, PlayerHealth.cs
             WildAnimal.cs <-x DeathEffectBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class WildAnimal : MonoBehaviour
{
    // ===== Fields =====
    [Header("Wandering")]
    [Tooltip("How far from its home it wanders (meters). Home = where it appeared, or where it calmed down after running away.")]
    [Min(0f)]
    [SerializeField] float wanderRadius = 12f;

    [Tooltip("Walking speed in meters per second (the player walks at 7.8).")]
    [Min(0.1f)]
    [SerializeField] float walkSpeed = 1.4f;

    [Tooltip("Seconds it stands still and grazes between walks: a random time from X to Y.")]
    [SerializeField] Vector2 idleSeconds = new Vector2(2f, 7f);

    [Header("Running away")]
    [Tooltip("It runs away when the player comes closer than this (meters).")]
    [Min(0f)]
    [SerializeField] float fleeDistance = 12f;

    [Tooltip("It calms down when the player is farther away than this (meters). Keep it bigger than Flee Distance.")]
    [Min(0f)]
    [SerializeField] float safeDistance = 24f;

    [Tooltip("Running speed in meters per second. A bit slower than the player walks (7.8), so the player can catch it. Raise it to make hunting harder.")]
    [Min(0.1f)]
    [SerializeField] float runSpeed = 7f;

    [Tooltip("Seconds it keeps running after being hit, even if the player stops chasing.")]
    [Min(0f)]
    [SerializeField] float panicSeconds = 5f;

    [Header("Moving")]
    [Tooltip("How fast it turns, in degrees per second.")]
    [Min(10f)]
    [SerializeField] float turnSpeed = 270f;

    [Tooltip("Slopes steeper than this (degrees) are like walls to it.")]
    [Range(5f, 80f)]
    [SerializeField] float maxSlope = 35f;

    [Tooltip("How far ahead it looks for trees, rocks and walls (meters). It looks further ahead when running.")]
    [Min(0.5f)]
    [SerializeField] float lookAhead = 2f;

    [Header("Animation")]
    [Tooltip("How fast the walk animation plays (1 = as made).")]
    [Min(0.1f)]
    [SerializeField] float walkAnimationSpeed = 1f;

    [Tooltip("How fast the run animation plays at full running speed (1 = as made). Faster legs keep a fast run from looking like sliding.")]
    [Min(0.1f)]
    [SerializeField] float runAnimationSpeed = 1.5f;

    // The Animator's parameters (Tools > World > Build Wild Animal makes a controller with them).
    public const string SpeedName = "Speed";           // 0 = standing, 1 = walking, 2 = running (in between = mixed)
    public const string AnimSpeedName = "AnimSpeed";   // how fast the animation plays

    // Ways to try when the way ahead is blocked (degrees from where it wants to go), nearest first.
    static readonly float[] Detours = { 30f, -30f, 60f, -60f, 90f, -90f, 125f, -125f, 160f, -160f };
    static readonly RaycastHit[] hits = new RaycastHit[16];   // shared by all animals, so looking ahead makes no garbage

    enum Mood { Grazing, Walking, Fleeing }

    Mood mood = Mood.Grazing;
    float grazeEnds;        // when grazing ends (Time.time)
    float panicEnds;        // when the running after a hit ends
    float nextLook;         // when to check the way ahead again
    float turnSide = 1f;    // tries detours on this side first (+1 right, -1 left), so it doesn't zigzag
    Vector3 home;           // the middle of where it wanders
    Vector3 target;         // where it walks to
    Vector3 heading;        // the clear way it's going (flat, length 1)
    float speed;            // meters per second right now
    float lastHealth;       // ResourceNode's health last frame (it was hit when it goes down)
    bool healthKnown;

    Animator animator;
    bool hasSpeed, hasAnimSpeed;
    int speedId, animSpeedId;
    ResourceNode node;
    Transform player;
    PlayerHealth playerHealth;
    Terrain terrain;
    float probeHeight = 0.8f;   // chest height above its feet: where it looks ahead from
    float probeRadius = 0.3f;   // about half its width

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;   // this script moves it, not the animations
            speedId = Animator.StringToHash(SpeedName);
            animSpeedId = Animator.StringToHash(AnimSpeedName);
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == SpeedName) hasSpeed = true;
                if (p.name == AnimSpeedName) hasAnimSpeed = true;
            }
        }

        node = GetComponent<ResourceNode>();
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null)
        {
            player = controller.transform;
            playerHealth = controller.GetComponent<PlayerHealth>();
        }
        terrain = Terrain.activeTerrain;
        MeasureBody();

        home = transform.position;
        heading = Flat(transform.forward).normalized;
        if (Ground(transform.position, out float y, out _)) transform.position = new Vector3(transform.position.x, y, transform.position.z);
        Graze();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // the game is paused (e.g. the victory screen stops time)

        Decide();
        if (Time.time >= nextLook)
        {
            nextLook = Time.time + 0.15f;   // looking ahead about 7 times a second is plenty
            LookAhead();
        }
        Move(dt);
        Animate(dt);
    }

    // Keeps the numbers sensible while they're changed in the Inspector.
    void OnValidate()
    {
        safeDistance = Mathf.Max(safeDistance, fleeDistance + 1f);
        idleSeconds.x = Mathf.Max(0f, idleSeconds.x);
        idleSeconds.y = Mathf.Max(idleSeconds.x, idleSeconds.y);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 middle = Application.isPlaying ? home : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(middle, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }

    // ===== My methods =====

    // Grazing, walking or running away?
    void Decide()
    {
        // Hit by the player: Turki's ResourceNode lost health.
        if (node != null)
        {
            if (healthKnown && node.currentHealth < lastHealth) panicEnds = Time.time + panicSeconds;
            lastHealth = node.currentHealth;
            healthKnown = true;
        }

        float playerDistance = PlayerDistance();
        if (playerDistance < fleeDistance || Time.time < panicEnds)
        {
            if (mood != Mood.Fleeing) nextLook = 0f;   // just got scared: pick the way to run right now, not in 0.15 s
            mood = Mood.Fleeing;
        }
        else if (mood == Mood.Fleeing)
        {
            if (playerDistance > safeDistance)   // far enough: calm down, and live here now
            {
                home = transform.position;
                Graze();
            }
        }
        else if (mood == Mood.Grazing && Time.time >= grazeEnds)
        {
            StartWalking();
        }
        else if (mood == Mood.Walking && Flat(target - transform.position).magnitude < 0.6f)
        {
            Graze();   // arrived
        }
    }

    // Picks the way to go: straight away from the player (running) or to the target (walking), around what's in the way.
    void LookAhead()
    {
        Vector3 wanted;
        if (mood == Mood.Fleeing)
        {
            wanted = player != null ? Flat(transform.position - player.position) : heading;
            if (wanted.sqrMagnitude < 0.01f) wanted = Flat(transform.forward);
        }
        else if (mood == Mood.Walking)
        {
            wanted = Flat(target - transform.position);
        }
        else
        {
            return;   // grazing: standing still
        }
        wanted.Normalize();

        float distance = lookAhead + (mood == Mood.Fleeing ? speed * 0.35f : 0f);   // looks further ahead at speed
        if (Clear(wanted, distance))
        {
            heading = wanted;
            return;
        }

        if (mood == Mood.Walking)   // something's in the way of its walk: stop, graze, then pick another spot
        {
            Graze();
            return;
        }
        heading = Detour(wanted, distance);
    }

    // The nearest clear way around what's blocking "wanted". Nothing clear: turn around.
    Vector3 Detour(Vector3 wanted, float distance)
    {
        foreach (float angle in Detours)
        {
            float turn = angle * turnSide;
            Vector3 way = Quaternion.Euler(0f, turn, 0f) * wanted;
            if (!Clear(way, distance)) continue;
            turnSide = Mathf.Sign(turn);   // keep turning this way next time
            return way;
        }
        return -wanted;
    }

    // Can it go this way for this far? Not if a tree, rock, wall, building, other animal or the player is in the way,
    // or if the ground ahead is too steep or ends.
    bool Clear(Vector3 way, float distance)
    {
        Vector3 from = transform.position + Vector3.up * probeHeight;
        int count = Physics.SphereCastNonAlloc(from, probeRadius, way, hits, distance,
                                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c.transform.IsChildOf(transform)) continue;   // its own body
            if (hits[i].distance <= 0f) continue;              // already touching at the start (e.g. it appeared in a bush): let it walk out
            if (c is TerrainCollider)
            {
                if (Vector3.Angle(hits[i].normal, Vector3.up) > maxSlope) return false;   // a steep hillside
                continue;                                                                  // gentle ground: fine
            }
            return false;
        }

        Vector3 ahead = transform.position + way * Mathf.Min(distance, 2f);
        return Ground(ahead, out _, out Vector3 normal) && Vector3.Angle(normal, Vector3.up) <= maxSlope;
    }

    void Move(float dt)
    {
        float wanted = mood == Mood.Fleeing ? runSpeed : mood == Mood.Walking ? walkSpeed : 0f;
        float change = wanted > speed ? (mood == Mood.Fleeing ? 14f : 3f) : 6f;   // bolts off fast, slows down gently
        speed = Mathf.MoveTowards(speed, wanted, change * dt);

        if (mood != Mood.Grazing && heading.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), turnSpeed * dt);
        }
        if (speed <= 0f) return;

        // Forward, slower while turning sharply (so it doesn't run into what it's turning away from).
        Vector3 forward = Flat(transform.forward).normalized;
        float facing = heading.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(forward, heading)) : 1f;
        Vector3 next = transform.position + forward * (speed * Mathf.Max(0.2f, facing) * dt);
        if (Ground(next, out float y, out _))
        {
            next.y = y;
            transform.position = next;
        }
        else
        {
            speed = 0f;   // no ground there (the edge of the world): stop
        }
    }

    // Speed 0 = idle, 1 = walk, 2 = run; AnimSpeed makes the legs keep up with the speed.
    void Animate(float dt)
    {
        if (animator == null) return;
        float blend = speed <= walkSpeed
            ? speed / walkSpeed
            : 1f + Mathf.Clamp01((speed - walkSpeed) / Mathf.Max(0.01f, runSpeed - walkSpeed));
        if (hasSpeed) animator.SetFloat(speedId, blend, 0.1f, dt);
        if (hasAnimSpeed)
        {
            animator.SetFloat(animSpeedId, blend <= 1f ? walkAnimationSpeed : Mathf.Lerp(walkAnimationSpeed, runAnimationSpeed, blend - 1f));
        }
    }

    void Graze()
    {
        mood = Mood.Grazing;
        grazeEnds = Time.time + Random.Range(idleSeconds.x, idleSeconds.y);
    }

    // A random spot near home that it can walk straight toward. None found: grazes a bit longer.
    void StartWalking()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            Vector3 spot = home + new Vector3(r.x, 0f, r.y);
            Vector3 way = Flat(spot - transform.position);
            if (way.magnitude < 1.5f || !Clear(way.normalized, lookAhead)) continue;

            target = spot;
            heading = way.normalized;
            mood = Mood.Walking;
            return;
        }
        Graze();
    }

    // How far away the player is, along the ground. Very far if there's no player or they're dead (nothing to fear).
    float PlayerDistance()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead)) return float.MaxValue;
        return Flat(player.position - transform.position).magnitude;
    }

    // Where it looks ahead from: its chest, from the size of its collider.
    void MeasureBody()
    {
        Collider body = GetComponent<Collider>();
        if (body is BoxCollider box)
        {
            Vector3 scale = transform.lossyScale;
            probeHeight = Mathf.Max(0.2f, box.center.y * scale.y);
            probeRadius = Mathf.Clamp(box.size.x * 0.5f * scale.x * 0.8f, 0.1f, 1f);
        }
        else if (body != null)
        {
            Bounds b = body.bounds;
            probeHeight = Mathf.Max(0.2f, b.center.y - transform.position.y);
            probeRadius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z) * 0.8f, 0.1f, 1f);
        }
    }

    // The ground's height and slope at a point: the terrain under it, else the first solid thing below it.
    bool Ground(Vector3 point, out float height, out Vector3 normal)
    {
        Terrain t = TerrainUnder(point);
        if (t != null)
        {
            TerrainData data = t.terrainData;
            Vector3 local = point - t.transform.position;
            height = t.SampleHeight(point) + t.transform.position.y;
            normal = data.GetInterpolatedNormal(local.x / data.size.x, local.z / data.size.z);
            return true;
        }

        height = point.y;
        normal = Vector3.up;
        float nearest = float.MaxValue;
        int count = Physics.RaycastNonAlloc(point + Vector3.up * 2f, Vector3.down, hits, 6f,
                                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.IsChildOf(transform) || hits[i].distance >= nearest) continue;
            nearest = hits[i].distance;
            height = hits[i].point.y;
            normal = hits[i].normal;
        }
        return nearest < float.MaxValue;
    }

    // The terrain this point is over (the last one found is checked first; most worlds have only one).
    Terrain TerrainUnder(Vector3 point)
    {
        if (Over(terrain, point)) return terrain;
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (!Over(t, point)) continue;
            terrain = t;
            return t;
        }
        return null;
    }

    static bool Over(Terrain t, Vector3 point)
    {
        if (t == null || t.terrainData == null) return false;
        Vector3 local = point - t.transform.position;
        Vector3 size = t.terrainData.size;
        return local.x >= 0f && local.z >= 0f && local.x <= size.x && local.z <= size.z;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
