/*
First Date: 3/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the Player sphere (next to PlayerController, PlayerHealth and ObjectLevitator).
Unity Editor: The HungerBar (StatBar) finds this by itself. "On Changed" is only for extra things
              (e.g. a stomach growl): click +, drag in the object and pick a method from "Dynamic float, float".
              Testing: in Play mode, change "Hunger" in the Inspector, or click the component's ⋮ menu
              and choose "Test: Eat" / "Test: Lose Food" (uses "Test Amount").
Purpose: Holds the player's food (full = not hungry). It goes down over time:
         1x while idle, 2x while walking (a move key is held), 3x while sprinting (Left Shift),
         and +1x while levitating an object (idle + levitating = 2x, walking + levitating = 3x).
         At 0 the player starves (if a PlayerHealth is on the same object):
         dies at once ("Instant Death" on) or loses health over time (off).
Requirement: PlayerHunger.cs o-> PlayerController.cs, PlayerHealth.cs, ObjectLevitator.cs
             PlayerHunger.cs <-o ItemConsumer.cs, PlayerRespawn.cs, PlayerStats.cs, StatBar.cs
*/

using UnityEngine;
using UnityEngine.Events;   // UnityEvent

[DisallowMultipleComponent]
public class PlayerHunger : MonoBehaviour
{
    // ===== Fields =====
    [Header("Hunger")]
    [Tooltip("Most food the player can have (full bar).")]
    [Min(1f)]
    [SerializeField] float maxHunger = 100f;

    [Tooltip("Food right now. Change it in Play mode to test the bar.")]
    [SerializeField] float hunger = 100f;

    [Tooltip("Food lost per second while idle (1x). 0.1 = a full bar lasts about 17 minutes idle.")]
    [Min(0f)]
    [SerializeField] float drainPerSecond = 0.1f;

    [Tooltip("How many times faster food drains while walking.")]
    [Min(1f)]
    [SerializeField] float walkMultiplier = 2f;

    [Tooltip("How many times faster food drains while sprinting (used instead of Walk Multiplier). " +
             "Sprinting burns the most food, like Minecraft.")]
    [Min(1f)]
    [SerializeField] float sprintMultiplier = 3f;

    [Tooltip("Added on top while levitating an object (ObjectLevitator). 1 = idle 2x, walking 3x.")]
    [Min(0f)]
    [SerializeField] float levitateExtra = 1f;

    [Header("Starving")]
    [Tooltip("On: the player dies at once when food hits 0. Off: health goes down over time instead.")]
    [SerializeField] bool instantDeath = true;

    [Tooltip("Health lost per second while food is 0 (only when Instant Death is off).")]
    [Min(0f)]
    [SerializeField] float starveDamagePerSecond = 1f;

    [Tooltip("Called with (current, max) whenever food changes, and once at the start. E.g. a stomach growl. (The hunger bar doesn't need this.)")]
    [SerializeField] UnityEvent<float, float> onChanged;

    [Header("Testing")]
    [Tooltip("How much the test menu adds / removes.")]
    [Min(0f)]
    [SerializeField] float testAmount = 10f;

    PlayerController controller;   // tells us if the player is walking
    PlayerHealth health;           // loses health while starving (can be missing)
    ObjectLevitator levitator;     // tells us if the player is levitating (can be missing)

    public float Hunger => hunger;          // other scripts can read but not set it
    public float MaxHunger => maxHunger;
    public bool IsStarving => hunger <= 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // Cache once. Either can be missing: no controller = always idle, no health = no starving damage.
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        levitator = GetComponent<ObjectLevitator>();
    }

    void Start()
    {
        UpdateBar();   // show the starting food
    }

    void Update()
    {
        Drain();

        if (IsStarving)
        {
            Starve();
        }
    }

    // Unity calls this when a value is changed in the Inspector, so the bar follows while testing.
    void OnValidate()
    {
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);   // keep it between 0 and max
        UpdateBar();
    }

    // ===== My methods =====

    // Lose food over time: 1x idle, walkMultiplier x while walking, sprintMultiplier x while sprinting,
    // plus levitateExtra while levitating.
    void Drain()
    {
        if (IsStarving) return;                          // already empty
        if (health != null && health.IsDead) return;     // no getting hungry while dead (e.g. on the death screen)

        float multiplier = IsSprinting() ? sprintMultiplier
                         : IsWalking() ? walkMultiplier
                         : 1f;
        if (IsLevitating()) multiplier += levitateExtra;   // added, not multiplied: walking 2x + 1x = 3x

        // Time.deltaTime = seconds since last frame, so the drain is per second, not per frame.
        LoseFood(drainPerSecond * multiplier * Time.deltaTime);
    }

    // Food is gone: kill the player at once, or take health away over time.
    void Starve()
    {
        if (health == null || health.IsDead) return;   // no health to lose, or already dead

        if (instantDeath)
        {
            health.TakeDamage(health.Health);   // all remaining health = dead
        }
        else
        {
            health.TakeDamage(starveDamagePerSecond * Time.deltaTime);
        }
    }

    bool IsWalking()
    {
        return controller != null && controller.IsMoving;
    }

    bool IsSprinting()
    {
        return controller != null && controller.IsSprinting;
    }

    bool IsLevitating()
    {
        return levitator != null && levitator.Held != null;
    }

    // Get food back (e.g. eating), never above max.
    public void Eat(float amount)
    {
        if (amount <= 0f) return;

        hunger = Mathf.Min(hunger + amount, maxHunger);
        UpdateBar();
    }

    // Lose food, never below 0.
    public void LoseFood(float amount)
    {
        if (amount <= 0f) return;

        hunger = Mathf.Max(hunger - amount, 0f);
        UpdateBar();
    }

    void UpdateBar()
    {
        onChanged?.Invoke(hunger, maxHunger);
    }

    // Click the component's ⋮ menu in the Inspector to run these (Play mode).
    [ContextMenu("Test: Eat")]
    void TestEat()
    {
        Eat(testAmount);
    }

    [ContextMenu("Test: Lose Food")]
    void TestLoseFood()
    {
        LoseFood(testAmount);
    }
}
