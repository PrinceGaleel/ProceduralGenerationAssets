using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public enum NodeAIStates
{ 
    Idle,
    Waiting_For_Path,
    Following_Path
}

public class NodeAgent : ControllerExtension
{
    private Vector2 MyVec2Pos { get { return new(MyTransform.position.x, MyTransform.position.z); } }
    private Action CurrentCheck = null;
    [SerializeField] private NodeAIStates NodeState = NodeAIStates.Idle;

    [Header("JobInfo")]
    [SerializeField] private int _CurrentJobID;
    public int CurrentJobID { get { return _CurrentJobID; } set { _CurrentJobID = value; } }

    [Header("Physics")]
    public float CurrentMovementSpeed;
    public float CurrentRotationSpeed;
    public bool UpdateRotation = true;

    [Header("Pathfinding")]
    private ConcurrentQueue<Vector2> CurrentPath = new();
    private Vector2 TargetPosition;
    private const float MinDistToTarget = 0.1f;

    public void Warp(Vector3 position)
    {
        EnableController = false;
        MyTransform.position = position;
        EnableController = true;
    }

    public bool IsStopped
    {
        get
        {
            return Stopped;
        }
        set
        {
            Stopped = value;
        }
    }
    [SerializeField] private bool Stopped;

    private void Awake() { Stopped = false; }

    private void Update() 
    {
        CurrentCheck?.Invoke(); 
        MoveController();
    }

    public void SetPath(ConcurrentQueue<Vector2> positions, int jobID)
    {
        if (_CurrentJobID == jobID && NodeState == NodeAIStates.Waiting_For_Path)
        {
            CurrentPath = positions;
            SetFollowPath();
        }
    }

    private void SetFollowPath()
    {
        NodeState = NodeAIStates.Following_Path;
        CurrentCheck = MovingCheck;
        CurrentPath.TryDequeue(out TargetPosition);
    }

    private void MovingCheck()
    {
        if (UpdateRotation)
        {
            Quaternion targetRot = Quaternion.LookRotation(new Vector3(TargetPosition.x, 0, TargetPosition.y) - new Vector3(MyTransform.position.x, 0, MyTransform.position.z));
            MyTransform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, CurrentRotationSpeed * Time.deltaTime);
        }

        float aSide = Vector3.Distance(MyTransform.position, MyTransform.forward + MyTransform.position);
        float bSide = Vector3.Distance(MyTransform.position, new Vector3(TargetPosition.x, 0, TargetPosition.y));
        float cSide = Vector3.Distance(MyTransform.forward + MyTransform.position, new Vector3(TargetPosition.x, 0, TargetPosition.y));

        Velocity = (TargetPosition - MyVec2Pos).normalized * (CurrentMovementSpeed * ((360 - ExtraUtils.Cosine3Sides(aSide, bSide, cSide)) / 360));
        if (Vector2.Distance(TargetPosition, MyVec2Pos) < MinDistToTarget)
        {
            if (CurrentPath.Count > 0) CurrentPath.TryDequeue(out TargetPosition);
            else SetIdle();
        }
    }

    public void SetIdle()
    {
        NodeState = NodeAIStates.Idle;
        Velocity = Vector2.zero;
        CurrentPath = new();
        CurrentCheck = null;
    }

    public void SetDestination(Vector3 target)
    {
        NodeState = NodeAIStates.Waiting_For_Path;
        Velocity = Vector2.zero;
        CurrentCheck = null;
        AINodeManager.QueueGetPath(this, MyTransform.position, target);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (!MyTransform) MyTransform = transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (NodeState == NodeAIStates.Following_Path)
        {
            Gizmos.color = Color.blue;
            List<Vector2> path = new(CurrentPath);

            Gizmos.DrawSphere(MyTransform.position, 0.25f);
            Gizmos.DrawSphere(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), 0.25f);
            Gizmos.DrawLine(MyTransform.position, new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y));

            if (CurrentPath.Count > 0)
            {
                Gizmos.DrawLine(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), new Vector3(path[0].x, MyTransform.position.y, path[0].y));
                Gizmos.DrawSphere(new Vector3(path[0].x, MyTransform.position.y, path[0].y), 0.25f);
                Gizmos.DrawLine(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), new Vector3(path[0].x, MyTransform.position.y, path[0].y));

                for (int i = 1; i < path.Count; i++)
                {
                    Gizmos.DrawSphere(new Vector3(path[i].x, MyTransform.position.y, path[i].y), 0.25f);
                    Gizmos.DrawLine(new Vector3(path[i - 1].x, MyTransform.position.y, path[i - 1].y), new Vector3(path[i].x, MyTransform.position.y, path[i].y));
                }
            }
        }
    }
#endif
}