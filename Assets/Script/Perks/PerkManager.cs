using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PerkManager exists!");
            return;
        }

        Instance = this;
    }

    public void ApplyPerk(PerkData perk)
    {
        if (perk == null)
        {
            Debug.LogWarning("Cannot apply a null perk.");
            return;
        }

        switch (perk.perkType)
        {
            case ChestPerkType.MaxHealth:
                {
                    PlayerHealth health =
                        FindFirstObjectByType<PlayerHealth>();

                    if (health != null)
                        health.IncreaseMaxHealth(perk.value);
                    else
                        Debug.LogWarning("PlayerHealth was not found.");

                    break;
                }

            case ChestPerkType.MaxStamina:
                {
                    PlayerStamina stamina =
                        FindFirstObjectByType<PlayerStamina>();

                    if (stamina != null)
                        stamina.IncreaseMaxStamina(perk.value);
                    else
                        Debug.LogWarning("PlayerStamina was not found.");

                    break;
                }

            case ChestPerkType.MoveSpeed:
                {
                    PlayerController controller =
                        FindFirstObjectByType<PlayerController>();

                    if (controller != null)
                        controller.IncreaseMoveSpeed(perk.value);
                    else
                        Debug.LogWarning("PlayerController was not found.");

                    break;
                }

            case ChestPerkType.Fireball:
                {
                    PlayerFireball fireball =
                        FindFirstObjectByType<PlayerFireball>();

                    if (fireball != null)
                        fireball.AddFireballPerk();
                    else
                        Debug.LogWarning("PlayerFireball was not found.");

                    break;
                }

            // [Tony] added (21/11/2026): the loot boxes' new perks (Second Wind, Critical Strike, Glide...): Tony's
            // PerkEffects on the Player does what they do. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
            default:                                                                      // [Tony] added
                PerkEffects.Apply(perk);                                                  // [Tony] added
                break;                                                                    // [Tony] added
        }

        // [Tony] added (19/11/2026): tell the HUD's perk row (Tony's PlayerPerks, shown by PerkBar) which perk the
        // player got. Only the old random perk in PerkChest did that, so perks picked on the panel never showed.
        // To undo: delete the lines marked [Tony]. Log: Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md
        // [Tony] edited (21/11/2026): records the Perk Data itself (it was PlayerPerks.Record(HudPerk(perk.perkType))),
        // so the HUD shows every perk with its own picture, and the chests know what was picked (Tony's PerkRules).
        PlayerPerks.Record(perk); // [Tony] added

        Debug.Log("Perk selected: " + perk.displayName);
    }

    // [Tony] removed (21/11/2026): Tony's own HudPerk helper (ChestPerkType -> Tony's PerkType). Tony's PlayerPerks
    // uses your ChestPerkType now, so the two lists can't drift apart.
}
