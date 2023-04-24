using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class NavDenMob : BaseNavAI, IDenMob
{
    public MobDen Den;

    public float TargetDenDistance { get { return Vector3.Distance(Target.transform.position, Den.transform.position); } }

    public void ISetDen(MobDen den) { Den = den; }

    protected override void TargetCheck()
    {
        base.TargetCheck();

        if (TargetDenDistance > Den.TerritoryRadius * 1.2f) { Target = null; }

        if (!Target)
        {
            SetMainIdling();
            Den.GetNewAttackTarget(this);
        }
    }
}