using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.AI;
using System.Collections.Concurrent;
using Unity.Jobs;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance;

    private static HashSet<Vector2Int> AwaitingFoliage;
    private static ConcurrentQueue<Vector2Int> ChunksToAdd;
    private static Dictionary<Vector2Int, NavMeshBuildSource> SourcesDict;
    private static List<NavMeshBuildSource> ActiveSources;
    private static AsyncOperation NavMeshProgress;
    private static JobHandle SourcesProgress;

    private static Dictionary<Vector2Int, List<MonoBehaviour>> UnreadyToEnable;
    private static ConcurrentQueue<List<MonoBehaviour>> ToEnable;

    [Header("Surface")]
    private static NavMeshData _NavMeshData;
    private static NavMeshDataInstance DataInstance;
    private static NavMeshBuildSettings BuildSettings;
    private static Bounds _Bounds;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple nav mesh managers detected");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            ActiveSources = new();
            SourcesDict = new();
            UnreadyToEnable = new();
            ToEnable = new();
            ChunksToAdd = new();
            AwaitingFoliage = new();

            _Bounds = new(new(0, (SaveData.HeightMultipler / 2), 0), new(((World.LODTwoDistance * 2) + 1) * Chunk.DefaultChunkSize, SaveData.HeightMultipler + 50, ((World.LODTwoDistance * 2) + 1) * Chunk.DefaultChunkSize));

            BuildSettings = NavMesh.GetSettingsByID(0);
            BuildSettings.overrideTileSize = true;
            BuildSettings.overrideVoxelSize = true;
            BuildSettings.tileSize = 256;
            BuildSettings.voxelSize = 0.15f;

            _NavMeshData = new(0);
            _NavMeshData.name = Instance.gameObject.name;
            DataInstance = NavMesh.AddNavMeshData(_NavMeshData);
            DataInstance.owner = Instance;
        }

        enabled = false;
    }

    private void Update()
    {
        if (SourcesProgress.IsCompleted)
        {
            if (NavMeshProgress.isDone)
            {
                if (ToEnable.Count > 0)
                {
                    if (ToEnable.TryDequeue(out List<MonoBehaviour> monos))
                    {
                        foreach (MonoBehaviour mono in monos)
                        {
                            mono.enabled = true;
                        }
                    }
                }

                if (ChunksToAdd.Count == 0)
                {
                    enabled = false;
                }
                else if (ChunksToAdd.TryDequeue(out Vector2Int chunkPos))
                {
                    Chunk chunk = World.ActiveTerrain[chunkPos];
                    ActiveSources.Add(new()
                    {
                        transform = chunk.MyTransform.localToWorldMatrix,
                        size = Vector3.zero,
                        shape = NavMeshBuildSourceShape.Mesh,
                        area = 0,
                        sourceObject = chunk.TerrainMesh
                    });
                    SourcesDict[chunk.ChunkPosition] = ActiveSources[^1];
                    NavMeshProgress = NavMeshBuilder.UpdateNavMeshDataAsync(_NavMeshData, BuildSettings, ActiveSources, _Bounds);
                }
            }
        }
    }

    public static void Initialize()
    {
        NavMeshProgress = NavMeshBuilder.UpdateNavMeshDataAsync(_NavMeshData, BuildSettings, ActiveSources, _Bounds);
        SourcesProgress = new CheckNavMesh().Schedule();
        Instance.enabled = true;
    }

    public static void RemoveChunk(Vector2Int chunkPos)
    {
        if (SourcesDict.ContainsKey(chunkPos))
        {
            ActiveSources.Remove(SourcesDict[chunkPos]);
            SourcesDict.Remove(chunkPos);
        }
    }

    public static void AddChunk(Vector2Int chunkPos)
    {
        if (AwaitingFoliage.Contains(chunkPos))
        {
            AwaitingFoliage.Remove(chunkPos);

            if (World.ActiveTerrain[chunkPos].TerrainMesh && (int)World.ActiveTerrain[chunkPos].CurrentLODState <= (int)TerrainLODStates.LODTwo)
            {
                ChunksToAdd.Enqueue(chunkPos);
            }
        }
    }

    public static void UpdateNavMesh()
    {
        SourcesProgress = new CheckNavMesh().Schedule();
        Instance.enabled = true;
    }

    public struct CheckNavMesh : IJob
    {
        public void Execute()
        {
            for (int x = -World.LODTwoDistance; x <= World.LODTwoDistance; x++)
            {
                for (int z = -World.LODTwoDistance; z <= World.LODTwoDistance; z++)
                {
                    Vector2Int chunkToCheck = new(x + World.LastPlayerChunkPos.x, z + World.LastPlayerChunkPos.y);

                    if (World.ActiveTerrain.ContainsKey(chunkToCheck) && !SourcesDict.ContainsKey(chunkToCheck))
                    {
                        Chunk chunk = World.ActiveTerrain[chunkToCheck];
                        if (chunk.TerrainMesh && (int)chunk.CurrentLODState <= (int)TerrainLODStates.LODTwo)
                        {
                            SourcesDict.Add(chunk.ChunkPosition, new());

                            if (chunk.HasTrees)
                            {
                                ChunksToAdd.Enqueue(chunkToCheck);
                                _Bounds.center = new(World.LastPlayerChunkPos.x * Chunk.DefaultChunkSize, (SaveData.HeightMultipler / 2), World.LastPlayerChunkPos.y * Chunk.DefaultChunkSize);

                                if (UnreadyToEnable.ContainsKey(chunkToCheck))
                                {
                                    ToEnable.Enqueue(UnreadyToEnable[chunkToCheck]);
                                    UnreadyToEnable.Remove(chunkToCheck);
                                }
                            }
                            else
                            {
                                AwaitingFoliage.Add(chunk.ChunkPosition);
                            }
                        }
                    }
                }
            }

            List<Vector2Int> keys = new(SourcesDict.Keys);
            foreach (Vector2Int key in keys)
            {
                Chunk chunk = World.ActiveTerrain[key];
                if (Mathf.Abs(World.LastPlayerChunkPos.x - chunk.ChunkPosition.x) > World.LODTwoDistance || Mathf.Abs(World.LastPlayerChunkPos.y - chunk.ChunkPosition.y) > World.LODTwoDistance)
                {
                    RemoveChunk(chunk.ChunkPosition);
                    _Bounds.center = new(World.LastPlayerChunkPos.x * Chunk.DefaultChunkSize, (SaveData.HeightMultipler / 2), World.LastPlayerChunkPos.y * Chunk.DefaultChunkSize);
                }
            }
        }
    }
      
    public static void AddUnreadyToEnable(Vector2Int chunkPosition, MonoBehaviour behaviour)
    {
        if (SourcesDict.ContainsKey(chunkPosition))
        {
            behaviour.enabled = true;
        }
        else if (UnreadyToEnable.ContainsKey(chunkPosition))
        {
            UnreadyToEnable[chunkPosition].Add(behaviour);
        }
        else
        {
            UnreadyToEnable.Add(chunkPosition, new() { behaviour });
        }
    }
}