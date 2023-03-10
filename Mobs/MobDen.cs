using System;
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

    public Vector2 Highest;
    public Vector2 Lowest;

    public enum DenStates
    {
        Patrolling,
        Attacking,
        Empty
    }

    private void Awake()
    {
        if(!SpawnTransform)
        {
            SpawnTransform = transform;
        }

        FoliageManager.AddTreesToRemove(Lowest + new Vector2(transform.position.x, transform.position.z), Highest + new Vector2(transform.position.x, transform.position.z));

        if (!NavMesh.SamplePosition(transform.position, out _, 10, ~0))
        {
            NavMeshManager.AddUnreadyToEnable(Chunk.GetChunkPosition(transform.position.x, transform.position.z), this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!NavMesh.SamplePosition(transform.position, out _, 10, ~0))
        {
            enabled = false;
            return;
        }

        MyTeamID = TeamsManager.AddTeam("Mob Den", true, new());

        if (NavMesh.SamplePosition(SpawnTransform.position, out NavMeshHit hit, 10, ~0))
        {
            SpawnPoint = hit.position;
        }
        else
        {
            SpawnPoint = SpawnTransform.position;
        }

        Initialize();
    }

    public void Initialize()
    {
        NumToSpawn = World.Rnd.Next(MinSpawn, MaxSpawn);

        Enemies = new();
        CurrentState = DenStates.Patrolling;

        Mobs = new(NumToSpawn);
        for (int i = 0; i < NumToSpawn; i++)
        {
            SpawnMob();
        }
    }

    private void Update()
    {
        Enemies = TeamsManager.GetEnemies(this);

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
        TeamsManager.AddMember(MyTeamID, mob);
    }

    public void AssignPatrolPoints(DenMob mob)
    {
        List<Vector3> patrolPoints = new();

        for (int j = 0; j < 3; j++)
        {
            float x = ((float)World.Rnd.NextDouble() * (TerritoryRadius * 0.7f)) - (TerritoryRadius * 0.7f) + transform.position.x;
            float z = ((float)World.Rnd.NextDouble() * (TerritoryRadius * 0.7f)) - (TerritoryRadius * 0.7f) + transform.position.z;

            if(NavMesh.SamplePosition(Chunk.GetPerlinPosition(x,z), out NavMeshHit hit, 10, ~0))
            {
                patrolPoints.Add(hit.position);
            }
        }

        mob.SetPatrolling(patrolPoints.ToArray());
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
        if (Enemies.Count > 0)
        {
            CharacterStats target = Enemies[0];

            for (int j = 1; j < Enemies.Count; j++)
            {
                if (Vector3.Distance(mob.transform.position, target.transform.position) > Vector3.Distance(mob.transform.position, Enemies[j].transform.position))
                {
                    target = Enemies[j];
                }
            }

            mob.SetAttacking(target);
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
        for (int i = Mobs.Count - 1; i > -1; i--)
        {
            Destroy(Mobs[i].gameObject, i);
        }

        TeamsManager.RemoveTeam(MyTeamID);
    }
}