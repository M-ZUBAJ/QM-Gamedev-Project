using System.Collections;
using System.Collections.Generic;

using UnityEngine;


/// <summary>
/// This script is responsible for switching the day skybox to the night skybox
/// </summary>
public class NightLightingController : MonoBehaviour
{
    [Header("Skyboxes")]
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material nightSkybox;

    [Header("Moonlight")]
    [SerializeField] private Light moonLight;

    private void OnEnable()
    {
        DaylightCycle.NightStarted += OnNightStarted;
        DaylightCycle.DayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        DaylightCycle.NightStarted -= OnNightStarted;
        DaylightCycle.DayStarted -= OnDayStarted;
    }

    private void Start()
    {
        // Match the lighting to the time the game starts.
        if (DaylightCycle.Current != null &&
            DaylightCycle.Current.IsNight)
        {
            SetNight();
        }
        else
        {
            SetDay();
        }
    }

    private void OnNightStarted(int day)
    {
        SetNight();
    }

    private void OnDayStarted(int day)
    {
        SetDay();
    }

    private void SetNight()
    {
        if (nightSkybox != null)
            RenderSettings.skybox = nightSkybox;

        if (moonLight != null)
            moonLight.enabled = true;

        DynamicGI.UpdateEnvironment();
    }

    private void SetDay()
    {
        if (daySkybox != null)
            RenderSettings.skybox = daySkybox;

        if (moonLight != null)
            moonLight.enabled = false;

        DynamicGI.UpdateEnvironment();
    }
}
