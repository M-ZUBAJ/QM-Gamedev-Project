/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on by hand: PunchEffect adds it to a monster the first time a punch pushes it.
Unity Editor: How hard punches push is on the Player's PunchEffect (Knockback, Heavy Knockback, Knockback Seconds); the
              rare box's Brawler perk pushes farther. Testing: Play and punch a slime: it slides back a little.
Purpose: Pushes a monster away (a punch's knockback): its body (a non-kinematic Rigidbody) gets the push as its speed,
         and for a moment it's "pushed": Marko's EnemyMovement asks IsPushed and doesn't walk then, so the push isn't
         cancelled at once. Bodies that physics doesn't move (kinematic: a deer, a dead monster) and things without a
         body (trees, rocks) aren't pushed.
Requirement: Knockback.cs x-> Rigidbody (Unity)
             Knockback.cs <-x ArrowProjectile.cs, EnemyMovement.cs, PunchEffect.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class Knockback : MonoBehaviour
{
    // ===== Fields =====
    float pushedUntil;   // Time.time when the push is over

    // ===== My methods =====

    // Pushes the body "target" belongs to: velocity = the push (m/s, flat away from the puncher, plus a little up),
    // for "seconds". Does nothing to kinematic bodies or things without one.
    public static void Push(Component target, Vector3 velocity, float seconds)
    {
        if (target == null) return;
        Rigidbody body = target.GetComponentInParent<Rigidbody>();
        if (body == null || body.isKinematic) return;

        Knockback state = body.GetComponent<Knockback>();
        if (state == null) state = body.gameObject.AddComponent<Knockback>();
        state.pushedUntil = Time.time + seconds;

        // The push replaces its walk (the walk would fight it), and lifts it a little unless it's already going up.
        body.velocity = new Vector3(velocity.x, Mathf.Max(body.velocity.y, velocity.y), velocity.z);
    }

    // Is this body being pushed right now? (Marko's EnemyMovement doesn't walk then.)
    public static bool IsPushed(Rigidbody body)
    {
        if (body == null) return false;
        Knockback state = body.GetComponent<Knockback>();
        return state != null && Time.time < state.pushedUntil;
    }
}
