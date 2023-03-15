using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Linq;

public class AINodeManager : MonoBehaviour
{
    private static int JobCounter;

    private static HashSet<Vector2> BlockedBaseNodes;
    private static Dictionary<Vector2, NodePosition> JoinedNodes;
    private static Dictionary<Vector2, Vector2> BaseNodeParents;

    public const float BaseNodeSize = 0.25f;
    private const float NodeCheckDistance = 0.1f;

    public static bool IsWalkable(Vector3 position) { return BaseNodeParents.ContainsKey(new(ExtraUtils.RoundToFloat(position.x, BaseNodeSize), ExtraUtils.RoundToFloat(position.z, BaseNodeSize))); }
    private static bool IsWalkable(Vector2 gridPos) { return BaseNodeParents.ContainsKey(gridPos); }

    [Header("Chunk Specific")]
    private static HashSet<Vector2Int> NodedChunks;

    private void Awake()
    {
        JobCounter = 0;
        BlockedBaseNodes = new();
        JoinedNodes = new();
        BaseNodeParents = new();
        NodedChunks = new();
    }

    public static void GetPath(NodeAgent agent, Vector2 start, Vector2 target)
    {
        JobCounter++;
        agent.CurrentJobID = JobCounter;
        new AssignAgentPath(JobCounter, agent, start, target).Schedule();
    }

    private static List<Vector2> FindPath(Vector2 startPosition, Vector2 targetPosition)
    {
        Vector2 startGridPos = new(ExtraUtils.RoundToFloat(startPosition.x, BaseNodeSize), ExtraUtils.RoundToFloat(startPosition.y, BaseNodeSize));
        Vector2 targetGridPos = new(ExtraUtils.RoundToFloat(targetPosition.x, BaseNodeSize), ExtraUtils.RoundToFloat(targetPosition.y, BaseNodeSize));

        if (BaseNodeParents.ContainsKey(startGridPos) && BaseNodeParents.ContainsKey(targetGridPos))
        {
            if (BaseNodeParents[startGridPos] != BaseNodeParents[targetGridPos])
            {
                NodePosition startingNode = JoinedNodes[BaseNodeParents[startGridPos]];

                HashSet<Vector2> openSetHashes = new() { startingNode.ParentPosition };
                List<SearchNode> openSet = new() { new(startingNode, null, 0, Vector2.Distance(startPosition, targetPosition)) };
                Dictionary<Vector2, SearchNode> closedSet = new();

                while (openSet.Count > 0)
                {
                    int currentNodeNum = 0;
                    for (int i = 1; i < openSet.Count; i++)
                    {
                        if (openSet[i].TotalDist < openSet[currentNodeNum].TotalDist || (openSet[i].TotalDist == openSet[currentNodeNum].TotalDist && openSet[i].StartDist < openSet[currentNodeNum].StartDist))
                        {
                            currentNodeNum = i;
                        }
                    }

                    foreach (NodePosition neighbour in openSet[currentNodeNum].MyNode.Neighbours)
                    {
                        if (!openSetHashes.Contains(neighbour.ParentPosition))
                        {
                            if (!closedSet.ContainsKey(neighbour.ParentPosition))
                            {
                                openSet.Add(new(neighbour, openSet[currentNodeNum],
                                    Vector2.Distance(openSet[currentNodeNum].MyNode.ActualCenter, neighbour.ActualCenter) + openSet[currentNodeNum].StartDist,
                                    Vector2.Distance(neighbour.ActualCenter, targetPosition)));
                                openSetHashes.Add(neighbour.ParentPosition);

                                if (neighbour.ParentPosition == BaseNodeParents[targetGridPos])
                                {
                                    return GetPathFromFinalNode(openSet[^1], startPosition, targetPosition);
                                }
                            }
                            else closedSet[neighbour.ParentPosition].TryReassign(openSet[currentNodeNum]);
                        }
                    }

                    if (!closedSet.ContainsKey(openSet[currentNodeNum].MyNode.ParentPosition)) closedSet.Add(openSet[currentNodeNum].MyNode.ParentPosition, openSet[currentNodeNum]);

                    openSetHashes.Remove(openSet[currentNodeNum].MyNode.ParentPosition);
                    openSet.RemoveAt(currentNodeNum);
                }
            }
            else
            {
                return new() { targetPosition };
            }
        }

        return new() { startPosition };
    }

    private static List<Vector2> GetPathFromFinalNode(SearchNode finalNode, Vector2 startPosition, Vector2 targetPosition)
    {
        List<Vector2> finalPath = new() { targetPosition };

        if (finalNode.Parent != null)
        {
            SearchNode currentNode = finalNode.Parent;
            while (currentNode.Parent != null)
            {
                finalPath.Add(ClosestPoint2D(currentNode.MyNode.ActualCenter, currentNode.MyNode.HalfExtents, finalPath[^1]));
                currentNode = currentNode.Parent;
            }

            finalPath.Add(startPosition);
            for (int i = 1; i < finalPath.Count - 1; i++)
            {
                bool canRemove = true;
                float totalDist = NodeCheckDistance;
                Vector2 direction = finalPath[i + 1] - finalPath[i - 1];

                while (totalDist != 1 && canRemove && canRemove == true)
                {
                    if (!IsWalkable(new Vector2(ExtraUtils.RoundToFloat(finalPath[i - 1].x + (direction.x * totalDist), BaseNodeSize), ExtraUtils.RoundToFloat(finalPath[i - 1].y + (direction.y * totalDist), BaseNodeSize))))
                    {
                        canRemove = false;
                    }

                    totalDist = Mathf.Min(totalDist + NodeCheckDistance, 1);
                }

                if (canRemove)
                {
                    finalPath.RemoveAt(i);
                    i--;
                }
            }

            finalPath.Reverse();
            finalPath.RemoveAt(0);
        }

        return finalPath;
    }

    private static Vector2 ClosestPoint2D(Vector2 centre, Vector2 halfExtents, Vector2 point)
    {
        float gradient = (centre.y - (centre.y + halfExtents.y)) / (centre.x - (centre.x + halfExtents.x));
        float coefficient = centre.y - (gradient * centre.x);

        if ((gradient * point.x) + coefficient > point.y)
        {
            if ((gradient * -point.x) + coefficient > point.y) //bottom
            {
                return new(Mathf.Clamp(point.x, centre.x - halfExtents.x, centre.x + halfExtents.x), centre.y - halfExtents.y);
            }
            else //right
            {
                return new(centre.x + halfExtents.x, Mathf.Clamp(point.y, centre.y - halfExtents.y, centre.y + halfExtents.y));
            }
        }
        else
        {
            if ((gradient * point.x) + coefficient > -point.y) //top
            {
                return new(Mathf.Clamp(point.x, centre.x - halfExtents.x, centre.x + halfExtents.x), centre.y + halfExtents.y);
            }
            else //left
            {
                return new(centre.x - halfExtents.x, Mathf.Clamp(point.y, centre.y - halfExtents.y, centre.y + halfExtents.y));
            }
        }
    }

    private static Dictionary<Vector2, Vector2> GetBaseNodeParents(Dictionary<Vector2, NodePosition> joinedNodes)
    {
        Dictionary<Vector2, Vector2> baseNodeParents = new();
        foreach (Vector2 parentNode in joinedNodes.Keys)
        {
            foreach (Vector2 childNode in joinedNodes[parentNode].MyBaseNodes)
            {
                baseNodeParents.Add(childNode, parentNode);
            }
        }
        return baseNodeParents;
    }

    private static Dictionary<Vector2, NodePosition> JoinBaseNodes(List<Vector2> basePositions)
    {
        Dictionary<Vector2, NodePosition> nodePositions = new();
        while (basePositions.Count > 0)
        {
            Vector2 originalPos = basePositions[0];
            nodePositions.Add(originalPos, new(originalPos));
            basePositions.RemoveAt(0);

            if (basePositions.Contains(originalPos + new Vector2(BaseNodeSize, 0)))
            {
                if (basePositions.Contains(originalPos + new Vector2(0, BaseNodeSize)) && basePositions.Contains(originalPos + new Vector2(BaseNodeSize, BaseNodeSize)))
                {
                    nodePositions[originalPos].MyBaseNodes.Add(originalPos + new Vector2(BaseNodeSize, 0));
                    basePositions.Remove(originalPos + new Vector2(BaseNodeSize, 0));

                    nodePositions[originalPos].MyBaseNodes.Add(originalPos + new Vector2(0, BaseNodeSize));
                    basePositions.Remove(originalPos + new Vector2(0, BaseNodeSize));

                    nodePositions[originalPos].MyBaseNodes.Add(originalPos + new Vector2(BaseNodeSize, BaseNodeSize));
                    basePositions.Remove(originalPos + new Vector2(BaseNodeSize, BaseNodeSize));
                }
                else
                {
                    nodePositions[originalPos].MyBaseNodes.Add(originalPos + new Vector2(0, BaseNodeSize));
                    basePositions.Remove(originalPos + new Vector2(0, BaseNodeSize));
                }
            }
            else if (basePositions.Contains(originalPos + new Vector2(0, BaseNodeSize)))
            {
                nodePositions[originalPos].MyBaseNodes.Add(originalPos + new Vector2(0, BaseNodeSize));
                basePositions.Remove(originalPos + new Vector2(0, BaseNodeSize));
            }
        }
        return nodePositions;
    }

    private static void ApplyNodes(List<Vector2> positions)
    {
        Dictionary<Vector2, NodePosition> joinedNodes = JoinBaseNodes(positions);
        Dictionary<Vector2, Vector2> baseNodeParents = GetBaseNodeParents(joinedNodes);

        foreach (NodePosition nodePos in joinedNodes.Values)
        {
            nodePos.CalculateSize();
            nodePos.SetNeighbours(joinedNodes, baseNodeParents);
        }

        List<Vector2> keys = new(joinedNodes.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            NodePosition nodePosition = joinedNodes[keys[i]];
            foreach (NodePosition neighbour in nodePosition.Neighbours)
            {
                if ((nodePosition.ActualCenter.x == neighbour.ActualCenter.x && nodePosition.HalfExtents.x == neighbour.ActualCenter.x)
                    || (nodePosition.ActualCenter.y == neighbour.ActualCenter.y && nodePosition.HalfExtents.y == neighbour.ActualCenter.y))
                {
                    nodePosition.MyBaseNodes.AddRange(neighbour.MyBaseNodes);
                    foreach (Vector2 position in neighbour.MyBaseNodes)
                    {
                        baseNodeParents[position] = nodePosition.ParentPosition;
                    }

                    joinedNodes.Remove(neighbour.ParentPosition);
                    keys.Remove(neighbour.ParentPosition);

                    nodePosition.CalculateSize();
                    nodePosition.SetNeighbours(joinedNodes, baseNodeParents);

                    i--;
                    break;
                }
            }
        }

        foreach (KeyValuePair<Vector2, NodePosition> pair in joinedNodes) JoinedNodes.Add(pair.Key, pair.Value);
        foreach (KeyValuePair<Vector2, Vector2> pair in baseNodeParents) BaseNodeParents.Add(pair.Key, pair.Value);
    }

    public NodePosition GetParentNode(Vector2 pos) { return JoinedNodes[BaseNodeParents[new(ExtraUtils.RoundToFloat(pos.x, BaseNodeSize), ExtraUtils.RoundToFloat(pos.y, BaseNodeSize))]]; }

    public class NodePosition
    {
        public Vector2 ParentPosition;
        public Vector2 ActualCenter;
        public Vector2 HalfExtents;
        public HashSet<NodePosition> Neighbours;
        public List<Vector2> MyBaseNodes;

        public NodePosition(Vector2 startPos)
        {
            ParentPosition = startPos;
            HalfExtents = new(0.5f, 0.5f);
            ActualCenter = startPos;
            MyBaseNodes = new() { startPos };
        }

        public void CalculateSize()
        {
            Vector2 total = MyBaseNodes[0];
            float maxX = MyBaseNodes[0].x;
            float maxY = MyBaseNodes[0].y;

            for (int i = 1; i < MyBaseNodes.Count; i++)
            {
                total += MyBaseNodes[i];

                if (MyBaseNodes[i].x > maxX)
                {
                    maxX = MyBaseNodes[i].x;
                }
                if (MyBaseNodes[i].y > maxY)
                {
                    maxY = MyBaseNodes[i].y;
                }
            }

            ActualCenter = total / MyBaseNodes.Count;
            HalfExtents = new(Mathf.Abs(ActualCenter.x - maxX) + 0.5f, Mathf.Abs(ActualCenter.y - maxY) + 0.5f);
        }

        public void SetNeighbours(Dictionary<Vector2, NodePosition> joinedNodes, Dictionary<Vector2, Vector2> baseNodeParents)
        {
            Neighbours = new();
            Vector2 border = new(HalfExtents.x + 0.5f, HalfExtents.y + 0.5f);

            for (float y = -border.y; y < border.y + 0.5f; y += BaseNodeSize)
            {
                CheckPosForNeighbour(new(ExtraUtils.RoundToFloat(ActualCenter.x + border.x, BaseNodeSize), ExtraUtils.RoundToFloat(ActualCenter.y + y, BaseNodeSize)), joinedNodes, baseNodeParents);
                CheckPosForNeighbour(new(ExtraUtils.RoundToFloat(ActualCenter.x - border.x, BaseNodeSize), ExtraUtils.RoundToFloat(ActualCenter.y + y, BaseNodeSize)), joinedNodes, baseNodeParents);
            }

            for (float x = -border.x; x < border.x + 0.5f; x += BaseNodeSize)
            {
                CheckPosForNeighbour(new(ExtraUtils.RoundToFloat(ActualCenter.x + x, BaseNodeSize), ExtraUtils.RoundToFloat(ActualCenter.y + border.y, BaseNodeSize)), joinedNodes, baseNodeParents);
                CheckPosForNeighbour(new(ExtraUtils.RoundToFloat(ActualCenter.x + x, BaseNodeSize), ExtraUtils.RoundToFloat(ActualCenter.y - border.y, BaseNodeSize)), joinedNodes, baseNodeParents);
            }
        }

        private void CheckPosForNeighbour(Vector2 pos, Dictionary<Vector2, NodePosition> joinedNodes, Dictionary<Vector2, Vector2> baseNodeParents)
        {
            if (baseNodeParents.ContainsKey(pos))
            {
                if (!Neighbours.Contains(joinedNodes[baseNodeParents[pos]]))
                {
                    Neighbours.Add(joinedNodes[baseNodeParents[pos]]);
                }
            }
        }
    }

    private class SearchNode
    {
        public NodePosition MyNode;
        public SearchNode Parent;
        public float StartDist;
        public float EndDist;
        public float TotalDist { get { return EndDist + StartDist; } }

        public SearchNode(NodePosition position, SearchNode parent, float startDist, float endDist)
        {
            MyNode = position;
            Parent = parent;
            StartDist = startDist;
            EndDist = endDist;
        }

        public void TryReassign(SearchNode parent)
        {
            float newStartDist = parent.StartDist + Vector2.Distance(MyNode.ActualCenter, parent.MyNode.ActualCenter);
            if (newStartDist < StartDist)
            {
                Parent = parent;
                StartDist = newStartDist;
            }
        }
    }

    public class AddNodesChunkJob : AssignJoinedNodesJob
    {
        private readonly Vector2Int StartPosition;

        public AddNodesChunkJob(Vector2Int worldPos)
        {
            if (!NodedChunks.Contains(worldPos / Chunk.DefaultChunkSize))
            {
                NodedChunks.Add(worldPos / Chunk.DefaultChunkSize);
                StartPosition = worldPos - new Vector2Int(Chunk.DefaultChunkSize / 2, Chunk.DefaultChunkSize / 2);
            }
        }

        public override void Execute()
        {
            List<Vector2> baseNodes = new();
            for (float y = StartPosition.y; y < StartPosition.y + Chunk.DefaultChunkSize; y += BaseNodeSize)
            {
                for (float x = StartPosition.x; x < StartPosition.x + Chunk.DefaultChunkSize; x += BaseNodeSize)
                {
                    if (!BlockedBaseNodes.Contains(new(x, y))) baseNodes.Add(new(x, y));
                }
            }

            ApplyNodes(baseNodes);
        }
    }

    private class AssignAgentPath : PathfindingJob
    {
        private readonly int JobID;
        private readonly NodeAgent Agent;
        private readonly Vector2 StartPosition;
        private readonly Vector2 TargetPosition;

        public AssignAgentPath(int jobID, NodeAgent agent, Vector2 startPosition, Vector2 endPosition)
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