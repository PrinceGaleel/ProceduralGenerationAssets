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
    private List<Collider> ColliderHits;
    private List<CharacterStats> CharacterHits;

    private void Awake()
    {
        ColliderHits = new();
        CharacterHits = new();
    }

    private void Update()
    {
        if (Character.Anim.GetInteger("WeaponNum") == WeaponNum)
        {
            Collider[] colliders = Physics.OverlapBox(Center + transform.position, HalfExtents, transform.rotation, World.WeaponMask);

            foreach (Collider collider in colliders)
            {
                if (!ColliderHits.Contains(collider))
                {
                    HitBox hitBox = collider.GetComponent<HitBox>();

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
                    ColliderHits.Add(collider);
                }
            }
        }
        else
        {
            ColliderHits = new();
            CharacterHits = new();
            enabled = false;
        }
    }
}