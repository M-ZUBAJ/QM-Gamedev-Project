/*
First Date: 7/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). The New Game page fills it; it works by itself in the game.
Unity Editor: Nothing to set. Pressing Play straight in GameScene (testing) changes nothing: the choices are only used
              after START GAME on the title screen.
Purpose: Remembers the player's New Game choices and puts them into the game when it loads, before anything in it
         starts. Only the options the player CHANGED are used; everything left on its default stays exactly as
         GameScene has it. Play Again keeps the same choices.
           Days to win, Win at         -> VictoryScreen.SetGoal
           Day length, Start time      -> DaylightCycle.SetDayLength / SetStartTime
           Difficulty                  -> EnemySpawner's spawnAtNight and Night 1 - 5 Spawn Interval (Marko's public
                                          settings: Easy = monsters half as often, Hard = 1.5 times as often),
                                          NightMonsters.SetLimits on Easy (at most 20 alive at once, far unseen ones
                                          give up) and PlayerHunger.SetDrainPerSecond
           When I die, pickups, "+3 Wood" messages, the pause menu pausing the game -> GameRules
           Testing: start with all tools           -> GameRules Tool Test (ToolTestKit gives the 15 tier tools)
           Testing: start with all armor           -> GameRules Armor Test (ArmorTestKit gives the Copper and
                                                      Iron sets)
           Testing: Old chests / New chests        -> GameRules New Chests (ChestTier: the two chest systems)
           Map: Old world / New world              -> NewWorld: TitleMenu loads GameScene or ProceduralGenerateScene
                                                      (WorldGenerator: a new random world every game). New world is
                                                      the page's default since 23/11/2026, so NewGamePanel always
                                                      sets it (true or false)
           Starting coins                          -> PlayerCoins.SetStartCoins (Marko's script, Tony's [Tony] lines):
                                                      the coins now, and what dying puts them back to
         Other scripts can read what was chosen, e.g. if (GameSetup.ChosenDifficulty == GameSetup.Difficulty.Hard).
Requirement: GameSetup.cs x-> GameRules.cs, VictoryScreen.cs, DaylightCycle.cs, EnemySpawner.cs, NightMonsters.cs, PlayerHunger.cs, PlayerCoins.cs
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
    public static float? SpawnIntervalScale;   // Difficulty: the seconds between night monsters x this (2 = half as many)
    public static int? MonstersAtOnce;         // Difficulty (Easy): the most night monsters alive at once (0 = no limit)
    public static float? MonsterLeaveDistance; // Difficulty (Easy): unseen night monsters this far away give up (0 = never)
    public static float? HungerDrainPerSecond;
    public static DeathRule? WhenDying;
    public static bool? AutoPickupDrops;
    public static bool? ShowNotifications;
    public static bool? PauseGame;             // Rules: the Esc pause menu stops the game (GameRules > Pause Game)
    public static bool? StartWithAllTools;     // Testing: one of every tier tool from the start
    public static bool? StartWithAllArmor;     // Testing: every armor piece (the Copper and Iron sets) from the start
    public static bool? NewWorld;              // Map: true = the new world (a new random one every game), false / null = the old
    public static bool? NewChests;             // Testing: true = the new chest system, false = the old one
    public static int? StartCoins;             // Difficulty: the coins the player starts with (PlayerCoins > Starting Coins)

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
        SpawnIntervalScale = null;
        MonstersAtOnce = null;
        MonsterLeaveDistance = null;
        HungerDrainPerSecond = null;
        WhenDying = null;
        AutoPickupDrops = null;
        ShowNotifications = null;
        PauseGame = null;
        StartWithAllTools = null;
        StartWithAllArmor = null;
        NewWorld = null;
        NewChests = null;
        StartCoins = null;
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

            // Easy / Hard: how often the monsters come. Marko's spawner has no "enemies per night" any more: it spawns
            // one monster every few seconds all night (his Night 1 - 5 Spawn Interval), and he commented out the old
            // enemiesPerNight line here. So the difficulty stretches or shortens those seconds (x2 = half as many
            // monsters). The scene's own numbers load fresh with the scene, so Play Again doesn't stretch them twice.
            if (SpawnIntervalScale.HasValue)
            {
                float scale = SpawnIntervalScale.Value;
                spawner.night1SpawnInterval = Stretched(spawner.night1SpawnInterval, scale);
                spawner.night2SpawnInterval = Stretched(spawner.night2SpawnInterval, scale);
                spawner.night3SpawnInterval = Stretched(spawner.night3SpawnInterval, scale);
                spawner.night4SpawnInterval = Stretched(spawner.night4SpawnInterval, scale);
                spawner.night5SpawnInterval = Stretched(spawner.night5SpawnInterval, scale);
            }
        }

        // Easy: at most 20 night monsters at once, and far unseen ones give up (Tony's NightMonsters, next to the
        // spawner). Normal and Hard leave both off: the monsters gather until dawn, as Marko made it.
        if (MonstersAtOnce.HasValue || MonsterLeaveDistance.HasValue)
        {
            foreach (NightMonsters night in Object.FindObjectsByType<NightMonsters>(FindObjectsSortMode.None))
                night.SetLimits(MonstersAtOnce ?? night.MostAtOnce, MonsterLeaveDistance ?? night.LeaveDistance);
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
        if (PauseGame.HasValue) GameRules.Set(GameRule.PauseGame, PauseGame.Value);
        if (StartWithAllTools.HasValue) GameRules.Set(GameRule.ToolTest, StartWithAllTools.Value);
        if (StartWithAllArmor.HasValue) GameRules.Set(GameRule.ArmorTest, StartWithAllArmor.Value);
        if (NewChests.HasValue) GameRules.Set(GameRule.NewChests, NewChests.Value);

        if (StartCoins.HasValue)
        {
            PlayerCoins coins = Object.FindFirstObjectByType<PlayerCoins>();
            if (coins != null) coins.SetStartCoins(StartCoins.Value);   // before anything starts: no "+80 Coins" message
        }
    }

    // Seconds between night monsters x scale, never under 0.1 (the smallest Marko's spawner allows). The New Game page
    // uses it too, to show the same seconds the game will have.
    public static float Stretched(float seconds, float scale)
    {
        return Mathf.Max(0.1f, seconds * scale);
    }
}
