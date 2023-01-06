using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harvestable : MonoBehaviour
{
    public float MaxUses;
    public float CurrentUses;

    public CustomDictionary<Item, int> Drops;

    public void Harvest(Inventory inventory)
    {
        foreach (CustomPair<Item, int> drop in Drops.Pairs)
        {
            inventory.AddItem(drop.Key, drop.Value);
        }

        CurrentUses -= 1;

        for (int i = 0; i < Drops.Count; i++)
        {
            PlayerStats.Instance._Inventory.AddItem(Drops[i].Key, Drops[i].Value);
        }

        if (CurrentUses <= 0)
        {
            Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }
}