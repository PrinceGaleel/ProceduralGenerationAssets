using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DualDirectionDoor : Interactable
{
    [SerializeField] private Animator Anim;
    public bool IsOpen { get { return Anim.GetBool("OpenLeft") || Anim.GetBool("OpenRight"); } }
    public override string GetInteractInfo { get { return "Press F to Open"; } }

    public void Toggle(bool isRight)
    {
        if (isRight)
        {
            if (Anim.GetBool("OpenLeft")) Anim.SetBool("OpenLeft", false);
            else if (Anim.GetBool("OpenRight")) Anim.SetBool("OpenRight", false);
            else Anim.SetBool("OpenRight", true);
        }
        else if (Anim.GetBool("OpenRight")) Anim.SetBool("OpenRight", false);
        else if (Anim.GetBool("OpenLeft")) Anim.SetBool("OpenLeft", false);
        else Anim.SetBool("OpenLeft", true);
    }

    public override string PlayerInteract()
    {
        Toggle(true);
        return "";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Anim = GetComponent<Animator>();
    }
#endif
}
