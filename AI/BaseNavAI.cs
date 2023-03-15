using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseNavAI : BaseAI
{
    [SerializeField] protected NavMeshAgent Agent;
    protected override bool AgentUpdateRotation { get { return Agent.updateRotation; } set { Agent.updateRotation = value; } }
    protected override bool AgentStopped { get { return Agent.isStopped; } set { Agent.isStopped = value; } }
    protected override float AgentSpeed { get { return Agent.speed; } set { Agent.speed = value; } }
    protected override void ResetAgentPath() { Agent.ResetPath(); }
    public override void Teleport(Vector3 position) { Agent.Warp(position); }
    protected override void AgentMove(Vector3 direction) { Agent.Move(direction); }

    protected const float MaxDestinationSearch = 10;
    protected override void SetDestination(Vector3 destination)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, MaxDestinationSearch, ~0))
        {
            AgentUpdateRotation = true;
            CurrentDestination = hit.position;
            Agent.SetDestination(CurrentDestination);
            Agent.isStopped = false;
        }
    }
    public override void SetPatrolling(Vector3[] patrolPoints)
    {
        List<Vector3> points = new();

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (NavMesh.SamplePosition(patrolPoints[i], out NavMeshHit hit, 10, 1))
            {
                points.Add(hit.position);
            }
        }

        PatrolPoints = points.ToArray();
        SetPatrolling();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        Agent = GetComponent<NavMeshAgent>();

        Agent.speed = WalkingSpeed;
        Agent.updateRotation = true;
    }
#endif
}