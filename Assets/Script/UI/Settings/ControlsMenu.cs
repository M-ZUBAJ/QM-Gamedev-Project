/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Put this on the CONTROLS page (Tools > UI > Build Main Title Scene does it on the title screen's Settings page,
        Tools > UI > Build Pause Menu in the Esc pause menu). It finds by NAME: "SensitivityRow" (a SliderRow),
        "InvertRow" (with a Toggle inside) and, optionally, "ResetControlsButton" and a "Hint" text.
Unity Editor: Nothing to connect. Testing: Play, open CONTROLS, drag Mouse sensitivity and tick Invert mouse; in the game
              the camera follows at once. The choices are saved (MouseSettings), so they're kept for next time.
Purpose: The mouse settings: Mouse sensitivity (1 - 10, 5 = the game's normal speed) and Invert mouse (up / down).
         Changing them changes MouseSettings straight away (the camera reads it every frame); they're written to the
         computer when the page closes. RESET puts both back (5, not inverted).
Requirement: ControlsMenu.cs x-> MouseSettings.cs, SliderRow.cs, Toggle (Unity)
             ControlsMenu.cs <-x TitleSceneBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ControlsMenu : MonoBehaviour
{
    // ===== Fields =====
    const string SensitivityName = "SensitivityRow";
    const string InvertName = "InvertRow";
    const string ResetName = "ResetControlsButton";
    const string HintName = "Hint";
    const string NormalHint = "5 = the game's normal speed. Changes work at once and are saved.";

    SliderRow sensitivity;
    Toggle invert;
    TMP_Text hint;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Transform row = FindDeep(transform, SensitivityName);
        sensitivity = row != null ? row.GetComponent<SliderRow>() : null;
        if (sensitivity != null) sensitivity.Changed += OnSensitivity;
        else Debug.LogWarning("ControlsMenu: no SliderRow named \"" + SensitivityName + "\".", this);

        Transform invertRow = FindDeep(transform, InvertName);
        invert = invertRow != null ? invertRow.GetComponentInChildren<Toggle>(true) : null;
        if (invert != null) invert.onValueChanged.AddListener(MouseSettings.SetInvertY);

        Transform reset = FindDeep(transform, ResetName);
        Button resetButton = reset != null ? reset.GetComponent<Button>() : null;
        if (resetButton != null) resetButton.onClick.AddListener(ResetAll);

        Transform hintPart = FindDeep(transform, HintName);
        hint = hintPart != null ? hintPart.GetComponent<TMP_Text>() : null;
    }

    void OnEnable()
    {
        MouseSettings.Changed += Refresh;
        Refresh();
        if (hint != null) hint.text = NormalHint;
    }

    // The page closed (another tab, the menu closed, the scene changed): the choices are written to the computer.
    void OnDisable()
    {
        MouseSettings.Changed -= Refresh;
        MouseSettings.Save();
    }

    // ===== My methods =====

    // Shows the saved choices (without firing the controls' own change events).
    void Refresh()
    {
        if (sensitivity != null) sensitivity.SetValue(MouseSettings.Sensitivity);
        if (invert != null) invert.SetIsOnWithoutNotify(MouseSettings.InvertY);
    }

    void OnSensitivity(float value)
    {
        MouseSettings.SetSensitivity(value);
    }

    void ResetAll()
    {
        MouseSettings.ResetToDefaults();
        if (hint != null) hint.text = "Back to sensitivity 5, not inverted.";
    }

    // A child (or grandchild...) with this name, or null.
    static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
