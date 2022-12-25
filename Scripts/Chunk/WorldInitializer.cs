using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInitializer : MonoBehaviour
{
    private void Update()
    {
        if (World.MeshesToCreate.Count == 0 && World.MeshesToUpdate.Count == 0)
        {
            if (FoliageManager.FoliageToAdd.Count == 0)
            {
                PlayerController.Instance.gameObject.SetActive(true);
                SceneTransitioner.ToggleScreen(false);
                FoliageManager.Instance.enabled = true;
                Destroy(this);
                enabled = false;
            }
        }
    }
}