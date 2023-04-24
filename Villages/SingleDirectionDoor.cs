using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleDirectionDoor : Interactable
{
    [SerializeField] private Animator Anim;
    public override string GetInteractInfo { get { return "Press F to Open"; } }

    private void Awake()
    {
        Anim.SetBool("IsOpen", false);
    }

    public override string PlayerInteract()
    {
        Anim.SetBool("IsOpen", !Anim.GetBool("IsOpen"));
        return "";
    }

    public override void AIInteract(BaseCharacter character)
    {
        if (!Anim.GetBool("IsOpen"))
        {
            Anim.SetBool("IsOpen", true);
        }
    }
}
