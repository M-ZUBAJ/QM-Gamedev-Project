/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Drag this script onto a row with a child "Slider" (a Unity Slider), a "Value" text (TextMeshPro) and,
        optionally, "Minus" and "Plus" Buttons. E.g. Settings > CONTROLS > Mouse sensitivity (Tools > UI > Build Main
        Title Scene and Tools > UI > Build Pause Menu make it).
Unity Editor: Min / Max = the lowest and highest value, Step = how much - and + change it, Decimals = how it's written
              (1 = "5.5"). Testing: Play, drag the slider or click - and +.
Purpose: A number on a slider: drag it (it moves in steps of a tenth with Decimals 1), or click - / + for bigger steps.
         The number next to it pops when it changes, and - / + fade out at the lowest / highest value.
         Other scripts read Value, set it with SetValue(...) and hear about every change with Changed.
Requirement: SliderRow.cs x-> UITween.cs, Slider (Unity)
             SliderRow.cs <-x ControlsMenu.cs, TitleSceneBuilder.cs
*/

using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SliderRow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The lowest value.")]
    [SerializeField] float min = 1f;

    [Tooltip("The highest value.")]
    [SerializeField] float max = 10f;

    [Tooltip("How much - and + change it.")]
    [Min(0.01f)]
    [SerializeField] float step = 0.5f;

    [Tooltip("Decimals in the number (1 = \"5.5\"). The slider moves in the same steps (a tenth with 1).")]
    [Range(0, 3)]
    [SerializeField] int decimals = 1;

    [Tooltip("The value it starts with (other scripts usually set it, e.g. from the saved settings).")]
    [SerializeField] float value = 5f;

    Slider slider;
    TMP_Text valueText;
    Button minus, plus;
    CanvasGroup minusFade, plusFade;

    // Fired with the new value when the player changes it (dragging, - or +), not when a script sets it.
    public event Action<float> Changed;

    public float Value => value;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        slider = FindPart<Slider>("Slider");
        valueText = FindPart<TMP_Text>("Value");
        minus = FindPart<Button>("Minus");
        plus = FindPart<Button>("Plus");
        minusFade = Fader(minus);
        plusFade = Fader(plus);

        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(OnSlider);
        }
        if (minus != null) minus.onClick.AddListener(() => Nudge(-step));
        if (plus != null) plus.onClick.AddListener(() => Nudge(step));
        Show(false);
    }

    // ===== My methods =====

    // Sets the value from a script (e.g. the saved setting). notify = also fire Changed.
    public void SetValue(float newValue, bool notify = false)
    {
        newValue = Round(newValue);
        bool different = !Mathf.Approximately(newValue, value);
        value = newValue;
        if (slider != null) slider.SetValueWithoutNotify(value);
        Show(false);
        if (notify && different) Changed?.Invoke(value);
    }

    // The slider was dragged (or clicked along its bar).
    void OnSlider(float dragged)
    {
        float newValue = Round(dragged);
        if (Mathf.Approximately(newValue, value)) return;
        value = newValue;
        Show(true);
        Changed?.Invoke(value);
    }

    // - or + : a step down or up (the slider follows).
    void Nudge(float by)
    {
        float newValue = Round(value + by);
        if (Mathf.Approximately(newValue, value)) return;
        value = newValue;
        if (slider != null) slider.SetValueWithoutNotify(value);
        Show(true);
        Changed?.Invoke(value);
    }

    // Writes the number (it pops when the player changed it) and fades - / + at the ends.
    void Show(bool pop)
    {
        if (valueText != null)
        {
            valueText.text = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (pop && isActiveAndEnabled)
            {
                Transform t = valueText.transform;
                UITween.Run(this, 0.25f, UITween.Ease.OutBack, k => t.localScale = Vector3.one * Mathf.LerpUnclamped(1.25f, 1f, k));
            }
        }
        SetUsable(minus, minusFade, value > min + 0.0001f);
        SetUsable(plus, plusFade, value < max - 0.0001f);
    }

    static void SetUsable(Button button, CanvasGroup fade, bool usable)
    {
        if (button == null) return;
        button.interactable = usable;
        if (fade != null) fade.alpha = usable ? 1f : 0.35f;
    }

    // Kept within Min - Max, in steps of the last decimal (Decimals 1 = tenths).
    float Round(float v)
    {
        float scale = Mathf.Pow(10f, decimals);
        return Mathf.Clamp(Mathf.Round(v * scale) / scale, min, max);
    }

    static CanvasGroup Fader(Button button)
    {
        if (button == null) return null;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        return group != null ? group : button.gameObject.AddComponent<CanvasGroup>();
    }

    T FindPart<T>(string partName) where T : Component
    {
        Transform part = FindDeep(transform, partName);
        return part != null ? part.GetComponent<T>() : null;
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
