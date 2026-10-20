/*
First Date: 3/10/2026
Date: 12/10/2026
Author: Tony
Use by: Drag this script onto the Player sphere.
Unity Editor: The HealthBar (StatBar) finds this by itself. "On Changed" is only for extra things
              (e.g. a hurt sound): click +, drag in the object and pick a method from "Dynamic float, float".
              "On Died" works the same way, for extras like a death sound.
              Testing: in Play mode, change "Health" in the Inspector (0 = die), or right-click the component's
              title and choose "Test: Take Damage" / "Test: Heal" / "Test: Die" (uses "Test Amount").
Purpose: Holds the player's health. TakeDamage / Heal change it and fire On Changed (current, max).
         At 0 health the player dies: the Died event fires once (PlayerRespawn listens to it and brings the
         player back). Revive fills health back up.
Requirement: PlayerHealth.cs <-x DeathScreen.cs, PlayerRespawn.cs
             PlayerHealth.cs <-o AutoCollector.cs, DaylightCycle.cs, ItemConsumer.cs, ItemDropper.cs, PlayerHunger.cs, PlayerStats.cs, StatBar.cs
*/

using UnityEngine;
using UnityEngine.Events;   // UnityEvent

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    // ===== Fields =====
    [Header("Health")]
    [Tooltip("Most health the player can have.")]
    [Min(1f)]
    [SerializeField] float maxHealth = 100f;

    [Tooltip("Health right now. Change it in Play mode to test the bar.")]
    [SerializeField] float health = 100f;

    [Tooltip("Called with (current, max) whenever health changes, and once at the start. E.g. a hurt sound. (The health bar doesn't need this.)")]
    [SerializeField] UnityEvent<float, float> onChanged;

    [Tooltip("Called once when the player dies (health reaches 0). E.g. a death sound. (Respawning doesn't need this.)")]
    [SerializeField] UnityEvent onDied;

    [Header("Testing")]
    [Tooltip("How much the right-click test menu damages / heals.")]
    [Min(0f)]
    [SerializeField] float testAmount = 10f;

    bool dead;           // already died (so Died fires only once per death)
    bool dieNextFrame;   // 0 was typed into Health in the Inspector: die in the next Update (see OnValidate)

    // Fired once when health reaches 0. Scripts subscribe in code, e.g. PlayerRespawn: health.Died += Respawn;
    public event System.Action Died;

    public float Health => health;         // other scripts can read but not set it
    public float MaxHealth => maxHealth;
    public bool IsDead => health <= 0f;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        UpdateBar();   // show the starting health
    }

    // Unity calls this when a value is changed in the Inspector, so the bar follows while testing.
    void OnValidate()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);   // keep it between 0 and max
        UpdateBar();

        // Typing 0 into Health while playing counts as dying, so respawning can be tested that way too.
        // Not right here: Unity doesn't allow making objects inside OnValidate (dying drops items),
        // so only remember it and die in the next Update.
        if (Application.isPlaying && IsDead && !dead) dieNextFrame = true;
    }

    void Update()
    {
        if (!dieNextFrame) return;
        dieNextFrame = false;
        if (IsDead) Die();   // still 0 (it may have been typed back up already)
    }

    // ===== My methods =====

    // Lose health. Negative amounts are ignored (use Heal for that).
    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        health = Mathf.Max(health - amount, 0f);   // never below 0
        UpdateBar();

        if (IsDead)
        {
            Die();
        }
    }

    // Get health back, never above max.
    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        health = Mathf.Min(health + amount, maxHealth);
        if (!IsDead) dead = false;   // healed from 0 = alive again
        UpdateBar();
    }

    // Back to life with full health (PlayerRespawn calls this).
    public void Revive()
    {
        health = maxHealth;
        dead = false;
        UpdateBar();
    }

    void UpdateBar()
    {
        onChanged?.Invoke(health, maxHealth);
    }

    void Die()
    {
        if (dead) return;   // only once per death
        dead = true;

        Debug.Log(gameObject.name + " died.");
        onDied?.Invoke();
        Died?.Invoke();     // e.g. PlayerRespawn brings the player back
    }

    // Right-click the component title in the Inspector to run these (Play mode).
    [ContextMenu("Test: Take Damage")]
    void TestTakeDamage()
    {
        TakeDamage(testAmount);
    }

    [ContextMenu("Test: Heal")]
    void TestHeal()
    {
        Heal(testAmount);
    }

    [ContextMenu("Test: Die")]
    void TestDie()
    {
        TakeDamage(health);   // all remaining health
    }
}
