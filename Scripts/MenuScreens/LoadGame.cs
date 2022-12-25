using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadGame : MonoBehaviour
{
    [Header("Main")]
    public GameObject SaveDisplayPrefab;
    public Transform SaveContainer;
    public SaveInfoContainer Selected;
    private bool IsConfirmating;

    [Header("Raycast Info")]
    public GraphicRaycaster GR;
    public EventSystem EV;

    [Header("Confirmation Screens")]
    public GameObject LoadConfirmationScreen;
    public GameObject DeleteConfirmationScreen;

    private void Awake()
    {
        DeleteConfirmationScreen.SetActive(false);
        LoadConfirmationScreen.SetActive(false);
        IsConfirmating = false;

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
                    SaveData saveData = new BinaryFormatter().Deserialize(stream) as SaveData;

                    if(saveData.SaveName == null)
                    {
                        saveData.SaveName = Path.GetFileName(dirs[i]);
                    }

                    saves.Add(saveData);
                }

                stream.Close();
            }
        }

        if (saves.Count > 0)
        {
            for (int i = 0; i < saves.Count; i++)
            {
                Instantiate(SaveDisplayPrefab, SaveContainer).GetComponent<SaveInfoContainer>().Initialize(saves[i]);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && IsConfirmating == false)
        {
            List<RaycastResult> results = new();
            PointerEventData pointerEventData = new(EV);
            pointerEventData.position = Input.mousePosition;
            GR.Raycast(pointerEventData, results);

            bool deselectSave = true;
            SaveInfoContainer temp = null;

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.GetComponent<SaveInfoContainer>())
                {
                    temp = result.gameObject.GetComponent<SaveInfoContainer>();
                    break;
                }
                else if (result.gameObject.GetComponent<Button>())
                {
                    deselectSave = false;
                    break;
                }
            }

            if (deselectSave)
            {
                if (Selected)
                {
                    Selected.SelectionImage.SetActive(false);
                }

                if (temp)
                {
                    Selected = temp;
                    Selected.SelectionImage.SetActive(true);
                }
            }
        }
    }

    public void Back()
    {
        SceneTransitioner.LoadScene("MainMenu");
    }

    public void DeleteSave()
    {
        if(Selected)
        {
            ShowDeleteConfirmation(false);

            string path = Application.persistentDataPath + "/" + Selected.SaveName;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Destroy(Selected.gameObject);
        }
    }

    public void LoadSave()
    {
        if (Selected)
        {
            ShowLoadConfirmation(false);
        }
    }

    public void ShowLoadConfirmation(bool isActive)
    {
        if (Selected)
        {
            IsConfirmating = isActive;
            LoadConfirmationScreen.SetActive(isActive);
        }
    }

    public void ShowDeleteConfirmation(bool isActive)
    {
        if (Selected)
        {
            IsConfirmating = isActive;
            DeleteConfirmationScreen.SetActive(isActive);
        }
    }
}
