/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script next to a TabGroup (on the tab bar). Tools > UI > Build Main Title Scene does it for the
        settings tabs.
Unity Editor: Optional: a child of the tab bar named "Underline" (a thin gold Image, with a Layout Element that has
              Ignore Layout ticked) glides under the open tab. Testing: Play, open Settings and click the tabs.
Purpose: Animates tabs: the underline glides to the clicked tab with a little overshoot, and the new page slides in
         from the side of the tab you came from (a tab further right slides in from the right) while fading in.
Requirement: TabFX.cs x-> TabGroup.cs, UITween.cs
             TabFX.cs <-x TitleSceneBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TabGroup))]
[DisallowMultipleComponent]
public class TabFX : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The bar that glides under the open tab. Empty = the child named \"Underline\" (can be missing).")]
    [SerializeField] RectTransform underline;

    [Tooltip("How far the new page slides in from, in pixels.")]
    [SerializeField] float slideDistance = 160f;

    [Range(0.05f, 1f)]
    [Tooltip("Seconds the slide and the glide take.")]
    [SerializeField] float slideTime = 0.35f;

    TabGroup tabs;
    bool snapPending = true;   // put the underline in place once the layout is known (end of the frame)
    readonly Dictionary<RectTransform, Vector2> pageHomes = new Dictionary<RectTransform, Vector2>();

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        tabs = GetComponent<TabGroup>();
        if (underline == null) underline = transform.Find("Underline") as RectTransform;
        tabs.Changed += OnTabChanged;
    }

    void OnDestroy()
    {
        if (tabs != null) tabs.Changed -= OnTabChanged;
    }

    void OnEnable()
    {
        snapPending = true;
    }

    void LateUpdate()
    {
        if (!snapPending) return;
        snapPending = false;

        // The tab buttons' places may not be worked out yet on their first frame: do it now.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        if (UnderlineTarget(tabs.Current, out float x, out float width)) PlaceUnderline(x, width);
    }

    // ===== My methods =====

    void OnTabChanged(int from, int to)
    {
        // The first tab when the window opens: no animation, just in place.
        if (from < 0 || from == to || !isActiveAndEnabled) { snapPending = true; return; }

        GlideUnderline(to);
        SlidePageIn(tabs.PageAt(to), to > from ? 1 : -1);
    }

    void GlideUnderline(int tab)
    {
        if (!UnderlineTarget(tab, out float toX, out float toWidth)) return;

        float fromX = underline.localPosition.x, fromWidth = underline.sizeDelta.x;
        UITween.Run(this, slideTime, UITween.Ease.OutBack,
                    t => PlaceUnderline(Mathf.LerpUnclamped(fromX, toX, t), Mathf.LerpUnclamped(fromWidth, toWidth, t)));
    }

    // Where the underline goes for this tab: under the middle of its button, as wide as it.
    bool UnderlineTarget(int tab, out float x, out float width)
    {
        x = width = 0f;
        Button button = tabs.ButtonAt(tab);
        if (underline == null || button == null || underline.parent == null) return false;

        RectTransform b = (RectTransform)button.transform;
        x = underline.parent.InverseTransformPoint(b.TransformPoint(b.rect.center)).x;
        width = b.rect.width;
        return true;
    }

    void PlaceUnderline(float x, float width)
    {
        underline.localPosition = new Vector3(x, underline.localPosition.y, underline.localPosition.z);
        underline.sizeDelta = new Vector2(width, underline.sizeDelta.y);
    }

    // The new page comes in from one side (dir: 1 = from the right, -1 = from the left) and fades in.
    void SlidePageIn(GameObject page, int dir)
    {
        if (page == null) return;

        RectTransform rt = (RectTransform)page.transform;
        if (!pageHomes.TryGetValue(rt, out Vector2 home)) home = pageHomes[rt] = rt.anchoredPosition;

        CanvasGroup group = page.GetComponent<CanvasGroup>();
        if (group == null) group = page.AddComponent<CanvasGroup>();

        Vector2 start = home + new Vector2(dir * slideDistance, 0f);
        UITween.Run(this, slideTime, UITween.Ease.OutCubic, t =>
        {
            rt.anchoredPosition = Vector2.LerpUnclamped(start, home, t);
            group.alpha = t;
        });
    }
}
