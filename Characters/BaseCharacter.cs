using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Races
{
    Human = 0,
    Wolf = 1,
    Spider
}

[RequireComponent(typeof(Inventory))]
public abstract class BaseCharacter : MonoBehaviour
{
    public enum RegenState
    { 
        Waiting = 0,
        Full = 1,
        Regenerating = 2
    }

    [Header("Base Stats")]
    [SerializeField] protected float CurrentHealth = 100;
    [SerializeField] protected float MaxHealth = 100;

    [SerializeField] protected float CurrentStamina = 100;
    [SerializeField] protected float MaxStamina = 100;

    [SerializeField] protected float CurrentMana = 100;
    [SerializeField] protected float MaxMana = 100;

    [SerializeField] protected float BaseDamage = 10;

    [Header("Levelling")]
    [SerializeField] protected int CurrentExp;
    [SerializeField] protected int ExpToLevelUp;
    [SerializeField] protected int Level;
    [SerializeField] protected int SkillPoints;

    protected const int StartingXP = 100;
    protected const int StartingSkillPoints = 5;

    protected const float ExpMultiplier = 1.2f;
    protected const float SkillPointsMultiplier = 1.1f;

    [Header("Stamina Regen")]
    [SerializeField] private RegenState StaminaRegenState = RegenState.Full;
    [SerializeField] protected float StaminaRegen = 7;
    [SerializeField] protected float StaminaRegenTimer = 0;
    [SerializeField] protected float StaminaRegenTime = 5;

    [Header("Speeds")]
    [SerializeField] protected float WalkingSpeed = 6;
    [SerializeField] protected float SprintSpeed = 9;
    [SerializeField] protected float BackingOffSpeed = 4;
    [SerializeField] protected float StrafeSpeed = 5;
    [SerializeField] protected float DodgeSpeed = 10;
    [SerializeField] protected float RotationSpeed = 120;

    [Header("Standard Animation Names")]
    protected const string IdleName = "Idle";
    protected const string RestingAnim = "Resting";
    protected const string WalkingAnim = "Walk Forward";
    protected const string RunningAnim = "Run Forward";
    protected const string BackwardAnim = "Walk Backward";
    protected const string RightStrafeAnim = "Right Strafe";
    protected const string LeftStrafeAnim = "Left Strafe";
    protected const string FallingAnim = "Falling";
    protected const string DeathAnim = "Death";
    protected const string WeaponNumber = "Weapon Number";

    [Header("Base Containers")]
    [SerializeField] protected Transform MyTransform;
    public Transform GetTransform { get { return MyTransform; } }
    public Animator Anim;
    private string[] LastAnimations;
    public Inventory _Inventory;
    public BluntMeleeWeapon[] Fists;
    public int WeaponNum { get { return Anim.GetInteger("Weapon Number"); } }
    public List<CustomPair<Item, int>> Drops;

    [Header("Other")]
    [SerializeField] protected Races MyRace;
    public Races GetRace { get { return MyRace; } }
    public int MyTeamID;

    protected virtual void Awake()
    {
        Anim.SetInteger("WeaponNum", -1);
    }

    protected void SetRequiredXP()
    {
        ExpToLevelUp = Mathf.FloorToInt(StartingXP * Mathf.Pow(ExpMultiplier, Level));
        SkillPoints = Mathf.FloorToInt(StartingSkillPoints * Mathf.Pow(StartingSkillPoints, SkillPointsMultiplier));
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
            Transform drop = Instantiate(pair.Key.DropPrefab, new Vector3(MyTransform.position.x, MyTransform.position.y + 3, MyTransform.position.z), Quaternion.identity).transform;
            drop.gameObject.AddComponent<DropItem>().Initialize(pair.Key, pair.Value, true);
            drop.eulerAngles = new(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        }
    }

    protected void ChangeAnimation(params string[] newAnimations)
    {
        StopLastAnimations();
        LastAnimations = (string[])newAnimations.Clone();
        StartNewAnimations();
    }

    private void StartNewAnimations()
    {
        if (LastAnimations != null)
        {
            foreach (string animationName in LastAnimations)
            {
                Anim.SetBool(animationName, true);
            }
        }
    }

    private void StopLastAnimations()
    {
        if (LastAnimations != null)
        {
            foreach (string animationName in LastAnimations)
            {
                Anim.SetBool(animationName, false);
            }

            LastAnimations = null;
        }
    }

    public virtual void IncreaseHealth(float amount)
    {
        CurrentHealth = CurrentHealth + Mathf.Abs(amount) < MaxHealth ? CurrentHealth + Mathf.Abs(amount) : MaxHealth;
    }

    public virtual void DecreaseHealth(float amount)
    {
        CurrentHealth = CurrentHealth - Mathf.Abs(amount) > 0 ? CurrentHealth - Mathf.Abs(amount) : 0;

        if (CurrentHealth == 0)
        {
            Death();
        }
    }

    public virtual void IncreaseStamina(float amount)
    {
        CurrentStamina = CurrentStamina + Mathf.Abs(amount) < MaxStamina ? CurrentStamina + Mathf.Abs(amount) : MaxStamina;

        if (CurrentStamina == MaxStamina)
        {
            StaminaRegenState = RegenState.Full;
        }
    }

    public virtual void DecreaseStamina(float amount)
    {
        CurrentStamina = CurrentStamina - Mathf.Abs(amount) > 0 ? CurrentStamina - Mathf.Abs(amount) : 0;
        StaminaRegenState = RegenState.Waiting;
        StaminaRegenTimer = 0;
    }

    public virtual void IncreaseMana(float amount)
    {
        CurrentMana = CurrentMana + Mathf.Abs(amount) < MaxStamina ? CurrentMana + Mathf.Abs(amount) : MaxStamina;
    }

    public virtual void DecreaseMana(float amount)
    {
        CurrentMana = CurrentMana - Mathf.Abs(amount) > 0 ? CurrentMana - Mathf.Abs(amount) : 0;
    }

    protected virtual void Death()
    {
        ChangeAnimation(DeathAnim);
        DropLoot();
        enabled = false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        SetRequiredXP();
        MyTransform = transform;
        if (!Anim) Anim = GetComponent<Animator>();
        _Inventory = gameObject.GetComponent<Inventory>();
        FindHitboxes();

        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentStamina = MaxStamina;


        if (Fists != null)
        {
            if (Fists.Length > 0)
            {
                for (int i = 0; i < Fists.Length; i++) Fists[i].Initialize(this, BaseDamage);
            }
        }
    }

    private void FindHitboxes()
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
#endif 
}