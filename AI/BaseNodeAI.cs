using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NodeAgent))]
public abstract class BaseNodeAI : BaseAI
{
    [Header("Node AI Specific")]
    [SerializeField] protected NodeAgent Agent;

    protected override bool AgentUpdateRotation { get { return Agent.UpdateRotation; } set { Agent.UpdateRotation = value; } }
    protected override bool AgentStopped { get { return Agent.IsStopped; } set { Agent.IsStopped = value; } }
    protected override float AgentSpeed { get { return Agent.CurrentMovementSpeed; } set { Agent.CurrentMovementSpeed = value; } }
    protected override void ResetAgentPath() { Agent.SetIdle(); }
    public override void Teleport(Vector3 position) { Agent.Warp(position); }
    protected override void AgentMove(Vector3 position) { Agent.Move(position); }

    protected override void SetDestination(Vector3 destination)
    {
        Agent.SetDestination(destination);
        Agent.UpdateRotation = true;
        AgentStopped = false;
        CurrentDestination = destination;
    }

    public override void SetMainPatrolling(Vector3[] patrolPoints)
    {
        PatrolPoints = patrolPoints;
        SetMainPatrolling();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if(!Agent) Agent = GetComponent<NodeAgent>();
        if (Agent) Agent.CurrentRotationSpeed = RotationSpeed;
        if (!MyCharacter) MyCharacter = GetComponent<BaseCharacter>();

        Agent.UpdateRotation = true;
    }
#endif
}
