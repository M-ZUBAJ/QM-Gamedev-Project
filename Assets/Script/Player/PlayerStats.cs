/*
First Date: 11/10/2026
Date: 22/11/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerController, PlayerHealth, PlayerHunger, PlayerStamina...).
Unity Editor: Nothing to connect: it finds the other player scripts by itself.
              Testing: in Play mode, walk, sprint, jump, eat, kill a monster and build, and watch the numbers in the
              Inspector (they are shown for checking; typing into them does nothing useful). Then VictoryScreen's
              ⋮ > Test: Win shows them as the victory page does.
Purpose: Counts what the player did in this life (since the start, or since the last respawn), for the victory and death
         pages: monsters killed, coins earned and spent, distance walked and run (walking + running = all of it), jumps,
         stamina used, health lost / healed, food lost / eaten, items eaten and buildings built. Starts again from 0
         after each respawn.
         Health, food, stamina and coins are counted by watching the values go up and down, so every cause counts
         (falling, starving, eating, sprinting, a chest's price...) without those scripts knowing about this one.
         A bar that only gets bigger or smaller (a Max Health / Max Stamina perk, Night Power at night and at dawn)
         isn't healing, damage or stamina used, so those moments aren't counted. Coins aren't counted while the player
         is dead either: dying puts them back to the start (Marko's PlayerCoins), which isn't earning or spending.
         Any missing script is just not counted (e.g. no BlueprintPlacer = 0 buildings).
         Before 22/11/2026 Distance Run always said 0 (it asked PlayerController.IsSprinting at a moment when that is
         always false: see PlayerController.SprintedLastStep), and a Max Health perk counted as healing.
Requirement: PlayerStats.cs o-> PlayerController.cs, PlayerJump.cs, PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs,
             ItemConsumer.cs, BlueprintPlacer.cs, PlayerRespawn.cs, PlayerCoins.cs, MonsterBounty.cs, Rigidbody (Unity)
             PlayerStats.cs <-o DeathScreen.cs, VictoryScreen.cs
*/

using System.Globalization;   // CultureInfo: "10,748" and "2.5 km" the same on every computer
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    // ===== Fields =====
    [Header("This life (shown for checking)")]
    [Tooltip("Monsters the player killed (a punch, a weapon, an arrow, a fireball, Thorns...). Monsters that burn away " +
             "at dawn don't count.")]
    [SerializeField] int monstersKilled;
    [Tooltip("Coins gained (from monsters, set bonuses, perks...). Coins spent on chests aren't taken off (see Coins Spent).")]
    [SerializeField] int coinsEarned;
    [Tooltip("Coins paid (chests, rerolls...). Coins lost by dying (they go back to the start) aren't counted.")]
    [SerializeField] int coinsSpent;
    [Tooltip("Meters walked: a move key pressed, not sprinting.")]
    [SerializeField] float distanceWalked;
    [Tooltip("Meters run: sprinting. Walked + run = every meter moved.")]
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
    const float KilometerFrom = 1000f;   // from this many meters, a distance is shown in km ("2.5 km", not "2500 m")

    PlayerController controller;   // every one of these can be missing
    PlayerJump jump;
    PlayerHealth health;
    PlayerHunger hunger;
    PlayerStamina stamina;
    ItemConsumer consumer;
    BlueprintPlacer placer;
    PlayerRespawn respawn;
    PlayerCoins coins;             // Marko's coins (on the Player)
    Rigidbody body;                // where the physics has the player (the shown position is smoothed between steps)

    Vector3 lastPosition;          // where the player was last physics step
    float lastHealth, lastFood, lastStamina;   // the values last frame, to see how much they changed
    float lastMaxHealth, lastMaxStamina;       // the bars' sizes last frame (a change there isn't healing or using)
    int lastCoins;

    public int MonstersKilled => monstersKilled;
    public int CoinsEarned => coinsEarned;
    public int CoinsSpent => coinsSpent;
    public float DistanceWalked => distanceWalked;   // without running
    public float DistanceRun => distanceRun;
    public float DistanceMoved => distanceWalked + distanceRun;
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
        coins = GetComponent<PlayerCoins>();
        body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (jump != null) jump.Jumped += CountJump;
        if (consumer != null) consumer.Consumed += CountEaten;
        if (placer != null) placer.Built += CountBuilt;
        if (respawn != null) respawn.Respawned += ResetStats;
        MonsterBounty.MonsterKilled += CountKill;
    }

    void OnDisable()
    {
        if (jump != null) jump.Jumped -= CountJump;
        if (consumer != null) consumer.Consumed -= CountEaten;
        if (placer != null) placer.Built -= CountBuilt;
        if (respawn != null) respawn.Respawned -= ResetStats;
        MonsterBounty.MonsterKilled -= CountKill;
    }

    void Start()
    {
        if (coins == null) coins = PlayerCoins.Instance;   // on another object
        ResetStats();
    }

    // Distance: measured at the physics rate, the same rate the player moves at.
    void FixedUpdate()
    {
        Vector3 now = Position();
        Vector3 step = now - lastPosition;
        step.y = 0f;                             // only ground distance: falling and jumping up don't count
        lastPosition = now;

        float meters = step.magnitude;
        if (meters > TeleportDistance) return;   // respawn or other teleport
        if (controller == null || !controller.IsMoving) return;   // pushed or sliding, not walking

        // SprintedLastStep, not IsSprinting: in a FixedUpdate IsSprinting can say false while the player runs.
        if (controller.SprintedLastStep) distanceRun += meters;
        else distanceWalked += meters;
    }

    // Health, food, stamina, coins: how much each went down or up since last frame.
    void Update()
    {
        if (health != null)
        {
            float max = health.MaxHealth;
            if (!Mathf.Approximately(max, lastMaxHealth)) lastMaxHealth = max;   // the bar changed size: nobody was hurt or healed
            else
            {
                float change = health.Health - lastHealth;
                if (change < 0f) healthLost += -change; else healthHealed += change;
            }
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
            float max = stamina.MaxStamina;
            if (!Mathf.Approximately(max, lastMaxStamina)) lastMaxStamina = max;   // the bar changed size: nothing was used
            else
            {
                float change = stamina.Stamina - lastStamina;
                if (change < 0f) staminaUsed += -change;   // getting it back by resting isn't counted
            }
            lastStamina = stamina.Stamina;
        }

        if (coins != null)
        {
            int change = coins.Coins - lastCoins;
            bool dead = health != null && health.IsDead;     // dying puts the coins back to the start: not earned or spent
            if (!dead && change > 0) coinsEarned += change;  // spending (a chest) isn't taken off: it's what was earned
            if (!dead && change < 0) coinsSpent += -change;  // a chest, a reroll
            lastCoins = coins.Coins;
        }
    }

    // ===== My methods =====

    // Back to 0 for a new life. Also remembers the current values, so the refill on respawn isn't counted as "healed".
    public void ResetStats()
    {
        monstersKilled = coinsEarned = coinsSpent = 0;
        distanceWalked = distanceRun = 0f;
        jumps = itemsEaten = buildingsBuilt = 0;
        staminaUsed = healthLost = healthHealed = foodLost = foodEaten = 0f;

        lastPosition = Position();
        if (health != null) { lastHealth = health.Health; lastMaxHealth = health.MaxHealth; }
        if (hunger != null) lastFood = hunger.Hunger;
        if (stamina != null) { lastStamina = stamina.Stamina; lastMaxStamina = stamina.MaxStamina; }
        if (coins != null) lastCoins = coins.Coins;
    }

    // The numbers as lines of text, ready for a UI text (the victory and death pages use it).
    // Big numbers get a comma ("10,748"), long distances are in km ("23.1 km").
    public string Summary()
    {
        return "Monsters killed: " + Whole(monstersKilled) + "\n" +
               "Coins earned: " + Whole(coinsEarned) + "   spent: " + Whole(coinsSpent) + "\n" +
               "Distance walked: " + Distance(distanceWalked) + "\n" +
               "Distance run: " + Distance(distanceRun) + "\n" +
               "Jumps: " + Whole(jumps) + "\n" +
               "Stamina used: " + Whole(staminaUsed) + "\n" +
               "Health lost: " + Whole(healthLost) + "   healed: " + Whole(healthHealed) + "\n" +
               "Food lost: " + Whole(foodLost) + "   eaten: " + Whole(foodEaten) + "\n" +
               "Items eaten: " + Whole(itemsEaten) + "\n" +
               "Buildings built: " + Whole(buildingsBuilt);
    }

    // Where the physics has the player: the Rigidbody's position (the Transform is smoothed between physics steps,
    // Interpolate, so reading it at the physics rate could add a little back-and-forth to the distance).
    Vector3 Position() => body != null ? body.position : transform.position;

    // A whole number with commas: 10748.4 -> "10,748".
    static string Whole(float value) => Mathf.RoundToInt(value).ToString("N0", CultureInfo.InvariantCulture);

    // Meters, or km from KilometerFrom on: 850 -> "850 m", 23131 -> "23.1 km".
    static string Distance(float meters)
    {
        if (meters < KilometerFrom) return Whole(meters) + " m";
        return (meters / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km";
    }

    void CountJump() => jumps++;
    void CountEaten(ItemData item) => itemsEaten++;
    void CountBuilt(BuildingData building) => buildingsBuilt++;
    void CountKill() => monstersKilled++;
}
