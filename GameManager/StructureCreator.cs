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

    public class PrepareVillage : SecondaryThreadJob
    {
        private readonly Vector2Int ChunkPosition;
        private readonly Vector2Int WorldPosition;

        public PrepareVillage(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            WorldPosition = chunkPosition * Chunk.DefaultChunkSize;
        }

        public override void Execute()
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

            if (GameManager.ActiveTerrain[ChunkPosition])
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    new CreateStructure(ChunkPosition, buildings[i].Key, buildings[i].Value).Schedule();
                }
            }


            FoliageManager.AddTreesToRemove(new(lowestX, lowestY), new(highestX, highestY));
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
            if (GameManager.ActiveTerrain.ContainsKey(ChunkPosition))
            {
                Instantiate(MobDenPrefabs[Random.Range(0, MobDenPrefabs.Length)],
                    Chunk.GetPerlinPosition(ChunkPosition * Chunk.DefaultChunkSize), Quaternion.identity).
                    transform.SetParent(GameManager.ActiveTerrain[ChunkPosition].MyTransform);
            }
        }
    }

    public class CreateStructure : MainThreadJob
    {
        private readonly Vector2Int ChunkPosition;
        private readonly Vector2 Position;
        private readonly GameObject Prefab;

        public CreateStructure(Vector2Int chunkPosition, Vector2 position, GameObject prefab)
        {
            ChunkPosition = chunkPosition;
            Position = position;
            Prefab = prefab;
        }

        public override void Execute()
        {
            if(GameManager.ActiveTerrain.ContainsKey(ChunkPosition)) Instantiate(Prefab, Chunk.GetPerlinPosition(Position), Quaternion.identity, GameManager.ActiveTerrain[ChunkPosition].MyTransform);
        }
    }
}