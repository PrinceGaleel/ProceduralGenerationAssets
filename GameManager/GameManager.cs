using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;

using static Chunk;
using static PlayerStats;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static Transform WorldTransform { get; private set; }
    public static GameObject ChunkPrefab { get; private set; }
    public static SaveData CurrentSaveData { get; private set; }
    public static PerlinData MyPerlinData { get; private set; }
    public static int Seed { get; private set; }

    public static float GetHeightPerlin(Vector2 position) { return CurrentSaveData.MyPerlinData.GetHeightPerlin(position); }
    public static float GetHeightPerlin(float x, float y) { return CurrentSaveData.MyPerlinData.GetHeightPerlin(x, y); }
    public static float GetTemperaturePerlin(Vector2 position) { return CurrentSaveData.MyPerlinData.GetTemperaturePerlin(position); }
    public static float GetTemperaturePerlin(float x, float y) { return CurrentSaveData.MyPerlinData.GetTemperaturePerlin(x, y); }
    public static Vector3 GetPerlinPosition(float x, float y) { return CurrentSaveData.MyPerlinData.GetPerlinPosition(x, y); }
    public static Vector3 GetPerlinPosition(Vector2 position) { return CurrentSaveData.MyPerlinData.GetPerlinPosition(position); }

    public static Vector2Int LastPlayerChunkPos { get; private set; }
    public static Vector2Int CurrentPlayerChunkPos { get; private set; }

    public static ConcurrentDictionary<Vector2Int, Chunk> ActiveTerrain { get; private set; }
    public static ConcurrentDictionary<Vector2Int, float[]> HeightMap { get; private set; }
    public static ConcurrentDictionary<Vector2Int, float[]> TemperatureMap { get; private set; }

    public static ConcurrentQueue<Vector2Int> ChunksToAdd;
    private static JobHandle CheckChunksCheck;
    private static ConcurrentQueue<GameObject> ToDestroy;

    public const int ViewDistance = 7;
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
        CurrentPlayerChunkPos = Chunk.GetChunkPosition(PlayerTransform.position.x, PlayerTransform.position.z);
        CheckChunksCheck = new CheckChunksJob().Schedule();
    }

    private void Update()
    {
        CurrentPlayerChunkPos = GetChunkPosition(PlayerTransform.position.x, PlayerTransform.position.z);

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
            GrassManager.RemapGrass(CurrentPlayerChunkPos);

            if (CheckChunksCheck.IsCompleted)
            {
                CheckChunksCheck = new CheckChunksJob().Schedule();

                LastPlayerChunkPos = CurrentPlayerChunkPos;
                RenderTerrainMap.ReloadBlending();
            }
        }
    }

    public static void InitializeWorldData(SaveData saveData)
    {
        CurrentSaveData = saveData;
        Seed = CurrentSaveData.MyPerlinData.Seed;
        MyPerlinData = CurrentSaveData.MyPerlinData;
        Rnd = new(Seed);

        ActiveTerrain = new();
        HeightMap = new();
        TemperatureMap = new();

        CurrentPlayerChunkPos = new(Mathf.RoundToInt(CurrentSaveData.LastPosition.x / Chunk.ChunkSize), Mathf.RoundToInt(CurrentSaveData.LastPosition.z / Chunk.ChunkSize));
        LastPlayerChunkPos = CurrentPlayerChunkPos;
        ChunksToAdd = new();
        ToDestroy = new();

        GrassChunk.ShaderInteractors = new();
        WaitingForTerrain = new();
    }

    public static void InitializeConstants(GameObject chunkPrefab)
    {
        ChunkPrefab = chunkPrefab;
        TerrainMask = LayerMask.GetMask("Terrain");
        MeleeWeaponMask = LayerMask.GetMask("Controller", "Hitbox");
        GravityMask = ~LayerMask.GetMask("Water", "Grass", "Controller", "Controller Collider", "Weapon", "Arms", "Harvestable", "Hitbox", "Resource");
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
                ActiveTerrain[ChunkPosition] = Instantiate(ChunkPrefab, new(ChunkPosition.x * ChunkSize, 0, ChunkPosition.y * ChunkSize), Quaternion.identity, WorldTransform).GetComponent<Chunk>();
                ActiveTerrain[ChunkPosition].SetPositions(ChunkPosition);
                new PrepareChunk(ChunkPosition).Schedule();
            }
        }
    }

    private struct CheckChunksJob : IJob
    {
        public void Execute()
        {
            List<Vector2Int> keys = new(ActiveTerrain.Keys);
            foreach (Vector2Int key in keys)
            {
                if (Mathf.Abs(CurrentPlayerChunkPos.x - key.x) > ViewDistance || Mathf.Abs(CurrentPlayerChunkPos.y - key.y) > ViewDistance)
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

            for (int x = -ViewDistance; x <= ViewDistance; x++)
            {
                for (int z = -ViewDistance; z <= ViewDistance; z++)
                {
                    Vector2Int chunkPos = new(CurrentPlayerChunkPos.x + x, CurrentPlayerChunkPos.y + z);

                    if (!ActiveTerrain.ContainsKey(chunkPos))
                    {
                        if (ActiveTerrain.TryAdd(chunkPos, null))
                        {
                            ChunksToAdd.Enqueue(chunkPos);
                        }
                    }
                }
            }
        }
    }
}