using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using System.Collections.Concurrent;
using System.Linq;

public class FoliageManager : MonoBehaviour
{
    private struct MovedFoliageInfo
    {
        public Transform FoliageTransform;
        public int BiomeNum;
        public int FoliageNum;

        public MovedFoliageInfo(FoliageInfoToMove info)
        {
            BiomeNum = info.BiomeNum;
            FoliageNum = info.FoliageNum;

            FoliageTransform = FoliageStorage[BiomeNum][FoliageNum].Dequeue();

            FoliageTransform.SetPositionAndRotation(info.Position, info.Rotation);
            FoliageTransform.localScale = info.Scale;
            FoliageTransform.gameObject.SetActive(true);
        }

        public void ReturnToStorage()
        {
            FoliageTransform.gameObject.SetActive(false);
            FoliageStorage[BiomeNum][FoliageNum].Enqueue(FoliageTransform);
        }
    }

    public static FoliageManager Instance;
    public static Transform MyTransform { get; private set; }

    public static Dictionary<int, float[]> FoliageThresholds { private set; get; }
    public static Queue<Transform>[][] FoliageStorage { get; private set; }

    private static Dictionary<Vector2Int, List<MovedFoliageInfo>> CurrentFoliage;
    private static Dictionary<Vector2Int, List<MovedFoliageInfo>> FoliagesToClear;
    public static Dictionary<Vector2Int, List<FoliageInfoToMove>> FoliagesToAdd { get; private set; }
    private static Dictionary<Vector2Int, Bounds> FoliageToRemove;

    [SerializeField] private bool PreInitialized = false;
    public static bool HasTrees(Vector2Int chunkPos) { return CurrentFoliage.ContainsKey(chunkPos); }

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple foliage managers detected");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            MyTransform = transform;

            if (!PreInitialized)
            {
                int foliagePerBiome = (((Chunk.DefaultChunkSize + 1) * (Chunk.DefaultChunkSize + 1)) / Chunk.FoliageSquareCheckSize) * ((2 * World.LODTwoDistance) * (2 * World.LODTwoDistance));

                foreach (int biomeNum in FoliageThresholds.Keys)
                {
                    for (int foliageNum = FoliageThresholds[biomeNum].Length - 1; foliageNum > -1; foliageNum--)
                    {
                        FoliageInfo foliageInfo = World.Biomes[biomeNum]._FoliageSettings.FoliageInfos[foliageNum];

                        for (int j = Mathf.FloorToInt(FoliageThresholds[biomeNum][foliageNum] * foliagePerBiome) - 1; j > -1; j--)
                        {
                            GameObject treeTransform = Instantiate(foliageInfo.Prefab, Vector3.zero, Quaternion.Euler(0, Random.Range(-180, 180), 0), MyTransform);
                            treeTransform.SetActive(false);
                            treeTransform.layer = LayerMask.NameToLayer("Tree");

                            FoliageStorage[biomeNum][foliageNum].Enqueue(treeTransform.transform);
                        }
                    }
                }
            }
        }

        enabled = false;
    }

    private void Update()
    {
        if(FoliagesToClear.Count > 0)
        {
            Vector2Int key = FoliagesToClear.First().Key;
            if (FoliagesToClear[key].Count > 0)
            {
                FoliagesToClear[key][0].ReturnToStorage();
                FoliagesToClear[key].RemoveAt(0);
            }
            else
            {
                FoliagesToClear.Remove(key);
            }
        }

        if (FoliagesToAdd.Count > 0)
        {
            Nextoliage();
        }
    }

    public static void Nextoliage()
    {
        Vector2Int chunkPos = FoliagesToAdd.First().Key;
        if (FoliagesToAdd[chunkPos].Count > 0)
        {
            CurrentFoliage[chunkPos].Add(new(FoliagesToAdd[chunkPos][0]));
            FoliagesToAdd[chunkPos].RemoveAt(0);
        }
        else
        {
            World.ActiveTerrain[chunkPos].HasTrees = true;
            NavMeshManager.AddChunk(chunkPos);
            FoliagesToAdd.Remove(chunkPos);
        }
    }

    public static void ClearFoliage(Vector2Int chunkPos)
    {
        if(CurrentFoliage.ContainsKey(chunkPos) && !FoliagesToClear.ContainsKey(chunkPos))
        {
            if(FoliagesToAdd.ContainsKey(chunkPos))
            {
                FoliagesToAdd.Remove(chunkPos);
            }

            World.ActiveTerrain[chunkPos].HasTrees = false;
            NavMeshManager.RemoveChunk(chunkPos);

            FoliagesToClear.Add(chunkPos, CurrentFoliage[chunkPos]);
            CurrentFoliage.Remove(chunkPos);
        }
    }

    public static void AddFoliage(Vector2Int chunkPos, List<FoliageInfoToMove> foliages)
    {
        CurrentFoliage.Add(chunkPos, new());
        FoliagesToAdd.Add(chunkPos, foliages);
    }

    public static void AddTreesToRemove(Vector2 lowest, Vector2 highest)
    {
        highest.y += 5;
        lowest.y -= 5;
        highest.x += 5;
        lowest.x -= 5;

        int highestChunkY = Mathf.CeilToInt(highest.y / 240);
        int lowestChunkY = Mathf.FloorToInt(lowest.y / 240);
        int highestChunkX = Mathf.CeilToInt(highest.x / 240);
        int lowestChunkX = Mathf.FloorToInt(lowest.x / 240);

        Vector2 centre = new((lowest.x + highest.x) / 2, (lowest.y + highest.y) / 2);
        Vector2 extents = new(Mathf.Abs(lowest.x - highest.x), Mathf.Abs(lowest.y - highest.y));

        lock (FoliageToRemove)
        {
            for (int i = lowestChunkY; i <= highestChunkY; i++)
            {
                for (int j = lowestChunkX; j <= highestChunkX; j++)
                {
                    if (!FoliageToRemove.ContainsKey(new(i, j)))
                    {
                        FoliageToRemove.Add(new(i, j), new(new(centre.x, 0, centre.y), new(extents.x, 1000, extents.y)));
                    }
                }
            }
        }
    }

    public static void Initialize()
    {
        FoliageThresholds = new();
        FoliagesToAdd = new();
        FoliageToRemove = new();
        FoliagesToClear = new();
        CurrentFoliage = new();

        FoliageStorage = new Queue<Transform>[World.Biomes.Length][];

        for (int i = 0; i < World.Biomes.Length; i++)
        {
            FoliageStorage[i] = new Queue<Transform>[World.Biomes[i]._FoliageSettings.FoliageInfos.Length];

            for (int j = 0; j < World.Biomes[i]._FoliageSettings.FoliageInfos.Length; j++)
            {
                FoliageStorage[i][j] = new();
            }
        }

        for (int i = 0; i < World.Biomes.Length; i++)
        {
            if (World.Biomes[i]._FoliageSettings.FoliageInfos != null)
            {
                if (World.Biomes[i]._FoliageSettings.FoliageInfos.Length > 0)
                {
                    FoliageThresholds.Add(i, new float[World.Biomes[i]._FoliageSettings.FoliageInfos.Length]);

                    FoliageThresholds[i][0] = World.Biomes[i]._FoliageSettings.FoliageInfos[0].ChanceOfSpawning;

                    for (int j = 1; j < World.Biomes[i]._FoliageSettings.FoliageInfos.Length; j++)
                    {
                        FoliageThresholds[i][j] = FoliageThresholds[i][j - 1] + World.Biomes[i]._FoliageSettings.FoliageInfos[j].ChanceOfSpawning;
                    }
                }
            }
        }
    }
}