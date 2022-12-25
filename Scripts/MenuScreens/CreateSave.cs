using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateSave : MonoBehaviour
{
    public TMP_InputField CharacterName;

    public int SeedRange = 100000;

    public void NewGame()
    {
        string[] biomeIDs = new string[World.Biomes.Length];
        for (int i = 0; i < biomeIDs.Length; i++)
        {
            biomeIDs[i] = World.Biomes[i].BiomeName;
        }

        if (CharacterName.text != "")
        {
            string saveName = SaveData.SaveMap(CharacterName.text, biomeIDs);
            if (saveName != "")
            {
                World.InitializeWorldData(SaveData.LoadData(saveName));
                SceneTransitioner.LoadScene("GameWorld", true);
            }
        }
    }
}