/*
First Date: 4/10/2026
Date: 18/10/2026
Author: Tony
Use by: Drag this script onto the Directional Light (the sun) in the scene.
Unity Editor: For a sky that turns orange at sunset and dark at night, use the default Procedural Skybox
              (Window > Rendering > Lighting > Environment > Skybox Material = Default-Skybox) and set "Sun Source" there to this light.
              "Parts Of The Day" = exactly when Dawn, Morning, Noon, Afternoon, Dusk and Night start (Night ends at Dawn).
              Testing: drag "Time Of Day" in the Inspector while playing, or shorten "Day Length Minutes" (e.g. 1)
              and watch the HUD's day / time counters (DayCounter, TimeCounter).
Purpose: Day and night. Turns the sun around the world and changes its brightness and color.
         Time Of Day: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.
         Counts the days (Day 1, 2, 3... a new day starts at Dawn) and names the part of the day (Noon, Night...).
         Tells other scripts when night and day start (NightStarted / DayStarted), e.g. to spawn enemies at night.
         Only moves while the GameRule "DoDaylightCycle" is on (off = time stops, like Minecraft).
Requirement: DaylightCycle.cs x-> GameRules.cs, Light (Unity)
             DaylightCycle.cs <-x DayCounter.cs, TimeCounter.cs
*/

using UnityEngine;

// The parts of a day, in the order they come after midnight's Night.
public enum DayPhase
{
    Night,
    Dawn,
    Morning,
    Noon,
    Afternoon,
    Dusk,
}

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class DaylightCycle : MonoBehaviour
{
    // ===== Fields =====
    [Header("Time")]
    [Range(0f, 1f)]
    [Tooltip("Time of day right now: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.")]
    [SerializeField] float timeOfDay = 0.3f;

    [Range(0.5f, 60f)]
    [Tooltip("How many real minutes one full day (day + night) takes. Minecraft = 20.")]
    [SerializeField] float dayLengthMinutes = 10f;

    [Min(1)]
    [Tooltip("Which day it is. Starts at 1 and goes up by 1 each time Dawn starts.")]
    [SerializeField] int day = 1;

    [Header("Parts Of The Day (when each one STARTS, as Time Of Day)")]
    [Range(0f, 1f)]
    [Tooltip("Night ends and the new day starts (the day number goes up). 0.23 = 05:31, just before sunrise.")]
    [SerializeField] float dawnStart = 0.23f;

    [Range(0f, 1f)]
    [Tooltip("0.30 = 07:12.")]
    [SerializeField] float morningStart = 0.30f;

    [Range(0f, 1f)]
    [Tooltip("0.45 = 10:48.")]
    [SerializeField] float noonStart = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("0.55 = 13:12.")]
    [SerializeField] float afternoonStart = 0.55f;

    [Range(0f, 1f)]
    [Tooltip("0.70 = 16:48.")]
    [SerializeField] float duskStart = 0.70f;

    [Range(0f, 1f)]
    [Tooltip("Night starts: enemies can start spawning (NightStarted). 0.77 = 18:28, just after sunset.")]
    [SerializeField] float nightStart = 0.77f;

    [Header("Sun")]
    [Range(0f, 360f)]
    [Tooltip("Which way the sun rises from (turns the sun's path around the world).")]
    [SerializeField] float sunDirection = 170f;

    [Range(0f, 3f)]
    [Tooltip("Sun brightness at noon.")]
    [SerializeField] float maxSunIntensity = 1.2f;

    [Tooltip("Sun color by height: left = on the horizon (sunrise / sunset), right = high in the sky.")]
    [SerializeField] Gradient sunColor = DefaultSunColor();

    [Header("Ambient (light that comes from everywhere)")]
    [Range(0f, 2f)]
    [Tooltip("Ambient light in the day.")]
    [SerializeField] float dayAmbient = 1f;

    [Range(0f, 2f)]
    [Tooltip("Ambient light at night. Not 0, or the night is pitch black.")]
    [SerializeField] float nightAmbient = 0.2f;

    Light sun;
    DayPhase lastPhase;
    bool phaseKnown;   // false until the first Update, so the starting part of the day is announced once too

    // The one in the scene (null if there is none), e.g. DaylightCycle.Current.Day
    public static DaylightCycle Current { get; private set; }

    // Other scripts subscribe to these from anywhere (static: no need to find the sun first).
    // They also fire once at the start of the game for the part of the day it starts in.
    public static event System.Action<int> NightStarted;          // the day number (the night after that day)
    public static event System.Action<int> DayStarted;            // the new day number
    public static event System.Action<DayPhase> PhaseChanged;     // any part of the day starts

    public float TimeOfDay => timeOfDay;   // other scripts can read it (e.g. a clock, or monsters at night)
    public int Day => day;
    public DayPhase Phase => PhaseAt(timeOfDay);
    public bool IsNight => Phase == DayPhase.Night;
    public float DayLengthSeconds => dayLengthMinutes * 60f;

    // The clock: 0.5 -> 12:00, 0.77 -> 18:28.
    public int Hour => Mathf.FloorToInt(timeOfDay * 24f) % 24;
    public int Minute => Mathf.FloorToInt(timeOfDay * 24f * 60f) % 60;
    public string ClockText => Hour.ToString("00") + ":" + Minute.ToString("00");

    // Real seconds until night / morning starts (0 if it's already that time).
    public float SecondsUntilNight => IsNight ? 0f : RealSecondsUntil(nightStart);
    public float SecondsUntilMorning => IsNight ? RealSecondsUntil(dawnStart) : 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Current = this;
        sun = GetComponent<Light>();
        if (RenderSettings.sun == null) RenderSettings.sun = sun;   // the procedural sky follows this light
    }

    void OnDestroy()
    {
        if (Current != this) return;

        Current = null;   // static values outlive the scene, so clear them
        NightStarted = null;
        DayStarted = null;
        PhaseChanged = null;
    }

    void Update()
    {
        if (GameRules.DoDaylightCycle) AdvanceTime();
        ApplyLight();   // always, so dragging Time Of Day works even when time is stopped
        CheckPhase();   // also catches dragging Time Of Day in the Inspector
    }

    // Unity calls this when a value is changed in the Inspector: keep the parts of the day in order.
    void OnValidate()
    {
        morningStart = Mathf.Max(morningStart, dawnStart);
        noonStart = Mathf.Max(noonStart, morningStart);
        afternoonStart = Mathf.Max(afternoonStart, noonStart);
        duskStart = Mathf.Max(duskStart, afternoonStart);
        nightStart = Mathf.Max(nightStart, duskStart);
    }

    // ===== My methods =====

    void AdvanceTime()
    {
        // One full day = dayLengthMinutes * 60 seconds, so each second adds 1 / that.
        float before = timeOfDay;
        float after = before + Time.deltaTime / DayLengthSeconds;

        // Passed Dawn this frame (also right after midnight, when "after" goes over 1): a new day.
        bool passedDawn = (before < dawnStart && after >= dawnStart) || (after >= 1f && after - 1f >= dawnStart);
        if (passedDawn) day++;

        timeOfDay = after % 1f;   // after 1 (midnight again) start back at 0
    }

    // Tells the listening scripts when a new part of the day starts.
    // The very first check (start of the game) announces whichever it is, night or day.
    // After that: Night starting = NightStarted, Night ending = DayStarted.
    void CheckPhase()
    {
        DayPhase now = Phase;
        if (phaseKnown && now == lastPhase) return;   // still the same part of the day

        bool first = !phaseKnown;
        bool wasNight = lastPhase == DayPhase.Night;
        lastPhase = now;
        phaseKnown = true;

        PhaseChanged?.Invoke(now);
        bool isNight = now == DayPhase.Night;
        if (isNight && (first || !wasNight)) NightStarted?.Invoke(day);
        else if (!isNight && (first || wasNight)) DayStarted?.Invoke(day);
    }

    // Which part of the day a Time Of Day is.
    public DayPhase PhaseAt(float t)
    {
        if (t >= nightStart || t < dawnStart) return DayPhase.Night;
        if (t >= duskStart) return DayPhase.Dusk;
        if (t >= afternoonStart) return DayPhase.Afternoon;
        if (t >= noonStart) return DayPhase.Noon;
        if (t >= morningStart) return DayPhase.Morning;
        return DayPhase.Dawn;
    }

    // Real seconds until Time Of Day reaches "target" (going forward, past midnight if needed).
    float RealSecondsUntil(float target)
    {
        float left = target - timeOfDay;
        if (left < 0f) left += 1f;   // e.g. now 0.9, target 0.23 -> 0.33 of a day to go
        return left * DayLengthSeconds;
    }

    void ApplyLight()
    {
        // 0.25 (sunrise) = 0 degrees (horizon), 0.5 (noon) = 90 (straight down), 0.75 (sunset) = 180.
        float angle = timeOfDay * 360f - 90f;
        transform.rotation = Quaternion.Euler(angle, sunDirection, 0f);

        float height = SunHeight();   // 1 = noon, 0 = horizon, below 0 = night

        // Bright in the day, fading out quickly as the sun goes below the horizon.
        // "* 4" makes the fade short: full brightness once the sun is a bit above the horizon.
        sun.intensity = maxSunIntensity * Mathf.Clamp01(height * 4f + 0.1f);
        sun.color = sunColor.Evaluate(Mathf.Clamp01(height));
        sun.enabled = height > -0.1f;   // no sun shadows from below the ground at night

        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbient, dayAmbient, Mathf.Clamp01(height * 2f + 0.3f));
    }

    // How high the sun is: 1 = straight up, 0 = horizon, -1 = straight down (midnight).
    float SunHeight()
    {
        // The light shines along its forward, so the sun itself is in the opposite direction.
        return -transform.forward.y;
    }

    // Orange on the horizon, warm white high in the sky.
    static Gradient DefaultSunColor()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.65f), 0.25f),
                new GradientColorKey(new Color(1f, 0.97f, 0.9f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
