
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

    // Right click the component in Play mode to test without a player attack.
    [ContextMenu("Test Damage 10")]
    void TestDamage()
    {
        TakeDamage(10f);
    }

    // Gives the drop to the player, then removes the node.
    void Break()
    {
        PlayerInventory inv = PlayerInventory.Instance;

        if (inv == null || dropItem == null)
        {
            Debug.LogWarning(name + " broke but has no Drop Item or there is no PlayerInventory in the scene.");
        }
        else
        {
            int amount = Random.Range(dropMin, dropMax + 1);   // the top number is not included, so +1
            if (!inv.Add(dropItem.Id, dropItem.DisplayName, dropItem.GetIcon(), amount, dropItem))
            {
                Debug.Log("Inventory is full, the drop was lost.");
            }
        }

        Destroy(gameObject);
    }
}