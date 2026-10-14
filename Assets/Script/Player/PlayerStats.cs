/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerController, PlayerHealth, PlayerHunger, PlayerStamina...).
Unity Editor: Nothing to connect: it finds the other player scripts by itself.
              Testing: in Play mode, walk, sprint, jump, eat and build, and watch the numbers in the Inspector
              (they are shown for checking; typing into them does nothing useful).
Purpose: Counts what the player did in this life (since the start, or since the last respawn), for the death screen:
         distance walked (all moving) and run (sprinting), jumps, stamina used, health lost / healed,
         food lost / eaten, items eaten and buildings built. Starts again from 0 after each respawn.
         Health, food and stamina are counted by watching the values go up and down, so every cause counts
         (falling, starving, eating, sprinting...) without those scripts knowing about this one.
         Any missing script is just not counted (e.g. no BlueprintPlacer = 0 buildings).
Requirement: PlayerStats.cs o-> PlayerController.cs, PlayerJump.cs, PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs,
             ItemConsumer.cs, BlueprintPlacer.cs, PlayerRespawn.cs
             PlayerStats.cs <-o DeathScreen.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    // ===== Fields =====
    [Header("This life (shown for checking)")]
    [Tooltip("Meters moved while pressing a move key (walking AND running).")]
    [SerializeField] float distanceWalked;
    [Tooltip("Meters moved while sprinting (part of Distance Walked).")]
    [SerializeField] float distanceRun;
    [SerializeField] int jumps;
    [SerializeField] float staminaUsed;
    [SerializeField] float healthLost;
    [SerializeField] float healthHealed;
    [SerializeField] float foodLost;
    [SerializeField] float foodEaten;
    [SerializeField] int itemsEaten;
    [SerializeField] int buildingsBuilt;

    // A move longer than this in one physics step isn't walking, it's a teleport (e.g. respawning): not counted.
    const float TeleportDistance = 5f;

    PlayerController controller;   // every one of these can be missing
    PlayerJump jump;
    PlayerHealth health;
    PlayerHunger hunger;
    PlayerStamina stamina;
    ItemConsumer consumer;
    BlueprintPlacer placer;
    PlayerRespawn respawn;

    Vector3 lastPosition;          // where the player was last physics step
    float lastHealth, lastFood, lastStamina;   // the values last frame, to see how much they changed

    public float DistanceWalked => distanceWalked;
    public float DistanceRun => distanceRun;
    public int Jumps => jumps;
    public float StaminaUsed => staminaUsed;
    public float HealthLost => healthLost;
    public float HealthHealed => healthHealed;
    public float FoodLost => foodLost;
    public float FoodEaten => foodEaten;
    public int ItemsEaten => itemsEaten;
    public int BuildingsBuilt => buildingsBuilt;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        jump = GetComponent<PlayerJump>();
        health = GetComponent<PlayerHealth>();
        hunger = GetComponent<PlayerHunger>();
        stamina = GetComponent<PlayerStamina>();
        consumer = GetComponent<ItemConsumer>();
        placer = GetComponent<BlueprintPlacer>();
        respawn = GetComponent<PlayerRespawn>();
    }

    void OnEnable()
    {
        if (jump != null) jump.Jumped += CountJump;
        if (consumer != null) consumer.Consumed += CountEaten;
        if (placer != null) placer.Built += CountBuilt;
        if (respawn != null) respawn.Respawned += ResetStats;
    }

    void OnDisable()
    {
        if (jump != null) jump.Jumped -= CountJump;
        if (consumer != null) consumer.Consumed -= CountEaten;
        if (placer != null) placer.Built -= CountBuilt;
        if (respawn != null) respawn.Respawned -= ResetStats;
    }

    void Start()
    {
        ResetStats();
    }

    // Distance: measured at the physics rate, the same rate the player moves at.
    void FixedUpdate()
    {
        Vector3 step = transform.position - lastPosition;
        step.y = 0f;                             // only ground distance: falling and jumping up don't count
        lastPosition = transform.position;

        float meters = step.magnitude;
        if (meters > TeleportDistance) return;   // respawn or other teleport
        if (controller == null || !controller.IsMoving) return;   // pushed or sliding, not walking

        distanceWalked += meters;
        if (controller.IsSprinting) distanceRun += meters;
    }

    // Health, food, stamina: how much each went down or up since last frame.
    void Update()
    {
        if (health != null)
        {
            float change = health.Health - lastHealth;
            if (change < 0f) healthLost += -change; else healthHealed += change;
            lastHealth = health.Health;
        }

        if (hunger != null)
        {
            float change = hunger.Hunger - lastFood;
            if (change < 0f) foodLost += -change; else foodEaten += change;
            lastFood = hunger.Hunger;
        }

        if (stamina != null)
        {
            float change = stamina.Stamina - lastStamina;
            if (change < 0f) staminaUsed += -change;   // getting it back by resting isn't counted
            lastStamina = stamina.Stamina;
        }
    }

    // ===== My methods =====

    // Back to 0 for a new life. Also remembers the current values, so the refill on respawn isn't counted as "healed".
    public void ResetStats()
    {
        distanceWalked = distanceRun = 0f;
        jumps = itemsEaten = buildingsBuilt = 0;
        staminaUsed = healthLost = healthHealed = foodLost = foodEaten = 0f;

        lastPosition = transform.position;
        if (health != null) lastHealth = health.Health;
        if (hunger != null) lastFood = hunger.Hunger;
        if (stamina != null) lastStamina = stamina.Stamina;
    }

    // The numbers as lines of text, ready for a UI text (the death screen uses it).
    // F0 = no decimals (12.7 -> "13").
    public string Summary()
    {
        return "Distance walked: " + distanceWalked.ToString("F0") + " m\n" +
               "Distance run: " + distanceRun.ToString("F0") + " m\n" +
               "Jumps: " + jumps + "\n" +
               "Stamina used: " + staminaUsed.ToString("F0") + "\n" +
               "Health lost: " + healthLost.ToString("F0") + "   healed: " + healthHealed.ToString("F0") + "\n" +
               "Food lost: " + foodLost.ToString("F0") + "   eaten: " + foodEaten.ToString("F0") + "\n" +
               "Items eaten: " + itemsEaten + "\n" +
               "Buildings built: " + buildingsBuilt;
    }

    void CountJump() => jumps++;
    void CountEaten(ItemData item) => itemsEaten++;
    void CountBuilt(BuildingData building) => buildingsBuilt++;
}
