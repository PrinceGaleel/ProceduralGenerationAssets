using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomizeSprites : MonoBehaviour
{
    public Sprite[] Sprites;
    private Image[] Images;

    private void Awake()
    {
        Images = GetComponentsInChildren<Image>();

        foreach (Image image in Images)
        {
            image.sprite = Sprites[Random.Range(0, Sprites.Length)];
        }
    }
}