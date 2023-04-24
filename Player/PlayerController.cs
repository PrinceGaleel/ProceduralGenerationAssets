using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static UnityEngine.Input;


public class PlayerController : Controller
{
    [SerializeField] private float AttackTime = 5f;
    [SerializeField] private float AttackTimer = 0;

    private void Update()
    {
        if (AnimManager.GetBool("Attacking"))
        {
            if (AttackTimer > AttackTime)
            {
                AnimManager.SetBool("Attacking", false);
                AttackTimer = 0;
            }
            else if (AttackTimer < AttackTime) AttackTimer += Time.deltaTime;
        }

        if (CanMove)
        {
            Vec2Velocity = (GetAxis("Vertical") * new Vector2(MyTransform.forward.x, MyTransform.forward.z)) + (GetAxis("Horizontal") * new Vector2(MyTransform.right.x, MyTransform.right.z));

            if (GetKey(KeyCode.W)) Vec2Velocity *= WalkingSpeed;
            else if (GetKey(KeyCode.S)) Vec2Velocity *= BackingOffSpeed;
            else if (GetKey(KeyCode.A)) Vec2Velocity *= StrafeSpeed;
            else if (GetKey(KeyCode.D)) Vec2Velocity *= StrafeSpeed;
            else Vec2Velocity = Vector2.zero;

            if (IsGroundedPhysics)
            {
                if (GetMouseButtonDown(0))
                {
                    AnimManager.SetBool("NextAttack", true);
                    AnimManager.SetBool("Attacking", true);
                    AttackTimer = 0;
                }

                if (GetKey(KeyCode.Space) && VerticalSpeed < 0)
                {
                    Jump();
                }
            }
        }
        else Vec2Velocity = Vector2.zero;

        if (IsGroundedPhysics)
        {
            if (GetKey(KeyCode.W))
            {
                AnimManager.Run();
            }
            else if (GetKey(KeyCode.S))
            {
                AnimManager.WalkBackwards();
            }
            else if (GetKey(KeyCode.A))
            {
                AnimManager.StrafeLeft();
            }
            else if (GetKey(KeyCode.D))
            {
                AnimManager.StrafeRight();
            }
            else
            {
                AnimManager.Idle();
            }
        }
        else
        {
            AnimManager.Fall();
        }

        MoveController();
    }
}