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
    Attacking,
    BackingOff
}

public enum SecondaryAIStates
{
    Waiting,
    Moving,
    Rotating,
    Sprinting,
    Encircling,
    Null
}

public abstract class BasicAI : CharacterStats
{
    public NavMeshAgent Agent;
    public MultiAimConstraint HeadConstraint;
    
    public CharacterStats Target;

    [Header("Speeds")]
    public float DodgeSpeed;
    public float RotationSpeed;

    [Header("Timer Variables")]
    public float AttackRange;
    protected float AttackTimer;
    public float AttackCooldown;

    protected float GenericTimer;
    protected float GenericTime;
    protected int PatrolNum = 0;

    protected MainAIStates MainState;
    protected SecondaryAIStates SecondaryState;
    protected Vector3 CurrentDestination;
    protected string CurrentAnimation;

    protected Vector3[] PatrolPoints;

    protected float TargetDistance { get { return Vector3.Distance(transform.position, Target.transform.position); } }
    protected float DestinationDistance { get { return Vector3.Distance(CurrentDestination, transform.position); } }
    protected float DestinationTargetDistance { get { return Vector3.Distance(CurrentDestination, ExtraUtils.GetNavMeshPos(Target.transform.position)); } }

    protected void SetWalking(Vector3 destination, float maxDistance = 10)
    {
        ChangeAnimation(WalkingAnim);
        Agent.speed = NormalSpeed;
        SecondaryState = SecondaryAIStates.Moving;
        SetDestination(destination, maxDistance);
    }

    protected void SetRunning(Vector3 destination, float maxDistance = 10)
    {
        ChangeAnimation(RunningAnim);
        Agent.speed = SprintSpeed;
        SecondaryState = SecondaryAIStates.Moving;
        SetDestination(destination, maxDistance);
    }

    private void SetDestination(Vector3 destination, float maxDistance)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, maxDistance, ~0))
        {
            CurrentDestination = hit.position;
            Agent.SetDestination(CurrentDestination);
            Agent.isStopped = false;
        }
    }

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

    protected void Attack()
    {
        Anim.SetInteger("WeaponNum", 0);
        Anim.SetTrigger("AttackOne");
        GenericTimer = 0;
        Fists[0].enabled = true;
    }

    public void FollowThroughAttack(Vector3 target, float maxDistance = 10)
    {
        MainState = MainAIStates.BackingOff;
        SecondaryState = SecondaryAIStates.Null;
        SetDestination(target, maxDistance);
    }

    public void SetRotatingTowardsTarget()
    {
        SecondaryState = SecondaryAIStates.Rotating;
        ChangeAnimation(IdleName);
    }

    public void SetIdling()
    {
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Waiting;
        ChangeAnimation(IdleName);
        Agent.isStopped = true;
    }

    public void SetIdling(Vector3 destination)
    {
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Moving;
        SetWalking(destination);
    }

    protected void ChangeAnimation(string newAnimation)
    {
        Anim.SetBool(CurrentAnimation, false);
        CurrentAnimation = newAnimation;
        Anim.SetBool(CurrentAnimation, true);
    }

    public void SetBackingOff(Vector3 target, float maxDistance = 10)
    {
        if (NavMesh.SamplePosition(target - (3 * AttackRange * (target - transform.position).normalized), out NavMeshHit hit, maxDistance, ~0))
        {
            ChangeAnimation(BackwardAnim);
            MainState = MainAIStates.BackingOff;

            CurrentDestination = hit.position;
            Agent.SetDestination(CurrentDestination);

            SecondaryState = SecondaryAIStates.Moving;
            Agent.isStopped = false;
            Agent.speed = BackingOffSpeed;
        }
    }

    protected bool RotateTowardsTarget()
    {
        float yRotation = Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - transform.eulerAngles.y)));
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
            if (NavMesh.SamplePosition(patrolPoints[i], out NavMeshHit hit, 10, 1))
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
                GenericTime = 2;
                GenericTimer = 0;

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

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - transform.eulerAngles.y)));
        }
    }

    protected void RotateAwayFromPath()
    {
        if (Agent.path.corners.Length > 0)
        {
            float yRotation;

            if (Agent.path.corners.Length > 1)
            {
                yRotation = Quaternion.LookRotation(transform.position - Agent.path.corners[1]).eulerAngles.y;
            }
            else
            {
                yRotation = Quaternion.LookRotation(transform.position - Agent.destination).eulerAngles.y;
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - transform.eulerAngles.y)));
        }
    }

    public void SetAttacking()
    {        
        AttackTimer = 0;
        MainState = MainAIStates.Attacking;
        SecondaryState = SecondaryAIStates.Moving;
        SetRunning(Target.transform.position);
    }
}
