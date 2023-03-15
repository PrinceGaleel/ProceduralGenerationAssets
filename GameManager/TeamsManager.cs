using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;

public class TeamsManager : MonoBehaviour
{
    private static TeamsManager Instance;

    private PopulateEnemiesJob PopulateEnemiesCheck;
    private static int TeamCounter;
    private static Dictionary<int, Team> Teams;

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
            PopulateEnemiesCheck = new();
            PopulateEnemiesCheck.Schedule();
        }
    }

    private void Update()
    {
        if(PopulateEnemiesCheck.IsCompleted)
        {
            PopulateEnemiesCheck = new();
            PopulateEnemiesCheck.Schedule();
        }
    }

    public static void AddMember(int teamID, BaseCharacter member)
    {
        lock (Teams)
        {
            Teams[teamID].Members.Add(member);
        }
    }

    public class PopulateEnemiesJob : SecondaryThreadJob
    {
        public bool IsCompleted { get; private set; }

        public PopulateEnemiesJob()
        {
            IsCompleted = false;
        }

        public override void Execute()
        {
            Dictionary<int, Team> teams = new(Teams);

            foreach (int teamID in teams.Keys)
            {
                List<BaseCharacter> enemies = new();

                foreach (int enemyTeamID in teams[teamID].Enemies)
                {
                    if (enemyTeamID != teamID)
                    {
                        List<BaseCharacter> members;

                        lock (teams[enemyTeamID].Members)
                        {
                            members = new(teams[enemyTeamID].Members);
                        }

                        foreach (BaseCharacter character in members)
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

            IsCompleted = true;
        }
    }

    public static void InitializeStatics()
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

    public static int AddTeam(string factionName, bool isAutoHostile, List<BaseCharacter> members)
    {
        List<int> keys = new(Teams.Keys);
        Teams.Add(TeamCounter, new(factionName, isAutoHostile, members));

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

    public static bool IsEnemy(BaseCharacter characterOne, BaseCharacter characterTwo)
    {
        return Teams[characterOne.MyTeamID].Enemies.Contains(characterTwo.MyTeamID);
    }

    public static bool IsAlly(BaseCharacter characterOne, BaseCharacter characterTwo)
    {
        return Teams[characterOne.MyTeamID].Enemies.Contains(characterTwo.MyTeamID);
    }
        
    private static List<BaseCharacter> GetEnemies(int myTeamID, Vector3 position, float searchDistance)
    {
        List<BaseCharacter> enemies = new();

        lock (Teams[myTeamID].AllEnemies)
        {
            foreach (BaseCharacter enemy in Teams[myTeamID].AllEnemies)
            {
                if (Vector3.Distance(enemy.transform.position, position) < searchDistance)
                {
                    enemies.Add(enemy);
                }
            }
        }

        return enemies;
    }

    public static List<BaseCharacter> GetEnemies(MobDen den)
    {
        return GetEnemies(den.MyTeamID, den.transform.position, den.TerritoryRadius);
    }

    public class Team
    {
        public string FactionName;
        public List<BaseCharacter> Members;
        public List<BaseCharacter> AllEnemies;
        public bool IsAutoHostile;

        public List<int> Enemies;
        public List<int> Allies;

        public Team(string factionName, bool isAutoHostile, List<BaseCharacter> members)
        {
            FactionName = factionName;
            Members = members;
            IsAutoHostile = isAutoHostile;
            Enemies = new();
            Allies = new();
            AllEnemies = new();
        }
    }
}