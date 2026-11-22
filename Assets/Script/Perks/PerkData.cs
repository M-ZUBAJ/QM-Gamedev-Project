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
    Glide,           // [Tony] added: hold Space in the air to float down, tap it twice to dive (21/11/2026: a comma added)
    // [Tony] added (21/11/2026): four more (attack speed for the rare boxes; the rest for the epic and legendary boxes).
    // Log: Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md
    AttackSpeed,     // [Tony] added: attacks come faster
    Vampire,         // [Tony] added: heal a share of the damage you deal
    Thorns,          // [Tony] added: a monster that hits you takes a share of that damage back
    NightPower,      // [Tony] added: more health, stamina, speed and damage at night (21/11/2026: a comma added)
    // [Tony] added (21/11/2026): three more. Reroll (epic and legendary boxes): the REROLL button only works once the
    // player has it. Keen Eye (epic and legendary boxes): the crosshair turns yellow when the attack can reach what it
    // points at. Long Reach (rare boxes, in the new system common boxes too; only after Keen Eye): the attack reaches
    // farther.
    // Log: Docs/TonyEditMate/Marko/21-11-2026 Reroll Perk, Keen Eye And Long Reach.md
    Reroll,          // [Tony] added: unlocks the REROLL button at loot boxes
    KeenEye,         // [Tony] added: the crosshair turns yellow when the attack can reach what it points at
    LongReach,       // [Tony] added: melee attacks reach farther (22/11/2026: a comma added)
    // [Tony] added (22/11/2026): Arrow Recovery (epic and legendary boxes): arrows stay in monsters and animals to be taken
    // back, a dead one drops them, and missed arrows stay on the ground to be picked up (Tony's ArrowProjectile asks
    // PerkEffects.ArrowRecovery). Your PerkManager hands it to Tony's PerkEffects like the others.
    // To undo: delete these lines and the comma after LongReach. Log: Docs/TonyEditMate/Marko/22-11-2026 Arrow Recovery Perk.md
    ArrowRecovery    // [Tony] added: your arrows can be picked up again
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

    [Tooltip("How many of it the player can have at a time: 1 = one (e.g. Last Stand, offered again once it's used up), 0 = no limit.")]   // [Tony] added (21/11/2026: text edited, was "How many times one life can pick it: 1 = once (e.g. Last Stand), 0 = no limit.")
    [Min(0)]                                                                                           // [Tony] added
    public int maxPicks;                                                                              // [Tony] added
}
