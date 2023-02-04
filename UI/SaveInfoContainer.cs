using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveInfoContainer : MonoBehaviour
{
    public TextMeshProUGUI CharacterName;
    public TextMeshProUGUI SaveTime;
    public string SaveName;
    public SaveData _SaveData;
    private LoadGame _LoadGame;

    public void Initialize(SaveData saveData, LoadGame loadGame)
    {
        CharacterName.text = saveData.CharacterName;
        SaveTime.text = saveData.TotalTime;
        SaveName = saveData.SaveName;
        _SaveData = saveData;
        _LoadGame = loadGame;
    }

    public void AttemptDeleteSave()
    {
        _LoadGame.ToggleDeleteConfirmation(this);
    }

    public void AttemptLoadSave()
    {
        _LoadGame.ToggleLoadConfirmation(this);
    }
}