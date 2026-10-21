using System.Collections;
using System.Collections.Generic;


using UnityEngine;

public class PlayerFireball : MonoBehaviour
{
    [Header("Fireball Cooldown")]
    [SerializeField] float startingCooldown = 3f; // 3 seconds cooldown
    [SerializeField, Range(0f, 1f)]
    float cooldownReductionPerPerk = 0.20f; // cool down reduced by 20% per perk stack
    [SerializeField] float minimumCooldown = 1f;

    [Header("Fireball")]
    [SerializeField] GameObject fireballPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float damageMultiplier = 1.2f;

    int perkStacks;
    float nextFireTime;

    PlayerCombat playerCombat;

    public int PerkStacks => perkStacks;

    public bool HasFireballPerk => perkStacks > 0;

    public float CurrentCooldown
    {
        get
        {
            float cooldown = startingCooldown;

            for (int i = 1; i < perkStacks; i++)
                cooldown *= 1f - cooldownReductionPerPerk;

            return Mathf.Max(minimumCooldown, cooldown);
        }
    }

    void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        if (!HasFireballPerk) return;
        if (!Input.GetKeyDown(KeyCode.Q)) return;

        PlayerInventory inventory = PlayerInventory.Instance;

        if (inventory == null ||
            inventory.ActiveItem == null ||
            inventory.ActiveItem.IsEmpty ||
            inventory.ActiveItem.id != "Sword")
        {
            Debug.Log("Equip your sword to cast Fireball.");
            return;
        }

        if (Time.time < nextFireTime) return;

        if (fireballPrefab == null || firePoint == null)
        {
            Debug.LogWarning(
                "Fireball prefab or Fire Point has not been assigned."
            );
            return;
        }

        if (playerCombat == null)
        {
            Debug.LogWarning("PlayerCombat was not found on the player.");
            return;
        }

        float damage = playerCombat.swordDamage * damageMultiplier;

        GameObject projectile = Instantiate(
            fireballPrefab,
            firePoint.position,
            firePoint.rotation
        );

        FireballProjectile fireball =
            projectile.GetComponent<FireballProjectile>();

        if (fireball == null)
        {
            Debug.LogError(
                "Fireball prefab is missing FireballProjectile."
            );

            Destroy(projectile);
            return;
        }

        fireball.Initialize(damage);
        nextFireTime = Time.time + CurrentCooldown;
    }

    public void AddFireballPerk()
    {
        perkStacks++;

        Debug.Log(
            "Fireball perk stacks: " + perkStacks +
            " | Cooldown: " + CurrentCooldown.ToString("F2") +
            " seconds"
        );
    }
}
