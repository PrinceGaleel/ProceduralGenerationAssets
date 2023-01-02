using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobDen : MonoBehaviour
{
    public Transform SpawnTransform;
    public Vector3 SpawnPoint;
    public int MyTeamID;
    private DenStates CurrentState;

    public int MinSpawn;
    public int MaxSpawn;

    private int NumToSpawn;
    public float TerritoryRadius;

    public List<DenMob> Mobs;
    public List<CharacterStats> Enemies;

    public GameObject Mob;

    public float SearchTime;
    private float SearchTimer;

    public Vector2 Highest;
    public Vector2 Lowest;

    public enum DenStates
    {
        Patrolling,
        Attacking
    }

    private void Awake()
    {
        NumToSpawn = World.Rnd.Next(MinSpawn, MaxSpawn);

        Enemies = new();
        CurrentState = DenStates.Patrolling;

        if(!SpawnTransform)
        {
            SpawnTransform = transform;
        }

        FoliageManager.AddTreesToRemove(Lowest + new Vector2(transform.position.x, transform.position.z), Highest + new Vector2(transform.position.x, transform.position.z));

        if (!NavMesh.SamplePosition(transform.position, out _, 10, ~0))
        {
            enabled = false;
            NavMeshManager.AddUnreadyToEnable(Chunk.GetChunkPosition(new(transform.position.x, transform.position.z)), this);
        }
    }

    private void Start()
    {
        if (!NavMesh.SamplePosition(transform.position, out _, 10, ~0))
        {
            enabled = false;
            return;
        }

        if (NavMesh.SamplePosition(SpawnTransform.position, out NavMeshHit hit, 10, ~0))
        {
            SpawnPoint = hit.position;
        }
        else
        {
            SpawnPoint = SpawnTransform.position;
        }

        MyTeamID = TeamsManager.AddTeam("Mob Den", true);
        Mobs = new(NumToSpawn);
        for (int i = 0; i < NumToSpawn; i++)
        {
            SpawnMob();
        }

        TeamsManager.Teams[MyTeamID].Members.AddRange(Mobs);
        SearchTimer = 0;
    }

    private void Update()
    {
        SearchTimer += Time.deltaTime;

        if (SearchTimer > SearchTime)
        {
            SearchTimer = 0;

            EnemySearch();
        }

        if (CurrentState == DenStates.Patrolling)
        {
            if (Enemies.Count > 0)
            {
                for (int i = 0; i < Mobs.Count; i++)
                {
                    GetNewAttackTarget(Mobs[i]);
                }

                CurrentState = DenStates.Attacking;
            }
        }
        else if (CurrentState == DenStates.Attacking)
        {
            if (Enemies.Count == 0)
            {
                SetPatrolling();
            }
        }
    }

    public void SpawnMob()
    {
        DenMob mob = Instantiate(Mob, SpawnPoint, Quaternion.identity).GetComponent<DenMob>();
        Mobs.Add(mob);
        mob.Den = this;
        mob.MyTeamID = MyTeamID;
        mob.transform.SetParent(transform);
        AssignPatrolPoints(mob);
    }

    public void AssignPatrolPoints(DenMob mob)
    {
        List<Vector3> patrolPoints = new();

        for (int j = 0; j < 3; j++)
        {
            float x = (((float)World.Rnd.NextDouble() * (TerritoryRadius * 0.7f) * 2) - (TerritoryRadius * 0.7f)) + transform.position.x;
            float z = (((float)World.Rnd.NextDouble() * (TerritoryRadius * 0.7f) * 2) - (TerritoryRadius * 0.7f)) + transform.position.z;

            if(NavMesh.SamplePosition(Chunk.GetPerlinPosition(x,z), out NavMeshHit hit, 10, ~0))
            {
                patrolPoints.Add(hit.position);
            }
        }

        mob.SetPatrolling(patrolPoints.ToArray());
    }

    private void EnemySearch()
    {
        Enemies = new();
        Dictionary<int, Team> teams = TeamsManager.GetTeams();
        foreach (int teamID in teams.Keys)
        {
            if (teamID != MyTeamID)
            {
                foreach (CharacterStats character in teams[teamID].Members)
                {
                    if (Vector3.Distance(character.transform.position, transform.position) < TerritoryRadius)
                    {
                        Enemies.Add(character);
                    }
                }
            }
        }
    }

    private void SetPatrolling()
    {
        foreach (DenMob mob in Mobs)
        {
            mob.SetPatrolling();
        }

        CurrentState = DenStates.Patrolling;
    }

    public void GetNewAttackTarget(DenMob mob)
    {
        EnemySearch();

        if (Enemies.Count > 0)
        {
            mob.Target = Enemies[0];

            for (int j = 1; j < Enemies.Count; j++)
            {
                if (Vector3.Distance(mob.transform.position, mob.Target.transform.position) > Vector3.Distance(mob.transform.position, Enemies[j].transform.position))
                {
                    mob.Target = Enemies[j];
                }
            }

            mob.SetAttacking();
        }
        else
        {
            mob.SetPatrolling();
        }
    }

    private void OnDisable()
    {
        foreach (DenMob mob in Mobs)
        {
            mob.Agent.Warp(SpawnPoint);
            mob.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        foreach (DenMob mob in Mobs)
        {
            mob.gameObject.SetActive(true);
        }

        SetPatrolling();
    }

    private void OnDestroy()
    {
        TeamsManager.RemoveTeam(MyTeamID);
    }
}