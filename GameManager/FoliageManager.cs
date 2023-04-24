using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using UnityEngine;
using Unity.Jobs;

using static Chunk;
using static PerlinData;
using static TerrainColorGradient;

public class FoliageManager : MonoBehaviour
{
    public static FoliageManager Instance;
    public static Transform MyTransform { get; private set; }

    public static Dictionary<int, float[]> FoliageThresholds { private set; get; }

    public static ConcurrentQueue<Vector2Int> FutureFoliagesToClear;

    private static ConcurrentDictionary<Vector2Int, List<Transform>> CurrentFoliages;
    private static ConcurrentDictionary<Vector2Int, List<Transform>> FoliagesToClear;
    private static ConcurrentDictionary<Vector2Int, List<FoliageInfoToMove>> FoliagesToAdd;
    private static ConcurrentDictionary<Vector2Int, Bounds> FoliageToRemove;

    public static bool HasTrees(Vector2Int chunkPos) { return CurrentFoliages.ContainsKey(chunkPos); }

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

            FutureFoliagesToClear = new();
            CurrentFoliages = new();
            FoliagesToClear = new();
            FoliagesToAdd = new();
            FoliageToRemove = new();
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
                new ToDestroyJob(FoliagesToClear[key][0].gameObject).Schedule();
                FoliagesToClear[key].RemoveAt(0);
            }
            else
            {
                while (FoliagesToClear.ContainsKey(key)) FoliagesToClear.TryRemove(key, out _);
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
            FoliageInfoToMove info = FoliagesToAdd[chunkPos][0];
            FoliagesToAdd[chunkPos].RemoveAt(0);
            Transform tree = Instantiate(info.Prefab, info.Position, info.Rotation, MyTransform).transform;
            CurrentFoliages[chunkPos].Add(tree);

            if (FoliagesToAdd[chunkPos].Count == 0)
            {
                while (FoliagesToAdd.ContainsKey(chunkPos)) FoliagesToAdd.TryRemove(chunkPos, out _);
            }
        }
    }

    public static void PopulateAllFoliage()
    {
        while (FoliagesToAdd.Count > 0) Nextoliage();
    }

    public static void ClearFoliage(Vector2Int chunkPos) { FutureFoliagesToClear.Enqueue(chunkPos); }

    private static void AddClearFoliage(Vector2Int chunkPos)
    {
        while (FoliagesToAdd.ContainsKey(chunkPos)) FoliagesToAdd.TryRemove(chunkPos, out _);

        if (CurrentFoliages.ContainsKey(chunkPos) && !FoliagesToClear.ContainsKey(chunkPos))
        {
            while (!FoliagesToClear.ContainsKey(chunkPos)) FoliagesToClear.TryAdd(chunkPos, CurrentFoliages[chunkPos]);
            while (CurrentFoliages.ContainsKey(chunkPos)) CurrentFoliages.TryRemove(chunkPos, out _);
        }
    }

    public static void AddFoliage(Vector2Int chunkPos, List<FoliageInfoToMove> foliages)
    {
        if (!CurrentFoliages.ContainsKey(chunkPos))
        {
            while (!AINodeManager.Obstacles.ContainsKey(chunkPos)) AINodeManager.Obstacles.TryAdd(chunkPos, new());
            while (!CurrentFoliages.ContainsKey(chunkPos)) CurrentFoliages.TryAdd(chunkPos, new());
            if (foliages.Count > 0)
            {
                while (!FoliagesToAdd.ContainsKey(chunkPos)) FoliagesToAdd.TryAdd(chunkPos, foliages);
            }

            if (FoliageToRemove.ContainsKey(chunkPos))
            {
                while (FoliageToRemove.ContainsKey(chunkPos)) FoliageToRemove.TryRemove(chunkPos, out _);

                for (int i = 0; i < foliages.Count; i++)
                {
                    if (FoliageToRemove[chunkPos].Contains(foliages[i].Position))
                    {
                        foliages.RemoveAt(i);
                        i--;
                    }
                }
            }
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

        Bounds bounds = new(new(centre.x, 0, centre.y), new(extents.x, 1000, extents.y));

        for (int i = lowestChunkY; i <= highestChunkY; i++)
        {
            for (int j = lowestChunkX; j <= highestChunkX; j++)
            {
                List<FoliageInfoToMove> infos = new();
                Vector2Int key = new(i, j);
                bool toAdd = false;

                if (FoliagesToAdd.ContainsKey(key))
                {
                    if (FoliagesToAdd[key].Count > 0) infos = FoliagesToAdd[key];
                    while (FoliagesToAdd.ContainsKey(key)) FoliagesToAdd.TryRemove(key, out _);
                    toAdd = true;
                }

                if (infos.Count > 0)
                {
                    for (int k = 0; k < infos.Count; k++)
                    {
                        if (bounds.Contains(infos[k].Position))
                        {
                            infos.RemoveAt(k);
                            k--;
                        }
                    }

                    if (infos.Count > 0)
                    {
                        while (!FoliagesToAdd.ContainsKey(key)) FoliagesToAdd.TryAdd(key, infos);
                    }
                }

                if (CurrentFoliages.ContainsKey(key))
                {
                    for (int k = 0; k < CurrentFoliages[key].Count; k++)
                    {
                        if (bounds.Contains(CurrentFoliages[key][k].position))
                        {
                            new ToDestroyJob(CurrentFoliages[key][k].gameObject).Schedule();
                            CurrentFoliages[key].RemoveAt(k);
                            k--;
                        }
                    }
                }
                else if (!toAdd && GameManager.ActiveTerrain.ContainsKey(key))
                {
                    if (GameManager.ActiveTerrain[key].HasStructures)
                    {
                        while (!FoliagesToAdd.ContainsKey(key)) FoliageToRemove.TryAdd(key, bounds);
                    }
                }
            }
        }
    }

    public static void InitializeThresholds()
    {
        FoliageThresholds = new();
        for (int i = 0; i < BiomeDatas.Length; i++)
        {
            if (BiomeDatas[i]._FoliageSettings.FoliageInfos != null)
            {
                if (BiomeDatas[i]._FoliageSettings.FoliageInfos.Length > 0)
                {
                    FoliageThresholds.Add(i, new float[BiomeDatas[i]._FoliageSettings.FoliageInfos.Length]);

                    FoliageThresholds[i][0] = BiomeDatas[i]._FoliageSettings.FoliageInfos[0].ChanceOfSpawning;

                    for (int j = 1; j < BiomeDatas[i]._FoliageSettings.FoliageInfos.Length; j++)
                    {
                        FoliageThresholds[i][j] = FoliageThresholds[i][j - 1] + BiomeDatas[i]._FoliageSettings.FoliageInfos[j].ChanceOfSpawning;
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        FoliagesToAdd = null;
        FoliagesToClear = null;
        CurrentFoliages = null;
        FutureFoliagesToClear = null;
        FoliageToRemove = null;
    }
}