/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: The flying arrow prefab, Assets/Prefabs/Item/Arrows/Arrow Projectile (Tools > Items > Build Bow And Arrows makes
        it). BowShooter makes a copy of it for each shot: nothing to put in a scene.
Unity Editor: The numbers can be changed on the prefab (they count for the next shots): how fast it falls, how hard a hit
              pushes, how long it stays stuck, how close you must come to get an arrow back. Look and Length are set by
              the tool. Testing the Arrow Recovery perk: Play, Player > PerkEffects ⋮ > "Test: Every New Perk Once", then
              shoot a deer and walk up to it, and shoot the ground and walk over the arrow: "+1 Diamond Arrow" each time.
Purpose: One flying arrow. It flies from the bow, falls a little (Gravity) and turns to point the way it flies. Every
         frame it checks the whole stretch it flies through (a thin sphere cast), so even a fast arrow can't skip
         through a monster. What it hits:
           - a monster (Marko's EnemyCombat) or an animal (Turki's ResourceNode of type Animal): hurt by the arrow's
             damage. It's a projectile hit, not a melee one: a turtle in its shell is hurt too (like the fireball), and
             the monster's own dodge chance still counts. The Vampire perk heals a share, the damage pops up where it hit
             (big and orange for a critical), and the monster is pushed back a little. Then the arrow is used up: no
             arrow stays in a monster's or an animal's skin;
           - anything else (ground, trees, rocks, chests): nothing is hurt; it sticks there and disappears after Stuck
             Seconds (it shrinks away), or with that thing.
         With the Arrow Recovery perk (epic and legendary boxes; PerkEffects.ArrowRecovery) arrows can be had back:
           - one that hits a monster or an animal stays in it (it's carried along); come within Take Back Distance and
             it's yours again. When that creature dies (or burns away at dawn) its arrows fall out onto the ground;
           - one that misses stays where it landed; come within Pick Up Distance and it's yours again. One in a tree or a
             rock falls out when that breaks.
           An arrow that can be picked up stays Pick Up Seconds (one in a living creature stays until that dies). Each
           one back is "+1 <arrow>" in the inventory (it waits while the inventory is full). Misses For Everyone lets
           everyone pick up missed arrows, even without the perk (like Minecraft).
         Only the newest Most Stuck arrows stay (the oldest go first), so a long fight doesn't fill the world.
         It never hits the player who shot it.
Requirement: ArrowProjectile.cs x-> EnemyCombat.cs, ResourceNode.cs, DamageNumbers.cs, PerkEffects.cs, Knockback.cs, ItemData.cs, PlayerInventory.cs
             ArrowProjectile.cs o-> PlayerController.cs, EnemyAnimator.cs, PlayerHealth.cs
             ArrowProjectile.cs <-x BowBuilder.cs, BowShooter.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArrowProjectile : MonoBehaviour
{
    // ===== Fields =====
    [Header("Flying")]
    [Tooltip("How fast it falls, in m/s² (real gravity is 9.8; less = flatter shots, easier to aim).")]
    [Range(0f, 20f)]
    [SerializeField] float gravity = 6f;

    [Tooltip("How thick the arrow is for hitting things, in meters (a little more than the real arrow, so near misses count).")]
    [Range(0.01f, 0.3f)]
    [SerializeField] float hitRadius = 0.05f;

    [Tooltip("Seconds it flies before it vanishes if it hits nothing.")]
    [Range(1f, 30f)]
    [SerializeField] float flySeconds = 8f;

    [Header("Hitting")]
    [Tooltip("How hard a hit monster is pushed back, in m/s (0 = no push).")]
    [Range(0f, 10f)]
    [SerializeField] float knockback = 1.5f;

    [Tooltip("How long the push lasts, in seconds (the monster doesn't walk meanwhile).")]
    [Range(0f, 1f)]
    [SerializeField] float knockbackSeconds = 0.15f;

    [Header("Stuck")]
    [Tooltip("How deep the tip goes into what it hits, in meters.")]
    [Range(0f, 0.5f)]
    [SerializeField] float stickDepth = 0.12f;

    [Tooltip("Seconds an arrow that can't be picked up stays stuck before it disappears (it shrinks away in its last half second).")]
    [Range(0f, 120f)]
    [SerializeField] float stuckSeconds = 20f;

    [Tooltip("The most arrows stuck in things at once: one more makes the oldest disappear.")]
    [Range(1, 300)]
    [SerializeField] int mostStuck = 60;

    [Header("Getting arrows back (the Arrow Recovery perk)")]
    [Tooltip("How close you must come to an arrow stuck in a living monster or animal to take it back, in meters.")]
    [Range(0.5f, 6f)]
    [SerializeField] float takeBackDistance = 2.5f;

    [Tooltip("How close you must come to an arrow on the ground, or in a tree or a rock, to pick it up, in meters.")]
    [Range(0.3f, 4f)]
    [SerializeField] float pickUpDistance = 1.6f;

    [Tooltip("Seconds an arrow that can be picked up stays before it disappears (one in a living creature stays until " +
             "that creature dies, then this long on the ground).")]
    [Range(10f, 900f)]
    [SerializeField] float pickUpSeconds = 120f;

    [Tooltip("Off: only with the Arrow Recovery perk do missed arrows stay to be picked up. On: everyone can pick up " +
             "missed arrows (like Minecraft), and the perk adds the ones in monsters and animals.")]
    [SerializeField] bool missesForEveryone = false;

    [Header("Look (set by Tools > Items > Build Bow And Arrows)")]
    [Tooltip("The arrow's look: a child whose forward (z) is the way the arrow points, with the tip at its middle and the " +
             "arrow model under it. ToolInHand copies it onto the drawn bow's string.")]
    [SerializeField] GameObject look;

    [Tooltip("The arrow's length in meters, tip to back (measured by the tool).")]
    [Min(0.05f)]
    [SerializeField] float length = 0.8f;

    // What an arrow can hit: every layer but Ignore Raycast (2: the world barrier's walls), the gear preview (29), the
    // first-person hands (30) and the icon studio (31).
    public static readonly int HitLayers = ~((1 << 2) | (1 << 29) | (1 << 30) | (1 << 31));
    const float ShrinkSeconds = 0.5f;
    const float LookGap = 0.1f;        // seconds between two looks for the player (ten a second is plenty)
    const float PlayerRadius = 0.5f;   // the player's ball: its bottom is where the feet are
    const float PlayerHeight = 1.8f;   // the character standing on it, feet to head
    const float LieHeight = 0.02f;     // a fallen arrow lies this far above the ground (so it doesn't flicker into it)
    const float FallGravity = 9.8f;    // how fast an arrow falling out of something drops, m/s²
    static readonly RaycastHit[] hits = new RaycastHit[16];
    static readonly List<ArrowProjectile> stuck = new List<ArrowProjectile>();   // oldest first

    Vector3 velocity;
    float damage;
    bool critical;
    bool flying;
    float endTime;          // when it goes: Fly Seconds after the shot, Stuck Seconds or Pick Up Seconds after it stopped
    Vector3 stuckScale;     // its size once stuck (it shrinks from this at the end)
    ItemData item;          // which arrow it is: what's given back
    bool pickable;          // it can be picked up (the Arrow Recovery perk was there when it hit)
    bool inCreature;        // it's in a living monster or animal (Take Back Distance, no timer)
    Component carrier;      // the monster, animal, tree or rock it's in: it falls out when that dies or breaks
    EnemyAnimator carrierLook;   // that monster's animator (dying, burning away at dawn), if it has one
    bool falling;           // falling out of something onto the ground
    float fallSpeed;
    Vector3 liePosition;    // where it ends up lying (its tip; its middle is on the ground spot)
    Quaternion lieRotation;
    float nextLook;         // Time.time of the next look for the player

    public GameObject Look => look;
    public float Length => length;

    // ===== Unity messages (Unity calls these) =====

    void Update()
    {
        if (falling)
        {
            Fall();
            return;
        }
        if (!flying)
        {
            if (pickable && TryGiveBack()) return;
            if (inCreature) return;   // stays in a living creature until it dies or the player comes for it
            float left = endTime - Time.time;
            if (left <= 0f) Destroy(gameObject);
            else if (left < ShrinkSeconds) transform.localScale = stuckScale * (left / ShrinkSeconds);
            return;
        }
        if (Time.time >= endTime)
        {
            Destroy(gameObject);   // flew off into nothing
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;      // the game is paused
        velocity += Vector3.down * (gravity * dt);
        Vector3 step = velocity * dt;
        float distance = step.magnitude;
        if (distance < 0.00001f) return;
        Vector3 along = step / distance;

        if (FirstHit(transform.position, along, distance, out RaycastHit hit, out bool atStart))
        {
            Hit(hit, along, atStart);
            return;
        }
        transform.position += step;
        transform.rotation = Quaternion.LookRotation(along);
    }

    // After every Update of the frame: a creature, tree or rock that died this frame is removed at the end of the frame,
    // with everything stuck in it, so its arrows fall out now.
    void LateUpdate()
    {
        if (carrier != null && CarrierGone()) DropOut(Middle());
    }

    void OnDestroy()
    {
        stuck.Remove(this);
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        stuck.Clear();
    }

    // ===== My methods =====

    // Starts it flying (BowShooter calls this right after making the copy): its speed and direction in m/s, how much it
    // hurts, whether it's a critical hit (a big orange number), and which arrow it is (its head's look, and what's given
    // back with the Arrow Recovery perk).
    public void Launch(Vector3 startVelocity, float hurtBy, bool isCritical, ItemData arrow)
    {
        velocity = startVelocity;
        damage = hurtBy;
        critical = isCritical;
        item = arrow;
        flying = true;
        endTime = Time.time + flySeconds;
        if (velocity.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(velocity);
        Material head = arrow != null && arrow.IsAmmo ? arrow.Ammo.head : null;
        if (head != null) DressHead(head);
    }

    // The arrowhead of this kind: the arrow model's second part is its head.
    void DressHead(Material head)
    {
        foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = part.sharedMaterials;
            if (materials.Length < 2) continue;
            materials[1] = head;
            part.sharedMaterials = materials;
        }
    }

    // The nearest thing in the stretch flown this frame that isn't the player. atStart = it was already touching it
    // where the stretch began (a monster right in front of the bow): that counts as a hit there, but only on a monster
    // or an animal (inside the ground or a wall at the start = fly on, like a shot from inside a bush).
    bool FirstHit(Vector3 from, Vector3 along, float distance, out RaycastHit best, out bool atStart)
    {
        best = default;
        atStart = false;
        bool found = false;
        int count = Physics.SphereCastNonAlloc(from, hitRadius, along, hits, distance, HitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            bool touching = hit.distance <= 0f;
            if (touching && TargetIn(hit.collider) == null) continue;
            if (found && hit.distance >= best.distance) continue;
            best = hit;
            atStart = touching;
            found = true;
        }
        return found;
    }

    // A monster or an animal this collider belongs to, or null.
    static Component TargetIn(Collider part)
    {
        if (part == null) return null;
        EnemyCombat enemy = part.GetComponentInParent<EnemyCombat>();
        if (enemy != null) return enemy;
        ResourceNode node = part.GetComponentInParent<ResourceNode>();
        return node != null && node.resourceType == ResourceNode.ResourceType.Animal ? node : null;
    }

    // It hit something. A monster or an animal is hurt; without the Arrow Recovery perk the arrow is then used up
    // (nothing stays in its skin), with it the arrow stays in it, or falls out if the hit killed it (or it can't carry
    // an arrow). Anything else: it sticks there, to be picked up with the perk (or by everyone: Misses For Everyone).
    void Hit(RaycastHit hit, Vector3 along, bool atStart)
    {
        flying = false;
        Vector3 point = atStart ? transform.position : hit.point;
        Transform into = hit.collider != null ? hit.collider.transform : null;
        Component target = TargetIn(hit.collider);
        if (target == null)
        {
            pickable = PerkEffects.ArrowRecovery || missesForEveryone;
            Stick(into, point, along);
            if (pickable && into != null && transform.parent != null)
            {
                carrier = into.GetComponentInParent<ResourceNode>();   // a tree or a rock: it falls out when that breaks
            }
            return;
        }

        if (damage > 0f) Hurt(target, point, along);
        if (!PerkEffects.ArrowRecovery)
        {
            Destroy(gameObject);   // used up: no arrow stays in a monster's or an animal's skin
            return;
        }
        pickable = true;
        inCreature = true;
        Stick(into, point, along);
        carrier = target;
        carrierLook = target.GetComponentInParent<EnemyAnimator>();
        if (transform.parent == null || CarrierGone()) DropOut(point);   // the hit killed it, or it can't carry an arrow
    }

    // The arrow's damage, as a projectile: the Vampire perk heals a share, the number pops up where it hit, and a monster
    // is pushed back a little the way the arrow flew.
    void Hurt(Component target, Vector3 point, Vector3 along)
    {
        EnemyCombat enemy = target as EnemyCombat;
        ResourceNode node = target as ResourceNode;
        float hurtBy = damage;
        System.Action hurt = enemy != null ? (System.Action)(() => enemy.TakeDamage(hurtBy)) : () => node.TakeDamage(hurtBy);

        float before = DamageNumbers.HealthOf(target, out bool creature);
        PerkEffects.WithVampire(target, hurt)?.Invoke();
        DamageNumbers.ShowDealt(target, before, creature, point, critical);

        if (enemy != null && knockback > 0f)
        {
            Vector3 flat = new Vector3(along.x, 0f, along.z).normalized;
            Knockback.Push(enemy, flat * knockback + Vector3.up * 0.1f, knockbackSeconds);   // nothing if it just died (its body stops)
        }
    }

    // Stays where it hit, its tip Stick Depth in, moving with what it hit (unless that's stretched unevenly: the arrow
    // would stretch too). It stays Stuck Seconds, or Pick Up Seconds if it can be picked up. The oldest stuck arrow goes
    // when there are more than Most Stuck.
    void Stick(Transform into, Vector3 point, Vector3 along)
    {
        transform.SetPositionAndRotation(point + along * stickDepth, Quaternion.LookRotation(along));
        if (into != null && EvenlySized(into)) transform.SetParent(into, true);
        stuckScale = transform.localScale;
        endTime = Time.time + (pickable ? pickUpSeconds : stuckSeconds);

        stuck.Add(this);
        while (stuck.Count > mostStuck)
        {
            ArrowProjectile oldest = stuck[0];
            stuck.RemoveAt(0);
            if (oldest != null) Destroy(oldest.gameObject);
        }
    }

    // True once what it's in has died or broken: a monster's or an animal's health at 0 (or a monster burning away at
    // dawn), a tree or a rock broken.
    bool CarrierGone()
    {
        if (carrier == null) return true;
        if (carrier is EnemyCombat enemy && enemy.currentHealth <= 0f) return true;
        if (carrier is ResourceNode node && node.currentHealth <= 0f) return true;
        return carrierLook != null && (carrierLook.IsDead || carrierLook.IsVanishing);
    }

    // Falls out of a creature that died (or couldn't carry it), or of a tree or a rock that broke, onto the ground below,
    // where it lies to be picked up. No ground below: it's gone.
    void DropOut(Vector3 from)
    {
        carrier = null;
        carrierLook = null;
        inCreature = false;
        transform.SetParent(null, true);
        if (!GroundBelow(from, out RaycastHit ground))
        {
            Destroy(gameObject);
            return;
        }
        Vector3 flat = Vector3.ProjectOnPlane(Random.onUnitSphere, ground.normal);   // it lies any way round
        if (flat.sqrMagnitude < 0.0001f) flat = Vector3.ProjectOnPlane(Vector3.forward, ground.normal);
        flat.Normalize();
        lieRotation = Quaternion.LookRotation(flat, ground.normal);
        liePosition = ground.point + ground.normal * LieHeight + flat * (length * 0.5f);   // its middle on the spot
        fallSpeed = 0f;
        falling = true;
    }

    // Falling out: down faster and faster, turning to lie flat, until it lies on its spot (then it can be picked up).
    void Fall()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // the game is paused
        fallSpeed += FallGravity * dt;
        Vector3 at = transform.position;
        float y = at.y - fallSpeed * dt;
        if (y <= liePosition.y)
        {
            transform.SetPositionAndRotation(liePosition, lieRotation);
            falling = false;
            pickable = true;
            stuckScale = transform.localScale;
            endTime = Time.time + pickUpSeconds;
            return;
        }
        Vector3 across = Vector3.MoveTowards(new Vector3(at.x, 0f, at.z), new Vector3(liePosition.x, 0f, liePosition.z), 2f * dt);
        transform.SetPositionAndRotation(new Vector3(across.x, y, across.z), Quaternion.RotateTowards(transform.rotation, lieRotation, 540f * dt));
    }

    // The nearest ground straight below a point: not a monster or an animal (one that just died is still there this
    // frame), not the player, not something loose that moves.
    static bool GroundBelow(Vector3 from, out RaycastHit ground)
    {
        ground = default;
        bool found = false;
        int count = Physics.RaycastNonAlloc(from + Vector3.up * 0.3f, Vector3.down, hits, 60f, HitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider part = hits[i].collider;
            if (part == null || part.GetComponentInParent<PlayerController>() != null || TargetIn(part) != null) continue;
            if (part.attachedRigidbody != null && !part.attachedRigidbody.isKinematic) continue;
            if (found && hits[i].distance >= ground.distance) continue;
            ground = hits[i];
            found = true;
        }
        return found;
    }

    // Close enough (alive, with room in the inventory): one of this arrow goes back into the inventory ("+1 Stone Arrow")
    // and this arrow is gone. Looks ten times a second.
    bool TryGiveBack()
    {
        if (item == null || Time.time < nextLook) return false;
        nextLook = Time.time + LookGap;
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null) return false;
        float reach = inCreature ? takeBackDistance : pickUpDistance;
        if (DistanceToPlayer(Middle(), inventory.transform) > reach) return false;
        PlayerHealth health = inventory.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return false;
        if (!inventory.Add(item.Id, item.DisplayName, item.GetIcon(), 1, item)) return false;   // no room: it waits
        Destroy(gameObject);
        return true;
    }

    // The middle of the arrow (its root is the tip; the rest of it is behind).
    Vector3 Middle() => transform.position - transform.forward * (length * 0.5f);

    // How far a point is from the player, who stands from the bottom of the ball up to head height.
    static float DistanceToPlayer(Vector3 point, Transform player)
    {
        Vector3 feet = player.position + Vector3.down * PlayerRadius;
        float y = Mathf.Clamp(point.y, feet.y, feet.y + PlayerHeight);
        return Vector3.Distance(point, new Vector3(feet.x, y, feet.z));
    }

    // True if this object's size is the same every way (an arrow parented to a stretched one would stretch with it).
    static bool EvenlySized(Transform part)
    {
        Vector3 s = part.lossyScale;
        float small = Mathf.Min(Mathf.Abs(s.x), Mathf.Min(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        float big = Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        return small > 0.0001f && big / small < 1.02f;
    }
}
