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

        Debug.Log("Perk selected: " + perk.displayName);
    }
}
