/*
First Date: 4/10/2026
Date: 20/11/2026
Author: Tony
Use by: Drag this script onto the Directional Light (the sun) in the scene.
Unity Editor: For a sky that turns orange at sunset and dark blue with a moon at night, use the default Procedural
              Skybox (Window > Rendering > Lighting > Environment > Skybox Material = Default-Skybox) and set "Sun Source"
              there to this light.
              "Parts Of The Day" = exactly when Dawn, Morning, Noon, Afternoon, Dusk and Night start (Night ends at Dawn).
              Testing: drag "Time Of Day" in the Inspector while playing (0 = midnight), or shorten "Day Length Minutes"
              (e.g. 1) and watch the HUD's day / time counters (DayCounter, TimeCounter). The Moon, Ambient and Night
              Sky settings change the night while you watch. Guide: Docs/Day And Night Setup.md
              Starting the game from the title screen needs the scene's light from the sky saved once, or every shadow
              is pitch black: Tools > World > Set Up Sky Lighting (Docs/Sky Lighting Setup.md).
Purpose: Day and night. Turns the sun around the world and changes its brightness and color.
         Time Of Day: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.
         At night (Moonlight on) the same light becomes the moon, on the other side of the sky from the sun: a soft
         blue light with soft shadows, a moon in the sky (the sky's sun disk), a dark blue sky, and dark blue light in
         the shadows instead of darkness. Off = the old dark nights.
         Counts the days (Day 1, 2, 3... a new day starts at Dawn) and names the part of the day (Noon, Night...).
         Tells other scripts when night and day start (NightStarted / DayStarted), e.g. to spawn enemies at night.
         Only moves while the GameRule "DoDaylightCycle" is on (off = time stops, like Minecraft).
         When the player dies (PlayerHealth), the day, time and light go back to how the game started
         (Reset On Player Death).
         The New Game page can change the start time and day length before the game starts (SetStartTime /
         SetDayLength, used by GameSetup).
         If the scene has no saved light from the sky (pitch-black shadows when the title screen opens it), it asks
         Unity to work it out from the sky when the game starts, and says in the Console how to fix it for good.
Requirement: DaylightCycle.cs x-> GameRules.cs, Light (Unity)
             DaylightCycle.cs o-> PlayerHealth.cs
             DaylightCycle.cs <-x DayCounter.cs, EnemySpawner.cs, GameSetup.cs, NewGameDefaultsSync.cs, TimeCounter.cs, VictoryScreen.cs
             DaylightCycle.cs <-o DesertMonsterSpawner.cs, PerkEffects.cs, ViewDistance.cs
*/

using UnityEngine;
using UnityEngine.Rendering;   // AmbientMode, SphericalHarmonicsL2 (the light from the sky)

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

    [Header("Moon (the same light, at night)")]
    [Tooltip("On: at night this light becomes the moon (on the other side of the sky from the sun): a soft blue light " +
             "with shadows, a moon in the sky, a dark blue sky and blue light in the shadows. Off: dark nights, the old way.")]
    [SerializeField] bool moonlight = true;

    [Range(0f, 2f)]
    [Tooltip("Moon brightness high in the sky (the sun at noon is Max Sun Intensity). Higher = easier to see at night.")]
    [SerializeField] float moonIntensity = 0.5f;

    [Tooltip("Moonlight color: a pale blue.")]
    [SerializeField] Color moonColor = new Color(0.62f, 0.74f, 1f);

    [Range(0f, 1f)]
    [Tooltip("How dark the moon's shadows are: 0 = no shadows, 1 = as dark as the sun's.")]
    [SerializeField] float moonShadowStrength = 0.6f;

    [Header("Ambient (light that comes from everywhere)")]
    [Range(0f, 2f)]
    [Tooltip("Ambient light in the day: 1 = the sky's light as saved by Tools > World > Set Up Sky Lighting.")]
    [SerializeField] float dayAmbient = 1f;

    [Range(0f, 2f)]
    [Tooltip("Ambient light at night with Moonlight off: the day's, times this. Not 0, or the night is pitch black.")]
    [SerializeField] float nightAmbient = 0.2f;

    [Tooltip("Ambient light at night with Moonlight on: the blue that lights the shadows (what a white wall in the " +
             "shade looks like). Lighter = easier to see at night.")]
    [SerializeField] Color nightAmbientColor = new Color(0.25f, 0.3f, 0.42f);

    [Header("Night Sky (Moonlight on, Procedural Skybox)")]
    [Range(0f, 8f)]
    [Tooltip("How bright the night sky is (the sky material's Exposure; the day keeps the material's own, 1.3 by default).")]
    [SerializeField] float nightSkyExposure = 0.2f;

    [Tooltip("The night sky's color (the sky material's Sky Tint; grey = a normal sky).")]
    [SerializeField] Color nightSkyTint = new Color(0.3f, 0.42f, 0.8f);

    [Header("Player Death")]
    [Tooltip("On: when the player dies, the day, the time and the light go back to how the game started " +
             "(the Day and Time Of Day set here before pressing Play). Off: time just carries on.")]
    [SerializeField] bool resetOnPlayerDeath = true;

    Light sun;   // this Directional Light: the sun in the day, the moon at night (Moonlight)
    float sunShadowStrength;   // its own shadow darkness, from the scene (the moon has Moon Shadow Strength)
    DayPhase lastPhase;
    bool phaseKnown;   // false until the first Update, so the starting part of the day is announced once too
    float startTimeOfDay;   // how the game started, to go back to when the player dies
    int startDay;
    PlayerHealth playerHealth;   // found by itself, can be missing (then dying resets nothing)

    // The day's sky, remembered in Start before the night changes anything.
    bool skyRemembered;          // false before Start: the sky and the ambient light aren't touched yet
    bool skyLightSaved;          // the scene has saved light from the sky (else Unity works it out, so it's left alone)
    SphericalHarmonicsL2 dayProbe;   // that saved light from everywhere (Unity's "ambient probe")
    float dayReflection = 1f;    // how much the sky shows in shiny things in the day
    Material originalSky;        // the scene's sky material, given back when this goes
    Material skyCopy;            // a copy of it that the night changes (the material file itself never changes)
    float dayExposure = 1.3f;    // the sky material's own Exposure and tint (the day keeps them)
    Color dayTint = Color.gray;
    int tintId;                  // which tint the sky material has (_SkyTint or _Tint), if hasTint
    bool hasTint;

    // The light turns from sun into moon (and back at dawn) when the sun is this far below the horizon (about 6
    // degrees). Then it has no brightness at all, so the change isn't seen.
    const float SwitchHeight = 0.1f;
    const float SwitchDip = 0.1f;        // around that moment the sky fades out and back in (it's drawn from this light)
    const float MoonRise = 0.25f;        // the moon is at full brightness this much higher (about 20 degrees up)
    const float NightReflection = 0.3f;  // how much the sky shows in shiny things at night (1 = as in the day)
    static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");   // the Procedural Skybox's tint
    static readonly int TintId = Shader.PropertyToID("_Tint");         // other skies' tint (Cubemap, 6 Sided, Panoramic)

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

    // When each part of the day starts (Time Of Day, 0 - 1), e.g. for a "win at dawn" rule.
    public float DawnStart => dawnStart;
    public float MorningStart => morningStart;
    public float NoonStart => noonStart;
    public float AfternoonStart => afternoonStart;
    public float DuskStart => duskStart;
    public float NightStart => nightStart;

    // The clock: 0.5 -> 12:00, 0.77 -> 18:28.
    public int Hour => Mathf.FloorToInt(timeOfDay * 24f) % 24;
    public int Minute => Mathf.FloorToInt(timeOfDay * 24f * 60f) % 60;
    public string ClockText => Hour.ToString("00") + ":" + Minute.ToString("00");

    // Real seconds until night / morning starts (0 if it's already that time).
    public float SecondsUntilNight => IsNight ? 0f : RealSecondsUntil(nightStart);
    public float SecondsUntilMorning => IsNight ? RealSecondsUntil(dawnStart) : 0f;

    // How high the sun is right now: 1 = straight up (noon), 0 = on the horizon (sunrise / sunset), -1 = straight down
    // (midnight). E.g. ViewDistance's mist turns orange while the sun is near the horizon.
    public float SunHeight => -Mathf.Cos(timeOfDay * Mathf.PI * 2f);

    // How bright the sky is drawn because of the sun-to-moon change: 1 normally, dipping to 0 for a moment while this
    // light turns from sun into moon (and back at dawn). Always 1 with Moonlight off.
    public float SkyDip => moonlight ? Mathf.SmoothStep(0f, 1f, Mathf.Abs(SunHeight + SwitchHeight) / SwitchDip) : 1f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Current = this;
        sun = GetComponent<Light>();
        sunShadowStrength = sun.shadowStrength;
        if (RenderSettings.sun == null) RenderSettings.sun = sun;   // the procedural sky follows this light

        startTimeOfDay = timeOfDay;   // remember the start, to go back to it when the player dies
        startDay = day;
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        CheckSkyLight();
        RememberSky();
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        if (skyCopy != null)   // give the scene its own sky material back
        {
            if (RenderSettings.skybox == skyCopy) RenderSettings.skybox = originalSky;
            Destroy(skyCopy);
        }
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

    void OnPlayerDied()
    {
        if (resetOnPlayerDeath) ResetToStart();
    }

    // Back to how the game started: the starting day, time of day and light. Listening scripts hear about it
    // (e.g. dying at night = DayStarted, so Marko's EnemySpawner removes the night's enemies).
    public void ResetToStart()
    {
        timeOfDay = startTimeOfDay;
        day = startDay;
        ApplyLight();   // the sun and sky change right away, not next frame
        CheckPhase();
    }

    // Starts the game at another time of day (0 = midnight, 0.5 = midday), e.g. chosen on the New Game page.
    // Also where dying brings the player back to (Reset On Player Death). Best called before the first frame.
    public void SetStartTime(float time)
    {
        timeOfDay = Mathf.Repeat(time, 1f);
        startTimeOfDay = timeOfDay;
        ApplyLight();
    }

    // How many real minutes one full day (day + night) takes from now on.
    public void SetDayLength(float minutes)
    {
        dayLengthMinutes = Mathf.Clamp(minutes, 0.5f, 60f);
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
        // Where the sun is: 0.25 (sunrise) = 0 degrees (horizon), 0.5 (noon) = 90 (straight down), 0.75 (sunset) = 180.
        float angle = timeOfDay * 360f - 90f;
        Quaternion sunTurn = Quaternion.Euler(angle, sunDirection, 0f);
        // How high the sun is: 1 = straight up, 0 = horizon, -1 = straight down (midnight). The light shines along its
        // forward, so the sun itself is the other way.
        float height = -(sunTurn * Vector3.forward).y;

        // Once the sun is a bit below the horizon, this light is the moon (with Moonlight on).
        if (moonlight && height < -SwitchHeight) ShineAsMoon(angle, -height);
        else ShineAsSun(sunTurn, height);

        ApplyAmbient(Mathf.Clamp01(height * 2f + 0.3f));   // 1 = the day's light from everywhere, 0 = the night's
        ApplySky(height);
    }

    // This light is the sun: bright in the day, fading out quickly as the sun goes below the horizon.
    void ShineAsSun(Quaternion turn, float height)
    {
        transform.rotation = turn;
        // "* 4" makes the fade short: full brightness once the sun is a bit above the horizon.
        sun.intensity = maxSunIntensity * Mathf.Clamp01(height * 4f + 0.1f);
        sun.color = sunColor.Evaluate(Mathf.Clamp01(height));
        sun.shadowStrength = sunShadowStrength;
        // No sun shadows from below the ground at night. With Moonlight on it stays on (with no brightness left) until
        // it turns into the moon, because the sky is drawn from where this light is.
        sun.enabled = moonlight || height > -0.1f;
    }

    // This light is the moon: on the other side of the sky from the sun, pale blue, starting with no light at all (so
    // the change from sun to moon isn't seen) and getting brighter as it rises.
    void ShineAsMoon(float sunAngle, float moonHeight)
    {
        transform.rotation = Quaternion.Euler(sunAngle + 180f, sunDirection, 0f);
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SwitchHeight, SwitchHeight + MoonRise, moonHeight));
        sun.intensity = moonIntensity * rise;
        sun.color = moonColor;
        sun.shadowStrength = moonShadowStrength;
        sun.enabled = true;
    }

    // The light from everywhere (it lights the shadows): the sky's own saved light in the day, a dark blue at night
    // (Moonlight on), or the day's light times Night Ambient (Moonlight off). dayShare: 1 = day, 0 = night.
    void ApplyAmbient(float dayShare)
    {
        if (!skyRemembered) return;   // before Start: the day's sky isn't remembered yet
        RenderSettings.reflectionIntensity = Mathf.Lerp(NightReflection, dayReflection, dayShare);
        if (!skyLightSaved) return;   // Unity works the sky's light out by itself (see CheckSkyLight): left to Unity

        SphericalHarmonicsL2 night;
        if (moonlight)
        {
            night = new SphericalHarmonicsL2();   // the same blue from every side
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;   // the lighting's maths uses linear colors
            night.AddAmbientLight(linear ? nightAmbientColor.linear : nightAmbientColor);
        }
        else night = dayProbe * nightAmbient;
        RenderSettings.ambientProbe = dayProbe * (dayAmbient * dayShare) + night * (1f - dayShare);
    }

    // The sky (a sky material with an Exposure, e.g. the Procedural Skybox): a dim blue at night with the moon where the
    // sun's disk would be. The sky is drawn from where this light is, so it fades out for a moment while the light
    // turns from sun into moon near the horizon (and back at dawn), or the jump would show.
    void ApplySky(float height)
    {
        if (skyCopy == null) return;
        float night = moonlight ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, -0.25f, height)) : 0f;   // 1 = night sky
        float dip = SkyDip;   // 0 = at the change
        skyCopy.SetFloat(ExposureId, Mathf.Lerp(dayExposure, nightSkyExposure, night) * dip);
        if (hasTint) skyCopy.SetColor(tintId, Color.Lerp(dayTint, nightSkyTint, night));
    }

    // Remembers the day's sky before the night changes it: its light from everywhere (ambient), how much it shows in
    // shiny things, and its sky material, which is swapped for a copy that the night can change.
    void RememberSky()
    {
        dayProbe = RenderSettings.ambientProbe;
        skyLightSaved = !IsBlack(dayProbe);
        dayReflection = RenderSettings.reflectionIntensity;
        skyRemembered = true;

        Material sky = RenderSettings.skybox;
        if (sky == null || !sky.HasProperty(ExposureId)) return;   // no sky, or one without an Exposure: only the light changes
        originalSky = sky;
        skyCopy = new Material(sky) { name = sky.name + " (day and night)" };
        RenderSettings.skybox = skyCopy;
        dayExposure = sky.GetFloat(ExposureId);
        hasTint = true;
        if (sky.HasProperty(SkyTintId)) tintId = SkyTintId;
        else if (sky.HasProperty(TintId)) tintId = TintId;
        else hasTint = false;
        if (hasTint) dayTint = sky.GetColor(tintId);
    }

    // The light from the whole sky (ambient light) is what lights the shadows. Unity works it out by itself only for
    // the scene opened in the Editor. A scene the title screen opens (or a built game) gets only what was saved for it
    // with Tools > World > Set Up Sky Lighting: nothing saved = no sky light = every shadow pitch black. Then this asks
    // Unity to work it out from the sky now (a backup: the sky's reflections stay missing) and says how to fix it.
    void CheckSkyLight()
    {
        if (RenderSettings.ambientMode != AmbientMode.Skybox || RenderSettings.skybox == null) return;   // not lit by the sky
        if (!IsBlack(RenderSettings.ambientProbe)) return;   // saved, or worked out by the Editor: fine

        DynamicGI.UpdateEnvironment();
        Debug.LogWarning("DaylightCycle: \"" + gameObject.scene.name + "\" has no saved light from the sky, so its shadows " +
                         "are pitch black when the title screen opens it. Fix: open it, then Tools > World > Set Up Sky Lighting.");
    }

    // True if the ambient light holds no light at all (all its numbers are 0).
    static bool IsBlack(SphericalHarmonicsL2 sky)
    {
        for (int color = 0; color < 3; color++)
            for (int part = 0; part < 9; part++)
                if (Mathf.Abs(sky[color, part]) > 0.0001f) return false;
        return true;
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
