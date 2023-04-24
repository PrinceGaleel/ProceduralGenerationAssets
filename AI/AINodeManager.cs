using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System;

using UnityEngine;
using Unity.Jobs;

public class AINodeManager : MonoBehaviour
{
    private readonly static object JobCounterLock = new();
    private static int JobCounter;

    public static ConcurrentDictionary<Vector2Int, ConcurrentBag<Bounds2D>> Obstacles { get; private set; }
    private static HashSet<Vector2Int> BlockedNodes;

    private void Awake()
    {
        lock (JobCounterLock) { JobCounter = 0; }

        BlockedNodes = new();
        Obstacles = new();
    }

    public readonly struct AddObstacleJob : IJob
    {
        private readonly Vector2Int ChunkPos;
        private readonly Bounds2D Bounds;

        public AddObstacleJob(Vector2Int chunkPos, Bounds2D bounds)
        {
            ChunkPos = chunkPos;
            Bounds = bounds;
        }

        public void Execute()
        {
            Vector2Int min = new(Mathf.CeilToInt(Bounds.Min.x), Mathf.CeilToInt(Bounds.Min.y));
            Vector2Int max = new(Mathf.FloorToInt(Bounds.Max.x), Mathf.FloorToInt(Bounds.Max.y));
            while (!Obstacles.ContainsKey(ChunkPos)) Obstacles.TryAdd(ChunkPos, new());

            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    lock (BlockedNodes) { if (!BlockedNodes.Contains(new(x, y))) BlockedNodes.Add(new(x, y)); }
                }
            }

            Obstacles[ChunkPos].Add(Bounds);
        }
    }

    private void OnDestroy()
    {
        JobCounter = 0;
        Obstacles = null;
        BlockedNodes = null;
    }

    private static readonly Vector2Int[] NeighbourTransformations = new Vector2Int[8] { new(1, 1), new(1, -1), new(-1, -1), new(-1, 1), new(0, 1), new(0, -1), new(-1, 0), new(0, 1) };
    private static Vector2Int RoundPosition(Vector2 position) { return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y)); }

    public static bool SamplePosition(Vector2Int position, int maxDistance, out Vector2Int closestPos)
    {
        closestPos = position;
        if (!BlockedNodes.Contains(position))
        {
            return true;
        }

        bool found = false;
        Tuple<Vector2Int, float> closestPoint = new(new(0, 0), float.MaxValue);
        for (int y = 0; y <= maxDistance; y++)
        {
            for (int x = 0; x <= maxDistance; x++)
            {
                if (!BlockedNodes.Contains(new(x, y)))
                {
                    found = true;
                    if (Vector2Int.Distance(new(x, y), position) < closestPoint.Item2)
                    {
                        closestPoint = new(new(x, y), Vector2Int.Distance(new(x, y), position));
                    }
                }
            }
        }

        if (found) closestPos = closestPoint.Item1;
        return found;
    }

    public static void QueueGetPath(NodeAgent agent, Vector3 start, Vector3 target)
    {
        lock (JobCounterLock)
        {
            JobCounter++;
            agent.CurrentJobID = JobCounter;
            new AssignAgentPath(JobCounter, agent, start, target).Schedule();
        }
    }

    private const int MaxSearches = 5000;
    private static List<Vector2> FindPath(Vector3 vec3StartPosition, Vector3 vec3TargetPosition, int maxDistance = 5)
    {
        Vector2 startPosition = new(vec3StartPosition.x, vec3StartPosition.z);
        Vector2 targetPosition = new(vec3TargetPosition.x, vec3TargetPosition.z);

        if (SamplePosition(RoundPosition(startPosition), maxDistance, out Vector2Int startGridPos) && SamplePosition(RoundPosition(targetPosition), maxDistance, out Vector2Int endGridPos))
        {
            if (GetGridPath(startPosition, targetPosition, startGridPos, endGridPos, out List<Vector2> path))
            {
                return path;
            }
        }

        return new() { startPosition };
    }

    private static bool GetGridPath(Vector2 startPosition, Vector2 targetPosition, Vector2Int actualStartPos, Vector2Int endGridPos, out List<Vector2> path)
    {
        SearchNode startingNode = new(actualStartPos, targetPosition);
        HashSet<Vector2Int> openSetHashes = new() { actualStartPos };
        List<SearchNode> openSet = new() { startingNode };
        Dictionary<Vector2Int, SearchNode> closedSet = new();

        int numSearches = 0;
        while (openSet.Count > 0 && numSearches < MaxSearches)
        {
            int currentNodeNum = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].TotalDist < openSet[currentNodeNum].TotalDist || (openSet[i].TotalDist == openSet[currentNodeNum].TotalDist && openSet[i].StartDist < openSet[currentNodeNum].StartDist))
                {
                    currentNodeNum = i;
                }
            }

            Vector2Int[] neighbours = openSet[currentNodeNum].MyNeighbours;
            foreach (Vector2Int neighbour in neighbours)
            {
                if (!BlockedNodes.Contains(neighbour))
                {
                    if (neighbour == endGridPos)
                    {
                        openSet.Add(new(neighbour, openSet[currentNodeNum], targetPosition));
                        openSetHashes.Add(neighbour);
                        path = GetPathFromFinalNode(openSet[^1], startPosition, targetPosition);
                        return true;
                    }

                    if (!openSetHashes.Contains(neighbour))
                    {
                        if (!closedSet.ContainsKey(neighbour))
                        {
                            openSet.Add(new(neighbour, openSet[currentNodeNum], targetPosition));
                            openSetHashes.Add(neighbour);
                        }
                        else
                        {
                            closedSet[neighbour].TryReassign(openSet[currentNodeNum]);
                        }
                    }
                }
            }

            closedSet.Add(openSet[currentNodeNum].Position, openSet[currentNodeNum]);
            openSetHashes.Remove(openSet[currentNodeNum].Position);
            openSet.RemoveAt(currentNodeNum);
            numSearches++;
        }

        path = new();
        return false;
    }

    private class SearchNode
    {
        public readonly Vector2Int Position;
        public SearchNode Parent;
        public float StartDist;
        public readonly float EndDist;
        public float TotalDist { get { return EndDist + StartDist; } }

        public Vector2Int[] MyNeighbours 
        {
            get
            {
                return new Vector2Int[8] { Position + NeighbourTransformations[0], Position + NeighbourTransformations[1], Position + NeighbourTransformations[2],
                 Position + NeighbourTransformations[3],  Position + NeighbourTransformations[4],  Position + NeighbourTransformations[5],
                 Position + NeighbourTransformations[6],  Position + NeighbourTransformations[7] };
            }
        }

        public SearchNode(Vector2Int position, Vector2 endPos)
        {
            Position = position;
            Parent = null;
            StartDist = 0;
            EndDist = Vector2.Distance(Position, endPos);
        }

        public SearchNode(Vector2Int position, SearchNode parent, Vector2 endPos)
        {
            Position = position;
            Parent = parent;
            StartDist = Vector2.Distance(Position, parent.Position) + parent.StartDist;
            EndDist = Vector2.Distance(Position, endPos);
        }

        public void TryReassign(SearchNode parent)
        {
            float newStartDist = parent.StartDist + Vector2.Distance(Position, parent.Position);
            if (newStartDist < StartDist)
            {
                Parent = parent;
                StartDist = newStartDist;
            }
        }
    }

    private const float NodeCheckDistance = 0.1f;
    private static List<Vector2> GetPathFromFinalNode(SearchNode finalNode, Vector2 startPosition, Vector2 endPosition)
    {
        List<Vector2> path = new() { endPosition };
        SearchNode currentNode = finalNode.Parent;
        while (currentNode.Parent != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Add(startPosition);

        for (int i = 1; i < path.Count - 1; i++)
        {
            bool canRemove = true;
            float totalDist = NodeCheckDistance;
            Vector2 direction = path[i + 1] - path[i - 1];

            while (totalDist != 1 && canRemove && canRemove == true)
            {
                if (BlockedNodes.Contains(new Vector2Int(Mathf.RoundToInt(path[i - 1].x + (direction.x * totalDist)), Mathf.RoundToInt(path[i - 1].y + (direction.y * totalDist)))))
                {
                    canRemove = false;
                }

                totalDist = Mathf.Min(totalDist + NodeCheckDistance, 1);
            }

            if (canRemove)
            {
                path.RemoveAt(i);
                i--;
            }
        }

        path.Reverse();
        path.RemoveAt(0);

        path.Reverse();
        return path;
    }

    private class AssignAgentPath : PathfindingThreadJob
    {
        private readonly int JobID;
        private readonly NodeAgent Agent;
        private readonly Vector3 StartPosition;
        private readonly Vector3 TargetPosition;

        public AssignAgentPath(int jobID, NodeAgent agent, Vector3 startPosition, Vector3 endPosition)
        {
            JobID = jobID;
            Agent = agent;
            StartPosition = startPosition;
            TargetPosition = endPosition;
        }

        public override void Execute()
        {
            Agent.SetPath(new(FindPath(StartPosition, TargetPosition)), JobID);
        }
    }
}