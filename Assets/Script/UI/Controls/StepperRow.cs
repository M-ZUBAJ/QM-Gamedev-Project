/*
First Date: 7/11/2026
Date: 22/11/2026
Author: Tony
Use by: Drag this script onto a row with children named "Minus" and "Plus" (Buttons) and "Value" (a TextMeshPro
        text, best inside a box with a Rect Mask 2D so the rolling numbers stay inside). E.g. Days to win.
        Or, instead of "Value", a TextMeshPro Input Field anywhere in the row: then the player can also click the
        number and type one (e.g. Starting coins).
Unity Editor: Min / Max = the smallest and biggest number allowed. Step = how much - and + change it (1 = one at a
              time, 10 for coins).
              Testing: Play and click - and +. With an Input Field, click the number, type, then press Enter or click
              somewhere else to keep it (Esc puts the old number back).
Purpose: A number with - and + buttons. When it changes, the old number rolls away and the new one rolls in,
         like an odometer (up when it grows, down when it shrinks). - / + fade out at the smallest / biggest number.
         With an Input Field the number can be typed too (whole numbers only): it counts while typing, and when typing
         ends the box shows the number kept between Min and Max (an empty box gets its number back). It pops instead
         of rolling. While someone types, Typing is true, so the title screen's Esc only stops the typing.
         Other scripts read Value, set it with SetValue(...) and hear about clicks and typing with Changed.
Requirement: StepperRow.cs x-> UITween.cs
             StepperRow.cs <-x NewGamePanel.cs, TitleSceneBuilder.cs
             StepperRow.cs <-o TitleMenu.cs
*/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StepperRow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The smallest number allowed.")]
    [SerializeField] int min = 1;

    [Tooltip("The biggest number allowed.")]
    [SerializeField] int max = 30;

    [Tooltip("The number now.")]
    [SerializeField] int value = 5;

    [Tooltip("How much - and + change the number: 1 = one at a time, 10 = ten at a time (e.g. coins).")]
    [Min(1)]
    [SerializeField] int step = 1;

    const float Roll = 28f;   // pixels a number rolls away / in from

    Button minus, plus;
    TMP_Text valueText;
    Vector2 textHome;
    Coroutine rolling;
    TMP_InputField input;     // the box the number can be typed in (null = the number only rolls)
    Vector3 inputSize;
    bool wasTyping;           // the box had the keyboard last frame

    static int typingFrame = -10;   // the last frame someone typed in a stepper's box (or stopped typing)

    // Fired when the player clicks - or + or types a number (not when a script sets the value): the new number.
    public event Action<int> Changed;

    public int Value => value;
    public int Min => min;
    public int Max => max;

    // True while the player types a number in a stepper's box, and the frame after (e.g. so Esc only stops typing).
    public static bool Typing => Time.frameCount - typingFrame <= 1;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        minus = FindButton("Minus");
        plus = FindButton("Plus");
        Transform t = FindDeep(transform, "Value");
        valueText = t != null ? t.GetComponent<TMP_Text>() : null;
        if (valueText != null) textHome = valueText.rectTransform.anchoredPosition;

        input = GetComponentInChildren<TMP_InputField>(true);
        if (input != null)
        {
            inputSize = input.transform.localScale;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;   // digits (and a minus sign) only
            input.onValueChanged.AddListener(OnTyped);
            input.onEndEdit.AddListener(_ => TypingEnded());
        }

        if (minus != null) minus.onClick.AddListener(() => Step(-1));
        if (plus != null) plus.onClick.AddListener(() => Step(1));
        Show(false, 0);
    }

    void Update()
    {
        if (input == null) return;
        bool typing = input.isFocused;
        if (typing) typingFrame = Time.frameCount;
        else if (wasTyping) TypingEnded();   // the box lost the keyboard (in case it didn't say so itself)
        wasTyping = typing;
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        typingFrame = -10;
    }

    // ===== My methods =====

    // Sets the number (kept between Min and Max). notify = also tell Changed. animate = roll the number.
    public void SetValue(int newValue, bool notify, bool animate = true)
    {
        newValue = Mathf.Clamp(newValue, min, max);
        int direction = newValue > value ? 1 : -1;
        bool changed = newValue != value;
        value = newValue;

        Show(animate && changed && isActiveAndEnabled, direction);
        if (notify && changed) Changed?.Invoke(value);
    }

    // - (direction -1) or + (1): one Step down or up.
    void Step(int direction)
    {
        SetValue(value + direction * step, true);
    }

    // A key was typed in the box: a whole number counts at once (kept between Min and Max), but the box isn't rewritten
    // while the player types (an empty box or a lone "-" waits for more).
    void OnTyped(string text)
    {
        typingFrame = Time.frameCount;
        if (!int.TryParse(text, out int typed)) return;
        int kept = Mathf.Clamp(typed, min, max);
        if (kept == value) return;
        value = kept;
        ShowButtons();
        Changed?.Invoke(value);
    }

    // Typing ended (Enter, a click somewhere else, or Esc, which puts the old number back): the box shows the number
    // that's kept, e.g. "0" for "-5", the number again for an empty box.
    void TypingEnded()
    {
        typingFrame = Time.frameCount;
        int kept = int.TryParse(input.text, out int typed) ? Mathf.Clamp(typed, min, max) : value;
        bool changed = kept != value;
        value = kept;
        if (changed || input.text != value.ToString()) Show(true, 0);
        if (changed) Changed?.Invoke(value);
    }

    // Writes the number, rolling it (direction 1 = rolls up, -1 = rolls down), and fades - / + at the ends. A box the
    // number can be typed in just shows the number, with a little pop, instead of rolling it.
    void Show(bool animate, int direction)
    {
        ShowButtons();
        if (input != null)
        {
            input.SetTextWithoutNotify(value.ToString());
            if (!animate || !isActiveAndEnabled) return;
            input.transform.localScale = inputSize * 1.12f;
            UITween.Scale(this, input.transform, inputSize, 0.25f);
            return;
        }
        if (valueText == null) return;

        if (rolling != null) StopCoroutine(rolling);
        if (!animate)
        {
            valueText.text = value.ToString();
            valueText.rectTransform.anchoredPosition = textHome;
            valueText.alpha = 1f;
            return;
        }

        RectTransform rt = valueText.rectTransform;
        Vector2 outTo = textHome + new Vector2(0f, Roll * direction);
        Vector2 inFrom = textHome - new Vector2(0f, Roll * direction);
        Vector2 start = rt.anchoredPosition;

        // The old number rolls away and fades, then the new one rolls in from the other side.
        rolling = UITween.Run(this, 0.09f, UITween.Ease.InCubic, t =>
        {
            rt.anchoredPosition = Vector2.Lerp(start, outTo, t);
            valueText.alpha = 1f - t;
        }, () =>
        {
            valueText.text = value.ToString();
            rolling = UITween.Run(this, 0.22f, UITween.Ease.OutBack, t =>
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(inFrom, textHome, t);
                valueText.alpha = Mathf.Clamp01(t);
            });
        });
    }

    // - and + fade out at the smallest / biggest number.
    void ShowButtons()
    {
        if (minus != null) minus.interactable = value > min;
        if (plus != null) plus.interactable = value < max;
    }

    Button FindButton(string childName)
    {
        Transform t = FindDeep(transform, childName);
        return t != null ? t.GetComponent<Button>() : null;
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
