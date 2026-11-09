/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto a row with children named "Minus" and "Plus" (Buttons) and "Value" (a TextMeshPro
        text, best inside a box with a Rect Mask 2D so the rolling numbers stay inside). E.g. Days to win.
Unity Editor: Min / Max = the smallest and biggest number allowed. Testing: Play and click - and +.
Purpose: A number with - and + buttons. When it changes, the old number rolls away and the new one rolls in,
         like an odometer (up when it grows, down when it shrinks). - / + fade out at the smallest / biggest number.
         Other scripts read Value, set it with SetValue(...) and hear about clicks with Changed.
Requirement: StepperRow.cs x-> UITween.cs
             StepperRow.cs <-x NewGamePanel.cs, TitleSceneBuilder.cs
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

    const float Roll = 28f;   // pixels a number rolls away / in from

    Button minus, plus;
    TMP_Text valueText;
    Vector2 textHome;
    Coroutine rolling;

    // Fired when the player clicks - or + (not when a script sets the value): the new number.
    public event Action<int> Changed;

    public int Value => value;
    public int Min => min;
    public int Max => max;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        minus = FindButton("Minus");
        plus = FindButton("Plus");
        Transform t = FindDeep(transform, "Value");
        valueText = t != null ? t.GetComponent<TMP_Text>() : null;
        if (valueText != null) textHome = valueText.rectTransform.anchoredPosition;

        if (minus != null) minus.onClick.AddListener(() => Step(-1));
        if (plus != null) plus.onClick.AddListener(() => Step(1));
        Show(false, 0);
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

    void Step(int by)
    {
        SetValue(value + by, true);
    }

    // Writes the number, rolling it (direction 1 = rolls up, -1 = rolls down), and fades - / + at the ends.
    void Show(bool animate, int direction)
    {
        if (minus != null) minus.interactable = value > min;
        if (plus != null) plus.interactable = value < max;
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
