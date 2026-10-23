using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    public Transform target;

    private float nextAttackTime = 0f; // tracks when next attack is allowed
    // this makes sure enemy cant attack every frame and thus kill player 
    // within a second

    void Update()
    {
        // Dont attack if no target is assigned
        if (target == null)
        {
            return;
        }

        // else work out distance to target.
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        // if enemy is close enough and 
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {   // line below will be replaced once player sphere is in the game.
        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
}