using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeData : ScriptableObject
{
    public string BiomeName;

    public Color TintColor;
    public Color TerrainColor;
    public Color DirtColor = new(0.3137255f, 0.1960784f, 0.07843138f);
    public Gradient TerrainGradient;
    public FoliageSettings _FoliageSettings;

    [Header("Height & Temperature Ranges")]
    public float LowestPoint;
    public float HighestPoint;
    public float LowestTemperature;
    public float HighestTemperature;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (TerrainGradient == null) TerrainGradient = new();

        TerrainGradient.colorKeys = new GradientColorKey[3] { new(DirtColor, 0.1f), new(TerrainColor, 0.5f), new(TintColor, 1) };
    }
#endif
}

[Serializable]
public class FoliageSettings
{
    [Header("Foliage")]
    public FoliageInfo[] FoliageInfos;
    public float ChanceOfTree;
}

[Serializable]
public struct FoliageInfo
{
    public GameObject Prefab;
    public float MaxExtensionHeight;
    public float ChanceOfSpawning;
}