using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobDen : MonoBehaviour
{
    public Transform SpawnPoint;
    public Team MyTeam;
    private DenStates CurrentState;

    public int MaxSpawn;
    public int MinSpawn;

    private int NumToSpawn;
    public float TerritoryRadius;

    public List<DenMob> Mobs;
    public List<CharacterStats> Enemies;

    public GameObject Mob;

    public float SearchTime;
    public float SearchTimer;

    public enum DenStates
    {
        Patrolling,
        Attacking
    }

    private void Awake()
    {
        NumToSpawn = World.Rnd.Next(MinSpawn, MaxSpawn);

        MyTeam = TeamsManager.AddTeam("Mob Den", true);

        Enemies = new();
        Mobs = new(NumToSpawn);
        CurrentState = DenStates.Patrolling;
    }

    private void Start()
    {
        for (int i = 0; i < NumToSpawn; i++)
        {
            Mobs.Add(Instantiate(Mob, SpawnPoint.position, Quaternion.identity).GetComponent<DenMob>());
            Mobs[i].Den = this;

            List<Vector3> patrolPoints = new();
            for (int j = 0; j < patrolPoints.Count; j++)
            {
                float x = (((float)World.Rnd.NextDouble() * TerritoryRadius * 2) - TerritoryRadius) + transform.position.x;
                float z = (((float)World.Rnd.NextDouble() * TerritoryRadius * 2) - TerritoryRadius) + transform.position.z;

                if (Physics.Raycast(new Vector3(x, 1000, z), -transform.up, out RaycastHit hit, 10000, LayerMask.GetMask("Terrain")))
                {
                    patrolPoints.Add(hit.point);
                }
            }

            Mobs[i].SetPatrolling(patrolPoints.ToArray());
        }

        MyTeam.Members.AddRange(Mobs);
    }

    private void Update()
    {
        SearchTimer += Time.deltaTime;

        if (SearchTimer > SearchTime)
        {
            SearchTimer = 0;

            Dictionary<int, Team> teams = TeamsManager.GetTeams();
            foreach (int teamID in teams.Keys)
            {
                if (teams[teamID] != MyTeam)
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
                foreach (DenMob mob in Mobs)
                {
                    mob.SetPatrolling();
                }

                CurrentState = DenStates.Patrolling;
            }
        }
    }

    public void GetNewAttackTarget(DenMob mob)
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
}