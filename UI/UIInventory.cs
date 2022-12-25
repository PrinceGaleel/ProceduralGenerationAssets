using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIInventory : MonoBehaviour
{
    public GameObject InvSlotPrefab;
    public TextMeshProUGUI Gold;
    public Transform ItemsContainer;
    public VerticalLayoutGroup Layout;
    public TextMeshProUGUI CategoryText;
    private string CurrentType;
    public List<Item> Favourites = new();

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            UIController.Instance.ToggleInventory(false);
        }
    }

    public void ToggleInventory(bool isEnabled)
    {
        gameObject.SetActive(isEnabled);

        if (isEnabled)
        {
            CurrentType = "";
            StringToItemType("all");
            gameObject.SetActive(true);
        }
        else
        {
            ClearInventorySlots();
        }
    }

    public void StringToItemType(string strItemType)
    {
        strItemType = strItemType.ToLower();
        if (strItemType == "favourite" && CurrentType != strItemType)
        {
            ClearInventorySlots();
            int numItems = 0;
            List<Item> items = new(PlayerStats.Instance._Inventory.Items.Keys);
            CurrentType = strItemType;
            CategoryText.text = "FAVOURITES";

            for (int i = 0; i < items.Count; i++)
            {
                if (Favourites.Contains(items[i]))
                {
                    Instantiate(InvSlotPrefab).GetComponent<UIInventorySlot>().Intialize(items[i], PlayerStats.Instance._Inventory.Items[items[i]], ItemsContainer, true, this);
                    numItems++;
                }
            }

            Layout.GetComponent<RectTransform>().sizeDelta = new(Layout.transform.parent.GetComponent<RectTransform>().sizeDelta.x, InvSlotPrefab.GetComponent<RectTransform>().sizeDelta.y * numItems);
        }
        else if (strItemType == "all" && CurrentType != strItemType)
        {
            ClearInventorySlots();
            CategoryText.text = "ALL";
            CurrentType = strItemType;
            List<Item> items = new(PlayerStats.Instance._Inventory.Items.Keys);

            for (int i = 0; i < items.Count; i++)
            {
                bool isFavourited = false;
                if (Favourites.Contains(items[i]))
                {
                    isFavourited = true;
                }

                Instantiate(InvSlotPrefab).GetComponent<UIInventorySlot>().Intialize(items[i], PlayerStats.Instance._Inventory.Items[items[i]], ItemsContainer, isFavourited, this);
            }

            Layout.GetComponent<RectTransform>().sizeDelta = new(Layout.transform.parent.GetComponent<RectTransform>().sizeDelta.x, InvSlotPrefab.GetComponent<RectTransform>().sizeDelta.y * items.Count);
            LayoutRebuilder.MarkLayoutForRebuild(Layout.GetComponent<RectTransform>());
        }
        else
        {
            ItemType itemType = ItemType.Null;

            if (strItemType == "weapon")
            {
                itemType = ItemType.Weapon;
                CategoryText.text = "WEAPONS";
            }
            else if (strItemType == "tool")
            {
                itemType = ItemType.Tool;
                CategoryText.text = "TOOLS";
            }
            else if (strItemType == "equipment")
            {
                itemType = ItemType.Equipment;
                CategoryText.text = "EQUIPMENT";
            }
            else if (strItemType == "crafting")
            {
                itemType = ItemType.Crafting;
                CategoryText.text = "CRAFTING ITEMS";
            }
            else if (strItemType == "misc")
            {
                itemType = ItemType.Misc;
                CategoryText.text = "MISC";
            }
            else if (strItemType == "potion")
            {
                itemType = ItemType.Potion;
                CategoryText.text = "POTIONS";
            }
            else if (strItemType == "food")
            {
                itemType = ItemType.Food;
                CategoryText.text = "FOOD";
            }

            if (CurrentType != itemType.ToString() && itemType != ItemType.Null)
            {
                ClearInventorySlots();
                int numItems = 0;

                List<Item> items = new(PlayerStats.Instance._Inventory.Items.Keys);
                CurrentType = itemType.ToString();

                for (int i = 0; i < PlayerStats.Instance._Inventory.Items.Count; i++)
                {
                    if (items[i]._ItemType.ToString() == CurrentType)
                    {
                        bool isFavourited = false;
                        if (Favourites.Contains(items[i]))
                        {
                            isFavourited = true;
                        }

                        Instantiate(InvSlotPrefab).GetComponent<UIInventorySlot>().Intialize(items[i], PlayerStats.Instance._Inventory.Items[items[i]], ItemsContainer, isFavourited, this);
                        numItems++;
                    }
                }

                Layout.GetComponent<RectTransform>().sizeDelta = new(Layout.transform.parent.GetComponent<RectTransform>().sizeDelta.x, InvSlotPrefab.GetComponent<RectTransform>().sizeDelta.y * numItems);
            }
        }

        LayoutRebuilder.MarkLayoutForRebuild(Layout.GetComponent<RectTransform>());
    }

    private void ClearInventorySlots()
    {
        for (int i = ItemsContainer.childCount - 1; i > -1; i--)
        {
            Destroy(ItemsContainer.GetChild(i).gameObject);
        }
    }
}
