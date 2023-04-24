using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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

public abstract class BaseAI : BaseController
{
    [Header("Base AI Specific")]
    [SerializeField] protected MultiAimConstraint HeadConstraint;
    [SerializeField] protected BaseCharacter Target;
    [SerializeField] protected AnimatorManager Anim;

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

    public BaseCharacter GetCharacter { get { return MyCharacter; } }
    public int SetTeamID { set { MyCharacter.MyTeamID = value; } }

    protected void Awake()
    {
        Anim.Rest();
    }

    protected virtual void Start()
    {
        SetMainIdling();
    }

    protected void Update()
    {
        CurrentStateAction?.Invoke();
    }

    protected virtual void SetWalking(Vector3 destination)
    {
        Anim.Walk();
        SetMoving(destination, WalkingSpeed);
    }
    protected virtual void SetRunning(Vector3 destination)
    {
        Anim.Run();
        SetMoving(destination, SprintSpeed);
    }
    protected virtual void SetMoving(Vector3 destination, float speed)
    {
        SecondaryState = SecondaryAIStates.Walking;
        SetDestination(destination);
        AgentSpeed = speed;
        AgentStopped = false;
        AgentUpdateRotation = true;
    }

    protected abstract void SetDestination(Vector3 destination);
    protected virtual void SetRotating()
    {
        SecondaryState = SecondaryAIStates.Rotating;
        Anim.Idle();
        AgentStopped = true;
        AgentUpdateRotation = false;
    }
    public virtual void SetMainIdling()
    {
        CurrentStateAction = null;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Waiting;
        Anim.Idle();
        AgentStopped = false;
    }
    public void SetMainIdling(Vector3 destination)
    {
        CurrentStateAction = IdlingAction;
        MainState = MainAIStates.Idling;
        SecondaryState = SecondaryAIStates.Walking;
        SetWalking(destination);
    }
    protected virtual void SetBackingOff()
    {
        Anim.WalkBackwards();
        SecondaryState = SecondaryAIStates.BackingOff;
        ResetAgentPath();
        AgentStopped = false; 
        AgentUpdateRotation = false;
        AgentSpeed = BackingOffSpeed;
    }
    public virtual void SetWaiting()
    {
        SecondaryState = SecondaryAIStates.Waiting;
        Anim.Idle();
        AgentStopped = true;
    }
    public virtual void SetMainPatrolling()
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
            else if (PatrolPoints.Length == 1)
            {
                SetMainIdling(PatrolPoints[0]);
                return;
            }
        }

        SetMainIdling();
    }
    public abstract void SetMainPatrolling(Vector3[] patrolPoints);
    public virtual void SetAttackingChase(BaseCharacter target)
    {
        AttackTimer = AttackCooldown + 1;
        MainState = MainAIStates.Attacking;
        CurrentStateAction = AttackingChaseAction;
        Target = target;
        SetRunning(Target.GetTransform.position);
    }

    public virtual void SetEncircling(BaseCharacter target) 
    {
        Target = target;
        HeadConstraint.enabled = true;
        SetEncircling(); 
    }
    public virtual void SetEncircling()
    {
        Anim.Run();
        AgentSpeed = WalkingSpeed;
        ResetAgentPath();
        AgentStopped = false;
        AgentUpdateRotation = false;
        SecondaryState = SecondaryAIStates.Encircling;
    }
    public virtual void EncirclingAction()
    {
        TargetCheck();

        Vector3 newPos = (Quaternion.AngleAxis(1, new(0, 1, 0)) * (MyTransform.position - Target.GetTransform.position)) + Target.GetTransform.position;
        AgentMove(0.6f * Time.deltaTime * WalkingSpeed * (newPos - MyTransform.position).normalized);
        RotateTowards(newPos);

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

    protected virtual void TargetCheck() { AttackTimer += Time.deltaTime; }

    protected virtual void AttackingChaseAction()
    {
        TargetCheck();
        if (TargetDestinationDistance > 1) SetDestination(Target.GetTransform.position);
        else if (AttackTimer > AttackCooldown)
        {
            if (TargetDistance < AttackRange)
            {
                SetRotating();
                CurrentStateAction = AttackingRotatingAction;
            }
        }
        else if (TargetDistance < MinBackOffDistance)
        {
            SetBackingOff();
            CurrentStateAction = AttackingBackingOffAction;
        }
        else if(TargetDistance < (MinBackOffDistance + MaxBackOffDistance) / 2)
        {
            SetEncircling();
            CurrentStateAction = EncirclingAction;
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
            SetEncircling();
            CurrentStateAction = EncirclingAction;
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

    protected void IdlingAction() { if (SecondaryState == SecondaryAIStates.Walking && DestinationDistance < 1) SetMainIdling(); }
    protected void PatrollingAction()
    {
        if (SecondaryState == SecondaryAIStates.Walking)
        {
            if (DestinationDistance < 1)
            {
                WaitingTimer = 0;
                SetWaiting();
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
        Anim.SetWeaponNumber(0);
        Anim.SetTrigger("AttackOne");
        WaitingTimer = 0;
        AttackTimer = 0;
        Fists[0].ToggleWeapon(true);
    }
    protected bool RotateTowards(Vector3 position)
    {
        float yRotation = Quaternion.LookRotation(position - MyTransform.position).eulerAngles.y;
        MyTransform.rotation = Quaternion.RotateTowards(MyTransform.rotation, Quaternion.Euler(MyTransform.eulerAngles.x, yRotation, MyTransform.eulerAngles.z), Mathf.Min(Time.deltaTime * RotationSpeed, MathF.Abs(yRotation - MyTransform.eulerAngles.y)));
        return true && Mathf.Abs(yRotation - MyTransform.eulerAngles.y) < 5f;
    }
    protected bool RotateTowardsTarget()
    {
        return RotateTowards(Target.GetTransform.position);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (!Anim) Anim = GetComponentInChildren<AnimatorManager>();
    }
#endif
}