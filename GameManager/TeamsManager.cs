using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;

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

    private void Update()
    {
        PopulateAllEnemies();
    }

    private static void PopulateAllEnemies()
    {
        Dictionary<int, Team> teams = new(Teams);

        foreach (int teamID in teams.Keys)
        {
            List<CharacterStats> enemies = new();

            foreach (int enemyTeamID in teams[teamID].Enemies)
            {
                if (enemyTeamID != teamID)
                {
                    List<CharacterStats> members;

                    lock (teams[enemyTeamID].Members)
                    {
                        members = new(teams[enemyTeamID].Members);
                    }

                    foreach (CharacterStats character in members)
                    {
                        enemies.Add(character);
                    }
                }
            }

            lock (Teams[teamID].AllEnemies)
            {
                Teams[teamID].AllEnemies = enemies;
            }
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
            if (Teams[keys[i]].IsAutoHostile || isAutoHostile)
            {
                Teams[keys[i]].Enemies.Add(TeamCounter);
            }
        }

        TeamCounter += 1;
        return TeamCounter - 1;
    }

    public static bool IsEnemy(CharacterStats characterOne, CharacterStats characterTwo)
    {
        return Teams[characterOne.MyTeamID].Enemies.Contains(characterTwo.MyTeamID);
    }

    public static bool IsAlly(CharacterStats characterOne, CharacterStats characterTwo)
    {
        return Teams[characterOne.MyTeamID].Enemies.Contains(characterTwo.MyTeamID);
    }
        
    private static List<CharacterStats> GetEnemies(int myTeamID, Vector3 position, float searchDistance)
    {
        List<CharacterStats> enemies = new();

        lock (Teams[myTeamID].AllEnemies)
        {
            foreach (CharacterStats enemy in Teams[myTeamID].AllEnemies)
            {
                if (Vector3.Distance(enemy.transform.position, position) < searchDistance)
                {
                    enemies.Add(enemy);
                }
            }
        }

        return enemies;
    }

    public static List<CharacterStats> GetEnemies(MobDen den)
    {
        return GetEnemies(den.MyTeamID, den.transform.position, den.TerritoryRadius);
    }

    public class Team
    {
        public string FactionName;
        public List<CharacterStats> Members;
        public List<CharacterStats> AllEnemies;
        public bool IsAutoHostile;

        public List<int> Enemies;
        public List<int> Allies;

        public Team(string factionName, bool isAutoHostile)
        {
            FactionName = factionName;
            Members = new();
            IsAutoHostile = isAutoHostile;
            Enemies = new();
            Allies = new();
            AllEnemies = new();
        }
    }
}