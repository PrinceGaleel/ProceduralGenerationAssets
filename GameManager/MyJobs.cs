using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;
using System;

public class MyJobs : MonoBehaviour
{
    private static MyJobs Instance;

    private static MainThreadJob NextMainJob;
    private static List<MainThreadJob> QueueMain;
    private static ConcurrentQueue<MainThreadJob> QueueMainBuffer;

    private static ConcurrentQueue<PathfindingThreadJob> QueuePathfindingOne;
    private static ConcurrentQueue<PathfindingThreadJob> QueuePathfindingTwo;
    private static ConcurrentQueue<PathfindingThreadJob> QueuePathfindingThree;
    private static ConcurrentQueue<PathfindingThreadJob> QueuePathfindingFour;

    private Thread ThreadAdmin;

    private static Thread PathfindingThreadOne;
    private static Thread PathfindingThreadTwo;
    private static Thread PathfindingThreadThree;
    private static Thread PathfindingThreadFour;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(this);
            enabled = false;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);


            QueueMain = new();
            QueueMainBuffer = new();
            ThreadAdmin = new(new ThreadStart(AdminUpdate));
            ThreadAdmin.Start();

            QueuePathfindingOne = new();
            QueuePathfindingTwo = new();
            QueuePathfindingThree = new();
            QueuePathfindingFour = new();
            PathfindingThreadOne = new(new ThreadStart(PathfindingUpdateOne));
            PathfindingThreadTwo = new(new ThreadStart(PathfindingUpdateTwo));
            PathfindingThreadThree = new(new ThreadStart(PathfindingUpdateThree));
            PathfindingThreadFour = new(new ThreadStart(PathfindingUpdateFour));
            PathfindingThreadOne.Start();
            PathfindingThreadTwo.Start();
            PathfindingThreadThree.Start();
            PathfindingThreadFour.Start();
        }
    }

    private void Update()
    {
        if (NextMainJob != null)
        { 
            NextMainJob.Execute();
            NextMainJob = null;
        }
    }

    private static void AdminUpdate()
    {
        while (true)
        {
            lock (QueueMain)
            {
                while (QueueMainBuffer.Count > 0)
                {
                    if(QueueMainBuffer.TryDequeue(out MainThreadJob job))
                    {
                        QueueMain.Add(job);
                    }
                }
            }

            if (NextMainJob == null && QueueMain.Count > 0)
            {
                List<MainThreadJob> jobs = new(QueueMain);
                int nextJobIndex = 0;
                for (int i = 1; i < jobs.Count; i++)
                {
                    if (jobs[i].GetPriority < jobs[nextJobIndex].GetPriority)
                    {
                        nextJobIndex = i;
                    }
                }

                lock (QueueMain)
                {
                    NextMainJob = QueueMain[nextJobIndex];
                    QueueMain.RemoveAt(nextJobIndex);
                }
            }
        }
    }

    private static void PathfindingUpdateOne()
    {
        while (true)
        {
            while (QueuePathfindingOne.Count > 0)
            {
                if (QueuePathfindingOne.TryDequeue(out PathfindingThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void PathfindingUpdateTwo()
    {
        while (true)
        {
            while (QueuePathfindingTwo.Count > 0)
            {
                if (QueuePathfindingTwo.TryDequeue(out PathfindingThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void PathfindingUpdateThree()
    {
        while (true)
        {
            while (QueuePathfindingThree.Count > 0)
            {
                if (QueuePathfindingThree.TryDequeue(out PathfindingThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private static void PathfindingUpdateFour()
    {
        while (true)
        {
            while (QueuePathfindingFour.Count > 0)
            {
                if (QueuePathfindingFour.TryDequeue(out PathfindingThreadJob job))
                {
                    job.Execute();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (ThreadAdmin != null) { if (ThreadAdmin.IsAlive) { ThreadAdmin.Abort(); } }
        if (PathfindingThreadOne != null) { if (PathfindingThreadOne.IsAlive) { PathfindingThreadOne.Abort(); } }
        if (PathfindingThreadTwo != null) { if (PathfindingThreadTwo.IsAlive) { PathfindingThreadTwo.Abort(); } }
        if (PathfindingThreadThree != null) { if (PathfindingThreadThree.IsAlive) { PathfindingThreadThree.Abort(); } }
        if (PathfindingThreadFour != null) { if (PathfindingThreadFour.IsAlive) { PathfindingThreadFour.Abort(); } }

        QueueMain = null;
        QueueMainBuffer = null;
        QueuePathfindingOne = null;
        QueuePathfindingTwo = null;
        QueuePathfindingThree = null;
        QueuePathfindingFour = null;
    }

    public static void Schedule(MainThreadJob job) { QueueMainBuffer.Enqueue(job); }

    public static void Schedule(PathfindingThreadJob job)
    {
        if (QueuePathfindingOne.Count < QueuePathfindingTwo.Count)
        {
            QueuePathfindingOne.Enqueue(job);
        }
        else if (QueuePathfindingTwo.Count < QueuePathfindingThree.Count)
        {
            QueuePathfindingTwo.Enqueue(job);
        }
        else if (QueuePathfindingThree.Count < QueuePathfindingFour.Count)
        {
            QueuePathfindingThree.Enqueue(job);
        }
        else
        {
            QueuePathfindingFour.Enqueue(job);
        }
    }
}

public abstract class Job
{
    public abstract void Execute();

    public abstract void Schedule();
}

public abstract class MainThreadJob : Job
{
    protected int Priority = 1;
    public int GetPriority { get { return Priority; } }

    public override void Schedule()
    {
        MyJobs.Schedule(this);
    }
}

public abstract class PathfindingThreadJob : Job
{
    public override void Schedule()
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
        UnityEngine.Object.Destroy(ToDestroy);
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

public class ToEnableMonobehaivourJob : MainThreadJob
{
    private readonly MonoBehaviour ToEnable;

    public ToEnableMonobehaivourJob(MonoBehaviour toEnable)
    {
        ToEnable = toEnable;
    }

    public override void Execute()
    {
        ToEnable.enabled = true;
    }
}