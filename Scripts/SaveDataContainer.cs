using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class SaveData
{
    public int SaveNum;
    public const float HeightMultipler = 200;
    private const float MaxPerlinOffset = 100000;

    [Header("Base Perlin Settings")]
    public PerlinData HeightPerlin;
    public PerlinData TemperaturePerlin;

    [Header("World Data")]
    public Vector3Serializable LastPosition;
    public string[] BiomesIDs;

    [Header("Save Info")]
    public string SaveName;
    public string CharacterName;
    public float TimeInSeconds;

    public string TotalTime
    {
        get
        {
            TimeSpan t = TimeSpan.FromSeconds(TimeInSeconds);
            return t.ToString(@"hh\:mm\:ss");
        }
    }

    public SaveData(string characterName, string[] biomeIDs, string saveName, PerlinData heightPerlin, PerlinData temperaturePerlin)
    {
        HeightPerlin = heightPerlin;
        TemperaturePerlin = temperaturePerlin;

        LastPosition = new();
        SaveName = saveName;
        CharacterName = characterName;
        TimeInSeconds = 0;
        BiomesIDs = biomeIDs;
        LastPosition = new();
    }

    public static string SaveMap(string characterName, string[] biomeIDs)
    {
        BinaryFormatter formatter = new();

        int saveNum = GetFreeSaveNum();
        string savePath = Application.persistentDataPath + "/" + saveNum;

        System.Random random = new();
        Vector2Serializable offset = new(((float)random.NextDouble() * MaxPerlinOffset) + 10000, ((float)random.NextDouble() * MaxPerlinOffset) + 10000);

        SaveData saveData = new(characterName, biomeIDs, saveNum.ToString(), new(offset), new(offset + new Vector2Serializable(10000, 10000)));

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            FileStream stream = new(savePath + "/SaveData.pgw", FileMode.Create);
            formatter.Serialize(stream, saveData);
            stream.Close();
            return saveNum.ToString();
        }

        Debug.Log("Error: Persistent save path has not been detected");
        return "";
    }

    private static int GetFreeSaveNum()
    {
        string[] dirs = Directory.GetDirectories(Application.persistentDataPath);

        List<int> nums = new();
        for (int i = 0; i < dirs.Length; i++)
        {
            string fileName = Path.GetFileName(dirs[i]);

            if (int.TryParse(fileName, out int num))
            {
                nums.Add(num);
            }
        }

        //Sort in ascending order
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 1; i < nums.Count; i++)
            {
                if (nums[i - 1] > nums[i])
                {
                    int temp = nums[i - 1];
                    nums[i - 1] = nums[i];
                    nums[i] = temp;
                    changed = true;
                }
            }
        }

        int saveNum = 0;
        for (int i = 0; i < nums.Count; i++)
        {
            if (saveNum < nums[i])
            {
                return saveNum;
            }

            saveNum++;
        }

        return saveNum;
    }

    public static SaveData LoadData(string saveName)
    {
        string path = Application.persistentDataPath + "/" + saveName + "/SaveData.pgw";

        if (File.Exists(path))
        {
            FileStream stream = new(path, FileMode.Open);

            if (stream.Length > 0)
            {
                SaveData saveData = new BinaryFormatter().Deserialize(stream) as SaveData;
                stream.Close();
                return saveData;
            }

            stream.Close();
        }

        Debug.Log("Error: Loading map data");
        return null;
    }
}