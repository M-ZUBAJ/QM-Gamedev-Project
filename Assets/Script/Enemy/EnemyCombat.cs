using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    [Header("Attack Miss Chance")]
    [Range(1f, 15f)]// percentage chance the attack will miss/enemy dodges
    public float missChance = 5f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Notice this method is public
    // You can call it from other Scripts !!!
    // whenever player attacks an enemy
    public void TakeDamage(float damage)
    {

        float roll = Random.Range(0f, 100f); // generates a random number from 0-100

        if (roll <= missChance)
        {
            Debug.Log("Enemy Dodged Attack And Took No Damage !");
            return;
        }


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

        int coinsEarned = Random.Range(3, 7);// player can get 1-3 coins per kill

        if (PlayerCoins.Instance != null) // if the player has a PlayerCoins component
        {
            PlayerCoins.Instance.AddCoins(coinsEarned);
        }
        else
        {
            Debug.LogWarning(
                "Enemy defeated, but PlayerCoins was not found!"
            );
        }

        Destroy(gameObject);
    }

}