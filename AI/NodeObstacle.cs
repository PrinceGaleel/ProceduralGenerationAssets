using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Jobs;

public class NodeObstacle : MonoBehaviour
{
    [SerializeField] private Transform Min, Max;

    private void Awake()
    {
        if (Min && Max)
        {
            new AINodeManager.AddObstacleJob(Chunk.GetChunkPosition(transform.position), new((Min.position + Max.position) / 2, (Max.position - Min.position) / 2, transform.eulerAngles.y)).Schedule();

            Destroy(Min.gameObject);
            Destroy(Max.gameObject);
        }
        Destroy(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Bounds2D obstacleBounds = new((Min.position + Max.position) / 2, (Max.position - Min.position) / 2, transform.eulerAngles.y);
        Gizmos.DrawCube(new(obstacleBounds.Center.x, transform.position.y, obstacleBounds.Center.y), new(obstacleBounds.HalfExtents.x, 0, obstacleBounds.HalfExtents.y));
    }
#endif
}
