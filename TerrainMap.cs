using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMap : MonoBehaviour
{
    private const int ColorMapWidth = 1000;

    private void Awake()
    {
        Color[] colorMap = new Color[ColorMapWidth * ColorMapWidth];
        for (int yPos = 0; yPos < ColorMapWidth; yPos++)
        {
            for (int xPos = 0; xPos < ColorMapWidth; xPos++)
            {
                colorMap[xPos + yPos * ColorMapWidth] = new();
                float x = (float)xPos / ColorMapWidth;
                float y = (float)yPos / ColorMapWidth;

                for (int i = 0; i < TerrainGradient.ColorKeys.Length; i++)
                {
                    if (TerrainGradient.ColorKeys[i].Contains(x, y))
                    {
                        int rightNum = TerrainGradient.BiomeNumFromPosition(x + TerrainGradient.BlendPercent, y);
                        int topNum = TerrainGradient.BiomeNumFromPosition(x, y + TerrainGradient.BlendPercent);

                        bool isOutX = x > 1 - TerrainGradient.BlendPercent;
                        bool isOutY = y > 1 - TerrainGradient.BlendPercent;

                        if (isOutX && !isOutY)
                        {
                            colorMap[xPos + yPos * ColorMapWidth] = Color.Lerp(TerrainGradient.ColorKeys[i]._Color, TerrainGradient.ColorKeys[topNum]._Color, Mathf.Abs(y - TerrainGradient.ColorKeys[i].YBorder) / TerrainGradient.BlendPercent);
                        }
                        else if (isOutY && !isOutX)
                        {
                            colorMap[xPos + yPos * ColorMapWidth] = Color.Lerp(TerrainGradient.ColorKeys[i]._Color, TerrainGradient.ColorKeys[rightNum]._Color, Mathf.Abs(x - TerrainGradient.ColorKeys[i].RightBorderX) / TerrainGradient.BlendPercent);
                        }
                        else if ((y >= TerrainGradient.ColorKeys[i].YBorder || x >= TerrainGradient.ColorKeys[i].RightBorderX) && !isOutY && !isOutX)
                        {
                            int topRightNum = TerrainGradient.BiomeNumFromPosition(x + TerrainGradient.BlendPercent, y + TerrainGradient.BlendPercent);

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

                            Color colorTop = Color.Lerp(TerrainGradient.ColorKeys[topNum]._Color, TerrainGradient.ColorKeys[topRightNum]._Color, Mathf.Abs(TerrainGradient.ColorKeys[whichXBorder].RightBorderX - x) / TerrainGradient.BlendPercent);
                            Color colorBottom = Color.Lerp(TerrainGradient.ColorKeys[i]._Color, TerrainGradient.ColorKeys[rightNum]._Color, Mathf.Abs(TerrainGradient.ColorKeys[whichXBorder].RightBorderX - x) / TerrainGradient.BlendPercent);

                            colorMap[xPos + yPos * ColorMapWidth] = Color.Lerp(colorBottom, colorTop, Mathf.Abs(y - TerrainGradient.ColorKeys[whichYBorder].YBorder) / TerrainGradient.BlendPercent);
                        }
                        else
                        {
                            colorMap[xPos + yPos * ColorMapWidth] = TerrainGradient.ColorKeys[i]._Color;
                        }
                    }
                }
            }
        }

        Texture2D texture = new(ColorMapWidth, ColorMapWidth);
        texture.SetPixels(colorMap);
        texture.Apply();

        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }
}