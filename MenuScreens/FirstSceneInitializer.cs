using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class FirstSceneInitializer : MonoBehaviour
{
    public GameObject ChunkPrefab;
    public BiomeData[] Biomes;

    public Material GrassMaterial;
    public ComputeShader GrassShader;

    public GameObject[] MobDenPrefabs;
    public PairList<GameObject, Vector2> CenterBuildings, EssentialBuildings, Houses, OptionalBuildings, Extras;

    private void Awake()
    {
        GrassManager.GrassMaterial = Instantiate(GrassMaterial);
        GrassManager.GrassShader = Instantiate(GrassShader);

        StructureCreator.InitializePrefabs(MobDenPrefabs, CenterBuildings, EssentialBuildings, Houses, OptionalBuildings, Extras);
        TerrainGradient.Initialize(Biomes);
        World.InitializeConstants(ChunkPrefab);
        GrassManager.Initialize(GrassMaterial, GrassShader);
        Chunk.InitializeTriangles();
    }
}