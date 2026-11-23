/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up: a trap (BearTrap) puts it on a monster when it catches it (EnemyStun.Stun), and it stays
        there switched off until the next time.
Unity Editor: Testing: Play, build a bear trap (B) and let a monster walk into it: the monster stands still for the
              trap's Hold Seconds and doesn't attack, then chases you again. While it's held, ⋮ (top right of this
              component on the monster) > Test: Let Go Now frees it at once.
Purpose: Holds a monster still for a few seconds (stunned), so the player can hit it freely:
         - Marko's EnemyMovement and EnemyAttack are switched off for that time (the same way the turtle's
           ShellDefense hides it), and back on at the end, only if they were on before;
         - its sideways speed is set to 0 every physics step (a punch's knockback can't push it out either), and if
           a spot was given (the trap's middle) it's kept on that spot;
         - stunned again while still held = held until the later end;
         - a monster that dies or burns away at dawn while held is simply let go (its death code has already
           switched everything off).
         Marko's scripts aren't changed.
Requirement: EnemyStun.cs x-> EnemyCombat.cs
             EnemyStun.cs o-> EnemyMovement.cs, EnemyAttack.cs, EnemyAnimator.cs, Rigidbody (Unity)
             EnemyStun.cs <-x BearTrap.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStun : MonoBehaviour
{
    // ===== Fields =====
    EnemyCombat combat;          // Marko's: its health (a dead monster is let go)
    EnemyMovement movement;      // Marko's: switched off while held
    EnemyAttack attack;          // Marko's: switched off while held
    EnemyAnimator look;          // Tony's: dying / burning away = let go
    Rigidbody body;

    float until;                 // game time (Time.time) when it's let go
    bool held;                   // being held right now
    bool movementWasOn;          // what to switch back on at the end
    bool attackWasOn;
    bool pinned;                 // kept on a spot (the trap's middle)
    Vector3 pin;

    public bool IsHeld => held;
    public float SecondsLeft => held ? Mathf.Max(0f, until - Time.time) : 0f;

    // ===== Unity messages (Unity calls these) =====

    void FixedUpdate()
    {
        if (!held) return;
        if (Gone())
        {
            held = false;   // dying or burning away: its death code switched everything off already
            enabled = false;
            return;
        }
        if (Time.time >= until)
        {
            LetGo();
            return;
        }
        Hold();
    }

    [ContextMenu("Test: Let Go Now")]
    void TestLetGo()
    {
        if (held) LetGo();
    }

    // ===== My methods =====

    // Holds this monster still for "seconds" (a monster that is already held stays held until the later end).
    // pinAt: a spot to keep it on (e.g. the trap's middle; only the sideways position is used), or null for where it is.
    // Returns false for something that isn't a living monster (no EnemyCombat, or dead / dying).
    public static bool Stun(GameObject monster, float seconds, Vector3? pinAt = null)
    {
        if (monster == null || seconds <= 0f) return false;
        EnemyCombat health = monster.GetComponentInParent<EnemyCombat>();
        if (health == null || health.currentHealth <= 0f) return false;

        EnemyStun stun = health.GetComponent<EnemyStun>();
        if (stun == null) stun = health.gameObject.AddComponent<EnemyStun>();
        return stun.Begin(seconds, pinAt);
    }

    // True while this monster (or the monster this collider belongs to) is held.
    public static bool IsStunned(Component part)
    {
        if (part == null) return false;
        EnemyStun stun = part.GetComponentInParent<EnemyStun>();
        return stun != null && stun.held;
    }

    bool Begin(float seconds, Vector3? pinAt)
    {
        if (combat == null) Find();
        if (Gone()) return false;

        if (!held)   // the first stun: remember what was on, then switch it off
        {
            movementWasOn = movement != null && movement.enabled;
            attackWasOn = attack != null && attack.enabled;
        }
        held = true;
        enabled = true;
        until = Mathf.Max(until, Time.time + seconds);
        pinned = pinAt.HasValue;
        if (pinned) pin = pinAt.Value;
        Hold();
        return true;
    }

    void Find()
    {
        combat = GetComponent<EnemyCombat>();
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
        look = GetComponent<EnemyAnimator>();
        body = GetComponent<Rigidbody>();
    }

    // Keeps it still: Marko's movement and attack off (something else may have switched them on, e.g. the turtle
    // coming out of its shell), no sideways speed, on its spot.
    void Hold()
    {
        if (movement != null && movement.enabled) movement.enabled = false;
        if (attack != null && attack.enabled) attack.enabled = false;

        if (body != null && !body.isKinematic)
        {
            Vector3 v = body.velocity;
            body.velocity = new Vector3(0f, v.y, 0f);   // falling still works
            if (pinned) body.MovePosition(new Vector3(pin.x, body.position.y, pin.z));
        }
        else if (pinned)
        {
            transform.position = new Vector3(pin.x, transform.position.y, pin.z);
        }
    }

    // Free again: what was on before is switched back on.
    void LetGo()
    {
        held = false;
        enabled = false;
        if (movement != null && movementWasOn) movement.enabled = true;
        if (attack != null && attackWasOn) attack.enabled = true;
    }

    // Dead, dying or burning away (at dawn).
    bool Gone()
    {
        return combat == null || combat.currentHealth <= 0f || (look != null && (look.IsDead || look.IsVanishing));
    }
}
