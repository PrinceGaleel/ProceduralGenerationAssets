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

public enum GenericAIStates
{

    Waiting,
    Walking,
    Sprinting,
    BackingOff,
    Encircling,
    Attacking,
    Dodging,
    Blocking
}

public class DenMob : CharacterStats
{
    public NavMeshAgent Agent;
    public MultiAimConstraint HeadConstraint;

    public CharacterStats Target;
    public MobDen Den;

    protected MainAIStates CurrentState;
    protected GenericAIStates GenericState;
    protected Vector3 CurrentDestination;

    [Header("Speeds")]
    public float NormalSpeed;
    public float BackingOffSpeed;
    public float SprintSpeed;
    public float DodgeSpeed;

    [Header("Timer Variables")]
    public float AttackRange;
    public float AttackCooldown;

    protected float GenericTimer;
    protected float GenericTime;
    protected string CurrentAnimation;
    protected int PatrolNum = 0;

    protected Vector3[] PatrolPoints;
    protected float TargetDistance { get { return Vector3.Distance(transform.position, Target.transform.position); } }
    protected float TargetAngle { get { return Vector3.Angle(Target.transform.position - transform.position, transform.forward); } }
    protected float DestinationDistance { get { return Vector3.Distance(CurrentDestination, transform.position); } }

    private void Awake()
    {
        CharStandardAwake();
        AIAwake();
        CurrentAnimation = "Resting";
        SetRequiredXP();
        CurrentXP = 0;
    }

    protected void AIAwake()
    {
        SetIdling();

        if (!Agent)
        {
            Agent = GetComponent<NavMeshAgent>();

            if (!Agent)
            {
                Agent = gameObject.AddComponent<NavMeshAgent>();
            }
        }

        Agent.updateRotation = false;
    }

    private void Update()
    {
        if (CurrentState == MainAIStates.Idling)
        {
            if (GenericState == GenericAIStates.Walking)
            {
                if (DestinationDistance < 1)
                {
                    ChangeAnimation("Resting");
                    GenericState = GenericAIStates.Waiting;
                }
            }
        }
        else if (CurrentState == MainAIStates.Patrolling)
        {
            if (GenericState == GenericAIStates.Walking)
            {
                RotateTowardsPath();

                if (DestinationDistance < 1)
                {
                    ChangeAnimation("Look Around");
                    GenericTimer = 0;
                    GenericState = GenericAIStates.Waiting;
                }
            }
            else if (GenericState == GenericAIStates.Waiting)
            {
                GenericTimer += Time.deltaTime;

                if (GenericTime < GenericTimer)
                {
                    ChangeAnimation("Walk Forward");
                    PatrolNum = (int)Mathf.Repeat(PatrolNum + 1, PatrolPoints.Length - 1);

                    if (PatrolNum == 0)
                    {
                        Array.Reverse(PatrolPoints);
                    }

                    CurrentDestination = PatrolPoints[PatrolNum];
                    Agent.SetDestination(CurrentDestination);
                    GenericState = GenericAIStates.Walking;
                    
                }
            }
        }
        else if (CurrentState == MainAIStates.Attacking)
        {
            GenericTimer += Time.deltaTime;

            if (Target)
            {
                if (GenericState == GenericAIStates.Walking)
                {
                    if (GenericTimer > AttackCooldown)
                    {
                        RotateTowardsTarget();
                        if (TargetAngle < 5f)
                        {
                            Anim.SetInteger("WeaponNum", 0);
                            Anim.SetTrigger("AttackOne");
                            GenericTimer = 0;
                            Fists[0].enabled = false;
                            SetBackingOff();
                        }
                        else if (TargetDistance > AttackRange / 2)
                        {
                            SetDestination(Target.transform.position);
                        }
                    }
                    else
                    {
                        SetBackingOff();
                    }
                }
                else if (GenericState == GenericAIStates.BackingOff)
                {
                    RotateAwayFromPath();
                    if (GenericTimer > AttackCooldown)
                    {
                        SetAttacking();
                    }
                    else
                    {
                        SetBackingOff();
                    }
                }
                else if (GenericState == GenericAIStates.Encircling)
                {
                    RotateTowardsPath();
                }
                else if (GenericState == GenericAIStates.Waiting)
                {
                    if(GenericTimer < GenericTime)
                    {
                        GenericState = GenericAIStates.Walking;
                    }
                }
            }
            else if (Den.Enemies.Count > 0)
            {
                Den.GetNewAttackTarget(this);
            }
            else
            {
                SetPatrolling();
            }
        }
    }

    protected void SetDestination(Vector3 destination)
    {
        if(NavMesh.SamplePosition(destination, out NavMeshHit hit, 10, 0))
        {
            CurrentDestination = hit.position;
            Agent.SetDestination(CurrentDestination);
        }
    }

    public void SetIdling()
    {
        SetIdling(transform.position);
    }

    public void SetIdling(Vector3 destination)
    {
        CurrentState = MainAIStates.Idling;
        GenericState = GenericAIStates.Walking;
        ChangeAnimation("Resting");
        SetDestination(destination);
    }

    protected void ChangeAnimation(string newAnimation)
    {
        Anim.SetBool(CurrentAnimation, false);
        CurrentAnimation = newAnimation;
        Anim.SetBool(CurrentAnimation, true);
    }

    public void SetBackingOff()
    {
        SetBackingOff(Target.transform.position);
    }

    public void SetBackingOff(Vector3 target)
    {
        for (float i = AttackRange; i < 0; i++)
        {
            Vector3 origin = (i  * (transform.position - target).normalized) + transform.position;
            origin.y += 20;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100))
            {
                if (hit.transform.gameObject.layer.ToString() == "Terrain")
                {
                    Agent.SetDestination(hit.point);
                    ChangeAnimation("Walk Backward");
                    GenericState = GenericAIStates.BackingOff;
                    return;
                }
            }
        }
    }

    protected void RotateTowardsTarget()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, 
            Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.eulerAngles.z), Time.deltaTime * 2);
    }

    protected void RotateTowardsPath()
    {
        float yRotation = transform.eulerAngles.y;

        if (Agent.path.corners.Length > 1)
        {
            yRotation = Quaternion.LookRotation(Agent.path.corners[1] - transform.position).eulerAngles.y;
        }
        else if (Agent.destination != transform.position)
        {
            yRotation = Quaternion.LookRotation(Agent.destination - transform.position).eulerAngles.y;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Time.deltaTime * 2);
    }

    protected void RotateAwayFromPath()
    {
        float yRotation = transform.eulerAngles.y;

        if (Agent.path.corners.Length > 1)
        {
            yRotation = Quaternion.LookRotation(Agent.path.corners[1] - transform.position).eulerAngles.y;
        }
        else if (Agent.destination != transform.position)
        {
            yRotation = Quaternion.LookRotation(Agent.destination - transform.position).eulerAngles.y;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Time.deltaTime * 2);
    }

    public void SetPatrolling(Vector3[] patrolPoints)
    {
        List<Vector3> points = new();

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (NavMesh.SamplePosition(patrolPoints[i], out NavMeshHit hit, 10, 0))
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

                CurrentState = MainAIStates.Patrolling;
                GenericState = GenericAIStates.Walking;

                CurrentDestination = PatrolPoints[0];
                Agent.SetDestination(CurrentDestination);
                ChangeAnimation("Walk Forward");
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

    public void SetAttacking()
    {
        GenericTimer = 0;
        GenericTime = -1;
        CurrentState = MainAIStates.Attacking;
        GenericState = GenericAIStates.Attacking;
        SetDestination(Target.transform.position);
    }

    protected override void Death()
    {
        Anim.SetTrigger("Die");
        StandardDeath();
    }

    public override void DecreaseHealth(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseHealth(float amount)
    {
        DefaultIncreaseHealth(amount);
    }
}