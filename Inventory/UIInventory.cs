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
    [SerializeField] private int SlotsPerPage = 20;
    [SerializeField] private int CurrentPage;
    [SerializeField] private int MaxPage;

    [SerializeField] private List<Item> CurrentItems;
    [SerializeField] private List<int> ItemAmounts;

    [SerializeField] private InventorySlot CurrentlySelected;
    [SerializeField] private GameObject InvSlotPrefab;
    [SerializeField] private VerticalLayoutGroup Layout;

    [SerializeField] private TextMeshProUGUI GoldAmountText;
    [SerializeField] private TextMeshProUGUI PageNumText;

    [SerializeField] private SortingMethod CurrentSortingMethod;
    [SerializeField] private ItemTypes CurrentType;

    private readonly static char[] Characters = new char[27] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '};
    private static Dictionary<char, int> NumCharacterValues;

    public static void InitializeStatics()
    {
        NumCharacterValues = new();
        for (int i = 0; i < Characters.Length; i++)
        {
            NumCharacterValues.Add(Characters[i], i);
        }
    }

    private void Awake()
    {
        CurrentPage = 1;
        CurrentItems = new();
        CurrentlySelected = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIController.Instance.ToggleInventory(false);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            IncrementPage(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            IncrementPage(-1);
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
            GoldAmountText.text = PlayerStats.Instance.MyInventory.Gold.ToString();
            SetInventoryType(ItemTypes.Null);
            gameObject.SetActive(true);
        }
        else
        {
            ClearInventorySlots();
        }
    }

    public void SetInvType(string itemType)
    {
        if(itemType.ToLower() == "favorite")
        {

        }
        else if(itemType.ToLower() == "all")
        {
            SetInventoryType(ItemTypes.Null);
        }
        else if (itemType.ToLower() == "misc")
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

    private void SetInventoryType(ItemTypes itemType)
    {
        if (CurrentType != itemType)
        {
            CurrentType = itemType;
            ClearInventorySlots();
            
            CurrentItems = PlayerStats.Instance.MyInventory.GetItems();
            ItemAmounts = PlayerStats.Instance.MyInventory.GetItemAmounts();

            if (itemType == ItemTypes.Null)
            {
                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    Instantiate(InvSlotPrefab).GetComponent<InventorySlot>().Intialize(CurrentItems[i], ItemAmounts[i], Layout.transform, PlayerStats.Instance.MyInventory);
                }
            }
            else
            {
                for (int i = CurrentItems.Count - 1; i > -1; i--)
                {
                    if(CurrentItems[i].MyItemType != CurrentType)
                    {
                        CurrentItems.RemoveAt(i);
                    }
                }

                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    if (CurrentItems[i].MyItemType == CurrentType)
                    {
                        Instantiate(InvSlotPrefab).GetComponent<InventorySlot>().Intialize(CurrentItems[i], ItemAmounts[i], Layout.transform, PlayerStats.Instance.MyInventory);
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
        for (int i = Layout.transform.childCount - 1; i > -1; i--)
        {
            Destroy(Layout.transform.GetChild(i).gameObject);
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (SlotsPerPage == -1)
        {
            if (InvSlotPrefab)
            {
                if (InvSlotPrefab.TryGetComponent(out RectTransform rectTransform))
                {
                    SlotsPerPage = Mathf.FloorToInt(Layout.GetComponent<RectTransform>().sizeDelta.y / rectTransform.sizeDelta.y);
                }
            }
        }
    }
#endif
}