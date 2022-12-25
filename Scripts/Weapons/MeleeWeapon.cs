using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponTypes
{
    Null,
    Melee,
    Bow
}

public class MeleeWeapon : MonoBehaviour
{
    public CharacterStats Character;
    public float Damage;
    public int WeaponNum;

    public Vector3 Center;
    public Vector3 HalfExtents;

    public WeaponTypes WeaponType;

    public bool CanPierce;
    private List<CharacterStats> Hits;

    private void Awake()
    {
        Hits = new();
    }

    private void Update()
    {
        if (Character.Anim.GetInteger("WeaponNum") == WeaponNum)
        {
            Collider[] colliders = Physics.OverlapBox(Center + transform.position, HalfExtents, transform.rotation, World.ToolMask);

            foreach (Collider collider in colliders)
            {
                if (collider.GetComponent<HitBox>())
                {
                    CharacterStats character = collider.GetComponent<HitBox>().Character;

                    if (character)
                    {
                        if (!CanPierce)
                        {
                            character.DecreaseHealth(Damage);
                            enabled = false;
                            return;
                        }
                        else if (!Hits.Contains(character))
                        {
                            character.DecreaseHealth(Damage);
                            Hits.Add(character);
                        }
                    }
                }
            }
        }
        else
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        Hits = new();
    }
}