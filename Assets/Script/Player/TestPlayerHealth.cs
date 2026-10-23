using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayerHealth : MonoBehaviour
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;

        Debug.Log("Player took " + damage + " damage. Health: " + health);

        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player defeated!");
        }
    }
}