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
    public float ChanceOfTree;
}

[Serializable]
public struct FoliageInfo
{
    public GameObject Prefab;
    public float MaxExtensionHeight;
    public float ChanceOfSpawning;
}