/*
First Date: 18/10/2026
Date: 18/10/2026
Author: Tony
Use by: Put this on a TextMeshPro text in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > TopRight > DayText).
Unity Editor: Nothing to connect: it finds the DaylightCycle (on the sun) by itself.
              Testing: Play and drag the sun's "Time Of Day"; the text changes between Dawn, Morning, Noon... Night.
Purpose: Shows which day it is and which part of the day, e.g. "DAY 5 - Noon" or "DAY 8 - Night".
Requirement: DayCounter.cs x-> DaylightCycle.cs, TextMeshProUGUI (Unity)
             DayCounter.cs <-x HUDBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
[DisallowMultipleComponent]
public class DayCounter : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("What to write. {0} = the day number, {1} = the part of the day (Dawn, Morning, Noon, Afternoon, Dusk, Night).")]
    [SerializeField] string format = "DAY {0} - {1}";

    [Tooltip("Shown while there is no DaylightCycle in the scene.")]
    [SerializeField] string noDaylightText = "DAY -";

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
        string now = sun != null ? string.Format(format, sun.Day, sun.Phase) : noDaylightText;
        if (now == shown) return;

        shown = now;
        text.text = now;
    }
}
