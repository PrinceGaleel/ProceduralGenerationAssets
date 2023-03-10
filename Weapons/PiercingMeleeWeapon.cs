using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiercingMeleeWeapon : BluntMeleeWeapon
{
    private List<CharacterStats> CharacterHits;

    protected override void HitCheck(Collider collider)
    {
        if (collider.GetComponent<HitBox>() is HitBox hitBox)
        {
            if (!CharacterHits.Contains(hitBox.Character))
            {
                hitBox.DecreaseHealth(new(Damage, Character, DamageTypes.Physical));
                CharacterHits.Add(hitBox.Character);
            }
        }

        TransformHits.Add(collider.transform);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CharacterHits = new();
    }
}