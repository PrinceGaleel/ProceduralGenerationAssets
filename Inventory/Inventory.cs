using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public Dictionary<Item, int> Items = new();
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
}
