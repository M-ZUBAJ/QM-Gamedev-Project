/*
First Date: 18/10/2026
Date: 18/10/2026
Author: Tony
Use by: Put this on a TextMeshPro text in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > TopRight > TimeText).
Unity Editor: Nothing to connect: it finds the DaylightCycle (on the sun) by itself.
              Testing: Play and watch the clock move and the countdown go down. Shorten the sun's
              "Day Length Minutes" (e.g. 1) to see night come quickly.
Purpose: Shows the time of day as a clock and how long until night (or until morning while it's night),
         e.g. "14:30 - Night in 2:15" or "22:10 - Morning in 3:40". The countdown is in REAL minutes:seconds,
         so the player knows how long they have left to get ready.
Requirement: TimeCounter.cs x-> DaylightCycle.cs, TextMeshProUGUI (Unity)
             TimeCounter.cs <-x HUDBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
[DisallowMultipleComponent]
public class TimeCounter : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("On: 24-hour clock (14:30). Off: 12-hour clock (2:30 PM).")]
    [SerializeField] bool clock24Hour = true;

    [Tooltip("Show how long until night / morning after the clock.")]
    [SerializeField] bool showCountdown = true;

    [Tooltip("Before the countdown in the daytime.")]
    [SerializeField] string untilNightText = "Night in";

    [Tooltip("Before the countdown at night.")]
    [SerializeField] string untilMorningText = "Morning in";

    [Tooltip("Between the clock and the countdown.")]
    [SerializeField] string separator = " - ";

    TextMeshProUGUI text;
    string shown;   // what the text says now, so it's only rebuilt when something changed

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        DaylightCycle sun = DaylightCycle.Current;
        string now = sun != null ? Build(sun) : "--:--";
        if (now == shown) return;

        shown = now;
        text.text = now;
    }

    // ===== My methods =====

    string Build(DaylightCycle sun)
    {
        string clock = clock24Hour ? sun.ClockText : Clock12(sun.Hour, sun.Minute);
        if (!showCountdown) return clock;

        string label = sun.IsNight ? untilMorningText : untilNightText;
        float seconds = sun.IsNight ? sun.SecondsUntilMorning : sun.SecondsUntilNight;
        return clock + separator + label + " " + MinutesSeconds(seconds);
    }

    // 14:05 -> "2:05 PM", 0:30 -> "12:30 AM".
    static string Clock12(int hour, int minute)
    {
        int h = hour % 12;
        if (h == 0) h = 12;
        return h + ":" + minute.ToString("00") + (hour < 12 ? " AM" : " PM");
    }

    // 135.4 seconds -> "2:15". CeilToInt: it shows "0:01" until the very last moment, never "0:00" too early.
    static string MinutesSeconds(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }
}
