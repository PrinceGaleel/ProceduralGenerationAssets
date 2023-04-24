using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDenMob
{
    float TargetDenDistance { get; }

    void ISetDen(MobDen den);
}
