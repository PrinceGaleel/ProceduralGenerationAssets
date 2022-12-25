using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IdleAnimController : MonoBehaviour
{
    public Animator _Animator;
    public NavMeshAgent Agent;

    public string WalkingTransition = "SlowWalk";
    public string IdleTransition = "Idle";
    public AnimationInformation[] Animations;

    private float Timer = 0;
    private float _Time;

    private int CurrentTransition;
    private bool IsMoving;
    private bool IsTurning;

    private System.Random Rnd;

    private void Awake()
    {
        if (Animations == null)
        {
            Debug.Log("Error: IdleAnimController has no transition names, " + gameObject.name);
            Destroy(this);
            enabled = false;
            return;
        }

        if (Animations.Length == 0)
        {
            Debug.Log("Error: IdleAnimController has no transition names, " + gameObject.name);
            Destroy(this);
            enabled = false;
            return;
        }

        if(!_Animator)
        {
            _Animator = GetComponent<Animator>();

            if (!_Animator)
            {
                Debug.Log("Error: IdleAnimController missing anim, " + gameObject.name);
                Destroy(this);
                enabled = false;
                return;
            }
        }

        if(!Agent)
        {
            Agent = GetComponent<NavMeshAgent>();

            if (!Agent)
            {
                Debug.Log("Error: IdleAnimController missing agent, " + gameObject.name);
                Destroy(this);
                enabled = false;
                return;
            }
        }

        Agent.updateRotation = false;
        Rnd = new();
    }

    private void Start()
    {
        NewIdle(0);
        _Time = Animations[0].TimeRange.Random(Rnd);
        Timer = _Time + 1;
    }

    private void Update()
    {
        if (IsMoving)
        {
            if (Agent.remainingDistance == 0)
            {
                IsMoving = false;
            }
            else
            {
                float yRotation = transform.eulerAngles.y;

                if (Agent.path.corners.Length > 1)
                {
                    yRotation = Quaternion.LookRotation(Agent.path.corners[1] - transform.position).eulerAngles.y;
                }
                else if (Agent.destination != transform.position)
                {
                    yRotation = Quaternion.LookRotation(Agent.destination - transform.position).eulerAngles.y;
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z), Time.deltaTime * 2);
            }
        }
        else if (transform.rotation == Animations[CurrentTransition].Position.rotation)
        {
            _Animator.SetTrigger(Animations[CurrentTransition].TransitionName);

            if (Timer > _Time)
            {
                int newTransition = Rnd.Next(Animations.Length);

                if (newTransition != CurrentTransition)
                {
                    NewIdle(newTransition);
                }
                else
                {
                    _Time = Animations[newTransition].TimeRange.Random(Rnd);
                    Timer = 0;
                }
            }
            else
            {
                Timer += Time.deltaTime;
            }
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Animations[CurrentTransition].Position.rotation, Time.deltaTime * 2);
        }
    }

    public void NewIdle(int newTransition)
    {
        CurrentTransition = newTransition;
        Agent.SetDestination(Animations[CurrentTransition].Position.position);
        _Animator.SetTrigger(WalkingTransition);
        IsMoving = true;
        _Time = Animations[0].TimeRange.Random(Rnd);
        Timer = 0;
    }

    [System.Serializable]
    public struct AnimationInformation
    {
        public Transform Position;
        public string TransitionName;
        public FloatRange TimeRange;
    }
}
