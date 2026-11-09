using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PerkChest : MonoBehaviour
{
    [Header("Chest Cost")]
    [Min(0)]
    [SerializeField] int minCost = 5;

    [Min(0)]
    [SerializeField] int maxCost = 15;

    [SerializeField] KeyCode interactKey = KeyCode.E;

    [Header("Interaction")]
    [Min(1f)]
    [SerializeField] float interactionRange = 3f;

    bool opened;
    int openingCost;



    [Header("Available Perks")]
    [SerializeField] List<PerkData> availablePerks = new List<PerkData>();
    // above line creates an editable list in the inspector, now we can add future perk assets without editing script

    void Start()
    {
        if (maxCost < minCost)
            maxCost = minCost;

        openingCost = Random.Range(minCost, maxCost + 1);

        Debug.Log(name + " costs " + openingCost + " coins to open.");
    }

    void Update()
    {
        if (opened) return;

        if (!Input.GetKeyDown(interactKey)) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            return;

        // Only open this chest when the player is looking directly at it.
        if (hit.collider.transform != transform &&
            !hit.collider.transform.IsChildOf(transform))
            return;

        TryOpen();
    }


    void TryOpen()
    {
        if (opened) return;

        if (PerkSelectionUI.Instance == null)
        {
            Debug.LogWarning("PerkSelectionUI was not found.");
            return;
        }

        if (availablePerks == null || availablePerks.Count < 3)
        {
            Debug.LogWarning("The chest needs at least 3 available perks.");
            return;
        }

        if (PlayerCoins.Instance == null)
        {
            Debug.LogWarning("PlayerCoins was not found.");
            return;
        }

        if (PlayerCoins.Instance.Coins < openingCost)
        {
            Debug.Log("You need " + openingCost + " coins to open this chest.");
            return;
        }

        List<PerkData> choices = new List<PerkData>();

        List<PerkData> pool = new List<PerkData>(
            availablePerks.FindAll(perk => perk != null)
        );

        if (pool.Count < 3)
        {
            Debug.LogWarning("The chest needs at least 3 valid perk assets.");
            return;
        }

        // Randomly select three different perks.
        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, pool.Count);
            choices.Add(pool[index]);
            pool.RemoveAt(index);
        }

        // Show the UI before taking payment.
        bool shown = PerkSelectionUI.Instance.Show(
            choices,
            OnPerkSelected
        );

        if (!shown) return;

        if (!PlayerCoins.Instance.SpendCoins(openingCost))
        {
            PerkSelectionUI.Instance.Hide();
            return;
        }

        opened = true;

        Debug.Log("Chest opened! Choose your perk.");
    }




    void OnPerkSelected(PerkData selectedPerk)
    {
        if (PerkManager.Instance == null)
        {
            Debug.LogError("PerkManager was not found.");
            return;
        }

        PerkManager.Instance.ApplyPerk(selectedPerk);

        Debug.Log("Selected perk: " + selectedPerk.displayName);
    }



    void GrantRandomPerk()
    {
        int perk = Random.Range(0, 4);

        switch (perk)
        {
            // Perk 1 :increase heath by 20 percent
            case 0:
                {
                    PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

                    if (health != null)
                        health.IncreaseMaxHealth(0.20f);
                    else
                        Debug.LogWarning("PlayerHealth was not found.");
                    PlayerPerks.Record(PerkType.MaxHealth);   // [Tony] added: the HUD shows the perk (PerkBar)
                    break;
                }

            // Perk 2 : Increase max stamina by 25 percent
            case 1:
                {
                    PlayerStamina stamina = FindFirstObjectByType<PlayerStamina>();

                    if (stamina != null)
                        stamina.IncreaseMaxStamina(25f);
                    else
                        Debug.LogWarning("PlayerStamina was not found.");
                    PlayerPerks.Record(PerkType.MaxStamina);   // [Tony] added: the HUD shows the perk (PerkBar)
                    break;
                }

            // Perk 3 : increase Max move speed by 15 percent 
            case 2:
                {
                    PlayerController controller = FindFirstObjectByType<PlayerController>();

                    if (controller != null)
                        controller.IncreaseMoveSpeed(0.15f);
                    else
                        Debug.LogWarning("PlayerController was not found.");
                    PlayerPerks.Record(PerkType.MoveSpeed);   // [Tony] added: the HUD shows the perk (PerkBar)
                    break;
                }

            // Perk 4: Unlock or upgrade the Fireball ability.
            case 3:
                {
                    PlayerFireball fireball =
                        FindFirstObjectByType<PlayerFireball>();

                    if (fireball != null)
                        fireball.AddFireballPerk();
                    else
                        Debug.LogWarning(
                            "PlayerFireball was not found on the player."
                        );
                    PlayerPerks.Record(PerkType.Fireball);   // [Tony] added: the HUD shows the perk (PerkBar)

                    break;
                }


        }
    }
}
