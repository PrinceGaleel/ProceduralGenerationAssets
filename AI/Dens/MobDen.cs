using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobDen : MonoBehaviour
{
    [SerializeField] private Transform SpawnTransform;
    [SerializeField] private Vector3 SpawnPoint;
    public int MyTeamID;
    [SerializeField] private DenStates CurrentState;

    [SerializeField] private int MinSpawn;
    [SerializeField] private int MaxSpawn;

    [SerializeField] private int NumToSpawn;
    public float TerritoryRadius;

    [SerializeField] private List<BaseAI> Mobs;
    [SerializeField] private List<BaseCharacter> Enemies;

    [SerializeField] private GameObject Mob;

    [SerializeField] private Vector2 Highest;
    [SerializeField] private Vector2 Lowest;

    public enum DenStates
    {
        Patrolling,
        Attacking,
        Empty
    }

    private void Awake()
    {
        if (!SpawnTransform)
        {
            SpawnTransform = transform;
        }

        SpawnPoint = SpawnTransform.position + new Vector3(0, 1, 0);

        FoliageManager.AddTreesToRemove(Lowest + new Vector2(transform.position.x, transform.position.z), Highest + new Vector2(transform.position.x, transform.position.z));

        Vector2Int chunkPos = Chunk.GetChunkPosition(transform.position);
        if (GameManager.ActiveTerrain.ContainsKey(chunkPos))
        {
            if (!GameManager.ActiveTerrain[chunkPos].HasTerrain) AddToWaiting(chunkPos);
        }
        else AddToWaiting(chunkPos);
    }

    private void AddToWaiting(Vector2Int chunkPos)
    {
        if (!Chunk.WaitingForTerrain.ContainsKey(chunkPos)) Chunk.WaitingForTerrain.Add(chunkPos, new() { this });
        else Chunk.WaitingForTerrain[chunkPos].Add(this);
        enabled = false;
    }

    private void Start()
    {
        MyTeamID = TeamsManager.AddTeam("Mob Den", true, new());
        Initialize();
    }

    public void Initialize()
    {
        NumToSpawn = GameManager.Rnd.Next(MinSpawn, MaxSpawn);

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
        BaseAI mob = Instantiate(Mob, SpawnPoint, Quaternion.identity).GetComponent<BaseAI>();
        Mobs.Add(mob);
        (mob as IDenMob).ISetDen(this);
        mob.SetTeamID = MyTeamID;
        mob.transform.SetParent(transform);
        AssignPatrolPoints(mob);
        TeamsManager.AddMember(MyTeamID, mob.GetCharacter);
    }

    public void AssignPatrolPoints(BaseAI mob)
    {
        List<Vector3> patrolPoints = new();

        for (int j = 0; j < 3; j++)
        {
            float x = ((float)GameManager.Rnd.NextDouble() * (TerritoryRadius * 0.7f)) - (TerritoryRadius * 0.7f) + transform.position.x;
            float z = ((float)GameManager.Rnd.NextDouble() * (TerritoryRadius * 0.7f)) - (TerritoryRadius * 0.7f) + transform.position.z;

            if (Physics.Raycast(GameManager.GetPerlinPosition(x, z) + new Vector3(0, 5, 0), Vector3.down, out RaycastHit hit, 10))
            {
                patrolPoints.Add(hit.point);
            }
        }

        mob.SetMainPatrolling(patrolPoints.ToArray());
    }

    private void SetPatrolling()
    {
        foreach (BaseAI mob in Mobs)
        {
            mob.SetMainPatrolling();
        }

        CurrentState = DenStates.Patrolling;
    }

    public void GetNewAttackTarget(BaseAI mob)
    {
        if (Enemies.Count > 0)
        {
            BaseCharacter target = Enemies[0];

            for (int j = 1; j < Enemies.Count; j++)
            {
                if (Vector3.Distance(mob.transform.position, target.transform.position) > Vector3.Distance(mob.transform.position, Enemies[j].transform.position))
                {
                    target = Enemies[j];
                }
            }

            mob.SetAttackingChase(target);
        }
        else
        {
            mob.SetMainPatrolling();
        }
    }

    private void OnDisable()
    {
        foreach (BaseAI mob in Mobs)
        {
            mob.Teleport(SpawnPoint);
            mob.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        foreach (BaseAI mob in Mobs)
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