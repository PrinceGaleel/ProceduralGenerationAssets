using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.AI;

public class NavMeshManager : MonoBehaviour
{
    private static NavMeshManager Instance;

    private static ThreadSafeNavMeshSurface _NavMeshSurface;
    private static Dictionary<Vector2Int, Chunk> NavMeshes;
    private static List<Chunk> ChunksToAdd;
    private static List<Chunk> ChunksToRemove;
    private static NavMeshData _NavMeshData;
    private static AsyncOperation UpdateOperation;

    //private readonly static Thread NavMeshThread = new(new ThreadStart(NavMeshUpdate));
    private const int NavMeshedDistance = 150;
    private static readonly int NormalizedNavDistance = Mathf.CeilToInt((float)NavMeshedDistance / Chunk.ChunkSize);
    private const int UpdatePauseSeconds = 2;

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

            NavMeshes = new();
            ChunksToAdd = new();
            ChunksToRemove = new();

            transform.position = Vector3.zero;

            if (!_NavMeshSurface)
            {
                _NavMeshSurface = GetComponent<ThreadSafeNavMeshSurface>();

                if (!_NavMeshSurface)
                {
                    _NavMeshSurface = gameObject.AddComponent<ThreadSafeNavMeshSurface>();
                }
            }
        }
    }

    private void Start()
    {
        _NavMeshData = _NavMeshSurface.BuildNavMesh();
        UpdateOperation = _NavMeshSurface.UpdateNavMesh(_NavMeshData);
        StartCoroutine(UpdateNav());
    }

    private void Update()
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
                        if (!ChunksToAdd.Contains(chunk) && !NavMeshes.ContainsKey(chunk.WorldPosition))
                        {
                            if (Vector2Int.Distance(World.LastPlayerChunkPos, chunk.WorldPosition) < NavMeshedDistance)
                            {
                                ChunksToAdd.Add(chunk);
                            }
                        }
                    }
                }
            }
        }

        foreach (Vector2Int key in NavMeshes.Keys)
        {
            if (NavMeshes[key].gameObject.activeSelf && !ChunksToRemove.Contains(NavMeshes[key]))
            {
                if (Vector2Int.Distance(NavMeshes[key].WorldPosition, World.LastPlayerChunkPos) > NavMeshedDistance)
                {
                    ChunksToRemove.Add(NavMeshes[key]);
                }
            }
        }
    }

    private static IEnumerator UpdateNav()
    {
        while (Instance)
        {
            if (UpdateOperation.isDone)
            {
                for (int i = 0; i < ChunksToRemove.Count; i++)
                {
                    if (Vector2Int.Distance(ChunksToRemove[i].WorldPosition, World.LastPlayerChunkPos) < NavMeshedDistance)
                    {
                        ChunksToRemove.RemoveAt(i);
                        i -= 1;
                    }
                }

                for (int i = 0; i < ChunksToAdd.Count; i++)
                {
                    if (Vector2Int.Distance(ChunksToAdd[i].WorldPosition, World.LastPlayerChunkPos) > NavMeshedDistance)
                    {
                        ChunksToAdd.RemoveAt(i);
                        i -= 1;
                    }
                }

                if (ChunksToAdd.Count > 0)
                {
                    NavMeshes.Add(ChunksToAdd[0].WorldPosition, ChunksToAdd[0]);
                    ChunksToAdd[0].transform.SetParent(Instance.transform);
                    ChunksToAdd.RemoveAt(0);

                    _NavMeshSurface.UpdateNavMesh(_NavMeshData);

                    yield return new WaitForSeconds(UpdatePauseSeconds);
                }
                
                if (ChunksToRemove.Count > 0)
                {
                    NavMeshes[ChunksToRemove[0].WorldPosition].transform.SetParent(World.WorldTransform);
                    NavMeshes.Remove(ChunksToRemove[0].WorldPosition);
                    ChunksToRemove.RemoveAt(0);

                    _NavMeshSurface.UpdateNavMesh(_NavMeshData);

                    yield return new WaitForSeconds(UpdatePauseSeconds);
                }
            }

            yield return null;
        }
    }

    /*
    private void OnDestroy()
    {
        if (NavMeshThread.IsAlive)
        {
            NavMeshThread.Abort();
        }
    }
    */
}