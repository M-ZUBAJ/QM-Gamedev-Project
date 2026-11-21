using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public enum ChestPerkType
{
    MaxHealth,
    MaxStamina,
    MoveSpeed,
    Fireball,        // [Tony] edited: a comma added, so the new kinds below can follow

    // [Tony] added (21/11/2026): the new perks of the common, rare and epic loot boxes. Tony's PerkEffects does what
    // they do (your PerkManager hands them over). They go at the end, so the Perk Data files keep their kinds.
    // To undo: delete the lines marked [Tony]. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
    SecondWind,      // [Tony] added: stamina recharges faster
    CriticalStrike,  // [Tony] added: a chance for a hit to do double damage
    SpringLegs,      // [Tony] added: jump higher
    BountyHunter,    // [Tony] added: more coins for every monster killed
    Gourmet,         // [Tony] added: food does more
    Lumberjack,      // [Tony] added: more wood from every tree
    Prospector,      // [Tony] added: more stone or ore from every rock
    Sprinter,        // [Tony] added: sprint faster, using less stamina
    Brawler,         // [Tony] added: punches knock monsters back farther
    LastStand,       // [Tony] added: survive one killing blow each life
    Glide            // [Tony] added: hold Space in the air to float down, tap it twice to dive
}

[CreateAssetMenu(
    fileName = "NewPerk",
    menuName = "Game/Perk Data"
)]
public class PerkData : ScriptableObject
{
    [Header("Display Information")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Perk Effect")]
    public ChestPerkType perkType;

    [Min(0f)]
    public float value;

    // [Tony] added (21/11/2026): when a chest may offer this perk (Tony's PerkRules checks it). Left empty / 0, a perk
    // is always offered, like before. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
    [Header("Offered when ([Tony])")]                                                                  // [Tony] added
    [Tooltip("Only offered once the player has picked this perk in this life (e.g. Spring Legs needs Glide). Empty = always.")]   // [Tony] added
    public PerkData requires;                                                                         // [Tony] added

    [Tooltip("How many times one life can pick it: 1 = once (e.g. Last Stand), 0 = no limit.")]      // [Tony] added
    [Min(0)]                                                                                           // [Tony] added
    public int maxPicks;                                                                              // [Tony] added
}
