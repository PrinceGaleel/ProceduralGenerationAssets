using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageManager : MonoBehaviour
{
    public static FoliageManager Instance;
    private const int FoliagePerBiome = 20000;

    public static Dictionary<int, float[]> FoliageThresholds { private set; get; }
    public static Queue<Chunk> FoliageToAdd;
    public static Queue<Chunk> FoliageToRemove;
    private static Dictionary<Vector2Int, Bounds> TreesToRemove;
    private static Transform InactiveParent;
    private static Queue<MeshRenderer>[][] FoliageStorage;

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
            InactiveParent = new GameObject().transform;
            InactiveParent.transform.position = new(0, 0, 0);
            InactiveParent.gameObject.SetActive(false);

            foreach (int biomeNum in FoliageThresholds.Keys)
            {
                for (int foliageNum = FoliageThresholds[biomeNum].Length - 1; foliageNum > -1; foliageNum--)
                {
                    FoliageInfo foliageInfo = World.Biomes[biomeNum]._FoliageSettings.FoliageInfos[foliageNum];

                    for (int j = Mathf.FloorToInt(FoliageThresholds[biomeNum][foliageNum] * FoliagePerBiome) - 1; j > -1; j--)
                    {
                        Transform treeTransform = Instantiate(foliageInfo.Prefab, Vector3.zero, Quaternion.Euler(0, Random.Range(-180, 180), 0), InactiveParent).transform;
                        float randomScale = Random.Range(foliageInfo.MinFoliageScale, foliageInfo.MinFoliageScale + foliageInfo.MaxExtensionHeight);
                        treeTransform.localScale = new(randomScale, randomScale, randomScale);
                        treeTransform.gameObject.layer = LayerMask.NameToLayer("Tree");

                        FoliageStorage[biomeNum][foliageNum].Enqueue(treeTransform.GetComponent<MeshRenderer>());
                    }
                }
            }
        }

        enabled = false;
    }

    private void Update()
    {
        if (TreesToRemove.Count > 0)
        {
            lock (TreesToRemove)
            {
                List<Vector2Int> keys = new(TreesToRemove.Keys);
                for (int i = keys.Count - 1; i > -1; i--)
                {
                    if (World.ActiveTerrain.ContainsKey(keys[i]))
                    {
                        if (World.ActiveTerrain[keys[i]].Foliages.Count > 0)
                        {
                            RemoveTrees(World.ActiveTerrain[keys[i]], TreesToRemove[keys[i]]);
                            TreesToRemove.Remove(keys[i]);
                        }
                    }               
                }
            }
        }
        
        if (FoliageToAdd.Count > 0 && (FoliageToAdd.Count <= FoliageToRemove.Count || FoliageToRemove.Count == 0))
        {
            lock (FoliageToAdd)
            {
                PopulateChunks();
            }
        }
        else if (FoliageToRemove.Count > 0)
        {
            lock (FoliageToRemove)
            {
                while (FoliageToRemove.Count > 0)
                {
                    Chunk chunk = FoliageToRemove.Dequeue();
                    for (int j = chunk.Foliages.Count - 1; j > -1; j--)
                    {
                        StoreFoliage(chunk.Foliages[j]);
                    }

                    chunk.TreeReady = false;
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
        int highestChunkX = Mathf.CeilToInt(highest.x/ 240);
        int lowestChunkX = Mathf.FloorToInt(lowest.x / 240);

        Vector2 centre = new((lowest.x + highest.x) / 2, (lowest.y + highest.y) / 2);
        Vector2 extents = new(Mathf.Abs(lowest.x - highest.x), Mathf.Abs(lowest.y - highest.y));

        lock (TreesToRemove)
        {
            for (int i = lowestChunkY; i <= highestChunkY; i++)
            {
                for (int j = lowestChunkX; j <= highestChunkX; j++)
                {
                    if (!TreesToRemove.ContainsKey(new(i, j)))
                    {
                        TreesToRemove.Add(new(i, j), new(new(centre.x, 0, centre.y), new(extents.x, 1000, extents.y)));
                    }
                }
            }
        }
    }
    
    public static void PopulateChunks()
    {
        while (FoliageToAdd.Count > 0)
        {
            Chunk chunk = FoliageToAdd.Dequeue();

            for (int i = 0; i < chunk.Foliages.Count; i++)
            {
                if (FoliageStorage[chunk.Foliages[i].BiomeNum][chunk.Foliages[i].FoliageNum].Count > 0)
                {
                    Chunk.FoliageInfoToMove foliage = chunk.Foliages[i];
                    foliage.Foliage = FoliageStorage[chunk.Foliages[i].BiomeNum][chunk.Foliages[i].FoliageNum].Dequeue();
                    foliage.Foliage.transform.position = foliage.Position;
                    foliage.Foliage.transform.parent = foliage.ParentChunk.FoliageParent.GetChild(foliage.BiomeNum).GetChild(foliage.FoliageNum);
                }
            }

            chunk.TreeReady = true;
        }
    }

    private void StoreFoliage(Chunk.FoliageInfoToMove foliage)
    {
        lock (FoliageStorage[foliage.BiomeNum][foliage.FoliageNum])
        {
            FoliageStorage[foliage.BiomeNum][foliage.FoliageNum].Enqueue(foliage.Foliage);
            foliage.Foliage.transform.parent = InactiveParent;
            foliage.Foliage.transform.position = new(0, 0, 0);
            foliage.Foliage = null;
        }
    }

    private void RemoveTrees(Chunk chunk, Bounds bounds)
    {
        lock (chunk.Foliages)
        {
            for (int i = chunk.Foliages.Count - 1; i > -1; i--)
            {
                if (bounds.Contains(chunk.Foliages[i].Position))
                {
                    if (chunk.Foliages[i].Foliage)
                    {
                        lock (FoliageStorage[chunk.Foliages[i].BiomeNum][chunk.Foliages[i].FoliageNum])
                        {
                            FoliageStorage[chunk.Foliages[i].BiomeNum][chunk.Foliages[i].FoliageNum].Enqueue(chunk.Foliages[i].Foliage);
                            chunk.Foliages[i].Foliage.transform.parent = InactiveParent;
                        }
                    }

                    chunk.Foliages.RemoveAt(i);
                }
            }
        }
    }

    public static void InitializeStatics()
    {
        FoliageStorage = new Queue<MeshRenderer>[World.Biomes.Length][];

        for (int i = 0; i < World.Biomes.Length; i++)
        {
            FoliageStorage[i] = new Queue<MeshRenderer>[World.Biomes[i]._FoliageSettings.FoliageInfos.Length];

            for (int j = 0; j < World.Biomes[i]._FoliageSettings.FoliageInfos.Length; j++)
            {
                FoliageStorage[i][j] = new();
            }
        }

        FoliageThresholds = new();
        FoliageToAdd = new();
        FoliageToRemove = new();
        TreesToRemove = new();

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