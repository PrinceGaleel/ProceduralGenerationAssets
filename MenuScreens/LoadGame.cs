using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

public class LoadGame : MonoBehaviour
{
    [Header("Main")]
    public GameObject SaveDisplayPrefab;
    public Transform SaveContainer;
    private SaveInfoContainer Selected;

    [Header("Confirmation Screens")]
    public GameObject LoadConfirmationScreen;
    public GameObject DeleteConfirmationScreen;

    private void Awake()
    {
        DeleteConfirmationScreen.SetActive(false);
        LoadConfirmationScreen.SetActive(false);

        string path = Application.persistentDataPath;
        string[] dirs = Directory.GetDirectories(path);
        List<SaveData> saves = new();

        for (int i = 0; i < dirs.Length; i++)
        {
            if (File.Exists(path + "/" + Path.GetFileName(dirs[i]) + "/SaveData.pgw"))
            {
                FileStream stream = new(path + "/" + Path.GetFileName(dirs[i]) + "/SaveData.pgw", FileMode.Open);

                if (stream.Length > 0)
                {
                    try
                    {
                        SaveData saveData = new BinaryFormatter().Deserialize(stream) as SaveData;
                        saveData.SaveName ??= Path.GetFileName(dirs[i]);
                        saves.Add(saveData);
                    }
                    catch (SerializationException) { }
                }

                stream.Close();
            }
        }

        if (saves.Count > 0)
        {
            for (int i = 0; i < saves.Count; i++)
            {
                Instantiate(SaveDisplayPrefab, SaveContainer).GetComponent<SaveInfoContainer>().Initialize(saves[i], this);
            }
        }
    }

    public void Back()
    {
        SceneTransitioner.LoadScene("MainMenu", false, false);
    }

    public void DeleteSave()
    {
        if (Selected)
        {
            string path = Application.persistentDataPath + "/" + Selected.SaveName;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Destroy(Selected.gameObject);
            CancelDelete();
        }
    }

    public void LoadSave()
    {
        if (Selected)
        {
            if (SaveHandler.LoadData(Selected._SaveData.SaveNum.ToString(), out SaveData saveData))
            {
                GameManager.InitializeWorldData(saveData);
                SceneTransitioner.LoadScene("GameWorld", false, true);
            }

            CancelLoad();
        }
    }

    public void ToggleLoadConfirmation(SaveInfoContainer saveInfo)
    {
        if (!Selected)
        {
            Selected = saveInfo;
            LoadConfirmationScreen.SetActive(true);
        }
    }

    public void ToggleDeleteConfirmation(SaveInfoContainer saveInfo)
    {
        if (!Selected)
        {
            Selected = saveInfo;
            DeleteConfirmationScreen.SetActive(true);
        }
    }

    public void CancelDelete()
    {
        Selected = null;
        DeleteConfirmationScreen.SetActive(false);
    }

    public void CancelLoad()
    {
        Selected = null;
        LoadConfirmationScreen.SetActive(false);
    }
}