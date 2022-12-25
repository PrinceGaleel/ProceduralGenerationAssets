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
    public static Dictionary<Vector2Int, Bounds> TreesToRemove;
    public static Dictionary<Vector2Int, Bounds> FutureTreesToRemove;
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
                            continue;
                        }
                    }

                    FutureTreesToRemove.Add(keys[i], TreesToRemove[keys[i]]);                    
                }
                TreesToRemove = new();
            }
        }
        else if (FoliageToAdd.Count > 0 && (FoliageToAdd.Count <= FoliageToRemove.Count || FoliageToRemove.Count == 0))
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

                    chunk.HasTrees = true;
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

                    chunk.HasTrees = false;
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
                StoreFoliage(chunk.Foliages[i]);
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
        FutureTreesToRemove = new();

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