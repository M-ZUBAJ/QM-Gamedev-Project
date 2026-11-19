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
        }

        // [Tony] added (19/11/2026): tell the HUD's perk row (Tony's PlayerPerks, shown by PerkBar) which perk the
        // player got. Only the old random perk in PerkChest did that, so perks picked on the panel never showed.
        // To undo: delete the lines marked [Tony]. Log: Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md
        PlayerPerks.Record(HudPerk(perk.perkType)); // [Tony] added

        Debug.Log("Perk selected: " + perk.displayName);
    }

    // [Tony] added: your chest perk -> the same perk in Tony's HUD list (PerkType has the same four names).
    static PerkType HudPerk(ChestPerkType type)                                       // [Tony] added
    {                                                                                 // [Tony] added
        switch (type)                                                                 // [Tony] added
        {                                                                             // [Tony] added
            case ChestPerkType.MaxHealth:  return PerkType.MaxHealth;                 // [Tony] added
            case ChestPerkType.MaxStamina: return PerkType.MaxStamina;                // [Tony] added
            case ChestPerkType.MoveSpeed:  return PerkType.MoveSpeed;                 // [Tony] added
            default:                       return PerkType.Fireball;                  // [Tony] added
        }                                                                             // [Tony] added
    }                                                                                 // [Tony] added
}
