using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class EquipmentContainer : MonoBehaviour
{
    [SerializeField] private CharacterStats Character;
    [SerializeField] private Inventory Inventory;

    public WeaponEquippable RightHand;
    public WeaponEquippable LeftHand;

    public CustomPair<HitBox[], ArmourEquippable> Helmet;
    public CustomPair<HitBox[], ArmourEquippable> Chestpiece;
    public CustomPair<HitBox[], ArmourEquippable> Arms;
    public CustomPair<HitBox[], ArmourEquippable> Leggings;
    public CustomPair<HitBox[], ArmourEquippable> Feet;

    private void Awake()
    {
        if(!Character)
        {
            Destroy(this);
            enabled = false;
        }
        else if (!Inventory)
        {
            Destroy(this);
            enabled = false;
        }
    }

    public void UnequipLeftHand()
    {

    }

    public void UnequipRightHand()
    {

    }

    public bool EquipLeftHand(WeaponEquippable weapon)
    {
        if(LeftHand)
        {
            UnequipLeftHand();
        }

        return false;
    }

    public bool EquipRightHand(WeaponEquippable weapon)
    {


        return false;
    }

    public bool EquipHelmet(ArmourEquippable helmet)
    {
        if(helmet._ArmourType == ArmourType.Helmet)
        {

        }

        return false;
    }

    public bool EquipChestpiece(ArmourEquippable chestpiece)
    {
        if (chestpiece._ArmourType == ArmourType.Chestpiece)
        {

        }

        return false;
    }

    public bool EquipLeggings(ArmourEquippable leggings)
    {
        if (leggings._ArmourType == ArmourType.Leggings)
        {

        }

        return false;
    }

    public bool EquipArms(ArmourEquippable arms)
    {
        if (arms._ArmourType == ArmourType.Arms)
        {

        }

        return false;
    }

    public bool EquipFeet(ArmourEquippable feet)
    {
        if (feet._ArmourType == ArmourType.Shoes)
        {

        }

        return false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if(!Character) Character = GetComponent<CharacterStats>();
        if(!Inventory) Inventory = GetComponent<Inventory>();
    }
#endif
}
