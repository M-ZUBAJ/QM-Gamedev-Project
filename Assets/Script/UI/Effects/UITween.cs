/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). The menu effects (ButtonFX, CascadeIn, TitleMenu...) use it.
Unity Editor: Nothing to set. TitleMenu's Animation Speed sets UITween.Speed (2 = every menu animation twice as fast,
              handy while testing).
Purpose: A tiny animation helper: moves, scales, fades or recolors something smoothly over a few seconds, with an
         "ease" = how it speeds up and slows down (overshoot, bounce, elastic...). Uses real time, so menus keep moving
         while the game is paused (Time.timeScale = 0). Example:
           UITween.Run(this, 0.4f, UITween.Ease.OutBack, t => box.localScale = Vector3.one * t);
         Each animation is a coroutine on the script that starts it: switching that object off stops it.
Requirement: UITween.cs <-x ButtonFX.cs, CascadeIn.cs, ChoiceRow.cs, DropdownUnfold.cs, IncomingMessage.cs, KeyBindingMenu.cs, NewGamePanel.cs, PulseFX.cs, ScreenFader.cs, StepperRow.cs, TabFX.cs, TitleBackground.cs, TitleMenu.cs, TitleSceneBuilder.cs, TitleTextFX.cs, TogglePop.cs, UISparks.cs
*/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class UITween
{
    // How an animation moves from start (t = 0) to end (t = 1).
    public enum Ease
    {
        Linear,      // the same speed all the way
        OutCubic,    // fast start, gentle stop (the usual "smooth")
        InCubic,     // slow start, fast end (things flying away)
        InOutSine,   // gentle at both ends
        OutBack,     // goes a little past the end, then settles back (a "pop")
        OutElastic,  // wobbles around the end like a spring
        OutBounce,   // bounces at the end like a dropped ball
    }

    // ===== Fields =====
    // 1 = normal speed, 2 = twice as fast, 0.5 = slow motion. Set by TitleMenu's Animation Speed.
    public static float Speed = 1f;

    // Real seconds since the last frame, times Speed: how far every menu animation moves this frame.
    public static float DeltaTime => Time.unscaledDeltaTime * Speed;

    // ===== My methods =====

    // Calls step(t) every frame for "seconds", with t going from 0 to 1 along the ease, then calls done.
    // delay = seconds to wait first. The owner runs it: a switched-off owner just jumps to the end.
    public static Coroutine Run(MonoBehaviour owner, float seconds, Ease ease, Action<float> step,
                                Action done = null, float delay = 0f)
    {
        if (owner == null || !owner.isActiveAndEnabled)
        {
            step?.Invoke(1f);
            done?.Invoke();
            return null;
        }
        return owner.StartCoroutine(Routine(seconds, ease, step, done, delay));
    }

    static IEnumerator Routine(float seconds, Ease ease, Action<float> step, Action done, float delay)
    {
        for (float wait = 0f; wait < delay; wait += DeltaTime) yield return null;

        for (float time = 0f; time < seconds; time += DeltaTime)
        {
            step?.Invoke(Evaluate(ease, time / seconds));
            yield return null;
        }
        step?.Invoke(Evaluate(ease, 1f));
        done?.Invoke();
    }

    // Waits this many seconds (menu time, so Animation Speed counts). Use with "yield return" in a coroutine.
    public static IEnumerator Wait(float seconds)
    {
        for (float time = 0f; time < seconds; time += DeltaTime) yield return null;
    }

    // ----- Shortcuts for the usual things -----

    // Fades a CanvasGroup (a whole panel and everything in it) to "to" (0 = invisible, 1 = fully shown).
    public static Coroutine Fade(MonoBehaviour owner, CanvasGroup group, float to, float seconds,
                                 Ease ease = Ease.OutCubic, float delay = 0f, Action done = null)
    {
        float from = group.alpha;
        return Run(owner, seconds, ease, t => group.alpha = Mathf.LerpUnclamped(from, to, t), done, delay);
    }

    // Recolors a picture or text (also its see-through amount).
    public static Coroutine Color(MonoBehaviour owner, Graphic graphic, Color to, float seconds,
                                  Ease ease = Ease.OutCubic, float delay = 0f, Action done = null)
    {
        Color from = graphic.color;
        return Run(owner, seconds, ease, t => graphic.color = UnityEngine.Color.LerpUnclamped(from, to, t), done, delay);
    }

    // Scales something to "to" (1 = normal size).
    public static Coroutine Scale(MonoBehaviour owner, Transform target, Vector3 to, float seconds,
                                  Ease ease = Ease.OutBack, float delay = 0f, Action done = null)
    {
        Vector3 from = target.localScale;
        return Run(owner, seconds, ease, t => target.localScale = Vector3.LerpUnclamped(from, to, t), done, delay);
    }

    // Moves a UI object to "to" (its Pos X / Pos Y in the Inspector).
    public static Coroutine Move(MonoBehaviour owner, RectTransform target, Vector2 to, float seconds,
                                 Ease ease = Ease.OutCubic, float delay = 0f, Action done = null)
    {
        Vector2 from = target.anchoredPosition;
        return Run(owner, seconds, ease, t => target.anchoredPosition = Vector2.LerpUnclamped(from, to, t), done, delay);
    }

    // How far along an animation is (0 - 1, can go past 1 for overshoot) at time t (0 - 1).
    public static float Evaluate(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
            case Ease.InCubic: return t * t * t;
            case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
            case Ease.OutBack:
            {
                const float overshoot = 1.70158f;   // the usual amount: about 10% past the end
                float u = t - 1f;
                return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
            }
            case Ease.OutElastic:
                if (t <= 0f || t >= 1f) return t;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
            case Ease.OutBounce: return Bounce(t);
            default: return t;
        }
    }

    // A ball dropped on the floor: 3 smaller and smaller bounces before it stops.
    static float Bounce(float t)
    {
        const float n = 7.5625f, d = 2.75f;
        if (t < 1f / d) return n * t * t;
        if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
        if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
        t -= 2.625f / d;
        return n * t * t + 0.984375f;
    }
}
