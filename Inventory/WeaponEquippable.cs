using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponTypes
{
    One_Handed_Melee = 0,
    Two_Handed_Melee = 1,
    Bow = 2,
    Rifle = 3,
    One_Handed_Magic = 4,
    Two_Handed_Magic = 5
}

[CreateAssetMenu(menuName = "Equippable/Weapon")]
public class WeaponEquippable : Item
{
    public float WeaponDamage;
    public WeaponTypes _WeaponType;

    public override string ToString()
    {
        return DefaultToString() + "\nWeapon Type: " + _WeaponType.ToString().Remove('_', ' ') + "\nWeapon Damage: " + WeaponDamage;
    }
}
