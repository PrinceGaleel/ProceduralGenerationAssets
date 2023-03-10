using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class WorldInitializer : MonoBehaviour
{
    private List<Vector2Int> ChunksToCreate;

    private void Start()
    {
        PlayerStats.Instance.gameObject.SetActive(false);
        ChunksToCreate = new();
        for (int i = 0, x = -World.LODThreeDistance; x <= World.LODThreeDistance; x++)
        {
            for (int z = -World.LODThreeDistance; z <= World.LODThreeDistance; z++)
            {
                Vector2Int chunkPos = new(World.CurrentPlayerChunkPos.x + x, World.CurrentPlayerChunkPos.y + z);

                if (World.ActiveTerrain.TryAdd(chunkPos, Instantiate(World.ChunkPrefab, new(chunkPos.x * Chunk.DefaultChunkSize, 0, chunkPos.y * Chunk.DefaultChunkSize), Quaternion.identity, World.WorldTransform).GetComponent<Chunk>()))
                {
                    ChunksToCreate.Add(chunkPos);
                    World.ActiveTerrain[chunkPos].SetPositions(chunkPos);
                    new World.InitializeChunk(chunkPos).Schedule();
                }

                i++;
            }
        }
    }

    private void Update()
    {
        if (ChunksToCreate.Count == 0)
        {
            FoliageManager.PopulateAllFoliage();
            NavMeshManager.AddPOI(World.CurrentPlayerChunkPos, 1);
            World.Instance.enabled = true;
            FoliageManager.Instance.enabled = true;
            PlayerStats.Instance.gameObject.SetActive(true);

            SceneTransitioner.ToggleScreen(false);
            RenderTerrainMap.ReloadBlending();

            Destroy(this);
            enabled = false;
        }
        else if (World.ActiveTerrain[ChunksToCreate[0]])
        {
            while (ChunksToCreate.Count > 0)
            {
                if (World.ActiveTerrain[ChunksToCreate[0]].HasTerrain)
                {
                    ChunksToCreate.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }
    }
}