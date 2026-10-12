using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Notice this method is public
    // You can call it from other Scripts !!!
    // whenever player attacks an enemy
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log("Enemy took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy defeated!");
        Destroy(gameObject);
    }
}