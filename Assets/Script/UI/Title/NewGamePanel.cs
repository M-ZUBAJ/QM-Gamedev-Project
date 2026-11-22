/*
First Date: 7/11/2026
Date: 22/11/2026
Author: Tony
Use by: Put this on the New Game page (Tools > UI > Build Main Title Scene does it: TitleCanvas > Pages > NewGamePage).
Unity Editor: Defaults = Assets/Config/NewGame/NewGameDefaults.asset (the game as it is now; the tool fills it in).
              It finds its rows by NAME, so keep the names: DaysRow (StepperRow), WinAtRow and StartTimeRow (each with a
              "Dropdown"), DayLengthRow, DifficultyRow, DeathRow (ChoiceRows), PickupRow and MessagesRow (each with a
              "Toggle"), and the texts DayLengthRow/Info, DifficultyInfo, Summary, plus DefaultsButton.
              AllToolsRow and ArmorTestRow (each with a "Toggle", side by side in the Testing section) are optional: an
              older title scene without them still works. So is PauseRow (a "Toggle", beside MessagesRow).
              WorldRow (a ChoiceRow: Old world / New world, with an "Info" text) is optional too: without it the game
              always starts in the old world. So is ChestRow (a ChoiceRow: Old chests / New chests, in the Testing
              section): without it the game keeps GameScene's GameRules > New Chests. So is CoinsRow (a StepperRow whose
              number is an Input Field you can type in, under Difficulty): without it the game keeps PlayerCoins >
              Starting Coins.
              Each row can have a small "Dot" picture: it pops up when that option differs from the game as it is now.
              Testing: Play, click PLAY, change the options; DEFAULTS puts them all back.
Purpose: The New Game options. Every option starts at the game as it is now (the defaults). Changed options get a dot,
         the line under the panel says what the goal is, and DEFAULTS puts everything back.
         Commit() hands only the CHANGED options to GameSetup, which puts them into the game when it loads.
           Map                      = Old world (GameScene, the hand-made map) or New world (ProceduralGenerateScene:
                                      a new random endless world every game, WorldGenerator)
           Days to win / Win at     = when the player wins (VictoryScreen)
           Day length               = Short / Normal / Long (half / as now / double the day length)
           Start time               = when the first day starts
           Difficulty               = Peaceful (no enemies), Easy, Normal (as now), Hard: enemies per night and hunger
           Starting coins           = the coins the player starts with, and goes back to after dying (PlayerCoins),
                                      0 - 9999: - / + change it by 10, or click the number and type one (default 20)
           When I die               = Keep items, Drop items or Lose items
           Pickups / messages       = GameRules Auto Pickup Drops / Show Notifications
           Pause menu pauses        = GameRules Pause Game: the Esc pause menu stops the game (on by default)
           All tools (Testing)      = GameRules Tool Test: start with one of every tier tool (off by default)
           All armor (Testing)      = GameRules Armor Test: start with every armor piece, the Copper and Iron sets
                                      (off by default)
           Chests (Testing)         = GameRules New Chests: Old chests (every box: choose 1 of 3) or New chests (random
                                      common and epic boxes, a legendary box, REROLL everywhere: the default)
Requirement: NewGamePanel.cs x-> NewGameDefaults.cs, GameSetup.cs, StepperRow.cs, ChoiceRow.cs, UITween.cs, TMP_Dropdown (Unity)
             NewGamePanel.cs <-x NewGameDefaultsSync.cs, TitleMenu.cs, TitleSceneBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NewGamePanel : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The game as it is now (Assets/Config/NewGame/NewGameDefaults.asset): every option starts here.")]
    [SerializeField] NewGameDefaults defaults;

    [Header("Difficulty (compared to Normal = the game as it is now)")]
    [Range(0f, 1f)]
    [Tooltip("Easy: enemies per night = Normal's x this (at least 1). 0.5 = half.")]
    [SerializeField] float easyEnemies = 0.5f;

    [Range(1f, 3f)]
    [Tooltip("Hard: enemies per night = Normal's x this. 1.5 = half as many again.")]
    [SerializeField] float hardEnemies = 1.5f;

    [Range(0.25f, 1f)]
    [Tooltip("Easy: hunger drains this many times as fast (0.7 = slower).")]
    [SerializeField] float easyHunger = 0.7f;

    [Range(1f, 3f)]
    [Tooltip("Hard: hunger drains this many times as fast (1.5 = faster).")]
    [SerializeField] float hardHunger = 1.5f;

    [Header("Day length (compared to Normal = the game as it is now)")]
    [Range(0.1f, 1f)]
    [Tooltip("Short day = Normal's length x this.")]
    [SerializeField] float shortDay = 0.5f;

    [Range(1f, 5f)]
    [Tooltip("Long day = Normal's length x this.")]
    [SerializeField] float longDay = 2f;

    // The row names it looks for. If you rename them in the Hierarchy, rename them here too.
    static readonly string[] RowNames =
        { "DaysRow", "WinAtRow", "DayLengthRow", "StartTimeRow", "DifficultyRow", "DeathRow", "PickupRow", "MessagesRow",
          "AllToolsRow", "WorldRow", "ArmorTestRow", "ChestRow", "PauseRow", "CoinsRow" };   // the same order as Refresh's "changed"
    const int NormalDay = 1;   // Short, Normal, Long
    const int OldWorld = 0;    // Map: Old world (as now), New world
    const int NewWorld = 1;
    const int OldChests = 0;   // Testing: Old chests, New chests
    const int NewChests = 1;
    const string AllDefault = "Everything is set to the game as it is now.";

    StepperRow days;
    TMP_Dropdown winAt, startAt;
    ChoiceRow dayLength, difficulty, death;
    Toggle pickup, messages;
    Toggle allTools;           // Testing: start with all tools (null in an older title scene without the row)
    Toggle allArmor;           // Testing: start with all armor (null in an older title scene without the row)
    ChoiceRow world;           // Map: Old world / New world (null in an older title scene without the row)
    ChoiceRow chests;          // Testing: Old chests / New chests (null in an older title scene without the row)
    Toggle pauseGame;          // Rules: the pause menu pauses the game (null in an older title scene without the row)
    StepperRow coins;          // Difficulty: starting coins (null in an older title scene without the row)
    TMP_Text dayLengthInfo, difficultyInfo, summary, worldInfo;
    Button defaultsButton;
    readonly Dictionary<string, RectTransform> dots = new Dictionary<string, RectTransform>();   // row name -> its Dot
    readonly List<float> startTimes = new List<float>();   // the Time Of Day of each Start time option
    int defaultStart;          // which Start time option is the game as it is now
    bool ready;

    // The chosen days to win (e.g. for the "DAY 1 - Survive 5 nights" card).
    public int DaysToWin => days != null ? days.Value : defaults != null ? defaults.DaysToWin : 5;

    // ===== Unity messages (Unity calls these) =====

    // Start (not Awake): the rows' own scripts have woken up by now.
    void Start()
    {
        if (defaults == null)
        {
            Debug.LogWarning("NewGamePanel: Defaults is empty. Run Tools > UI > Build Main Title Scene.", this);
            return;
        }

        FindRows();
        FillOptions();
        ShowChoices();
        Listen();
        ready = true;
        Refresh(false);
    }

    // ===== My methods =====

    // Puts every option back to the game as it is now.
    public void SetDefaults()
    {
        if (!ready) return;

        days.SetValue(defaults.DaysToWin, false);
        winAt.value = (int)defaults.WinTime;
        dayLength.Pick(NormalDay, false);
        startAt.value = defaultStart;
        difficulty.Pick((int)GameSetup.Difficulty.Normal, false);
        death.Pick((int)defaults.WhenDying, false);
        pickup.isOn = defaults.AutoPickupDrops;
        messages.isOn = defaults.ShowNotifications;
        if (allTools != null) allTools.isOn = defaults.ToolTest;
        if (allArmor != null) allArmor.isOn = defaults.ArmorTest;
        if (world != null) world.Pick(OldWorld, false);
        if (chests != null) chests.Pick(defaults.NewChests ? NewChests : OldChests, false);
        if (pauseGame != null) pauseGame.isOn = defaults.PauseGame;
        if (coins != null) coins.SetValue(defaults.StartCoins, false);
        Refresh(true);
    }

    // Hands the changed options to GameSetup (unchanged ones stay null = "as the game is").
    public void Commit()
    {
        GameSetup.Clear();
        if (!ready) return;

        if (days.Value != defaults.DaysToWin) GameSetup.DaysToWin = days.Value;
        if (winAt.value != (int)defaults.WinTime) GameSetup.WinAt = (WinTime)winAt.value;
        if (dayLength.Picked != NormalDay) GameSetup.DayLengthMinutes = DayMinutes(dayLength.Picked);
        if (startAt.value != defaultStart) GameSetup.StartTime = startTimes[startAt.value];

        GameSetup.Difficulty level = (GameSetup.Difficulty)difficulty.Picked;
        if (level != GameSetup.Difficulty.Normal)
        {
            GameSetup.ChosenDifficulty = level;
            GameSetup.SpawnEnemies = level != GameSetup.Difficulty.Peaceful;
            GameSetup.EnemiesPerNight = EnemiesFor(level);
            GameSetup.HungerDrainPerSecond = defaults.HungerDrainPerSecond * HungerFor(level);
        }

        if (death.Picked != (int)defaults.WhenDying) GameSetup.WhenDying = (GameSetup.DeathRule)death.Picked;
        if (pickup.isOn != defaults.AutoPickupDrops) GameSetup.AutoPickupDrops = pickup.isOn;
        if (messages.isOn != defaults.ShowNotifications) GameSetup.ShowNotifications = messages.isOn;
        if (allTools != null && allTools.isOn != defaults.ToolTest) GameSetup.StartWithAllTools = allTools.isOn;
        if (allArmor != null && allArmor.isOn != defaults.ArmorTest) GameSetup.StartWithAllArmor = allArmor.isOn;
        if (world != null && world.Picked == NewWorld) GameSetup.NewWorld = true;
        if (chests != null && (chests.Picked == NewChests) != defaults.NewChests) GameSetup.NewChests = chests.Picked == NewChests;
        if (pauseGame != null && pauseGame.isOn != defaults.PauseGame) GameSetup.PauseGame = pauseGame.isOn;
        if (coins != null && coins.Value != defaults.StartCoins) GameSetup.StartCoins = coins.Value;
    }

    void FindRows()
    {
        days = FindDeep(transform, "DaysRow")?.GetComponent<StepperRow>();
        winAt = FindDeep(FindDeep(transform, "WinAtRow"), "Dropdown")?.GetComponent<TMP_Dropdown>();
        startAt = FindDeep(FindDeep(transform, "StartTimeRow"), "Dropdown")?.GetComponent<TMP_Dropdown>();
        dayLength = FindDeep(transform, "DayLengthRow")?.GetComponent<ChoiceRow>();
        difficulty = FindDeep(transform, "DifficultyRow")?.GetComponent<ChoiceRow>();
        death = FindDeep(transform, "DeathRow")?.GetComponent<ChoiceRow>();
        pickup = FindDeep(FindDeep(transform, "PickupRow"), "Toggle")?.GetComponent<Toggle>();
        messages = FindDeep(FindDeep(transform, "MessagesRow"), "Toggle")?.GetComponent<Toggle>();
        allTools = FindDeep(FindDeep(transform, "AllToolsRow"), "Toggle")?.GetComponent<Toggle>();
        allArmor = FindDeep(FindDeep(transform, "ArmorTestRow"), "Toggle")?.GetComponent<Toggle>();
        world = FindDeep(transform, "WorldRow")?.GetComponent<ChoiceRow>();
        worldInfo = FindDeep(FindDeep(transform, "WorldRow"), "Info")?.GetComponent<TMP_Text>();
        chests = FindDeep(transform, "ChestRow")?.GetComponent<ChoiceRow>();
        pauseGame = FindDeep(FindDeep(transform, "PauseRow"), "Toggle")?.GetComponent<Toggle>();
        coins = FindDeep(transform, "CoinsRow")?.GetComponent<StepperRow>();
        dayLengthInfo = FindDeep(FindDeep(transform, "DayLengthRow"), "Info")?.GetComponent<TMP_Text>();
        difficultyInfo = FindDeep(transform, "DifficultyInfo")?.GetComponent<TMP_Text>();
        summary = FindDeep(transform, "Summary")?.GetComponent<TMP_Text>();
        defaultsButton = FindDeep(transform, "DefaultsButton")?.GetComponent<Button>();

        foreach (string row in RowNames)
        {
            if (FindDeep(FindDeep(transform, row), "Dot") is RectTransform dot)
            {
                dots[row] = dot;
                dot.localScale = Vector3.zero;   // hidden until that option changes
            }
        }

        if (days == null || winAt == null || startAt == null || dayLength == null || difficulty == null ||
            death == null || pickup == null || messages == null)
        {
            Debug.LogWarning("NewGamePanel: some rows are missing or renamed. Run Tools > UI > Build Main Title Scene.", this);
            enabled = false;
        }
    }

    // The dropdowns' option texts, with the clock times of this game, e.g. "Dawn (05:31)".
    void FillOptions()
    {
        if (!enabled) return;

        List<string> winTexts = new List<string>();
        foreach (WinTime w in System.Enum.GetValues(typeof(WinTime)))
            winTexts.Add(w + " (" + NewGameDefaults.Clock(defaults.TimeOf(w)) + ")");
        winAt.ClearOptions();
        winAt.AddOptions(winTexts);

        // Start time: the same moments, plus "Now" first if the game starts at another time.
        List<string> startTexts = new List<string>();
        startTimes.Clear();
        defaultStart = -1;
        foreach (WinTime w in System.Enum.GetValues(typeof(WinTime)))
        {
            float t = defaults.TimeOf(w);
            if (defaultStart < 0 && Mathf.Abs(Mathf.DeltaAngle(t * 360f, defaults.StartTime * 360f)) < 0.5f)
                defaultStart = startTimes.Count;
            startTexts.Add(w + " (" + NewGameDefaults.Clock(t) + ")");
            startTimes.Add(t);
        }
        if (defaultStart < 0)
        {
            startTexts.Insert(0, "Now (" + NewGameDefaults.Clock(defaults.StartTime) + ")");
            startTimes.Insert(0, defaults.StartTime);
            defaultStart = 0;
        }
        startAt.ClearOptions();
        startAt.AddOptions(startTexts);
    }

    // Shows the choices made earlier this session, or the defaults (nothing chosen = the game as it is).
    void ShowChoices()
    {
        if (!enabled) return;

        days.SetValue(GameSetup.DaysToWin ?? defaults.DaysToWin, false, false);
        winAt.SetValueWithoutNotify((int)(GameSetup.WinAt ?? defaults.WinTime));
        dayLength.Pick(GameSetup.DayLengthMinutes.HasValue ? ClosestDay(GameSetup.DayLengthMinutes.Value) : NormalDay, false);
        startAt.SetValueWithoutNotify(GameSetup.StartTime.HasValue ? ClosestStart(GameSetup.StartTime.Value) : defaultStart);
        difficulty.Pick((int)(GameSetup.ChosenDifficulty ?? GameSetup.Difficulty.Normal), false);
        death.Pick((int)(GameSetup.WhenDying ?? defaults.WhenDying), false);
        pickup.SetIsOnWithoutNotify(GameSetup.AutoPickupDrops ?? defaults.AutoPickupDrops);
        messages.SetIsOnWithoutNotify(GameSetup.ShowNotifications ?? defaults.ShowNotifications);
        if (allTools != null) allTools.SetIsOnWithoutNotify(GameSetup.StartWithAllTools ?? defaults.ToolTest);
        if (allArmor != null) allArmor.SetIsOnWithoutNotify(GameSetup.StartWithAllArmor ?? defaults.ArmorTest);
        if (world != null) world.Pick(GameSetup.NewWorld == true ? NewWorld : OldWorld, false);
        if (chests != null) chests.Pick((GameSetup.NewChests ?? defaults.NewChests) ? NewChests : OldChests, false);
        if (pauseGame != null) pauseGame.SetIsOnWithoutNotify(GameSetup.PauseGame ?? defaults.PauseGame);
        if (coins != null) coins.SetValue(GameSetup.StartCoins ?? defaults.StartCoins, false, false);
    }

    void Listen()
    {
        if (!enabled) return;

        days.Changed += _ => Refresh(true);
        winAt.onValueChanged.AddListener(_ => Refresh(true));
        startAt.onValueChanged.AddListener(_ => Refresh(true));
        dayLength.Changed += _ => Refresh(true);
        difficulty.Changed += _ => Refresh(true);
        death.Changed += _ => Refresh(true);
        pickup.onValueChanged.AddListener(_ => Refresh(true));
        messages.onValueChanged.AddListener(_ => Refresh(true));
        if (allTools != null) allTools.onValueChanged.AddListener(_ => Refresh(true));
        if (allArmor != null) allArmor.onValueChanged.AddListener(_ => Refresh(true));
        if (world != null) world.Changed += _ => Refresh(true);
        if (chests != null) chests.Changed += _ => Refresh(true);
        if (pauseGame != null) pauseGame.onValueChanged.AddListener(_ => Refresh(true));
        if (coins != null) coins.Changed += _ => Refresh(true);
        if (defaultsButton != null) defaultsButton.onClick.AddListener(SetDefaults);
    }

    // Updates the dots, the info texts, the summary and the DEFAULTS button after any change.
    void Refresh(bool animate)
    {
        if (!ready || !enabled) return;

        bool[] changed =
        {
            days.Value != defaults.DaysToWin,
            winAt.value != (int)defaults.WinTime,
            dayLength.Picked != NormalDay,
            startAt.value != defaultStart,
            difficulty.Picked != (int)GameSetup.Difficulty.Normal,
            death.Picked != (int)defaults.WhenDying,
            pickup.isOn != defaults.AutoPickupDrops,
            messages.isOn != defaults.ShowNotifications,
            allTools != null && allTools.isOn != defaults.ToolTest,
            world != null && world.Picked != OldWorld,
            allArmor != null && allArmor.isOn != defaults.ArmorTest,
            chests != null && (chests.Picked == NewChests) != defaults.NewChests,
            pauseGame != null && pauseGame.isOn != defaults.PauseGame,
            coins != null && coins.Value != defaults.StartCoins,
        };

        bool any = false;
        for (int i = 0; i < RowNames.Length; i++)
        {
            any |= changed[i];
            if (dots.TryGetValue(RowNames[i], out RectTransform dot)) ShowDot(dot, changed[i], animate);
        }

        if (dayLengthInfo != null) dayLengthInfo.text = Mathf.RoundToInt(DayMinutes(dayLength.Picked)) + " min";
        if (worldInfo != null && world != null) worldInfo.text = world.Picked == NewWorld ? "new random map" : "the map as it is";
        SetText(difficultyInfo, DifficultyText((GameSetup.Difficulty)difficulty.Picked), animate);
        SetText(summary, any ? GoalText() : AllDefault, animate);
        if (defaultsButton != null) defaultsButton.interactable = any;
    }

    // The dot pops up (or shrinks away) next to a changed option.
    void ShowDot(RectTransform dot, bool show, bool animate)
    {
        bool shown = dot.localScale.x > 0.5f;
        if (show == shown) return;

        if (!animate) { dot.localScale = show ? Vector3.one : Vector3.zero; return; }
        UITween.Scale(this, dot, show ? Vector3.one : Vector3.zero, show ? 0.35f : 0.18f,
                      show ? UITween.Ease.OutBack : UITween.Ease.InCubic);
    }

    // Changes a text with a quick fade out and back in.
    void SetText(TMP_Text text, string value, bool animate)
    {
        if (text == null || text.text == value) return;
        if (!animate) { text.text = value; return; }

        UITween.Run(this, 0.12f, UITween.Ease.Linear, t => text.alpha = 1f - t, () =>
        {
            text.text = value;
            UITween.Run(this, 0.2f, UITween.Ease.OutCubic, t => text.alpha = t);
        });
    }

    string GoalText()
    {
        int n = days.Value;
        string where = world != null && world.Picked == NewWorld ? "In a new random world: survive " : "Survive ";
        return where + n + (n == 1 ? " day and night" : " days and nights") +
               ", then win at " + winAt.options[winAt.value].text + " on day " + (n + 1) + ".";
    }

    string DifficultyText(GameSetup.Difficulty level)
    {
        bool enemies = level == GameSetup.Difficulty.Peaceful ? false
                     : level == GameSetup.Difficulty.Normal ? defaults.SpawnAtNight : true;
        int count = EnemiesFor(level);
        string enemyText = enemies && count > 0 ? count + (count == 1 ? " enemy" : " enemies") + " every night"
                                                : "No enemies at night";
        float hunger = HungerFor(level);
        string hungerText = hunger < 1f ? "slower hunger" : hunger > 1f ? "faster hunger" : "normal hunger";
        return enemyText + ", " + hungerText;
    }

    int EnemiesFor(GameSetup.Difficulty level)
    {
        int normal = defaults.EnemiesPerNight;
        switch (level)
        {
            case GameSetup.Difficulty.Peaceful: return 0;
            case GameSetup.Difficulty.Easy: return Mathf.Max(1, Mathf.RoundToInt(normal * easyEnemies));
            case GameSetup.Difficulty.Hard: return Mathf.Max(normal + 1, Mathf.RoundToInt(normal * hardEnemies));
            default: return normal;
        }
    }

    float HungerFor(GameSetup.Difficulty level)
    {
        switch (level)
        {
            case GameSetup.Difficulty.Easy: return easyHunger;
            case GameSetup.Difficulty.Hard: return hardHunger;
            default: return 1f;   // Peaceful keeps normal hunger: no enemies, but food still matters
        }
    }

    // Minutes in a day for Short (0), Normal (1) or Long (2).
    float DayMinutes(int pick)
    {
        float normal = defaults.DayLengthMinutes;
        return pick == 0 ? normal * shortDay : pick == 2 ? normal * longDay : normal;
    }

    int ClosestDay(float minutes)
    {
        int best = NormalDay;
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(DayMinutes(i) - minutes) < Mathf.Abs(DayMinutes(best) - minutes)) best = i;
        }
        return best;
    }

    int ClosestStart(float time)
    {
        int best = defaultStart;
        for (int i = 0; i < startTimes.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(startTimes[i] * 360f, time * 360f)) <
                Mathf.Abs(Mathf.DeltaAngle(startTimes[best] * 360f, time * 360f))) best = i;
        }
        return best;
    }

    // A child (or grandchild...) with this name, or null.
    static Transform FindDeep(Transform parent, string childName)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
