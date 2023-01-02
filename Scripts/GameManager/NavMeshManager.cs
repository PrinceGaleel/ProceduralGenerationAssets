using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.AI;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance;
    private static Dictionary<Transform, NavMeshBuildSource> Sources;
    private static Dictionary<Vector2Int, List<MonoBehaviour>> UnreadyToEnable;
    private static Vector2Int LastToEnable;

    private static List<Chunk> ChunksToAdd;
    private static List<Chunk> ChunksToRemove;

    private static AsyncOperation UpdateOperation;

    private const int NavMeshedDistance = 150;
    private static readonly int NormalizedNavDistance = Mathf.CeilToInt((float)NavMeshedDistance / Chunk.ChunkSize);

    [Header("Surface")]
    public NavMeshData _NavMeshData;
    private NavMeshDataInstance NavMeshDataInstance;
    private NavMeshBuildSettings BuildSettings;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple nav mesh managers detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;

            ChunksToAdd = new();
            ChunksToRemove = new();
            UnreadyToEnable = new();
            transform.position = Vector3.zero;

            Sources = new();

            BuildSettings = NavMesh.GetSettingsByID(0);
        }
    }

    private void Start()
    {
        LastToEnable = Chunk.GetChunkPosition(new Vector2(World.CurrentSaveData.LastPosition.x, World.CurrentSaveData.LastPosition.z) + new Vector2(1000, 1000));
        _NavMeshData = NavMeshBuilder.BuildNavMeshData(BuildSettings, new(Sources.Values), CalculateWorldBounds(new(Sources.Values)), transform.position, transform.rotation);

        if (_NavMeshData != null)
        {
            _NavMeshData.name = gameObject.name;

            NavMeshDataInstance.Remove();
            NavMeshDataInstance = new();

            if (!NavMeshDataInstance.valid)
            {
                if (_NavMeshData != null)
                {
                    NavMeshDataInstance = NavMesh.AddNavMeshData(_NavMeshData, transform.position, transform.rotation);
                    NavMeshDataInstance.owner = this;
                }
            }
        }

        UpdateOperation = NavMeshBuilder.UpdateNavMeshDataAsync(_NavMeshData, BuildSettings, new(Sources.Values), CalculateWorldBounds(new(Sources.Values)));
    }

    private void Update()
    {
        if (UpdateOperation.isDone)
        {
            if (UnreadyToEnable.ContainsKey(LastToEnable))
            {
                foreach (MonoBehaviour value in UnreadyToEnable[LastToEnable])
                {
                    value.enabled = true;
                }

                UnreadyToEnable.Remove(LastToEnable);
            }

            for (int i = 0; i < ChunksToRemove.Count; i++)
            {
                if (Vector2Int.Distance(ChunksToRemove[i].WorldPosition, World.LastPlayerChunkPos * Chunk.ChunkSize) < NavMeshedDistance)
                {
                    ChunksToRemove.RemoveAt(i);
                    i -= 1;
                }
            }

            for (int i = 0; i < ChunksToAdd.Count; i++)
            {
                if (Vector2Int.Distance(ChunksToAdd[i].WorldPosition, World.LastPlayerChunkPos * Chunk.ChunkSize) > NavMeshedDistance)
                {
                    ChunksToAdd.RemoveAt(i);
                    i -= 1;
                }
            }

            bool updateMesh = false;
            if (ChunksToAdd.Count > 0)
            {
                AddSource(ChunksToAdd[0].transform, ChunksToAdd[0].ChunkMeshFilter.sharedMesh);
                LastToEnable = ChunksToAdd[0].ChunkPosition;
                ChunksToAdd.RemoveAt(0);
                updateMesh = true;
            }

            if (ChunksToRemove.Count > 0)
            {
                Sources.Remove(ChunksToRemove[0].transform);
                ChunksToRemove.RemoveAt(0);
                updateMesh = true;
            }

            if (updateMesh)
            {
                UpdateOperation = NavMeshBuilder.UpdateNavMeshDataAsync(_NavMeshData, BuildSettings, new(Sources.Values), CalculateWorldBounds(new(Sources.Values)));
            }
        }
    }

    public static void CheckNavMesh()
    {
        for (int x = -NormalizedNavDistance; x <= NormalizedNavDistance; x++)
        {
            for (int z = -NormalizedNavDistance; z <= NormalizedNavDistance; z++)
            {
                if (World.ActiveTerrain.ContainsKey(new(x + World.LastPlayerChunkPos.x, z + World.LastPlayerChunkPos.y)))
                {
                    Chunk chunk = World.ActiveTerrain[new(x + World.LastPlayerChunkPos.x, z + World.LastPlayerChunkPos.y)];
                    if (chunk.Active)
                    {
                        if (!ChunksToAdd.Contains(chunk) && !Sources.ContainsKey(chunk.transform))
                        {
                            if (Vector2Int.Distance(World.LastPlayerChunkPos * Chunk.ChunkSize, chunk.WorldPosition) < NavMeshedDistance)
                            {
                                ChunksToAdd.Add(chunk);
                            }
                        }
                    }
                }
            }
        }

        foreach (Transform key in Sources.Keys)
        {
            Chunk chunk = World.ActiveTerrain[Chunk.GetChunkPosition(new(key.position.x, key.position.z))];
            if (key.gameObject.activeSelf && !ChunksToRemove.Contains(chunk))
            {
                if (Vector2Int.Distance(chunk.WorldPosition, World.LastPlayerChunkPos * Chunk.ChunkSize) > NavMeshedDistance)
                {
                    ChunksToRemove.Add(chunk);
                }
            }
        }
    }

    public static void AddUnreadyToEnable(Vector2Int chunkPosition, MonoBehaviour behaviour)
    {
        if (Sources.ContainsKey(World.ActiveTerrain[chunkPosition].transform))
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

    private void AddSource(Transform sourceTransform, Mesh mesh)
    {
        Sources.Add(sourceTransform, new()
        {
            transform = sourceTransform.localToWorldMatrix,
            size = Vector3.zero,
            shape = NavMeshBuildSourceShape.Mesh,
            area = 0,
            sourceObject = mesh
        });
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
    {
        Vector3 absAxisX = Abs(mat.MultiplyVector(Vector3.right));
        Vector3 absAxisY = Abs(mat.MultiplyVector(Vector3.up));
        Vector3 absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
        Vector3 worldPosition = mat.MultiplyPoint(bounds.center);
        Vector3 worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
        return new Bounds(worldPosition, worldSize);
    }

    public Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
    {
        // Use the unscaled matrix for the NavMeshSurface
        Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        worldToLocal = worldToLocal.inverse;

        Bounds result = new();
        foreach (NavMeshBuildSource src in sources)
        {
            switch (src.shape)
            {
                case NavMeshBuildSourceShape.Mesh:
                    {
                        Mesh m = src.sourceObject as Mesh;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                        break;
                    }
                case NavMeshBuildSourceShape.Terrain:
                    {
                        // Terrain pivot is lower/left corner - shift bounds accordingly
                        TerrainData t = src.sourceObject as TerrainData;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
                        break;
                    }
                case NavMeshBuildSourceShape.Box:
                case NavMeshBuildSourceShape.Sphere:
                case NavMeshBuildSourceShape.Capsule:
                case NavMeshBuildSourceShape.ModifierBox:
                    result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                    break;
            }
        }

        // Inflate the bounds a bit to avoid clipping co-planar sources
        result.Expand(0.1f);
        return result;
    }
}