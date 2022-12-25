using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayTimeController : MonoBehaviour
{
    public float TimeMultiplier;
    public float StartHour;

    //
    private DateTime CurrentTime;
    public Light SunLight;
    public Light MoonLight;

    //
    public float SunRiseHour;
    public float SunSetHour;
    private TimeSpan SunRiseTime;
    private TimeSpan SunSetTime;

    //
    public Color DayAmbientLight;
    public Color NightAmbientLight;
    public AnimationCurve LightChangeCurve;
    public float MaxSunLightIntensity;
    public float MaxMoonLightIntensity;

    // Start is called before the first frame update
    private void Start()
    {
        CurrentTime = DateTime.Now.Date + TimeSpan.FromHours(StartHour);
        SunRiseTime = TimeSpan.FromHours(SunRiseHour);
        SunSetTime = TimeSpan.FromHours(SunSetHour);
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
    }

    private void UpdateTimeOfDay()
    {
        CurrentTime = CurrentTime.AddSeconds(Time.deltaTime * TimeMultiplier);
        UIController.Instance.TimeText.text = CurrentTime.ToString("HH:mm");
    }

    private void UpdateLightSettings()
    {
        float dotProduct = Vector3.Dot(SunLight.transform.forward, Vector3.down);
        SunLight.intensity = Mathf.Lerp(0, MaxSunLightIntensity, LightChangeCurve.Evaluate(dotProduct));
        MoonLight.intensity = Mathf.Lerp(MaxMoonLightIntensity, 0, LightChangeCurve.Evaluate(dotProduct));
        RenderSettings.ambientLight = Color.Lerp(NightAmbientLight, DayAmbientLight, LightChangeCurve.Evaluate(dotProduct));
    }

    private void RotateSun()
    {
        float sunLightRotation;

        if(CurrentTime.TimeOfDay > SunRiseTime && CurrentTime.TimeOfDay < SunSetTime)
        {
            TimeSpan sunRiseToSunSetDuration = CalculateTimeDifference(SunRiseTime, SunSetTime);
            TimeSpan timeSinceSunrise = CalculateTimeDifference(SunRiseTime, CurrentTime.TimeOfDay);

            double percentage = timeSinceSunrise.TotalMinutes / sunRiseToSunSetDuration.TotalMinutes;

            sunLightRotation = Mathf.Lerp(0, 180, (float)percentage);
        }
        else
        {
            TimeSpan sunSetToSunRiseDuration = CalculateTimeDifference(SunSetTime, SunRiseTime);
            TimeSpan timeSinceSunSet = CalculateTimeDifference(SunSetTime, CurrentTime.TimeOfDay);

            double percentage = timeSinceSunSet.TotalMinutes / sunSetToSunRiseDuration.TotalMinutes;

            sunLightRotation = Mathf.Lerp(180, 360, (float)percentage);
        }

        SunLight.transform.rotation = Quaternion.AngleAxis(sunLightRotation, Vector3.right);
    }

    private TimeSpan CalculateTimeDifference(TimeSpan fromTime, TimeSpan toTime)
    {
        TimeSpan difference = toTime - fromTime;

        if(difference.TotalSeconds < 0)
        {
            difference += TimeSpan.FromHours(24);
        }

        return difference;
    }
}
