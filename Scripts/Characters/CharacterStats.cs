using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Races
{
    Wolf,
    Human
}

public abstract class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float CurrentHealth;
    public float MaxHealth;

    public float CurrentStamina;
    public float MaxStamina;

    public float CurrentMana;
    public float MaxMana;

    public float BaseDamage;

    [Header("Levelling")]
    public int CurrentXP;
    public int ExpToLevelUp;
    public int Level;
    public int SkillPoints;

    public const int StartingXP = 100;
    public const int StartingSkillPoints = 5;

    public const float ExpMultiplier = 1.2f;
    public const float SkillPointsMultiplier = 1.1f;

    [Header("Other")]
    public Races Race;

    public void SetRequiredXP()
    {
        ExpToLevelUp = Mathf.FloorToInt(StartingXP * Mathf.Pow(ExpMultiplier, Level));
        SkillPoints += Mathf.FloorToInt(StartingSkillPoints * Mathf.Pow(StartingSkillPoints, SkillPointsMultiplier));
    }

    [Header("Base Containers")]
    public Animator Anim;
    public Inventory _Inventory;
    public MeleeWeapon[] Fists;
    public CustomDictionary<Item, int> Drops;
    public WeaponTypes CurrentWeaponType;

    private void Awake()
    {
        CharStandardAwake();
        SetRequiredXP();
        CurrentXP = 0;
    }

    protected void CharStandardAwake()
    {
        FindHitboxes();
        if (!Anim)
        {
            Anim = GetComponent<Animator>();
        }

        if(!_Inventory)
        {
            _Inventory = GetComponent<Inventory>();

            if(!_Inventory)
            {
                _Inventory = gameObject.AddComponent<Inventory>();
            }
        }

        if (Fists.Length > 0)
        {
            for (int i = 0; i < Fists.Length; i++)
            {
                Fists[i].WeaponNum = i;
                Fists[i].Character = this;
                Fists[i].Damage = BaseDamage;
            }

            CurrentWeaponType = WeaponTypes.Melee;
        }

        Anim.SetInteger("WeaponNum", -1);
    }

    public void DropLoot()
    {
        foreach(CustomPair<Item, int> pair in Drops.Pairs)
        {
            Transform drop = Instantiate(pair.Key.DropPrefab, new Vector3(transform.position.x, transform.position.y + 3, transform.position.z), Quaternion.identity).transform;
            drop.gameObject.AddComponent<DropItem>().Initialize(pair.Key, pair.Value, true);
            drop.eulerAngles = new(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        }
    }

    public void FindHitboxes()
    {
        HitBox[] hitboxes = GetComponentsInChildren<HitBox>();

        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (!hitboxes[i].Character)
            {
                hitboxes[i].Character = this;
            }
        }
    }

    public void DefaultIncreaseHealth(float amount)
    {
        CurrentHealth = CurrentHealth + Mathf.Abs(amount) < MaxHealth ? CurrentHealth + Mathf.Abs(amount) : MaxHealth;
    }

    public void DefaultDecreaseHealth(float amount)
    {
        CurrentHealth = CurrentHealth - Mathf.Abs(amount) > 0 ? CurrentHealth - Mathf.Abs(amount) : 0;

        if (CurrentHealth == 0)
        {
            Death();
        }
    }

    protected abstract void Death();

    public abstract void IncreaseHealth(float amount);

    public abstract void DecreaseHealth(float amount);

    public void StandardDeath()
    {
        DropLoot();
        enabled = false;
    }
}