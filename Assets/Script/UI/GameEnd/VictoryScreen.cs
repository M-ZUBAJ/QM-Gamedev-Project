/*
First Date: 20/10/2026
Date: 22/11/2026
Author: Tony
Use by: On GameEndCanvas (Tools > UI > Build Game End Screen makes it and adds this script).
Unity Editor: Nothing to connect. It finds these children by their NAME, so keep the names:
                StatusText      = a Text (TextMeshPro or legacy) where the short stats are written
                PlayAgainButton = the Button that starts the game again
              and, made by Tools > UI > Build Game End Screen since 22/11/2026 (each one can be missing):
                TrophyIcon      = the trophy picture in the middle at the top (TrophyGlow and TrophyRays behind it)
                DaysText        = a TextMeshPro text: how long the player survived and when they won
                CoinsText       = a TextMeshPro text: coins left, earned and spent
                StatsToggle     = a Button (with a text inside): MINIMISE / SHOW ALL STATS
                StatsPanel      = all stats, like the inventory's Info tab: LifeText (this life's numbers), the
                                  character's stats and perks (PlayerInfoPage)
                MainMenuButton  = the Button that goes back to the title screen
              Leave GameEndCanvas switched ON in the Hierarchy: this script hides it when the game starts
              (it turns off the Canvas component, not the object, so it still hears the days go by).
              When to win = Days To Win + Win Time. The Console says the exact moment when the game starts,
              e.g. "Victory at Day 6, 12:00".
              Testing: in Play mode, click this component's ⋮ menu > "Test: Win". Or set the sun's
              Day Length Minutes to 0.5 and wait (5 days = about 2.5 minutes).
Purpose: The victory page. When the player has survived Days To Win full days and nights and then reaches
         the next Win Time (Dawn, Morning, Midday, Afternoon, Dusk, Night or Midnight, read from DaylightCycle),
         the game freezes and this page shows:
           - a gold trophy in the middle that pops in with sparks, floats a little, its light rays turning slowly;
           - how long they survived and when they won ("5 DAYS AND NIGHTS", "won on Day 6 at 05:31");
           - their coins: how many are left, how many this life earned and spent (on chests, rerolls...);
           - all stats, like the inventory's Info tab (Tab): this life's numbers (PlayerStats), health, hunger, stamina,
             armor, speeds, jump, fireball, coins, what perks and armor added, and every perk. MINIMISE swaps them for
             the short numbers, SHOW ALL STATS brings them back.
         Every open menu closes first (e.g. a chest's perk panel, the inventory: MenuState.CloseAll), so only this page
         is left. Play Again loads the scene again from the start (fresh world, full stats, Day 1). Main Menu fades to
         the title screen (Main Menu Scene).
         Dying resets the day count (DaylightCycle), so the days must be survived in one life.
         The New Game page can change Days To Win and Win Time before the game starts (SetGoal, used by GameSetup).
Requirement: VictoryScreen.cs x-> DaylightCycle.cs, MenuState.cs, ScreenFader.cs, UITween.cs, Canvas (Unity)
             VictoryScreen.cs o-> PlayerStats.cs, PlayerCoins.cs, UISparks.cs
             VictoryScreen.cs <-x GameEndUIBuilder.cs, GameSetup.cs, NewGameDefaults.cs, NewGameDefaultsSync.cs, NewGamePanel.cs
*/

using System.Globalization;       // CultureInfo: "1,250" the same on every computer
using System.Text;                // StringBuilder: the short stats in two columns
using TMPro;                      // TMP_Text (TextMeshPro text)
using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: which UI object is selected
using UnityEngine.SceneManagement;
using UnityEngine.UI;             // Button, Image, Text

// The moment of the day the player wins at (after surviving Days To Win days). One at a time (a dropdown).
public enum WinTime
{
    Dawn,        // the night ends, a new day starts (DaylightCycle's Dawn Start, 05:31)
    Morning,     // DaylightCycle's Morning Start (07:12)
    Midday,      // 12:00 exactly
    Afternoon,   // DaylightCycle's Afternoon Start (13:12)
    Dusk,        // DaylightCycle's Dusk Start (16:48)
    Night,       // night starts (DaylightCycle's Night Start, 18:28)
    Midnight,    // 00:00, in the middle of the night
}

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class VictoryScreen : MonoBehaviour
{
    // ===== Fields =====
    [Min(1)]
    [Tooltip("How many full days and nights the player must survive (counted from Day 1's dawn). " +
             "Then they win at the next Win Time. 5 + Dawn = when Day 6 starts.")]
    [SerializeField] int daysToWin = 5;

    [Tooltip("When the player wins, after surviving Days To Win days: e.g. Dawn = Day 6 05:31, Midday = Day 6 12:00, " +
             "Night = Day 6 18:28, Midnight = the middle of Day 6's night (00:00).")]
    [SerializeField] WinTime winTime = WinTime.Dawn;

    [Tooltip("Only for a page without DaysText (built before 22/11/2026): the first line of the stats text. " +
             "{0} = Days To Win. The page built now shows the days under the title instead.")]
    [SerializeField] string headline = "You survived {0} days and nights!";

    [Header("Main menu button")]
    [Tooltip("The scene Main Menu goes back to: the title screen (it must be in File > Build Settings).")]
    [SerializeField] string mainMenuScene = "MainTitleScene";

    [Header("Show all stats")]
    [Tooltip("On: the page opens with all stats (like the inventory's Info tab) and MINIMISE shows the short numbers. " +
             "Off: it opens with the short numbers and SHOW ALL STATS shows everything.")]
    [SerializeField] bool startShowingAll = true;

    [Tooltip("The toggle's words while the short numbers show / while all stats show.")]
    [SerializeField] string showAllText = "SHOW ALL STATS";
    [SerializeField] string minimiseText = "MINIMISE";

    [Tooltip("The short numbers in two columns side by side (half as tall). Needs a wide stats box like the one " +
             "Tools > UI > Build Game End Screen makes (it ticks this); a narrow box would make the columns overlap.")]
    [SerializeField] bool twoColumns = false;

    [Header("Coins")]
    [Tooltip("The color of \"earned 120\" in the coins line.")]
    [SerializeField] Color earnedColor = new Color(0.62f, 0.92f, 0.55f);
    [Tooltip("The color of \"spent 95\" in the coins line.")]
    [SerializeField] Color spentColor = new Color(1f, 0.66f, 0.55f);

    [Header("Trophy")]
    [Tooltip("How fast the light rays behind the trophy turn, in degrees per second. 0 = still.")]
    [Range(0f, 90f)]
    [SerializeField] float raysTurnSpeed = 12f;

    [Tooltip("How far the trophy floats up and down, in pixels (of a 1080 pixel high screen). 0 = it stays still.")]
    [Range(0f, 30f)]
    [SerializeField] float floatHeight = 6f;

    [Tooltip("How many sparks fly out when the trophy pops in. 0 = none.")]
    [Range(0, 80)]
    [SerializeField] int sparks = 36;

    // The children this script looks for. If you rename them in the Hierarchy, rename them here too.
    const string StatusName = "StatusText";
    const string ButtonName = "PlayAgainButton";
    const string StatusBoxName = "Status";   // StatusText's box (FitToText): hidden while all stats show
    const string DaysName = "DaysText";
    const string CoinsName = "CoinsText";
    const string ToggleName = "StatsToggle";
    const string PanelName = "StatsPanel";
    const string LifeName = "LifeText";
    const string MainMenuName = "MainMenuButton";
    const string TrophyName = "TrophyIcon";
    const string GlowName = "TrophyGlow";
    const string RaysName = "TrophyRays";

    const float RewriteEvery = 0.5f;   // real seconds: the numbers are written again now and then while the page shows
    static readonly Color SparkColor = new Color(1f, 0.82f, 0.3f);   // the trophy's sparks: gold

    Canvas canvas;
    TMP_Text statusTmp;       // the short stats: TextMeshPro...
    Text statusLegacy;        // ...or the old UI Text, whichever StatusText has
    GameObject statusBox;     // StatusText's box (or StatusText itself): the short stats
    Vector3 statusScale;
    Button playAgainButton;
    PlayerStats stats;        // can be missing: then only the days show

    // The parts Tools > UI > Build Game End Screen adds since 22/11/2026 (each can be missing).
    TMP_Text daysText;
    TMP_Text coinsText;
    TMP_Text lifeText;        // this life's numbers inside the "all stats" panel
    Button toggleButton;
    TMP_Text toggleLabel;
    GameObject statsPanel;
    CanvasGroup panelFade;
    Vector3 panelScale;       // the panel's own size (it pops in from a little smaller)
    Button mainMenuButton;
    RectTransform trophy, glow, rays;
    Image glowImage;
    Vector2 trophyHome;       // where the trophy floats around
    Vector3 trophyScale, glowScale, raysScale;
    float glowAlpha;

    bool won;
    bool showingAll;
    bool leaving;             // Play Again or Main Menu was clicked: the next scene is loading
    float nextRewrite;
    float shownAt;            // real time the page appeared (the trophy's animations count from here)

    // Fired once when the player wins (e.g. stop spawning enemies, play music).
    public static event System.Action Won;

    public bool HasWon => won;
    public int DaysToWin => daysToWin;
    public WinTime WinTime => winTime;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        Transform status = FindChild(transform, StatusName);
        if (status != null)
        {
            statusTmp = status.GetComponent<TMP_Text>();
            statusLegacy = status.GetComponent<Text>();
            statusBox = status.parent != null && status.parent.name == StatusBoxName ? status.parent.gameObject : status.gameObject;
            statusScale = statusBox.transform.localScale;
        }
        else Debug.LogWarning("VictoryScreen: no child named \"" + StatusName + "\". The stats won't show.", this);

        playAgainButton = FindButton(ButtonName);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);   // no Inspector wiring needed
        else Debug.LogWarning("VictoryScreen: no Button named \"" + ButtonName + "\". The game can't restart.", this);

        // The parts Tools > UI > Build Game End Screen adds (a page without them works as before).
        daysText = FindText(DaysName);
        coinsText = FindText(CoinsName);
        lifeText = FindText(LifeName);
        toggleButton = FindButton(ToggleName);
        if (toggleButton != null)
        {
            toggleLabel = toggleButton.GetComponentInChildren<TMP_Text>(true);
            toggleButton.onClick.AddListener(OnToggleClicked);
        }
        Transform panel = FindChild(transform, PanelName);
        if (panel != null)
        {
            statsPanel = panel.gameObject;
            panelFade = panel.GetComponent<CanvasGroup>();
            panelScale = panel.localScale;
        }
        mainMenuButton = FindButton(MainMenuName);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        trophy = FindChild(transform, TrophyName) as RectTransform;
        glow = FindChild(transform, GlowName) as RectTransform;
        rays = FindChild(transform, RaysName) as RectTransform;
        if (trophy != null)
        {
            trophyHome = trophy.anchoredPosition;
            trophyScale = trophy.localScale;
        }
        if (glow != null)
        {
            glowScale = glow.localScale;
            glowImage = glow.GetComponent<Image>();
            if (glowImage != null) glowAlpha = glowImage.color.a;
        }
        if (rays != null) raysScale = rays.localScale;

        showingAll = startShowingAll;
        canvas.enabled = false;   // hidden until the player wins
    }

    void Start()
    {
        stats = FindFirstObjectByType<PlayerStats>();

        DaylightCycle sun = DaylightCycle.Current;
        if (sun != null) Debug.Log("Victory at " + WinMomentText(sun) + ".");
        else Debug.LogWarning("VictoryScreen: no DaylightCycle in the scene, so nobody can win by surviving.", this);
    }

    // Every frame: has the game reached the winning moment yet? Once won: the trophy moves, the numbers stay up to date.
    void Update()
    {
        if (won)
        {
            AnimateTrophy();
            if (Time.unscaledTime >= nextRewrite) WriteNumbers();   // e.g. a coin counted in the same frame as the win
            return;
        }

        DaylightCycle sun = DaylightCycle.Current;
        if (sun != null && DaysSurvived(sun) >= WinPoint(sun)) Win();
    }

    void OnDestroy()
    {
        if (won) Time.timeScale = 1f;            // never leave the game frozen (e.g. the scene is closed)
        MenuState.SetOpen(this, false);          // static values outlive the scene, so clear them
        MenuState.SetKeysCaptured(this, false);
        Won = null;
    }

    // ===== My methods =====

    // How long the player has survived, in days, counted from Day 1's dawn: e.g. 5.5 = 5 full days and half a day.
    // A new day starts at Dawn, so Day 3 at dawn = 2.0, and Day 3 just before the next dawn = almost 3.0.
    static float DaysSurvived(DaylightCycle sun)
    {
        float intoTheDay = Mathf.Repeat(sun.TimeOfDay - sun.DawnStart, 1f);   // 0 at dawn, almost 1 just before the next
        return (sun.Day - 1) + intoTheDay;
    }

    // The winning point on the same scale: Days To Win full days, plus how far after dawn the Win Time is.
    float WinPoint(DaylightCycle sun)
    {
        return daysToWin + Mathf.Repeat(WinTimeOfDay(sun) - sun.DawnStart, 1f);
    }

    // The Win Time as a Time Of Day (0 = midnight, 0.5 = midday), from the sun's own Parts Of The Day.
    float WinTimeOfDay(DaylightCycle sun)
    {
        switch (winTime)
        {
            case WinTime.Morning:   return sun.MorningStart;
            case WinTime.Midday:    return 0.5f;
            case WinTime.Afternoon: return sun.AfternoonStart;
            case WinTime.Dusk:      return sun.DuskStart;
            case WinTime.Night:     return sun.NightStart;
            case WinTime.Midnight:  return 0f;
            default:                return sun.DawnStart;   // Dawn
        }
    }

    // E.g. "Day 6, 12:00 (Midday)", for the Console.
    string WinMomentText(DaylightCycle sun)
    {
        float point = WinPoint(sun);
        int day = Mathf.FloorToInt(point) + 1;   // the day number DaylightCycle shows at that moment
        float t = WinTimeOfDay(sun);
        int hour = Mathf.FloorToInt(t * 24f) % 24;
        int minute = Mathf.FloorToInt(t * 24f * 60f) % 60;
        return "Day " + day + ", " + hour.ToString("00") + ":" + minute.ToString("00") + " (" + winTime + ")";
    }

    // Changes when the player wins (e.g. chosen on the New Game page). Best called before the game starts,
    // so the Console's "Victory at ..." line is right.
    public void SetGoal(int days, WinTime time)
    {
        daysToWin = Mathf.Max(1, days);
        winTime = time;
    }

    // Shows the victory page and freezes the game (time stops, no moving, the mouse is free for the buttons).
    public void Win()
    {
        if (won) return;
        won = true;

        MenuState.CloseAll();                    // close every other menu first (a chest's perk panel, the inventory...)
        canvas.enabled = true;
        if (daysText != null) daysText.text = DaysWords();
        WriteNumbers();
        ShowAll(showingAll, true);
        SetButtons(true);
        PopTrophy();

        MenuState.SetOpen(this, true);           // the camera, building, attacking... pause
        MenuState.SetKeysCaptured(this, true);   // moving, jumping, eating, the hotbar... stop
        Cursor.lockState = CursorLockMode.None;  // free the mouse to click the buttons
        Cursor.visible = true;
        Time.timeScale = 0f;                     // everything that moves with time stops (enemies, the sun, hunger)

        Won?.Invoke();
    }

    // Loads the scene again: a fresh world, Day 1, full health / hunger / stamina, empty inventory.
    void PlayAgain()
    {
        if (!won || leaving) return;   // Space / Enter on a selected button shouldn't restart a running game
        leaving = true;
        Deselect();

        Time.timeScale = 1f;   // the new game must not start frozen
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
        ReloadScene();
    }

    // Back to the title screen: the screen fades to black and the title scene loads (the page stays up meanwhile, its
    // buttons off, so nothing can be clicked twice).
    void OnMainMenuClicked()
    {
        if (!won || leaving) return;
        leaving = true;
        Deselect();
        SetButtons(false);
        Time.timeScale = 1f;   // the title screen must not start frozen
        ScreenFader.LoadScene(mainMenuScene, "", "");
    }

    void OnToggleClicked()
    {
        if (!won || leaving) return;
        Deselect();
        ShowAll(!showingAll, true);
    }

    // All stats (the panel) or the short numbers (the stats box). animate = the one shown pops in.
    void ShowAll(bool all, bool animate)
    {
        showingAll = all && statsPanel != null;
        if (toggleLabel != null) toggleLabel.text = showingAll ? minimiseText : showAllText;
        if (statusBox != null)
        {
            statusBox.SetActive(!showingAll);
            if (!showingAll) Pop(statusBox.transform, statusScale, null, animate);
        }
        if (statsPanel == null) return;
        statsPanel.SetActive(showingAll);
        if (showingAll) Pop(statsPanel.transform, panelScale, panelFade, animate);
    }

    // Grows "part" from a little smaller to its own size and fades it in (or just puts it at its size).
    void Pop(Transform part, Vector3 size, CanvasGroup fade, bool animate)
    {
        part.localScale = animate ? size * 0.95f : size;
        if (animate) UITween.Scale(this, part, size, 0.25f);
        if (fade == null) return;
        fade.alpha = animate ? 0f : 1f;
        if (animate) UITween.Fade(this, fade, 1f, 0.18f);
    }

    void SetButtons(bool on)
    {
        if (playAgainButton != null) playAgainButton.interactable = on;
        if (mainMenuButton != null) mainMenuButton.interactable = on;
        if (toggleButton != null) toggleButton.interactable = on;
    }

    // Forget the clicked button, so Space / Enter can't press it again later.
    static void Deselect()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // "5 DAYS AND NIGHTS" and, smaller, "won on Day 6 at 05:31" (the game's clock at the moment of winning).
    string DaysWords()
    {
        string big = daysToWin + (daysToWin == 1 ? " DAY AND NIGHT" : " DAYS AND NIGHTS");
        DaylightCycle sun = DaylightCycle.Current;
        if (sun == null) return big;
        return big + "\n<size=60%>won on Day " + sun.Day + " at " + sun.ClockText + "</size>";
    }

    void WriteNumbers()
    {
        nextRewrite = Time.unscaledTime + RewriteEvery;
        WriteStats();
        WriteCoins();
    }

    // The short numbers (the stats box) and the same lines in the panel's THIS LIFE column.
    void WriteStats()
    {
        string summary = stats != null ? stats.Summary() : "";
        string text = twoColumns && statusTmp != null ? TwoColumns(summary) : summary;   // the old UI Text can't do columns
        if (daysText == null)   // a page built before 22/11/2026: the days go first, in the stats text
            text = string.Format(headline, daysToWin) + (summary.Length > 0 ? "\n\n" + text : "");

        if (statusTmp != null) SetText(statusTmp, text);
        else if (statusLegacy != null) statusLegacy.text = text;
        SetText(lifeText, summary);
    }

    // "45 coins left   earned 120   spent 95": what's in the purse now (Marko's PlayerCoins), and what this life earned
    // and spent (PlayerStats: chests, rerolls...).
    void WriteCoins()
    {
        if (coinsText == null) return;
        PlayerCoins purse = PlayerCoins.Instance;
        int left = purse != null ? purse.Coins : 0;
        string text = "<b>" + Whole(left) + "</b> " + (left == 1 ? "coin" : "coins") + " left";
        if (stats != null)
        {
            text += "    <color=#" + ColorUtility.ToHtmlStringRGB(earnedColor) + ">earned " + Whole(stats.CoinsEarned) + "</color>" +
                    "    <color=#" + ColorUtility.ToHtmlStringRGB(spentColor) + ">spent " + Whole(stats.CoinsSpent) + "</color>";
        }
        SetText(coinsText, text);
    }

    // The lines side by side in two columns: the first half on the left, the rest on the right (TextMeshPro's
    // <pos=50%> starts the right column in the middle of the box), so the box is half as tall.
    static string TwoColumns(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string[] lines = text.Split('\n');
        int half = (lines.Length + 1) / 2;
        StringBuilder both = new StringBuilder();
        for (int i = 0; i < half; i++)
        {
            if (i > 0) both.Append('\n');
            both.Append(lines[i]);
            if (i + half < lines.Length) both.Append("<pos=50%>").Append(lines[i + half]);
        }
        return both.ToString();
    }

    // The trophy pops in from small (a little past its size, then back), its rays open out, and sparks fly out of it.
    void PopTrophy()
    {
        shownAt = Time.unscaledTime;
        if (rays != null)
        {
            rays.localScale = raysScale * 0.5f;
            UITween.Scale(this, rays, raysScale, 0.9f, UITween.Ease.OutCubic);
        }
        if (trophy == null) return;
        trophy.localScale = trophyScale * 0.2f;
        UITween.Scale(this, trophy, trophyScale, 0.6f, UITween.Ease.OutBack);
        if (sparks > 0) UISparks.Burst(trophy, SparkColor, sparks);
    }

    // While the page shows (real time: the game is frozen): the rays turn, the trophy floats up and down every 2
    // seconds, the glow behind it breathes.
    void AnimateTrophy()
    {
        float t = Time.unscaledTime - shownAt;
        if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -t * raysTurnSpeed);
        if (trophy != null) trophy.anchoredPosition = trophyHome + Vector2.up * (Mathf.Sin(t * Mathf.PI) * floatHeight);
        if (glow == null) return;
        float wave = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 1.2f);   // 0 -> 1 -> 0, smoothly
        glow.localScale = glowScale * (1f + 0.08f * wave);
        if (glowImage == null) return;
        Color c = glowImage.color;
        c.a = glowAlpha * Mathf.Lerp(0.7f, 1f, wave);
        glowImage.color = c;
    }

    // The open scene, from the start. A scene that isn't in Build Settings can still be reloaded while testing
    // in the Unity Editor (in a built game every scene is in Build Settings).
    static void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("VictoryScreen: add the scene to File > Build Settings so it can be loaded again.");
#endif
    }

    // A whole number with commas: 1250 -> "1,250".
    static string Whole(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value) text.text = value;
    }

    TMP_Text FindText(string childName)
    {
        Transform child = FindChild(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    Button FindButton(string childName)
    {
        Transform child = FindChild(transform, childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    // Finds a child (or grandchild...) by name, anywhere under "parent". null if there is none.
    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform deeper = FindChild(child, childName);
            if (deeper != null) return deeper;
        }
        return null;
    }

    // Click the component's ⋮ menu in the Inspector to run this (Play mode).
    [ContextMenu("Test: Win")]
    void TestWin()
    {
        Win();
    }
}
