using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTerrainMap : MonoBehaviour
{
    public static RenderTerrainMap Instance;

    public Transform Focus;
    private Camera CamToDrawWidth;
    public LayerMask Layer;

    public const int Resolution = 512;
    public const float AdjustScaling = 2.5f;

    private RenderTexture TempTexture;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple render terrain maps instances detected");
            Destroy(this);
            enabled = false;
        }
        else
        {
            Instance = this;
            
            gameObject.layer = LayerMask.NameToLayer("Terrain");
            CamToDrawWidth = GetComponent<Camera>();

            transform.position = new(transform.position.x, SaveData.HeightMultipler * 1.5f, transform.position.z);
            CamToDrawWidth.farClipPlane = SaveData.HeightMultipler * 2;
            CamToDrawWidth.nearClipPlane = 0;

            Bounds bounds = new(transform.position, new(World.GrassRenderDistance, SaveData.HeightMultipler * 2, World.GrassRenderDistance));
            CamToDrawWidth.cullingMask = Layer;
            CamToDrawWidth.orthographicSize = bounds.size.magnitude / AdjustScaling;

            TempTexture = new(Resolution, Resolution, 24);
            CamToDrawWidth.targetTexture = TempTexture;
            CamToDrawWidth.depthTextureMode = DepthTextureMode.Depth;
        }
    }

    private void Start()
    {
        if (!Focus)
        {
            Focus = PlayerStats.PlayerTransform;
        }

        enabled = false;
    }

    public void ReloadBlending()
    {
        transform.position = new(Focus.position.x, transform.position.y, Focus.position.z);

        CamToDrawWidth.Render();

        Shader.SetGlobalFloat("_OrthographicCamSize", CamToDrawWidth.orthographicSize);
        Shader.SetGlobalVector("_OrthographicCamPos", transform.position);
        Shader.SetGlobalTexture("_TerrainDiffuse", TempTexture);
    }
}