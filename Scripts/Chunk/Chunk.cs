using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Chunk : MonoBehaviour
{
    public Vector2Int WorldPosition { get; private set; }
    public Vector2Int ChunkPosition { private set; get; }
    private MeshFilter _MeshFilter;
    private MeshRenderer _MeshRenderer;
    private MeshCollider _MeshCollider;

    public Transform FoliageParent;
    public Transform StructureParent;
    public GrassChunk GrassMesh;

    public static int[] Triangles;
    public const int ChunkSize = 60;

    public bool TerrainReady;
    public bool HasTrees;
    public bool GrassReady;
    public bool Active
    {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    [Header("Collections")]
    public List<FoliageInfoToMove> Foliages;
    public Vector3[] Vertices;
    private Color[] Colors;
    private Vector2[] UVs;
    public int[] BiomeNums;

    public void Initialize(Vector2Int worldPos) 
    {
        WorldPosition = worldPos;
        ChunkPosition = worldPos / ChunkSize;
        Active = false;
        TerrainReady = false;
        HasTrees = false;

        transform.position = new(worldPos.x, 0, worldPos.y);
        gameObject.transform.SetParent(World.WorldTransform);
        gameObject.name = "Chunk: " + (worldPos.x / ChunkSize) + ", " + (worldPos.y / ChunkSize);
        gameObject.layer = LayerMask.NameToLayer("Terrain");
        gameObject.isStatic = true;

        _MeshFilter = gameObject.AddComponent<MeshFilter>();
        _MeshRenderer = gameObject.AddComponent<MeshRenderer>();
        _MeshCollider = gameObject.AddComponent<MeshCollider>();

        _MeshRenderer.materials = new Material[1] { World.Instance.MeshMaterial };

        if (RenderTerrainMap.Instance)
        {
            RenderTerrainMap.Instance.Renderers.Add(_MeshRenderer);
        }

        Foliages = new();
        FoliageParent = new GameObject("Foliage").transform;
        FoliageParent.SetParent(transform);

        for (int i = 0; i < World.Biomes.Length; i++)
        {
            Transform newBiomeParent = new GameObject().transform;
            newBiomeParent.SetParent(FoliageParent);

            for (int j = 0; j < World.Biomes[i]._FoliageSettings.FoliageInfos.Length; j++)
            {
                Transform newFoliageParent = new GameObject().transform;
                newFoliageParent.SetParent(newBiomeParent);
            }
        }

        GrassMesh = new GameObject("Grass").AddComponent<GrassChunk>();
        GrassMesh.transform.SetParent(transform);
        GrassMesh.transform.localPosition = Vector3.zero;
        GrassMesh.ParentChunk = this;

        StructureParent = new GameObject("Structures").transform;
        StructureParent.transform.SetParent(transform);
        StructureParent.transform.localPosition = Vector3.zero;
    }

    public const float BaseOffset = 10000 - (ChunkSize / 2) + 0.1f;
    public static float GetPerlinNoise(float x, float y, PerlinData perlinData)
    {
        return Mathf.Abs(Mathf.PerlinNoise((x + perlinData.Offset.x + BaseOffset) / ChunkSize * perlinData.PerlinScale, (y + perlinData.Offset.y + BaseOffset) / ChunkSize * perlinData.PerlinScale));
    }

    public void CreateMesh()
    {
        Vertices = new Vector3[(ChunkSize + 1) * (ChunkSize + 1)];
        Colors = new Color[Vertices.Length];
        UVs = new Vector2[Vertices.Length];
        BiomeNums = new int[Vertices.Length];
        System.Random random = new();

        for (int i = 0, z = 0; z <= ChunkSize; z++)
        {
            for (int x = 0; x <= ChunkSize; x++)
            {
                float heightNoise = GetPerlinNoise(x + WorldPosition.x, z + WorldPosition.y, World.CurrentSaveData.HeightPerlin);
                float temperatureNoise = GetPerlinNoise(x + WorldPosition.x, z + WorldPosition.y, World.CurrentSaveData.TemperaturePerlin);
                TerrainGradient.Evaluate(heightNoise, temperatureNoise, BiomeNums, Colors, i);

                Vertices[i] = new(x - (ChunkSize / 2), heightNoise * SaveData.HeightMultipler, z - (ChunkSize / 2));
                UVs[i] = new((float)x / ChunkSize, (float)z / ChunkSize);

                //Foliage
                FoliageSettings foliage = World.Biomes[BiomeNums[i]]._FoliageSettings;
                if (foliage.ChanceOfTree > 0)
                {
                    if ((z + 1) % foliage.SquareCheckSize == 0 && (x + 1) % foliage.SquareCheckSize == 0)
                    {
                        if (GetPerlinNoise(x, z, foliage._PerlinData) > foliage.PlacementThreshold)
                        {
                            if (World.Biomes[BiomeNums[i]]._FoliageSettings.ChanceOfTree > random.NextDouble())
                            {
                                float whichFoliage = (float)random.NextDouble() * FoliageManager.FoliageThresholds[BiomeNums[i]][^1];

                                for (int foliageNum = 0; foliageNum < FoliageManager.FoliageThresholds[BiomeNums[i]].Length; foliageNum++)
                                {
                                    if (FoliageManager.FoliageThresholds[BiomeNums[i]][foliageNum] > whichFoliage)
                                    {
                                        int randX = Mathf.FloorToInt((float)random.NextDouble() * foliage.SquareCheckSize);
                                        int randZ = Mathf.FloorToInt((float)random.NextDouble() * foliage.SquareCheckSize);

                                        Foliages.Add(new(BiomeNums[i], foliageNum, Vertices[Mathf.Max(i - randX - (randZ * (ChunkSize - 1)), 0)] + new Vector3(WorldPosition.x, 0, WorldPosition.y), this));

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                i++;
            }
        }

        GrassMesh.Initialize();

        lock (World.MeshesToUpdate)
        {
            World.MeshesToUpdate.Enqueue(ChunkPosition);
        }

        if (StructureCreator.VillageChunks.Contains(ChunkPosition))
        {
            lock (World.StructuresToCreate)
            {
                World.StructuresToCreate.Add(this, StructureTypes.Village);
            }
        }
        else if (StructureCreator.MobDensToCreate.Contains(ChunkPosition))
        {
            lock (World.StructuresToCreate)
            {
                World.StructuresToCreate.Add(this, StructureTypes.MobDen);
            }
        }

        if (Foliages.Count > 0)
        {
            lock (FoliageManager.FoliageToAdd)
            {
                FoliageManager.FoliageToAdd.Enqueue(this);
            }
        }
    }

    public void AssignMesh()
    {
        Mesh mesh = new();

        mesh.Clear();

        mesh.vertices = Vertices;
        mesh.triangles = Triangles;
        mesh.colors = Colors;
        mesh.uv = UVs;

        mesh.RecalculateNormals();

        _MeshFilter.mesh = mesh;
        _MeshCollider.sharedMesh = mesh;

        TerrainReady = true;
    }

    public class FoliageInfoToMove
    {
        public Transform Foliage;
        public int BiomeNum;
        public int FoliageNum;
        public Vector3 Position;
        public Chunk ParentChunk;

        public FoliageInfoToMove(int biomeNum, int foliageNum, Vector3 position, Chunk chunk)
        {
            Foliage = null;
            BiomeNum = biomeNum;
            FoliageNum = foliageNum;
            ParentChunk = chunk;
            Position = position;
        }
    }
}