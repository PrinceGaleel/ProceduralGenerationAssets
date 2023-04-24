using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public abstract string GetInteractInfo { get; }

    public abstract string PlayerInteract();

    public virtual void AIInteract(BaseCharacter character)
    {
        PlayerInteract();
    }
}