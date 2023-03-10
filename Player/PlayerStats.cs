using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerStats : CharacterStats
{
    public static PlayerStats Instance { get; private set; }

    [Header("Player Specific")]
    [SerializeField] private Transform MyTransform;
    public static Transform PlayerTransform { get { return Instance.MyTransform; } }
    public Vector3 SpawnPoint;

    [Header("Gravity")]
    public bool CanMove;
    private bool IsGroundedState;
    private bool IsGroundedPhysics;

    [SerializeField] private CharacterController Controller;
    [SerializeField] private Transform GroundChecker;
    private Vector2 Vec2Velocity;

    private const float MaxGroundDist = 0.2f;
    private float VerticalAcceleration;
    private float VerticalSpeed;
    public float JumpStrength = 2;

    [Header("Skin Settings")]
    public Transform SkinnedMeshParent;
    public CharacterObjectParents GenderParts;
    public CharacterObjectListsAllGender AllGenderParts;

    protected override void Awake()
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
            SpawnPoint = Chunk.GetPerlinPosition(0, 0) + new Vector3(0, 5, 0);

            CanMove = true;
            Vec2Velocity = new();
            VerticalSpeed = 0;

            GenderParts = new();
            AllGenderParts = new();

            IsGroundedPhysics = true;
            IsGroundedState = true;

            PlayerTransform.position = Chunk.GetPerlinPosition(World.CurrentSaveData.LastPosition.x, World.CurrentSaveData.LastPosition.z) + new Vector3(0, 50, 0);
        }
    }

    private void Start()
    {
        UIController.UpdateHealthBar(CurrentHealth, MaxHealth);
        UIController.UpdateManaBar(CurrentMana, MaxMana);
        UIController.UpdateStaminaBar(CurrentStamina, MaxStamina);
        MyTeamID = TeamsManager.AddTeam("Player Faction", false, new() { this });
        ChangeAnimation(IdleName);
    }

    private void Update()
    {
        CheckStamina();

        if (CanMove)
        {
            Vec2Velocity = (Input.GetAxis("Vertical") * new Vector2(PlayerTransform.forward.x, PlayerTransform.forward.z)) + (Input.GetAxis("Horizontal") * new Vector2(PlayerTransform.right.x, transform.right.z));

            if (Input.GetKey(KeyCode.W)) Vec2Velocity *= NormalSpeed;
            else if (Input.GetKey(KeyCode.S)) Vec2Velocity *= BackingOffSpeed;
            else if (Input.GetKey(KeyCode.A)) Vec2Velocity *= StrafeSpeed;
            else if (Input.GetKey(KeyCode.D)) Vec2Velocity *= StrafeSpeed;
            else Vec2Velocity = Vector2.zero;

            if (IsGroundedPhysics && Input.GetKey(KeyCode.Space) && VerticalSpeed < 0)
            {
                VerticalAcceleration = 0;
                VerticalSpeed = JumpStrength;
            }
        }
        else Vec2Velocity = Vector2.zero;

        if (IsGroundedState)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Anim.SetBool("NextAttack", true);
            }

            if (!IsGroundedPhysics)
            {
                IsGroundedState = false;
                ChangeAnimation(FallingAnim);
            }
            else if (CanMove)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    if (!Anim.GetBool(WalkingAnim)) ChangeAnimation(WalkingAnim);
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    if (!Anim.GetBool(BackwardAnim)) ChangeAnimation(BackwardAnim);
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    if (!Anim.GetBool(LeftStrafeAnim)) ChangeAnimation(WalkingAnim, LeftStrafeAnim);
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    if (!Anim.GetBool(RightStrafeAnim)) ChangeAnimation(WalkingAnim, RightStrafeAnim);
                }
                else if (!Anim.GetBool(IdleName))
                {
                    ChangeAnimation(IdleName);
                }
            }
            else if (!Anim.GetBool(IdleName))
            {
                ChangeAnimation(IdleName);
            }
        }
        else if (IsGroundedPhysics)
        {
            IsGroundedState = true;
            ChangeAnimation(IdleName);
        }
    }

    private void FixedUpdate()
    {
        if (Physics.BoxCast(GroundChecker.transform.position, new Vector3(0.2f, 0.1f, 0.125f), -GroundChecker.transform.up, PlayerTransform.rotation, MaxGroundDist, World.GravityMask))
        {
            VerticalAcceleration = Mathf.Clamp(VerticalAcceleration + (Physics.gravity.y * Time.deltaTime), Physics.gravity.y, 50);
            VerticalSpeed = Mathf.Clamp(VerticalSpeed + (VerticalAcceleration * Time.deltaTime), Physics.gravity.y, 50);

            IsGroundedPhysics = true;
        }
        else
        {
            VerticalAcceleration = Mathf.Clamp(VerticalAcceleration + (Physics.gravity.y * Time.deltaTime), -50, 50);
            VerticalSpeed = Mathf.Clamp(VerticalSpeed + (VerticalAcceleration * Time.deltaTime), -50, 50);

            IsGroundedPhysics = false;
        }

        Controller.Move(new Vector3(Vec2Velocity.x, VerticalSpeed, Vec2Velocity.y) * Time.deltaTime);
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
        Controller.enabled = false;
        VerticalSpeed = 0;
        Vec2Velocity = new();
        PlayerTransform.position = SpawnPoint;
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        CurrentMana = MaxMana;
        Controller.enabled = true;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        Controller = GetComponent<CharacterController>();
        MyTransform = transform;

        if (GroundChecker)
        {
            GroundChecker.localPosition = new(0, 0.1f, 0);
            GroundChecker.name = "GroundChecker";
        }
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