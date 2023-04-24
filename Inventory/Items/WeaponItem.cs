using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquipmentContainer;

public enum WeaponTypes
{
    One_Handed_Melee = 0,
    Two_Handed_Melee = 1,
    Bow = 2,
    Rifle = 3,
    One_Handed_Magic = 4,
    Two_Handed_Magic = 5
}

[CreateAssetMenu(menuName = "Item/Weapon")]
public class WeaponItem : Item
{
    public float WeaponDamage;
    public WeaponTypes _WeaponType;

    public override bool Use(params object[] parameters)
    {
        WeaponSlot slot = parameters[0] as WeaponSlot;

        BaseWeapon weapon = slot.Equip(this);

        for (int i = 1; i < parameters.Length; i++)
        {
            if (parameters[i] as WeaponSlot != null) 
            {
                (parameters[i] as WeaponSlot).Occupy(weapon);
            }
        }

        return true;
    }

    public override string ToString()
    {
        return DefaultToString() + "\nWeapon Type: " + _WeaponType.ToString().Remove('_', ' ') + "\nWeapon Damage: " + WeaponDamage;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        MyItemType = ItemTypes.Weapon;
        if (OtherPrefab) if (OtherPrefab.GetComponent<BluntMeleeWeapon>()) WeaponDamage = OtherPrefab.GetComponent<BluntMeleeWeapon>().WeaponDamage;
    }
#endif
}
