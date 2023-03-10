using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject MainPanel, CreateLoad, CharacterCreator, SettingsPanel, CreditsPanel, LoadPanel;

    private GameObject LastToEnable;

    private void Awake()
    {
        MainPanel.SetActive(true);
        CreateLoad.SetActive(false);
        CharacterCreator.SetActive(false);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        LoadPanel.SetActive(false);

        LastToEnable = MainPanel;
    }

    public void EnableMain()
    {
        TogglePanel(MainPanel);
    }

    public void EnableSettings()
    {
        TogglePanel(SettingsPanel);
    }

    public void EnableCredits()
    {
        TogglePanel(CreditsPanel);
    }

    public void EnableLoadPanel()
    {
        TogglePanel(LoadPanel);
    }

    public void EnableCharacterCreator()
    {
        TogglePanel(CharacterCreator);
    }

    private void TogglePanel(GameObject panel)
    {
        LastToEnable.SetActive(false);
        LastToEnable = panel;
        LastToEnable.SetActive(true);
    }

    public void ToScene(string sceneName)
    {
        SceneTransitioner.LoadScene(sceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}