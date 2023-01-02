using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    [Header("Player Specific")]
    public static PlayerStats Instance;
    public static Transform PlayerTransform { get; private set; }

    public Vector3 SpawnPoint;

    public List<CharacterStats> Enemies;

    [Header("Skin Settings")]
    public Transform SkinnedMeshParent;
    public CharacterObjectParents GenderParts;
    public CharacterObjectListsAllGender AllGenderParts;

    private void Awake()
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
            PlayerTransform = transform;
            CharStandardAwake();
            Enemies = new();
            gameObject.SetActive(false);
            SpawnPoint = Chunk.GetPerlinPosition(0,0);

            GenderParts = new();
            AllGenderParts = new();
        }
    }

    private void Start()
    {
        UIController.SetHealthBar(CurrentHealth / MaxHealth);
        UIController.SetManaBar(CurrentHealth / MaxHealth);
        UIController.SetStaminaBar(CurrentHealth / MaxHealth);
        MyTeamID = TeamsManager.AddTeam("Player Faction", false);
        TeamsManager.Teams[MyTeamID].Members.Add(this);
    }

    public override void DecreaseHealth(float amount)
    {
        DefaultDecreaseHealth(amount);
        UIController.SetHealthBar(CurrentHealth / MaxHealth);
    }

    public override void IncreaseHealth(float amount)
    {
        DefaultIncreaseHealth(amount);
        UIController.SetHealthBar(CurrentHealth / MaxHealth);
    }

    public override void DecreaseStamina(float amount)
    {
        DefaultDecreaseHealth(amount);
        UIController.SetStaminaBar(CurrentStamina / MaxStamina);
    }

    public override void IncreaseStamina(float amount)
    {
        DefaultIncreaseHealth(amount);
        UIController.SetStaminaBar(CurrentStamina / MaxStamina);
    }

    public override void DecreaseMana(float amount)
    {
        DefaultDecreaseHealth(amount);
        UIController.SetManaBar(CurrentMana / MaxMana);
    }

    public override void IncreaseMana(float amount)
    {
        DefaultIncreaseHealth(amount);
        UIController.SetManaBar(CurrentMana / MaxMana);
    }

    protected override void Death()
    {

    }
}
