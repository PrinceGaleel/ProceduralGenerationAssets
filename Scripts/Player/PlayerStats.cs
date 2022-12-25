using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    public static PlayerStats Instance;

    public List<CharacterStats> Enemies;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple player stats instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            CharStandardAwake();
            Enemies = new();
        }
    }

    private void Start()
    {
        TeamsManager.AddTeam("Player Faction", false);
    }

    public override void DecreaseHealth(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseHealth(float amount)
    {
        DefaultIncreaseHealth(amount);
    }

    protected override void Death()
    {

    }
}
