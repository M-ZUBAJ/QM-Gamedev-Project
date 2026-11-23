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

    // NEW: collect-by-key fields (used by the berry bush)
    [Header("Collect by key (berries)")]
    [Tooltip("On: attacks do nothing. The player collects it by looking at it and pressing the collect key.")]
    [SerializeField] bool collectWithKey = false;

    // [Tooltip("The key that collects it (same key as the chests).")]
    // [SerializeField] KeyCode collectKey = KeyCode.E;   // [Tony] replaced (21/11/2026): the key is KeyBindings.Interact (E),
    //                                                   the same key as the chests, shown and changed on the title screen's
    //                                                   Key Bindings page. Log: Docs/TonyEditMate/Turki/21-11-2026 Berry Key On The Key Bindings Page.md

    [Tooltip("How close the player must be, in metres.")]
    [Min(1f)]
    [SerializeField] float collectRange = 3f;

    public float currentHealth;

    // [Tony] added (12/11/2026): lets Tony's ToolRules read which pickaxe this rock needs (the drop item's Mined With:
    // copper and iron need a Stone pickaxe). Read only. Log: Docs/TonyEditMate/Turki/12-11-2026 Tier Tools.md
    public ItemData DropItem => dropItem; // [Tony] added

    // [Tony] added (23/11/2026): Tony's BerryBush (on the berry bush; Tools > World > Build Berry Bushes puts it there)
    // makes E pick the berries without removing the bush (they grow back), and lets the bush be hit and broken (then it
    // gives the berries still on it). Your Update, TakeDamage and Break ask it first; a node without one works exactly
    // as before. CollectRange lets Tony's HUD prompt ("E  Pick berries") use your Collect Range.
    // To undo: delete the lines marked [Tony] (23/11/2026) and put back the lines in their comments.
    // Log: Docs/TonyEditMate/Turki/23-11-2026 Berry Bushes You Pick With E.md
    BerryBush bush;                                                                   // [Tony] added (23/11/2026): Tony's, on the berry bush (null on other nodes)
    public float CollectRange => collectRange;                                        // [Tony] added (23/11/2026)

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        currentHealth = maxHealth;
        bush = GetComponent<BerryBush>();                                             // [Tony] added (23/11/2026)

        // [Tony] added (22/11/2026): only a berry bush (Collect With Key) needs Update below (it watches for the key).
        // Every other node (trees, rocks, ores, deer) switches its Update off here, after its health is set: the new
        // world has about 2000 of them near the player, and each one's empty Update cost time every frame. Hitting it
        // still works (TakeDamage doesn't need Update), so it shows unticked in the Inspector while playing.
        // To undo: delete this line. Log: Docs/TonyEditMate/Turki/22-11-2026 Trees And Rocks Skip Their Empty Update.md
        if (!collectWithKey) enabled = false;                                         // [Tony] added
    }

    // NEW: berries are picked with a key instead of being hit, so the node watches for the key itself.
    void Update()
    {
        if (!collectWithKey) return;                   // normal nodes are mined with attacks
        // if (!Input.GetKeyDown(collectKey)) return;   // [Tony] replaced (21/11/2026): the key from KeyBindings (E)
        if (!Input.GetKeyDown(KeyBindings.Interact)) return;   // [Tony] replaced
        if (MenuState.AnyOpen) return;                 // no collecting while a menu is open

        Camera cam = Camera.main;
        if (cam == null) return;

        // The camera sits at the edge of the player's own sphere, so a normal Raycast hits the player first.
        // RaycastAll returns everything along the ray, and we skip the player's body.
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, collectRange);

        float closest = float.MaxValue;
        bool lookingAtThis = false;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;   // ignore our own body

            if (h.distance < closest)                  // the nearest thing that is not the player
            {
                closest = h.distance;
                lookingAtThis = h.collider.transform == transform || h.collider.transform.IsChildOf(transform);
            }
        }

        // if (lookingAtThis) Break();                    // gives the drop and removes the bush   // [Tony] replaced (23/11/2026)
        if (lookingAtThis) { if (bush != null) bush.Pick(); else Break(); }   // [Tony] replaced: Tony's BerryBush picks the berries, the bush stays
    }

    // ===== My methods =====

    // The player's attack calls this (same name as EnemyCombat.TakeDamage).
    public void TakeDamage(float damage)
    {
        // if (collectWithKey) return;   // NEW: berries are picked with a key, not hit   // [Tony] replaced (23/11/2026)
        if (collectWithKey && bush == null) return;   // [Tony] replaced: a bush with Tony's BerryBush can be hit and broken

        currentHealth -= damage;
        Debug.Log(name + " took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0f) Break();
    }

    // Gives the drop to the player, then removes the node, the drops go straight to the inventory.
    void Break()
    {
        if (bush != null) { bush.GiveLeft(); Destroy(gameObject); return; }          // [Tony] added (23/11/2026): a broken berry bush gives only the berries still on it
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || dropItem == null)
        {
            Debug.LogWarning(name + " broke but has no Drop Item or there is no PlayerInventory in the scene.");
        }
        else
        {
            Give(inv, dropItem, dropMin, dropMax);   // the normal drop, always

            // [Tony] added (21/11/2026): the rare box's Lumberjack perk (+1 wood per tree) and Prospector perk (+1 per
            // rock), from Tony's PerkEffects (0 without them; berries picked with the key get nothing extra).
            // To undo: delete the 2 lines marked [Tony]. Log: Docs/TonyEditMate/Turki/21-11-2026 Lumberjack And Prospector Perks.md
            int extra = PerkEffects.ExtraDrop(resourceType, collectWithKey);          // [Tony] added
            if (extra > 0) Give(inv, dropItem, extra, extra);                         // [Tony] added

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