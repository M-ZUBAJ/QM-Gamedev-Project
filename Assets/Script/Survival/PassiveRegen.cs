/*
First Date: 16/11/2026
Date: 16/11/2026
Author: Turki
Use by: Drag this script onto the Player sphere (next to PlayerHealth and PlayerHunger).
Unity Editor: Change "Heal Amount", "Interval" and "Min Hunger Fraction" in the Inspector.
Purpose: Slow passive health regeneration. While the player is hurt and well fed (hunger bar nearly full),
         health goes up by Heal Amount every Interval seconds, so players don't have to eat just to heal.
Requirement: PassiveRegen.cs o-> PlayerHealth.cs, PlayerHunger.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PassiveRegen : MonoBehaviour
{
    // ===== Fields =====
    [Header("Regeneration")]
    [Tooltip("Health gained every time the timer finishes.")]
    [Min(0f)]
    [SerializeField] float healAmount = 2f;

    [Tooltip("Seconds between each heal.")]
    [Min(0.1f)]
    [SerializeField] float interval = 10f;

    [Tooltip("How full the hunger bar must be to regenerate (0.9 = 90%). Hunger drains all the time, " +
             "so exactly 100% would almost never happen.")]
    [Range(0f, 1f)]
    [SerializeField] float minHungerFraction = 0.9f;

    PlayerHealth health;
    PlayerHunger hunger;
    float timer;   // seconds since the last heal

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // Both scripts are on the same object (the Player), so we can find them once here.
        health = GetComponent<PlayerHealth>();
        hunger = GetComponent<PlayerHunger>();
    }

    void Update()
    {
        if (health == null || hunger == null) return;

        // Regenerate only while alive, hurt and well fed. Otherwise the timer starts again,
        // so a player can't "save up" a heal.
        bool wellFed = hunger.Hunger >= hunger.MaxHunger * minHungerFraction;
        bool hurt = health.Health < health.MaxHealth;

        if (health.IsDead || !hurt || !wellFed)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;   // Time.deltaTime = seconds since the last frame
        if (timer >= interval)
        {
            timer = 0f;
            health.Heal(healAmount);
        }
    }
}