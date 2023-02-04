using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Races
{
    Human = 0,
    Wolf = 1
}

public abstract class CharacterStats : MonoBehaviour
{
    public enum RegenState
    { 
        Waiting = 0,
        Full = 1,
        Regenerating = 2
    }

    [Header("Base Stats")]
    public float CurrentHealth;
    public float MaxHealth;

    public float CurrentStamina;
    public float MaxStamina;

    public float CurrentMana;
    public float MaxMana;

    public float BaseDamage;

    [Header("Levelling")]
    public int CurrentExp;
    public int ExpToLevelUp;
    public int Level;
    public int SkillPoints;

    public const int StartingXP = 100;
    public const int StartingSkillPoints = 5;

    public const float ExpMultiplier = 1.2f;
    public const float SkillPointsMultiplier = 1.1f;

    [Header("Regen")]
    private RegenState StaminaRegenState = RegenState.Full;
    public float StaminaRegen = 7;
    public float StaminaRegenTimer = 0;
    public float StaminaRegenTime = 5;

    [Header("Speeds")]
    public float NormalSpeed;
    public float BackingOffSpeed;
    public float SprintSpeed;

    [Header("Animation Names")]
    public string IdleName = "Idle";
    public string RestingAnim = "Resting";
    public string WalkingAnim = "Walk Forward";
    public string RunningAnim = "Run Forward";
    public string BackwardAnim = "Walk Backward";
    public string WaitingAnim = "Idle";

    [Header("Other")]
    public Races Race;
    public int MyTeamID;

    [Header("Base Containers")]
    public Animator Anim;
    public Inventory _Inventory;
    public MeleeWeapon[] Fists;
    public List<CustomPair<Item, int>> Drops;

    private void Awake()
    {
        CharStandardAwake();
        SetRequiredXP();
        CurrentExp = 0;
    }

    protected void CharStandardAwake()
    {
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentStamina = MaxStamina;

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
        }

        Anim.SetInteger("WeaponNum", -1);
    }

    protected void SetRequiredXP()
    {
        ExpToLevelUp = Mathf.FloorToInt(StartingXP * Mathf.Pow(ExpMultiplier, Level));
        SkillPoints += Mathf.FloorToInt(StartingSkillPoints * Mathf.Pow(StartingSkillPoints, SkillPointsMultiplier));
    }

    protected void CheckStamina()
    {
        if(StaminaRegenState == RegenState.Regenerating)
        {
            IncreaseStamina(StaminaRegen * Time.deltaTime);
        }
        else if (StaminaRegenState == RegenState.Waiting)
        {
            StaminaRegenTimer += Time.deltaTime;

            if(StaminaRegenTimer > StaminaRegenTime)
            {
                StaminaRegenState = RegenState.Regenerating;
            }
        }
    }

    public void DropLoot()
    {
        foreach (CustomPair<Item, int> pair in Drops)
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

    public void DefaultIncreaseStamina(float amount)
    {
        CurrentStamina = CurrentStamina + Mathf.Abs(amount) < MaxStamina ? CurrentStamina + Mathf.Abs(amount) : MaxStamina;

        if(CurrentStamina == MaxStamina)
        {
            StaminaRegenState = RegenState.Full;
        }
    }

    public void DefaultDecreaseStamina(float amount)
    {
        CurrentStamina = CurrentStamina - Mathf.Abs(amount) > 0 ? CurrentStamina - Mathf.Abs(amount) : 0;
        StaminaRegenState = RegenState.Waiting;
        StaminaRegenTimer = 0;
    }

    public void DefaultIncreaseMana(float amount)
    {
        CurrentMana = CurrentMana + Mathf.Abs(amount) < MaxStamina ? CurrentMana + Mathf.Abs(amount) : MaxStamina;
    }

    public void DefaultDecreaseMana(float amount)
    {
        CurrentMana = CurrentMana - Mathf.Abs(amount) > 0 ? CurrentMana - Mathf.Abs(amount) : 0;
    }

    protected abstract void Death();

    public abstract void IncreaseHealth(float amount);

    public abstract void DecreaseHealth(float amount);

    public abstract void IncreaseStamina(float amount);

    public abstract void DecreaseStamina(float amount);

    public abstract void IncreaseMana(float amount);

    public abstract void DecreaseMana(float amount);

    public void StandardDeath()
    {
        DropLoot();
        enabled = false;
    }
}