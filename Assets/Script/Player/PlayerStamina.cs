/*
First Date: 11/10/2026
Date: 21/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerController and PlayerJump).
Unity Editor: The StaminaBar (StatBar, Stat = Stamina) finds this by itself.
              Testing: in Play mode, hold Left Shift + W or press Space and watch the bar, or change
              "Stamina" in the Inspector, or click the component's ⋮ menu and choose "Test: Use Stamina".
Purpose: Holds the player's stamina. Sprinting (PlayerController) and jumping (PlayerJump) use it up,
         and it fills back up by itself after a short rest.
         When it hits 0 the player is tired: no sprinting or jumping until it has refilled to "Recover To".
Requirement: PlayerStamina.cs <-x PerkChest.cs
             PlayerStamina.cs <-o ItemConsumer.cs, PlayerController.cs, PlayerJump.cs, PlayerPerks.cs, PlayerRespawn.cs, PlayerStats.cs, StatBar.cs
*/

using UnityEngine;
using UnityEngine.Events;   // UnityEvent

[DisallowMultipleComponent]
public class PlayerStamina : MonoBehaviour
{
    // ===== Fields =====
    [Header("Stamina")]
    [Tooltip("Most stamina the player can have (full bar).")]
    [Min(1f)]
    [SerializeField] float maxStamina = 100f;

    [Tooltip("Stamina right now. Change it in Play mode to test the bar.")]
    [SerializeField] float stamina = 100f;

    [Header("Using it")]
    [Tooltip("Stamina used per second while sprinting. 20 = a full bar (100) lasts 5 seconds of sprinting.")]
    [Min(0f)]
    [SerializeField] float sprintDrainPerSecond = 20f;

    [Tooltip("Stamina used by each jump. 5 = 20 jumps from a full bar (100).")]
    [Min(0f)]
    [SerializeField] float jumpCost = 5f;

    [Header("Getting it back")]
    [Tooltip("Stamina gained back per second while resting. 25 = empty to full (100) in 4 seconds.")]
    [Min(0f)]
    [SerializeField] float regenPerSecond = 25f;

    [Tooltip("Seconds after the last sprint or jump before stamina starts filling back up.")]
    [Min(0f)]
    [SerializeField] float regenDelay = 1f;

    [Tooltip("After running out, the player is tired until stamina is back to this much. " +
             "Stops sprint from stuttering on and off at an empty bar.")]
    [Min(0f)]
    [SerializeField] float recoverTo = 20f;

    [Tooltip("Called with (current, max) whenever stamina changes, and once at the start. E.g. a panting sound. (The stamina bar doesn't need this.)")]
    [SerializeField] UnityEvent<float, float> onChanged;

    [Header("Testing")]
    [Tooltip("How much the test menu uses up.")]
    [Min(0f)]
    [SerializeField] float testAmount = 10f;

    float startMaxStamina;       // max stamina before any perk (ResetMaxStamina goes back to it)
    float lastUsedTime = -10f;   // when stamina was last used (regen waits regenDelay after this)
    bool tired;                  // ran out and hasn't recovered to recoverTo yet

    public float Stamina => stamina;          // other scripts can read but not set it
    public float MaxStamina => maxStamina;
    public bool IsTired => tired;
    public bool CanSprint => !tired && stamina > 0f;
    public bool CanJump => !tired && stamina > 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        startMaxStamina = maxStamina;   // before any perk raises it
    }

    void Start()
    {
        UpdateBar();   // show the starting stamina
    }

    void Update()
    {
        Regen();
    }

    // Unity calls this when a value is changed in the Inspector, so the bar follows while testing.
    void OnValidate()
    {
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);   // keep it between 0 and max
        UpdateBar();
    }

    // ===== My methods =====

    // PlayerController calls this every physics step while sprinting (seconds = that step's length).
    public void SpendSprint(float seconds)
    {
        Use(sprintDrainPerSecond * seconds);
    }

    // PlayerJump calls this once per jump.
    public void SpendJump()
    {
        Use(jumpCost);
    }

    // Use up stamina, never below 0. Hitting 0 makes the player tired.
    public void Use(float amount)
    {
        if (amount <= 0f) return;

        stamina = Mathf.Max(stamina - amount, 0f);
        lastUsedTime = Time.time;
        if (stamina <= 0f) tired = true;
        UpdateBar();
    }

    // Get stamina back at once (e.g. eating), never above max. Doesn't count as resting or using.
    public void Restore(float amount)
    {
        if (amount <= 0f) return;

        stamina = Mathf.Min(stamina + amount, maxStamina);
        if (stamina >= Mathf.Min(recoverTo, maxStamina)) tired = false;
        UpdateBar();
    }


    // Method below added by Marko to support Perk system
    public void IncreaseMaxStamina(float amount)
    {
        if (amount <= 0f) return;

        maxStamina += amount;
        stamina += amount;

        if (stamina >= Mathf.Min(recoverTo, maxStamina))
            tired = false;

        UpdateBar();

        Debug.Log("Maximum stamina increased to " + maxStamina);
    }

    // [Tony] added: undoes every Max Stamina perk (IncreaseMaxStamina above). PlayerPerks calls it when the player dies.
    public void ResetMaxStamina()
    {
        maxStamina = startMaxStamina;
        stamina = Mathf.Min(stamina, maxStamina);
        UpdateBar();
    }





    // Fill back up after resting for regenDelay seconds.
    void Regen()
    {
        if (Time.time - lastUsedTime < regenDelay) return;   // still resting
        if (stamina >= maxStamina) return;                    // already full

        // Time.deltaTime = seconds since last frame, so the regen is per second, not per frame.
        stamina = Mathf.Min(stamina + regenPerSecond * Time.deltaTime, maxStamina);
        if (stamina >= Mathf.Min(recoverTo, maxStamina)) tired = false;
        UpdateBar();
    }

    void UpdateBar()
    {
        onChanged?.Invoke(stamina, maxStamina);
    }

    // Click the component's ⋮ menu in the Inspector to run this (Play mode).
    [ContextMenu("Test: Use Stamina")]
    void TestUse()
    {
        Use(testAmount);
    }
}
