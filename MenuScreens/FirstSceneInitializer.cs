using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class FirstSceneInitializer : MonoBehaviourPunCallbacks
{
    public GameObject ChunkPrefab;
    public BiomeData[] Biomes;

    public Material MeshMaterial;
    public Material GrassMaterial;
    public ComputeShader GrassShader;

    public DictList<GameObject, Vector2> CenterBuildings;
    public DictList<GameObject, Vector2> EssentialBuildings;
    public DictList<GameObject, Vector2> Houses;
    public DictList<GameObject, Vector2> OptionalBuilding;
    public DictList<GameObject, Vector2> Extras;

    private void Awake()
    {
        World.ChunkPrefab = ChunkPrefab;

        GrassManager.GrassMaterial = Instantiate(GrassMaterial);
        GrassManager.GrassShader = Instantiate(GrassShader);

        StructureCreator.CenterBuildings = CenterBuildings;
        StructureCreator.EssentialBuildings = EssentialBuildings;
        StructureCreator.Houses = Houses;
        StructureCreator.OptionalBuilding = OptionalBuilding;
        StructureCreator.Extras = Extras;

        for (int i = 1; i < Biomes.Length - 1; i++)
        {
            for (int j = i + 1; j > 1; j--)
            {
                if (Vector2.Distance(new(Biomes[j - 1].HighestPoint, Biomes[j - 1].HighestTemperature), new(0, 0)) > Vector2.Distance(new(Biomes[j].HighestPoint, Biomes[j].HighestTemperature), new(0, 0)))
                {
                    BiomeData temp = Biomes[j - 1];
                    Biomes[j - 1] = Biomes[j];
                    Biomes[j] = temp;
                }
            }
        }

        World.SetMasks();
        World.Biomes = Biomes;
        TerrainGradient.SetColorKeys();
        PhotonNetwork.ConnectUsingSettings();
        Chunk.InitializeTriangles();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
}