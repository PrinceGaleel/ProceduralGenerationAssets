using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static GameManager;

public static class TerrainColorGradient
{
    public static BiomeData[] BiomeDatas { get; private set; }
    private static Dictionary<Vector2Int, BiomeData> BiomesDict;
    private static Dictionary<BiomeData, int> BiomeNums;
    private const float BlendPercent = 0.02f;

    public static void InitializeStatics(BiomeData[] biomeDatas)
    {
        BiomesDict = new();
        BiomeDatas = biomeDatas;
        BiomeNums = new();

        for (int i = 0; i < BiomeDatas.Length; i++)
        {
            BiomeNums.Add(BiomeDatas[i], i);
        }
        foreach (BiomeData biome in biomeDatas)
        {
            if (Mathf.RoundToInt(biome.LowestPoint * 10) == 0 && Mathf.RoundToInt(biome.LowestTemperature * 10) == 0)
            {
                BiomesDict.TryAdd(new(0, 0), biome);
            }

            if (biome.LowestPoint == 0)
            {
                for (int j = Mathf.RoundToInt((biome.LowestTemperature + 0.1f) * 10); j <= Mathf.RoundToInt(biome.HighestTemperature * 10); j += 1)
                {
                    BiomesDict.TryAdd(new(0, j), biome);
                }
            }

            if (biome.LowestTemperature == 0)
            {
                for (int i = Mathf.RoundToInt((biome.LowestPoint + 0.1f) * 10); i <= Mathf.RoundToInt(biome.HighestPoint * 10); i += 1)
                {
                    BiomesDict.TryAdd(new(i, 0), biome);
                }
            }
        }
        foreach (BiomeData biome in biomeDatas)
        {
            for (int i = Mathf.RoundToInt((biome.LowestPoint + 0.1f) * 10); i <= Mathf.RoundToInt(biome.HighestPoint * 10); i += 1)
            {
                for (int j = Mathf.RoundToInt((biome.LowestTemperature + 0.1f) * 10); j <= Mathf.RoundToInt(biome.HighestTemperature * 10); j += 1)
                {
                    BiomesDict.Add(new(i, j), biome);
                }
            }
        }
    }

    public static int GetBiomeNum(BiomeData biomeData)
    {
        return BiomeNums[biomeData];
    }

    public static Vector3 TerrainColorAsVector3(float x, float y)
    {
        return ColorAsVec3(GetTerrainColor(GetHeightPerlin(x, y), GetTemperaturePerlin(x, y)));
    }

    public static Vector3 ColorAsVec3(Color color)
    {
        return new(color.r, color.g, color.b);
    }

    private static int CeilToOneDPInt(float f)
    {
        return Mathf.Clamp(Mathf.CeilToInt(f * 10), 0, 10);
    }

    public static BiomeData GetBiomeData(float heightNoise, float tempNoise)
    {
        heightNoise = Mathf.Clamp(heightNoise, 0, 1); tempNoise = Mathf.Clamp(tempNoise, 0, 1);
        return BiomesDict[new(CeilToOneDPInt(heightNoise), CeilToOneDPInt(tempNoise))];
    }

    public static Color GetTerrainColor(float heightNoise, float tempNoise)
    {
        int xPos = CeilToOneDPInt(heightNoise);
        int yPos = CeilToOneDPInt(tempNoise);

        BiomeData biome = BiomesDict[new(xPos, yPos)];

        BiomeData topNum = BiomesDict[new(xPos, CeilToOneDPInt(tempNoise + BlendPercent))];
        BiomeData rightNum = BiomesDict[new(CeilToOneDPInt(heightNoise + BlendPercent), yPos)];
        BiomeData topRightNum = BiomesDict[new(CeilToOneDPInt(heightNoise + BlendPercent), CeilToOneDPInt(tempNoise + BlendPercent))];

        BiomeData whichXBorder = topNum != topRightNum ? topNum : biome;
        BiomeData whichYBorder = topRightNum != rightNum ? rightNum : biome;

        Color colorTop = Color.Lerp(topNum.TerrainColor, topRightNum.TerrainColor, Mathf.Abs(whichXBorder.HighestPoint - BlendPercent - heightNoise) / BlendPercent);
        Color colorBottom = Color.Lerp(biome.TerrainColor, rightNum.TerrainColor, Mathf.Abs(whichXBorder.HighestPoint - BlendPercent - heightNoise) / BlendPercent);
        Color blendedColor = Color.Lerp(colorBottom, colorTop, Mathf.Abs(whichYBorder.HighestTemperature - BlendPercent - tempNoise) / BlendPercent);

        return blendedColor;
    }
}