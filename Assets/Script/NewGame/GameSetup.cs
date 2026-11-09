/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). The New Game page fills it; it works by itself in the game.
Unity Editor: Nothing to set. Pressing Play straight in GameScene (testing) changes nothing: the choices are only used
              after START GAME on the title screen.
Purpose: Remembers the player's New Game choices and puts them into the game when it loads, before anything in it
         starts. Only the options the player CHANGED are used; everything left on its default stays exactly as
         GameScene has it. Play Again keeps the same choices.
           Days to win, Win at         -> VictoryScreen.SetGoal
           Day length, Start time      -> DaylightCycle.SetDayLength / SetStartTime
           Difficulty                  -> EnemySpawner's spawnAtNight / enemiesPerNight (Marko's public settings,
                                          his file isn't changed) and PlayerHunger.SetDrainPerSecond
           When I die, pickups, "+3 Wood" messages -> GameRules
         Other scripts can read what was chosen, e.g. if (GameSetup.ChosenDifficulty == GameSetup.Difficulty.Hard).
Requirement: GameSetup.cs x-> GameRules.cs, VictoryScreen.cs, DaylightCycle.cs, EnemySpawner.cs, PlayerHunger.cs
             GameSetup.cs <-x NewGameDefaults.cs, NewGamePanel.cs, TitleMenu.cs
*/

using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSetup
{
    // What happens to the player's items when they die.
    public enum DeathRule
    {
        KeepItems,   // GameRules Keep Inventory on
        DropItems,   // both off: everything drops where the player died
        LoseItems,   // GameRules Clear Item On Dead on: everything is deleted
    }

    public enum Difficulty { Peaceful, Easy, Normal, Hard }

    // ===== Fields =====
    // The choices. null = not changed: the game keeps what GameScene has.
    public static int? DaysToWin;
    public static WinTime? WinAt;
    public static float? DayLengthMinutes;
    public static float? StartTime;            // Time Of Day, 0 - 1 (0.5 = midday)
    public static Difficulty? ChosenDifficulty;
    public static bool? SpawnEnemies;
    public static int? EnemiesPerNight;
    public static float? HungerDrainPerSecond;
    public static DeathRule? WhenDying;
    public static bool? AutoPickupDrops;
    public static bool? ShowNotifications;

    static bool chosen;   // START GAME was pressed: use the choices in every game scene that loads (Play Again too)

    public static bool IsChosen => chosen;

    // ===== My methods =====

    // Called by START GAME: from now on, every game scene that loads gets the choices.
    public static void Choose()
    {
        chosen = true;
    }

    // Forgets every choice (back to "the game as it is").
    public static void Clear()
    {
        DaysToWin = null;
        WinAt = null;
        DayLengthMinutes = null;
        StartTime = null;
        ChosenDifficulty = null;
        SpawnEnemies = null;
        EnemiesPerNight = null;
        HungerDrainPerSecond = null;
        WhenDying = null;
        AutoPickupDrops = null;
        ShowNotifications = null;
        chosen = false;
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time, and listen for scenes.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unity calls this after a scene's objects woke up (Awake) but before they start (Start).
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (chosen) Apply();
    }

    // Puts the changed choices into the game that just loaded.
    static void Apply()
    {
        DaylightCycle sun = Object.FindFirstObjectByType<DaylightCycle>();
        VictoryScreen victory = Object.FindFirstObjectByType<VictoryScreen>(FindObjectsInactive.Include);
        if (sun == null && victory == null) return;   // not a game scene (e.g. the title screen)

        if (victory != null && (DaysToWin.HasValue || WinAt.HasValue))
            victory.SetGoal(DaysToWin ?? victory.DaysToWin, WinAt ?? victory.WinTime);

        if (sun != null)
        {
            if (DayLengthMinutes.HasValue) sun.SetDayLength(DayLengthMinutes.Value);
            if (StartTime.HasValue) sun.SetStartTime(StartTime.Value);
        }

        foreach (EnemySpawner spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
        {
            if (SpawnEnemies.HasValue) spawner.spawnAtNight = SpawnEnemies.Value;
            if (EnemiesPerNight.HasValue) spawner.enemiesPerNight = EnemiesPerNight.Value;
        }

        if (HungerDrainPerSecond.HasValue)
        {
            PlayerHunger hunger = Object.FindFirstObjectByType<PlayerHunger>();
            if (hunger != null) hunger.SetDrainPerSecond(HungerDrainPerSecond.Value);
        }

        if (WhenDying.HasValue)
        {
            GameRules.Set(GameRule.KeepInventory, WhenDying.Value == DeathRule.KeepItems);
            GameRules.Set(GameRule.ClearItemOnDead, WhenDying.Value == DeathRule.LoseItems);
        }
        if (AutoPickupDrops.HasValue) GameRules.Set(GameRule.AutoPickupDrops, AutoPickupDrops.Value);
        if (ShowNotifications.HasValue) GameRules.Set(GameRule.ShowNotifications, ShowNotifications.Value);
    }
}
