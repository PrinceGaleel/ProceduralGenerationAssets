using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInitializer : MonoBehaviour
{
    private void Start()
    {
        PlayerStats.PlayerTransform.position = Chunk.GetPerlinPosition(World.CurrentSaveData.LastPosition.x, World.CurrentSaveData.LastPosition.z) + new Vector3(0, 50, 0);
        Vector2Int currentPlayerChunkPos = new(Mathf.RoundToInt(PlayerStats.PlayerTransform.position.x / Chunk.DefaultChunkSize), Mathf.RoundToInt(PlayerStats.PlayerTransform.position.z / Chunk.DefaultChunkSize));

        for (int x = -World.LODFiveDistance; x <= World.LODFiveDistance; x++)
        {
            for (int z = -World.LODFiveDistance; z <= World.LODFiveDistance; z++)
            {
                Vector2Int chunkPos = new(x + currentPlayerChunkPos.x, z + currentPlayerChunkPos.y);
                Vector2Int absDistance = new(Mathf.Abs(currentPlayerChunkPos.x - chunkPos.x), Mathf.Abs(currentPlayerChunkPos.y - chunkPos.y));

                if (World.ActiveTerrain.TryAdd(chunkPos, Instantiate(World.ChunkPrefab, World.WorldTransform).GetComponent<Chunk>()))
                {
                    World.ActiveTerrain[chunkPos].SetPositions(chunkPos);
                    World.ActiveTerrain[chunkPos].MoveTransform();

                    if (absDistance.x <= World.LODOneDistance && absDistance.y <= World.LODOneDistance)
                    {
                        World.ActiveTerrain[chunkPos].AssignLODOne();
                    }
                    else if (absDistance.x <= World.LODTwoDistance && absDistance.y <= World.LODTwoDistance)
                    {
                        World.ActiveTerrain[chunkPos].AssignLODTwo();
                    }
                    else if (absDistance.x <= World.LODThreeDistance && absDistance.y <= World.LODThreeDistance)
                    {
                        World.ActiveTerrain[chunkPos].AssignLODThree();
                    }
                    else if (absDistance.x <= World.LODFourDistance && absDistance.y <= World.LODFourDistance)
                    {
                        World.ActiveTerrain[chunkPos].AssignLODFour();
                    }
                    else
                    {
                        World.ActiveTerrain[chunkPos].AssignLODFive();
                    }

                    World.ActiveTerrain[chunkPos].AssignMesh();
                }
            }
        }

        while (FoliageManager.FoliagesToAdd.Count > 0)
        {
            FoliageManager.Nextoliage();
        }

        NavMeshManager.Initialize();
        FoliageManager.Instance.enabled = true;
        World.Instance.enabled = true;
        PlayerStats.Instance.gameObject.SetActive(true);

        StartCoroutine(Initializer());
    }

    private IEnumerator Initializer()
    {
        yield return new WaitForSecondsRealtime(5);
        SceneTransitioner.ToggleScreen(false);
        RenderTerrainMap.ReloadBlending();
        Destroy(this);
        enabled = false;
    }
}