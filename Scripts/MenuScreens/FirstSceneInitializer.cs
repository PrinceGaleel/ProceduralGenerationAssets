using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FirstSceneInitializer : MonoBehaviour
{
    public BiomeData[] Biomes;

    public Material MeshMaterial;
    public Material GrassMaterial;
    public ComputeShader GrassShader;

    public CustomDictionary<GameObject, Vector2> CenterBuildings;
    public CustomDictionary<GameObject, Vector2> EssentialBuildings;
    public CustomDictionary<GameObject, Vector2> Houses;
    public CustomDictionary<GameObject, Vector2> OptionalBuilding;
    public CustomDictionary<GameObject, Vector2> Extras;

    private void Awake()
    {
        World.MeshMaterial = MeshMaterial;
        World.GrassMaterial = GrassMaterial;
        World.GrassShader = GrassShader;

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
        SetTerrainGradient();
    }

    private void Start()
    {
        GlobalSettings.LoadSettings();
        SceneTransitioner.LoadScene("MainMenu");
    }

    private static void SetTerrainGradient()
    {
        TerrainGradient.Gradient2DColorKey[] colorKeys = new TerrainGradient.Gradient2DColorKey[World.Biomes.Length];

        for (int i = 0; i < colorKeys.Length; i++)
        {
            colorKeys[i] = new(World.Biomes[i].TerrainColor, World.Biomes[i].LowestPoint, World.Biomes[i].HighestPoint, World.Biomes[i].LowestTemperature, World.Biomes[i].HighestTemperature);
        }

        TerrainGradient.ColorKeys = colorKeys;
    }
}