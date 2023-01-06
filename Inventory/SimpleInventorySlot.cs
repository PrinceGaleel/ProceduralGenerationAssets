using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimpleInventorySlot : MonoBehaviour
{
    public Item _Item;
    public Image ItemImage;
    public TextMeshProUGUI ItemAmount;
    public Inventory ParentInventory;

    public void Intialize(Item item, int amount, Transform newParent, Inventory parentInventory)
    {
        transform.SetParent(newParent);
        _Item = item;
        ItemImage.sprite = item.ItemImage;
        ItemAmount.text = amount.ToString();
        transform.localScale = new(1, 1, 1);
        ParentInventory = parentInventory;
    }
}