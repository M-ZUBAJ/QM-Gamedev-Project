/*
First Date: 21/10/2026
Date: 21/11/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerHealth, PlayerStamina, PlayerController, PlayerFireball).
Unity Editor: Nothing to connect. Perk chests (Marko's PerkManager) tell it which perk the player got.
              Icons = each perk kind's picture: Tools > UI > Build HUD fills it with the drawn ones (Assets/UI/Perks).
              A kind without a picture there shows its Perk Data's icon.
              Testing: in Play mode, click this component's ⋮ menu > "Test: Add Random Perk", or open a perk chest (E),
              and watch the HUD's perk slots (PerkBar).
Purpose: Remembers which perks the player has, in the order they were gained, and how many times each kind
         (the same kind twice = "x2"). Also gives each perk a name and its stats right now, for the HUD:
           Max Health  -> "Max Health 144"            (PlayerHealth)
           Max Stamina -> "Max Stamina 150"           (PlayerStamina)
           Move Speed  -> "Walk 5.8 m/s"              (PlayerController)
           Fireball    -> "Q with sword, 2.4 s cooldown" (PlayerFireball)
           the loot boxes' new perks (Second Wind, Critical Strike, Glide...) -> PerkEffects.Describe
         The kinds are Marko's ChestPerkType (PerkData.cs), the same list his Perk Data use.
         It also remembers each Perk Data picked (TimesPicked), so a chest knows what may be offered (PerkRules: a perk
         that needs another one first, a perk the player may have only one of at a time).
         Lose(kind) takes one away again: Last Stand once it has saved the player (PerkEffects) leaves the HUD, and a
         loot box may offer it again.
         When the player dies, every perk is lost (Reset On Death): the list is emptied and the boosts are undone
         (max health, max stamina and walk speed back to their start, the fireball locked again, the new perks undone by
         PerkEffects), and the loot boxes' prices start again (ChestTier).
         Each perk's picture (Icons, IconOf) is shown by the HUD's perk icons (GameRules > Perk Icons) and the
         inventory's Info tab.
         Adding a new perk kind: a new ChestPerkType line (Marko's PerkData.cs), what it does in PerkEffects (Add,
         Describe, ResetEffects), its name in NameOf and its picture (IconArt.ForPerk).
Requirement: PlayerPerks.cs x-> PerkData.cs, ChestTier.cs
             PlayerPerks.cs o-> PlayerHealth.cs, PlayerStamina.cs, PlayerController.cs, PlayerFireball.cs, PerkEffects.cs
             PlayerPerks.cs <-x HUDBuilder.cs, IconArt.cs, InventoryUIBuilder.cs, PerkBar.cs, PerkChest.cs, PerkEffects.cs, PerkManager.cs, PerkRules.cs
             PlayerPerks.cs <-o PlayerInfoPage.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPerks : MonoBehaviour
{
    // One owned perk kind: which one, how many times the player got it, and a picture (the first Perk Data's icon).
    [System.Serializable]
    public class OwnedPerk
    {
        public ChestPerkType type;
        public int stacks;
        public Sprite icon;
    }

    // A perk kind's picture.
    [System.Serializable]
    public class PerkIcon
    {
        public ChestPerkType type;
        public Sprite icon;
    }

    // ===== Fields =====
    [Tooltip("The perks the player has, in the order they were gained. Shown here for checking while playing.")]
    [SerializeField] List<OwnedPerk> owned = new List<OwnedPerk>();

    [Tooltip("On: when the player dies, every perk is lost and its boost is undone (like losing your gear). " +
             "Off: perks are kept after dying.")]
    [SerializeField] bool resetOnDeath = true;

    [Tooltip("Each perk kind's picture, shown by the HUD's perk icons (GameRules > Perk Icons) and the inventory's Info " +
             "tab. Tools > UI > Build HUD fills it with the drawn ones (Assets/UI/Perks). A kind not here shows its Perk " +
             "Data's icon.")]
    [SerializeField] List<PerkIcon> icons = new List<PerkIcon>();

    static PlayerPerks instance;   // the one on the player (found by itself)

    PlayerHealth health;           // each can be missing: that perk's stats then show "-"
    PlayerStamina stamina;
    PlayerController controller;
    PlayerFireball fireball;
    readonly Dictionary<PerkData, int> picked = new Dictionary<PerkData, int>();   // each Perk Data picked in this life, and how often

    // Fired whenever a perk is gained or lost (e.g. the HUD redraws).
    public event System.Action Changed;

    public IReadOnlyList<OwnedPerk> Owned => owned;

    // The one in the scene, or null (found by itself the first time).
    public static PlayerPerks Current
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<PlayerPerks>();
            return instance;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
        health = GetComponent<PlayerHealth>();
        stamina = GetComponent<PlayerStamina>();
        controller = GetComponent<PlayerController>();
        fireball = GetComponent<PlayerFireball>();
    }

    void OnEnable()
    {
        if (health != null) health.Died += OnPlayerDied;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnPlayerDied;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear it
    }

    // ===== My methods =====

    // Marko's PerkManager calls this after giving a perk: remembers its kind (for the HUD) and the Perk Data itself
    // (for the chests' rules). Does nothing if there's no PlayerPerks on the player (the perk itself still works).
    public static void Record(PerkData perk)
    {
        if (perk != null && Current != null) Current.Add(perk);
    }

    // The same with only a kind (Marko's older random chest perk, GrantRandomPerk).
    public static void Record(ChestPerkType type)
    {
        if (Current != null) Current.Add(type, null);
    }

    // How many times this Perk Data was picked in this life (0 = never, or no PlayerPerks).
    public static int TimesPicked(PerkData perk)
    {
        if (perk == null || Current == null) return 0;
        return Current.picked.TryGetValue(perk, out int count) ? count : 0;
    }

    // Takes one perk of this kind away again (Last Stand once it has saved the player: PerkEffects calls it): one stack
    // less (the HUD drops it at 0) and one pick less of its Perk Data, so a loot box may offer it again.
    public static void Lose(ChestPerkType type)
    {
        if (Current != null) Current.Remove(type);
    }

    public void Remove(ChestPerkType type)
    {
        OwnedPerk perk = owned.Find(p => p.type == type);
        if (perk != null)
        {
            perk.stacks--;
            if (perk.stacks <= 0) owned.Remove(perk);
        }

        PerkData data = null;   // a Perk Data of this kind that was picked
        foreach (KeyValuePair<PerkData, int> entry in picked)
        {
            if (entry.Key != null && entry.Key.perkType == type && entry.Value > 0) { data = entry.Key; break; }
        }
        if (data != null)
        {
            if (picked[data] <= 1) picked.Remove(data);
            else picked[data]--;
        }
        Changed?.Invoke();
    }

    public void Add(PerkData perk)
    {
        if (perk == null) return;
        picked.TryGetValue(perk, out int count);
        picked[perk] = count + 1;
        Add(perk.perkType, perk.icon);
    }

    // Remembers a perk kind: a new one goes at the end, one the player already has gets one more stack.
    public void Add(ChestPerkType type, Sprite icon = null)
    {
        OwnedPerk perk = owned.Find(p => p.type == type);
        if (perk == null)
        {
            perk = new OwnedPerk { type = type };
            owned.Add(perk);
        }
        if (perk.icon == null) perk.icon = icon;
        perk.stacks++;
        Changed?.Invoke();
    }

    void OnPlayerDied()
    {
        if (resetOnDeath) ResetPerks();
    }

    // Loses every perk: undoes the boosts and empties the list (the HUD's boxes disappear), and the loot boxes' prices
    // start again. Runs on death, before respawning, so the player comes back with their starting max health / stamina.
    public void ResetPerks()
    {
        if (health != null) health.ResetMaxHealth();
        if (stamina != null) stamina.ResetMaxStamina();
        if (controller != null) controller.ResetMoveSpeed();
        if (fireball != null) fireball.ResetFireballPerk();
        PerkEffects effects = GetComponent<PerkEffects>();
        if (effects != null) effects.ResetEffects();
        ChestTier.ResetPrices();

        owned.Clear();
        picked.Clear();
        Changed?.Invoke();
    }

    // The perk's name for the HUD, with "x2" when the player has it more than once.
    public string PerkName(OwnedPerk perk)
    {
        string name = NameOf(perk.type);
        return perk.stacks > 1 ? name + " x" + perk.stacks : name;
    }

    // A perk kind's name, e.g. "Max Health" (the HUD's icon tiles show it for a perk that has no picture).
    public static string NameOf(ChestPerkType type)
    {
        switch (type)
        {
            case ChestPerkType.MaxHealth:      return "Max Health";
            case ChestPerkType.MaxStamina:     return "Max Stamina";
            case ChestPerkType.MoveSpeed:      return "Move Speed";
            case ChestPerkType.Fireball:       return "Fireball";
            case ChestPerkType.SecondWind:     return "Second Wind";
            case ChestPerkType.CriticalStrike: return "Critical Strike";
            case ChestPerkType.SpringLegs:     return "Spring Legs";
            case ChestPerkType.BountyHunter:   return "Bounty Hunter";
            case ChestPerkType.Gourmet:        return "Gourmet";
            case ChestPerkType.Lumberjack:     return "Lumberjack";
            case ChestPerkType.Prospector:     return "Prospector";
            case ChestPerkType.Sprinter:       return "Sprinter";
            case ChestPerkType.Brawler:        return "Brawler";
            case ChestPerkType.LastStand:      return "Last Stand";
            case ChestPerkType.Glide:          return "Glide";
            case ChestPerkType.AttackSpeed:    return "Attack Speed";
            case ChestPerkType.Vampire:        return "Vampire";
            case ChestPerkType.Thorns:         return "Thorns";
            case ChestPerkType.NightPower:     return "Night Power";
            case ChestPerkType.Reroll:         return "Reroll";
            case ChestPerkType.KeenEye:        return "Keen Eye";
            case ChestPerkType.LongReach:      return "Long Reach";
            default:                           return type.ToString();
        }
    }

    // A perk kind's picture: from Icons, else the Perk Data's icon it was gained with, else null.
    public Sprite IconOf(ChestPerkType type)
    {
        foreach (PerkIcon entry in icons)
        {
            if (entry != null && entry.type == type && entry.icon != null) return entry.icon;
        }
        OwnedPerk perk = owned.Find(p => p.type == type);
        return perk != null ? perk.icon : null;
    }

    // What the perk does and its value right now, e.g. "Max Health 144". "-" if that script isn't on the player.
    public string PerkStats(OwnedPerk perk)
    {
        switch (perk.type)
        {
            case ChestPerkType.MaxHealth:
                return health != null ? "+20% each\nMax Health " + health.MaxHealth.ToString("F0") : "-";
            case ChestPerkType.MaxStamina:
                return stamina != null ? "+25 each\nMax Stamina " + stamina.MaxStamina.ToString("F0") : "-";
            case ChestPerkType.MoveSpeed:
                return controller != null ? "+15% each\nWalk " + controller.Speed.ToString("F1") + " m/s" : "-";
            case ChestPerkType.Fireball:
                return fireball != null ? "Q with sword\n" + fireball.CurrentCooldown.ToString("F1") + " s cooldown" : "-";
            default:
                PerkEffects effects = GetComponent<PerkEffects>();
                return effects != null ? effects.Describe(perk.type) : "-";
        }
    }

    // Click the component's ⋮ menu in the Inspector to run this (Play mode). Only records it for the HUD:
    // the stats don't really change (a real chest does that).
    [ContextMenu("Test: Add Random Perk")]
    void TestAddRandom()
    {
        int count = System.Enum.GetValues(typeof(ChestPerkType)).Length;
        Add((ChestPerkType)Random.Range(0, count));
    }
}
