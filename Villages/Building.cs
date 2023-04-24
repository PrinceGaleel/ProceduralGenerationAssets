using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private Transform Min, Max;
    [SerializeField] private Vector2 HalfExtents;
    public Vector2 GetHalfExtents { get { return HalfExtents; } }

#if UNITY_EDITOR
    [SerializeField] private bool ForceUpdate = true;
    [SerializeField] private bool ShowInfo = true;
    [SerializeField] private Transform Centerer;

    private void OnValidate()
    {
        if (ForceUpdate) ForceUpdate = false;

        if (transform.Find("Min")) Min = transform.Find("Min");
        if (transform.Find("Max")) Max = transform.Find("Max");

        if (Min && Max)
        {
            HalfExtents = new Vector2(Mathf.Abs(Max.position.x - Min.position.x), Mathf.Abs(Max.position.z - Min.position.z)) * 0.5f;
        }

        if (Centerer) Centerer.position = new(Vector3.Lerp(Min.localPosition, Max.localPosition, 0.5f).x, 0, 0);
    }

    private void OnDrawGizmos()
    {
        if (ShowInfo)
        {
            Gizmos.color = Color.red;
            if (Centerer)
            {
                Gizmos.DrawSphere(Centerer.position, 0.5f);
            }

            if (Min && Max)
            {
                Gizmos.DrawWireCube((Min.position + Max.position) / 2, new Vector3(HalfExtents.x, 0, HalfExtents.y) * 2);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere((Min.position + Max.position) / 2, 0.5f);
            }

            Gizmos.color = Color.blue;
            if (Min) Gizmos.DrawSphere(Min.position, 0.5f);
            if (Max) Gizmos.DrawSphere(Max.position, 0.5f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
#endif
    }
}