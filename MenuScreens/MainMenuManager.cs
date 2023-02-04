using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    public GameObject Main;
    public GameObject Multiplayer;
    public GameObject CreateLoad;
    public GameObject CharacterCreator;

    [Header("Main Panels")]
    public GameObject MainMainPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;

    [Header("Create Load Panels")]
    public GameObject MainCreateLoadPanel;
    public GameObject LoadPanel;

    [Header("Multiplayer")]
    public GameObject MainCreateJoinPanel;
    public GameObject CreateGamePanel;
    public GameObject JoinGamePanel;
    public TMP_InputField JoinGameInput;
    public TMP_InputField CreateGameInput;
    public Toggle IsPublic;

    private void Awake()
    {
        Main.SetActive(true);
        CreateLoad.SetActive(false);
        Multiplayer.SetActive(false);
        CharacterCreator.SetActive(false);

        ToggleLoadPanel(false);

        MainMainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);

        MainCreateLoadPanel.SetActive(true);
        LoadPanel.SetActive(false);

        MainCreateJoinPanel.SetActive(true);
        CreateGamePanel.SetActive(false);
        JoinGamePanel.SetActive(false);
    }

    public void OpenMainPanel()
    {
        MainMainPanel.SetActive(true);
        SettingsPanel.SetActive(false);

        if (CreditsPanel)
        {
            CreditsPanel.SetActive(false);
        }
    }

    public void ToggleSettings(bool isEnabled)
    {
        SettingsPanel.SetActive(isEnabled);
        MainMainPanel.SetActive(!isEnabled);
    }

    public void ToggleCredits(bool isEnabled)
    {
        MainMainPanel.SetActive(!isEnabled);
        CreditsPanel.SetActive(isEnabled);
    }

    public void ToggleLoadPanel(bool isEnabled)
    {
        LoadPanel.SetActive(isEnabled);
        MainCreateLoadPanel.SetActive(!isEnabled);
    }

    public void OpenSingleplayer()
    {
        MultiplayerSettings.IsMultiplayer = false;
        Main.SetActive(false);
        CreateLoad.SetActive(true);
    }

    public void ToggleMultiplayer(bool isEnabled)
    {
        MultiplayerSettings.IsMultiplayer = isEnabled;
        Main.SetActive(!isEnabled);
        Multiplayer.SetActive(isEnabled);
    }

    public void ToggleCharacterCreator(bool isEnabled)
    {
        CharacterCreator.SetActive(isEnabled);
        CreateLoad.SetActive(!isEnabled);
    }

    public void ToggleCreateLoad(bool isEnabled)
    {
        CreateLoad.SetActive(isEnabled);

        if (MultiplayerSettings.IsMultiplayer)
        {
            Multiplayer.SetActive(!isEnabled);

            if (!isEnabled)
            {

            }
        }
        else
        {
            Main.SetActive(!isEnabled);

            if (!isEnabled)
            {

            }
        }
    }

    public void ToggleJoinGame(bool isEnabled)
    {
        MainCreateJoinPanel.SetActive(!isEnabled);
        JoinGamePanel.SetActive(isEnabled);
    }

    public void ToggleCreateGame(bool isEnabled)
    {
        MainCreateJoinPanel.SetActive(!isEnabled);
        CreateGamePanel.SetActive(isEnabled);
    }

    public void JoinGame()
    {
        if (PhotonNetwork.JoinRoom(JoinGameInput.text))
        {

        }
    }

    public void CreateGame()
    {
        if (ExtraUtils.RemoveSpace(CreateGameInput.text) != "")
        {
            Photon.Realtime.RoomOptions options = new();
            options.IsVisible = false;
            options.IsOpen = true;

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            if (PhotonNetwork.CreateRoom(ExtraUtils.RemoveSpace(CreateGameInput.text), options))
            {
                ToggleCreateLoad(true);
            }
        }
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