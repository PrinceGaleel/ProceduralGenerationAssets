using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    [SerializeField] protected BaseCharacter Owner;
    [SerializeField] protected AnimatorManager AnimManager;
    [SerializedDictionary("Item", "Amount")] public SerializedDictionary<Item, int> Items = new();
    public int Gold;

    [SerializeField] protected string CurrentAnimationSet;

    public void AddItem(Item item, int amount)
    {
        if (Items.ContainsKey(item))
        {
            Items[item] += amount;
        }
        else
        {
            Items.Add(item, amount);
        }
    }

    public void AddItems(List<CustomTuple<Item, int>> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            items.Add(new(items[i].Item1, items[i].Item2));
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
        if(!Owner) Owner = GetComponent<BaseCharacter>();
        if (GetComponent<AnimatorManager>()) AnimManager = GetComponent<AnimatorManager>();
    }
#endif
}
