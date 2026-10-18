using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Camera playerCamera;

    public float attackRange = 2.5f; // from how far away player can attack
    public float handDamage = 10f;
    public float swordDamage = 25f;

    [Header("Stamina")]
    public float attackStaminaCost = 10f;
    private PlayerStamina playerStamina;

    // finds the stamina component on the same Player Object
    void Awake()
    {
        playerStamina = GetComponent<PlayerStamina>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // if the player presses the left click
        {
            Attack();
        }
    }

    void Attack()
    {
        // if the player stamina field exists and it is <=0
        if (playerStamina != null && playerStamina.Stamina <= 0f)
        {
            Debug.Log("Attack Failed: Not Enough Stamina !");
            return; // dont attack
        }

        if (playerStamina != null)
        {
            playerStamina.Use(attackStaminaCost);
        }

        // fires and invisible ray from the centre of the camera (where the crosshair is)
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange))
        {
            EnemyCombat enemy =
                hit.collider.GetComponentInParent<EnemyCombat>(); // lookds for enemy's health script

            ResourceNode resource =
                hit.collider.GetComponentInParent<ResourceNode>();// looks for ResourceNodes Health

            float damage = handDamage;

            // check if the player has another weapon equipped
            // if yes, use the damage of that weapon instead
            PlayerInventory inventory = PlayerInventory.Instance;

            if (inventory != null &&
                inventory.ActiveItem != null &&
                !inventory.ActiveItem.IsEmpty &&
                inventory.ActiveItem.id == "Sword")
            {
                damage = swordDamage;
            }

            // if the enemy is found, deal damage
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            else if (resource != null)
            {
                resource.TakeDamage(damage);
            }

        }
    }
}
