using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(CharacterController))]
public class Gravity : MonoBehaviour
{
    private CharacterController CharController;
    private Transform GroundChecker;

    private const float MaxGroundDist = 0.2f;
    private float VerticalAcceleration;
    public float JumpStrength = 2;

    public Vector3 Velocity = new();

    private void Awake()
    {
        if (!GroundChecker)
        {
            GroundChecker = new GameObject().transform;
            GroundChecker.SetParent(transform);
            GroundChecker.localPosition = new(0, -0.9f, 0);
            GroundChecker.name = "GroundChecker";
        }

        if (!CharController)
        {
            CharController = GetComponent<CharacterController>();
        }

        transform.position = new(transform.position.x, 300, transform.position.z);
    }

    private void Start()
    {
        PlayerStats.Instance.Anim.SetBool("IsGrounded", true);
    }

    private void FixedUpdate()
    {
        if (Physics.BoxCast(GroundChecker.transform.position, new Vector3(0.2f, 0.1f, 0.125f), -GroundChecker.transform.up, transform.rotation, MaxGroundDist, ~LayerMask.GetMask("Player", "Harvestable")))
        {
            VerticalAcceleration = Mathf.Clamp(VerticalAcceleration + (Physics.gravity.y * Time.deltaTime), Physics.gravity.y, 50);
            Velocity.y = Mathf.Clamp(Velocity.y + (VerticalAcceleration * Time.deltaTime), Physics.gravity.y, 50);

            PlayerStats.Instance.Anim.SetBool("IsGrounded", true);

            if (Input.GetKey(KeyCode.Space) && Velocity.y < 0)
            {
                VerticalAcceleration = 0;
                Velocity.y = JumpStrength;
            }
        }
        else
        {
            VerticalAcceleration = Mathf.Clamp(VerticalAcceleration + (Physics.gravity.y * Time.deltaTime), -50, 50);
            Velocity.y = Mathf.Clamp(Velocity.y + (VerticalAcceleration * Time.deltaTime), -50, 50);

            PlayerStats.Instance.Anim.SetBool("IsGrounded", false);
        }

        CharController.Move(Velocity * Time.deltaTime);
    }
}