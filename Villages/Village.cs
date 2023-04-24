using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static StructureCreator;
using static PerlinData;
using static GameManager;

public class Village : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private float SphereSize = 2;

    private void OnDrawGizmosSelected()
    {
        /*
        foreach (Road road in Roads)
        {
            Gizmos.DrawSphere(GetPerlinPosition(road.StartPosition), SphereSize);
            Gizmos.DrawSphere(GetPerlinPosition(road.TargetPosition), SphereSize);

            Gizmos.DrawLine(GetPerlinPosition(road.StartPosition), GetPerlinPosition(road.TargetPosition));
        }
        */
    }
#endif
}


[Serializable]
public readonly struct Road
{
    public enum RoadTypes
    {
        Main_Road,
        Side_Road
    }

    public readonly RoadTypes RoadType;
    public readonly Vector2 StartPosition;
    public readonly Vector2 TargetPosition;

    public Vector2 TargetDirection { get { return (TargetPosition - StartPosition).normalized; } }

    public readonly Vector2 Max;
    public readonly Vector2 Min;

    public readonly float LineGradient;
    public readonly float YIntercept;

    public Road(Vector3 pointA, Vector3 pointB, RoadTypes roadType)
    {
        RoadType = roadType;
        StartPosition = new(pointA.x, pointA.z);
        TargetPosition = new(pointB.x, pointB.z);

        LineGradient = (StartPosition.y - TargetPosition.y) / (StartPosition.x - TargetPosition.x);
        YIntercept = StartPosition.y - (LineGradient * StartPosition.x);

        Max = new();
        Min = new();

        if (StartPosition.x > TargetPosition.x)
        {
            Max.x = StartPosition.x;
            Min.x = TargetPosition.x;
        }
        else
        {
            Max.x = TargetPosition.x;
            Min.x = StartPosition.x;
        }

        if (StartPosition.y > TargetPosition.y)
        {
            Max.y = StartPosition.y;
            Min.y = TargetPosition.y;
        }
        else
        {
            Max.y = TargetPosition.y;
            Min.y = StartPosition.y;
        }
    }


    public bool GetIntersection(Road road, out Vector2 intersection)
    {
        if (road.LineGradient == LineGradient)
        {
            intersection = new(0, 0);
            return false;
        }

        float x = (YIntercept - road.YIntercept) / (road.LineGradient - LineGradient);
        intersection = new(x, LineGradient * x + YIntercept);

        if (Min.x <= intersection.x && intersection.x <= Max.x && Min.y <= intersection.y && intersection.y <= Max.y) return true;

        return false;
    }
}