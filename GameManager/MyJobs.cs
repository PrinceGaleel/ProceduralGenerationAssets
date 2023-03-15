using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;

public class MyJobs : MonoBehaviour
{
    private static MyJobs Instance;
    private static ConcurrentQueue<MainThreadJob> QueueMainThread;
    private static ConcurrentQueue<SecondaryThreadJob> QueueOne;
    private static ConcurrentQueue<SecondaryThreadJob> QueueTwo;

    private static ConcurrentQueue<AssignJoinedNodesJob> NodesQueueOne;
    private static ConcurrentQueue<AssignJoinedNodesJob> NodesQueueTwo;
    private static ConcurrentQueue<AssignJoinedNodesJob> NodesQueueThree;
    private static ConcurrentQueue<AssignJoinedNodesJob> NodesQueueFour;

    private static ConcurrentQueue<PathfindingJob> PathfindingQueue;
    private static ConcurrentQueue<AssignNodeNeighboursJob> NeighboursQueue;

    private static Thread ThreadOne;
    private static Thread ThreadTwo;

    private static Thread ThreadNodesOne;
    private static Thread ThreadNodesTwo;
    private static Thread ThreadNodesThree;
    private static Thread ThreadNodesFour;

    private static Thread ThreadPathfinding;
    private static Thread ThreadNeighbours;

    private void Awake()
    {
        if(Instance)
        {
            Destroy(this);
            enabled = false;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            QueueMainThread = new();
            QueueOne = new();
            QueueTwo = new();

            NodesQueueOne = new();
            NodesQueueTwo = new();
            NodesQueueThree = new();
            NodesQueueFour = new();

            PathfindingQueue = new();
            NeighboursQueue = new();

            ThreadOne = new(new ThreadStart(ThreadUpdateOne));
            ThreadTwo = new(new ThreadStart(ThreadUpdateTwo));

            ThreadNodesOne = new(new ThreadStart(ThreadUpdateNodesOne));
            ThreadNodesTwo = new(new ThreadStart(ThreadUpdateNodesTwo));
            ThreadNodesThree = new(new ThreadStart(ThreadUpdateNodesThree));
            ThreadNodesFour = new(new ThreadStart(ThreadUpdateNodesFour));

            ThreadPathfinding = new(new ThreadStart(ThreadUpdatePathfinding));
            ThreadNeighbours = new(new ThreadStart(ThreadUpdateNeighbours));

            ThreadOne.Start();
            ThreadTwo.Start();

            ThreadNodesOne.Start();
            ThreadNodesTwo.Start();
            ThreadNodesThree.Start();
            ThreadNodesFour.Start();

            ThreadPathfinding.Start();
            ThreadNeighbours.Start();
        }
    }

    private void Update()
    {
        if(QueueMainThread.Count > 0)
        {
            if (QueueMainThread.TryDequeue(out MainThreadJob job))
            {
                job.Execute();
            }
        }
    }

    private static void ThreadUpdatePathfinding()
    {
        while (true)
        {
            if (PathfindingQueue.Count > 0)
            {
                if (PathfindingQueue.TryDequeue(out PathfindingJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateNodesOne()
    {
        while (true)
        {
            if (NodesQueueOne.Count > 0)
            {
                if (NodesQueueOne.TryDequeue(out AssignJoinedNodesJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateNodesTwo()
    {
        while (true)
        {
            if (NodesQueueTwo.Count > 0)
            {
                if (NodesQueueTwo.TryDequeue(out AssignJoinedNodesJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateNodesThree()
    {
        while (true)
        {
            if (NodesQueueThree.Count > 0)
            {
                if (NodesQueueThree.TryDequeue(out AssignJoinedNodesJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateNodesFour()
    {
        while (true)
        {
            if (NodesQueueFour.Count > 0)
            {
                if (NodesQueueFour.TryDequeue(out AssignJoinedNodesJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateNeighbours()
    {
        while (true)
        {
            if (NeighboursQueue.Count > 0)
            {
                if (NeighboursQueue.TryDequeue(out AssignNodeNeighboursJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateOne()
    {
        while(true)
        {
            if(QueueOne.Count > 0)
            { 
                if(QueueOne.TryDequeue(out SecondaryThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void ThreadUpdateTwo()
    {
        while (true)
        {
            if (QueueTwo.Count > 0)
            {
                if (QueueTwo.TryDequeue(out SecondaryThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (ThreadOne != null) { if (ThreadOne.IsAlive) { ThreadOne.Abort(); } }

        if (ThreadTwo != null) { if (ThreadTwo.IsAlive) { ThreadTwo.Abort(); } }

        if (ThreadNodesOne != null) { if (ThreadNodesOne.IsAlive) { ThreadNodesOne.Abort(); } }
        if (ThreadNodesTwo != null) { if (ThreadNodesTwo.IsAlive) { ThreadNodesTwo.Abort(); } }
        if (ThreadNodesThree != null) { if (ThreadNodesThree.IsAlive) { ThreadNodesThree.Abort(); } }
        if (ThreadNodesFour != null) { if (ThreadNodesFour.IsAlive) { ThreadNodesFour.Abort(); } }

        if (ThreadPathfinding != null) { if (ThreadPathfinding.IsAlive) { ThreadPathfinding.Abort(); } }
        if (ThreadNeighbours != null) { if (ThreadNeighbours.IsAlive) { ThreadNeighbours.Abort(); } }

        QueueMainThread = null;
        QueueOne = null;
        QueueTwo = null;

        ThreadNodesOne = null;
        ThreadNodesTwo = null;
        ThreadNodesThree = null;
        ThreadNodesFour = null;

        PathfindingQueue = null;
        NeighboursQueue = null;
    }

    public static void Schedule(MainThreadJob job) { QueueMainThread.Enqueue(job); }

    public static void Schedule(SecondaryThreadJob job)
    {
        if(QueueOne.Count < QueueTwo.Count)
        {
            QueueOne.Enqueue(job);
        }
        else
        {
            QueueTwo.Enqueue(job);
        }
    }

    public static void Schedule(PathfindingJob job) { PathfindingQueue.Enqueue(job); }

    public static void Schedule(AssignJoinedNodesJob job)
    {
        if (NodesQueueOne.Count < NodesQueueTwo.Count) NodesQueueOne.Enqueue(job);
        else if (NodesQueueTwo.Count < NodesQueueThree.Count) NodesQueueTwo.Enqueue(job);
        else if(NodesQueueThree.Count < NodesQueueFour.Count) NodesQueueThree.Enqueue(job);
        else NodesQueueFour.Enqueue(job);
    }

    public static void Schedule(AssignNodeNeighboursJob job) { NeighboursQueue.Enqueue(job); }
}

public abstract class PathfindingJob
{
    public abstract void Execute();

    public void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public abstract class AssignNodeNeighboursJob
{
    public abstract void Execute();

    public void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public abstract class AssignJoinedNodesJob
{
    public abstract void Execute();

    public void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public abstract class MainThreadJob
{
    public abstract void Execute();

    public void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public abstract class SecondaryThreadJob
{
    public abstract void Execute();

    public void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public class ToDestroyJob : MainThreadJob
{
    private readonly GameObject ToDestroy;

    public ToDestroyJob(GameObject toDestroy)
    {
        ToDestroy = toDestroy;
    }

    public override void Execute()
    {
        Object.Destroy(ToDestroy);
    }
}

public class ToDisableJob : MainThreadJob
{
    private readonly GameObject ToDisable;

    public ToDisableJob(GameObject toDisable)
    {
        ToDisable = toDisable;
    }

    public override void Execute()
    {
        ToDisable.SetActive(false);
    }
}
