/*
First Date: 16/11/2026
Date: 16/11/2026
Author: Turki
Use by: Drag this script onto the Player (next to ItemConsumer and PlayerHealth).
Unity Editor: In "Buffs", add one entry per food that gives a buff: drag its Item Data into "Food", then set
              "Max Health Percent" (0.05 = +5%) and "Duration" (seconds).
Purpose: Crafted food (e.g. Stew) gives a temporary max health buff after it is eaten, so it is worth eating
         even at full health and hunger. Only one buff is active at a time: eating the same food again restarts
         its timer, and eating another buff food replaces the current one (buffs do not stack).
         The buff ends when its timer runs out or the player dies.
Requirement: FoodBuff.cs o-> ItemConsumer.cs, PlayerHealth.cs, ItemData.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FoodBuff : MonoBehaviour
{
    // One buff: which food gives it, how strong it is and how long it lasts.
    [System.Serializable]
    public class Buff
    {
        [Tooltip("The food that gives this buff (an Item Data asset, e.g. Stew).")]
        public ItemData food;

        [Tooltip("Extra max health as a fraction of the current max (0.05 = +5%).")]
        [Range(0f, 1f)] public float maxHealthPercent = 0.05f;

        [Tooltip("How many seconds the buff lasts.")]
        [Min(1f)] public float duration = 120f;
    }

    // ===== Fields =====
    [Tooltip("The foods that give a buff. Add one entry per food.")]
    [SerializeField] List<Buff> buffs = new List<Buff>();

    ItemConsumer consumer;
    PlayerHealth health;
    Buff active;           // the buff running now (null = none)
    float remaining;       // seconds left on it
    float appliedAmount;   // how much max health it added, so we can take exactly that away again

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        consumer = GetComponent<ItemConsumer>();
        health = GetComponent<PlayerHealth>();
    }

    // Listen to events instead of editing Tony's scripts.
    void OnEnable()
    {
        if (consumer != null) consumer.Consumed += OnConsumed;
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (consumer != null) consumer.Consumed -= OnConsumed;
        if (health != null) health.Died -= OnDied;
    }

    void Update()
    {
        if (active == null) return;

        // Time.deltaTime = seconds since the last frame.
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Debug.Log("Food buff ended.");
            RemoveBuff();
        }
    }

    // ===== My methods =====

    // Called every time the player eats something.
    void OnConsumed(ItemData item)
    {
        foreach (Buff buff in buffs)
        {
            if (buff.food != item) continue;

            if (active == buff)
            {
                remaining = buff.duration;   // same food again: only restart the timer, don't add max health twice
                Debug.Log("Food buff restarted: " + item.DisplayName);
            }
            else
            {
                RemoveBuff();                // another buff food replaces the old buff, never both
                ApplyBuff(buff);
                Debug.Log("Food buff started: " + item.DisplayName);
            }
            return;
        }
    }

    void ApplyBuff(Buff buff)
    {
        if (health == null) return;

        appliedAmount = health.MaxHealth * buff.maxHealthPercent;
        health.AddMaxHealth(appliedAmount);
        health.Heal(appliedAmount);          // the new health bar space starts filled, like the max health perk
        active = buff;
        remaining = buff.duration;
    }

    // Takes back exactly the max health the buff gave.
    void RemoveBuff()
    {
        if (active == null) return;

        if (health != null) health.AddMaxHealth(-appliedAmount);
        active = null;
        appliedAmount = 0f;
    }

    // On death the perk reset already puts max health back to normal, so only forget the buff (taking it away
    // again would lower max health below normal).
    void OnDied()
    {
        active = null;
        appliedAmount = 0f;
    }
}