using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEditor;

public class TeamsManager : MonoBehaviour
{
    private static TeamsManager Instance;

    private static int TeamCounter;
    private static Dictionary<int, Team> Teams;
    private static JobHandle EnemiesCheck;

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
            Teams = new();
            TeamCounter = 0;
            EnemiesCheck = new PopulateEnemiesJob().Schedule();
        }
    }

    private void Update()
    {
        if (EnemiesCheck.IsCompleted) EnemiesCheck = new PopulateEnemiesJob().Schedule();
    }

    public static void AddMember(int teamID, BaseCharacter member)
    {
        lock (Teams)
        {
            Teams[teamID].Members.Add(member);
        }
    }

    public static void RemoveTeam(int teamID)
    {
        List<int> keys = new(Teams.Keys);
        foreach (int key in keys)
        {
            if (Teams.ContainsKey(key))
            {
                if (Teams[key].EnemyTeamIDs.Contains(teamID))
                {
                    Teams[key].EnemyTeamIDs.Remove(teamID);
                }
            }
        }

        Teams.Remove(teamID);
    }

    public static int AddTeam(string factionName, bool isAutoHostile, List<BaseCharacter> members)
    {
        List<int> keys = new(Teams.Keys);
        Teams.Add(TeamCounter, new(TeamCounter, factionName, isAutoHostile, new(members)));

        if (isAutoHostile)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                Teams[TeamCounter].EnemyTeamIDs.Add(keys[i]);
                Teams[keys[i]].EnemyTeamIDs.Add(TeamCounter);
            }
        }
        else
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (isAutoHostile || Teams[keys[i]].IsAutoHostile)
                {
                    Teams[keys[i]].EnemyTeamIDs.Add(TeamCounter);
                    Teams[TeamCounter].EnemyTeamIDs.Add(keys[i]);
                }
            }
        }

        TeamCounter += 1;
        return TeamCounter - 1;
    }

    public static bool IsEnemy(BaseCharacter characterOne, BaseCharacter characterTwo)
    {
        return Teams[characterOne.MyTeamID].EnemyTeamIDs.Contains(characterTwo.MyTeamID);
    }

    public static bool IsAlly(BaseCharacter characterOne, BaseCharacter characterTwo)
    {
        return Teams[characterOne.MyTeamID].EnemyTeamIDs.Contains(characterTwo.MyTeamID);
    }
        
    private static List<BaseCharacter> GetEnemies(int myTeamID, Vector3 position, float searchDistance)
    {
        List<BaseCharacter> enemies = new(Teams[myTeamID].AllEnemies);

        for (int i = enemies.Count -1; i > -1; i--)
        {
            if (Vector3.Distance(enemies[i].transform.position, position) > searchDistance)
            {
                enemies.RemoveAt(i);
            }
        }

        return enemies;
    }

    public static List<BaseCharacter> GetEnemies(MobDen den)
    {
        return GetEnemies(den.MyTeamID, den.transform.position, den.TerritoryRadius);
    }

    private void OnDestroy()
    {
        Teams = null;
        TeamCounter = 0;
    }

    private struct PopulateEnemiesJob : IJob
    {
        public void Execute()
        {
            Dictionary<int, Team> teams = new(Teams);

            foreach (KeyValuePair<int, Team> teamPair in teams)
            {
                List<BaseCharacter> enemies = new();

                foreach (int enemyTeamID in teamPair.Value.EnemyTeamIDs)
                {
                    if (enemyTeamID != teamPair.Key)
                    {
                        List<BaseCharacter> members = new(teams[enemyTeamID].Members);
                        foreach (BaseCharacter character in members)
                        {
                            enemies.Add(character);
                        }
                    }
                }

                if (Teams.ContainsKey(teamPair.Key)) Teams[teamPair.Key].AllEnemies = new(enemies);
            }
        }
    }

    public class Team
    {
        public int ID;
        public string FactionName;
        public ConcurrentBag<BaseCharacter> Members;
        public ConcurrentBag<BaseCharacter> AllEnemies;
        public bool IsAutoHostile;

        public List<int> EnemyTeamIDs;
        public List<int> AllyTeamIDs;

        public Team(int id, string factionName, bool isAutoHostile, ConcurrentBag<BaseCharacter> members)
        {
            ID = id;
            FactionName = factionName;
            Members = members;
            IsAutoHostile = isAutoHostile;
            EnemyTeamIDs = new();
            AllyTeamIDs = new();
            AllEnemies = new();
        }
    }
}