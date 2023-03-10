using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Unity.Jobs;
using System.Collections.Concurrent;

public enum StructureTypes
{
    Village,
    MobDen
}

public class StructureCreator : MonoBehaviour
{
    public static StructureCreator Instance { get; private set; }

    public static ConcurrentQueue<Vector2Int> MobDensToCreate;
    private static ConcurrentQueue<VillageToCreate> VillagesToCreate;

    public static List<Vector2Int> VillageChunks = new();
    public static List<Vector2Int> MobDenPositions = new() { new(0, 0) };

    private const int MinVillageSize = 20;
    private const int MaxVillageSize = 30;

    private const int MainRoadThickness = 5;
    private const int SideRoadThickness = 2;
    private const int BuildingPadding = 3;

    private static GameObject[] MobDenPrefabs;
    private static PairList<GameObject, Vector2> CenterBuildings;
    private static PairList<GameObject, Vector2> EssentialBuildings;
    private static PairList<GameObject, Vector2> Houses;
    private static PairList<GameObject, Vector2> OptionalBuildings;
    private static PairList<GameObject, Vector2> Extras;

    public static void InitializePrefabs(GameObject[] mobDenPrefabs, PairList<GameObject, Vector2> centerBuildings,
        PairList<GameObject, Vector2> essentialBuildings, PairList<GameObject, Vector2> houses,
        PairList<GameObject, Vector2> optionalBuildings, PairList<GameObject, Vector2> extras)
    {

        MobDenPrefabs = mobDenPrefabs;
        CenterBuildings = centerBuildings;
        EssentialBuildings = essentialBuildings;
        Houses = houses;
        OptionalBuildings = optionalBuildings;
        Extras = extras;
    }

    private static System.Random Rnd;

    private readonly static Vector2[] StartingQuadrants = new Vector2[4]
    {
        new(-1, -1),
        new(-1, 1),
        new(1, -1),
        new(1, 1)
    };

    private struct VillageToCreate
    {
        public Vector2Int ChunkPosition;
        public PairList<Vector2, GameObject> Buildings;

        public VillageToCreate(Vector2Int chunk, PairList<Vector2, GameObject> buildings)
        {
            ChunkPosition = chunk;
            Buildings = buildings;
        }
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
        }
    }

    private void Update()
    {
        if (VillagesToCreate.Count > 0)
        {
            if (VillagesToCreate.TryDequeue(out VillageToCreate villageInfo))
            {
                if (World.ActiveTerrain[villageInfo.ChunkPosition])
                { 
                    for (int i = 0; i < villageInfo.Buildings.Count; i++)
                    {
                        Instantiate(villageInfo.Buildings[i].Value, Chunk.GetPerlinPosition(villageInfo.Buildings[i].Key), Quaternion.identity, World.ActiveTerrain[villageInfo.ChunkPosition].MyTransform);
                    }
                }
            }
        }

        if (MobDensToCreate.Count > 0)
        {
            if (MobDensToCreate.TryDequeue(out Vector2Int chunkPos))
            {
                if (World.ActiveTerrain.ContainsKey(chunkPos))
                {
                    Instantiate(MobDenPrefabs[Random.Range(0, MobDenPrefabs.Length)],
                        Chunk.GetPerlinPosition(chunkPos * Chunk.DefaultChunkSize), Quaternion.identity).
                        transform.SetParent(World.ActiveTerrain[chunkPos].MyTransform);
                }
            }
        }
    }

    public static void InitializeStatics()
    {
        MobDensToCreate = new();
        VillagesToCreate = new();
    }

    public struct PrepareVillage : IJob
    {
        private readonly Vector2Int ChunkPosition;
        private readonly Vector2Int WorldPosition;

        public PrepareVillage(Vector2Int chunkPosition, Vector2Int worldPosition)
        {
            ChunkPosition = chunkPosition;
            WorldPosition = worldPosition;
        }

        public void Execute()
        {
            PairList<Vector2, GameObject> buildings = new();
            int whichCenter = Rnd.Next(CenterBuildings.Count);
            buildings.Add(WorldPosition, CenterBuildings.Keys[whichCenter]);

            PairList<GameObject, Vector2> buildingOrder = new();
            for (int i = 0; i < EssentialBuildings.Count; i++)
            {
                buildingOrder.Add(EssentialBuildings[i]);
            }
            int numHouses = Rnd.Next(MinVillageSize, MaxVillageSize);
            for (int i = 0; i < numHouses; i++)
            {
                buildingOrder.Add(Houses[Rnd.Next(Houses.Count)]);
            }
            buildingOrder.Shuffle();

            //
            float highestX = WorldPosition.x + CenterBuildings[whichCenter].Value.x;
            float lowestX = WorldPosition.x - CenterBuildings[whichCenter].Value.x;
            float highestY = WorldPosition.y + CenterBuildings[whichCenter].Value.y;
            float lowestY = WorldPosition.y - CenterBuildings[whichCenter].Value.y;

            //
            for (int i = 0; i < 4; i++)
            {
                CustomPair<GameObject, Vector2> pair = buildingOrder.TakePairAt(0);
                buildings.Add(buildings[0].Key + ((CenterBuildings.Values[whichCenter] + pair.Value) * StartingQuadrants[i]) + (0.5f * MainRoadThickness * StartingQuadrants[i]), pair.Key);

                Vector2 halfExtents = pair.Value;

                Vector2 currentX = buildings[^1].Key;
                Vector2 currentZ = buildings[^1].Key;

                bool isHorizontal = true;

                for (int j = Mathf.Min(Mathf.FloorToInt((float)buildingOrder.Count / (4 - i)), buildingOrder.Count); j > 0; j--)
                {
                    pair = buildingOrder.TakePairAt(0);
                    Vector2 newPos;

                    if (isHorizontal)
                    {
                        currentX += (halfExtents + pair.Value) * (new Vector2(1, 0) * StartingQuadrants[i]);
                        newPos = currentX;
                    }
                    else
                    {
                        currentZ += (halfExtents + pair.Value) * (new Vector2(0, 1) * StartingQuadrants[i]);
                        newPos = currentZ;
                    }

                    buildings.Add(newPos, pair.Key);
                    halfExtents = pair.Value;
                    isHorizontal = !isHorizontal;

                    if (highestX < newPos.x + pair.Value.x)
                    {
                        highestX = newPos.x + pair.Value.x;
                    }
                    else if (lowestX > newPos.x - pair.Value.x)
                    {
                        lowestX = newPos.x - pair.Value.x;
                    }

                    if (highestY < newPos.y + pair.Value.y)
                    {
                        highestY = newPos.y + pair.Value.y;
                    }
                    else if (lowestY > newPos.y - pair.Value.y)
                    {
                        lowestY = newPos.y - pair.Value.y;
                    }
                }
            }

            lock (VillagesToCreate) 
                {
                    VillagesToCreate.Enqueue(new(ChunkPosition, buildings));
                }


            FoliageManager.AddTreesToRemove(new(lowestX, lowestY), new(highestX, highestY));
        }
    }
}