using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    [Header("Player Specific")]
    public static PlayerStats Instance;
    public static Transform PlayerTransform { get; private set; }
    public Vector3 SpawnPoint;

    [Header("Gravity")]
    public bool CanMove;
    private bool IsGroundedState;
    private bool IsGroundedPhysics;

    private CharacterController _CharacterController;
    private Transform GroundChecker;
    private Vector2 Velocity;
    private Action EndLastMovement;

    private const float MaxGroundDist = 0.2f;
    private float VerticalAcceleration;
    private float VerticalSpeed;
    public float JumpStrength = 2;

    [Header("Skin Settings")]
    public Transform SkinnedMeshParent;
    public CharacterObjectParents GenderParts;
    public CharacterObjectListsAllGender AllGenderParts;

    [Header("Animation Names")]
    public string RightStrafeAnim;
    public string LeftStrafeAnim;
    public string FallingAnim;

    [Header("Speeds")]
    public float StrafeSpeed;

    [Header("Ohter")]
    private int GravityMask;

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
            SpawnPoint = Chunk.GetPerlinPosition(0, 0) + new Vector3(0, 5, 0);

            CanMove = true;
            Velocity = new();
            VerticalSpeed = 0;
            if (!GroundChecker)
            {
                GroundChecker = new GameObject().transform;
                GroundChecker.SetParent(transform);
                GroundChecker.localPosition = new(0, 0.1f, 0);
                GroundChecker.name = "GroundChecker";
            }

            if (!_CharacterController)
            {
                _CharacterController = GetComponent<CharacterController>();

                if (!_CharacterController)
                {
                    _CharacterController = gameObject.AddComponent<CharacterController>();
                }
            }

            GenderParts = new();
            AllGenderParts = new();
            IsGroundedPhysics = true;
            IsGroundedState = true;

            transform.position = Chunk.GetPerlinPosition(transform.position.x, transform.position.z) + new Vector3(0, 5, 0);

            GravityMask = ~LayerMask.GetMask("Water", "Grass", "Controller", "Weapon", "Arms", "Harvestable", "Hitbox", "Resource");
        }
    }

    private void Start()
    {
        UIController.SetHealthBar(CurrentHealth / MaxHealth);
        UIController.SetManaBar(CurrentHealth / MaxHealth);
        UIController.SetStaminaBar(CurrentHealth / MaxHealth);
        MyTeamID = TeamsManager.AddTeam("Player Faction", false);
        TeamsManager.Teams[MyTeamID].Members.Add(this);

        if (!Anim.GetBool(IdleName))
        {
            Anim.SetBool(IdleName, true);
            EndLastMovement = () => Anim.SetBool(IdleName, false);
        }
        gameObject.SetActive(false);
    }

    private void Update()
    {
        CheckStamina();

        if (CanMove)
        {
            Velocity = (Input.GetAxis("Vertical") * new Vector2(transform.forward.x, transform.forward.z)) + (Input.GetAxis("Horizontal") * new Vector2(transform.right.x, transform.right.z));

            if (Input.GetKey(KeyCode.W))
            {
                Velocity *= NormalSpeed;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                Velocity *= BackingOffSpeed;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                Velocity *= StrafeSpeed;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                Velocity *= StrafeSpeed;
            }
            else
            {
                Velocity = new();
            }

            if (IsGroundedPhysics && Input.GetKey(KeyCode.Space) && VerticalSpeed < 0)
            {
                VerticalAcceleration = 0;
                VerticalSpeed = JumpStrength;
            }
        }
        else
        {
            Velocity = new();
        }

        if (IsGroundedState)
        {
            if(Input.GetMouseButtonDown(0))
            {
                Anim.SetBool("NextAttack", true);
            }

            if (!IsGroundedPhysics)
            {
                IsGroundedState = false;
                EndLastMovement?.Invoke();
                Anim.SetBool(FallingAnim, true);
                EndLastMovement = () => Anim.SetBool(FallingAnim, false);
            }
            else if (CanMove)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    if (!Anim.GetBool(WalkingAnim))
                    {
                        EndLastMovement?.Invoke();
                        Anim.SetBool(WalkingAnim, true);
                        EndLastMovement = () => Anim.SetBool(WalkingAnim, false);
                    }
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    if (!Anim.GetBool(BackwardAnim))
                    {
                        EndLastMovement?.Invoke();
                        Anim.SetBool(BackwardAnim, true);
                        EndLastMovement = () => Anim.SetBool(BackwardAnim, false);
                    }
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    if (!Anim.GetBool(LeftStrafeAnim))
                    {
                        EndLastMovement?.Invoke();
                        Anim.SetBool(WalkingAnim, true);
                        Anim.SetBool(LeftStrafeAnim, true);
                        EndLastMovement = () => StopLeftStrafe();
                    }
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    if (!Anim.GetBool(RightStrafeAnim))
                    {
                        EndLastMovement?.Invoke();
                        Anim.SetBool(WalkingAnim, true);
                        Anim.SetBool(RightStrafeAnim, true);
                        EndLastMovement = () => StopRightStrafe();
                    }
                }
                else if (!Anim.GetBool(IdleName))
                {
                    EndLastMovement?.Invoke();
                    Anim.SetBool(IdleName, true);
                    EndLastMovement = () => Anim.SetBool(IdleName, false);
                }
            }
            else if (!Anim.GetBool(IdleName))
            {
                EndLastMovement?.Invoke();
                Anim.SetBool(IdleName, true);
                EndLastMovement = () => Anim.SetBool(IdleName, false);
            }
        }
        else if (IsGroundedPhysics)
        {
            IsGroundedState = true;
            EndLastMovement?.Invoke();
            Anim.SetBool(IdleName, true);
            EndLastMovement = () => Anim.SetBool(IdleName, false);
        }
    }

    private void FixedUpdate()
    {
        if (Physics.BoxCast(GroundChecker.transform.position, new Vector3(0.2f, 0.1f, 0.125f), -GroundChecker.transform.up, transform.rotation, MaxGroundDist, GravityMask))
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

        _CharacterController.Move(new Vector3(Velocity.x, VerticalSpeed, Velocity.y) * Time.deltaTime);
    }

    private void StopRightStrafe()
    {
        Anim.SetBool(WalkingAnim, false);
        Anim.SetBool(RightStrafeAnim, false);
        EndLastMovement = null;
    }

    private void StopLeftStrafe()
    {
        Anim.SetBool(WalkingAnim, false);
        Anim.SetBool(LeftStrafeAnim, false);
        EndLastMovement = null;
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
        _CharacterController.enabled = false;
        VerticalSpeed = 0;
        Velocity = new();
        transform.position = SpawnPoint;
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        CurrentMana = MaxMana;
        _CharacterController.enabled = true;
    }
}
