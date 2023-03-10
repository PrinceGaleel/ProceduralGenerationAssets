using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomizeSprites : MonoBehaviour
{
    [SerializeField] private Sprite[] Sprites;
    [SerializeField] private Image[] Images;

    private void Awake()
    {
        if (Images.Length == 0)
        {
            Images = GetComponentsInChildren<Image>();
        }

        foreach (Image image in Images)
        {
            image.sprite = Sprites[Random.Range(0, Sprites.Length)];
        }
    }
}