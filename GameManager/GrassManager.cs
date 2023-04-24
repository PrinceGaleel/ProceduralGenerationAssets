using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using Unity.Jobs;
using Unity.VisualScripting;
using System;

public class GrassManager : MonoBehaviour
{
    public static GrassManager Instance { get; private set; }

    private static Vector2Int CurrentPlayerChunkPosition;
    private static JobHandle GrassUpdateCheck;
    public static ConcurrentDictionary<Vector2Int, int> GrassChunks { get; private set; }
    public static ConcurrentDictionary<int, System.Random> Randoms;
    public static ConcurrentDictionary<int, GrassChunk> References;

    private static Queue<int> ChunksBuffer;
    private static ConcurrentQueue<Vector2Int> GrassToUpdate;

    public static ComputeShader GrassComputeShader;
    public static Material GrassMaterial;

    public const int MaxGrassDistance = 1;
    private static bool IsRemapGrass;

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
            gameObject.layer = LayerMask.NameToLayer("Grass");
            IsRemapGrass = true;
            
            GrassChunks = new();
            ChunksBuffer = new();
            GrassToUpdate = new();
            Randoms = new();
            References = new();

            int i = 0;
            for (int y = -MaxGrassDistance; y <= MaxGrassDistance; y++)
            {
                for (int x = -MaxGrassDistance; x <= MaxGrassDistance; x++)
                {
                    GrassChunk chunk = gameObject.AddComponent<GrassChunk>();
                    while(!Randoms.ContainsKey(i)) Randoms.TryAdd(i, new());
                    while (!References.ContainsKey(i)) References.TryAdd(i, chunk);
                    ChunksBuffer.Enqueue(i);

                    i++;
                }
            }

            GrassUpdateCheck = new GrassDictUpdate(Chunk.GetChunkPosition(GameManager.CurrentSaveData.LastPosition)).Schedule();            
        }
    }

    private void OnDestroy()
    {
        References = new();
        GrassChunks = null;
        ChunksBuffer = null;
        GrassToUpdate = null;
        Randoms = new();
    }

    private void Update()
    {
        if (IsRemapGrass == true && GrassUpdateCheck.IsCompleted)
        {
            GrassUpdateCheck = new GrassDictUpdate(CurrentPlayerChunkPosition).Schedule();
        }

        if (GrassToUpdate.Count > 0)
        {
            if (GrassToUpdate.TryDequeue(out Vector2Int chunkToCheck))
            {
                if (GrassChunks.ContainsKey(chunkToCheck))
                {
                    new GrassChunk.GrassUpdateJob(chunkToCheck).Schedule();
                }
            }
        }
    }

    public static void RemapGrass(Vector2Int playerChunkPosition)
    {
        if (playerChunkPosition != CurrentPlayerChunkPosition)
        {
            if (GrassUpdateCheck.IsCompleted) GrassUpdateCheck = new GrassDictUpdate(playerChunkPosition).Schedule();
            else
            {
                CurrentPlayerChunkPosition = playerChunkPosition;
                IsRemapGrass = true;
            }
        }
    }

    public static void InitializeStatics(Material grassMaterial, ComputeShader grassComputeShader)
    {
        GrassComputeShader = grassComputeShader;
        GrassMaterial = grassMaterial;
    }

    private struct GrassDictUpdate : IJob
    {
        private Vector2Int PlayerChunkPos;

        public GrassDictUpdate(Vector2Int playerChunkPos)
        {
            PlayerChunkPos = playerChunkPos;
        }

        public void Execute()
        {
            IsRemapGrass = false;
            List<Vector2Int> keys = new(GrassChunks.Keys);
            foreach (Vector2Int key in keys)
            {
                if (Mathf.Abs(key.x - PlayerChunkPos.x) > MaxGrassDistance || Mathf.Abs(key.y - PlayerChunkPos.y) > MaxGrassDistance)
                {
                    ChunksBuffer.Enqueue(GrassChunks[key]);
                    while(GrassChunks.ContainsKey(key)) GrassChunks.TryRemove(key, out _);
                }
            }

            if (ChunksBuffer.Count > 0)
            {
                for (int y = -MaxGrassDistance; y <= MaxGrassDistance; y++)
                {
                    for (int x = -MaxGrassDistance; x <= MaxGrassDistance; x++)
                    {
                        Vector2Int chunkToCheck = new(PlayerChunkPos.x + x, PlayerChunkPos.y + y);
                        if (!GrassChunks.ContainsKey(chunkToCheck))
                        {
                            int grassChunkNum = ChunksBuffer.Dequeue();
                            while (!GrassChunks.ContainsKey(chunkToCheck)) GrassChunks.TryAdd(chunkToCheck, grassChunkNum);
                            References[grassChunkNum].MyPos = chunkToCheck;
                            GrassToUpdate.Enqueue(chunkToCheck);
                        }
                    }

                    if (ChunksBuffer.Count == 0)
                    {
                        if (IsRemapGrass) IsRemapGrass = false;
                        return;
                    }
                }
            }

            if (IsRemapGrass) IsRemapGrass = false;            
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (GrassChunks != null)
        {
            Gizmos.color = Color.green;
            List<Vector2Int> keys = new(GrassChunks.Keys);
            foreach (Vector2Int key in keys)
            {
                Gizmos.DrawSphere(new(key.x * Chunk.ChunkSize, 50, key.y * Chunk.ChunkSize), 25);
            }
        }
    }
#endif
}