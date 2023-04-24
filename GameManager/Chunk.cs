using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;

using static GameManager;
using static PerlinData;
using static FoliageManager;
using static TerrainColorGradient;

public class Chunk : MonoBehaviour
{
    public static Dictionary<Vector2Int, List<MonoBehaviour>> WaitingForTerrain;
    public static int ChunkSize { get { return CurrentSaveData.MyPerlinData.ChunkSize; } }
    public static int Seed { get { return CurrentSaveData.MyPerlinData.Seed; } }
    public static int[] Triangles { get{ return CurrentSaveData.MyPerlinData.Triangles; } }

    public Vector2Int WorldPosition;
    public Vector2Int ChunkPosition;

    [SerializeField] private MeshFilter ChunkMeshFilter;
    [SerializeField] private MeshRenderer _MeshRenderer;
    [SerializeField] private MeshCollider _MeshCollider;

    public Transform MyTransform;
    public GameObject MyGameObject;

    public bool HasTerrain { get; private set; }
    public bool HasStructures { get; private set;}

    private Mesh MyMesh;
    private int MyMeshID;

    private void Awake()
    {
        HasStructures = false;
    }

    public void SetPositions(Vector2Int chunkPos)
    {
        ChunkPosition = chunkPos;
        WorldPosition = chunkPos * ChunkSize;
        HasTerrain = false;
        HasStructures = false;
        MyGameObject.name = "Chunk: " + (ChunkPosition.x) + ", " + (ChunkPosition.y);
    }

    public static Vector2Int GetChunkPosition(float x, float y) { return new(Mathf.RoundToInt(x / ChunkSize), Mathf.RoundToInt(y / ChunkSize)); }

    public static Vector2Int GetChunkPosition(Vector3 position) { return GetChunkPosition(position.x, position.z); }

    public static Vector3[] GetNormals(Vector3[] vertices)
    {
        Vector3[] normals = new Vector3[vertices.Length];
        int triCount = Triangles.Length / 3;
        for (int j = 0; j < triCount; j++)
        {
            int normalTriangeIndex = j * 3;
            int vertexIndexA = Triangles[normalTriangeIndex];
            int vertexIndexB = Triangles[normalTriangeIndex + 1];
            int vertexIndexC = Triangles[normalTriangeIndex + 2];

            Vector3 triangleNormal = Vector3.Cross(vertices[vertexIndexB] - vertices[vertexIndexA], vertices[vertexIndexC] - vertices[vertexIndexA]).normalized;

            normals[vertexIndexA] += triangleNormal;
            normals[vertexIndexB] += triangleNormal;
            normals[vertexIndexC] += triangleNormal;
        }

        foreach (Vector3 normal in normals) normal.Normalize();

        return normals;
    }

    public readonly struct PrepareChunk : IJob
    {
        public readonly Vector2Int ChunkPosition;
        public readonly Vector2Int WorldPosition;

        public PrepareChunk(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            WorldPosition = chunkPosition * ChunkSize;
        }

        public void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition))
            {
                lock (CurrentSaveData.Structures) if (CurrentSaveData.Structures.ContainsKey(ChunkPosition)) new StructureCreator.ScheduleVillageCreation(ChunkPosition).Schedule();

                if (ActiveTerrain[ChunkPosition])
                {
                    ChunkData chunkData = SaveHandler.GetChunkData(ChunkPosition);

                    while (!HeightMap.ContainsKey(ChunkPosition)) { HeightMap.TryAdd(ChunkPosition, chunkData.HeightData); }
                    while (!TemperatureMap.ContainsKey(ChunkPosition)) { TemperatureMap.TryAdd(ChunkPosition, chunkData.TemperatureData); }

                    Vector3[] vertices = new Vector3[HeightMap[ChunkPosition].Length];
                    Color[] colors = new Color[HeightMap[ChunkPosition].Length];
                    Vector2[] uvs = new Vector2[HeightMap[ChunkPosition].Length];

                    for (int i = 0, y = 0; y <= ChunkSize; y++)
                    {
                        for (int x = 0; x <= ChunkSize; x++)
                        {
                            colors[i] = GetTerrainColor(HeightMap[ChunkPosition][i], TemperatureMap[ChunkPosition][i]);
                            vertices[i] = new(x - (ChunkSize / 2), (HeightMap[ChunkPosition][i] * HeightMultipler), y - (ChunkSize / 2));
                            uvs[i] = new((float)x / ChunkSize, (float)y / ChunkSize);

                            i++;
                        }
                    }

                    if (ActiveTerrain.ContainsKey(ChunkPosition))
                    {
                        if (ActiveTerrain[ChunkPosition])
                        {
                            Vector3[] normals = GetNormals(vertices);
                            new CreateChunkMesh(ActiveTerrain[ChunkPosition], vertices, colors, uvs, normals).Schedule();
                            PrepareFoliage(normals);
                        }
                    }
                }
            }
        }

        public void PrepareFoliage(Vector3[] normals)
        {
            System.Random random = new(Mathf.Abs((WorldPosition.x + WorldPosition.y) * Seed));
            List<FoliageInfoToMove> foliages = new();

            for (int y = 0; y < ChunkSize - FoliageSquareCheckSize; y += FoliageSquareCheckSize)
            {
                for (int x = 0; x < ChunkSize - FoliageSquareCheckSize; x += FoliageSquareCheckSize)
                {
                    int randX = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + x;
                    int randZ = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + y;

                    int index = x + (y * ChunkSize);

                    BiomeData biome = GetBiomeData(HeightMap[ChunkPosition][index], TemperatureMap[ChunkPosition][index]);

                    if (biome._FoliageSettings.ChanceOfTree > random.NextDouble())
                    {
                        float whichFoliage = (float)random.NextDouble() * FoliageThresholds[GetBiomeNum(biome)][^1];

                        for (int foliageNum = 0; foliageNum < FoliageThresholds[GetBiomeNum(biome)].Length; foliageNum++)
                        {
                            if (FoliageThresholds[GetBiomeNum(biome)][foliageNum] > whichFoliage)
                            {
                                Vector3 newRot = Quaternion.FromToRotation(Vector3.up, normals[randX + (ChunkSize * randZ)]).eulerAngles + new Vector3(0, (float)random.NextDouble(), 0);
                                foliages.Add(new(GetBiomeNum(biome), foliageNum, GetPerlinPosition(GetVertexPosition(new(randX, randZ), WorldPosition)), Quaternion.Euler(newRot)));

                                break;
                            }
                        }
                    }
                }
            }

            AddFoliage(WorldPosition / ChunkSize, foliages);
        }
    }

    public class CreateChunkMesh : MainThreadJob
    {
        private readonly Chunk MyChunk;
        private readonly Vector3[] Vertices;
        private readonly Color[] Colors;
        private readonly Vector2[] UVs;
        private readonly Vector3[] Normals;

        public CreateChunkMesh(Chunk chunk, Vector3[] vertices, Color[] colors, Vector2[] uvs, Vector3[] normals)
        {
            MyChunk = chunk;
            Vertices = vertices;
            Colors = colors;
            UVs = uvs;
            Normals = normals;
        }

        public override void Execute()
        {
            if (MyChunk.ChunkMeshFilter)
            {
                MyChunk.MyMesh = new();
                MyChunk.MyMesh.SetVertices(Vertices);
                MyChunk.MyMesh.SetTriangles(MyPerlinData.Triangles, 0);
                MyChunk.MyMesh.SetColors(Colors);
                MyChunk.MyMesh.SetUVs(0, UVs);
                MyChunk.MyMesh.SetNormals(Normals);

                MyChunk.MyMeshID = MyChunk.MyMesh.GetInstanceID();
                new BakePhysicsJob(MyChunk.ChunkPosition, MyChunk.MyMesh.GetInstanceID()).Schedule();
            }
        }
    }

    private readonly struct BakePhysicsJob : IJob
    {
        private readonly Vector2Int ChunkPos;
        private readonly int MeshID;

        public BakePhysicsJob(Vector2Int chunkPos, int meshID)
        {
            ChunkPos = chunkPos;
            MeshID = meshID;
        }

        public void Execute()
        {
            try
            {
                if (ActiveTerrain.ContainsKey(ChunkPos))
                {
                    if (MeshID == ActiveTerrain[ChunkPos].MyMeshID)
                    {
                        Physics.BakeMesh(MeshID, false);
                        new ApplyMeshJob(ActiveTerrain[ChunkPos], MeshID).Schedule();
                    }
                }
            }
            catch (NullReferenceException) { }
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
            if (MyChunk.MyMeshID == MyMeshID)
            {
                if (MyChunk.ChunkMeshFilter)
                {
                    MyChunk.ChunkMeshFilter.sharedMesh = MyChunk.MyMesh;
                    MyChunk._MeshCollider.sharedMesh = MyChunk.MyMesh;
                    MyChunk.HasTerrain = true;

                    if (WaitingForTerrain.ContainsKey(MyChunk.ChunkPosition))
                    {
                        foreach (MonoBehaviour mono in WaitingForTerrain[MyChunk.ChunkPosition])
                        {
                            if (mono != null) new ToEnableMonobehaivourJob(mono).Schedule();
                        }
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ChunkMeshFilter = GetComponent<MeshFilter>(); 
    }
#endif
}

[Serializable]
public struct FoliageInfoToMove
{
    public Quaternion Rotation { get; private set; }
    public GameObject Prefab { get; private set; }
    public Vector3 Position { get; private set; }

    public FoliageInfoToMove(int biomeNum, int foliageNum, Vector3 position, Quaternion rotation)
    {
        Prefab = TerrainColorGradient.BiomeDatas[biomeNum]._FoliageSettings.FoliageInfos[foliageNum].Prefab;
        Position = position;
        Rotation = rotation;
    }
}