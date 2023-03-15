using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class NavDenMob : BaseNavAI
{
    public MobDen Den;

    protected float TargetDenDistance { get { return Vector3.Distance(Target.transform.position, Den.transform.position); } }

    private void Update()
    {
        CheckStamina();
        CurrentStateAction?.Invoke();
    }

    protected override void TargetCheck()
    {
        base.TargetCheck();
        if (!Target)
        {
            SetIdling();
            Den.GetNewAttackTarget(this);
        }
    }
}