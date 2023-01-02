using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public static World Instance;
    public static Transform WorldTransform { get; private set; }
    public static SaveData CurrentSaveData { private set; get; }
    public static Vector2Int LastPlayerChunkPos { private set; get; }
    private static Vector2Int CurrentPlayerChunkPos { get { return new(Mathf.RoundToInt(PlayerController.Instance.transform.position.x / Chunk.ChunkSize), Mathf.RoundToInt(PlayerController.Instance.transform.position.z / Chunk.ChunkSize)); } }
    public static BiomeData[] Biomes;

    [Header("Character Variables")]
    public GameObject MaleCharacterBase;
    public GameObject FemaleCharacterBase;

    [Header("Chunk Variables")]
    private static Thread ChunkThread;
    public static Dictionary<Vector2Int, Chunk> ActiveTerrain;
    private static List<Vector2Int> ActiveGrass;
    private static List<Vector2Int> ActiveTrees;

    public static Queue<Vector2Int> MeshesToCreate { get; set; }
    public static Queue<Vector2Int> MeshesToUpdate;
    public static CustomDictionary<GrassChunk, List<GrassChunk.SourceVertex>> AssignGrassMesh;
    public static CustomDictionary<Chunk, StructureTypes> StructuresToCreate;

    public const int ChunkRenderDistance = 400;
    public const int GrassRenderDistance = 120;
    public const int TreeRenderDistance = 200;

    private static int RoundedChunkViewDistance;
    private static int RoundedGrassViewDistance;
    private static int RoundedTreeViewDistance;

    [Header("Materials and Shaders")]
    public static Material MeshMaterial;
    public static Material GrassMaterial;
    public static ComputeShader GrassShader;
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
            CheckForNewChunks();
        }
    }

    private void Update()
    {
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
                        Chunk.GetPerlinPosition(pair.Key.WorldPosition.x, pair.Key.WorldPosition.y), Quaternion.identity).transform.SetParent(pair.Key.StructureParent);
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

        if (LastPlayerChunkPos != CurrentPlayerChunkPos)
        {
            CheckChunks();
            NavMeshManager.CheckNavMesh();
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

        RoundedChunkViewDistance = Mathf.FloorToInt((float)ChunkRenderDistance / Chunk.ChunkSize);
        RoundedGrassViewDistance = Mathf.FloorToInt((float)GrassRenderDistance / Chunk.ChunkSize);
        RoundedTreeViewDistance = Mathf.FloorToInt((float)TreeRenderDistance / Chunk.ChunkSize);

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

    public void CheckChunks()
    {
        LastPlayerChunkPos = CurrentPlayerChunkPos;

        foreach (KeyValuePair<Vector2Int, Chunk> pair in ActiveTerrain)
        {
            if (Vector2Int.Distance(pair.Key * Chunk.ChunkSize, LastPlayerChunkPos * Chunk.ChunkSize) >= ChunkRenderDistance)
            {
                pair.Value.Active = false;
            }
        }

        for (int i = ActiveGrass.Count - 1; i > -1; i--)
        {
            if (Vector2Int.Distance(ActiveTerrain[ActiveGrass[i]].WorldPosition, LastPlayerChunkPos * Chunk.ChunkSize) >= GrassRenderDistance)
            {
                ActiveTerrain[ActiveGrass[i]].GrassMesh.enabled = false;
                ActiveGrass.RemoveAt(i);
            }
        }

        for (int i = ActiveTrees.Count - 1; i > -1; i--)
        {
            if (Vector2Int.Distance(ActiveTerrain[ActiveTrees[i]].WorldPosition, LastPlayerChunkPos * Chunk.ChunkSize) >= TreeRenderDistance)
            {
                ActiveTerrain[ActiveTrees[i]].FoliageParent.gameObject.SetActive(false);
                ActiveTrees.RemoveAt(i);
            }
        }

        CheckForNewChunks();
    }

    private void CheckForNewChunks()
    {
        for (int x = -RoundedChunkViewDistance; x <= RoundedChunkViewDistance; x++)
        {
            for (int z = -RoundedChunkViewDistance; z <= RoundedChunkViewDistance; z++)
            {
                Vector2Int chunkToCheck = new(x + LastPlayerChunkPos.x, z + LastPlayerChunkPos.y);

                if (Vector2Int.Distance(chunkToCheck, LastPlayerChunkPos) < RoundedChunkViewDistance)
                {
                    if (!ActiveTerrain.ContainsKey(chunkToCheck))
                    {
                        ActiveTerrain.Add(chunkToCheck, new GameObject().AddComponent<Chunk>());
                        ActiveTerrain[chunkToCheck].Initialize(chunkToCheck * Chunk.ChunkSize);
                        MeshesToCreate.Enqueue(chunkToCheck);
                    }
                    else
                    {
                        ActiveTerrain[chunkToCheck].Active = true;
                    }
                }
            }
        }

        for (int x = -RoundedGrassViewDistance; x <= RoundedGrassViewDistance; x++)
        {
            for (int z = -RoundedGrassViewDistance; z <= RoundedGrassViewDistance; z++)
            {
                Vector2Int chunkToCheck = new(x + LastPlayerChunkPos.x, z + LastPlayerChunkPos.y);

                if (ActiveTerrain.ContainsKey(chunkToCheck))
                {
                    if (Vector2Int.Distance(LastPlayerChunkPos, chunkToCheck) < RoundedGrassViewDistance)
                    {
                        lock (ActiveGrass)
                        {
                            Chunk chunk = ActiveTerrain[chunkToCheck];
                            if (!ActiveGrass.Contains(chunkToCheck) && chunk.GrassReady && chunk.TerrainReady)
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

        for (int x = -RoundedTreeViewDistance; x <= RoundedTreeViewDistance; x++)
        {
            for (int z = -RoundedTreeViewDistance; z <= RoundedTreeViewDistance; z++)
            {
                Vector2Int chunkToCheck = new(x + LastPlayerChunkPos.x, z + LastPlayerChunkPos.y);

                if (ActiveTerrain.ContainsKey(chunkToCheck))
                {
                    if (Vector2Int.Distance(LastPlayerChunkPos, chunkToCheck) < RoundedTreeViewDistance)
                    {
                        lock (ActiveTrees)
                        {
                            Chunk chunk = ActiveTerrain[chunkToCheck];
                            if (!ActiveTrees.Contains(chunkToCheck) && chunk.TreeReady)
                            {
                                ActiveTerrain[chunkToCheck].FoliageParent.gameObject.SetActive(true);
                                ActiveTrees.Add(chunkToCheck);
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