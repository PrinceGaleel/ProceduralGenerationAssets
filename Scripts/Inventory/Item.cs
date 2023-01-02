using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemTypes
{
    Crafting,
    Tool,
    Weapon,
    Equipment,
    Misc,
    Potion,
    Food,
    Other
}

[CreateAssetMenu()]
public class Item : ScriptableObject
{
    [Header("Item Info")]
    public ItemTypes _ItemType;
    public string ItemName;
    [TextArea(10, 10)]
    public string Description;
    public Sprite ItemImage;
    public int Value;

    public Recipe[] Recipes;

    [Header("Prefabs")]
    public GameObject DropPrefab;
    public GameObject OtherPrefab;

    [System.Serializable]
    public class Recipe
    {
        public CustomDictionary<Item, int> ItemsRequired;
        public int AmountCrafted;
    }
}