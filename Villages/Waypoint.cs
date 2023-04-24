using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Interactable Interact;
    public Waypoint[] Neighbours;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);

        if (Neighbours != null)
        {
            foreach (Waypoint waypoint in Neighbours)
            {
                Gizmos.DrawLine(transform.position, waypoint.transform.position);
            }
        }
    }
#endif
}