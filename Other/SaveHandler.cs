using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public class SaveHandler
{
    public static readonly string PersistentDataPath = Application.persistentDataPath;

    public static SaveData SaveMap(string characterName, CharacterSkin charSkin, int mapSize = 25)
    {
        int saveNum = GetFreeSaveNum();
        string savePath = PersistentDataPath + "/" + saveNum;

        SaveData saveData = new(characterName, charSkin, saveNum, mapSize);

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            using FileStream stream = new(savePath + "/SaveData.pgw", FileMode.Create);
            {
                new BinaryFormatter().Serialize(stream, saveData);
            }

            savePath += "/Chunks";
            Directory.CreateDirectory(savePath);
            GameManager.InitializeWorldData(saveData);
            return saveData;
        }

        Debug.Log("Error: Persistent save path has not been detected");
        return null;
    }

    public static ChunkData GetChunkData(Vector2Int position)
    {
        if (GetChunkPath(PersistentDataPath + "/" + GameManager.CurrentSaveData.SaveNum + "/Chunks", position, out string chunkPath))
        {
            FileStream stream = new(chunkPath, FileMode.Open);
            ChunkData chunkData = (ChunkData)new BinaryFormatter().Deserialize(stream);
            stream.Close();
            return chunkData;
        }
        return new();
    }

    public static bool LoadData(string saveName, out SaveData saveData)
    {
        string path = PersistentDataPath + "/" + saveName + "/SaveData.pgw";

        if (File.Exists(path))
        {
            FileStream stream = new(path, FileMode.Open);

            if (stream.Length > 0)
            {
                saveData = new BinaryFormatter().Deserialize(stream) as SaveData;
                stream.Close();
                return true;
            }

            stream.Close();
        }

        Debug.Log("Error: Loading map data");
        saveData = null;
        return false;
    }

    public static bool GetChunkPath(string path, Vector2Int position, out string chunkPath)
    {
        chunkPath = path;

        if (Directory.Exists(path))
        {
            if(File.Exists(path + "/" + position.x + "," + position.y + ".chunk"))
            {
                chunkPath += "/" + position.x + "," + position.y + ".chunk";
                return true;
            }
        }

        return false;
    }

    private static int GetFreeSaveNum()
    {
        string[] dirs = Directory.GetDirectories(PersistentDataPath);

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
                    (nums[i], nums[i - 1]) = (nums[i - 1], nums[i]);
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
}

[Serializable]
public class SaveData
{
    public int SaveNum;
    private const float MaxSeedNum = 100000;
    public int MapSize;

    public readonly Dictionary<Vector2IntSerializable, StructureTypes> Structures;
    public PerlinData MyPerlinData;
    public Vector3Serializable LastPosition;

    public string SaveName;
    public string CharacterName;
    public float TimeInSeconds;
    public CharacterSkin CharSkin;

    public static string SavePath(int saveNum) { return SaveHandler.PersistentDataPath + "/" + saveNum + "/SaveData.pgw"; }

    public string TotalTime
    {
        get
        {
            TimeSpan t = TimeSpan.FromSeconds(TimeInSeconds);
            return t.ToString(@"hh\:mm\:ss");
        }
    }

    public SaveData(string characterName, CharacterSkin charSkin, int saveNum, int mapSize, int seed = -1)
    {
        MapSize = mapSize;
        CharSkin = charSkin;
        LastPosition = new();
        SaveNum = saveNum;
        SaveName = characterName + " " + saveNum;
        CharacterName = characterName;
        TimeInSeconds = 0;
        Structures = new()
        {
            { new(),StructureTypes.Village }
        };

        if (seed == -1) MyPerlinData = new((int)UnityEngine.Random.Range(0, MaxSeedNum) + 10000);
        else MyPerlinData = new(seed + 10000);
    }
}

[Serializable]
public readonly struct PerlinData
{
    public readonly int Seed;
    public readonly int ChunkSize;

    public readonly Vector2Serializable HeightPerlinOffsetOne;
    public readonly Vector2Serializable TemperaturePerlinOffset;

    public readonly Vector2Serializable[] OctaveOffsetsOne;
    public readonly int[] Triangles;

    public const int FoliageSquareCheckSize = 5;
    public const float HeightMultipler = 50;

    private const float BaseOffset = 100000 + 0.1f;
    private const int NumOctaves = 4;
    private const float HeightPerlinScale = 0.05f;
    private const float TemperaturePerlinScale = 0.05f;

    public static AnimationCurve TerrainGradient;
    
    public PerlinData(int seed, int chunkSize = 60)
    {
        Seed = seed;
        HeightPerlinOffsetOne = new(seed + BaseOffset, seed + BaseOffset);
        TemperaturePerlinOffset = new(seed + 10000 + BaseOffset, seed + 10000 + BaseOffset);
        OctaveOffsetsOne = new Vector2Serializable[NumOctaves];
        ChunkSize = chunkSize;

        System.Random prng = new(seed);
        for (int i = 0; i < NumOctaves; i++)
        {
            OctaveOffsetsOne[i].x = prng.Next(100000) + HeightPerlinOffsetOne.x;
            OctaveOffsetsOne[i].y = prng.Next(100000) + HeightPerlinOffsetOne.y;
        }

        Triangles = new int[ChunkSize * ChunkSize * 6];
        int tris = 0, vertexIndex = 0;
        for (int y = 0; y < ChunkSize; y++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                Triangles[tris] = vertexIndex;
                Triangles[tris + 1] = vertexIndex + ChunkSize + 1;
                Triangles[tris + 2] = vertexIndex + 1;
                Triangles[tris + 3] = vertexIndex + 1;
                Triangles[tris + 4] = vertexIndex + ChunkSize + 1;
                Triangles[tris + 5] = vertexIndex + ChunkSize + 2;

                vertexIndex++;
                tris += 6;
            }

            vertexIndex++;
        }
    }

    public static Vector2Int GetVertexPosition(Vector2Int position, Vector2Int worldPosition, int chunkSize)
    {
        return GetVertexPosition(position.x, position.y, worldPosition, chunkSize);
    }

    public static Vector2Int GetVertexPosition(Vector2Int position, Vector2Int worldPosition)
    {
        return GetVertexPosition(position.x, position.y, worldPosition, GameManager.CurrentSaveData.MyPerlinData.ChunkSize);
    }

    public static Vector2Int GetVertexPosition(int x, int y, Vector2Int worldPosition, int chunkSize)
    {
        return new Vector2Int(x + worldPosition.x - (chunkSize / 2), y + worldPosition.y - (chunkSize / 2));
    }

    public readonly struct GenerateChunkData : IJob
    {
        [ReadOnly] private readonly Vector2Int ChunkPosition;
        [ReadOnly] private readonly int ChunkSize;

        public GenerateChunkData(int chunkSize, Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            ChunkSize = chunkSize;
        }

        public void Execute()
        {
            PerlinData perlinData = GameManager.MyPerlinData;
            ChunkData newChunk = new(ChunkPosition, ChunkSize);

            for (int i = 0, y = 0; y <= ChunkSize; y++)
            {
                for (int x = 0; x <= ChunkSize; x++)
                {
                    newChunk.HeightData[i] = perlinData.GetHeightPerlin(GetVertexPosition(new(x, y), ChunkPosition * ChunkSize, ChunkSize));
                    newChunk.TemperatureData[i] = perlinData.GetTemperaturePerlin(GetVertexPosition(new(x, y), ChunkPosition * ChunkSize, ChunkSize));
                    i++;
                }
            }

            new CharacterCreator.WriteChunkData(newChunk).Schedule();
        }
    }

    public float GetHeightPerlin(float x, float y)
    {
        float amplitude = 1;
        float frequency = 1;
        float perlinNoise = 0;

        for (int i = 0; i < OctaveOffsetsOne.Length; i++)
        {
            float sampleXOne = (x + OctaveOffsetsOne[i].x) / ChunkSize * HeightPerlinScale * frequency;
            float sampleYOne = (y + OctaveOffsetsOne[i].y) / ChunkSize * HeightPerlinScale * frequency;
            perlinNoise += (Mathf.PerlinNoise(sampleXOne, sampleYOne) * 2 - 1) * amplitude;

            amplitude *= 0.5f;
            frequency *= 2;
        }

        lock (TerrainGradient) return TerrainGradient.Evaluate(Mathf.Abs(perlinNoise));
    }
    public float GetHeightPerlin(Vector2 pos) { return GetHeightPerlin(pos.x, pos.y); }
    
    public float GetTemperaturePerlin(Vector2 position)
    {
        return Mathf.Abs(Mathf.PerlinNoise((position.x + TemperaturePerlinOffset.x) / ChunkSize * TemperaturePerlinScale, (position.y + TemperaturePerlinOffset.y) / ChunkSize * TemperaturePerlinScale));
    }
    public float GetTemperaturePerlin(float x, float y)
    {
        return Mathf.Abs(Mathf.PerlinNoise((x + TemperaturePerlinOffset.x) / ChunkSize * TemperaturePerlinScale, (y + TemperaturePerlinOffset.y) / ChunkSize * TemperaturePerlinScale));
    }

    public Vector3 GetPerlinPosition(Vector2 position)
    {
        return new(position.x, (GetHeightPerlin(position.x, position.y) * HeightMultipler), position.y);
    }
    public Vector3 GetPerlinPosition(float x, float y)
    {
        return new(x, (GetHeightPerlin(x, y) * HeightMultipler), y);
    }
}

[Serializable]
public readonly struct ChunkData
{
    public readonly Vector2IntSerializable ChunkPosition;
    public readonly float[] HeightData;
    public readonly float[] TemperatureData;

    public ChunkData(Vector2IntSerializable chunkPos, int chunkSize)
    {
        ChunkPosition = chunkPos;
        HeightData = new float[(chunkSize + 1) * (chunkSize + 1)];
        TemperatureData = new float[(chunkSize + 1) * (chunkSize + 1)];
    }
}
