using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainGradient 
{
    public static Gradient2DColorKey[] ColorKeys;
    public const float BlendPercent = 0.02f;

    public static int BiomeNumFromPosition(float x, float y)
    {
        for (int i = 0; i < ColorKeys.Length; i++)
        {
            if (ColorKeys[i].Contains(x, y))
            {
                return i;
            }
        }

        return 0;
    }

    public static Gradient2DColorKey ColorKeyFromPosition(float x, float y)
    {
        return ColorKeys[BiomeNumFromPosition(x, y)];
    }

    public static void Evaluate(float heightNoise, float temperatureNoise, int[] biomeNums, Color[] colors, int index)
    {
        for (int i = 0; i < ColorKeys.Length; i++)
        {
            if (ColorKeys[i].Contains(heightNoise, temperatureNoise))
            {
                bool isOutX = heightNoise > 1 - BlendPercent;
                bool isOutY = temperatureNoise > 1 - BlendPercent;

                if (isOutX && !isOutY)
                {
                    int topNum = BiomeNumFromPosition(heightNoise, temperatureNoise + BlendPercent);
                    colors[index] = Color.Lerp(ColorKeys[i]._Color, ColorKeys[topNum]._Color, Mathf.Abs(temperatureNoise - ColorKeys[i].YBorder) / BlendPercent);
                }
                else if (isOutY && !isOutX)
                {
                    int rightNum = BiomeNumFromPosition(heightNoise + BlendPercent, temperatureNoise);
                    colors[index] = Color.Lerp(ColorKeys[i]._Color, ColorKeys[rightNum]._Color, Mathf.Abs(heightNoise - ColorKeys[i].RightBorderX) / BlendPercent);
                }
                else if ((temperatureNoise >= ColorKeys[i].YBorder || heightNoise >= ColorKeys[i].RightBorderX) && !isOutY && !isOutX)
                {
                    int topNum = BiomeNumFromPosition(heightNoise, temperatureNoise + BlendPercent);
                    int rightNum = BiomeNumFromPosition(heightNoise + BlendPercent, temperatureNoise);
                    int topRightNum = BiomeNumFromPosition(heightNoise + BlendPercent, temperatureNoise + BlendPercent);

                    int whichXBorder;
                    if (topNum != topRightNum)
                    {
                        whichXBorder = topNum;
                    }
                    else
                    {
                        whichXBorder = i;
                    }

                    int whichYBorder;
                    if (topRightNum != rightNum)
                    {
                        whichYBorder = rightNum;
                    }
                    else
                    {
                        whichYBorder = i;
                    }

                    Color colorTop = Color.Lerp(ColorKeys[topNum]._Color, ColorKeys[topRightNum]._Color, Mathf.Abs(ColorKeys[whichXBorder].RightBorderX - heightNoise) / BlendPercent);
                    Color colorBottom = Color.Lerp(ColorKeys[i]._Color, ColorKeys[rightNum]._Color, Mathf.Abs(ColorKeys[whichXBorder].RightBorderX - heightNoise) / BlendPercent);

                    colors[index] = Color.Lerp(colorBottom, colorTop, Mathf.Abs(temperatureNoise - ColorKeys[whichYBorder].YBorder) / BlendPercent);
                }
                else
                {
                    colors[index] = ColorKeys[i]._Color;
                }

                biomeNums[index] = i;
                return;
            }
        }
    }

    public struct Gradient2DColorKey
    {
        public Color _Color;

        public float LowestX;
        public float HighestX;
        public float LowestY;
        public float HighestY;

        public float RightBorderX;
        public float YBorder;

        public bool Contains(float x, float y)
        {
            if(LowestX <= x && HighestX >= x && LowestY <= y && HighestY >= y)
            {
                return true;
            }

            return false;
        }

        public Gradient2DColorKey(Color color, float lowestX, float highestX, float lowestY, float highestY)
        {
            _Color = color;

            LowestX = lowestX;
            HighestX = highestX;
            LowestY = lowestY;
            HighestY = highestY;

            RightBorderX = HighestX - BlendPercent;
            YBorder = HighestY - BlendPercent;
        }
    }
}
