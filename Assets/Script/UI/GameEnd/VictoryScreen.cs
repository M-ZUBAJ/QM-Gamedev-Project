/*
First Date: 20/10/2026
Date: 20/10/2026
Author: Tony
Use by: On GameEndCanvas (Tools > UI > Build Game End Screen makes it and adds this script).
Unity Editor: Nothing to connect. It finds these children by their NAME, so keep the names:
                StatusText      = a Text (TextMeshPro or legacy) where the stats are written
                PlayAgainButton = the Button that starts the game again
              Leave GameEndCanvas switched ON in the Hierarchy: this script hides it when the game starts
              (it turns off the Canvas component, not the object, so it still hears the days go by).
              When to win = Days To Win + Win Time. The Console says the exact moment when the game starts,
              e.g. "Victory at Day 6, 12:00".
              Testing: in Play mode, click this component's ⋮ menu > "Test: Win". Or set the sun's
              Day Length Minutes to 0.5 and wait (5 days = about 2.5 minutes).
Purpose: The victory screen. When the player has survived Days To Win full days and nights and then reaches
         the next Win Time (Dawn, Morning, Midday, Afternoon, Dusk, Night or Midnight, read from DaylightCycle),
         the game freezes and this page shows what they did (PlayerStats).
         Play Again loads the scene again from the start (fresh world, full stats, Day 1).
         Dying resets the day count (DaylightCycle), so the days must be survived in one life.
Requirement: VictoryScreen.cs x-> DaylightCycle.cs, MenuState.cs, Canvas (Unity)
             VictoryScreen.cs o-> PlayerStats.cs
             VictoryScreen.cs <-x GameEndUIBuilder.cs
*/

using TMPro;            // TMP_Text (TextMeshPro text)
using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: which UI object is selected
using UnityEngine.SceneManagement;
using UnityEngine.UI;   // Button, Text

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

    [Tooltip("The first line of the stats text. {0} = Days To Win.")]
    [SerializeField] string headline = "You survived {0} days and nights!";

    // The children this script looks for. If you rename them in the Hierarchy, rename them here too.
    const string StatusName = "StatusText";
    const string ButtonName = "PlayAgainButton";

    Canvas canvas;
    TMP_Text statusTmp;     // the stats text: TextMeshPro...
    Text statusLegacy;      // ...or the old UI Text, whichever StatusText has
    Button playAgainButton;
    PlayerStats stats;      // can be missing: then only the headline shows
    bool won;

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
        }
        else Debug.LogWarning("VictoryScreen: no child named \"" + StatusName + "\". The stats won't show.", this);

        Transform button = FindChild(transform, ButtonName);
        if (button != null) playAgainButton = button.GetComponent<Button>();
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);   // no Inspector wiring needed
        else Debug.LogWarning("VictoryScreen: no Button named \"" + ButtonName + "\". The game can't restart.", this);

        canvas.enabled = false;   // hidden until the player wins
    }

    void Start()
    {
        stats = FindFirstObjectByType<PlayerStats>();

        DaylightCycle sun = DaylightCycle.Current;
        if (sun != null) Debug.Log("Victory at " + WinMomentText(sun) + ".");
        else Debug.LogWarning("VictoryScreen: no DaylightCycle in the scene, so nobody can win by surviving.", this);
    }

    // Every frame: has the game reached the winning moment yet?
    void Update()
    {
        if (won) return;

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

    // Shows the victory page and freezes the game (time stops, no moving, the mouse is free for the button).
    public void Win()
    {
        if (won) return;
        won = true;

        canvas.enabled = true;
        WriteStats();

        MenuState.SetOpen(this, true);           // the camera, building, attacking... pause
        MenuState.SetKeysCaptured(this, true);   // moving, jumping, eating, the hotbar... stop
        Cursor.lockState = CursorLockMode.None;  // free the mouse to click Play Again
        Cursor.visible = true;
        Time.timeScale = 0f;                     // everything that moves with time stops (enemies, the sun, hunger)

        Won?.Invoke();
    }

    // Loads the scene again: a fresh world, Day 1, full health / hunger / stamina, empty inventory.
    void PlayAgain()
    {
        if (!won) return;   // Space / Enter on a selected button shouldn't restart a running game
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        Time.timeScale = 1f;   // the new game must not start frozen
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
        ReloadScene();
    }

    void WriteStats()
    {
        string text = string.Format(headline, daysToWin);
        if (stats != null) text += "\n\n" + stats.Summary();

        if (statusTmp != null) statusTmp.text = text;
        else if (statusLegacy != null) statusLegacy.text = text;
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
