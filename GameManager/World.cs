using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.Rendering;
using System.Collections.Concurrent;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    public static Transform WorldTransform { get; private set; }
    public static GameObject ChunkPrefab { get; private set; }
    public static SaveData CurrentSaveData { get; private set; }

    public static Vector2Int LastPlayerChunkPos { get; private set; }
    public static Vector2Int CurrentPlayerChunkPos { get; private set; }

    public static ConcurrentDictionary<Vector2Int, Chunk> ActiveTerrain { get; private set; }
    public static ConcurrentQueue<Vector2Int> ChunksToAdd;
    public static ConcurrentQueue<Chunk> MeshesToAssign;
    private static CheckChunksJob CheckChunksCheck;
    private static ConcurrentQueue<GameObject> ToDestroy;

    [Header("Renderer Distances")]
    public const int LODOneDistance = 3;
    public const int LODTwoDistance = 7;
    public const int LODThreeDistance = 15;

    [Header("Other")]
    public static System.Random Rnd;

    public static int TerrainMask { get; private set; }
    public static int MeleeWeaponMask { get; private set; }
    public static int GravityMask { get; private set; }

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

    private void Start()
    {
        CurrentPlayerChunkPos = Chunk.GetChunkPosition(PlayerStats.PlayerTransform.position.x, PlayerStats.PlayerTransform.position.z);
        CheckChunksCheck = new CheckChunksJob();
        CheckChunksCheck.Schedule();
    }

    private void Update()
    {
        CurrentPlayerChunkPos = Chunk.GetChunkPosition(PlayerStats.PlayerTransform.position.x, PlayerStats.PlayerTransform.position.z);

        if (MeshesToAssign.Count > 0)
        {
            if (MeshesToAssign.TryDequeue(out Chunk chunk))
            {
                if (ActiveTerrain.ContainsKey(chunk.ChunkPosition))
                {
                    chunk.CreateMesh();
                }
            }
        }

        if (ChunksToAdd.Count > 0)
        {
            if (ChunksToAdd.TryDequeue(out Vector2Int chunkPos))
            {
                new CreateChunk(chunkPos).Schedule();
            }
        }
        else if (ToDestroy.Count > 0)
        {
            if (ToDestroy.TryDequeue(out GameObject chunk))
            {
                Destroy(chunk, 1);
            }
        }

        if (LastPlayerChunkPos != CurrentPlayerChunkPos)
        { 
            if (CheckChunksCheck.IsCompleted)
            {
                CheckChunksCheck = new CheckChunksJob();
                CheckChunksCheck.Schedule();

                NavMeshManager.AddPOI(CurrentPlayerChunkPos);
                NavMeshManager.RemovePOI(LastPlayerChunkPos);

                LastPlayerChunkPos = CurrentPlayerChunkPos;
                RenderTerrainMap.ReloadBlending();
                GrassManager.RemapGrass();
            }
        }
    }

    public static void InitializeWorldData(SaveData saveData)
    {
        CurrentSaveData = saveData;
        Rnd = new();

        ActiveTerrain = new();
        MeshesToAssign = new();

        CurrentPlayerChunkPos = new(Mathf.RoundToInt(CurrentSaveData.LastPosition.x / Chunk.DefaultChunkSize), Mathf.RoundToInt(CurrentSaveData.LastPosition.z / Chunk.DefaultChunkSize));
        LastPlayerChunkPos = CurrentPlayerChunkPos;
        ChunksToAdd = new();
        ToDestroy = new();

        GrassChunk.ShaderInteractors = new();
        Chunk.InitializeStatics();
        StructureCreator.InitializeStatics();
        TeamsManager.InitializeStatics();
        FoliageManager.InitializeStatics();
    }

    public static void InitializeConstants(GameObject chunkPrefab)
    {
        ChunkPrefab = chunkPrefab;
        TerrainMask = LayerMask.GetMask("Terrain");
        MeleeWeaponMask = LayerMask.GetMask("Controller", "Hitbox");
        GravityMask = ~LayerMask.GetMask("Water", "Grass", "Controller", "Weapon", "Arms", "Harvestable", "Hitbox", "Resource");
        GrassChunk.GrassLayer = LayerMask.NameToLayer("Grass");
    }

    public class CreateChunk : MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;

        public CreateChunk(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public override void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition))
            {
                ActiveTerrain[ChunkPosition] = Instantiate(ChunkPrefab, new(ChunkPosition.x * Chunk.DefaultChunkSize, 0, ChunkPosition.y * Chunk.DefaultChunkSize), Quaternion.identity, WorldTransform).GetComponent<Chunk>();
                ActiveTerrain[ChunkPosition].SetPositions(ChunkPosition);
                new InitializeChunk(ChunkPosition).Schedule();
            }
        }
    }

    public class InitializeChunk : SecondaryThreadJob
    {
        private readonly Vector2Int ChunkPosition;

        public InitializeChunk(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public override void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition))
            {
                Vector2Int absDistance = new(Mathf.Abs(CurrentPlayerChunkPos.x - ChunkPosition.x), Mathf.Abs(CurrentPlayerChunkPos.y - ChunkPosition.y));

                if (absDistance.x <= LODOneDistance && absDistance.y <= LODOneDistance)
                {
                    ActiveTerrain[ChunkPosition].InitializeLODOne();
                }
                else if (absDistance.x <= LODTwoDistance && absDistance.y <= LODTwoDistance)
                {
                    ActiveTerrain[ChunkPosition].InitializeLODTwo();
                }
                else
                {
                    ActiveTerrain[ChunkPosition].InitializeLODThree();
                }

                new AssignChunkMeshJob(ChunkPosition).Schedule();
            }
        }
    }

    public class AssignChunkMeshJob: MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;

        public AssignChunkMeshJob(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public override void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition))
            {
                if (ActiveTerrain[ChunkPosition] != null)
                {
                    ActiveTerrain[ChunkPosition].CreateMesh();
                }
            }
        }
    }

    public class CheckChunksJob : SecondaryThreadJob
    {
        public bool IsCompleted = false;

        public override void Execute()
        {
            List<Vector2Int> keys = new(ActiveTerrain.Keys);
            foreach (Vector2Int key in keys)
            {
                if (Mathf.Abs(CurrentPlayerChunkPos.x - key.x) > LODThreeDistance || Mathf.Abs(CurrentPlayerChunkPos.y - key.y) > LODThreeDistance)
                {
                    if (ActiveTerrain.TryRemove(key, out Chunk chunk))
                    {
                        if (chunk)
                        {
                            ToDestroy.Enqueue(chunk.MyGameObject);
                        }

                        FoliageManager.ClearFoliage(key);
                    }
                }
            }

            for (int x = -LODThreeDistance; x <= LODThreeDistance; x++)
            {
                for (int z = -LODThreeDistance; z <= LODThreeDistance; z++)
                {
                    Vector2Int chunkPos = new(CurrentPlayerChunkPos.x + x, CurrentPlayerChunkPos.y + z);

                    if (!ActiveTerrain.ContainsKey(chunkPos))
                    {
                        if (ActiveTerrain.TryAdd(chunkPos, null))
                        {
                            ChunksToAdd.Enqueue(chunkPos);
                        }
                    }
                    else if (ActiveTerrain.ContainsKey(chunkPos))
                    {
                        if (ActiveTerrain[chunkPos])
                        {
                            Vector2Int absDistance = new(Mathf.Abs(CurrentPlayerChunkPos.x - chunkPos.x), Mathf.Abs(CurrentPlayerChunkPos.y - chunkPos.y));

                            if (absDistance.x <= LODOneDistance && absDistance.y <= LODOneDistance)
                            {
                                if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODOne)
                                {
                                    ActiveTerrain[chunkPos].InitializeLODOne();
                                }
                            }
                            else if (absDistance.x <= LODTwoDistance && absDistance.y <= LODTwoDistance)
                            {
                                if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODTwo)
                                {
                                    ActiveTerrain[chunkPos].InitializeLODTwo();
                                }
                            }
                            else if (ActiveTerrain[chunkPos].CurrentLODState != TerrainLODStates.LODThree)
                            {
                                ActiveTerrain[chunkPos].InitializeLODThree();
                            }
                        }
                    }
                }
            }

            IsCompleted = true;
        }
    }
}

public class ToDestroyJob : MainThreadJob
{
    private readonly GameObject ToDestroy;

    public ToDestroyJob(GameObject toDestroy)
    {
        ToDestroy = toDestroy;
    }

    public override void Execute()
    {
        Object.Destroy(ToDestroy);
    }
}

public class ToDisableJob : MainThreadJob
{
    private readonly GameObject ToDisable;

    public ToDisableJob(GameObject toDisable)
    {
        ToDisable = toDisable;
    }

    public override void Execute()
    {
        ToDisable.SetActive(false);
    }
}