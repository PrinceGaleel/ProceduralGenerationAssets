using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;

    private void Awake()
    {
        OpenMainPanel();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (MainPanel.activeSelf)
            {
                UIController.Instance.TogglePauseMenu(false);
            }
        }
    }

    private void OpenMainPanel()
    {
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);

        if (CreditsPanel)
        {
            CreditsPanel.SetActive(false);
        }
    }

    public void ToggleSettings(bool isEnabled)
    {
        SettingsPanel.SetActive(isEnabled);
        MainPanel.SetActive(!isEnabled);
    }

    public void ToggleCredits(bool isEnabled)
    {
        MainPanel.SetActive(!isEnabled);
        CreditsPanel.SetActive(isEnabled);
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
