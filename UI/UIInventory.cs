using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SortingMethod
{
    Alphabetical,
    Price
}

public class UIInventory : MonoBehaviour
{
    private const int SlotsPerPage = 20;
    private int CurrentPage;
    private int MaxPage;

    private List<Item> CurrentItems;
    public GameObject InvSlotPrefab;
    public Transform ItemsContainer;
    public VerticalLayoutGroup Layout;

    public TextMeshProUGUI GoldAmountText;
    public TextMeshProUGUI CategoryText;
    public TextMeshProUGUI PageNumText;

    private SortingMethod CurrentSortingMethod;
    private ItemTypes CurrentType;

    private void Awake()
    {
        CurrentPage = 1;
        CurrentItems = new();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIController.Instance.ToggleInventory(false);
        }
    }

    public void ToggleInventory(bool isEnabled)
    {
        gameObject.SetActive(isEnabled);

        if (isEnabled)
        {
            CurrentSortingMethod = SortingMethod.Alphabetical;
            CurrentType = ItemTypes.Weapon;

            CurrentPage = 1;
            GoldAmountText.text = PlayerStats.Instance._Inventory.Gold.ToString();
            SetInventoryType(ItemTypes.Other);
            gameObject.SetActive(true);
        }
        else
        {
            ClearInventorySlots();
        }
    }

    public void SetInvType(string itemType)
    {
        if (itemType.ToLower() == "other")
        {
            SetInventoryType(ItemTypes.Other);
        }
        else if (itemType.ToLower() == "crafting")
        {
            SetInventoryType(ItemTypes.Crafting);
        }
        else if (itemType.ToLower() == "tool")
        {
            SetInventoryType(ItemTypes.Tool);
        }
        else if (itemType.ToLower() == "weapon")
        {
            SetInventoryType(ItemTypes.Weapon);
        }
        else if (itemType.ToLower() == "equipment")
        {
            SetInventoryType(ItemTypes.Equipment);
        }
        else if (itemType.ToLower() == "misc")
        {
            SetInventoryType(ItemTypes.Misc);
        }
        else if (itemType.ToLower() == "potion")
        {
            SetInventoryType(ItemTypes.Potion);
        }
        else if (itemType.ToLower() == "food")
        {
            SetInventoryType(ItemTypes.Food);
        }
    }

    public void SetInventoryType(ItemTypes itemType)
    {
        if (CurrentType != itemType)
        {
            CurrentType = itemType;
            ClearInventorySlots();
            CurrentItems = new(PlayerStats.Instance._Inventory.Items.Keys);
            if (itemType == ItemTypes.Other)
            {
                CategoryText.text = "ALL";

                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    Instantiate(InvSlotPrefab).GetComponent<SimpleInventorySlot>().Intialize(CurrentItems[i], PlayerStats.Instance._Inventory.Items[CurrentItems[i]], ItemsContainer, PlayerStats.Instance._Inventory);
                }
            }
            else
            {
                for (int i = CurrentItems.Count - 1; i > -1; i--)
                {
                    if(CurrentItems[i]._ItemType != CurrentType)
                    {
                        CurrentItems.RemoveAt(i);
                    }
                }

                if (itemType == ItemTypes.Weapon)
                {
                    CategoryText.text = "WEAPONS";
                }
                else if (itemType == ItemTypes.Tool)
                {
                    CategoryText.text = "TOOLS";
                }
                else if (itemType == ItemTypes.Equipment)
                {
                    CategoryText.text = "EQUIPMENT";
                }
                else if (itemType == ItemTypes.Crafting)
                {
                    CategoryText.text = "CRAFTING ITEMS";
                }
                else if (itemType == ItemTypes.Misc)
                {
                    CategoryText.text = "MISC";
                }
                else if (itemType == ItemTypes.Potion)
                {
                    CategoryText.text = "POTIONS";
                }
                else if (itemType == ItemTypes.Weapon)
                {
                    CategoryText.text = "FOOD";
                }

                for (int i = 0; i < PlayerStats.Instance._Inventory.Items.Count; i++)
                {
                    if (CurrentItems[i]._ItemType == CurrentType)
                    {
                        Instantiate(InvSlotPrefab).GetComponent<SimpleInventorySlot>().Intialize(CurrentItems[i], PlayerStats.Instance._Inventory.Items[CurrentItems[i]], ItemsContainer, PlayerStats.Instance._Inventory);
                    }
                }
            }

            MaxPage = Mathf.CeilToInt((float)CurrentItems.Count / SlotsPerPage);
            LayoutRebuilder.MarkLayoutForRebuild(Layout.GetComponent<RectTransform>());
        }
    }

    private void ClearInventorySlots()
    {
        for (int i = ItemsContainer.childCount - 1; i > -1; i--)
        {
            Destroy(ItemsContainer.GetChild(i).gameObject);
        }
    }

    public void ChangeSortingMethod(string methodName)
    {
        if (methodName.ToLower() == "alphabetical")
        {
            CurrentSortingMethod = SortingMethod.Alphabetical;
        }
        else if (methodName.ToLower() == "price")
        {
            CurrentSortingMethod = SortingMethod.Price;
        }
    }

    public void IncrementPage(int direction)
    {
        CurrentPage = (int)Mathf.Repeat(CurrentPage + direction, MaxPage);
        PageNumText.text = CurrentPage.ToString();
    }
}