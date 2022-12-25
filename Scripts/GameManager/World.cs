using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public enum StructureTypes
{ 
    Village,
    MobDen
}

public class World : MonoBehaviour
{
    public static World Instance;
    public static Transform WorldTransform { get; private set; }
    public static SaveData CurrentSaveData { private set; get; }
    public static Vector2Int LastPlayerChunkPos { private set; get; }
    private static Vector2Int CurrentPlayerChunkPos { get { return new(Mathf.RoundToInt(PlayerController.Instance.transform.position.x / Chunk.ChunkSize), Mathf.RoundToInt(PlayerController.Instance.transform.position.z / Chunk.ChunkSize)); } }
    public static BiomeData[] Biomes;

    [Header("Chunk Variables")]
    private static Thread ChunkThread;
    public static Dictionary<Vector2Int, Chunk> ActiveTerrain;
    private static List<Vector2Int> ActiveGrass;
    private static List<Vector2Int> ActiveTrees;

    public static Queue<Vector2Int> MeshesToCreate { get; set; }
    public static Queue<Vector2Int> MeshesToUpdate;
    public static CustomDictionary<GrassChunk, List<GrassChunk.SourceVertex>> AssignGrassMesh;
    public static CustomDictionary<Chunk, StructureTypes> StructuresToCreate;
    private static int RoundedChunkViewDist;

    [Header("Materials and Shaders")]
    public Material MeshMaterial;
    public Material GrassMaterial;
    public ComputeShader GrassShader;
    public Transform WaterDisplay;
    public static List<ShaderInteractor> ShaderInteractors;

    [Header("Other")]
    public List<Item> UnlockedRecipes;
    public static System.Random Rnd;

    [Header("Masks")]
    public static int ToolMask;
    public static int TerrainMask;
    public static int WeaponMask;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple world instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            WorldTransform = transform;
            WorldTransform.position = Vector3.zero;
            WaterDisplay.position = new(0, Biomes[0].HighestPoint * SaveData.HeightMultipler, 0);

            ChunkThread = new(new ThreadStart(ChunkUpdate));
        }
    }

    private void Start()
    {
        ChunkThread.Start();

        if (!PlayerController.Instance)
        {
            enabled = false;
        }
        else
        {
            PlayerController.Instance.gameObject.SetActive(false);
            CheckForNewChunks();
        }
    }

    private void Update()
    {
        if (LastPlayerChunkPos != CurrentPlayerChunkPos)
        {
            LastPlayerChunkPos = CurrentPlayerChunkPos;

            foreach (KeyValuePair<Vector2Int, Chunk> pair in ActiveTerrain)
            {
                if (pair.Value.Active)
                {
                    if (Vector2Int.Distance(pair.Key * Chunk.ChunkSize, LastPlayerChunkPos) >= GlobalSettings.CurrentSettings.ViewDistanceInWorld)
                    {
                        pair.Value.Active = false;
                    }
                }
            }

            lock (ActiveGrass)
            {
                for (int i = ActiveGrass.Count - 1; i > -1; i--)
                {
                    if (Vector2Int.Distance(ActiveTerrain[ActiveGrass[i]].WorldPosition, LastPlayerChunkPos) >= GrassChunk.GrassRenderDistance)
                    {
                        ActiveTerrain[ActiveGrass[i]].GrassMesh.enabled = false;
                        ActiveGrass.RemoveAt(i);
                    }
                }
            }
        }

        //CheckForNewChunks();

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
                    Chunk.GetPerlinNoise(pair.Key.WorldPosition.x, pair.Key.WorldPosition.y, CurrentSaveData.HeightPerlin);
                }
            }
        }
        else if (AssignGrassMesh.Count > 0)
        {
            lock (AssignGrassMesh)
            {
                CustomPair<GrassChunk, List<GrassChunk.SourceVertex>> pair = AssignGrassMesh.TakePairAt(0);
                pair.Key.AssignMesh(pair.Value);
            }
        }
        else if (MeshesToUpdate.Count > 0)
        {
            lock (MeshesToUpdate)
            {
                ActiveTerrain[MeshesToUpdate.Dequeue()].AssignMesh();
            }
        }
    }

    private void LateUpdate()
    {
        WaterDisplay.transform.position = new(PlayerController.Instance.transform.position.x, WaterDisplay.transform.position.y, PlayerController.Instance.transform.position.z);
    }

    public static void InitializeWorldData(SaveData saveData)
    {
        CurrentSaveData = saveData;
        Rnd = new();

        ShaderInteractors = new();

        ActiveTerrain = new();
        ActiveGrass = new();
        ActiveTrees = new();

        MeshesToCreate = new();
        MeshesToUpdate = new();
        AssignGrassMesh = new();

        StructuresToCreate = new();

        LastPlayerChunkPos = new(0, 0);

        GrassChunk.RoundedGrassViewDistance = Mathf.CeilToInt((float)GrassChunk.GrassRenderDistance / Chunk.ChunkSize);
        RoundedChunkViewDist = Mathf.CeilToInt((float)GlobalSettings.CurrentSettings.ViewDistanceInWorld / Chunk.ChunkSize);

        Chunk.Triangles = new int[Chunk.ChunkSize * Chunk.ChunkSize * 6];
        for (int y = 0, tris = 0, vertexIndex = 0; y < Chunk.ChunkSize; y++)
        {
            for (int x = 0; x < Chunk.ChunkSize; x++)
            {
                Chunk.Triangles[tris] = vertexIndex;
                Chunk.Triangles[tris + 1] = vertexIndex + Chunk.ChunkSize + 1;
                Chunk.Triangles[tris + 2] = vertexIndex + 1;
                Chunk.Triangles[tris + 3] = vertexIndex + 1;
                Chunk.Triangles[tris + 4] = vertexIndex + Chunk.ChunkSize + 1;
                Chunk.Triangles[tris + 5] = vertexIndex + Chunk.ChunkSize + 2;

                vertexIndex++;
                tris += 6;
            }

            vertexIndex++;
        }

        TeamsManager.Initialize();
        FoliageManager.InitializeStatics();
    }

    public static void ChunkUpdate()
    {
        while (true)
        {
            if (MeshesToCreate.Count > 0)
            {
                if (MeshesToCreate.Count > 0)
                {
                    ActiveTerrain[MeshesToCreate.Dequeue()].CreateMesh();
                }
            }
        }
    }

    private void CheckForNewChunks()
    {
        for (int x = -RoundedChunkViewDist; x <= RoundedChunkViewDist; x++)
        {
            for (int z = -RoundedChunkViewDist; z <= RoundedChunkViewDist; z++)
            {
                Vector2Int chunkToCheck = new(x + LastPlayerChunkPos.x, z + LastPlayerChunkPos.y);

                if (ActiveTerrain.ContainsKey(chunkToCheck))
                {
                    ActiveTerrain[chunkToCheck].Active = true;
                }
                else
                {
                    ActiveTerrain.Add(chunkToCheck, new GameObject().AddComponent<Chunk>());
                    ActiveTerrain[chunkToCheck].Initialize(chunkToCheck * Chunk.ChunkSize);
                    MeshesToCreate.Enqueue(chunkToCheck);
                }
            }
        }

        for (int x = -GrassChunk.RoundedGrassViewDistance; x <= GrassChunk.RoundedGrassViewDistance; x++)
        {
            for (int z = -GrassChunk.RoundedGrassViewDistance; z <= GrassChunk.RoundedGrassViewDistance; z++)
            {
                Vector2Int chunkToCheck = new(x + LastPlayerChunkPos.x, z + LastPlayerChunkPos.y);

                if (ActiveTerrain.ContainsKey(chunkToCheck))
                {
                    if (Vector2Int.Distance(LastPlayerChunkPos, chunkToCheck * Chunk.ChunkSize) < GrassChunk.GrassRenderDistance)
                    {
                        lock (ActiveGrass)
                        {
                            Chunk chunk = ActiveTerrain[chunkToCheck];
                            if (!ActiveGrass.Contains(chunk.ChunkPosition) && chunk.GrassReady && chunk.TerrainReady)
                            {
                                chunk.GrassMesh.enabled = true;
                                ActiveGrass.Add(chunk.ChunkPosition);
                                RenderTerrainMap.Instance.ReloadBlending();
                            }
                        }
                    }
                }
            }
        }
    }

    public static void SetMasks()
    {
        ToolMask = ~LayerMask.GetMask("Controller", "Harvestable");
        TerrainMask = LayerMask.GetMask("Terrain");
        WeaponMask = LayerMask.GetMask("Hitbox");
    }

    private void OnDestroy()
    {
        ChunkThread.Abort();
    }
}