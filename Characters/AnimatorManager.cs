using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    [SerializeField] protected Animator Anim;
    [SerializeField] private string[] LastAnimations;
    [SerializeField] private bool Dead = false;

    public bool IsDead { get { return Dead; } }

    [Header("Standard Animation Names")]
    protected const string IdlingAnim = "Idle";
    protected const string RestingAnim = "Resting";
    protected const string WalkingAnim = "Walk Forward";
    protected const string RunningAnim = "Run Forward";
    protected const string BackwardAnim = "Walk Backward";
    protected const string RightStrafeAnim = "Right Strafe";
    protected const string LeftStrafeAnim = "Left Strafe";
    protected const string FallingAnim = "Falling";
    protected const string DeathAnim = "Death";
    protected const string WeaponNumberAnim = "Weapon Number";
    protected const string JumpAnim = "Jump";

    public int WeaponNum { get { return Anim.GetInteger("Weapon Number"); } }

    public void Idle() { if (!Anim.GetBool(IdlingAnim)) ChangeAnimation(IdlingAnim); }
    public void Walk() { if (!Anim.GetBool(WalkingAnim)) ChangeAnimation(WalkingAnim); }
    public void Rest() { if (!Anim.GetBool(RestingAnim)) ChangeAnimation(RestingAnim); }
    public void Run() { if (!Anim.GetBool(RunningAnim)) ChangeAnimation(RunningAnim); }
    public void WalkBackwards() { if (!Anim.GetBool(BackwardAnim)) ChangeAnimation(BackwardAnim); }
    public void StrafeRight() { if (!Anim.GetBool(RightStrafeAnim)) ChangeAnimation(RightStrafeAnim); }
    public void StrafeLeft() { if (!Anim.GetBool(LeftStrafeAnim)) ChangeAnimation(LeftStrafeAnim); }
    public void Fall() { if (!Anim.GetBool(FallingAnim)) ChangeAnimation(FallingAnim); }
    public void Die() { Anim.SetTrigger(DeathAnim); }
    public void SetWeaponNumber(int num) { Anim.SetInteger(WeaponNumberAnim, num); }
    public void SetTrigger(string trigger) { Anim.SetTrigger(trigger); }
    public void SetBool(string boolName, bool state) { Anim.SetBool(boolName, state); }
    public bool GetBool(string boolName) { return Anim.GetBool(boolName); }
    public void Jump() { Anim.SetTrigger(JumpAnim); }
    public void NextAttack() { Anim.SetBool("NextAttack", true); }

    private void ChangeAnimation(params string[] newAnimations)
    {
        StopLastAnimations();
        LastAnimations = (string[])newAnimations.Clone();
        StartNewAnimations();
    }

    private void StartNewAnimations()
    {
        if (LastAnimations != null)
        {
            foreach (string animationName in LastAnimations)
            {
                Anim.SetBool(animationName, true);
            }
        }
    }

    private void StopLastAnimations()
    {
        if (LastAnimations != null)
        {
            foreach (string animationName in LastAnimations)
            {
                Anim.SetBool(animationName, false);
            }

            LastAnimations = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Anim) Anim = GetComponentInChildren<Animator>() ;
    }
#endif
}
