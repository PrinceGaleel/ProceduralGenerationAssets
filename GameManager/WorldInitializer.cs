using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using static GameManager;

public class WorldInitializer : MonoBehaviour
{
    private List<Vector2Int> ChunksToCreate;

    private void Start()
    {
        PlayerStats.Instance.gameObject.SetActive(false);
        ChunksToCreate = new();
        for (int i = 0, x = -ViewDistance; x <= ViewDistance; x++)
        {
            for (int z = -ViewDistance; z <= ViewDistance; z++)
            {
                Vector2Int chunkPos = new(CurrentPlayerChunkPos.x + x, CurrentPlayerChunkPos.y + z);

                if (ActiveTerrain.TryAdd(chunkPos, Instantiate(ChunkPrefab, new(chunkPos.x * Chunk.ChunkSize, 0, chunkPos.y * Chunk.ChunkSize), Quaternion.identity, WorldTransform).GetComponent<Chunk>()))
                {
                    ChunksToCreate.Add(chunkPos);
                    ActiveTerrain[chunkPos].SetPositions(chunkPos);
                    new CreateChunk(chunkPos).Schedule();
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
            Instance.enabled = true;
            FoliageManager.Instance.enabled = true;
            PlayerStats.Instance.gameObject.SetActive(true);

            SceneTransitioner.ToggleScreen(false);
            RenderTerrainMap.ReloadBlending();

            Destroy(this);
            enabled = false;
        }
        else if (ActiveTerrain[ChunksToCreate[0]])
        {
            while (ChunksToCreate.Count > 0)
            {
                if (ActiveTerrain[ChunksToCreate[0]].HasTerrain) ChunksToCreate.RemoveAt(0);
                else break;
            }
        }
    }
}