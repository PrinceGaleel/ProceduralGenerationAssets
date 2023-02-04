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
    private List<int> ItemAmounts;

    private SimpleInventorySlot CurrentlySelected;
    public GameObject InvSlotPrefab;
    public Transform ItemsContainer;
    public GridLayoutGroup Layout;

    public TextMeshProUGUI GoldAmountText;
    public TextMeshProUGUI PageNumText;

    private SortingMethod CurrentSortingMethod;
    private ItemTypes CurrentType;

    private readonly static char[] Characters = new char[27] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '};
    private Dictionary<char, int> NumCharacterValues;

    private void Awake()
    {
        CurrentPage = 1;
        CurrentItems = new();
        CurrentlySelected = null;

        NumCharacterValues = new();
        for (int i = 0; i < Characters.Length; i++)
        {
            NumCharacterValues.Add(Characters[i], i);
        }
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
            CurrentlySelected = null;
            CurrentSortingMethod = SortingMethod.Alphabetical;
            CurrentType = ItemTypes.Weapon;

            CurrentPage = 1;
            GoldAmountText.text = PlayerStats.Instance._Inventory.Gold.ToString();
            SetInventoryType(ItemTypes.Misc);
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
            SetInventoryType(ItemTypes.Misc);
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
            SetInventoryType(ItemTypes.Armour);
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
            
            CurrentItems = PlayerStats.Instance._Inventory.GetItems();
            ItemAmounts = PlayerStats.Instance._Inventory.GetItemAmounts();

            if (itemType == ItemTypes.Misc)
            {
                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    Instantiate(InvSlotPrefab).GetComponent<SimpleInventorySlot>().Intialize(CurrentItems[i], ItemAmounts[i], ItemsContainer, PlayerStats.Instance._Inventory);
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

                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    if (CurrentItems[i]._ItemType == CurrentType)
                    {
                        Instantiate(InvSlotPrefab).GetComponent<SimpleInventorySlot>().Intialize(CurrentItems[i], ItemAmounts[i], ItemsContainer, PlayerStats.Instance._Inventory);
                    }
                }
            }

            MaxPage = Mathf.CeilToInt((float)CurrentItems.Count / SlotsPerPage);

            if(MaxPage < 2)
            {
                PageNumText.gameObject.SetActive(false);
            }
            else
            {
                PageNumText.gameObject.SetActive(true);
            }

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
        if (MaxPage > 1)
        {
            CurrentPage = (int)Mathf.Repeat(CurrentPage + direction, MaxPage);
            PageNumText.text = CurrentPage.ToString();
        }
    }
}