using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BluntMeleeWeapon : BaseWeapon
{
    [SerializeField] protected Collider MyCollider;
    public void ToggleWeapon(bool isEnabled) { MyCollider.enabled = isEnabled; enabled = isEnabled; }

    protected HashSet<Transform> TransformHits = new();
    protected readonly Collider[] ColliderHits = new Collider[5];

    protected virtual void Awake()
    {
        ToggleWeapon(false);
    }

    public void Initialize(BaseCharacter owner, float damage)
    {
        Damage = damage;
        Character = owner;
    }

    protected virtual void Update()
    {
        if(Character.WeaponNum != WeaponNum)
        {
            ToggleWeapon(false);
        }
    }

    protected virtual void HitCheck(Collider collider)
    {
        HitBox hitBox = collider.GetComponent<HitBox>();
        if (hitBox)
        {
            if (hitBox.DecreaseHealth(new(Damage, Character, DamageTypes.Physical)))
            {
                ToggleWeapon(false);
                return;
            }
        }

        TransformHits.Add(collider.transform);
    }

    protected virtual void OnEnable()
    {
        TransformHits = new();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TransformHits.Contains(other.transform)) HitCheck(other);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        MyCollider = GetComponent<Collider>();
        if (MyCollider) { MyCollider.isTrigger = true; }
    }
#endif
}