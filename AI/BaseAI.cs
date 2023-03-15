using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

public abstract class BaseAI : BaseCharacter
{
    public MultiAimConstraint HeadConstraint;
    [SerializeField] protected BaseCharacter Target;

    [Header("Timer Variables")]
    [SerializeField] protected float AttackRange;
    [SerializeField] protected float AttackTimer;
    [SerializeField] protected float AttackCooldown;

    protected float WaitingTimer;
    protected float WaitingTime;
    protected int PatrolNum = 0;

    [SerializeField] protected MainAIStates MainState;
    [SerializeField] protected SecondaryAIStates SecondaryState;
    protected Action CurrentStateAction;
    [SerializeField] protected Vector3 CurrentDestination;
    [SerializeField] protected Vector3[] PatrolPoints;

    protected abstract bool AgentUpdateRotation { get; set; }
    protected abstract float AgentSpeed { set; get; }
    protected abstract bool AgentStopped { set; get; }
    protected abstract void ResetAgentPath();
    public abstract void Teleport(Vector3 position);
    protected abstract void AgentMove(Vector3 direction);

    protected float TargetDestinationDistance { get { return Vector3.Distance(Target.GetTransform.position, CurrentDestination); } }
    protected float TargetDistance { get { return Vector3.Distance(MyTransform.position, Target.GetTransform.position); } }
    protected float DestinationDistance { get { return Vector3.Distance(CurrentDestination, MyTransform.position); } }
    [SerializeField] protected float MinBackOffDistance;
    [SerializeField] protected float MaxBackOffDistance;

    protected override void Awake()
    {
        base.Awake();
        ChangeAnimation(RestingAnim);
    }

    protected virtual void Start()
    {
        SetIdling();
    }

    protected virtual void SetWalking(Vector3 destination)
    {
        SetMoving(destination, WalkingSpeed, WalkingAnim);
    }
    protected virtual void SetRunning(Vector3 destination)
    {
        SetMoving(destination, SprintSpeed, RunningAnim);
    }
    protected virtual void SetMoving(Vector3 destination, float speed, string animName)
    {
        ChangeAnimation(animName);
        SecondaryState = SecondaryAIStates.Walking;
        SetDestination(destination);
        AgentSpeed = speed;
        AgentStopped = false;
    }

    protected abstract void SetDestination(Vector3 destination);
    protected virtual void SetRotating()
    {
        SecondaryState = SecondaryAIStates.Rotating;
        ChangeAnimation(IdleName);
        AgentStopped = true;
        AgentUpdateRotation = false;
    }
    public virtual void SetIdling()
    {
        CurrentStateAction = null;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Waiting;
        ChangeAnimation(IdleName);
        AgentStopped = false;
    }
    public void SetIdling(Vector3 destination)
    {
        CurrentStateAction = IdlingAction;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Walking;
        SetWalking(destination);
    }
    protected virtual void SetBackingOff()
    {
        ChangeAnimation(BackwardAnim);
        SecondaryState = SecondaryAIStates.BackingOff;
        ResetAgentPath();
        AgentStopped = false;
        AgentSpeed = BackingOffSpeed;
    }
    public virtual void SetWaiting(string animName)
    {
        SecondaryState = SecondaryAIStates.Waiting;
        ChangeAnimation(animName);
        AgentStopped = true;
    }
    public virtual void SetPatrolling()
    {
        if (PatrolPoints != null)
        {
            if (PatrolPoints.Length > 1)
            {
                PatrolNum = 0;
                WaitingTime = 2;
                WaitingTimer = 0;

                CurrentStateAction = PatrollingAction;
                MainState = MainAIStates.Patrolling;

                AgentUpdateRotation = true;
                SetWalking(PatrolPoints[0]);
                return;
            }
            else if(PatrolPoints.Length == 1)
            {
                SetIdling(PatrolPoints[0]);
                return;
            }
        }

        SetIdling();
    }
    public abstract void SetPatrolling(Vector3[] patrolPoints);
    public virtual void SetAttackingChase(BaseCharacter target)
    {
        AttackTimer = AttackCooldown + 1;
        MainState = MainAIStates.Attacking;
        CurrentStateAction = AttackingChaseAction;
        Target = target;
        SetRunning(Target.GetTransform.position);
    }

    protected virtual void TargetCheck() { AttackTimer += Time.deltaTime; }

    protected virtual void AttackingChaseAction()
    {
        TargetCheck();

        if (TargetDestinationDistance > 1) SetDestination(Target.GetTransform.position);
        else if (AttackTimer > AttackCooldown && TargetDistance < AttackRange)
        {
            SetRotating();
            CurrentStateAction = AttackingRotatingAction;
        }
        else if (TargetDistance < MinBackOffDistance)
        {
            SetBackingOff();
            CurrentStateAction = AttackingBackingOffAction;
        }
        else if (DestinationDistance < 1)
        {
            SetWaiting(IdleName);
            CurrentStateAction = AttackingWaitingAction;
        }
    }
    protected virtual void AttackingRotatingAction()
    {
        TargetCheck();

        if (TargetDistance < AttackRange)
        {
            if (RotateTowardsTarget())
            {
                BasicMeleeAttack();
                SetFollowThrough();
                CurrentStateAction = AttackingFollowThroughAction;
            }
        }
        else
        {
            SetRunning(Target.GetTransform.position);
            CurrentStateAction = AttackingChaseAction;
        }
    }
    protected virtual void AttackingBackingOffAction()
    {
        TargetCheck();
        BackingOffAction();
        if (AttackTimer > AttackCooldown || TargetDistance > MaxBackOffDistance)
        {
            SetRunning(Target.GetTransform.position);
            CurrentStateAction = AttackingChaseAction;
        }
        else if (TargetDistance > MinBackOffDistance)
        {
            SetWaiting(IdleName);
            CurrentStateAction = AttackingWaitingAction;
        }
    }
    protected virtual void AttackingWaitingAction()
    {
        TargetCheck();
        RotateTowardsTarget();
        if (AttackTimer > AttackCooldown || TargetDistance > MaxBackOffDistance)
        {
            SetRunning(Target.GetTransform.position);
            CurrentStateAction = AttackingChaseAction;
        }
        else if (TargetDistance < MinBackOffDistance)
        {
            SetBackingOff();
            CurrentStateAction = AttackingBackingOffAction;
        }
    }
    protected virtual void AttackingFollowThroughAction()
    {
        TargetCheck();
        WaitingTimer += Time.deltaTime;
        AgentMove(SprintSpeed * Time.deltaTime * MyTransform.forward);
        if (WaitingTimer > WaitingTime)
        {
            SetBackingOff();
            CurrentStateAction = AttackingBackingOffAction;
        }
    }

    protected void IdlingAction() { if (SecondaryState == SecondaryAIStates.Walking && DestinationDistance < 1) SetIdling(); }
    protected void PatrollingAction()
    {
        if (SecondaryState == SecondaryAIStates.Walking)
        {
            if (DestinationDistance < 1)
            {
                WaitingTimer = 0;
                SetWaiting(IdleName);
            }
        }
        else if (SecondaryState == SecondaryAIStates.Waiting)
        {
            WaitingTimer += Time.deltaTime;

            if (WaitingTime < WaitingTimer)
            {
                PatrolNum = (int)Mathf.Repeat(PatrolNum + 1, PatrolPoints.Length - 1);

                if (PatrolNum == 0)
                {
                    Array.Reverse(PatrolPoints);
                }

                SetWalking(PatrolPoints[PatrolNum]);
            }
        }
    }
    protected virtual void SetFollowThrough()
    {
        SecondaryState = SecondaryAIStates.FollowThrough;
        WaitingTimer = 0;
        WaitingTime = 1;
        ResetAgentPath();
    }
    protected virtual void BackingOffAction()
    {
        RotateTowardsTarget();
        AgentMove(BackingOffSpeed * Time.deltaTime * (MyTransform.position - Target.GetTransform.position).normalized);
    }
    protected void BasicMeleeAttack()
    {
        Anim.SetInteger(WeaponNumber, 0);
        Anim.SetTrigger("AttackOne");
        WaitingTimer = 0;
        AttackTimer = 0;
        Fists[0].ToggleFist(true);
    }
    protected bool RotateTowardsTarget()
    {
        float yRotation = Quaternion.LookRotation(Target.GetTransform.position - MyTransform.position).eulerAngles.y;
        MyTransform.rotation = Quaternion.RotateTowards(MyTransform.rotation, Quaternion.Euler(MyTransform.eulerAngles.x, yRotation, MyTransform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - MyTransform.eulerAngles.y)));
        return true && Mathf.Abs(yRotation - MyTransform.eulerAngles.y) < 5f;
    }
}