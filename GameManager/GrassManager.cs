using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class GrassManager : MonoBehaviour
{
    public static GrassManager Instance { get; private set; }
    private static GrassDictUpdate GrassUpdateCheck;
    private static ConcurrentDictionary<Vector2Int, GrassChunk> GrassChunks;
    private static ConcurrentQueue<GrassChunk> ChunksBuffer;

    public static ComputeShader GrassComputeShader;
    public static Material GrassMaterial;
    public static ComputeShader GrassShader;

    private const int MaxGrassDistance = 2;
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

            for (int y = -MaxGrassDistance; y <= MaxGrassDistance; y++)
            {
                for (int x = -MaxGrassDistance; x <= MaxGrassDistance; x++)
                {
                    ChunksBuffer.Enqueue(gameObject.AddComponent<GrassChunk>());
                }
            }

            GrassUpdateCheck = new();
            GrassUpdateCheck.Schedule();
        }

        enabled = false;
    }

    private void Update()
    {
        if(GrassUpdateCheck.IsCompleted)
        {
            if(IsRemapGrass)
            {
                IsRemapGrass = false;
                GrassUpdateCheck = new();
                GrassUpdateCheck.Schedule();
            }
            else
            {
                enabled = false;
            }
        }
    }

    public static void Initialize(Material grassMaterial, ComputeShader grassComputeShader)
    {
        GrassComputeShader = grassComputeShader;
        GrassMaterial = grassMaterial;
    }

    public static void RemapGrass()
    {
        if (GrassUpdateCheck.IsCompleted)
        {
            GrassUpdateCheck = new();
            GrassUpdateCheck.Schedule();
        }
        else
        {
            IsRemapGrass = true;
        }

        Instance.enabled = true;
    }

    private class GrassDictUpdate : SecondaryThreadJob
    {
        public bool IsCompleted { get; private set; }

        public GrassDictUpdate()
        {
            IsCompleted = false;
        }

        public override void Execute()
        {
            foreach (Vector2Int key in GrassChunks.Keys)
            {
                if(Mathf.Abs(key.x - World.CurrentPlayerChunkPos.x) > MaxGrassDistance || Mathf.Abs(key.y - World.CurrentPlayerChunkPos.y) > MaxGrassDistance)
                {
                    if(GrassChunks.TryRemove(key, out GrassChunk grassChunk))
                    {
                        ChunksBuffer.Enqueue(grassChunk);
                    }
                }
            }

            for (int y = -MaxGrassDistance; y < MaxGrassDistance; y++)
            {
                for (int x = -MaxGrassDistance; x < MaxGrassDistance; x++)
                {
                    if (ChunksBuffer.Count == 0)
                    {
                        IsCompleted = true;
                        return;
                    }

                    Vector2Int chunkToCheck = new(World.CurrentPlayerChunkPos.x + x, World.CurrentPlayerChunkPos.y + y);
                    if (!GrassChunks.ContainsKey(chunkToCheck))
                    {
                        if(ChunksBuffer.TryDequeue(out GrassChunk grassChunk))
                        {
                            if (GrassChunks.TryAdd(chunkToCheck, grassChunk))
                            {
                                new GrassChunk.GrassUpdateJob(grassChunk, chunkToCheck).Schedule();
                            }
                            else
                            {
                                ChunksBuffer.Enqueue(grassChunk);
                            }
                        }
                    }
                }
            }

            IsCompleted = true;
        }
    }
}