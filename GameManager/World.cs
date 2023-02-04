using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using System.Collections.Concurrent;

public class World : MonoBehaviour
{
    public static World Instance;
    public static Transform WorldTransform;
    public static GameObject ChunkPrefab;

    public static SaveData CurrentSaveData;
    public static BiomeData[] Biomes;

    public static Vector2Int LastPlayerChunkPos;
    private static Vector2Int CurrentPlayerChunkPos;

    [Header("Chunk Variables")]
    private static Queue<Chunk> ChunksBuffer;
    private static HashSet<Vector2Int> ChunksToInitialize;

    public static ConcurrentDictionary<Vector2Int, Chunk> ActiveTerrain { get; private set; }
    private static ConcurrentQueue<Chunk> MeshesToAssign;
    public static DictList<Chunk, StructureTypes> StructuresToCreate;

    [Header("Renderer Distances")]
    public const int LODOneDistance = 2;
    public const int LODTwoDistance = 5;
    public const int LODThreeDistance = 10;
    public const int LODFourDistance = 20;
    public const int LODFiveDistance = 50;

    [Header("Other")]
    public static System.Random Rnd;

    [Header("Masks")]
    public static int TerrainMask;
    public static int WeaponMask;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple world instances detected");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            WorldTransform = transform;
            WorldTransform.position = Vector3.zero;
        }

        enabled = false;
    }

    private void Update()
    {
        CurrentPlayerChunkPos = Chunk.GetChunkPosition(PlayerStats.PlayerTransform.position.x, PlayerStats.PlayerTransform.position.z);

        if (StructuresToCreate.Count > 0)
        {
            lock (StructuresToCreate)
            {
                CustomPair<Chunk, StructureTypes> pair = StructuresToCreate.TakePairAt(0);

                if (pair.Value == StructureTypes.Village)
                {
                    StructureCreator.CreateVillage(pair.Key);
                }
                else if (pair.Value == StructureTypes.MobDen)
                {
                    Instantiate(StructureCreator.Instance.MobDenPrefabs[Random.Range(0, StructureCreator.Instance.MobDenPrefabs.Length)],
                        Chunk.GetPerlinPosition(pair.Key.WorldPosition), Quaternion.identity).transform.SetParent(pair.Key.MyTransform);
                }
            }
        }
        else if (MeshesToAssign.Count > 0)
        {
            if (MeshesToAssign.TryDequeue(out Chunk chunk))
            {
                if (ActiveTerrain.ContainsKey(chunk.ChunkPosition))
                {
                    if(ChunksToInitialize.Contains(chunk.ChunkPosition))
                    {
                        chunk.MoveTransform();
                    }

                    chunk.AssignMesh();
                    RenderTerrainMap.ReloadBlending();
                }
            }
        }

        if (LastPlayerChunkPos != CurrentPlayerChunkPos)
        {
            new CheckChunksJob().Schedule();
            LastPlayerChunkPos = CurrentPlayerChunkPos;
            NavMeshManager.UpdateNavMesh();
            RenderTerrainMap.ReloadBlending();
        }
    }

    public static void InitializeWorldData(SaveData saveData)
    {
        CurrentSaveData = saveData;
        Rnd = new();

        ActiveTerrain = new();
        MeshesToAssign = new();

        StructuresToCreate = new();

        CurrentPlayerChunkPos = new(Mathf.RoundToInt(CurrentSaveData.LastPosition.x / Chunk.DefaultChunkSize), Mathf.RoundToInt(CurrentSaveData.LastPosition.z / Chunk.DefaultChunkSize));
        LastPlayerChunkPos = CurrentPlayerChunkPos - new Vector2Int(1, 1);
        ChunksToInitialize = new();
        ChunksBuffer = new();

        TeamsManager.Initialize();
        FoliageManager.Initialize();
    }

    public static void SetMasks()
    {
        TerrainMask = LayerMask.GetMask("Terrain");
        WeaponMask = LayerMask.GetMask("Hitbox");
    }

    public struct CheckChunksJob : IJob
    {
        public void Execute()
        {
            List<Vector2Int> keys = new(ActiveTerrain.Keys);
            foreach (Vector2Int item in keys)
            {
                if (Mathf.Abs(CurrentPlayerChunkPos.x - ActiveTerrain[item].ChunkPosition.x) > LODFiveDistance || Mathf.Abs(CurrentPlayerChunkPos.y - ActiveTerrain[item].ChunkPosition.y) > LODFiveDistance)
                {
                    if (ActiveTerrain.TryRemove(item, out Chunk chunk))
                    {
                        ChunksBuffer.Enqueue(chunk);
                    }
                }
            }

            for (int x = -LODFiveDistance; x <= LODFiveDistance; x++)
            {
                for (int z = -LODFiveDistance; z <= LODFiveDistance; z++)
                {
                    Vector2Int chunkPos = new(CurrentPlayerChunkPos.x + x, CurrentPlayerChunkPos.y + z);
                    Vector2Int absDistance = new(Mathf.Abs(CurrentPlayerChunkPos.x - chunkPos.x), Mathf.Abs(CurrentPlayerChunkPos.y - chunkPos.y));

                    if (!ActiveTerrain.ContainsKey(chunkPos))
                    {
                        if (ChunksBuffer.TryDequeue(out Chunk chunk))
                        {
                            if (ActiveTerrain.TryAdd(chunkPos, chunk))
                            {
                                chunk.SetPositions(chunkPos);
                                ChunksToInitialize.Add(chunk.ChunkPosition);

                                if (absDistance.x <= LODOneDistance && absDistance.y <= LODOneDistance)
                                {
                                    ActiveTerrain[chunkPos].AssignLODOne();
                                }
                                else if (absDistance.x <= LODTwoDistance && absDistance.y <= LODTwoDistance)
                                {
                                    ActiveTerrain[chunkPos].AssignLODTwo();
                                }
                                else if (absDistance.x <= LODThreeDistance && absDistance.y <= LODThreeDistance)
                                {
                                    ActiveTerrain[chunkPos].AssignLODThree();
                                }
                                else if (absDistance.x <= LODFourDistance && absDistance.y <= LODFourDistance)
                                {
                                    ActiveTerrain[chunkPos].AssignLODFour();
                                }
                                else
                                {
                                    ActiveTerrain[chunkPos].AssignLODFive();
                                }
                            }
                            else
                            {
                                ChunksBuffer.Enqueue(chunk);
                            }
                        }
                    }
                    else if (ActiveTerrain.ContainsKey(chunkPos))
                    {
                        if (absDistance.x <= LODOneDistance && absDistance.y <= LODOneDistance)
                        {
                            if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODOne)
                            {
                                ActiveTerrain[chunkPos].AssignLODOne();
                            }
                        }
                        else if (absDistance.x <= LODTwoDistance && absDistance.y <= LODTwoDistance)
                        {
                            if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODTwo)
                            {
                                ActiveTerrain[chunkPos].AssignLODTwo();
                            }
                        }
                        else if (absDistance.x <= LODThreeDistance && absDistance.y <= LODThreeDistance)
                        {
                            if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODThree)
                            {
                                ActiveTerrain[chunkPos].AssignLODThree();
                            }
                        }
                        else if (absDistance.x <= LODFourDistance && absDistance.y <= LODFourDistance)
                        {
                            if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODFour)
                            {
                                ActiveTerrain[chunkPos].AssignLODFour();
                            }
                        }
                        else if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODFive)
                        {
                            ActiveTerrain[chunkPos].AssignLODFive();
                        }
                    }
                }
            }
        }
    }
}