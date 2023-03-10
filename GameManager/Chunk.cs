using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine.Rendering;

public enum TerrainLODStates
{
    Empty = 0,
    LODOne = 1,
    LODTwo = 2,
    LODThree = 3
}

public class Chunk : MonoBehaviour
{
    private const float HeightPerlinScale = 0.025f;
    private const float TemperaturePerlinScale = 0.05f;
    public const float HeightMultipler = 100;
    private const float BaseOffset = 100000 + 0.1f;

    private static Vector2 HeightPerlinOffset;
    private static Vector2 TemperaturePerlinOffset;

    public static void InitializeStatics()
    {
        HeightPerlinOffset = new(World.CurrentSaveData.Seed + BaseOffset, World.CurrentSaveData.Seed + BaseOffset);
        TemperaturePerlinOffset = new(World.CurrentSaveData.Seed + 10000 + BaseOffset, World.CurrentSaveData.Seed + 10000 + BaseOffset);
        OctaveOffsets = new Vector2[NumOctaves];
        System.Random prng = new(World.CurrentSaveData.Seed);
        for (int i = 0; i < NumOctaves; i++)
        {
            OctaveOffsets[i].x = prng.Next(100000) + HeightPerlinOffset.x;
            OctaveOffsets[i].y = prng.Next(100000) + HeightPerlinOffset.y;
        }
    }

    private static int Seed { get { return World.CurrentSaveData.Seed; } }
    public const int DefaultChunkSize = 60;
    public const int FoliageSquareCheckSize = 5;

    public Vector2Int WorldPosition;
    public Vector2Int ChunkPosition;

    public MeshFilter ChunkMeshFilter { get; private set; }
    [SerializeField] private MeshRenderer _MeshRenderer;
    [SerializeField] private MeshCollider _MeshCollider;
    public TerrainLODStates CurrentLODState { get; private set; }
    public Transform MyTransform;
    public GameObject MyGameObject;

    public bool HasTerrain { get; private set; }
    private bool HasStructures = false;
    public bool HasTrees { get { return FoliageManager.HasTrees(ChunkPosition); } }

    private Vector3[] Vertices;
    private static Dictionary<TerrainLODStates, int[]> Triangles;
    private Color32[] Colors;
    private Vector2[] UVs;
    private Vector3[] Normals;

    private Mesh MyMesh;
    private int MyMeshID;

    private void Awake()
    {
        ChunkMeshFilter = GetComponent<MeshFilter>();
    }

    public void SetPositions(Vector2Int chunkPos)
    {
        ChunkPosition = chunkPos;
        WorldPosition = chunkPos * DefaultChunkSize;
        HasTerrain = false;
        CurrentLODState = TerrainLODStates.Empty;
        MyGameObject.name = "Chunk: " + (ChunkPosition.x) + ", " + (ChunkPosition.y);
    }

    private const int NumOctaves = 4;
    private static Vector2[] OctaveOffsets;
    private static float GetHeightPerlinValue(Vector2 pos)
    {
        return GetHeightPerlinValue(pos.x, pos.y);
    }
    public static float GetHeightPerlinValue(float x, float y)
    {
        float amplitude = 1;
        float frequency = 1;
        float perlinNoise = 0;

        for (int i = 0; i < NumOctaves; i++)
        {
            float sampleX = (x + OctaveOffsets[i].x) / DefaultChunkSize * HeightPerlinScale * frequency;
            float sampleY = (y + OctaveOffsets[i].y) / DefaultChunkSize * HeightPerlinScale * frequency;
            perlinNoise += (Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1) * amplitude;

            amplitude *= 0.5f;
            frequency *= 2;
        }

        return Mathf.Abs(perlinNoise);
    }
    private const float DirtScale = 5;
    public static float GetDirtPerlin(float x, float y)
    {
        return Mathf.PerlinNoise((x + OctaveOffsets[0].x) / DefaultChunkSize * DirtScale, (y + OctaveOffsets[0].y) / DefaultChunkSize * DirtScale);
    }

    private static float GetTemperaturePerlin(Vector2Int pos) { return GetTemperaturePerlin(pos.x, pos.y); }
    public static float GetTemperaturePerlin(float x, float y)
    {
        return Mathf.Abs(Mathf.PerlinNoise((x + TemperaturePerlinOffset.x) / DefaultChunkSize * TemperaturePerlinScale, (y + TemperaturePerlinOffset.y) / DefaultChunkSize * TemperaturePerlinScale));
    }

    public static Vector3 GetPerlinPosition(Vector2 position)
    {
        return GetPerlinPosition(position.x, position.y);
    }

    public static Vector3 GetPerlinPosition(float x, float y)
    {
        return new(x, (GetHeightPerlinValue(x, y) * HeightMultipler) + (GetDirtPerlin(x, y) * 2), y);
    }

    public static Vector2Int GetChunkPosition(float x, float y)
    {
        return new(Mathf.RoundToInt(x / DefaultChunkSize), Mathf.RoundToInt(y / DefaultChunkSize));
    }

    public static Vector2Int GetChunkPosition(Vector3 position)
    {
        return GetChunkPosition(position.x, position.z);
    }

    private void AssignVerts(int lodDetail)
    {
        Vertices = new Vector3[((DefaultChunkSize / lodDetail) + 1) * ((DefaultChunkSize / lodDetail) + 1)];
        UVs = new Vector2[Vertices.Length];
        Normals = new Vector3[Vertices.Length];
        Colors = new Color32[Vertices.Length];

        for (int i = 0, y = 0; y <= DefaultChunkSize; y += lodDetail)
        {
            for (int x = 0; x <= DefaultChunkSize; x += lodDetail)
            {
                Vector2Int pos = GetVertexPosition(x, y);
                float heightNoise = GetHeightPerlinValue(pos.x, pos.y);
                float temperatureNoise = GetTemperaturePerlin(pos.x, pos.y);
                float dirtNoise = GetDirtPerlin(pos.x, pos.y);

                Colors[i] = TerrainGradient.GetTerrainColor(heightNoise, temperatureNoise, dirtNoise);

                Vertices[i] = new(x - (DefaultChunkSize / 2), (heightNoise * HeightMultipler) + (dirtNoise * 2), y - (DefaultChunkSize / 2));
                UVs[i] = new((float)x / DefaultChunkSize, (float)y / DefaultChunkSize);
                i++;
            }
        }

        CalculateNormals();
        World.MeshesToAssign.Enqueue(this);
    }

    private void CalculateNormals()
    {
        int triCount = Triangles[CurrentLODState].Length / 3;
        for (int j = 0; j < triCount; j++)
        {
            int normalTriangeIndex = j * 3;
            int vertexIndexA = Triangles[CurrentLODState][normalTriangeIndex];
            int vertexIndexB = Triangles[CurrentLODState][normalTriangeIndex + 1];
            int vertexIndexC = Triangles[CurrentLODState][normalTriangeIndex + 2];

            Vector3 triangleNormal = Vector3.Cross(Vertices[vertexIndexB] - Vertices[vertexIndexA], Vertices[vertexIndexC] - Vertices[vertexIndexA]).normalized;

            Normals[vertexIndexA] += triangleNormal;
            Normals[vertexIndexB] += triangleNormal;
            Normals[vertexIndexC] += triangleNormal;
        }

        for (int j = 0; j < Normals.Length; j++)
        {
            Normals[j].Normalize();
        }
    }

    public void InitializeLODOne()
    {
        if (CurrentLODState != TerrainLODStates.LODOne)
        {
            HasTerrain = false;
            CurrentLODState = TerrainLODStates.LODOne;
            AssignVerts((int)TerrainLODStates.LODOne);
            CheckForBuildings();
            AssignTreeData();
        }
    }

    public void InitializeLODTwo()
    {
        if (CurrentLODState != TerrainLODStates.LODTwo)
        {
            HasTerrain = false;
            CurrentLODState = TerrainLODStates.LODTwo;
            AssignVerts((int)TerrainLODStates.LODTwo);
            CheckForBuildings();
        }
    }

    public void InitializeLODThree()
    {
        if (CurrentLODState != TerrainLODStates.LODThree)
        {
            HasTerrain = false;
            CurrentLODState = TerrainLODStates.LODThree;
            AssignVerts((int)TerrainLODStates.LODThree);
        }
    }

    private void CheckForBuildings()
    {
        if (!HasStructures)
        {
            HasStructures = true;
            if (StructureCreator.VillageChunks.Contains(ChunkPosition))
            {
                new StructureCreator.PrepareVillage(ChunkPosition, WorldPosition).Schedule();
            }
            else if (StructureCreator.MobDenPositions.Contains(ChunkPosition))
            {
                StructureCreator.MobDensToCreate.Enqueue(ChunkPosition);
            }
        }
    }

    private Vector2Int GetVertexPosition(int x, int y)
    {
        return new Vector2Int(x + WorldPosition.x - (DefaultChunkSize / 2), y + WorldPosition.y - (DefaultChunkSize / 2));
    }

    public void AssignTreeData()
    {
        if (!HasTrees)
        {
            System.Random random = new(Mathf.Abs(WorldPosition.x * WorldPosition.y));
            Queue<FoliageInfoToMove> foliages = new();

            for (int y = 0; y < DefaultChunkSize - FoliageSquareCheckSize; y += FoliageSquareCheckSize)
            {
                for (int x = 0; x < DefaultChunkSize - FoliageSquareCheckSize; x += FoliageSquareCheckSize)
                {
                    BiomeData biome = TerrainGradient.GetBiomeData(GetHeightPerlinValue(GetVertexPosition(x, y)), GetTemperaturePerlin(GetVertexPosition(x, y)));

                    if (biome._FoliageSettings.ChanceOfTree > random.NextDouble())
                    {
                        int randX = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + x;
                        int randZ = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + y;

                        float whichFoliage = (float)random.NextDouble() * FoliageManager.FoliageThresholds[TerrainGradient.GetBiomeNum(biome)][^1];

                        for (int foliageNum = 0; foliageNum < FoliageManager.FoliageThresholds[TerrainGradient.GetBiomeNum(biome)].Length; foliageNum++)
                        {
                            if (FoliageManager.FoliageThresholds[TerrainGradient.GetBiomeNum(biome)][foliageNum] > whichFoliage)
                            {
                                Vector3 newRot = Quaternion.FromToRotation(Vector3.up, Normals[randX + (DefaultChunkSize * randZ)]).eulerAngles + new Vector3(0, (float)random.NextDouble(), 0);
                                foliages.Enqueue(new(TerrainGradient.GetBiomeNum(biome), foliageNum, GetPerlinPosition(GetVertexPosition(randX, randZ)), Quaternion.Euler(newRot)));

                                break;
                            }
                        }
                    }
                }
            }

            FoliageManager.AddFoliage(ChunkPosition, foliages);
        }
    }

    public void CreateMesh()
    {
        if (ChunkMeshFilter)
        {
            MyMesh = new();

            MyMesh.SetVertices(Vertices);
            MyMesh.SetTriangles(Triangles[CurrentLODState], 0);
            MyMesh.SetUVs(0, UVs);
            MyMesh.SetNormals(Normals);
            MyMesh.SetColors(Colors);

            MyMeshID = MyMesh.GetInstanceID();
            new BakePhysicsJob(this, MyMesh.GetInstanceID()).Schedule();
        }
    }

    private void ApplyMesh()
    {
        ChunkMeshFilter.sharedMesh = MyMesh;
        _MeshCollider.sharedMesh = MyMesh;
        HasTerrain = true;
    }

    public class BakePhysicsJob : SecondaryThreadJob
    {
        private readonly Chunk MyChunk;
        private readonly int MeshID;

        public BakePhysicsJob(Chunk chunk, int meshID)
        {
            MyChunk = chunk;
            MeshID = meshID;
        }

        public override void Execute()
        {
            if (MeshID == MyChunk.MyMeshID)
            {
                Physics.BakeMesh(MeshID, false);
                new ApplyMeshJob(MyChunk, MeshID).Schedule();
            }
        }
    }

    public class ApplyMeshJob : MainThreadJob
    {
        private readonly Chunk MyChunk;
        private readonly int MyMeshID;

        public ApplyMeshJob(Chunk chunk, int meshID)
        {
            MyChunk = chunk;
            MyMeshID = meshID;
        }

        public override void Execute()
        {
            if (MyChunk.MyMeshID == MyMeshID) MyChunk.ApplyMesh();
        }
    }

    public static void InitializeTriangles()
    {
        Triangles = new();
        AssignTriangles(TerrainLODStates.LODThree, (int)TerrainLODStates.LODThree);
        AssignTriangles(TerrainLODStates.LODTwo, (int)TerrainLODStates.LODTwo);
        AssignTriangles(TerrainLODStates.LODOne, (int)TerrainLODStates.LODOne);
    }

    private static void AssignTriangles(TerrainLODStates lodState, float lodDetail)
    {
        int num = Mathf.CeilToInt(DefaultChunkSize / lodDetail);

        Triangles.Add(lodState, new int[num * num * 6]);
        int tris = 0, vertexIndex = 0;
        for (int y = 0; y < num; y++)
        {
            for (int x = 0; x < num; x++)
            {
                Triangles[lodState][tris] = vertexIndex;
                Triangles[lodState][tris + 1] = vertexIndex + num + 1;
                Triangles[lodState][tris + 2] = vertexIndex + 1;
                Triangles[lodState][tris + 3] = vertexIndex + 1;
                Triangles[lodState][tris + 4] = vertexIndex + num + 1;
                Triangles[lodState][tris + 5] = vertexIndex + num + 2;

                vertexIndex++;
                tris += 6;
            }

            vertexIndex++;
        }
    }
}

[System.Serializable]
public struct FoliageInfoToMove
{
    public Quaternion Rotation;
    public GameObject Prefab;
    public Vector3 Position;

    public FoliageInfoToMove(int biomeNum, int foliageNum, Vector3 position, Quaternion rotation)
    {
        Prefab = TerrainGradient.BiomeDatas[biomeNum]._FoliageSettings.FoliageInfos[foliageNum].Prefab;
        Position = position;
        Rotation = rotation;
    }
}