using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public CharacterStats Character;
    public float Damage;
    public int WeaponNum;

    public Vector3 Center;
    public Vector3 HalfExtents;

    public bool CanPierce;
    private List<Transform> TransformHits;
    private readonly Collider[] ColliderHits = new Collider[10];
    private List<CharacterStats> CharacterHits;

    private void Awake()
    {
        TransformHits = new();
        CharacterHits = new();
    }

    private void FixedUpdate()
    {
        if (Character.Anim.GetInteger("WeaponNum") == WeaponNum)
        {
            int numHits = Physics.OverlapBoxNonAlloc(Center + transform.position, HalfExtents, ColliderHits, transform.rotation, World.WeaponMask);

            for(int i = 0; i < numHits; i++)
            {
                if (!TransformHits.Contains(ColliderHits[i].transform))
                {
                    HitBox hitBox = ColliderHits[i].GetComponent<HitBox>();

                    if (CanPierce)
                    {
                        if (!CharacterHits.Contains(hitBox.Character))
                        {
                            hitBox.DecreaseHealth(new(Damage, Character, DamageTypes.Physical));
                            CharacterHits.Add(hitBox.Character);
                        }
                    }
                    else
                    {
                        hitBox.DecreaseHealth(new(Damage, Character, DamageTypes.Physical));
                        enabled = false;
                        return;
                    }
                }
                else
                {
                    TransformHits.Add(ColliderHits[i].transform);
                }

            }
        }
        else
        {
            TransformHits = new();
            CharacterHits = new();
            enabled = false;
        }
    }
}