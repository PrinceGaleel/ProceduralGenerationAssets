using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest
{
    public readonly string Name;
    public readonly string Description;
    public readonly int CurrentAmount;
    public readonly int RequiredAmount;

    public readonly int GoldReward;
    public readonly List<CustomTuple<Item, int>> Drops;
    public readonly BaseCharacter Questee;

    public Quest(string name, string description, int requiredAmount)
    {
        Name = name;
        Description = description;
        CurrentAmount = 0;
        RequiredAmount = requiredAmount;
    }

    public virtual void GiveReward()
    {
        PlayerStats.Instance.MyInventory.AddItems(Drops);

    }
}
