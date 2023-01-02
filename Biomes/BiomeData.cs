using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeData : ScriptableObject
{
    public string BiomeName;

    public Color TerrainColor;
    public FoliageSettings _FoliageSettings;

    [Header("Height & Temperature Ranges")]
    public float LowestPoint;
    public float HighestPoint;
    public float LowestTemperature;
    public float HighestTemperature;
}

[Serializable]
public class FoliageSettings
{
    [Header("Foliage")]
    public FoliageInfo[] FoliageInfos;
    public PerlinData _PerlinData;
    public float PlacementThreshold;
    public int SquareCheckSize;
    public float ChanceOfTree;
}

[Serializable]
public struct FoliageInfo
{
    public GameObject Prefab;
    public float MinFoliageScale;
    public float MaxExtensionHeight;
    public float ChanceOfSpawning;
}

[Serializable]
public class PerlinData
{
    private const float DefaultPerlinScale = 0.05f;
    public Vector2Serializable Offset;
    public float PerlinScale;

    public PerlinData(Vector2Serializable perlinOffset, float perlinScale = DefaultPerlinScale)
    {
        Offset = perlinOffset;
        PerlinScale = perlinScale;
    }

    public PerlinData(float perlinScale)
    {
        Offset = new();
        PerlinScale = perlinScale;
    }

    public PerlinData()
    {
        Offset = new();
        PerlinScale = DefaultPerlinScale;
    }
}