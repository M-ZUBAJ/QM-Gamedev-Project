/*
First Date: 11/10/2026
Date: 21/11/2026
Author: Tony
Use by: Drag this script onto DeadCanvas (the top object of the death page).
Unity Editor: Nothing to connect. It finds these children by their NAME, so keep the names:
                StatusText     = a Text (TextMeshPro or legacy) where the stats are written
                RespawnButton  = the Button that brings the player back
              and, made by Tools > UI > Upgrade Death Screen (each one can be missing):
                DaysText       = a TextMeshPro text: how long the player survived
                StatsToggle    = a Button (with a text inside) that switches between the short stats and all of them
                StatsPanel     = the "all stats" panel: LifeText (this life's numbers) and the character's stats and
                                 perks (PlayerInfoPage with Last Life on)
                MainMenuButton = the Button that goes back to the title screen
              Leave DeadCanvas switched ON in the Hierarchy: this script hides it by itself when the game starts
              (it turns off the Canvas component, not the object, so it can still hear the player die).
              Give DeadCanvas a higher Sort Order than the HUD so it covers it.
              The scene needs an EventSystem for the button (Tools > UI > Build Inventory UI makes one).
              Testing: in Play mode, click PlayerHealth's ⋮ menu > "Test: Die", then click Respawn.
Purpose: The GAME OVER page. When the player dies, closes every other open menu (MenuState.CloseAll), shows how long
         they survived ("YOU SURVIVED 2 DAYS", and the day and time they died) and what they did this life
         (PlayerStats), frees the mouse and freezes the player.
         SHOW ALL STATS swaps the short stats for everything about the character at the moment they died, like the
         inventory's Info tab (Tab): health, hunger, stamina, armor, speeds, jump, fireball, coins, with what perks and
         armor added, and every perk. MINIMISE goes back. It's remembered the moment the player dies (PlayerHealth's
         Dying event, just before dying resets the day, perks, max health, coins and armor).
         Clicking Respawn brings them back (PlayerRespawn) and hides the page again. In the new world (WorldGenerator >
         New World On Death) Respawn makes a brand-new world instead: the page stays up, its buttons off, until the
         screen fades to black and the new world loads. MAIN MENU fades to the title screen (Main Menu Scene).
Requirement: DeathScreen.cs x-> PlayerHealth.cs, PlayerRespawn.cs, MenuState.cs, ScreenFader.cs, UITween.cs, Canvas (Unity)
             DeathScreen.cs o-> PlayerStats.cs, PlayerInfoPage.cs, DaylightCycle.cs
             DeathScreen.cs <-x DeathScreenBuilder.cs
*/

using TMPro;            // TMP_Text (TextMeshPro text)
using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: which UI object is selected
using UnityEngine.UI;             // Button, Text

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class DeathScreen : MonoBehaviour
{
    // ===== Fields =====
    [Header("Main menu button")]
    [Tooltip("The scene MAIN MENU goes back to: the title screen (it must be in File > Build Settings).")]
    [SerializeField] string mainMenuScene = "MainTitleScene";

    [Header("Show all stats")]
    [Tooltip("Off: the page opens with the short stats (SHOW ALL STATS shows everything). On: it opens with everything.")]
    [SerializeField] bool startShowingAll = false;

    [Tooltip("The toggle's words while the short stats show / while all of them show.")]
    [SerializeField] string showAllText = "SHOW ALL STATS";
    [SerializeField] string minimiseText = "MINIMISE";

    // The children this script looks for. If you rename them in the Hierarchy, rename them here too.
    const string StatusName = "StatusText";
    const string ButtonName = "RespawnButton";
    const string DaysName = "DaysText";
    const string ToggleName = "StatsToggle";
    const string PanelName = "StatsPanel";
    const string LifeName = "LifeText";
    const string MainMenuName = "MainMenuButton";
    const string StatusBoxName = "Status";   // StatusText's box (FitToText): hidden while all stats show

    Canvas canvas;
    TMP_Text statusTmp;     // the stats text: TextMeshPro...
    Text statusLegacy;      // ...or the old UI Text, whichever StatusText has
    Button respawnButton;
    TMP_Text daysText;
    TMP_Text lifeText;      // this life's numbers inside the "all stats" panel
    Button toggleButton;
    TMP_Text toggleLabel;
    GameObject statsPanel;
    GameObject statusBox;   // the short stats (StatusText's box, or StatusText itself)
    CanvasGroup panelFade;
    Vector3 panelScale;     // the panel's own size (it pops in from a little smaller)
    PlayerInfoPage lastLife;   // the panel's character stats and perks
    Button mainMenuButton;

    PlayerHealth health;    // found in the scene by itself
    PlayerRespawn respawn;
    PlayerStats stats;      // can be missing: then the page shows no numbers

    bool showingAll;
    string daysWords = "";  // "YOU SURVIVED 2 DAYS ..." (remembered the moment the player dies)
    bool leaving;           // MAIN MENU was clicked: the title screen is loading

    public bool IsShown => canvas != null && canvas.enabled;

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
        }
        else Debug.LogWarning("DeathScreen: no child named \"" + StatusName + "\". The stats won't show.", this);

        respawnButton = FindButton(ButtonName);
        if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawnClicked);   // no Inspector wiring needed
        else Debug.LogWarning("DeathScreen: no Button named \"" + ButtonName + "\". The player can't respawn.", this);

        // The parts Tools > UI > Upgrade Death Screen adds (a page without them works as before).
        daysText = FindText(DaysName);
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
            lastLife = panel.GetComponentInChildren<PlayerInfoPage>(true);
        }
        mainMenuButton = FindButton(MainMenuName);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        showingAll = startShowingAll;
        canvas.enabled = false;   // hidden until the player dies
    }

    void Start()
    {
        health = FindFirstObjectByType<PlayerHealth>();
        respawn = FindFirstObjectByType<PlayerRespawn>();
        stats = FindFirstObjectByType<PlayerStats>();

        if (health == null || respawn == null)
        {
            Debug.LogWarning("DeathScreen: the Player needs PlayerHealth and PlayerRespawn.", this);
            return;
        }

        respawn.WaitForButton = true;   // we decide when to respawn now (the button), not PlayerRespawn
        health.Dying += Remember;       // first, while everything is as it was
        health.Died += Show;
    }

    // While shown, keep the numbers up to date (the last hit is counted a frame after dying).
    void Update()
    {
        if (IsShown) WriteStats();
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.Dying -= Remember;
            health.Died -= Show;
        }
        if (respawn != null) respawn.WaitForButton = false;   // without us, respawn straight away again
        MenuState.SetOpen(this, false);                       // static values outlive the scene, so clear them
        MenuState.SetKeysCaptured(this, false);
    }

    // ===== My methods =====

    // The moment of death, just before dying resets the day, the perks, max health, the coins and drops the armor:
    // what the page will show of the life that just ended.
    void Remember()
    {
        daysWords = DaysWords();
        if (lastLife != null) lastLife.Capture();
    }

    void Show()
    {
        MenuState.CloseAll();   // any menu still open closes first, so the death page is the only one (and keeps the mouse)
        canvas.enabled = true;
        WriteStats();
        if (daysText != null) daysText.text = daysWords;
        ShowAll(showingAll, false);
        SetButtons(true);

        MenuState.SetOpen(this, true);          // the camera, building, levitating... pause
        MenuState.SetKeysCaptured(this, true);  // moving, jumping, eating, the hotbar... stop
        Cursor.lockState = CursorLockMode.None; // free the mouse to click Respawn
        Cursor.visible = true;
    }

    void OnRespawnClicked()
    {
        // Only while the page is showing. (Unity's UI also "clicks" the selected button when Space or Enter is pressed,
        // so without this, jumping after respawning could press Respawn again and teleport the player back.)
        if (!IsShown || leaving) return;

        Deselect();

        // The new world makes a brand-new world instead (PlayerRespawn.StartsNewWorld): this page stays up, its buttons
        // switched off, until the screen has faded to black and the new world loads.
        if (respawn != null && respawn.StartsNewWorld)
        {
            leaving = true;
            SetButtons(false);
            respawn.Respawn();
            return;
        }

        if (respawn != null) respawn.Respawn();

        canvas.enabled = false;
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
        Cursor.lockState = CursorLockMode.Locked;   // back to aiming
        Cursor.visible = false;
    }

    // Back to the title screen: the screen fades to black and the title scene loads (the page stays up meanwhile, its
    // buttons off, so nothing can be clicked twice).
    void OnMainMenuClicked()
    {
        if (!IsShown || leaving) return;
        leaving = true;
        Deselect();
        SetButtons(false);
        Time.timeScale = 1f;   // the title screen must not start frozen
        ScreenFader.LoadScene(mainMenuScene, "", "");
    }

    void OnToggleClicked()
    {
        if (!IsShown) return;
        Deselect();
        ShowAll(!showingAll, true);
    }

    // All stats (the panel) or the short ones (the stats box). animate = the panel pops in.
    void ShowAll(bool all, bool animate)
    {
        showingAll = all && statsPanel != null;
        if (statusBox != null) statusBox.SetActive(!showingAll);
        if (toggleLabel != null) toggleLabel.text = showingAll ? minimiseText : showAllText;
        if (statsPanel == null) return;

        statsPanel.SetActive(showingAll);
        Transform panel = statsPanel.transform;
        if (!showingAll || !animate)
        {
            panel.localScale = panelScale;
            if (panelFade != null) panelFade.alpha = 1f;
            return;
        }
        panel.localScale = panelScale * 0.95f;
        UITween.Scale(this, panel, panelScale, 0.25f);
        if (panelFade == null) return;
        panelFade.alpha = 0f;
        UITween.Fade(this, panelFade, 1f, 0.18f);
    }

    void SetButtons(bool on)
    {
        if (respawnButton != null) respawnButton.interactable = on;
        if (mainMenuButton != null) mainMenuButton.interactable = on;
        if (toggleButton != null) toggleButton.interactable = on;
    }

    // Forget the clicked button, so Space / Enter can't press it again later.
    static void Deselect()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void WriteStats()
    {
        string text = stats != null ? stats.Summary() : "";
        if (statusTmp != null) statusTmp.text = text;
        else if (statusLegacy != null) statusLegacy.text = text;
        if (lifeText != null && lifeText.text != text) lifeText.text = text;
    }

    // "YOU SURVIVED 2 DAYS" and, smaller, "and 6 hours · died on Day 3 at 14:30" (game time, from the start of this
    // life). Empty without a DaylightCycle.
    static string DaysWords()
    {
        DaylightCycle sun = DaylightCycle.Current;
        if (sun == null) return "";

        float survived = Mathf.Max(0f, sun.DaysSurvived);
        int days = Mathf.FloorToInt(survived);
        int hours = Mathf.FloorToInt((survived - days) * 24f);
        string big = days > 0 ? "YOU SURVIVED " + Count(days, "DAY", "DAYS")
                   : hours > 0 ? "YOU SURVIVED " + Count(hours, "HOUR", "HOURS")
                   : "YOU SURVIVED LESS THAN AN HOUR";
        string small = (days > 0 && hours > 0 ? "and " + Count(hours, "hour", "hours") + "  ·  " : "") +
                       "died on Day " + sun.Day + " at " + sun.ClockText;
        return big + "\n<size=60%>" + small + "</size>";
    }

    static string Count(int amount, string one, string many) => amount + " " + (amount == 1 ? one : many);

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
}
