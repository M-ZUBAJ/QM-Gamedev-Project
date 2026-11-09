/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto a row with a child named "Options" that holds the option Buttons side by side
        (a Horizontal Layout Group), e.g. Day length: Short / Normal / Long. Tools > UI > Build Main Title Scene makes them.
Unity Editor: Optional: a child of Options named "Highlight" (an Image with a Layout Element, Ignore Layout ticked,
              first in the list so it's drawn behind) glides behind the picked option.
              Option Colors = the highlight color for each option (e.g. green / blue / gold / red for the difficulties).
              Testing: Play and click the options.
Purpose: Options where exactly one is picked (like radio buttons). Clicking one picks it: the highlight glides to it
         with a little overshoot and takes that option's color, and the picked text turns dark to read on it.
         Other scripts read Picked, pick from code with Pick(...) and hear about clicks with Changed.
Requirement: ChoiceRow.cs x-> UITween.cs
             ChoiceRow.cs <-x NewGamePanel.cs, TitleSceneBuilder.cs
*/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChoiceRow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The highlight color for each option, in order. Empty (or too short) = gold.")]
    [SerializeField] Color[] optionColors = new Color[0];

    [Tooltip("Text color of the picked option (dark, so it reads on the highlight).")]
    [SerializeField] Color pickedTextColor = new Color(0.12f, 0.09f, 0.04f);

    [Tooltip("Text color of the other options.")]
    [SerializeField] Color normalTextColor = Color.white;

    static readonly Color Gold = new Color(1f, 0.8f, 0.25f);

    readonly List<Button> options = new List<Button>();
    readonly List<TMP_Text> labels = new List<TMP_Text>();
    RectTransform highlight;   // can be missing
    Image highlightImage;
    int picked = -1;
    bool snapPending;          // put the highlight in place once the layout is known (end of the frame)

    // Fired when the player clicks an option (not when a script picks one): the option's number.
    public event Action<int> Changed;

    public int Picked => picked;
    public int Count => options.Count;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Transform holder = transform.Find("Options");
        if (holder == null) holder = transform;
        highlight = holder.Find("Highlight") as RectTransform;
        if (highlight != null) highlightImage = highlight.GetComponent<Image>();

        foreach (Transform child in holder)
        {
            Button b = child.GetComponent<Button>();
            if (b == null || child == highlight) continue;

            int index = options.Count;   // copied per button, so each click remembers its own number
            b.onClick.AddListener(() => Pick(index, true));
            options.Add(b);
            labels.Add(child.GetComponentInChildren<TMP_Text>(true));
        }

        if (picked < 0 && options.Count > 0) Pick(0, false);
    }

    void OnEnable()
    {
        snapPending = true;
    }

    void LateUpdate()
    {
        if (!snapPending || highlight == null) return;
        snapPending = false;

        // The options' places may not be worked out yet on their first frame: do it now.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)highlight.parent);
        if (Target(picked, out float x, out float width)) Place(x, width, ColorOf(picked));
    }

    // ===== My methods =====

    // Picks option i. notify = also tell Changed (true when the player clicked).
    public void Pick(int i, bool notify)
    {
        if (options.Count == 0) return;
        i = Mathf.Clamp(i, 0, options.Count - 1);
        bool moved = i != picked && picked >= 0;
        picked = i;

        for (int j = 0; j < labels.Count; j++)
        {
            if (labels[j] != null) labels[j].color = j == picked ? pickedTextColor : normalTextColor;
        }

        if (moved && isActiveAndEnabled) Glide(i);
        else snapPending = true;

        if (notify) Changed?.Invoke(i);
    }

    // Changes the text on option i (e.g. "10 min").
    public void SetLabel(int i, string text)
    {
        if (i >= 0 && i < labels.Count && labels[i] != null) labels[i].text = text;
    }

    void Glide(int i)
    {
        if (highlight == null || !Target(i, out float toX, out float toWidth)) return;

        float fromX = highlight.localPosition.x, fromWidth = highlight.sizeDelta.x;
        Color fromColor = highlightImage != null ? highlightImage.color : Gold;
        Color toColor = ColorOf(i);
        UITween.Run(this, 0.35f, UITween.Ease.OutBack, t =>
        {
            Place(Mathf.LerpUnclamped(fromX, toX, t), Mathf.LerpUnclamped(fromWidth, toWidth, t),
                  Color.Lerp(fromColor, toColor, Mathf.Clamp01(t)));
            float punch = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);   // swells a little on the way
            highlight.localScale = new Vector3(1f, punch, 1f);
        });
    }

    // Where the highlight goes for option i: behind the middle of its button, as wide as it.
    bool Target(int i, out float x, out float width)
    {
        x = width = 0f;
        if (highlight == null || i < 0 || i >= options.Count) return false;

        RectTransform b = (RectTransform)options[i].transform;
        x = highlight.parent.InverseTransformPoint(b.TransformPoint(b.rect.center)).x;
        width = b.rect.width;
        return true;
    }

    void Place(float x, float width, Color color)
    {
        highlight.localPosition = new Vector3(x, highlight.localPosition.y, highlight.localPosition.z);
        highlight.sizeDelta = new Vector2(width, highlight.sizeDelta.y);
        if (highlightImage != null) highlightImage.color = color;
    }

    Color ColorOf(int i)
    {
        return i >= 0 && i < optionColors.Length ? optionColors[i] : Gold;
    }
}
