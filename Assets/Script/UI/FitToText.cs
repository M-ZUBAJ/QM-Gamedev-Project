/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the box that holds a background panel and a text, e.g. Status on the death page:
          Status        <- this script
            StatusPanel <- an Image (the background)
            StatusText  <- a TextMeshPro text
Unity Editor: Nothing to connect: it finds the first TextMeshPro text and the first Image among its children.
              Set the box's WIDTH as you like; the HEIGHT is set by this script to fit the text.
              Pivot Y = 1 (top) makes the box grow downward; 0.5 grows up and down from the middle.
              Remove any Vertical Layout Group / Content Size Fitter from the box: this script does their job.
              Works in Edit mode too: type more lines into the text and watch the box grow.
Purpose: Makes the box as tall as its text (plus Padding), so the background panel always covers all the text,
         however many lines there are. The panel fills the whole box, behind the text.
Requirement: FitToText.cs x-> RectTransform (Unity)
*/

using TMPro;            // TMP_Text
using UnityEngine;
using UnityEngine.UI;   // Image

[ExecuteAlways]                         // also runs in Edit mode, so the size is right while designing
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class FitToText : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Space between the edge of the panel and the text, in pixels.")]
    [Min(0f)]
    [SerializeField] float padding = 15f;

    [Tooltip("The box is never shorter than this, in pixels (e.g. so an empty text still shows a small panel).")]
    [Min(0f)]
    [SerializeField] float minHeight = 0f;

    [Tooltip("The text to fit. Empty = the first TextMeshPro text among the children.")]
    [SerializeField] TMP_Text text;

    [Tooltip("The background. Empty = the first Image among the children.")]
    [SerializeField] Image panel;

    RectTransform box;

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        box = GetComponent<RectTransform>();
        FindChildren();
    }

    // LateUpdate: after other scripts changed the text this frame (e.g. DeathScreen writing the stats).
    void LateUpdate()
    {
        if (text == null) FindChildren();
        Fit();
    }

    // ===== My methods =====

    void FindChildren()
    {
        if (text == null) text = GetComponentInChildren<TMP_Text>(true);

        if (panel == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject == gameObject) continue;   // not an Image on the box itself
                panel = image;
                break;
            }
        }
    }

    void Fit()
    {
        if (box == null || text == null) return;

        // The panel fills the whole box and sits first among the children, so it's drawn BEHIND the text.
        if (panel != null)
        {
            Stretch(panel.rectTransform, 0f);
            if (panel.transform.parent == transform && panel.transform.GetSiblingIndex() != 0) panel.transform.SetAsFirstSibling();
        }

        // The text fills the box minus the padding on every side.
        Stretch(text.rectTransform, padding);

        // How tall the text wants to be at the box's width (minus padding), then the box = that + padding above and below.
        float textWidth = Mathf.Max(1f, box.rect.width - padding * 2f);
        float textHeight = text.GetPreferredValues(text.text, textWidth, Mathf.Infinity).y;
        float height = Mathf.Max(minHeight, textHeight + padding * 2f);

        if (!Mathf.Approximately(box.rect.height, height))   // only when it changed (no needless "scene changed" mark)
        {
            box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }

    // Makes a child fill its parent, leaving "inset" pixels free on every side.
    static void Stretch(RectTransform rt, float inset)
    {
        Vector2 min = new Vector2(inset, inset);
        Vector2 max = new Vector2(-inset, -inset);
        if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.offsetMin == min && rt.offsetMax == max) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }
}
