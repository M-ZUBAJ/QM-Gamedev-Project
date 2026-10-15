using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Camera playerCamera;

    public float attackRange = 2.5f; // from how far away player can attack
    public float handDamage = 10f;
    public float swordDamage = 25f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // if the player presses the left click
        {
            Attack();
        }
    }

    void Attack()
    {
        // fires and invisible ray from the centre of the camera (where the crosshair is)
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange))
        {
            EnemyCombat enemy =
                hit.collider.GetComponentInParent<EnemyCombat>(); // lookds for enemy's health script

            // if the enemy is found, deal damage
            if (enemy != null)
            {
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

                enemy.TakeDamage(damage);
            }

        }
    }
}
