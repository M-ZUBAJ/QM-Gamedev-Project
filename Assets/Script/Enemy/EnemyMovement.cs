using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform target; // who the emeny should follow 
    public float moveSpeed = 2f;
    public float stopDistance = 2f; // how close enemy needs to be to attack
    // for eg: needs to be within 2 meters

    // [Tony] added (10/11/2026): with a Rigidbody on the enemy (the SlimeEnemy has one), it moves with physics in
    // FixedUpdate below: walls and buildings stop it, it walks up and down the ground, and it turns to face its target.
    // Without a Rigidbody (the old ball Enemy) nothing changes: Update moves it exactly like before.
    // To undo: delete every line marked [Tony] in this file. Log: Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md
    [Tooltip("[Tony] How fast the enemy turns to face its target, in degrees per second (only with a Rigidbody).")]
    public float turnSpeed = 360f;                                                    // [Tony] added
    private Rigidbody body;                                                           // [Tony] added

    void Awake()                                                                      // [Tony] added
    {                                                                                 // [Tony] added
        body = GetComponent<Rigidbody>();                                             // [Tony] added
    }                                                                                 // [Tony] added

    void Update()
    {
        if (body != null) return; // [Tony] added: an enemy with a Rigidbody moves in FixedUpdate (physics) instead

        // Don't move if we haven't assigned a target.
        if (target == null)
        {
            return;
        }

        //Else Work out the distance to the target.
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        //AND Move towards the target if we're too far away.
        if (distance > stopDistance)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;// means the enemy will not follow enemy up or down

            transform.position += direction.normalized
                * moveSpeed * Time.deltaTime;
        }
    }

    // [Tony] added: physics movement, used only when the enemy has a Rigidbody. Setting the speed (velocity) instead
    // of the position lets the physics stop the enemy at walls, and gravity keeps it on the ground.
    void FixedUpdate()                                                                // [Tony] added
    {                                                                                 // [Tony] added
        if (body == null || target == null) return;                                   // [Tony] added

        Vector3 toTarget = target.position - body.position;                           // [Tony] added
        toTarget.y = 0f; // flat: gravity does the up and down                       // [Tony] added
        float distance = toTarget.magnitude;                                          // [Tony] added

        Vector3 move = Vector3.zero;                                                  // [Tony] added
        if (distance > stopDistance && distance > 0.01f)                              // [Tony] added
        {                                                                             // [Tony] added
            move = toTarget / distance * moveSpeed;                                   // [Tony] added
        }                                                                             // [Tony] added
        body.velocity = new Vector3(move.x, body.velocity.y, move.z); // keeps its falling speed   // [Tony] added

        if (distance > 0.01f) // turn to face the target                             // [Tony] added
        {                                                                             // [Tony] added
            Quaternion face = Quaternion.LookRotation(toTarget);                      // [Tony] added
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, face, turnSpeed * Time.fixedDeltaTime));   // [Tony] added
        }                                                                             // [Tony] added
    }                                                                                 // [Tony] added
}