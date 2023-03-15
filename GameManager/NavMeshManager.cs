using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Concurrent;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance;
    private static Object MyObject;

    private static HashSet<Vector2Int> AwaitingFoliage;
    private static ConcurrentQueue<Vector2Int> ChunksToAdd;
    private static Dictionary<Vector2Int, int> MeshInterests;
    private static Dictionary<Vector2Int, NavMeshBuildSource> SourcesDict;
    private static List<NavMeshBuildSource> ActiveSources;
    private static AsyncOperation NavMeshProgress;

    private static Dictionary<Vector2Int, List<MonoBehaviour>> UnreadyToEnable;
    private static ConcurrentQueue<List<MonoBehaviour>> ToEnable;

    private static NavMeshDataInstance MeshInstance;
    private static NavMeshData MeshData;
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

            BuildSettings = NavMesh.GetSettingsByID(0);
            BuildSettings.overrideTileSize = true;
            BuildSettings.overrideVoxelSize = true;
            BuildSettings.tileSize = 256;
            BuildSettings.voxelSize = 0.15f;
            BuildSettings.maxJobWorkers = 100;

            MeshData = new(0);
            MeshData = NavMeshBuilder.BuildNavMeshData(BuildSettings, new(), new(), transform.position, transform.rotation);
            MeshInstance = NavMesh.AddNavMeshData(MeshData);

            ActiveSources = new();
            SourcesDict = new();
            MeshInterests = new();
            UnreadyToEnable = new();
            ToEnable = new();
            ChunksToAdd = new();
            AwaitingFoliage = new();
            _Bounds = new();
            MyObject = this;
        }

        enabled = false;
    }

    private void Start()
    {
        NavMeshProgress = NavMeshBuilder.UpdateNavMeshDataAsync(MeshData, BuildSettings, ActiveSources, _Bounds);
    }

    private void Update()
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
            else if (ChunksToAdd.Count == 0)
            {
                enabled = false;
            }
            else if (ChunksToAdd.TryDequeue(out Vector2Int chunkPos))
            {
                Chunk chunk = GameManager.ActiveTerrain[chunkPos];
                ActiveSources.Add(new()
                {
                    transform = chunk.MyTransform.localToWorldMatrix,
                    size = Vector3.zero,
                    shape = NavMeshBuildSourceShape.Mesh,
                    area = 0,
                    sourceObject = chunk.GetChunkMeshFilter.sharedMesh
                });

                SourcesDict[chunkPos] = ActiveSources[^1];

                if (UnreadyToEnable.ContainsKey(chunkPos))
                {
                    ToEnable.Enqueue(UnreadyToEnable[chunkPos]);
                    UnreadyToEnable.Remove(chunkPos);
                }

                NavMeshProgress = NavMeshBuilder.UpdateNavMeshDataAsync(MeshData, BuildSettings, ActiveSources, _Bounds);
            }
        }
    }

    private static void UpdateBounds()
    {
        if (SourcesDict.Count > 0)
        {
            List<Vector2Int> chunkPositions = new(SourcesDict.Keys);
            Bounds newBounds = new(new(chunkPositions[0].x * Chunk.DefaultChunkSize, Chunk.HeightMultipler / 2, chunkPositions[0].y * Chunk.DefaultChunkSize),
                new(Chunk.DefaultChunkSize + 10, Chunk.HeightMultipler * 1.5f, Chunk.DefaultChunkSize + 10));

            for (int i = 1; i < chunkPositions.Count; i++)
            {
                newBounds.Encapsulate(new Bounds(
                    new(chunkPositions[i].x * Chunk.DefaultChunkSize, Chunk.HeightMultipler / 2, chunkPositions[i].y * Chunk.DefaultChunkSize),
                    new(Chunk.DefaultChunkSize + 10, Chunk.HeightMultipler * 1.5f, Chunk.DefaultChunkSize + 10)));
            }

            _Bounds = newBounds;
        }
        else
        {
            _Bounds = new(Vector3.zero, Vector3.zero);
        }
    }

    public static void RemovePOI(Vector2Int chunkPos, int range = 1)
    {
        for (int z = -range; z <= range; z++)
        {
            for (int x = -range; x <= range; x++)
            {
                Vector2Int chunkToCheck = new(x + chunkPos.x, z + chunkPos.y);
                if (SourcesDict.ContainsKey(chunkToCheck))
                {
                    RemoveChunk(chunkToCheck);
                }
            }
        }

        UpdateBounds();
        Instance.enabled = true;
    }

    public static void RemoveChunk(Vector2Int chunkPos)
    {
        if (MeshInterests.ContainsKey(chunkPos))
        {
            MeshInterests[chunkPos] -= 1;

            if (MeshInterests[chunkPos] < 1)
            {
                MeshInterests.Remove(chunkPos);
                ActiveSources.Remove(SourcesDict[chunkPos]);
                SourcesDict.Remove(chunkPos);
            }
        }
    }

    public static void AddPOI(Vector2Int chunkPos, int range = 1)
    {
        for (int z = -range; z <= range; z++)
        {
            for (int x = -range; x <= range; x++)
            {
                Vector2Int chunkToCheck = new(x + chunkPos.x, z + chunkPos.y);

                if (GameManager.ActiveTerrain.ContainsKey(chunkToCheck))
                {
                    Chunk chunk = GameManager.ActiveTerrain[chunkToCheck];

                    if (!SourcesDict.ContainsKey(chunkToCheck))
                    {
                        if (chunk)
                        {
                            if (chunk.HasTerrain)
                            {
                                SourcesDict.Add(chunkToCheck, new());
                                MeshInterests.Add(chunkToCheck, 1);

                                if (FoliageManager.HasTrees(chunkToCheck))
                                {
                                    ChunksToAdd.Enqueue(chunkToCheck);
                                }
                                else
                                {
                                    AwaitingFoliage.Add(chunk.ChunkPosition);
                                }
                            }
                        }
                    }
                    else if (MeshInterests.ContainsKey(chunkToCheck))
                    {
                        MeshInterests[chunkToCheck] += 1;
                    }
                }
            }
        }

        UpdateBounds();
        Instance.enabled = true;
    }

    public static void CheckAwaiting(Vector2Int chunkPos)
    {
        if (AwaitingFoliage.Contains(chunkPos))
        {
            AwaitingFoliage.Remove(chunkPos);

            if (GameManager.ActiveTerrain[chunkPos].HasTerrain)
            {
                ChunksToAdd.Enqueue(chunkPos);
            }
        }
    }

    public static void AddUnreadyToEnable(Vector2Int chunkPosition, MonoBehaviour behaviour)
    {
        if (UnreadyToEnable.ContainsKey(chunkPosition))
        {
            UnreadyToEnable[chunkPosition].Add(behaviour);
        }
        else
        {
            UnreadyToEnable.Add(chunkPosition, new() { behaviour });
        }
    }

    private void OnDestroy()
    {
        MeshInstance.Remove();
    }
}