using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private CharacterStats Owner;
    [SerializeField] private EquipmentContainer MyEquipmentContainer;
    private readonly Dictionary<Item, int> Items = new();
    public int Gold;

    public void AddItem(Item item, int amount)
    {
        if (Items.ContainsKey(item))
        {
            Items[item] += amount;
        }
        else
        {
            Items[item] = amount;
        }
    }

    public void AddItems(PairList<Item, int> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            items.Add(items[i].Key, items[i].Value);
        }
    }

    public List<Item> GetItems()
    {
        return new(Items.Keys);
    }

    public List<int> GetItemAmounts()
    {
        return new(Items.Values);
    }

    public bool RemoveItem(Item item, int amount)
    {
        if (Items.ContainsKey(item))
        {
            if (Items[item] - amount < 0)
            {
                Debug.Log("Error:  Inventory does not have enough of item: " + item.name);
                return false;
            }
            else if (Items[item] - amount == 0)
            {
                Items.Remove(item);
            }
            else
            {
                Items[item] -= amount;
            }

            return true;
        }

        Debug.Log("Error: Inventory does not have item: " + item.name);
        return false;
    }

    public bool GiveItem(Item item, int amount, Inventory inv)
    {
        if(RemoveItem(item, amount))
        {
            inv.AddItem(item, amount);
        }

        return false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (GetComponent<EquipmentContainer>()) MyEquipmentContainer = GetComponent<EquipmentContainer>();
        if (!Owner) Owner = GetComponent<CharacterStats>();
    }
#endif
}
