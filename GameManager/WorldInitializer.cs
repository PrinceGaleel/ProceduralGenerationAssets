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
        for (int i = 0, x = -GameManager.LODThreeDistance; x <= GameManager.LODThreeDistance; x++)
        {
            for (int z = -GameManager.LODThreeDistance; z <= GameManager.LODThreeDistance; z++)
            {
                Vector2Int chunkPos = new(GameManager.CurrentPlayerChunkPos.x + x, GameManager.CurrentPlayerChunkPos.y + z);

                if (GameManager.ActiveTerrain.TryAdd(chunkPos, Instantiate(GameManager.ChunkPrefab, new(chunkPos.x * Chunk.DefaultChunkSize, 0, chunkPos.y * Chunk.DefaultChunkSize), Quaternion.identity, GameManager.WorldTransform).GetComponent<Chunk>()))
                {
                    ChunksToCreate.Add(chunkPos);
                    GameManager.ActiveTerrain[chunkPos].SetPositions(chunkPos);
                    new GameManager.InitializeChunk(chunkPos).Schedule();
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
            NavMeshManager.AddPOI(GameManager.CurrentPlayerChunkPos, 1);
            GameManager.Instance.enabled = true;
            FoliageManager.Instance.enabled = true;
            PlayerStats.Instance.gameObject.SetActive(true);

            SceneTransitioner.ToggleScreen(false);
            RenderTerrainMap.ReloadBlending();

            Destroy(this);
            enabled = false;
        }
        else if (GameManager.ActiveTerrain[ChunksToCreate[0]])
        {
            while (ChunksToCreate.Count > 0)
            {
                if (GameManager.ActiveTerrain[ChunksToCreate[0]].HasTerrain) ChunksToCreate.RemoveAt(0);
                else break;
            }
        }
    }
}