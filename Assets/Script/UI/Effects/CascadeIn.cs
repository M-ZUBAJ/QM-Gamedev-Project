/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto a panel whose children should appear one after another (e.g. a column of buttons,
        the rows of a settings list). Works with a Vertical / Horizontal Layout Group too.
Unity Editor: Offset = where each child starts from, compared to its place (0, -40 = from 40 pixels lower).
              Testing: Play, then switch the panel off and on: its children slide in one by one.
Purpose: Every time the panel is shown, its children slide in from Offset and fade in, one after another
         (Stagger seconds apart), like cards being dealt. Switched-off children are skipped.
         Other scripts can also Play() it by hand, Hide() everything first (e.g. before an intro), or Finish()
         to jump to the end (e.g. when the player skips the intro).
         It starts at the end of the frame the panel opens: by then every child is switched on, so a Layout Group
         has measured them right (measured any earlier, switched-off children count as 0 high and pile up).
Requirement: CascadeIn.cs x-> UITween.cs
             CascadeIn.cs <-x TitleMenu.cs, TitleSceneBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CascadeIn : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Where each child starts from, compared to its place, in pixels (0, -40 = from 40 pixels lower).")]
    [SerializeField] Vector2 offset = new Vector2(0f, -40f);

    [Range(0f, 0.5f)]
    [Tooltip("Seconds between one child starting and the next.")]
    [SerializeField] float stagger = 0.06f;

    [Range(0.05f, 2f)]
    [Tooltip("Seconds each child takes to arrive.")]
    [SerializeField] float duration = 0.45f;

    [Tooltip("How each child arrives: Out Back = goes a little past its place and settles back.")]
    [SerializeField] UITween.Ease ease = UITween.Ease.OutBack;

    [Tooltip("Play every time the panel is switched on. Off = only when a script calls Play().")]
    [SerializeField] bool playOnEnable = true;

    // Each moving child and the place it belongs (remembered, so a second Play mid-way doesn't lose it).
    readonly Dictionary<RectTransform, Vector2> homes = new Dictionary<RectTransform, Vector2>();
    bool playing;
    bool pending;          // Play was asked for: it starts at the end of this frame (LateUpdate)
    float pendingDelay;

    public bool IsPlaying => playing || pending;

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        Finish();   // put everything back in place, so nothing stays half-way while hidden
    }

    // The children switch on AFTER this panel does, and a switched-off child measures as 0 high: measured right away,
    // they'd all pile up at the top. By the end of the frame every child is on, so their places are right.
    void LateUpdate()
    {
        if (!pending) return;
        pending = false;
        PlayNow(pendingDelay);
    }

    // ===== My methods =====

    // Slides the children in, one after another, after "delay" seconds (starting at the end of this frame).
    public void Play(float delay = 0f)
    {
        Finish();
        SetAlpha(0f);   // invisible until they start moving in
        pending = true;
        pendingDelay = delay;
    }

    void PlayNow(float delay)
    {
        homes.Clear();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);   // every child is on: places are right

        int i = 0;
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rt)) continue;

            CanvasGroup group = GroupOf(rt);
            Vector2 home = rt.anchoredPosition;
            homes[rt] = home;
            group.alpha = 0f;
            rt.anchoredPosition = home + offset;

            UITween.Run(this, duration, ease, t =>
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(home + offset, home, t);
                group.alpha = Mathf.Clamp01(t * 1.6f);   // fully shown a bit before it arrives
            }, null, delay + i * stagger);
            i++;
        }
        playing = i > 0;
    }

    // Makes every child invisible (e.g. before an intro plays them in).
    public void Hide()
    {
        Finish();
        SetAlpha(0f);
    }

    // Stops at the end: every child in its place and fully shown.
    public void Finish()
    {
        pending = false;
        StopAllCoroutines();
        foreach (KeyValuePair<RectTransform, Vector2> pair in homes)
        {
            if (pair.Key == null) continue;
            pair.Key.anchoredPosition = pair.Value;
        }
        SetAlpha(1f);
        playing = false;

        // A Layout Group puts its children back in their places too (in case something changed while moving).
        if (isActiveAndEnabled) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }

    void SetAlpha(float alpha)
    {
        foreach (Transform child in transform)
        {
            if (child is RectTransform rt) GroupOf(rt).alpha = alpha;
        }
    }

    static CanvasGroup GroupOf(RectTransform rt)
    {
        CanvasGroup group = rt.GetComponent<CanvasGroup>();
        if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
        return group;
    }
}
