using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using UnityEngine.Animations.Rigging;

public enum MainAIStates
{
    Idling,
    Patrolling,
    Attacking
}

public enum SecondaryAIStates
{
    Waiting,
    Walking,
    BackingOff,
    Rotating,
    Sprinting,
    Encircling,
    FollowThrough,
    Null
}

public abstract class BasicAI : CharacterStats
{
    public NavMeshAgent Agent;
    public MultiAimConstraint HeadConstraint;
    
    protected CharacterStats Target;

    [Header("Speeds")]
    public float DodgeSpeed;
    public float RotationSpeed;

    [Header("Timer Variables")]
    public float AttackRange;
    protected float AttackTimer;
    public float AttackCooldown;

    protected float WaitingTimer;
    protected float WaitingTime;
    protected int PatrolNum = 0;

    protected MainAIStates MainState;
    protected SecondaryAIStates SecondaryState;
    protected Action CurrentStateAction;
    protected Vector3 CurrentDestination;
    protected string CurrentAnimation;

    protected Vector3[] PatrolPoints;

    protected float TargetDistance { get { return Vector3.Distance(transform.position, Target.transform.position); } }
    protected float DestinationDistance { get { return Vector3.Distance(CurrentDestination, transform.position); } }
    protected float DestinationTargetDistance { get { return Vector3.Distance(CurrentDestination, GetNavMeshPos()); } }
    protected float MaxBackOffDistance { get { return AttackRange * 3; } }
    protected float MinBackOffDistance { get { return AttackRange * 2; } }

    protected void AIAwake()
    {
        CurrentAnimation = RestingAnim;

        if (!Agent)
        {
            Agent = GetComponent<NavMeshAgent>();

            if (!Agent)
            {
                Agent = gameObject.AddComponent<NavMeshAgent>();
            }
        }

        if (!Anim)
        {
            Anim = GetComponent<Animator>();

            if (!Anim)
            {
                Debug.Log("Alert: AI missing anim, " + gameObject.name);
                enabled = false;
                return;
            }
        }

        AttackTimer = 0;
        Agent.speed = NormalSpeed;
        Agent.updateRotation = false;
    }

    protected void AIStart()
    {
        SetIdling();
    }

    protected void SetDestination(Vector3 destination, float maxDistance = 10)
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(destination, out NavMeshHit hit, maxDistance, ~0))
        {
            CurrentDestination = hit.position;
            Agent.SetDestination(CurrentDestination);
            Agent.isStopped = false;
        }
    }

    protected void SetWalking(Vector3 destination)
    {
        ChangeAnimation(WalkingAnim);
        Agent.speed = NormalSpeed;
        SecondaryState = SecondaryAIStates.Walking;
        Agent.isStopped = false;
        SetDestination(destination);
    }

    protected Vector3 GetNavMeshPos()
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10, ~0))
        {
            return hit.position;
        }

        return transform.position;
    }

    protected void SetRunning(Vector3 destination)
    {
        ChangeAnimation(RunningAnim);
        Agent.speed = SprintSpeed;
        SecondaryState = SecondaryAIStates.Walking;
        Agent.isStopped = false;
        SetDestination(destination);
    }

    protected void Attack()
    {
        Anim.SetInteger("WeaponNum", 0);
        Anim.SetTrigger("AttackOne");
        WaitingTimer = 0; 
        AttackTimer = 0;
        Fists[0].enabled = true;
    }

    public void FollowThroughAttack()
    {
        SecondaryState = SecondaryAIStates.FollowThrough;
        Agent.ResetPath();
        WaitingTimer = 0;
        WaitingTime = 1;
    }

    public void SetRotating()
    {
        SecondaryState = SecondaryAIStates.Rotating;
        ChangeAnimation(IdleName);
        Agent.isStopped = true;
    }

    protected abstract void IdleAction();

    protected abstract void PatrolAction();

    protected abstract void AttackAction();

    public void SetIdling()
    {
        CurrentStateAction = IdleAction;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Waiting;
        ChangeAnimation(IdleName);
        Agent.isStopped = true;
    }

    public void SetIdling(Vector3 destination)
    {
        CurrentStateAction = IdleAction;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Walking;
        SetWalking(destination);
    }

    protected void ChangeAnimation(string newAnimation)
    {
        Anim.SetBool(CurrentAnimation, false);
        CurrentAnimation = newAnimation;
        Anim.SetBool(CurrentAnimation, true);
    }

    public void SetBackingOff()
    {
        ChangeAnimation(BackwardAnim);
        SecondaryState = SecondaryAIStates.BackingOff;
        Agent.ResetPath();
        Agent.isStopped = false;
        Agent.speed = BackingOffSpeed;
    }

    protected void BackOff()
    {
        RotateTowardsTarget();
        Agent.Move(BackingOffSpeed * Time.deltaTime * (transform.position - Target.transform.position).normalized);
    }

    protected bool RotateTowardsTarget()
    {
        float yRotation = Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - transform.eulerAngles.y)));
        return true && Mathf.Abs(yRotation - transform.eulerAngles.y) < 5f;
    }

    public void SetWaiting(string animName)
    {
        SecondaryState = SecondaryAIStates.Waiting;
        Agent.isStopped = true;
        ChangeAnimation(animName);
    }

    public void SetPatrolling(Vector3[] patrolPoints)
    {
        List<Vector3> points = new();

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(patrolPoints[i], out NavMeshHit hit, 10, 1))
            {
                points.Add(hit.position);
            }
        }

        PatrolPoints = points.ToArray();
        SetPatrolling();
    }

    public void SetPatrolling()
    {
        if (PatrolPoints != null)
        {
            if (PatrolPoints.Length > 1)
            {
                PatrolNum = 0;
                WaitingTime = 2;
                WaitingTimer = 0;

                CurrentStateAction = PatrolAction;
                MainState = MainAIStates.Patrolling;

                SetWalking(PatrolPoints[0]);
                return;
            }
            else if (PatrolPoints.Length == 1)
            {
                SetIdling(PatrolPoints[0]);
                return;
            }
        }

        SetIdling();
    }

    protected void RotateTowardsPath()
    {
        if (Agent.path.corners.Length > 0)
        {
            float yRotation;

            if (Agent.path.corners.Length > 1)
            {
                yRotation = Quaternion.LookRotation(Agent.path.corners[1] - transform.position).eulerAngles.y;
            }
            else
            {
                yRotation = Quaternion.LookRotation(Agent.destination - transform.position).eulerAngles.y;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - transform.eulerAngles.y)));
        }
    }

    public void SetAttacking(CharacterStats target)
    {        
        AttackTimer = AttackCooldown + 1;
        MainState = MainAIStates.Attacking;
        CurrentStateAction = AttackAction;
        Target = target;
        SetRunning(Target.transform.position);
    }
}
