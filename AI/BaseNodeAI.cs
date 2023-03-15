using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NodeAgent))]
public abstract class BaseNodeAI : BaseAI
{
    [SerializeField] protected NodeAgent Agent;
    protected override bool AgentUpdateRotation { get { return Agent.UpdateRotation; } set { Agent.UpdateRotation = value; } }
    protected override bool AgentStopped { get { return Agent.IsStopped; } set { Agent.IsStopped = value; } }
    protected override float AgentSpeed { get { return Agent.MovementSpeed; } set { Agent.MovementSpeed = value; } }
    protected override void ResetAgentPath() { Agent.SetIdle(); }
    public override void Teleport(Vector3 position) { Agent.Warp(position); }
    protected override void AgentMove(Vector3 position) { Agent.Move(position); }

    protected override void SetDestination(Vector3 destination)
    {
        if (AINodeManager.IsWalkable(destination))
        {
            Agent.SetDestination(destination);
            Agent.UpdateRotation = true;
            AgentStopped = false;
            CurrentDestination = destination;
        }
    }

    public override void SetPatrolling(Vector3[] patrolPoints)
    {
        List<Vector3> points = new();

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (AINodeManager.IsWalkable(patrolPoints[i]))
            {
                points.Add(patrolPoints[i]);
            }
        }

        PatrolPoints = points.ToArray();
        SetPatrolling();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        Agent = GetComponent<NodeAgent>();
        Agent.MovementSpeed = WalkingSpeed;
        Agent.RotationSpeed = RotationSpeed;
        Agent.UpdateRotation = true;
    }
#endif
}
