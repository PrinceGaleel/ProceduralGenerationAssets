using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum TerrainLODStates
{
    Empty = 0,
    LODOne = 1,
    LODTwo = 6,
    LODThree = 12,
    LODFour = 20,
    LODFive = 30
}

public class Chunk : MonoBehaviour
{
    public const int DefaultChunkSize = 60;
    public const int FoliageSquareCheckSize = 5;

    public Vector2Int WorldPosition;
    public Vector2Int ChunkPosition;

    public MeshFilter ChunkMeshFilter;
    [SerializeField] private MeshCollider _MeshCollider;
    public TerrainLODStates CurrentLODState;

    public Transform MyTransform;
    public GameObject MyGameObject;

    public bool HasTrees = false;
    private bool StructuresReady = false;

    private Vector3[] Vertices;
    private static Dictionary<TerrainLODStates, int[]> Triangles;
    private Color[] Colors;
    private Vector2[] UVs;

    public Mesh TerrainMesh { get; private set; }
    public static PerlinData HeightPerlin { get { return World.CurrentSaveData.HeightPerlin; } }
    public static PerlinData TemperaturePerlin { get { return World.CurrentSaveData.TemperaturePerlin; } }

    public void SetPositions(Vector2Int chunkPos)
    {
        ChunkPosition = chunkPos;
        WorldPosition = chunkPos * DefaultChunkSize;
        TerrainMesh = null;
    }

    public void MoveTransform()
    {
        MyTransform.position = new(WorldPosition.x, 0, WorldPosition.y);
        MyGameObject.name = "Chunk: " + (ChunkPosition.x) + ", " + (ChunkPosition.y);
    }

    private const float BaseOffset = 1000000 + 0.1f;
    private static Vector2Serializable FullHeightOffset { get { return HeightPerlin.Offset + BaseOffset; } }
    public static float GetHeightPerlin(float x, float y)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < 4; i++)
        {
            float sampleX = (x + FullHeightOffset.x) / DefaultChunkSize * HeightPerlin.PerlinScale * frequency;
            float sampleY = (y + FullHeightOffset.y) / DefaultChunkSize * HeightPerlin.PerlinScale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= 0.5f;
            frequency *= 2;
        }

        return Mathf.Abs(noiseHeight); 
    }

    private static Vector2Serializable FullTemperatureOffset { get { return TemperaturePerlin.Offset + BaseOffset; } }
    public static float GetTemperaturePerlin(float x, float y)
    {
        return Mathf.Abs(Mathf.PerlinNoise((x + FullTemperatureOffset.x) / DefaultChunkSize * TemperaturePerlin.PerlinScale, (y + FullTemperatureOffset.y) / DefaultChunkSize * TemperaturePerlin.PerlinScale));
    }

    public static Vector3 GetPerlinPosition(Vector2 position)
    {
        return new(position.x, GetHeightPerlin(position.x, position.y) * SaveData.HeightMultipler, position.y);
    }

    public static Vector3 GetPerlinPosition(float x, float y)
    {
        return new(x, GetHeightPerlin(x, y) * SaveData.HeightMultipler, y);
    }

    public static Vector2Int GetChunkPosition(float x, float y)
    {
        return new(Mathf.RoundToInt(x / DefaultChunkSize), Mathf.RoundToInt(y / DefaultChunkSize));
    }

    private void AssignVerts(int lodNum)
    {
        Vertices = new Vector3[((DefaultChunkSize / lodNum) + 1) * ((DefaultChunkSize / lodNum) + 1)];
        UVs = new Vector2[Vertices.Length];
        Colors = new Color[Vertices.Length];

        for (int i = 0, z = 0; z <= DefaultChunkSize; z += lodNum)
        {
            for (int x = 0; x <= DefaultChunkSize; x += lodNum)
            {
                float heightNoise = GetHeightPerlin(x + WorldPosition.x - (DefaultChunkSize / 2), z + WorldPosition.y - (DefaultChunkSize / 2));
                Colors[i] = TerrainGradient.GetBiomeColor(heightNoise, GetTemperaturePerlin(x + WorldPosition.x, z + WorldPosition.y));
                Vertices[i] = new(x - (DefaultChunkSize / 2), heightNoise * SaveData.HeightMultipler, z - (DefaultChunkSize / 2));
                UVs[i] = new((float)x / DefaultChunkSize, (float)z / DefaultChunkSize);
                i++;
            }
        }
    }

    public void AssignLODOne()
    {
        if (CurrentLODState != TerrainLODStates.LODOne)
        {
            CurrentLODState = TerrainLODStates.LODOne;
            AssignVerts((int)TerrainLODStates.LODOne);
            CheckForBuildings();
            AssignTreeData();
        }
    }

    public void AssignLODTwo()
    {
        if (CurrentLODState != TerrainLODStates.LODTwo)
        {
            CurrentLODState = TerrainLODStates.LODTwo;
            AssignVerts((int)TerrainLODStates.LODTwo);
            CheckForBuildings();
            AssignTreeData();
        }
    }

    public void AssignLODThree()
    {
        if (CurrentLODState != TerrainLODStates.LODThree)
        {
            CurrentLODState = TerrainLODStates.LODThree;
            AssignVerts((int)TerrainLODStates.LODThree);
        }
    }

    public void AssignLODFour()
    {
        if (CurrentLODState != TerrainLODStates.LODFour)
        {
            CurrentLODState = TerrainLODStates.LODFour;
            AssignVerts((int)TerrainLODStates.LODFour);
        }
    }

    public void AssignLODFive()
    {
        if (CurrentLODState != TerrainLODStates.LODFive)
        {
            CurrentLODState = TerrainLODStates.LODFive;
            AssignVerts((int)TerrainLODStates.LODFive);
        }
    }

    private void CheckForBuildings()
    {
        if (!StructuresReady)
        {
            StructuresReady = true;
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
        }
    }

    public void AssignTreeData()
    {
        if (!FoliageManager.HasTrees(ChunkPosition))
        {
            System.Random random = new(Mathf.Abs(WorldPosition.x * WorldPosition.y));
            List<FoliageInfoToMove> foliages = new();

            for (int z = 0; z < DefaultChunkSize - 5; z += FoliageSquareCheckSize)
            {
                for (int x = 0; x < DefaultChunkSize - 5; x += FoliageSquareCheckSize)
                {
                    int biomeNum = TerrainGradient.GetBiomeNum(GetHeightPerlin(x + WorldPosition.x, z + WorldPosition.y),
                         GetTemperaturePerlin(x + WorldPosition.x, z + WorldPosition.y));

                    if (World.Biomes[biomeNum]._FoliageSettings.ChanceOfTree > random.NextDouble())
                    {
                        int randX = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + x + WorldPosition.x - (DefaultChunkSize / 2);
                        int randZ = Mathf.FloorToInt((float)random.NextDouble() * FoliageSquareCheckSize) + z + WorldPosition.y - (DefaultChunkSize / 2);

                        float whichFoliage = (float)random.NextDouble() * FoliageManager.FoliageThresholds[biomeNum][^1];

                        for (int foliageNum = 0; foliageNum < FoliageManager.FoliageThresholds[biomeNum].Length; foliageNum++)
                        {
                            if (FoliageManager.FoliageThresholds[biomeNum][foliageNum] > whichFoliage)
                            {
                                foliages.Add(new(biomeNum, foliageNum, GetPerlinPosition(randX, randZ), (float)random.NextDouble(), (float)random.NextDouble()));

                                break;
                            }
                        }
                    }
                }
            }

            FoliageManager.AddFoliage(ChunkPosition, foliages);
        }
    }

    public void AssignMesh()
    {
        Mesh mesh = new();

        mesh.vertices = Vertices;
        mesh.triangles = Triangles[CurrentLODState];
        mesh.colors = Colors;
        mesh.uv = UVs;

        mesh.RecalculateNormals();

        ChunkMeshFilter.sharedMesh = mesh;
        _MeshCollider.sharedMesh = mesh;
        TerrainMesh = mesh;
    }

    public static void InitializeTriangles()
    {
        Triangles = new();
        AssignTriangles(TerrainLODStates.LODFive, (int)TerrainLODStates.LODFive);
        AssignTriangles(TerrainLODStates.LODFour, (int)TerrainLODStates.LODFour);
        AssignTriangles(TerrainLODStates.LODThree, (int)TerrainLODStates.LODThree);
        AssignTriangles(TerrainLODStates.LODTwo, (int)TerrainLODStates.LODTwo);
        AssignTriangles(TerrainLODStates.LODOne, (int)TerrainLODStates.LODOne);
    }

    private static void AssignTriangles(TerrainLODStates lodState, int lodNum)
    {
        Triangles.Add(lodState, new int[(DefaultChunkSize / lodNum) * (DefaultChunkSize / lodNum) * 6]);
        for (int y = 0, tris = 0, vertexIndex = 0; y < DefaultChunkSize / lodNum; y++)
        {
            for (int x = 0; x < DefaultChunkSize / lodNum; x++)
            {
                Triangles[lodState][tris] = vertexIndex;
                Triangles[lodState][tris + 1] = vertexIndex + (DefaultChunkSize / lodNum) + 1;
                Triangles[lodState][tris + 2] = vertexIndex + 1;
                Triangles[lodState][tris + 3] = vertexIndex + 1;
                Triangles[lodState][tris + 4] = vertexIndex + (DefaultChunkSize / lodNum) + 1;
                Triangles[lodState][tris + 5] = vertexIndex + (DefaultChunkSize / lodNum) + 2;

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
    public Vector3 Scale;
    public int BiomeNum;
    public int FoliageNum;
    public Vector3 Position;

    public FoliageInfoToMove(int biomeNum, int foliageNum, Vector3 position, float scaleIncrease, float randomRotation)
    {
        BiomeNum = biomeNum;
        FoliageNum = foliageNum;
        Position = position;
        float scale = (World.Biomes[biomeNum]._FoliageSettings.FoliageInfos[foliageNum].MaxExtensionHeight * scaleIncrease) + World.Biomes[biomeNum]._FoliageSettings.FoliageInfos[foliageNum].MinFoliageScale;
        Scale = new(scale, scale, scale);
        Rotation = Quaternion.Euler(0, randomRotation * 360, 0);
    }
}