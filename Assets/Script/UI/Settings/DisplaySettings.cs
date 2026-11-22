/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on an object: every script can read it straight away (DisplaySettings.ShowDamage). The DISPLAY page
        (the title screen's Settings and the Esc pause menu) changes it through DisplayMenu.
Unity Editor: Nothing to set. Testing: Play, press Esc, open DISPLAY, untick Show damage numbers: hits show no numbers.
Purpose: The player's display choices, saved on the computer (PlayerPrefs, like the key bindings and mouse settings):
           - Show damage numbers (on): a number pops up where a hit lands (DamageNumbers);
           - Only monsters and animals (off): with it on, hits on trees, rocks and ores show no number. It belongs to
             Show damage numbers: it only counts while that's on (the DISPLAY page locks it while it's off);
           - Fullscreen: the game's screen fills the monitor (Screen.fullScreen; the browser decides, so it isn't saved).
             Alt + Enter or F11 switch it too (FullscreenKeys).
         Changed tells listening scripts when a choice changes. Save writes them to the computer for good (the pages call
         it when they close).
Requirement: DisplaySettings.cs <-x DamageNumbers.cs, DisplayMenu.cs, FullscreenKeys.cs
*/

using UnityEngine;

public static class DisplaySettings
{
    // ===== Fields =====
    const string ShowDamageName = "Display_ShowDamage";   // the names they're saved under
    const string OnlyCreaturesName = "Display_DamageOnlyCreatures";

    static bool loaded;
    static bool showDamage = true;
    static bool onlyCreatures;

    // Fired when Show damage numbers or Only monsters and animals changes (not for fullscreen: the browser switches that).
    public static event System.Action Changed;

    // True: hits show their damage as a number.
    public static bool ShowDamage { get { Load(); return showDamage; } }

    // True: only hits on monsters and animals show a number (not trees, rocks or ores). Only counts while ShowDamage is on.
    public static bool DamageOnlyCreatures { get { Load(); return onlyCreatures; } }

    // Whether a hit on this kind of target shows its number right now.
    public static bool ShowsDamageOn(bool creature) => ShowDamage && (creature || !DamageOnlyCreatures);

    // True while the game fills the screen.
    public static bool Fullscreen => Screen.fullScreen;

    // ===== My methods =====

    public static void SetShowDamage(bool on)
    {
        Load();
        if (on == showDamage) return;
        showDamage = on;
        PlayerPrefs.SetInt(ShowDamageName, on ? 1 : 0);
        Changed?.Invoke();
    }

    public static void SetDamageOnlyCreatures(bool on)
    {
        Load();
        if (on == onlyCreatures) return;
        onlyCreatures = on;
        PlayerPrefs.SetInt(OnlyCreaturesName, on ? 1 : 0);
        Changed?.Invoke();
    }

    // Fills the screen or not. In the web build the browser does it after this click or key press (it only allows
    // fullscreen in answer to the player), and Esc leaves it.
    public static void SetFullscreen(bool on)
    {
        if (Screen.fullScreen != on) Screen.fullScreen = on;
    }

    public static void ToggleFullscreen()
    {
        SetFullscreen(!Screen.fullScreen);
    }

    // Damage numbers back on, for every hit; saved.
    public static void ResetToDefaults()
    {
        Load();
        showDamage = true;
        onlyCreatures = false;
        PlayerPrefs.DeleteKey(ShowDamageName);
        PlayerPrefs.DeleteKey(OnlyCreaturesName);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Writes the choices to the computer for good (the web build needs this to remember them).
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // Reads the saved choices the first time they're needed.
    static void Load()
    {
        if (loaded) return;
        loaded = true;
        showDamage = PlayerPrefs.GetInt(ShowDamageName, 1) == 1;
        onlyCreatures = PlayerPrefs.GetInt(OnlyCreaturesName, 0) == 1;
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        loaded = false;
        Changed = null;
    }
}
