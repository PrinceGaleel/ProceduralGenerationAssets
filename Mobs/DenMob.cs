using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class DenMob : BasicAI
{
    public MobDen Den;

    protected float TargetDenDistance { get { return Vector3.Distance(Target.transform.position, Den.transform.position); } }

    protected override void Awake()
    {
        base.Awake();
        AIAwake();
    }

    private void Start()
    {
        AIStart();
    }

    private void Update()
    {
        CheckStamina();
        CurrentStateAction.Invoke();
    }

    protected override void IdleAction()
    {
        if (SecondaryState == SecondaryAIStates.Walking && DestinationDistance < 1)
        {
            SetIdling();
        }
    }

    protected override void PatrolAction()
    {
        if (SecondaryState == SecondaryAIStates.Walking)
        {
            RotateTowardsPath();

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

    protected override void AttackAction()
    {
        if (Target)
        {
            AttackTimer += Time.deltaTime;
            if (SecondaryState == SecondaryAIStates.Walking)
            {
                RotateTowardsPath();
                if (DestinationTargetDistance > 1)
                {
                    SetDestination(Target.transform.position);
                }
                else if (AttackTimer > AttackCooldown && TargetDistance < AttackRange)
                {
                    SetRotating();
                }
                else if (TargetDistance < MinBackOffDistance)
                {
                    SetBackingOff();
                }
                else if (DestinationDistance < 1)
                {
                    SetWaiting(IdleName);
                }
            }
            else if (SecondaryState == SecondaryAIStates.Rotating)
            {
                if (TargetDistance < AttackRange)
                {
                    if (RotateTowardsTarget())
                    {
                        Attack();
                        FollowThroughAttack();
                    }
                }
                else
                {
                    SetRunning(Target.transform.position);
                }
            }
            else if (SecondaryState == SecondaryAIStates.BackingOff)
            {
                BackOff();
                if (AttackTimer > AttackCooldown || TargetDistance > MaxBackOffDistance)
                {
                    SetRunning(Target.transform.position);
                }
                else if (TargetDistance > MinBackOffDistance)
                {
                    SetWaiting(IdleName);
                }
            }
            else if (SecondaryState == SecondaryAIStates.Waiting)
            {
                RotateTowardsTarget();
                if (AttackTimer > AttackCooldown)
                {
                    SetRunning(Target.transform.position);
                }
                else if (TargetDistance > MaxBackOffDistance)
                {
                    SetRunning(Target.transform.position);
                }
                else if (TargetDistance < MinBackOffDistance)
                {
                    SetBackingOff();
                }
            }
            else if (SecondaryState == SecondaryAIStates.FollowThrough)
            {
                WaitingTimer += Time.deltaTime;
                Agent.Move(SprintSpeed * Time.deltaTime * transform.forward);
                if (WaitingTimer > WaitingTime)
                {
                    SetBackingOff();
                }
            }
        }
        else
        {
            Den.GetNewAttackTarget(this);
        }
    }

    protected override void Death()
    {
        base.Death();
    }

    public override void IncreaseHealth(float amount)
    {
        base.IncreaseHealth(amount);
    }

    public override void DecreaseHealth(float amount)
    {
        base.DecreaseHealth(amount);
    }

    public override void IncreaseStamina(float amount)
    {
        base.IncreaseStamina(amount);
    }

    public override void DecreaseStamina(float amount)
    {
        base.DecreaseStamina(amount);
    }

    public override void IncreaseMana(float amount)
    {
        base.IncreaseMana(amount);
    }

    public override void DecreaseMana(float amount)
    {
        base.DecreaseMana(amount);
    }
}