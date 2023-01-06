using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;

public enum StructureTypes
{
    Village,
    MobDen
}

public class StructureCreator : MonoBehaviour
{
    public static StructureCreator Instance;
    public static List<Vector2Int> VillageChunks = new();
    public static List<Vector2Int> MobDensToCreate = new() { new(0, 0) };

    [Header("Mob Den Variables")]
    public GameObject[] MobDenPrefabs;

    [Header("Village Variables")]
    private static Queue<Chunk> VillagesToPrepare;
    private static Queue<Dictionary<Vector2, GameObject>> VillagesToCreate;
    public static Queue<Transform> StructureParents;

    public const int MinVillageSize = 20;
    public const int MaxVillageSize = 30;

    public const int MainRoadThickness = 5;
    public const int SideRoadThickness = 2;
    public const int BuildingPadding = 3;

    public static CustomDictionary<GameObject, Vector2> CenterBuildings;
    public static CustomDictionary<GameObject, Vector2> EssentialBuildings;
    public static CustomDictionary<GameObject, Vector2> Houses;
    public static CustomDictionary<GameObject, Vector2> OptionalBuilding;
    public static CustomDictionary<GameObject, Vector2> Extras;

    private static Thread VillageInfoThread;
    private static System.Random Rnd;

    private readonly static Vector2[] StartingQuadrants = new Vector2[4]
    {
        new(-1, -1),
        new(-1, 1),
        new(1, -1),
        new(1, 1)
    };

    [Header("Mob Dens")]
    public static List<GameObject> MobDens;

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
            VillagesToCreate = new();
            StructureParents = new();
            VillagesToPrepare = new();

            VillageInfoThread = new(new ThreadStart(PrepareVillage));
        }
    }

    private void Start()
    {
        VillageInfoThread.Start();
    }

    private void Update()
    {
        lock (VillagesToCreate) lock (StructureParents)
            {
                if (VillagesToCreate.Count > 0)
                {
                    Dictionary<Vector2, GameObject> buildings = VillagesToCreate.Dequeue();
                    Transform chunk = StructureParents.Dequeue();
                    foreach (KeyValuePair<Vector2, GameObject> pair in buildings)
                    {
                        Instantiate(pair.Value, new(pair.Key.x, Chunk.GetPerlinNoise(pair.Key.x + (Chunk.ChunkSize / 2), pair.Key.y + (Chunk.ChunkSize / 2), World.CurrentSaveData.HeightPerlin) * SaveData.HeightMultipler, pair.Key.y), Quaternion.identity, chunk);
                    }
                }
            }
    }

    public static void CreateVillage(Chunk chunk)
    {
        lock (VillagesToPrepare)
        {
            VillagesToPrepare.Enqueue(chunk);
        }
    }

    private static void PrepareVillage()
    {
        while (true)
        {
            if (VillagesToPrepare.Count > 0)
            {
                Chunk chunk;
                lock (VillagesToPrepare)
                {
                    chunk = VillagesToPrepare.Dequeue();
                }

                CustomDictionary<Vector2, GameObject> buildings = new();
                int whichCenter = Rnd.Next(CenterBuildings.Count);
                buildings.Add(chunk.WorldPosition, CenterBuildings.Pairs[whichCenter].Key);

                CustomDictionary<GameObject, Vector2> buildingOrder = new();
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
                float highestX = chunk.WorldPosition.x + CenterBuildings[whichCenter].Value.x;
                float lowestX = chunk.WorldPosition.x - CenterBuildings[whichCenter].Value.x;
                float highestY = chunk.WorldPosition.y + CenterBuildings[whichCenter].Value.y;
                float lowestY = chunk.WorldPosition.y - CenterBuildings[whichCenter].Value.y;

                //
                for (int i = 0; i < 4; i++)
                {
                    CustomPair<GameObject, Vector2> pair = buildingOrder.TakePairAt(0);
                    buildings.Add(buildings[0].Key + ((CenterBuildings.Pairs[whichCenter].Value + pair.Value) * StartingQuadrants[i]) + (0.5f * MainRoadThickness * StartingQuadrants[i]), pair.Key);

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

                lock (VillagesToCreate) lock (StructureParents)
                    {
                        VillagesToCreate.Enqueue(buildings);
                        StructureParents.Enqueue(chunk.StructureParent);
                    }


                FoliageManager.AddTreesToRemove(new(lowestX, lowestY), new(highestX, highestY));
            }
        }
    }

    private void OnDestroy()
    {
        VillageInfoThread.Abort();
    }
}