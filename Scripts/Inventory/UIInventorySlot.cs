using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIInventorySlot : MonoBehaviour
{
    public Item _Item;
    public Image ItemImage;

    public TextMeshProUGUI TextItemName;
    public TextMeshProUGUI TextItemType;
    public TextMeshProUGUI TextItemValue;
    public TextMeshProUGUI TextAmount;

    public GameObject FavouriteButton;
    public GameObject UnfavouriteButton;

    public UIInventory _UIInventory;

    public void Intialize(Item item, int amount, Transform container, bool isFavourited, UIInventory uiInv)
    {
        transform.SetParent(container);
        _Item = item;
        ItemImage.sprite = item.ItemImage;
        TextItemName.text = item.ItemName;
        TextItemType.text = _Item._ItemType.ToString();
        TextAmount.text = amount.ToString();
        TextItemValue.text = item.Value.ToString();
        transform.localScale = new(1, 1, 1);
        _UIInventory = uiInv;

        if(isFavourited)
        {
            FavouriteButton.SetActive(false);
            UnfavouriteButton.SetActive(true);
        }
        else
        {
            FavouriteButton.SetActive(true);
            UnfavouriteButton.SetActive(false);
        }
    }

    public void FavouriteItem()
    {
        if (!_UIInventory.Favourites.Contains(_Item))
        {
            _UIInventory.Favourites.Add(_Item);
        }
        else
        {
            Debug.Log("Error: Item already found in favourites");
        }

        UnfavouriteButton.SetActive(true);
        FavouriteButton.SetActive(false);
    }

    public void UnfavouriteItem()
    {
        if (_UIInventory.Favourites.Contains(_Item))
        {
            _UIInventory.Favourites.Remove(_Item);
        }
        else
        {
            Debug.Log("Error: Item not found in favourites");
        }

        FavouriteButton.SetActive(true);
        UnfavouriteButton.SetActive(false);
    }
}