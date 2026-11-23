/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: A human enemy's top object, next to Marko's EnemyCombat and HumanGear. Tools > Enemies > Build Human Enemies
        puts it there and fills in the four materials.
Unity Editor: Copper, Iron, Gold, Diamond = the items a piece of that tier drops (the tool finds them by Item Id).
              Testing: Play in the new world, select a human and watch Carried grow each time it breaks a tree, a rock
              or a berry bush. Kill it: "+3 Wood", "+1 Iron"... and "+N Coins" show, and it's in your inventory.
Purpose: What a human enemy leaves when the player kills it (the player gets what it had, not the Polytope gear itself,
         which the player's character can't wear):
           - 1 of its tier's material for each armor piece it wore, its weapon and its shield (a diamond helmet,
             chestplate and leggings = 3 diamonds; an iron sword +1 iron), and for each tool it carried (an axe and a
             pickaxe: wooden ones 1 Wood each, stone ones 1 Stone, iron / gold / diamond ones 1 of that);
           - everything it gathered (Carried: what the trees it chopped, the rocks it mined and the bushes it picked
             gave: wood, stone, ore, berries, apples);
           - coins: copper 1, iron 2, gold 3, diamond 4 for each of those pieces, the weapon, the shield and each
             iron / gold / diamond tool (wooden and stone tools give none). One with
             nothing at all to drop (no armor, no weapon, nothing gathered) gives Empty Coins instead (3 - 5).
         On top of that, Marko's EnemyCombat gives the armor set's coins per kill and Bounty Hunter's (MonsterBounty),
         and the end pages count it under "Humans killed". Its own coins from Marko's script are off on these prefabs
         (EnemyCombat > Coin Multiplier 0), so only these count.
         The items go straight into the player's inventory, like chopping a tree ("+3 Wood" messages). An item with no
         room left falls next to the body as a floating stack instead (ItemDropper's look), so nothing is lost.
Requirement: HumanLoot.cs x-> HumanGear.cs, EnemyCombat.cs, DroppedStack.cs, PlayerInventory.cs, ToolStats.cs
             HumanLoot.cs o-> ItemDropper.cs, ItemPickup.cs, PlayerCoins.cs
             HumanLoot.cs <-x HumanBrain.cs, HumanEnemyBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat))]
public class HumanLoot : MonoBehaviour
{
    // Something it gathered while working, and how many.
    [System.Serializable]
    public class Carried
    {
        public ItemData item;
        public int amount;
    }

    // ===== Fields =====
    [Header("What a tier drops (the tool fills these in)")]
    [Tooltip("What a copper piece, weapon or shield drops: 1 each.")]
    [SerializeField] ItemData copper;
    [SerializeField] ItemData iron;
    [SerializeField] ItemData gold;
    [SerializeField] ItemData diamond;
    [Tooltip("What a wooden tool drops (1 each).")]
    [SerializeField] ItemData wood;
    [Tooltip("What a stone tool drops (1 each).")]
    [SerializeField] ItemData stone;

    [Header("Coins")]
    [Tooltip("When it has nothing at all to drop (no armor, no weapon, nothing gathered): a random number of coins in this range.")]
    [SerializeField] Vector2Int emptyCoins = new Vector2Int(3, 5);

    [Header("Dropping")]
    [Range(0f, 8f)]
    [Tooltip("How fast the stacks fly out from the body (meters per second).")]
    [SerializeField] float scatterSpeed = 2.5f;

    [Range(0f, 5f)]
    [Tooltip("Seconds before walking over a stack picks it up (only with the GameRule Auto Pickup Drops), so they're seen first.")]
    [SerializeField] float pickupDelay = 0.8f;

    [Header("Now (while playing)")]
    [Tooltip("What it has gathered so far: it all drops when it dies.")]
    [SerializeField] List<Carried> carried = new List<Carried>();

    EnemyCombat combat;   // Marko's health script: health 0 = dead
    HumanGear gear;       // what it wore (can be missing: then only what it carried, or the empty coins)
    bool seenAlive;       // its health was above 0 once (a prefab saved with 0 health doesn't "die" at once)
    bool dropped;         // dropped its loot already
    static bool quitting; // the game is closing: nothing is dropped then

    // How many things it carries in all.
    public int CarriedCount
    {
        get
        {
            int count = 0;
            foreach (Carried c in carried) count += c.amount;
            return count;
        }
    }

    public IReadOnlyList<Carried> Carrying => carried;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        gear = GetComponent<HumanGear>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    void Update()
    {
        if (dropped || combat == null) return;
        if (combat.currentHealth > 0f) seenAlive = true;
        else if (seenAlive) GiveAll();   // just died (Marko's EnemyCombat removes the body a moment later)
    }

    // A safety net: removed in the same moment it died. Not when the scene closes or the game quits.
    void OnDestroy()
    {
        if (dropped || !seenAlive || quitting || combat == null || combat.currentHealth > 0f) return;
        if (!gameObject.scene.isLoaded) return;
        GiveAll();
    }

    // ===== My methods =====

    // HumanBrain calls this while it works: one more of what it's gathering (wood, stone...).
    public void Carry(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;
        foreach (Carried c in carried)
        {
            if (c.item == item)
            {
                c.amount += amount;
                return;
            }
        }
        carried.Add(new Carried { item = item, amount = amount });
    }

    // Everything it had goes into the player's inventory (what doesn't fit falls next to the body), and the coins too.
    void GiveAll()
    {
        dropped = true;
        List<Carried> drops = new List<Carried>();
        int coins = 0;

        if (gear != null)
        {
            for (int i = 0; i < HumanGear.PieceCount; i++) coins += AddTier(drops, gear.WornTier((HumanGear.Piece)i));
            coins += AddTier(drops, gear.WeaponTier);
            coins += AddTier(drops, gear.ShieldTier);
            coins += AddTool(drops, gear.KitAxeTier);
            coins += AddTool(drops, gear.KitPickaxeTier);
        }
        foreach (Carried c in carried) Add(drops, c.item, c.amount);
        if (drops.Count == 0 && coins == 0) coins = Random.Range(emptyCoins.x, Mathf.Max(emptyCoins.x, emptyCoins.y) + 1);

        PlayerInventory inventory = PlayerInventory.Instance;
        Vector3 center = transform.position + Vector3.up * 0.8f;
        DroppedStack.Look? look = null;
        foreach (Carried drop in drops)
        {
            if (inventory != null && inventory.Add(drop.item.Id, drop.item.DisplayName, drop.item.GetIcon(), drop.amount, drop.item))
                continue;   // in the inventory, with its "+N" message, like gathering
            if (look == null) look = DropLook();
            Throw(drop, center, look.Value);   // no room for it: on the ground next to the body instead
        }
        if (coins > 0 && PlayerCoins.Instance != null) PlayerCoins.Instance.AddCoins(coins);
    }

    // One piece of this tier: 1 of its material (if that item exists) and its coins (copper 1 ... diamond 4).
    int AddTier(List<Carried> drops, HumanGear.Tier tier)
    {
        if (tier == HumanGear.Tier.None) return 0;
        Add(drops, ItemOf(tier), 1);
        return (int)tier;
    }

    // One tool of this tier: 1 Wood / Stone / Iron / Gold / Diamond, and coins for iron 2, gold 3, diamond 4.
    int AddTool(List<Carried> drops, ToolTier tier)
    {
        switch (tier)
        {
            case ToolTier.Wood: Add(drops, wood, 1); return 0;
            case ToolTier.Stone: Add(drops, stone, 1); return 0;
            case ToolTier.Iron: return AddTier(drops, HumanGear.Tier.Iron);
            case ToolTier.Gold: return AddTier(drops, HumanGear.Tier.Gold);
            case ToolTier.Diamond: return AddTier(drops, HumanGear.Tier.Diamond);
            default: return 0;
        }
    }

    ItemData ItemOf(HumanGear.Tier tier)
    {
        switch (tier)
        {
            case HumanGear.Tier.Copper: return copper;
            case HumanGear.Tier.Iron: return iron;
            case HumanGear.Tier.Gold: return gold;
            case HumanGear.Tier.Diamond: return diamond;
            default: return null;
        }
    }

    // Adds to the drop list, one stack per item (3 diamond pieces = one stack of 3).
    static void Add(List<Carried> drops, ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;
        foreach (Carried d in drops)
        {
            if (d.item == item)
            {
                d.amount += amount;
                return;
            }
        }
        drops.Add(new Carried { item = item, amount = amount });
    }

    // One floating stack, thrown out in a random direction (like the player's own death drop).
    void Throw(Carried drop, Vector3 center, DroppedStack.Look look)
    {
        PlayerInventory.Slot stack = new PlayerInventory.Slot
        {
            id = drop.item.Id,
            name = drop.item.DisplayName,
            icon = drop.item.GetIcon(),
            count = drop.amount,
            data = drop.item,
        };
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 flat = new Vector3(dir.x, 0f, dir.y);
        DroppedStack onGround = DroppedStack.Spawn(stack, drop.amount, center + flat * 0.6f, look);
        if (onGround == null) return;
        ItemPickup pickup = onGround.GetComponent<ItemPickup>();
        if (pickup != null) pickup.BlockAutoCollect(pickupDelay);
        Rigidbody body = onGround.GetComponent<Rigidbody>();
        if (body != null) body.velocity = flat * scatterSpeed + Vector3.up * 2.5f;   // out and a little up
    }

    // The player's drop look (ItemDropper), so these stacks look like the player's own. A plain icon look without one.
    static DroppedStack.Look DropLook()
    {
        ItemDropper dropper = FindFirstObjectByType<ItemDropper>();
        if (dropper != null) return dropper.DropLook;
        return new DroppedStack.Look
        {
            style = DropStyle.Icon,
            iconSize = 0.5f,
            bundleSize = 0.4f,
            bundleColor = new Color(0.55f, 0.38f, 0.22f),
            mergeRadius = 1f,
            despawnSeconds = 300f,
        };
    }

    static void OnQuitting() => quitting = true;
}
