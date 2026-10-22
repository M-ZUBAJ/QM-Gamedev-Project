
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceNode : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The item this gives when it breaks (an Item Data file from Assets/Config/Items).")]
    [SerializeField] ItemData dropItem;

    [Tooltip("How much damage it takes before it breaks.")]
    public float maxHealth = 30f;

    //// below was added by marko ////////
    public enum ResourceType
    {
        Tree,
        Stone,
        Animal
    }
    // end of code added ////////////////


    public ResourceType resourceType;

    [Tooltip("Fewest items it drops.")]
    [SerializeField] int dropMin = 3;

    [Tooltip("Most items it drops.")]
    [SerializeField] int dropMax = 5;

    [Header("Bonus drop (optional)")]
    [Tooltip("An extra item it may also give, e.g. Apple from a tree. Leave empty for none.")]
    [SerializeField] ItemData bonusItem;
    [Tooltip("Chance of the bonus, from 0 (never) to 1 (always). 0.3 = 30%.")]
    [Range(0f, 1f)][SerializeField] float bonusChance = 0.3f;
    [Tooltip("Fewest bonus items.")]
    [SerializeField] int bonusMin = 1;
    [Tooltip("Most bonus items.")]
    [SerializeField] int bonusMax = 2;

    public float currentHealth;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ===== My methods =====

    // The player's attack calls this (same name as EnemyCombat.TakeDamage).
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log(name + " took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0f) Break();
    }

    // Gives the drop to the player, then removes the node, the drops go straight to the inventory.
    void Break()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || dropItem == null)
        {
            Debug.LogWarning(name + " broke but has no Drop Item or there is no PlayerInventory in the scene.");
        }
        else
        {
            Give(inv, dropItem, dropMin, dropMax);   // the normal drop, always

            // Random.value is a random number from 0 to 1. Below the chance = the bonus drops.
            if (bonusItem != null && Random.value < bonusChance)
                Give(inv, bonusItem, bonusMin, bonusMax);
        }
        Destroy(gameObject);
    }

    // Puts a random amount (min to max) of an item into the inventory.
    void Give(PlayerInventory inv, ItemData item, int min, int max)
    {
        int amount = Random.Range(min, max + 1);
        if (!inv.Add(item.Id, item.DisplayName, item.GetIcon(), amount, item))
            Debug.Log("Inventory is full, the drop was lost.");
    }
}