using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMap : MonoBehaviour
{
    private const int ColorMapWidth = 1000;

    private void OnEnable()
    {
        Color32[] colorMap = new Color32[ColorMapWidth * ColorMapWidth];
        for (int i = 0, y = 0; y < ColorMapWidth; y++)
        {
            for (int x = 0; x < ColorMapWidth; x++)
            {
                float noise = Chunk.GetDirtPerlin(x, y);
                colorMap[i] = Color.Lerp(Color.white, Color.black, noise);
                i++;
            }
        }

        Texture2D texture = new(ColorMapWidth, ColorMapWidth);
        texture.SetPixels32(colorMap);
        texture.Apply();

        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }
}