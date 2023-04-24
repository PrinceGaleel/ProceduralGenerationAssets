using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

public class FirstSceneInitializer : MonoBehaviour
{
    [Header("Chunks")]
    [SerializeField] private GameObject ChunkPrefab;
    [SerializeField] private BiomeData[] Biomes;
    [SerializeField] private AnimationCurve TerrainGradient;

    [Header("Grass")]
    [SerializeField] private Material GrassMaterial;
    [SerializeField] private ComputeShader GrassShader;

    [Header("Structures")]
    [SerializeField] private GameObject[] MobDenPrefabs;
    [SerializeField] private VillageBuildings Village;

    private void Awake()
    {
        PerlinData.TerrainGradient = TerrainGradient;

        StructureCreator.InitializePrefabs(MobDenPrefabs, Village);
        TerrainColorGradient.InitializeStatics(Biomes);
        GameManager.InitializeConstants(ChunkPrefab);
        GrassManager.InitializeStatics(Instantiate(GrassMaterial), Instantiate(GrassShader));
        FoliageManager.InitializeThresholds();
        UIInventory.InitializeStatics();
    }
}