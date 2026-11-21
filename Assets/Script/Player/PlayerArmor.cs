/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory and PlayerHealth). Tools > Items > Build Plate Armor puts it there. If the
        Player has none (e.g. GameScene wasn't saved after that tool), one is added to it the first time it's needed.
Unity Editor: Worn = what the player wears now (one per slot), for checking while playing; dragging an armor's Item Data
              in there while playing puts it on at once (without taking it from the inventory).
              Set Bonuses = what wearing a whole set gives (Copper: +1 coin for every monster killed; Iron: +2, and it
              counts as Copper; Gold: +4 and every chest 10 % cheaper, and it counts as Iron; Diamond: +8 and every
              chest 10 % cheaper, and it counts as Gold), and each set's color in menus. Better sets go lower in the
              list.
              Testing: Play, get a piece of armor (craft it, or tick GameRules > Testing > Armor Test), Tab > Gear, click
              a slot and pick it. Then PlayerHealth's ⋮ > Test: Take Damage takes less off. With all six copper pieces
              on, kill a monster: one more coin than before, and the Info tab shows the bonus in gold.
Purpose: The armor the player wears: one piece per slot (Helmet, Shoulders, Chest, Gloves, Legs, Boots).
         Putting a piece on takes it out of the inventory, and the piece worn there before goes back in; taking a piece
         off puts it back in the inventory (only if there's room). A piece held on the cursor (picked up on the Inventory
         page) can be put straight on by clicking its slot: the old piece then goes onto the cursor, like Minecraft.
         It protects: every worn piece's Protection is added up, in percent (the copper set: 1.5 % a piece, 2.5 % for
         the chestplate, so 10 % for all six; the iron set 15 %, the gold set 25 %, the diamond set 40 %), up to Max
         Protection. PlayerHealth
         asks it about every hit (TakeDamage), but not about starving or bad food (LoseHealth).
         Set bonus: a piece of the same set in every slot (Armor > Set, e.g. Copper) gives that set's bonus from Set
         Bonuses: extra coins for every monster killed (Coins Per Kill: the copper set +1, the iron set +2, the gold set
         +4, the diamond set +8; Marko's EnemyCombat gives them through MonsterBounty, which asks CoinsPerKill), and
         cheaper chests (Chest Discount: the gold and the diamond set take 10 % off every loot box price; ChestTier asks
         ChestDiscount). A set can count as a lower one (Counts As): Iron pieces fill Copper's slots too, Gold pieces
         Iron's and Diamond pieces Gold's (and so on down), so upgrading one piece at a time never loses the bonus you
         had; all six of the better set give its bonus.
         Upgrading a worn piece: crafting an Iron (Gold, Diamond) piece can use the Copper (Iron, Gold) piece the player
         wears (Turki's
         CraftingMenu calls LendForUpgrade, then FinishUpgrade): it comes off for the recipe, and the new piece is
         worn in its place.
         A Set Bonuses list saved before 21/11/2026 (stamina bonuses, Iron first then Reinforced Iron) is replaced by
         today's when the game starts (UpdateOldBonuses).
         Changed tells everyone when the armor changes: ArmorLook shows the pieces on the character (the body, the
         first-person hands and the Gear page's preview) and the Gear page updates its slots.
         When the player dies, ItemDropper takes the armor off with the items (GameRules Keep Inventory off).
Requirement: PlayerArmor.cs x-> PlayerInventory.cs, ItemData.cs, ArmorStats.cs
             PlayerArmor.cs <-x ArmorBuilder.cs, ArmorLook.cs, ArmorPicker.cs, ArmorSlotUI.cs, CraftingMenu.cs, CraftingSetPanels.cs, GearPage.cs, LootChestBuilder.cs
             PlayerArmor.cs <-o ArmorTestKit.cs, ChestTier.cs, CraftingRow.cs, ItemDropper.cs, MonsterBounty.cs, PlayerHealth.cs, PlayerInfoPage.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerArmor : MonoBehaviour
{
    // What wearing every piece of one armor set gives, and how the set shows in menus.
    [System.Serializable]
    public class SetBonus
    {
        [Tooltip("The set's name, as in its pieces' Armor > Set (e.g. Iron).")]
        public string set = "Iron";

        [Tooltip("Extra coins for every monster killed while the whole set is worn (on top of the monster's own coins).")]
        [Min(0)]
        public int coinsPerKill = 1;

        [Tooltip("While the whole set is worn, every loot box costs this share less (0.1 = 10 % off: the gold set), its " +
                 "rerolls too. 0 = no discount.")]
        [Range(0f, 0.9f)]
        public float chestDiscount;

        [Tooltip("A lower set this one also counts as (Iron counts as Copper): its pieces fill that set's slots too, so " +
                 "a mix of Copper and Iron pieces still gives the Copper bonus. Empty = none.")]
        public string countsAs = "";

        [Tooltip("The set's color in menus (its panel in the crafting list).")]
        public Color color = IronColor;

        public SetBonus() { }

        public SetBonus(string set, int coinsPerKill, string countsAs, Color color, float chestDiscount = 0f)
        {
            this.set = set;
            this.coinsPerKill = coinsPerKill;
            this.countsAs = countsAs;
            this.color = color;
            this.chestDiscount = chestDiscount;
        }

        // What it does, the way the Gear page and the crafting list say it (e.g. "+1 coin for every monster killed",
        // "+4 coins for every monster killed, 10% off every chest").
        public string Describe()
        {
            List<string> parts = new List<string>();
            if (coinsPerKill > 0) parts.Add("+" + Coins(coinsPerKill) + " for every monster killed");
            if (chestDiscount > 0f) parts.Add(Percent(chestDiscount) + " off every chest");
            return parts.Count > 0 ? string.Join(", ", parts) : "no bonus";
        }

        // The same, short (e.g. "+1 coin per kill", "+4 coins per kill, chests -10%"), for a line with little room.
        public string Short()
        {
            List<string> parts = new List<string>();
            if (coinsPerKill > 0) parts.Add("+" + Coins(coinsPerKill) + " per kill");
            if (chestDiscount > 0f) parts.Add("chests -" + Percent(chestDiscount));
            return parts.Count > 0 ? string.Join(", ", parts) : "no bonus";
        }

        static string Coins(int count) => count + (count == 1 ? " coin" : " coins");
        static string Percent(float share) => Mathf.RoundToInt(share * 100f) + "%";   // 0.1 -> "10%"
    }

    // The sets' colors in menus.
    public static readonly Color CopperColor = new Color(0.85f, 0.5f, 0.27f);        // copper
    public static readonly Color IronColor = new Color(0.56f, 0.6f, 0.67f);          // steel grey
    public static readonly Color GoldColor = new Color(0.95f, 0.75f, 0.2f);          // gold
    public static readonly Color DiamondColor = new Color(0.35f, 0.85f, 1f);         // ice blue

    // The sets Tools > Items > Build Plate Armor makes, weakest first: Copper (+1 coin per monster killed), Iron (each
    // piece a Copper piece + iron: +2 coins, and it counts as Copper), Gold (each piece an Iron piece + gold: +4 coins
    // and 10 % off every chest, and it counts as Iron) and Diamond (each piece a Gold piece + diamonds: +8 coins and
    // 10 % off every chest, and it counts as Gold; 21/11/2026). A list saved before a set existed gets the missing ones
    // added when the game starts, so a new set always has its bonus.
    public static List<SetBonus> DefaultBonuses() => new List<SetBonus>
    {
        new SetBonus("Copper", 1, "", CopperColor),
        new SetBonus("Iron", 2, "Copper", IronColor),
        new SetBonus("Gold", 4, "Iron", GoldColor, 0.1f),
        new SetBonus("Diamond", 8, "Gold", DiamondColor, 0.1f),
    };

    // Which version of the set bonuses a saved list has: 2 = coins per kill (21/11/2026). Older lists (stamina bonuses)
    // are replaced by DefaultBonuses when the game starts.
    const int BonusVersion = 2;

    // ===== Fields =====
    [Header("Protection")]
    [Tooltip("The most armor can ever take off a hit, in percent (so a player in full armor still gets hurt a little).")]
    [Range(0f, 100f)]
    [SerializeField] float maxProtection = 80f;

    [Header("Set bonus")]
    [Tooltip("What wearing a whole set gives: a piece of that set (Armor > Set) in every slot. Copper: +1 coin for every " +
             "monster killed; Iron: +2 (and its pieces count as Copper); Gold: +4 and 10 % off every chest (and its " +
             "pieces count as Iron); Diamond: +8 and 10 % off every chest (and its pieces count as Gold). Better sets go " +
             "lower in the list.")]
    [SerializeField] List<SetBonus> setBonuses = DefaultBonuses();

    [HideInInspector]
    [SerializeField] int bonusVersion;   // the version of Set Bonuses (0 = saved before versions existed)

    [Header("Worn now")]
    [Tooltip("What the player wears, one per slot in this order: Helmet, Shoulders, Chest, Gloves, Legs, Boots. " +
             "Shown here for checking while playing.")]
    [SerializeField] ItemData[] worn = new ItemData[6];

    // Every slot, top to bottom (the order of the ArmorSlot list).
    public static readonly ArmorSlot[] Slots = (ArmorSlot[])System.Enum.GetValues(typeof(ArmorSlot));

    static PlayerArmor current;
    ItemData lent;           // a worn piece taken off for an upgrade's recipe (LendForUpgrade), until FinishUpgrade
    const int MostSteps = 8; // how far a Counts As chain is followed (so a typo'd circle can't loop forever)

    // The player's armor. If the Player has none yet (e.g. the scene wasn't saved after Tools > Items > Build Plate Armor,
    // or the title screen opened the saved scene), one is added to the Player (the object with PlayerInventory) the
    // first time something asks. Null only when there's no player at all.
    public static PlayerArmor Current
    {
        get
        {
            if (current != null) return current;
            current = FindFirstObjectByType<PlayerArmor>();
            if (current == null && PlayerInventory.Instance != null)
                current = PlayerInventory.Instance.gameObject.AddComponent<PlayerArmor>();
            return current;
        }
    }

    public static event System.Action Changed;   // a piece was put on or taken off

    // Every worn piece's Protection added up, in percent (the full copper set: 10, iron: 15, gold: 25).
    public float ProtectionPercent
    {
        get
        {
            float total = 0f;
            foreach (ItemData piece in worn)
            {
                if (piece != null && piece.IsArmor) total += piece.Armor.protection;
            }
            return total;
        }
    }

    // How much of a hit the armor takes off: 0 = nothing, 0.1 = 10 % (the full copper set), never more than Max Protection.
    public float Protection => Mathf.Min(maxProtection, ProtectionPercent) / 100f;

    // A full set = one piece in every slot.
    public int SetSize => Slots.Length;

    // The set every slot counts toward (e.g. "Copper", also for a mix of Copper and Iron pieces), or null. With all six
    // Iron it's Iron (both sets are full then: the one with more pieces of its own wins).
    public string FullSet
    {
        get
        {
            string set = MostWornSet(out int count);
            return count == SetSize ? set : null;
        }
    }

    // The bonus given now (the whole set is worn and Set Bonuses has it), or null.
    public SetBonus ActiveBonus => BonusFor(FullSet);

    // Extra coins for every monster killed right now (the active set bonus; 0 without one). MonsterBounty asks it.
    public int CoinsPerKill => ActiveBonus != null ? ActiveBonus.coinsPerKill : 0;

    // How much cheaper every loot box is right now: the active set bonus's Chest Discount (the whole gold set: 0.1 =
    // 10 % off), 0 without one. ChestTier asks it for every price.
    public float ChestDiscount => ActiveBonus != null ? Mathf.Clamp(ActiveBonus.chestDiscount, 0f, 0.9f) : 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        FitSlots();
        UpdateOldBonuses();     // a list saved before the coin bonuses
        AddMissingBonuses();
    }

    void OnDestroy()
    {
        if (current != this) return;
        current = null;    // static values outlive the scene, so clear them
        Changed = null;
    }

    // Unity calls this when a value is changed in the Inspector: an armor dragged into Worn while playing shows at once.
    void OnValidate()
    {
        FitSlots();
        if (Application.isPlaying) Announce();
    }

    // ===== My methods =====

    // The piece worn in a slot (null = nothing there).
    public ItemData WornIn(ArmorSlot slot)
    {
        int i = (int)slot;
        return i >= 0 && i < worn.Length ? worn[i] : null;
    }

    // A hit of "damage" after the armor took its share off (PlayerHealth.TakeDamage asks this).
    public float Reduce(float damage)
    {
        return damage * (1f - Protection);
    }

    // The set most of the worn pieces count toward, and how many: 2 Copper + 2 Iron pieces = "Copper", 4 (an Iron
    // piece counts as Copper). A tie goes to the set with more pieces of its own (3 Iron pieces alone = "Iron", 3).
    // Null and 0 if no worn piece has a set. The Gear and Info pages show it as "Copper set 4/6".
    public string MostWornSet(out int count)
    {
        string best = null;
        int bestOwn = 0;
        count = 0;
        foreach (string set in WornSets())
        {
            int pieces = PiecesFor(set), own = WornOf(set);
            if (pieces > count || (pieces == count && own > bestOwn))
            {
                best = set;
                count = pieces;
                bestOwn = own;
            }
        }
        return best;
    }

    // A better set than the one whose bonus is on, partly worn (e.g. 3 Iron pieces in a mix that gives the Copper
    // bonus), and how many of its pieces are worn; null and 0 if there's none. The Gear page shows it as the next goal.
    public string BetterSetInProgress(out int count)
    {
        count = 0;
        string full = FullSet;
        if (full == null) return null;
        string best = null;
        foreach (string set in WornSets())
        {
            if (Same(set, full) || !CountsToward(set, full)) continue;   // only sets that count as the full one: better ones
            int pieces = PiecesFor(set);
            if (pieces < SetSize && pieces > count)
            {
                best = set;
                count = pieces;
            }
        }
        return best;
    }

    // How many worn pieces count toward this set: its own pieces and pieces of sets that count as it (an Iron piece
    // counts toward Copper too).
    public int PiecesFor(string set)
    {
        int count = 0;
        foreach (ItemData piece in worn)
        {
            if (CountsToward(SetOf(piece), set)) count++;
        }
        return count;
    }

    // How many worn pieces are of exactly this set.
    public int WornOf(string set)
    {
        int count = 0;
        foreach (ItemData piece in worn)
        {
            if (Same(SetOf(piece), set)) count++;
        }
        return count;
    }

    // Does a piece of set "own" count toward set "target"? Its own set does, and so do the sets it counts as
    // (Iron -> Copper), and theirs.
    public bool CountsToward(string own, string target)
    {
        for (int step = 0; step < MostSteps && !string.IsNullOrEmpty(own); step++)
        {
            if (Same(own, target)) return true;
            SetBonus bonus = BonusFor(own);
            own = bonus != null ? bonus.countsAs : null;
        }
        return false;
    }

    // Is a piece with this Item Id worn now? (The crafting list counts a worn piece toward an armor upgrade's recipe.)
    public bool IsWorn(string id)
    {
        foreach (ItemData piece in worn)
        {
            if (piece != null && piece.Id == id) return true;
        }
        return false;
    }

    // Crafting an upgrade of a worn piece (e.g. Copper Helmet + 4 Iron = Iron Helmet while the Copper Helmet is worn):
    // takes the worn piece off into the inventory, so the recipe can use it. Only when the inventory has none of
    // it (a spare one there is used instead). Turki's CraftingMenu calls this before it checks the recipe, and
    // FinishUpgrade after. True = taken off.
    public bool LendForUpgrade(ItemData upgrade)
    {
        lent = null;
        ItemData old = upgrade != null && upgrade.IsArmor ? upgrade.UpgradedFrom : null;
        PlayerInventory inventory = PlayerInventory.Instance;
        if (old == null || !old.IsArmor || inventory == null || inventory.Count(old.Id) > 0) return false;

        ItemData wornThere = WornIn(old.Armor.slot);
        if (wornThere == null || wornThere.Id != old.Id) return false;   // not wearing it
        if (!TakeOff(old.Armor.slot)) return false;                      // no room in the inventory for it
        lent = old;
        return true;
    }

    // After crafting (only if LendForUpgrade took a piece off): crafted = the new piece is worn where the old one was;
    // not crafted (something was missing, or the inventory was full) = the old piece goes back on.
    public void FinishUpgrade(ItemData made, bool crafted)
    {
        if (lent == null) return;
        ItemData old = lent;
        lent = null;
        if (crafted && made != null && made.IsArmor && Equip(made)) return;
        Equip(old);
    }

    // The bonus for wearing the whole of this set (null = none in Set Bonuses, or no set).
    public SetBonus BonusFor(string set)
    {
        if (string.IsNullOrEmpty(set) || setBonuses == null) return null;
        foreach (SetBonus bonus in setBonuses)
        {
            if (bonus != null && string.Equals(bonus.set, set, System.StringComparison.OrdinalIgnoreCase)) return bonus;
        }
        return null;
    }

    // Puts a piece of armor on: takes one out of the inventory, and puts the piece worn in that slot before back in.
    // False = it can't: not armor, the player doesn't have it, or no room in the inventory for the old piece.
    public bool Equip(ItemData item)
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (item == null || !item.IsArmor || inventory == null) return false;

        int i = (int)item.Armor.slot;
        PlayerInventory.Slot taken = inventory.TakeOne(item.Id);   // inventory slots first, then the hotbar
        if (taken == null) return false;                           // the player hasn't got one

        ItemData old = worn[i];
        if (old != null && !inventory.PutBack(old.Id, old.DisplayName, old.GetIcon(), 1, old))
        {
            inventory.PutBack(taken.id, taken.name, taken.icon, 1, taken.data);   // no room for the old piece: undo
            return false;
        }

        worn[i] = item;
        Announce();
        return true;
    }

    // Puts on the armor held on the cursor (the stack picked up on the Inventory page), like Minecraft: one piece comes
    // off the cursor, and the piece worn there before goes onto the cursor (or back into the inventory, if the cursor
    // still holds more). False = the cursor holds nothing, or something that doesn't go in this slot, or there was no
    // room for the old piece.
    public bool EquipFromCursor(ArmorSlot slot)
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null || inventory.Held.IsEmpty) return false;
        ItemData item = inventory.Held.data;
        if (item == null || !item.IsArmor || item.Armor.slot != slot) return false;

        int i = (int)slot;
        ItemData old = worn[i];
        bool lastOne = inventory.Held.count == 1;
        inventory.RemoveFromHeld(1);   // one comes off the cursor
        if (old != null)
        {
            bool placed = lastOne
                ? inventory.PutOnCursor(old.Id, old.DisplayName, old.GetIcon(), 1, old)   // a swap: the old piece is held now
                : inventory.PutBack(old.Id, old.DisplayName, old.GetIcon(), 1, old);
            if (!placed)
            {
                inventory.PutOnCursor(item.Id, item.DisplayName, item.GetIcon(), 1, item);   // no room: undo
                return false;
            }
        }

        worn[i] = item;
        Announce();
        return true;
    }

    // Takes off what's worn in a slot and puts it back in the inventory. False = no room (it stays on).
    public bool TakeOff(ArmorSlot slot)
    {
        int i = (int)slot;
        ItemData old = WornIn(slot);
        if (old == null) return true;   // nothing there

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null || !inventory.PutBack(old.Id, old.DisplayName, old.GetIcon(), 1, old)) return false;

        worn[i] = null;
        Announce();
        return true;
    }

    // Takes everything off at once and hands it over as stacks, NOT into the inventory (ItemDropper drops them around
    // the body when the player dies, or throws them away with GameRules Clear Item On Dead).
    public List<PlayerInventory.Slot> TakeAllOff()
    {
        List<PlayerInventory.Slot> pieces = new List<PlayerInventory.Slot>();
        for (int i = 0; i < worn.Length; i++)
        {
            ItemData piece = worn[i];
            if (piece == null) continue;
            pieces.Add(new PlayerInventory.Slot { id = piece.Id, name = piece.DisplayName, icon = piece.GetIcon(), count = 1, data = piece });
            worn[i] = null;
        }
        if (pieces.Count > 0) Announce();
        return pieces;
    }

    // The armor in the inventory (and hotbar) that fits a slot: each kind once, with how many there are.
    public List<(ItemData item, int count)> InInventory(ArmorSlot slot)
    {
        List<(ItemData, int)> found = new List<(ItemData, int)>();
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null) return found;

        foreach (PlayerInventory.Slot stack in inventory.Slots)
        {
            ItemData item = stack.data;
            if (stack.IsEmpty || item == null || !item.IsArmor || item.Armor.slot != slot) continue;
            if (found.Exists(entry => entry.Item1 == item)) continue;   // counted already
            found.Add((item, inventory.Count(item.Id)));
        }
        return found;
    }

    // The armor changed: tell everyone.
    void Announce()
    {
        Changed?.Invoke();
    }

    // Every set the worn pieces count toward: their own sets and the sets those count as.
    List<string> WornSets()
    {
        List<string> sets = new List<string>();
        foreach (ItemData piece in worn)
        {
            string set = SetOf(piece);
            for (int step = 0; step < MostSteps && !string.IsNullOrEmpty(set); step++)
            {
                string name = set;
                if (!sets.Exists(known => Same(known, name))) sets.Add(name);
                SetBonus bonus = BonusFor(set);
                set = bonus != null ? bonus.countsAs : null;
            }
        }
        return sets;
    }

    // A list saved before 21/11/2026 (its bonuses made stamina come back faster, and before 20/11/2026 Iron was the first
    // set, then Reinforced Iron) is replaced by today's DefaultBonuses: Copper +1 and Iron +2 coins per kill. A set you
    // added by hand to such an old list would need adding again. The build tool (Tools > Items > Build Plate Armor) calls
    // it on the scene's Player too. True = the list was replaced.
    public bool UpdateOldBonuses()
    {
        if (bonusVersion >= BonusVersion) return false;
        setBonuses = DefaultBonuses();
        bonusVersion = BonusVersion;
        return true;
    }

    // Sets from DefaultBonuses the list hasn't got (it was saved before they existed) are added at its end.
    void AddMissingBonuses()
    {
        if (setBonuses == null) setBonuses = new List<SetBonus>();
        foreach (SetBonus bonus in DefaultBonuses())
        {
            if (BonusFor(bonus.set) == null) setBonuses.Add(bonus);
        }
    }

    static bool Same(string a, string b) => string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

    // The set a piece belongs to, or null (no piece, not armor, or no set).
    static string SetOf(ItemData piece)
    {
        if (piece == null || !piece.IsArmor || string.IsNullOrEmpty(piece.Armor.set)) return null;
        return piece.Armor.set;
    }

    // Exactly one place per slot (an older list, or a new slot added to ArmorSlot, gets fixed here).
    void FitSlots()
    {
        if (worn == null) worn = new ItemData[Slots.Length];
        if (worn.Length != Slots.Length) System.Array.Resize(ref worn, Slots.Length);
    }
}
