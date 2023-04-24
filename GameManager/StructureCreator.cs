using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;

using static GameManager;
using static PerlinData;
using static Chunk;
using static Road;

using Random = System.Random;
public enum StructureTypes
{
    Empty,
    Village,
    MobDen
}

public class StructureCreator : MonoBehaviour
{
    public static StructureCreator Instance { get; private set; }

    public static List<Vector2Int> VillageChunks = new() { new(0, 0) };
    public static List<Vector2Int> MobDenPositions = new() { };

    private static Random Rnd;
    private static GameObject[] MobDenPrefabs;
    private static List<CustomTuple<GameObject, Vector2>> CenterBuildings, EssentialBuildings, Houses, OptionalBuildings, Extras;

    private static Dictionary<Vector2Int, Village> Villages;

    public static void InitializePrefabs(GameObject[] mobDenPrefabs, VillageBuildings village)
    {
        MobDenPrefabs = mobDenPrefabs;
        CenterBuildings = village.CenterBuildings;
        EssentialBuildings = village.EssentialBuildings;
        Houses = village.Houses;
        OptionalBuildings = village.OptionalBuildings;
        Extras = village.Extras;
    }

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
            Rnd = new();
            Villages = new();
        }
    }

    private void OnDestroy()
    {
        Villages = null;
    }

    public readonly struct PrepareVillage : IJob
    {
        private readonly Vector2Int ChunkPosition;
        private readonly Vector2 StartPosition;

        private const int MinBuildings = 20, MaxBuildings = 30;
        private const int MinSideRoads = 1, MaxSideRoads = 2;
        private const int MaxMainRoads = 0, MinMainRoads = 2;
        private const float MainRoadWidth = 10, SideRoadWidth = 4;
        private const float BuildingPadding = 3;

        public PrepareVillage(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            StartPosition = chunkPosition * ChunkSize;
        }

        public static Quaternion GetRotator(float angle) { return Quaternion.AngleAxis(angle, Vector3.up); }
        public static Vector2 Vec3ToVec2(Vector3 position) { return new(position.x, position.z); }
        public static Vector3 Vec2ToVec3(Vector2 position) { return new (position.x, 0, position.y); }

        public void Execute()
        {
            lock (Rnd)
            {
                Queue<CustomTuple<GameObject, Vector2>> buildings = GetBuildings();
                List<Vector2> positions = new()
                {
                    new Vector2((float)Rnd.NextDouble() * 2 - 1, (float)Rnd.NextDouble() * 2 - 1).normalized
                };

                positions.Add(-positions[0]);

                int[] roadsPerQuadrant = new int[4] { Rnd.Next(MinSideRoads, MaxSideRoads + 1), Rnd.Next(MinSideRoads, MaxSideRoads + 1), Rnd.Next(MinSideRoads, MaxSideRoads + 1), Rnd.Next(MinSideRoads, MaxSideRoads + 1) };
                CustomTuple<Vector2, float>[] startingDirections = new CustomTuple<Vector2, float>[4]
                {
                    new(positions[0], 90),
                    new(positions[0], -90),
                    new(positions[1], 90),
                    new(positions[1], -90)
                };

                for (int i = 0; i < 4; i++)
                {
                    Quaternion rotation = Quaternion.LookRotation((GetRotator(-startingDirections[i].Item2) * Vec2ToVec3(startingDirections[i].Item1)) - new Vector3(0, 0, 0));
                    Vector2 position = StartPosition + (startingDirections[i].Item1 * buildings.Peek().Item2.x) + Vec3ToVec2(MainRoadWidth * 0.5f * (GetRotator(startingDirections[i].Item2) * Vec2ToVec3(startingDirections[i].Item1)));

                    int numBuildings = Mathf.Min(Mathf.FloorToInt((float)buildings.Count / (4 - i)), buildings.Count);
                    int[] numBuildingsPerRoad = new int[roadsPerQuadrant[i] + 1];

                    for (int j = 0; j < numBuildingsPerRoad.Length; j++)
                    {
                        numBuildingsPerRoad[j] = Rnd.Next(numBuildings / 2, numBuildings);
                        numBuildings -= numBuildingsPerRoad[j];
                    }

                    for (int j = 0; j < numBuildingsPerRoad.Length; j++)
                    {
                        for (int k = numBuildingsPerRoad[j]; k > 0; k--)
                        {
                            CustomTuple<GameObject, Vector2> pair = buildings.Dequeue();

                            new CreateStructure(ChunkPosition, position, pair.Item1, rotation).Schedule();
                            position += startingDirections[i].Item1 * (pair.Item2.x + buildings.Peek().Item2.x + BuildingPadding); 
                        }

                        position += startingDirections[i].Item1 * (BuildingPadding + SideRoadWidth);
                    }
                }
            }
        }

        private Queue<CustomTuple<GameObject, Vector2>> GetBuildings()
        {
            Queue<CustomTuple<GameObject, Vector2>> buildings = new(EssentialBuildings);

            int numBuildings = Rnd.Next(MinBuildings, MaxBuildings + 1);
            int numHouses = Mathf.FloorToInt(numBuildings * 0.6f);
            int numOptional = Mathf.FloorToInt(numBuildings * 0.2f);

            numBuildings -= numHouses;
            numBuildings -= numOptional;

            if (Houses.Count > 0) for (int i = 0; i < numHouses; i++) buildings.Enqueue(Houses[Rnd.Next(Houses.Count)]);
            if (OptionalBuildings.Count > 0) for (int i = 0; i < numOptional; i++) buildings.Enqueue(OptionalBuildings[Rnd.Next(OptionalBuildings.Count)]);
            if (Extras.Count > 0) for (int i = 0; i < numBuildings; i++) buildings.Enqueue(Extras[Rnd.Next(Extras.Count)]);

            return buildings;
        }
    }

    public class ScheduleVillageCreation : MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;

        public ScheduleVillageCreation(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public override void Execute()
        {
            Village village = new GameObject("Village").AddComponent<Village>();
            village.transform.SetParent(ActiveTerrain[ChunkPosition].transform);
            Villages.Add(ChunkPosition, village);
            new PrepareVillage(ChunkPosition).Schedule();
        }
    }

    public class CreateDen : MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;

        public CreateDen(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public override void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition))
            {
                Instantiate(MobDenPrefabs[UnityEngine.Random.Range(0, MobDenPrefabs.Length)],
                    GetPerlinPosition(ChunkPosition * ChunkSize), Quaternion.identity).
                    transform.SetParent(ActiveTerrain[ChunkPosition].MyTransform);
            }
        }
    }

    public class CreateStructure : MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;
        private readonly Vector2 Position;
        private readonly GameObject Prefab;
        private readonly Quaternion Rotation;

        public CreateStructure(Vector2Int chunkPosition, Vector2 position, GameObject prefab, Quaternion rotation)
        {
            ChunkPosition = chunkPosition;
            Position = position;
            Prefab = prefab;
            Rotation = rotation;
        }

        public override void Execute()
        {
            if (ActiveTerrain.ContainsKey(ChunkPosition)) Instantiate(Prefab, GetPerlinPosition(Position), Rotation, Villages[ChunkPosition].transform);
        }
    }
}