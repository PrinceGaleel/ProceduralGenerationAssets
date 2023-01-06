using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class DenMob : BasicAI
{
    public MobDen Den;

    protected float TargetDenDistance { get { return Vector3.Distance(Target.transform.position, Den.transform.position); } }

    private void Awake()
    {
        CharStandardAwake();
        AIAwake();
        SetRequiredXP();
        CurrentExp = 0;
    }
    private void Start()
    {
        AIStart();
    }

    private void Update()
    {
        CheckStamina();
        GenericTimer += Time.deltaTime;

        if (MainState == MainAIStates.Idling)
        {
            if (SecondaryState == SecondaryAIStates.Moving && DestinationDistance < 1)
            {
                SetIdling();
            }
        }
        else if (MainState == MainAIStates.Patrolling)
        {
            if (SecondaryState == SecondaryAIStates.Moving)
            {
                RotateTowardsPath();

                if (DestinationDistance < 1)
                {
                    GenericTimer = 0;
                    SetWaiting(WaitingAnim);
                }
            }
            else if (SecondaryState == SecondaryAIStates.Waiting)
            {
                if (GenericTime < GenericTimer)
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
        else if (Target)
        {
            if (TargetDenDistance < Den.TerritoryRadius * 1.2f)
            {
                AttackTimer += Time.deltaTime;

                if (MainState == MainAIStates.Attacking)
                {
                    if (SecondaryState == SecondaryAIStates.Moving)
                    {
                        RotateTowardsPath();

                        if (TargetDistance < AttackRange)
                        {
                            SetRotatingTowardsTarget();
                        }
                        else if (DestinationTargetDistance > AttackRange)
                        {
                            SetRunning(Target.transform.position);
                        }
                    }
                    else if (SecondaryState == SecondaryAIStates.Rotating)
                    {
                        if (TargetDistance < AttackRange)
                        {
                            if (RotateTowardsTarget())
                            {
                                Attack();
                                GenericTimer = 0;
                                GenericTime = 1;
                                MainState = MainAIStates.BackingOff;
                                FollowThroughAttack(Target.transform.position);
                            }
                        }
                        else
                        {
                            SetRunning(Target.transform.position);
                        }
                    }
                    else if (SecondaryState == SecondaryAIStates.Waiting)
                    {
                        if (GenericTimer < GenericTime)
                        {
                            SetRunning(Target.transform.position);
                        }
                    }
                }
                else if (MainState == MainAIStates.BackingOff)
                {
                    if (SecondaryState == SecondaryAIStates.Moving)
                    {
                        RotateAwayFromPath();
                        if (AttackTimer > AttackCooldown)
                        {
                            SetAttacking();
                        }
                        else if (TargetDistance < AttackRange)
                        {
                            SetBackingOff(Target.transform.position);
                        }
                        else if (DestinationDistance < 1)
                        {
                            SetWaiting(IdleName);
                        }
                    }
                    else if (SecondaryState == SecondaryAIStates.Waiting)
                    {
                        RotateTowardsTarget();
                        if (AttackTimer > AttackCooldown)
                        {
                            SetAttacking();
                        }
                        else if (TargetDistance < AttackRange)
                        {
                            SetBackingOff(Target.transform.position);
                        }
                    }
                    else if (SecondaryState == SecondaryAIStates.Null)
                    {
                        RotateTowardsPath();
                        if (GenericTimer > GenericTime)
                        {
                            SetBackingOff(Target.transform.position);
                        }
                    }
                }
            }
            else
            {
                Den.GetNewAttackTarget(this);
            }
        }
        else
        {
            Den.GetNewAttackTarget(this);
        }
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

    public override void DecreaseStamina(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseStamina(float amount)
    {
        DefaultIncreaseHealth(amount);
    }

    public override void DecreaseMana(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseMana(float amount)
    {
        DefaultIncreaseHealth(amount);
    }
}