using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static GameManager;

[RequireComponent(typeof(PlayerController))]
public class PlayerStats : BaseCharacter
{
    public static PlayerStats Instance { get; private set; }
    
    [Header("Player Specific")]
    public Vector3 SpawnPoint;
    public static Transform PlayerTransform { get { return Instance.MyTransform; } }
    public static bool CanMove { set { Instance.MyController.CanMove = value; } }
    [SerializeField] private PlayerController MyController;

    [Header("Skin Settings")]
    public Transform SkinnedMeshParent;
    public CharacterObjectParents GenderParts;
    public CharacterObjectListsAllGender AllGenderParts;

    protected void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple player stats instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            SpawnPoint = GetPerlinPosition(0, 0) + new Vector3(0, 5, 0);

            GenderParts = new();
            AllGenderParts = new();

            PlayerTransform.position = GetPerlinPosition(CurrentSaveData.LastPosition.x, CurrentSaveData.LastPosition.z) + new Vector3(0, 50, 0);
        }
    }

    private void Start()
    {
        UIController.UpdateHealthBar(CurrentHealth, MaxHealth);
        UIController.UpdateManaBar(CurrentMana, MaxMana);
        UIController.UpdateStaminaBar(CurrentStamina, MaxStamina);
        MyTeamID = TeamsManager.AddTeam("Player Faction", false, new() { this });
        
    }

    public override void IncreaseHealth(float amount)
    {
        base.IncreaseHealth(amount);
        UIController.UpdateHealthBar(CurrentHealth, MaxHealth);
    }

    public override void DecreaseHealth(float amount)
    {
        base.DecreaseHealth(amount);
        UIController.UpdateHealthBar(CurrentHealth, MaxHealth);
    }

    public override void IncreaseStamina(float amount)
    {
        base.IncreaseStamina(amount);
        UIController.UpdateStaminaBar(CurrentStamina, MaxStamina);
    }

    public override void DecreaseStamina(float amount)
    {
        base.IncreaseStamina(amount);
        UIController.UpdateStaminaBar(CurrentStamina, MaxStamina);
    }

    public override void IncreaseMana(float amount)
    {
        base.IncreaseMana(amount);
        UIController.UpdateManaBar(CurrentMana, MaxMana);
    }

    public override void DecreaseMana(float amount)
    {
        base.DecreaseMana(amount);
        UIController.UpdateManaBar(CurrentMana, MaxMana);
    }

    protected override void Death()
    {
        MyController.enabled = false;
        PlayerTransform.position = SpawnPoint;
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        CurrentMana = MaxMana;
        MyController.enabled = true;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (!MyController) MyController = GetComponentInChildren<PlayerController>();
    }

    /*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (GroundChecker) Gizmos.DrawWireCube(GroundChecker.transform.position, new Vector3(0.2f, 0.1f, 0.125f) * 2);
    }
    */
#endif
}