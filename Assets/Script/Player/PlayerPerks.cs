/*
First Date: 21/10/2026
Date: 21/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerHealth, PlayerStamina, PlayerController, PlayerFireball).
Unity Editor: Nothing to connect. Perk chests (Marko's PerkChest) tell it which perk the player got.
              Testing: in Play mode, click this component's ⋮ menu > "Test: Add Random Perk", or open a perk chest (E),
              and watch the HUD's perk slots (PerkBar).
Purpose: Remembers which perks the player has, in the order they were gained, and how many times each
         (the same perk twice = "x2"). Also gives each perk a name and its stats right now, for the HUD:
           Max Health  -> "Max Health 144"            (PlayerHealth)
           Max Stamina -> "Max Stamina 150"           (PlayerStamina)
           Move Speed  -> "Walk 5.8 m/s"              (PlayerController)
           Fireball    -> "Q with sword, 2.4 s cooldown" (PlayerFireball)
         When the player dies, every perk is lost (Reset On Death): the list is emptied and the boosts are undone
         (max health, max stamina and walk speed back to their start, the fireball locked again).
         Adding a new perk: a new PerkType line, then a case in PerkName, PerkStats and ResetPerks.
Requirement: PlayerPerks.cs o-> PlayerHealth.cs, PlayerStamina.cs, PlayerController.cs, PlayerFireball.cs
             PlayerPerks.cs <-x HUDBuilder.cs, PerkBar.cs, PerkChest.cs
*/

using System.Collections.Generic;
using UnityEngine;

// Every perk a chest can give. New perks (e.g. more abilities like the fireball) get a new line here.
public enum PerkType
{
    MaxHealth,
    MaxStamina,
    MoveSpeed,
    Fireball,
}

[DisallowMultipleComponent]
public class PlayerPerks : MonoBehaviour
{
    // One owned perk: which one, and how many times the player got it.
    [System.Serializable]
    public class OwnedPerk
    {
        public PerkType type;
        public int stacks;
    }

    // ===== Fields =====
    [Tooltip("The perks the player has, in the order they were gained. Shown here for checking while playing.")]
    [SerializeField] List<OwnedPerk> owned = new List<OwnedPerk>();

    [Tooltip("On: when the player dies, every perk is lost and its boost is undone (like losing your gear). " +
             "Off: perks are kept after dying.")]
    [SerializeField] bool resetOnDeath = true;

    static PlayerPerks instance;   // the one on the player (found by itself)

    PlayerHealth health;           // each can be missing: that perk's stats then show "-"
    PlayerStamina stamina;
    PlayerController controller;
    PlayerFireball fireball;

    // Fired whenever a perk is gained (e.g. the HUD redraws).
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

    // A perk chest calls this after giving a perk: PlayerPerks.Record(PerkType.Fireball);
    // Does nothing if there's no PlayerPerks on the player (the perk itself still works).
    public static void Record(PerkType type)
    {
        if (Current != null) Current.Add(type);
    }

    // Remembers a perk: a new one goes at the end, one the player already has gets one more stack.
    public void Add(PerkType type)
    {
        OwnedPerk perk = owned.Find(p => p.type == type);
        if (perk == null)
        {
            perk = new OwnedPerk { type = type };
            owned.Add(perk);
        }
        perk.stacks++;
        Changed?.Invoke();
    }

    void OnPlayerDied()
    {
        if (resetOnDeath) ResetPerks();
    }

    // Loses every perk: undoes the boosts and empties the list (the HUD's boxes disappear).
    // Runs on death, before respawning, so the player comes back with their starting max health / stamina.
    public void ResetPerks()
    {
        if (health != null) health.ResetMaxHealth();
        if (stamina != null) stamina.ResetMaxStamina();
        if (controller != null) controller.ResetMoveSpeed();
        if (fireball != null) fireball.ResetFireballPerk();

        owned.Clear();
        Changed?.Invoke();
    }

    // The perk's name for the HUD, with "x2" when the player has it more than once.
    public string PerkName(OwnedPerk perk)
    {
        string name;
        switch (perk.type)
        {
            case PerkType.MaxHealth:  name = "Max Health"; break;
            case PerkType.MaxStamina: name = "Max Stamina"; break;
            case PerkType.MoveSpeed:  name = "Move Speed"; break;
            case PerkType.Fireball:   name = "Fireball"; break;
            default:                  name = perk.type.ToString(); break;
        }
        return perk.stacks > 1 ? name + " x" + perk.stacks : name;
    }

    // What the perk does and its value right now, e.g. "Max Health 144". "-" if that script isn't on the player.
    public string PerkStats(OwnedPerk perk)
    {
        switch (perk.type)
        {
            case PerkType.MaxHealth:
                return health != null ? "+20% each\nMax Health " + health.MaxHealth.ToString("F0") : "-";
            case PerkType.MaxStamina:
                return stamina != null ? "+25 each\nMax Stamina " + stamina.MaxStamina.ToString("F0") : "-";
            case PerkType.MoveSpeed:
                return controller != null ? "+15% each\nWalk " + controller.Speed.ToString("F1") + " m/s" : "-";
            case PerkType.Fireball:
                return fireball != null ? "Q with sword\n" + fireball.CurrentCooldown.ToString("F1") + " s cooldown" : "-";
        }
        return "";
    }

    // Click the component's ⋮ menu in the Inspector to run this (Play mode). Only records it for the HUD:
    // the stats don't really change (a real chest does that).
    [ContextMenu("Test: Add Random Perk")]
    void TestAddRandom()
    {
        int count = System.Enum.GetValues(typeof(PerkType)).Length;
        Add((PerkType)Random.Range(0, count));
    }
}
