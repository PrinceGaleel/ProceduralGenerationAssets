using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("FPS")]
    private readonly static FullScreenMode[] ScreenModes = new FullScreenMode[3] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };

    public TMP_Dropdown ResolutionsDropdown;
    public TMP_Dropdown ScreenModesDropdown;

    public Slider FPSSlider;
    public TextMeshProUGUI FPSText;
    public Toggle VSyncToggle;

    [Header("Other")]
    public Slider MusicSlider;
    public TextCarousel ViewDistance;

    private void Awake()
    {
        FPSSlider.value = Application.targetFrameRate;
        FPSText.text = Application.targetFrameRate.ToString();

        ViewDistance.SetOptions(new string[10] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }, GlobalSettings.CurrentSettings.ViewDistance);

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

    public void SetFPSText()
    {
        FPSText.text = FPSSlider.value.ToString();
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

        GlobalSettings.CurrentSettings.SetViewDistance = ViewDistance.CurrentPosition;
        GlobalSettings.CurrentSettings._ScreenSettings = new(Screen.resolutions[ResolutionsDropdown.value].width, Screen.resolutions[ResolutionsDropdown.value].height, 
            ScreenModes[ScreenModesDropdown.value], Mathf.FloorToInt(FPSSlider.value), vSync);
        GlobalSettings.SaveSettings();
    }

    public static void InitializeSettings()
    {

    }
}