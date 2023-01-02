using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Panel Parents")]
    public GameObject[] ParentPanels;
    private int CurrentPanel;

    [Header("Panels")]
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;

    private void Awake()
    {
        if (ParentPanels != null)
        {
            if (ParentPanels.Length > 0)
            {
                CurrentPanel = 0;
                ParentPanels[0].SetActive(true);

                for (int i = 1; i < ParentPanels.Length; i++)
                {
                    ParentPanels[i].SetActive(false);
                }
            }
        }

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

    public void OpenMainPanel()
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

    public void ChangePanel(int index)
    {
        ParentPanels[CurrentPanel].SetActive(false);
        CurrentPanel = index;
        ParentPanels[CurrentPanel].SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
