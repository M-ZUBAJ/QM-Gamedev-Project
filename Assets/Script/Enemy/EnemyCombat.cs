using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    // [Tony] added (10/11/2026): true once the enemy has died. The body now stays for its death animation, and
    // without this a second hit in that time would call Die() again and give the coins twice.
    // To undo: delete every line marked [Tony] and put back the line in the [Tony] replaced comment in Die().
    // Log: Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md
    private bool dead;                                                                // [Tony] added

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
        if (dead) return; // [Tony] added: a dying enemy can't be hit again

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
        dead = true; // [Tony] added
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

        // [Tony] replaced (10/11/2026): Destroy(gameObject);
        // With Tony's EnemyAnimator on the enemy, the death animation plays first (and the enemy stops moving,
        // attacking and being hit), then the body is removed. Without one it's removed at once, like before.
        float removeAfter = 0f;                                                       // [Tony] added
        EnemyAnimator animator = GetComponent<EnemyAnimator>();                      // [Tony] added
        if (animator != null) removeAfter = animator.PlayDeath();                    // [Tony] added
        Destroy(gameObject, removeAfter);                                             // [Tony] replaced
    }

}