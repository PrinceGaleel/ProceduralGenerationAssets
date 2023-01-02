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
    public Transform InactiveParent;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple foliage managers detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;

            InactiveParent = new GameObject("InactiveParent").transform;
            InactiveParent.gameObject.SetActive(false);
            InactiveParent.parent = transform;

            for (int i = 0; i < World.Biomes.Length; i++)
            {
                Transform newBiomeParent = new GameObject().transform;
                newBiomeParent.SetParent(InactiveParent);

                for (int j = 0; j < World.Biomes[i]._FoliageSettings.FoliageInfos.Length; j++)
                {
                    Transform newFoliageParent = new GameObject().transform;
                    newFoliageParent.SetParent(newBiomeParent);
                }
            }

            foreach (int biomeNum in FoliageThresholds.Keys)
            {
                for (int i = FoliageThresholds[biomeNum].Length - 1; i > -1; i--)
                {
                    for (int j = Mathf.FloorToInt(FoliageThresholds[biomeNum][i] * FoliagePerBiome) - 1; j > -1; j--)
                    {
                        FoliageInfo foliageInfo = World.Biomes[biomeNum]._FoliageSettings.FoliageInfos[i];
                        Transform treeTransform = Instantiate(foliageInfo.Prefab, Vector3.zero, Quaternion.Euler(0, Random.Range(-180, 180), 0)).transform;
                        float randomScale = Random.Range(foliageInfo.MinFoliageScale, foliageInfo.MinFoliageScale + foliageInfo.MaxExtensionHeight);
                        treeTransform.localScale = new(randomScale, randomScale, randomScale);
                        treeTransform.SetParent(InactiveParent.GetChild(biomeNum).GetChild(i));
                        treeTransform.gameObject.layer = LayerMask.NameToLayer("Tree");
                    }
                }
            }
        }
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
                while (FoliageToAdd.Count > 0)
                {
                    Chunk chunk = FoliageToAdd.Dequeue();
                    Transform foliageParent;

                    for (int i = 0; i < chunk.Foliages.Count; i++)
                    {
                        foliageParent = InactiveParent.GetChild(chunk.Foliages[i].BiomeNum).GetChild(chunk.Foliages[i].FoliageNum);
                        if (foliageParent.childCount > 0)
                        {
                            Chunk.FoliageInfoToMove foliage = chunk.Foliages[i];
                            foliage.Foliage = foliageParent.GetChild(0);
                            foliage.Foliage.position = foliage.Position;
                            foliage.Foliage.parent = foliage.ParentChunk.FoliageParent.GetChild(foliage.BiomeNum).GetChild(foliage.FoliageNum);
                        }
                    }

                    chunk.TreeReady = true;
                }
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

    private void StoreFoliage(Chunk.FoliageInfoToMove foliage)
    {
        foliage.Foliage.parent = InactiveParent.GetChild(foliage.BiomeNum).GetChild(foliage.FoliageNum);
        foliage.Foliage.position = new(0, 0, 0);
        foliage.Foliage = null;
    }

    private void RemoveTrees(Chunk chunk, Bounds bounds)
    {
        for (int i = chunk.Foliages.Count - 1; i > -1; i--)
        {
            if (bounds.Contains(chunk.Foliages[i].Position))
            {
                if (chunk.Foliages[i].Foliage)
                {
                    chunk.Foliages[i].Foliage.parent = InactiveParent.GetChild(chunk.Foliages[i].BiomeNum).GetChild(chunk.Foliages[i].FoliageNum);
                }

                chunk.Foliages.RemoveAt(i);
            }
        }
    }

    public static void InitializeStatics()
    {
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