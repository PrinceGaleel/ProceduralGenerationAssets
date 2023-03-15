using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomizeSprites : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private string SpriteName = "";
    [SerializeField] private Sprite[] Sprites;
    [SerializeField] private Image[] Images;
    [SerializeField] private bool Randomize = true;

    private void OnValidate()
    {
        if (Images != null)
        {
            if (Images.Length == 0)
            {
                Images = GetComponentsInChildren<Image>();
            }
            else if (Randomize)
            {
                Randomize = false;

                foreach (Image image in Images)
                {
                    image.sprite = Sprites[Random.Range(0, Sprites.Length)];
                }

                if (SpriteName != "")
                {
                    foreach (Image image in Images)
                    {
                        image.gameObject.name = SpriteName;
                    }
                }
            }
        }
    }
#endif
}