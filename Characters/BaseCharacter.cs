using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Races
{
    Human = 0,
    Wolf = 1,
    Spider = 2
}

[RequireComponent(typeof(AnimatorManager))]
public class BaseCharacter : MonoBehaviour
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
    [SerializeField] protected float SprintDepletion = 7;
    [SerializeField] protected float StaminaRegen = 5;
    [SerializeField] protected float StaminaRegenTimer = 0;
    [SerializeField] protected float StaminaRegenTime = 5;

    [Header("Base Containers")]
    [SerializeField] protected AnimatorManager AnimManager;
    [SerializeField] protected Transform MyTransform;
    public Transform GetTransform { get { return MyTransform; } }
    public Inventory MyInventory;
    public List<CustomTuple<Item, int>> Drops;
    public int WeaponNum { get { return AnimManager.WeaponNum; } }

    [Header("Other")]
    [SerializeField] private bool InCombat;
    [SerializeField] protected Races MyRace;
    public Races GetRace { get { return MyRace; } }
    public int MyTeamID;

    protected virtual void Update()
    {
        CheckStamina();
    }

    protected void SetRequiredXP()
    {
        ExpToLevelUp = Mathf.FloorToInt(StartingXP * Mathf.Pow(ExpMultiplier, Level));
        SkillPoints = Mathf.FloorToInt(StartingSkillPoints * Mathf.Pow(StartingSkillPoints, SkillPointsMultiplier));
    }

    private void CheckStamina()
    {
        if (StaminaRegenState == RegenState.Regenerating)
        {
            IncreaseStamina(StaminaRegen * Time.deltaTime);
        }
        else if (StaminaRegenState == RegenState.Waiting)
        {
            StaminaRegenTimer += Time.deltaTime;

            if (StaminaRegenTimer > StaminaRegenTime)
            {
                StaminaRegenState = RegenState.Regenerating;
            }
        }
    }

    public void Sprint()
    {
        DecreaseStamina(SprintDepletion * Time.deltaTime);

        if (InCombat)
        {
            StaminaRegenState = RegenState.Waiting;
            StaminaRegenTimer = 0;
        }
    }

    public void DropLoot()
    {
        foreach (CustomTuple<Item, int> pair in Drops)
        {
            Transform drop = Instantiate(pair.Item1.DropPrefab, new Vector3(MyTransform.position.x, MyTransform.position.y + 3, MyTransform.position.z), Quaternion.identity).transform;
            drop.gameObject.AddComponent<DropItem>().Initialize(pair.Item1, pair.Item2, true);
            drop.eulerAngles = new(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
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
        AnimManager.Die();
        DropLoot();
        enabled = false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!AnimManager) AnimManager = GetComponentInChildren<AnimatorManager>();
        if (!MyTransform) MyTransform = transform;
        if (!MyInventory) MyInventory = gameObject.GetComponent<Inventory>();

        SetRequiredXP();
        FindHitboxes();        
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