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
    private const float MaxSeedNum = 100000;

    [Header("Base Perlin Settings")]
    public int Seed;

    [Header("World Data")]
    public Vector3Serializable LastPosition;

    [Header("Save Info")]
    public string SaveName;
    public string CharacterName;
    public float TimeInSeconds;
    public CharacterSkin CharSkin;

    public string TotalTime
    {
        get
        {
            TimeSpan t = TimeSpan.FromSeconds(TimeInSeconds);
            return t.ToString(@"hh\:mm\:ss");
        }
    }

    public SaveData(string characterName, CharacterSkin charSkin, string saveName, int seed = -1)
    {
        CharSkin = charSkin;
        LastPosition = new();
        SaveName = saveName;
        CharacterName = characterName;
        TimeInSeconds = 0;
        LastPosition = new();

        if (seed == -1)
        {
            Seed = (int)UnityEngine.Random.Range(0, MaxSeedNum);
        }
        else
        {
            Seed = seed;
        }

        Seed += 10000;
    }

    public static string SaveMap(string characterName, CharacterSkin charSkin)
    {
        BinaryFormatter formatter = new();

        int saveNum = GetFreeSaveNum();
        string savePath = Application.persistentDataPath + "/" + saveNum;

        SaveData saveData = new(characterName, charSkin, saveNum.ToString());

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