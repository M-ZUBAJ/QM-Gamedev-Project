/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: This is an ASSET (Assets/Config/NewGame/NewGameDefaults.asset), not a script you put on an object.
        Tools > UI > Build Main Title Scene makes it; the New Game page (NewGamePanel) reads it.
Unity Editor: Don't type in it: it's a copy of how GameScene is set up right now. It's read again by itself every time
              GameScene is saved (NewGameDefaultsSync), so change the game in GameScene, save, and the New Game page's
              defaults follow. "Read From" says when it was last read.
Purpose: The game as it is right now = the New Game page's defaults: days to win, win time (VictoryScreen),
         day length, start time and the parts of the day (DaylightCycle), enemies per night (EnemySpawner),
         hunger speed (PlayerHunger) and the death / pickup / message rules (GameRules).
         Keeping them in an asset lets the title scene show them without loading GameScene.
Requirement: NewGameDefaults.cs x-> VictoryScreen.cs, GameSetup.cs
             NewGameDefaults.cs <-x NewGameDefaultsSync.cs, NewGamePanel.cs, TitleSceneBuilder.cs
*/

using UnityEngine;

public class NewGameDefaults : ScriptableObject
{
    // ===== Fields =====
    [Header("Goal (GameEndCanvas > VictoryScreen)")]
    [SerializeField] int daysToWin = 5;
    [SerializeField] WinTime winTime = WinTime.Dawn;

    [Header("World (the sun's DaylightCycle)")]
    [Tooltip("Real minutes in one full day + night.")]
    [SerializeField] float dayLengthMinutes = 10f;
    [Tooltip("Time Of Day the game starts at: 0 = midnight, 0.5 = midday.")]
    [SerializeField] float startTime = 0.3f;
    [SerializeField] float dawnStart = 0.23f;
    [SerializeField] float morningStart = 0.30f;
    [SerializeField] float afternoonStart = 0.55f;
    [SerializeField] float duskStart = 0.70f;
    [SerializeField] float nightStart = 0.77f;

    [Header("Enemies (EnemySpawner) and hunger (PlayerHunger)")]
    [SerializeField] bool spawnAtNight = true;
    [SerializeField] int enemiesPerNight = 3;
    [Tooltip("Hunger lost per second.")]
    [SerializeField] float hungerDrainPerSecond = 0.1f;

    [Header("Rules (GameRules)")]
    [SerializeField] bool keepInventory = false;
    [SerializeField] bool clearItemOnDead = false;
    [SerializeField] bool autoPickupDrops = false;
    [SerializeField] bool showNotifications = true;

    [Header("Where these came from")]
    [Tooltip("The scene and the moment these values were read.")]
    [SerializeField] string readFrom = "";

    public int DaysToWin => daysToWin;
    public WinTime WinTime => winTime;
    public float DayLengthMinutes => dayLengthMinutes;
    public float StartTime => startTime;
    public float DawnStart => dawnStart;
    public float MorningStart => morningStart;
    public float AfternoonStart => afternoonStart;
    public float DuskStart => duskStart;
    public float NightStart => nightStart;
    public bool SpawnAtNight => spawnAtNight;
    public int EnemiesPerNight => enemiesPerNight;
    public float HungerDrainPerSecond => hungerDrainPerSecond;
    public bool KeepInventory => keepInventory;
    public bool ClearItemOnDead => clearItemOnDead;
    public bool AutoPickupDrops => autoPickupDrops;
    public bool ShowNotifications => showNotifications;
    public string ReadFrom => readFrom;

    // What happens to the items when the player dies, from the two rules.
    public GameSetup.DeathRule WhenDying => keepInventory ? GameSetup.DeathRule.KeepItems
                                          : clearItemOnDead ? GameSetup.DeathRule.LoseItems
                                          : GameSetup.DeathRule.DropItems;

    // ===== My methods =====

    // The Time Of Day a Win Time means, with this game's parts of the day (Midday = 12:00, Midnight = 00:00).
    public float TimeOf(WinTime time)
    {
        switch (time)
        {
            case WinTime.Morning:   return morningStart;
            case WinTime.Midday:    return 0.5f;
            case WinTime.Afternoon: return afternoonStart;
            case WinTime.Dusk:      return duskStart;
            case WinTime.Night:     return nightStart;
            case WinTime.Midnight:  return 0f;
            default:                return dawnStart;   // Dawn
        }
    }

    // A Time Of Day as a clock: 0.5 -> "12:00", 0.23 -> "05:31".
    public static string Clock(float timeOfDay)
    {
        int minutes = Mathf.FloorToInt(Mathf.Repeat(timeOfDay, 1f) * 24f * 60f + 0.001f);
        return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
    }
}
