using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    // [Tony] added (22/11/2026): how many times your coins this enemy drops when it dies (1 = as you made it: the slime).
    // Tony's NightMonsters sets it when a turtle (1.5), a Chest Monster (2) or a Beholder (3) spawns.
    // To undo: delete this field and put back the line in the [Tony] replaced comment in Die().
    // Log: Docs/TonyEditMate/Marko/22-11-2026 Bigger Monsters Drop More Coins.md
    [Tooltip("[Tony] Coins dropped = your 3 - 6 x this (1 = the slime). Tony's NightMonsters sets it per monster when it spawns.")]
    public float coinMultiplier = 1f;                                                 // [Tony] added

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
    public void TakeDamage(float damage, string weaponId = null)
    {
        if (dead) return; // [Tony] added: a dying enemy can't be hit again

        float roll = Random.Range(0f, 100f); // generates a random number from 0-100

        if (roll <= missChance)
        {
            Debug.Log("Enemy Dodged Attack And Took No Damage !");
            return;
        }

        // [Tony] added (23/11/2026): a human enemy's armor (Tony's HumanGear) takes its share off the hit, like the
        // player's armor does (a full copper set 10 %, iron 15 %, gold 25 %, diamond 40 %). Monsters have no HumanGear:
        // their hits are unchanged. To undo: delete the two lines marked [Tony] added (23/11/2026).
        // Log: Docs/TonyEditMate/Marko/23-11-2026 Human Armor Takes Damage Off.md
        HumanGear armor = GetComponent<HumanGear>();                                  // [Tony] added (23/11/2026)
        if (armor != null) damage = armor.Reduce(damage);                             // [Tony] added (23/11/2026)

        currentHealth -= damage;

        Debug.Log("Enemy took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die(weaponId);
        }
    }

    void Die(string weaponId)
    {
        dead = true;

        // Count a kill for the sword that delivered the lethal hit.
        if (!string.IsNullOrEmpty(weaponId) && WeaponUpgradeSystem.Instance != null)
        {
            WeaponUpgradeSystem.Instance.RecordKill(weaponId);
        }
        Debug.Log("Enemy defeated!");

        // [Tony] replaced (22/11/2026): int coinsEarned = Random.Range(3, 7);// player can get 1-3 coins per kill
        // Bigger monsters drop more: your 3 - 6 coins x Coin Multiplier, rounded (the turtle 1.5 = 4 - 9, the Chest Monster
        // 2 = 6 - 12, the Beholder 3 = 9 - 18; the slime 1 = 3 - 6 as before).
        int coinsEarned = Mathf.RoundToInt(Random.Range(3, 7) * coinMultiplier);     // [Tony] replaced

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

        // [Tony] added (21/11/2026): extra coins on top of yours, from Tony's armor set bonus (copper set +1, iron set
        // +2) and the rare box's Bounty Hunter perk (+1 each). To undo: delete this line.
        // Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
        // [Tony] edited (23/11/2026): was MonsterBounty.Killed(); - now it also says who died, so a human enemy (Tony's
        // HumanBrain) counts as "Humans killed" on the end pages instead of "Monsters killed". The coins are the same.
        // To undo: put the line above back. Log: Docs/TonyEditMate/Marko/23-11-2026 Humans Killed Count Apart From Monsters.md
        MonsterBounty.Killed(gameObject);                                             // [Tony] edited

        // [Tony] replaced (10/11/2026): Destroy(gameObject);
        // With Tony's EnemyAnimator on the enemy, the death animation plays first (and the enemy stops moving,
        // attacking and being hit), then the body is removed. Without one it's removed at once, like before.
        float removeAfter = 0f;                                                       // [Tony] added
        EnemyAnimator animator = GetComponent<EnemyAnimator>();                      // [Tony] added
        if (animator != null) removeAfter = animator.PlayDeath();                    // [Tony] added
        Destroy(gameObject, removeAfter);                                             // [Tony] replaced
    }

}