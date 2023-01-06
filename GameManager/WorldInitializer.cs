using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInitializer : MonoBehaviour
{
    private void Start()
    {
        World.CheckForNewChunks();

        while (World.MeshesToCreate.Count > 0)
        {
            World.ActiveTerrain[World.MeshesToCreate.Dequeue()].CreateMesh();
        }

        while (World.MeshesToUpdate.Count > 0)
        {
            World.ActiveTerrain[World.MeshesToUpdate.Dequeue()].AssignMesh();
        }

        FoliageManager.PopulateChunks();

        World.CheckForNewChunks();

        if (World.StructuresToCreate.Count > 0)
        {
            lock (World.StructuresToCreate)
            {
                CustomPair<Chunk, StructureTypes> pair = World.StructuresToCreate.TakePairAt(0);

                if (pair.Value == StructureTypes.Village)
                {
                    StructureCreator.CreateVillage(pair.Key);
                }
                else if (pair.Value == StructureTypes.MobDen)
                {
                    Instantiate(StructureCreator.Instance.MobDenPrefabs[Random.Range(0, StructureCreator.Instance.MobDenPrefabs.Length)],
                        Chunk.GetPerlinPosition(pair.Key.WorldPosition.x, pair.Key.WorldPosition.y), Quaternion.identity).transform.SetParent(pair.Key.StructureParent);
                }
            }
        }
    }

    private void Update()
    {
        if (FoliageManager.FoliageToAdd.Count == 0)
        {
            PlayerStats.PlayerTransform.gameObject.SetActive(true);
            NavMeshManager.CheckNavMesh();
            SceneTransitioner.ToggleScreen(false);
            FoliageManager.Instance.enabled = true;

            World.Instance.enabled = true;
            World.ChunkThread.Start();
            Destroy(this);
            enabled = false;
        }
    }
}