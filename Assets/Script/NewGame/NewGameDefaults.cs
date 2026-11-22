/*
First Date: 7/11/2026
Date: 22/11/2026
Author: Tony
Use by: This is an ASSET (Assets/Config/NewGame/NewGameDefaults.asset), not a script you put on an object.
        Tools > UI > Build Main Title Scene makes it; the New Game page (NewGamePanel) reads it.
Unity Editor: Don't type in it: it's a copy of how GameScene is set up right now. It's read again by itself every time
              GameScene is saved (NewGameDefaultsSync), so change the game in GameScene, save, and the New Game page's
              defaults follow. "Read From" says when it was last read.
Purpose: The game as it is right now = the New Game page's defaults: days to win, win time (VictoryScreen),
         day length, start time and the parts of the day (DaylightCycle), the night monsters (EnemySpawner: on / off
         and the seconds between them on nights 1 - 5), hunger speed (PlayerHunger), starting coins (PlayerCoins), the
         death / pickup / message rules and the Tool Test, Armor Test and New Chests rules (GameRules).
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
    // Seconds between night monsters (EnemySpawner > Night 1 - 5 Spawn Interval). Easy doubles them, Hard divides them
    // by 1.5 (NewGamePanel). Every night after night 5 uses night 5's (Marko's EnemySpawner, Tony's line, 22/11/2026).
    [Tooltip("Seconds between night monsters on night 1 (EnemySpawner).")]
    [SerializeField] float night1SpawnInterval = 10f;
    [Tooltip("Seconds between night monsters on night 2 (EnemySpawner).")]
    [SerializeField] float night2SpawnInterval = 7f;
    [Tooltip("Seconds between night monsters on night 3 (EnemySpawner).")]
    [SerializeField] float night3SpawnInterval = 5f;
    [Tooltip("Seconds between night monsters on night 4 (EnemySpawner).")]
    [SerializeField] float night4SpawnInterval = 5f;
    [Tooltip("Seconds between night monsters on night 5 (EnemySpawner).")]
    [SerializeField] float night5SpawnInterval = 5f;
    [Tooltip("Hunger lost per second.")]
    [SerializeField] float hungerDrainPerSecond = 0.1f;

    [Header("Player (PlayerCoins)")]
    [Tooltip("Coins the player starts with (PlayerCoins > Starting Coins).")]
    [SerializeField] int startCoins = 20;

    [Header("Rules (GameRules)")]
    [SerializeField] bool keepInventory = false;
    [SerializeField] bool clearItemOnDead = false;
    [SerializeField] bool autoPickupDrops = false;
    [SerializeField] bool showNotifications = true;
    [Tooltip("Pause Game: the Esc pause menu stops the game while it's open.")]
    [SerializeField] bool pauseGame = true;

    [Header("Testing (GameRules)")]
    [Tooltip("Tool Test: start with one of every tier tool.")]
    [SerializeField] bool toolTest = false;
    [Tooltip("Armor Test: start with every armor piece (the Copper and Iron sets).")]
    [SerializeField] bool armorTest = false;
    [Tooltip("New Chests: the new chest system (random common and epic boxes, a legendary box, REROLL at every box).")]
    [SerializeField] bool newChests = true;

    [Header("Where these came from")]
    [Tooltip("The scene and the moment these values were read.")]
    [SerializeField] string readFrom = "";

    public const int SpawnNights = 5;   // EnemySpawner has a Spawn Interval for nights 1 - 5 (later nights use night 5's)

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
    public float HungerDrainPerSecond => hungerDrainPerSecond;
    public int StartCoins => startCoins;
    public bool KeepInventory => keepInventory;
    public bool ClearItemOnDead => clearItemOnDead;
    public bool AutoPickupDrops => autoPickupDrops;
    public bool ShowNotifications => showNotifications;
    public bool PauseGame => pauseGame;
    public bool ToolTest => toolTest;
    public bool ArmorTest => armorTest;
    public bool NewChests => newChests;
    public string ReadFrom => readFrom;

    // What happens to the items when the player dies, from the two rules.
    public GameSetup.DeathRule WhenDying => keepInventory ? GameSetup.DeathRule.KeepItems
                                          : clearItemOnDead ? GameSetup.DeathRule.LoseItems
                                          : GameSetup.DeathRule.DropItems;

    // ===== My methods =====

    // Seconds between night monsters on this night, as GameScene's EnemySpawner has them (night 6 on = night 5's).
    public float SpawnInterval(int night)
    {
        switch (night)
        {
            case 1: return night1SpawnInterval;
            case 2: return night2SpawnInterval;
            case 3: return night3SpawnInterval;
            case 4: return night4SpawnInterval;
            case 5: return night5SpawnInterval;
            default: return night > 5 ? night5SpawnInterval : 0f;   // night 6 on: night 5's, like EnemySpawner
        }
    }

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
