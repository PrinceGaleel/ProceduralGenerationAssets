using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class RenderTerrainMap : MonoBehaviour
{
    [SerializeField] private bool IsManualBounds = false;

    public static RenderTerrainMap Instance;
    private static Transform MyTransform;

    public Transform Focus;
    private static Camera CamToDrawWidth;

    public const int Resolution = 512;
    public const float AdjustScaling = 2.5f;

    private static RenderTexture TempTexture;

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
            MyTransform = transform;

            gameObject.layer = LayerMask.NameToLayer("Terrain");
            CamToDrawWidth = GetComponent<Camera>();

            if (!IsManualBounds)
            {
                Bounds bounds = new(transform.position, new(((GrassManager.MaxGrassDistance * 2) + 1) * Chunk.ChunkSize, PerlinData.HeightMultipler * 2, ((GrassManager.MaxGrassDistance * 2) + 1) * Chunk.ChunkSize));
                CamToDrawWidth.orthographicSize = bounds.size.magnitude / AdjustScaling;

                MyTransform.position = new(MyTransform.position.x, PerlinData.HeightMultipler * 1.5f, MyTransform.position.z);

                CamToDrawWidth.farClipPlane = PerlinData.HeightMultipler * 2;
                CamToDrawWidth.nearClipPlane = 0;
            }

            CamToDrawWidth.cullingMask = LayerMask.GetMask("Terrain");

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

        ReloadBlending();
        CamToDrawWidth.enabled = false;
    }

    public static void ReloadBlending()
    {
        CamToDrawWidth.enabled = true;
        MyTransform.position = new(Instance.Focus.position.x, MyTransform.position.y, Instance.Focus.position.z);

        CamToDrawWidth.Render();

        Shader.SetGlobalFloat("_OrthographicCamSize", CamToDrawWidth.orthographicSize);
        Shader.SetGlobalVector("_OrthographicCamPos", MyTransform.position);
        Shader.SetGlobalTexture("_TerrainDiffuse", TempTexture);
        CamToDrawWidth.enabled = false;
    }
}