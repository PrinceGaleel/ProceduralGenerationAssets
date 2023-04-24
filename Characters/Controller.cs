
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Controller : BaseController
{
    [Header("Gravity")]
    public bool CanMove;
    [SerializeField] protected bool IsGroundedPhysics;

    [SerializeField] protected CharacterController MyController;
    [SerializeField] protected Transform GroundChecker;
    [SerializeField] protected Vector2 Vec2Velocity;
    public void Move(Vector3 direction) { MyController.Move(direction); }
    public Vector2 Velocity { set { Vec2Velocity = value; } get { return Vec2Velocity; } }

    protected const float MaxGroundDist = 0.2f;
    [SerializeField] private float VerticalAcceleration;
    [SerializeField] protected float VerticalSpeed;
    public float JumpStrength = 2;

    public float GetWalkingSpeed { get { return WalkingSpeed; } }
    public float GetSprintSpeed { get { return SprintSpeed; } }
    public float GetRotationSpeed { get { return RotationSpeed; } }

    public bool EnableController { set { MyController.enabled = value; } }

    private void Update() { MoveController(); }

    protected void MoveController()
    {
        if (Physics.BoxCast(GroundChecker.transform.position, new Vector3(0.2f, 0.1f, 0.125f), -GroundChecker.transform.up, MyTransform.rotation, MaxGroundDist, GameManager.GravityMask) && VerticalSpeed <= 0)
        {
            VerticalAcceleration = 0;
            VerticalSpeed = Physics.gravity.y;
            IsGroundedPhysics = true;
        }
        else
        {
            VerticalAcceleration = Mathf.Clamp(VerticalAcceleration + (Physics.gravity.y * Time.deltaTime), -50, 50);
            VerticalSpeed = Mathf.Clamp(VerticalSpeed + (VerticalAcceleration * Time.deltaTime), -50, 50);
            IsGroundedPhysics = false;
        }

        MyController.Move(new Vector3(Vec2Velocity.x, VerticalSpeed, Vec2Velocity.y) * Time.deltaTime);
    }

    protected void Jump()
    {
        VerticalAcceleration = 0;
        VerticalSpeed = JumpStrength;
        AnimManager.Jump();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        Vec2Velocity = new();
        VerticalAcceleration = 0;
        VerticalSpeed = 0;
        CanMove = true;
        IsGroundedPhysics = false;

        if (!MyController) MyController = GetComponent<CharacterController>();
        if (!GroundChecker) if (transform.Find("Ground Checker")) GroundChecker = transform.Find("Ground Checker");
        if (GroundChecker) GroundChecker.transform.localPosition = new(0, 0.1f, 0);
    }
#endif
}