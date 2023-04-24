using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentContainer : Inventory
{
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        RightHand.Owner = this;
        RightHand.WeaponNum = 0;

        LeftHand.Owner = this;
        RightHand.WeaponNum = 1;
    }
#endif

    [Header("Equipment")]
    [SerializeField] protected WeaponSlot RightHand;
    [SerializeField] protected WeaponSlot LeftHand;

    public ArmorSlot Helmet;
    public ArmorSlot Chestpiece;
    public ArmorSlot Arms;
    public ArmorSlot Leggings;
    public ArmorSlot Feet;

    [Serializable]
    public class ArmorSlot
    {
        public HitBox[] HitsBoxes;
        public ArmorItem Equippable;
    }

    [Serializable]
    public class WeaponSlot
    {
        public enum SlotState
        {
            Empty,
            Equipped,
            Occupied
        }

        public int WeaponNum;
        public Transform Spawnpoint;
        public BaseWeapon CurrentWeapon;
        public WeaponItem Item;
        public Inventory Owner;
        public SlotState CurrentState = SlotState.Empty;

        public void Unequip()
        {
            if (CurrentState == SlotState.Equipped)
            {
                if (CurrentWeapon)
                {
                    Owner.Items.Add(Item, 1);
                    Destroy(CurrentWeapon.gameObject);
                    CurrentWeapon = null;
                }

                Item = null;
            }
            else
            {
                CurrentWeapon = null;
                Item = null;
            }

            CurrentState = SlotState.Empty;
        }

        public BaseWeapon Equip(WeaponItem weapon)
        {
            Unequip();
            CurrentState = SlotState.Equipped;

            Item = weapon;
            BaseWeapon newWeapon = Instantiate(weapon.OtherPrefab).GetComponent<BaseWeapon>();
            newWeapon.WeaponNumber = WeaponNum;
            CurrentWeapon = newWeapon;

            return newWeapon;
        }

        public void Occupy(BaseWeapon newWeapon)
        {
            Unequip();
            CurrentState = SlotState.Occupied;
            
            newWeapon.WeaponNumber = WeaponNum;
            CurrentWeapon = newWeapon;
        }
    }

    public bool EquipHelmet(ArmorItem helmet)
    {
        if (helmet._ArmourType == ArmourType.Helmet)
        {

        }

        return false;
    }

    public bool EquipChestpiece(ArmorItem chestpiece)
    {
        if (chestpiece._ArmourType == ArmourType.Chestpiece)
        {

        }

        return false;
    }

    public bool EquipLeggings(ArmorItem leggings)
    {
        if (leggings._ArmourType == ArmourType.Leggings)
        {

        }

        return false;
    }

    public bool EquipArms(ArmorItem arms)
    {
        if (arms._ArmourType == ArmourType.Arms)
        {

        }

        return false;
    }

    public bool EquipFeet(ArmorItem feet)
    {
        if (feet._ArmourType == ArmourType.Shoes)
        {

        }

        return false;
    }
}