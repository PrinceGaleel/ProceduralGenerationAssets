using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayTimeController : MonoBehaviour
{
    public Light DirectionalLight;
    public LightingPreset Preset;

    public static float TotalSeconds;
    public static float TimeMultiplier;

    private void Awake()
    {
        Initialize(12);
    }

    private void Update()
    {
        TotalSeconds = Mathf.Repeat(TotalSeconds + (Time.deltaTime * TimeMultiplier), 86400);
        UIController.Instance.TimeText.text = TimeToString();
        UpdateLighting(TotalSeconds / 86400);
    }

    private void UpdateLighting(float timePercent)
    {
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);
            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
        }
    }

    public static string TimeToString()
    {
        string hours = Mathf.Floor(TotalSeconds / 3600).ToString();
        string minutes = Mathf.Floor((TotalSeconds - (Mathf.Floor(TotalSeconds / 3600) * 3600)) / 60).ToString();

        if(hours.Length == 1)
        {
            hours = "0" + hours;
        }

        if(minutes.Length == 1)
        {
            minutes = "0" + minutes;
        }

        return hours + ":" + minutes;
    }

    public static void Initialize(float startHour, float timeMultiplier = 20)
    {
        TimeMultiplier = timeMultiplier;
        SetTimeOfDay(startHour);
    }

    public static void SetTimeOfDay(float hour)
    {
        TotalSeconds = 3600 * hour;
    }
}