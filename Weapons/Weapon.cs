using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] protected Transform HoldPosition;
    [SerializeField] protected BaseCharacter Character;

    [SerializeField] protected float Damage;
    public float WeaponDamage { get { return Damage; } }

    [SerializeField] protected int WeaponNum;
    public int WeaponNumber { set { WeaponNum = value; } }

}
