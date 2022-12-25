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
    public GameObject SelectionImage;

    public void Initialize(SaveData saveData)
    {
        CharacterName.text = saveData.CharacterName;
        SaveTime.text = saveData.TotalTime;
        SaveName = saveData.SaveName;
        _SaveData = saveData;
        SelectionImage.SetActive(false);
    }
}