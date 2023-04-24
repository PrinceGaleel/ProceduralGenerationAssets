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

    private void Start()
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

    public void OpenMainPanel()
    {
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
    }

    public void ToggleSettings(bool isEnabled)
    {
        SettingsPanel.SetActive(isEnabled);
        MainPanel.SetActive(!isEnabled);
    }
    
    public void ToScene(string sceneName)
    {
        SceneTransitioner.LoadScene(sceneName, false, false);
    }
}
