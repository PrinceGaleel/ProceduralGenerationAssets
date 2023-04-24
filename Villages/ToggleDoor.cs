using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleDoor : Interactable
{
    public override string GetInteractInfo { get { return "Press F to Open"; } }
    [SerializeField] private DualDirectionDoor MyDoor;
    [SerializeField] private bool IsRight;

    public override string PlayerInteract()
    {
        MyDoor.Toggle(IsRight);
        return "";
    }

    public override void AIInteract(BaseCharacter character)
    {
        if(!MyDoor.IsOpen)
        {
            MyDoor.Toggle(true);
        }
    }
}
