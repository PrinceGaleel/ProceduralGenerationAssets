using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseCharacter))]
public class BaseController : MonoBehaviour
{
    [SerializeField] protected AnimatorManager AnimManager;
    [SerializeField] protected Transform MyTransform;
    [SerializeField] protected BaseCharacter MyCharacter;
    [SerializeField] protected float BaseDamage = 10;
    public BluntMeleeWeapon[] Fists;

    [Header("Speeds")]
    [SerializeField] protected float WalkingSpeed = 6;
    [SerializeField] protected float SprintMultiplier = 1.5f;
    protected float SprintSpeed { get { return WalkingSpeed * SprintMultiplier; } }
    [SerializeField] protected float BackingOffSpeed = 4;
    [SerializeField] protected float StrafeSpeed = 5;
    [SerializeField] protected float DodgeSpeed = 10;
    [SerializeField] protected float RotationSpeed = 120;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!MyCharacter) MyCharacter = GetComponent<BaseCharacter>();
        if (!AnimManager) AnimManager = GetComponent<AnimatorManager>();
        if (!MyTransform) MyTransform = transform;

        if (MyCharacter)
        {
            if (Fists != null)
            {
                if (Fists.Length > 0)
                {
                    for (int i = 0; i < Fists.Length; i++) Fists[i].Initialize(MyCharacter, BaseDamage);
                }
            }
        }
    }
#endif
}
