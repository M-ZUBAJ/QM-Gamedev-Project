/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on an object: every script can read it straight away (MouseSettings.Multiplier). The Settings >
        CONTROLS page (title screen and the Esc pause menu) changes it through ControlsMenu.
Unity Editor: Nothing to set. Testing: Play, press Esc, open CONTROLS, drag Mouse sensitivity: the camera turns faster or
              slower at once. Stop and Play again: the choice is kept.
Purpose: The player's mouse choices, saved on the computer (PlayerPrefs, like the key bindings), so they work in every
         scene and the next time the game starts:
           - Sensitivity: 1 - 10 like many games, 5 = the camera's own speed (CameraController > Sensitivity). The
             camera multiplies its speed by Multiplier = Sensitivity / 5 (1 = a fifth as fast, 10 = twice as fast).
           - Invert Y: moving the mouse up looks down (like a plane's controls).
         Changed tells listening scripts when either changes. Save writes them to the computer for good (the pages
         call it when they close, so dragging the slider doesn't write the file every frame).
Requirement: MouseSettings.cs <-x CameraController.cs, ControlsMenu.cs, PauseMenu.cs, TitleSceneBuilder.cs
*/

using UnityEngine;

public static class MouseSettings
{
    // ===== Fields =====
    public const float MinSensitivity = 1f;
    public const float MaxSensitivity = 10f;
    public const float DefaultSensitivity = 5f;   // = the camera's own speed (Multiplier 1)

    const string SensitivityName = "Mouse_Sensitivity";   // the names they're saved under
    const string InvertName = "Mouse_InvertY";

    static bool loaded;
    static float sensitivity = DefaultSensitivity;
    static bool invertY;

    // Fired when Sensitivity or Invert Y changes (e.g. a settings page shows the new value).
    public static event System.Action Changed;

    // The sensitivity the player chose, 1 - 10 (one decimal).
    public static float Sensitivity { get { Load(); return sensitivity; } }

    // The camera's speed times this: 5 = 1 (as set on the camera), 10 = 2, 1 = 0.2.
    public static float Multiplier => Sensitivity / DefaultSensitivity;

    // True: moving the mouse up looks down.
    public static bool InvertY { get { Load(); return invertY; } }

    // ===== My methods =====

    // Sets the sensitivity (kept within 1 - 10, rounded to one decimal). Call Save to keep it for next time.
    public static void SetSensitivity(float value)
    {
        Load();
        value = Mathf.Clamp(Mathf.Round(value * 10f) / 10f, MinSensitivity, MaxSensitivity);
        if (Mathf.Approximately(value, sensitivity)) return;
        sensitivity = value;
        PlayerPrefs.SetFloat(SensitivityName, sensitivity);
        Changed?.Invoke();
    }

    public static void SetInvertY(bool on)
    {
        Load();
        if (on == invertY) return;
        invertY = on;
        PlayerPrefs.SetInt(InvertName, on ? 1 : 0);
        Changed?.Invoke();
    }

    // Back to 5 and not inverted, saved.
    public static void ResetToDefaults()
    {
        Load();
        sensitivity = DefaultSensitivity;
        invertY = false;
        PlayerPrefs.DeleteKey(SensitivityName);
        PlayerPrefs.DeleteKey(InvertName);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Writes the choices to the computer for good (PlayerPrefs only keeps them in memory until then; the web build
    // needs this to remember them).
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // Reads the saved choices the first time they're needed.
    static void Load()
    {
        if (loaded) return;
        loaded = true;
        sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityName, DefaultSensitivity), MinSensitivity, MaxSensitivity);
        invertY = PlayerPrefs.GetInt(InvertName, 0) == 1;
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        loaded = false;
        Changed = null;
    }
}
