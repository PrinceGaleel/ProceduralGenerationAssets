using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemTypes
{
    Crafting = 0,
    Tool = 1,
    Weapon = 2,
    Armour = 3,
    Potion = 4,
    Food = 5,
    Misc = 6
}

public abstract class Item : ScriptableObject
{
    [Header("Item Info")]
    public ItemTypes _ItemType;
    public string ItemName;
    [TextArea(10, 10)]
    public string Description;
    public Sprite ItemImage;
    public int Price;

    [Header("Prefabs")]
    public GameObject DropPrefab;
    public GameObject OtherPrefab;

    protected string DefaultToString()
    {
        return "Item Name: " + ItemName + "\nItem Type: " + _ItemType.ToString() + "\n" + Description + "Value: " + Price;
    }

    public override abstract string ToString();
}