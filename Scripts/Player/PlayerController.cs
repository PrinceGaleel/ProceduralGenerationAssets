using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public static Transform PlayerTransform { get { return Instance.transform; } }

    private Gravity _Grav;

    public float SprintSpeed;
    public float MovementSpeed;

    private Vector2 Velocity;

    private int Forward;

    private float AggressionTimer;
    public float AggressionTime = 5;

    private Action LastBool;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple player controller instances detected");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            _Grav = GetComponent<Gravity>();
            Forward = 0;
            AggressionTimer = AggressionTime;
        }
    }

    private void Start()
    {
        LastBool = () => PlayerStats.Instance.Anim.SetBool("Idle", false);
        PlayerStats.Instance.Anim.SetBool("Idle", true);
    }

    /* 
     * -1 - Backwards Walk
     * 0 - Idle
     * 1 - Walk
     * 2 - Run 
     * 3 - Right Strafe
     * 4 - Left Strafe     
     */

    private void Update()
    {        
        Velocity = Input.GetAxis("Vertical") * new Vector2(transform.forward.x, transform.forward.z);
        Velocity += Input.GetAxis("Horizontal") * new Vector2(transform.right.x, transform.right.z);

        if (Input.GetKey(KeyCode.W))
        {
            Velocity *= MovementSpeed;

            if (Forward != 1)
            {
                ChangeState();
                Forward = 1;
                PlayerStats.Instance.Anim.SetBool("Walk", true);
                LastBool = () => PlayerStats.Instance.Anim.SetBool("Walk", false);
            }
        }
        else if (Input.GetKey(KeyCode.S))
        {
            Velocity *= MovementSpeed * 0.6f;

            if (Forward != -1)
            {
                ChangeState();
                Forward = -1;
                PlayerStats.Instance.Anim.SetBool("Backwards Walk", true);
                LastBool = () => PlayerStats.Instance.Anim.SetBool("Backwards Walk", false);
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            Velocity *= MovementSpeed * 0.8f;

            if (Forward != 3)
            {
                ChangeState();
                Forward = 3;
                PlayerStats.Instance.Anim.SetBool("Left Strafe", true);
                LastBool = () => PlayerStats.Instance.Anim.SetBool("Left Strafe", false);
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            Velocity *= MovementSpeed * 0.8f;

            if (Forward != 4)
            {
                ChangeState();
                Forward = 4;
                PlayerStats.Instance.Anim.SetBool("Right Strafe", true);
                LastBool = () => PlayerStats.Instance.Anim.SetBool("Right Strafe", false);
            }
        }
        else
        {
            Velocity = new(0, 0);

            if (Forward != 0)
            {
                ChangeState();
                Forward = 0;
                PlayerStats.Instance.Anim.SetBool("Idle", true);
                LastBool = () => PlayerStats.Instance.Anim.SetBool("Idle", false);
            }
        }

        _Grav.Velocity.x = Velocity.x;
        _Grav.Velocity.z = Velocity.y;

        if (AggressionTimer < AggressionTime)
        {
            AggressionTimer += Time.deltaTime;

            if (!PlayerStats.Instance.Anim.GetBool("IsAggressive"))
            {
                PlayerStats.Instance.Anim.SetBool("IsAggressive", true);
                PlayerStats.Instance.Anim.SetTrigger("ChangeState");
            }
        }
        else if (PlayerStats.Instance.Anim.GetBool("IsAggressive"))
        {
            PlayerStats.Instance.Anim.SetBool("IsAggressive", false);
            PlayerStats.Instance.Anim.SetTrigger("ChangeState");
        }

        if (Input.GetMouseButton(0))
        {
            PlayerStats.Instance.Anim.SetBool("NextAttack", true);
            AggressionTimer = 0;
        }
    }

    private void ChangeState()
    {
        PlayerStats.Instance.Anim.SetTrigger("ChangeState");
        LastBool();
    }
}