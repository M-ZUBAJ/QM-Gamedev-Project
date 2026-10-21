using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerFireball : MonoBehaviour
{
    [Header("Fireball Cooldown")]
    [SerializeField] float startingCooldown = 3f;// 3 seconds
    [SerializeField, Range(0f, 1f)] float cooldownReductionPerPerk = 0.20f; // each perk stack will reduce cooldown by 20%%
    [SerializeField] float minimumCooldown = 1f;

    int perkStacks;

    public int PerkStacks => perkStacks;

    public float CurrentCooldown
    {
        get
        {
            float cooldown = startingCooldown;

            for (int i = 1; i < perkStacks; i++)
            {
                cooldown *= 1f - cooldownReductionPerPerk;
            }

            return Mathf.Max(minimumCooldown, cooldown);
        }
    }

    public bool HasFireballPerk => perkStacks > 0;

    public void AddFireballPerk()
    {
        perkStacks++;

        Debug.Log(
            "Fireball perk stacks: " + perkStacks +
            " | Cooldown: " + CurrentCooldown.ToString("F2") + " seconds"
        );
    }
}
