using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Item MyItem;
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI ItemName;
    [SerializeField] private TextMeshProUGUI ItemAmount;
    [SerializeField] private TextMeshProUGUI ItemType;
    [SerializeField] private TextMeshProUGUI ItemValue;
    [SerializeField] private Inventory ParentInventory;

    public void Intialize(Item item, int amount, Transform newParent, Inventory parentInventory)
    {
        transform.SetParent(newParent);
        transform.localScale = new(1, 1, 1);

        MyItem = item;
        ItemImage.sprite = item.ItemImage;
        ItemName.text = item.ItemName;
        ItemAmount.text = amount.ToString();
        ItemValue.text = item.Price.ToString();
        ItemType.text = item.MyItemType.ToString();

        ParentInventory = parentInventory;
    }
}