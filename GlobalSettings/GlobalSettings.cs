using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class GlobalSettings
{
    public static GlobalSettings CurrentSettings;

    [Header("View Distance")]
    public const int MinViewDistance = 200;
    public const int MaxViewDistance = 1000;
    public int SetViewDistance
    {
        set
        {
            ViewDistance = value;
            ViewDistanceInWorld = Mathf.FloorToInt(MinViewDistance + ((MaxViewDistance - MinViewDistance) * (value / (float)10)));
        }
    }
    public int ViewDistance { get; private set; }
    public int ViewDistanceInWorld { get; private set; }

    [Header("Graphics")]
    public ScreenSettings _ScreenSettings;

    [Header("Audio")]
    public float MasterVolume;
    public float MusicVolume;

    [Header("Grass Settings")]
    public const int BladesPerVertex = 3;
    public const int SegmentsPerBlade = 3;

    [Header("Grass LOD")]
    public const float minFadeDistance = 40;
    public const float maxFadeDistance = 60;
    public const float GrassAffectRadius = 1.5f;
    public const float GrassAffectStrength = 2;

    public GlobalSettings()
    {
        _ScreenSettings = new();
        ViewDistance = 1;
        MasterVolume = 0.6f;
        ViewDistanceInWorld = 200;
    }

    public GlobalSettings(GlobalSettings settings)
    {
        ViewDistance = settings.ViewDistance;
        ViewDistanceInWorld = settings.ViewDistanceInWorld;

        _ScreenSettings = settings._ScreenSettings;
        
        MasterVolume = settings.MasterVolume;
        MusicVolume = settings.MusicVolume;
    }

    public static void SaveSettings()
    {
        ApplySettings();
        BinaryFormatter formatter = new();
        FileStream stream = new(Application.persistentDataPath + "/Settings.s", FileMode.Create);
        formatter.Serialize(stream, CurrentSettings);
        stream.Close();
    }

    public static void LoadSettings()
    {
        string path = Application.persistentDataPath + "/Settings.s";

        if (File.Exists(path))
        {
            FileStream stream = new(path, FileMode.Open);

            if (stream.Length > 0)
            {
                try
                {
                    CurrentSettings = new(new BinaryFormatter().Deserialize(stream) as GlobalSettings);

                    ApplySettings();
                }
                catch(Exception e)
                {
                    stream.Close();
                    File.Delete(path);
                    Debug.Log(e);

                    ResetSettings();

                    return;
                }
            }

            stream.Close();
        }
        else
        {
            ResetSettings();
        }
    }

    private static void ApplySettings()
    {
        CurrentSettings.SetViewDistance = CurrentSettings.ViewDistance;
        CurrentSettings._ScreenSettings.AssignResolution();
    }

    private static void ResetSettings()
    {
        CurrentSettings = new();
        SaveSettings();
    }
}

[Serializable]
public class ScreenSettings
{
    public FullScreenMode ScreenMode;
    public int ScreenWidth;
    public int ScreenHeight;
    public int TargetFPS;
    public int VSyncCount;

    public ScreenSettings()
    {
        AutoAssignScreenSize();
        ScreenMode = FullScreenMode.ExclusiveFullScreen;
        TargetFPS = 60;
        VSyncCount = 0;
    }

    public ScreenSettings(int screenWidth, int screenHeight, FullScreenMode screenMode, int fpsTarget, int vSync)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        ScreenMode = screenMode;
        TargetFPS = fpsTarget;
        VSyncCount = vSync;
    }

    private void AutoAssignScreenSize()
    {
        ScreenWidth = 1920;
        ScreenHeight = 1080;

        if (Screen.resolutions.Length > 0)
        {
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                if (Screen.resolutions[i].width > ScreenWidth)
                {
                    ScreenWidth = Screen.resolutions[i].width;
                    ScreenHeight = Screen.resolutions[i].height;
                }
            }
        }
    }

    public void AssignResolution()
    {
        Screen.SetResolution(ScreenWidth, ScreenHeight, ScreenMode);
        Application.targetFrameRate = TargetFPS;
        QualitySettings.vSyncCount = VSyncCount;
    }
}