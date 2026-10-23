using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // [Tony] added: EventSystem, to know if the mouse is over a menu / button

public class PlayerCombat : MonoBehaviour
{
    public Camera playerCamera;

    public float attackRange = 2.5f; // from how far away player can attack


    [Header("Enemy and Animal Damage")]
    public float handDamage = 10f;
    public float swordDamage = 25f;
    public float axeDamage = 10f;
    public float pickAxeDamage = 10f;

    [Header("Resource Damage")]
    public float handResourceDamage = 5f;
    public float swordResourceDamage = 7f;
    public float axeTreeDamage = 25f;
    public float axeStoneDamage = 5f;
    public float pickAxeTreeDamage = 5f;
    public float pickAxeStoneDamage = 25f;



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
        // [Tony] added (20/10/2026): a left click on a menu is not an attack. Before this, clicking inventory slots,
        // tabs, crafting rows, build buttons or the Respawn button attacked too and used 10 stamina each time.
        // Skip while a menu is open (inventory, crafting, build menu, death screen), while another mode owns the mouse
        // (e.g. placing a building), or when the free mouse is over any UI. (While aiming the mouse is locked in the
        // middle of the screen, on the crosshair picture, so the UI check only runs while the mouse is free.)
        // To undo: delete the lines marked [Tony].
        if (MenuState.AnyOpen || ClickClaim.Claimed) return;                                                  // [Tony] added
        if (Cursor.lockState != CursorLockMode.Locked && EventSystem.current != null &&                       // [Tony] added
            EventSystem.current.IsPointerOverGameObject()) return;                                            // [Tony] added

        if (Input.GetMouseButtonDown(0)) // if the player presses the left click
        {
            Attack();
        }
    }



    void Attack()
    {
        // Check stamina before attacking.
        if (playerStamina != null && playerStamina.Stamina <= 0f)
        {
            Debug.Log("Attack Failed: Not Enough Stamina !");
            return;
        }

        if (playerStamina != null)
        {
            playerStamina.Use(attackStaminaCost);
        }

        // Fire a ray from the centre of the camera.
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange))
        {
            EnemyCombat enemy =
                hit.collider.GetComponentInParent<EnemyCombat>();

            ResourceNode resource =
                hit.collider.GetComponentInParent<ResourceNode>();

            // Find the currently equipped item.
            PlayerInventory inventory = PlayerInventory.Instance;

            string equippedItem = "";

            if (inventory != null &&
                inventory.ActiveItem != null &&
                !inventory.ActiveItem.IsEmpty)
            {
                equippedItem = inventory.ActiveItem.id;
            }

            float damage;

            // Damage against enemies.
            if (enemy != null)
            {
                damage = handDamage;

                if (equippedItem == "Sword")
                    damage = swordDamage;
                else if (equippedItem == "Axe")
                    damage = axeDamage;
                else if (equippedItem == "PickAxe")
                    damage = pickAxeDamage;

                enemy.TakeDamage(damage);
            }
            // Damage against trees, stone and animals.
            else if (resource != null)
            {
                damage = handResourceDamage;

                switch (resource.resourceType)
                {
                    case ResourceNode.ResourceType.Tree:
                        if (equippedItem == "Sword")
                            damage = swordResourceDamage;
                        else if (equippedItem == "Axe")
                            damage = axeTreeDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeTreeDamage;
                        break;

                    case ResourceNode.ResourceType.Stone:
                        if (equippedItem == "Sword")
                            damage = swordResourceDamage;
                        else if (equippedItem == "Axe")
                            damage = axeStoneDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeStoneDamage;
                        break;

                    case ResourceNode.ResourceType.Animal:
                        damage = handDamage;

                        if (equippedItem == "Sword")
                            damage = swordDamage;
                        else if (equippedItem == "Axe")
                            damage = axeDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeDamage;
                        break;
                }

                resource.TakeDamage(damage);
            }
        }
    }



    // void Attack()
    // {
    //     // if the player stamina field exists and it is <=0
    //     if (playerStamina != null && playerStamina.Stamina <= 0f)
    //     {
    //         Debug.Log("Attack Failed: Not Enough Stamina !");
    //         return; // dont attack
    //     }

    //     if (playerStamina != null)
    //     {
    //         playerStamina.Use(attackStaminaCost);
    //     }

    //     // fires and invisible ray from the centre of the camera (where the crosshair is)
    //     Ray ray = playerCamera.ViewportPointToRay(
    //         new Vector3(0.5f, 0.5f, 0f)
    //     );

    //     RaycastHit hit;

    //     if (Physics.Raycast(ray, out hit, attackRange))
    //     {
    //         EnemyCombat enemy =
    //             hit.collider.GetComponentInParent<EnemyCombat>(); // lookds for enemy's health script

    //         ResourceNode resource =
    //             hit.collider.GetComponentInParent<ResourceNode>();// looks for ResourceNodes Health

    //         float damage = handDamage;

    //         // check if the player has another weapon equipped
    //         // if yes, use the damage of that weapon instead
    //         PlayerInventory inventory = PlayerInventory.Instance;

    //         if (inventory != null &&
    //             inventory.ActiveItem != null &&
    //             !inventory.ActiveItem.IsEmpty &&
    //             inventory.ActiveItem.id == "Sword")
    //         {
    //             damage = swordDamage;
    //         }

    //         // if the enemy is found, deal damage
    //         if (enemy != null)
    //         {
    //             enemy.TakeDamage(damage);
    //         }

    //         else if (resource != null)
    //         {
    //             resource.TakeDamage(damage);
    //         }

    //     }
    // }
}
