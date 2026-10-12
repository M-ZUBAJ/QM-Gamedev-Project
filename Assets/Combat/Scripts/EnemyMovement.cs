using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform target; // who the emeny should follow 
    public float moveSpeed = 2f;
    public float stopDistance = 2f; // how close enemy needs to be to attack
    // for eg: needs to be within 2 meters

    void Update()
    {
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
}