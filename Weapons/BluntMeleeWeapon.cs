using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BluntMeleeWeapon : MonoBehaviour
{
    [SerializeField] protected BaseCharacter Character;
    [SerializeField] protected float Damage;
    [SerializeField] protected int WeaponNum;
    [SerializeField] protected Collider MyCollider;
    public void ToggleFist(bool isEnabled) { MyCollider.enabled = isEnabled; enabled = isEnabled; }

    protected HashSet<Transform> TransformHits = new();
    protected readonly Collider[] ColliderHits = new Collider[5];

    protected virtual void Awake()
    {
        ToggleFist(false);
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
            ToggleFist(false);
        }
    }

    protected virtual void HitCheck(Collider collider)
    {
        if (collider.GetComponent<HitBox>() is HitBox hitBox)
        {
            if (hitBox.DecreaseHealth(new(Damage, Character, DamageTypes.Physical)))
            {
                ToggleFist(false);
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