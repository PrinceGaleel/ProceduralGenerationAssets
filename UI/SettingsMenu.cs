using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance;

    [Header("FPS")]
    private readonly static FullScreenMode[] ScreenModes = new FullScreenMode[3] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };

    public TMP_Dropdown ResolutionsDropdown;
    public TMP_Dropdown ScreenModesDropdown;

    private const int FPSIncremenet = 10;
    private const int MinFPS = 30;
    private const int MaxFPS = 300;

    public Slider FPSSlider;
    public TextMeshProUGUI FPSText;
    public Toggle VSyncToggle;

    [Header("Other")]
    public Slider MusicSlider;
    public TextMeshProUGUI MusicVolumeText;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple settings menus detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            InitializeSettings();
        }
    }

    public void OnLeave()
    {
        int vSync;
        if (VSyncToggle.isOn)
        {
            vSync = 2;
        }
        else
        {
            vSync = 0;
        }

        GlobalSettings.CurrentSettings._ScreenSettings = new(Screen.resolutions[ResolutionsDropdown.value].width, Screen.resolutions[ResolutionsDropdown.value].height, 
            ScreenModes[ScreenModesDropdown.value], (int)(FPSSlider.value * FPSIncremenet), vSync);
        GlobalSettings.CurrentSettings.MusicVolume = MusicSlider.value / 100;

        GlobalSettings.SaveSettings();
    }

    public void InitializeSettings()
    {
        MusicSlider.value = GlobalSettings.CurrentSettings.MusicVolume * 100; 

        FPSSlider.minValue = MinFPS / FPSIncremenet;
        FPSSlider.maxValue = MaxFPS / FPSIncremenet;
        FPSSlider.value = Application.targetFrameRate / FPSIncremenet;
        FPSText.text = Application.targetFrameRate.ToString();

        if (QualitySettings.vSyncCount == 0)
        {
            VSyncToggle.isOn = false;
        }
        else
        {
            VSyncToggle.isOn = true;
        }

        int toSet = 0;
        List<TMP_Dropdown.OptionData> optionsData = new(ScreenModes.Length);
        for (int i = 0; i < ScreenModes.Length; i++)
        {
            if (ScreenModes[i] == Screen.fullScreenMode)
            {
                toSet = i;
            }

            optionsData.Add(new(ScreenModes[i].ToString()));
        }
        ScreenModesDropdown.options = optionsData;
        ScreenModesDropdown.value = toSet;

        toSet = 0;
        optionsData = new(Screen.resolutions.Length);
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            if (Screen.resolutions[i].height == Screen.currentResolution.height && Screen.resolutions[i].width == Screen.currentResolution.width)
            {
                toSet = i;
            }

            optionsData.Add(new(Screen.resolutions[i].width + " x " + Screen.resolutions[i].height));
        }
        ResolutionsDropdown.options = optionsData;
        ResolutionsDropdown.value = toSet;
    }

    public void ChangeMusicVolume()
    {
        MusicVolumeText.text = Mathf.FloorToInt(MusicSlider.value).ToString();
    }
}