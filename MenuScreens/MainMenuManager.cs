using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private GameObject LastToEnable;

    private void Awake()
    {
        if (GrassManager.GrassComputeShader == null)
        {
            SceneManager.LoadScene("FirstScene");
        }
    }

    private void Start()
    {
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform) { child.gameObject.SetActive(false); }
            LastToEnable = transform.GetChild(0).gameObject;
            LastToEnable.SetActive(true);
        }
    }

    public void TogglePanel(GameObject panel)
    {
        LastToEnable.SetActive(false);
        LastToEnable = panel;
        LastToEnable.SetActive(true);
    }

    public void ToScene(string sceneName) { SceneTransitioner.LoadScene(sceneName, false, false); }

    public void Quit() { Application.Quit(); }
}