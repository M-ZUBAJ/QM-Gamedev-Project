/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory and PlayerHealth). Tools > Items > Build Plate Armor puts it there. If the
        Player has none (e.g. GameScene wasn't saved after that tool), one is added to it the first time it's needed.
Unity Editor: Worn = what the player wears now (one per slot), for checking while playing; dragging an armor's Item Data
              in there while playing puts it on at once (without taking it from the inventory).
              Set Bonuses = what wearing a whole set gives (Iron: stamina comes back 50 % faster).
              Testing: Play, get a piece of armor (craft it, or tick GameRules > Testing > Tool Test), Tab > Gear, click a
              slot and pick it. Then PlayerHealth's ⋮ > Test: Take Damage takes less off. With all six iron pieces on,
              sprint until tired: the stamina bar refills faster, and the Info tab shows the bonus in gold.
Purpose: The armor the player wears: one piece per slot (Helmet, Shoulders, Chest, Gloves, Legs, Boots).
         Putting a piece on takes it out of the inventory, and the piece worn there before goes back in; taking a piece
         off puts it back in the inventory (only if there's room). A piece held on the cursor (picked up on the Inventory
         page) can be put straight on by clicking its slot: the old piece then goes onto the cursor, like Minecraft.
         It protects: every worn piece's Protection is added up, in percent (the iron set: 1.5 % a piece, 2.5 % for the
         chestplate, so 10 % for all six), up to Max Protection. PlayerHealth asks it about every hit (TakeDamage), but
         not about starving or bad food (LoseHealth).
         Set bonus: a piece of the same set in every slot (Armor > Set, e.g. Iron) gives that set's bonus from Set
         Bonuses. The iron set makes stamina come back 50 % faster (it tells PlayerStamina).
         Changed tells everyone when the armor changes: ArmorLook shows the pieces on the character (the body, the
         first-person hands and the Gear page's preview) and the Gear page updates its slots.
         When the player dies, ItemDropper takes the armor off with the items (GameRules Keep Inventory off).
Requirement: PlayerArmor.cs x-> PlayerInventory.cs, ItemData.cs, ArmorStats.cs
             PlayerArmor.cs o-> PlayerStamina.cs
             PlayerArmor.cs <-x ArmorBuilder.cs, ArmorLook.cs, ArmorPicker.cs, ArmorSlotUI.cs, GearPage.cs
             PlayerArmor.cs <-o ItemDropper.cs, PlayerHealth.cs, PlayerInfoPage.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerArmor : MonoBehaviour
{
    // What wearing every piece of one armor set gives.
    [System.Serializable]
    public class SetBonus
    {
        [Tooltip("The set's name, as in its pieces' Armor > Set (e.g. Iron).")]
        public string set = "Iron";

        [Tooltip("How much faster stamina comes back while the whole set is worn: 1.5 = 50 % faster, 1 = no change.")]
        [Min(1f)]
        public float staminaRegen = 1.5f;

        // What it does, the way the Gear and Info pages say it (e.g. "stamina recharges 50% faster").
        public string Describe()
        {
            return staminaRegen > 1.001f ? "stamina recharges " + ArmorStats.Percent((staminaRegen - 1f) * 100f) + " faster" : "no bonus";
        }
    }

    // ===== Fields =====
    [Header("Protection")]
    [Tooltip("The most armor can ever take off a hit, in percent (so a player in full armor still gets hurt a little).")]
    [Range(0f, 100f)]
    [SerializeField] float maxProtection = 80f;

    [Header("Set bonus")]
    [Tooltip("What wearing a whole set gives: a piece of that set (Armor > Set) in every slot. Iron: stamina comes back " +
             "50 % faster.")]
    [SerializeField] List<SetBonus> setBonuses = new List<SetBonus> { new SetBonus() };

    [Header("Worn now")]
    [Tooltip("What the player wears, one per slot in this order: Helmet, Shoulders, Chest, Gloves, Legs, Boots. " +
             "Shown here for checking while playing.")]
    [SerializeField] ItemData[] worn = new ItemData[6];

    // Every slot, top to bottom (the order of the ArmorSlot list).
    public static readonly ArmorSlot[] Slots = (ArmorSlot[])System.Enum.GetValues(typeof(ArmorSlot));

    static PlayerArmor current;
    PlayerStamina stamina;   // gets the set bonus (optional: no stamina = no bonus to give)

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

    // Every worn piece's Protection added up, in percent (the full iron set: 10).
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

    // How much of a hit the armor takes off: 0 = nothing, 0.1 = 10 % (the full iron set), never more than Max Protection.
    public float Protection => Mathf.Min(maxProtection, ProtectionPercent) / 100f;

    // A full set = one piece in every slot.
    public int SetSize => Slots.Length;

    // The set every slot is worn from (e.g. "Iron"), or null.
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

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        FitSlots();
        stamina = GetComponent<PlayerStamina>();
        if (stamina == null) stamina = FindFirstObjectByType<PlayerStamina>();
    }

    void OnEnable()
    {
        GiveBonus();
    }

    void OnDisable()
    {
        if (stamina != null) stamina.SetRegenBoost(this, 1f);   // no armor script, no bonus
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

    // The set most of the worn pieces belong to (e.g. "Iron"), and how many of them; null and 0 if no worn piece has a
    // set. The Gear and Info pages show it as "Iron set 4/6".
    public string MostWornSet(out int count)
    {
        string best = null;
        count = 0;
        foreach (ItemData piece in worn)
        {
            string set = SetOf(piece);
            if (set == null || set == best) continue;
            int pieces = PiecesOf(set);
            if (pieces > count)
            {
                best = set;
                count = pieces;
            }
        }
        return best;
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

    // The armor changed: the set bonus first (so everyone who's told sees it), then tell everyone.
    void Announce()
    {
        GiveBonus();
        Changed?.Invoke();
    }

    // Gives PlayerStamina the set bonus's faster refill (or takes it back: 1 = normal).
    void GiveBonus()
    {
        if (stamina == null) return;
        SetBonus bonus = ActiveBonus;
        stamina.SetRegenBoost(this, bonus != null ? bonus.staminaRegen : 1f);
    }

    // How many worn pieces belong to this set.
    int PiecesOf(string set)
    {
        int count = 0;
        foreach (ItemData piece in worn)
        {
            if (string.Equals(SetOf(piece), set, System.StringComparison.OrdinalIgnoreCase)) count++;
        }
        return count;
    }

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
