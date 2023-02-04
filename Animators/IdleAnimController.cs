using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
public class IdleAnimController : BasicAI
{
    public AnimationInformation[] Animations;
    public int CurrentTransition;

    public MainAIStates CurrentMainState;
    public SecondaryAIStates GenericAIState;

    private float Timer = 0;
    private float _Time;

    [Header("Transition Names")]
    public string WalkingTransition = "SlowWalk";
    public string IdleTransition = "Idle";

    private System.Random Rnd;

    private void Awake()
    {
        AIAwake();

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

        Rnd = new();
    }

    private void Start()
    {
        SetIdle(0);
        _Time = Animations[0].TimeRange.Random(Rnd);
        Timer = _Time + 1;
    }

    private void Update()
    {
        if (GenericAIState == SecondaryAIStates.Walking)
        {
            if (Vector3.Distance(transform.position, CurrentDestination) < 0.5f)
            {
                GenericAIState = SecondaryAIStates.Rotating;
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
        else if (GenericAIState == SecondaryAIStates.Rotating)
        {
            if (transform.rotation == Animations[CurrentTransition].Position.rotation)
            {
                GenericAIState = SecondaryAIStates.Waiting;
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Animations[CurrentTransition].Position.rotation, Time.deltaTime * 2);
            }
        }
        else 
        {
            ChangeAnimation(Animations[CurrentTransition].TransitionName);

            if (Timer > _Time)
            {
                int newTransition = Rnd.Next(Animations.Length);

                if (newTransition != CurrentTransition)
                {
                    SetIdle(newTransition);
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
    }

    public void SetIdle(int newTransition)
    {
        CurrentTransition = newTransition;
        SetIdling(Animations[CurrentTransition].Position.position);
        _Time = Animations[0].TimeRange.Random(Rnd);
        Timer = 0;
    }

    protected override void Death()
    {
        Anim.SetTrigger("Die");
        StandardDeath();
    }

    public override void DecreaseHealth(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseHealth(float amount)
    {
        DefaultIncreaseHealth(amount);
    }

    public override void DecreaseStamina(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseStamina(float amount)
    {
        DefaultIncreaseHealth(amount);
    }

    public override void DecreaseMana(float amount)
    {
        DefaultDecreaseHealth(amount);
    }

    public override void IncreaseMana(float amount)
    {
        DefaultIncreaseHealth(amount);
    }

    [System.Serializable]
    public struct AnimationInformation
    {
        public Transform Position;
        public string TransitionName;
        public Vector2Serializable TimeRange;
    }
}
*/