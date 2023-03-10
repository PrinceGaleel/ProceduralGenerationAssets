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

    private static Thread ThreadOne;
    private static Thread ThreadTwo;

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

            ThreadOne = new(new ThreadStart(ThreadUpdateOne));
            ThreadTwo = new(new ThreadStart(ThradUpdateTwo));

            ThreadOne.Start();
            ThreadTwo.Start();
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

    private static void ThradUpdateTwo()
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
        QueueOne.Clear();
        QueueTwo.Clear();

        if(ThreadOne.IsAlive)
        {
            ThreadOne.Abort();
        }

        if (ThreadTwo.IsAlive)
        {
            ThreadTwo.Abort();
        }
    }

    public static void Schedule(MainThreadJob job)
    {
        QueueMainThread.Enqueue(job);
    }

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
