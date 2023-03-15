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

[RequireComponent(typeof(CharacterController))]
public class NodeAgent : MonoBehaviour
{
    [SerializeField] private CharacterController Controller;
    [SerializeField] private Transform MyTransform;
    private Vector2 MyVec2Pos { get { return new(MyTransform.position.x, MyTransform.position.z); } }
    private Action CurrentCheck = null;
    [SerializeField] private NodeAIStates NodeState = NodeAIStates.Idle;

    public int CurrentJobID { get { return _CurrentJobID; } set { _CurrentJobID = value; } }
    [SerializeField] private int _CurrentJobID;

    [Header("Physics")]
    public float MovementSpeed;
    public float RotationSpeed;
    private Vector2 Velocity2D = new();
    private readonly float VerticalSpeed = Physics.gravity.y;
    public bool UpdateRotation = true;

    [Header("Pathfinding")]
    private ConcurrentQueue<Vector2> CurrentPath = new();
    private Vector2 TargetPosition;
    private const float MinDistToTarget = 0.1f;

    public void Move(Vector3 direction) { Controller.Move(direction); }

    public void Warp(Vector3 position)
    {
        Controller.enabled = false;
        MyTransform.position = position;
        Controller.enabled = true;
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
        if (!Stopped) Controller.Move(Time.deltaTime * new Vector3(Velocity2D.x * MovementSpeed, VerticalSpeed, Velocity2D.y * MovementSpeed));
        else { Controller.Move(Time.deltaTime * new Vector3(0, VerticalSpeed, 0)); }
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
            MyTransform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }

        Velocity2D = (TargetPosition - MyVec2Pos).normalized;
        if (Vector2.Distance(TargetPosition, MyVec2Pos) < MinDistToTarget)
        {
            if (CurrentPath.Count > 0) CurrentPath.TryDequeue(out TargetPosition);
            else SetIdle();
        }
    }

    public void SetIdle()
    {
        NodeState = NodeAIStates.Idle;
        Velocity2D = Vector2.zero;
        CurrentPath = new();
        CurrentCheck = null;
    }

    public void SetDestination(Vector2 target)
    {
        NodeState = NodeAIStates.Waiting_For_Path;
        Velocity2D = Vector2.zero;
        CurrentCheck = null;
        AINodeManager.GetPath(this, new(MyTransform.position.x, MyTransform.position.z), target);
    }

    public void SetDestination(Vector3 target)
    {
        SetDestination(new Vector2(target.x, target.z));
    }

#if UNITY_EDITOR
    protected void OnValidate()
    {
        Controller = GetComponent<CharacterController>();
        MyTransform = transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (NodeState == NodeAIStates.Following_Path)
        {
            Gizmos.color = Color.blue;
            List<Vector2> path = new(CurrentPath);

            Gizmos.DrawCube(MyTransform.position, new(0.5f, 0, 0.5f));
            Gizmos.DrawCube(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), new(0.5f, 0, 0.5f));
            Gizmos.DrawLine(MyTransform.position, new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y));

            if (CurrentPath.Count > 0)
            {
                Gizmos.DrawLine(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), new Vector3(path[0].x, MyTransform.position.y, path[0].y));
                Gizmos.DrawCube(new Vector3(path[0].x, MyTransform.position.y, path[0].y), new(0.5f, 0, 0.5f));
                Gizmos.DrawLine(new Vector3(TargetPosition.x, MyTransform.position.y, TargetPosition.y), new Vector3(path[0].x, MyTransform.position.y, path[0].y));

                for (int i = 1; i < path.Count; i++)
                {
                    Gizmos.DrawCube(new Vector3(path[i].x, MyTransform.position.y, path[i].y), new(0.5f, 0, 0.5f));
                    Gizmos.DrawLine(new Vector3(path[i - 1].x, MyTransform.position.y, path[i - 1].y), new Vector3(path[i].x, MyTransform.position.y, path[i].y));
                }
            }
        }
    }
#endif
}