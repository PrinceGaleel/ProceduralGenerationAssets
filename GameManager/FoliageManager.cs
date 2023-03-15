using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using System.Collections.Concurrent;
using System.Linq;
using UnityEditor;

public class FoliageManager : MonoBehaviour
{
    public static FoliageManager Instance;
    public static Transform MyTransform { get; private set; }

    public static Dictionary<int, float[]> FoliageThresholds { private set; get; }

    public static ConcurrentQueue<Vector2Int> FutureFoliagesToClear;

    private static Dictionary<Vector2Int, Queue<Transform>> CurrentFoliage;
    private static Dictionary<Vector2Int, Queue<Transform>> FoliagesToClear;
    private static Dictionary<Vector2Int, Queue<FoliageInfoToMove>> FoliagesToAdd;
    private static Dictionary<Vector2Int, Bounds> FoliageToRemove;

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
        }

        enabled = false;
    }

    private void Update()
    {
        if (FutureFoliagesToClear.Count > 0)
        {
            if (FutureFoliagesToClear.TryDequeue(out Vector2Int chunkPos))
            {
                AddClearFoliage(chunkPos);
            }
        }

        if (FoliagesToClear.Count > 0)
        {
            Vector2Int key = FoliagesToClear.First().Key;
            if (FoliagesToClear[key].Count > 0)
            {
                new ToDestroyJob(FoliagesToClear[key].Dequeue().gameObject).Schedule();
            }
            else
            {
                FoliagesToClear.Remove(key);
            }
        }

        if(FoliageToRemove.Count > 0)
        {

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
            FoliageInfoToMove info = FoliagesToAdd[chunkPos].Dequeue();
            CurrentFoliage[chunkPos].Enqueue(Instantiate(info.Prefab, info.Position, info.Rotation, MyTransform).transform);
        }
        else
        {
            new AINodeManager.AddNodesChunkJob(chunkPos * Chunk.DefaultChunkSize).Schedule();
            NavMeshManager.CheckAwaiting(chunkPos);
            FoliagesToAdd.Remove(chunkPos);
        }
    }

    public static void PopulateAllFoliage()
    {
        while (FoliagesToAdd.Count > 0)
        {
            Nextoliage();
        }
    }

    public static void ClearFoliage(Vector2Int chunkPos)
    {
        FutureFoliagesToClear.Enqueue(chunkPos);
    }

    private static void AddClearFoliage(Vector2Int chunkPos)
    {
        if (CurrentFoliage.ContainsKey(chunkPos) && !FoliagesToClear.ContainsKey(chunkPos))
        {
            if (FoliagesToAdd.ContainsKey(chunkPos))
            {
                FoliagesToAdd.Remove(chunkPos);
            }

            FoliagesToClear.Add(chunkPos, CurrentFoliage[chunkPos]);
            CurrentFoliage.Remove(chunkPos);
        }
    }

    public static void AddFoliage(Vector2Int chunkPos, Queue<FoliageInfoToMove> foliages)
    {
        lock (CurrentFoliage) lock(FoliagesToAdd)
        {
            CurrentFoliage.Add(chunkPos, new());
            FoliagesToAdd.Add(chunkPos, foliages);
        }
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

    public static void InitializeStatics()
    {
        FoliageThresholds = new();
        FoliagesToAdd = new();
        FoliageToRemove = new();
        FoliagesToClear = new();
        CurrentFoliage = new();
        FutureFoliagesToClear = new();

        for (int i = 0; i < TerrainGradient.BiomeDatas.Length; i++)
        {
            if (TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos != null)
            {
                if (TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos.Length > 0)
                {
                    FoliageThresholds.Add(i, new float[TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos.Length]);

                    FoliageThresholds[i][0] = TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos[0].ChanceOfSpawning;

                    for (int j = 1; j < TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos.Length; j++)
                    {
                        FoliageThresholds[i][j] = FoliageThresholds[i][j - 1] + TerrainGradient.BiomeDatas[i]._FoliageSettings.FoliageInfos[j].ChanceOfSpawning;
                    }
                }
            }
        }
    }
}