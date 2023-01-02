using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamsManager : MonoBehaviour
{
    public static TeamsManager Instance;

    public static int TeamCounter { private set; get; }
    public static Dictionary<int, Team> Teams { get; private set; }

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple teams manager instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
        }
    }

    public static void Initialize()
    {
        Teams = new();
        TeamCounter = 0;
    }

    public static void RemoveTeam(int teamID)
    {
        foreach (int team in Teams.Keys)
        {
            if(Teams[team].Enemies.Contains(teamID))
            {
                Teams[team].Enemies.Remove(teamID);
            }
        }

        Teams.Remove(teamID);
    }

    public static int AddTeam(string factionName, bool isAutoHostile)
    {
        List<int> keys = new(Teams.Keys);
        Teams.Add(TeamCounter, new(factionName, isAutoHostile));

        if (isAutoHostile)
        {
            for (int i = keys.Count - 1; i > -1; i--)
            {
                Teams[TeamCounter].Enemies.Add(keys[i]);
            }
        }

        for (int i = keys.Count - 1; i > -1; i--)
        {
            if (Teams[keys[i]].IsAutoHostile)
            {
                Teams[keys[i]].Enemies.Add(TeamCounter);
            }
        }

        TeamCounter += 1;
        return TeamCounter - 1;
    }

    public static Dictionary<int, Team> GetTeams()
    {
        return Teams;
    }
}

public class Team
{
    public string FactionName;
    public List<CharacterStats> Members;
    public bool IsAutoHostile;
    public List<int> Enemies;

    public Team(string factionName, bool isAutoHostile)
    {
        FactionName = factionName;
        Members = new();
        IsAutoHostile = isAutoHostile;
        Enemies = new();
    }
}