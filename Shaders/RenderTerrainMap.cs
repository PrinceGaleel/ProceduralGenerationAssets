using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTerrainMap : MonoBehaviour
{
    public static RenderTerrainMap Instance;

    private Camera CamToDrawWidth;
    public LayerMask Layer;

    public List<Renderer> Renderers;

    public const int Resoltuion = 512;
    public const float AdjustScaling = 2.5f;

    [SerializeField]
    private bool RealTimeDiffuse;
    private RenderTexture TempTex;

    private Bounds Bounds;
    // resolution of the map

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple render terrain maps instances detected");
            Destroy(this);
        }
        else
        {
            Instance = this;
            gameObject.layer = LayerMask.NameToLayer("Terrain");
            CamToDrawWidth = GetComponent<Camera>();
                        
            CamToDrawWidth.farClipPlane = SaveData.HeightMultipler;
            CamToDrawWidth.nearClipPlane = World.Biomes[0].HighestPoint * SaveData.HeightMultipler;
        }

        enabled = false;
    }

    private void Start()
    {
        GetBounds();
        SetUpCam();
        DrawToMap("_TerrainDiffuse");
    }

    private void GetBounds()
    {
        Bounds = new Bounds(transform.position, Vector3.zero);

        if (Renderers.Count > 0)
        {
            foreach (Renderer renderer in Renderers)
            {
                Bounds.Encapsulate(renderer.bounds);
            }
        }
    }

    public void ReloadBlending()
    {
        TempTex = new(Resoltuion, Resoltuion, 24);
        GetBounds();
        SetUpCam();
        DrawToMap("_TerrainDiffuse");
    }

    private void OnEnable()
    {
        ReloadBlending();
    }

    private void OnRenderObject()
    {
        if (!RealTimeDiffuse)
        {
            return;
        }

        UpdateTex();
    }

    private void UpdateTex()
    {
        CamToDrawWidth.enabled = true;
        CamToDrawWidth.targetTexture = TempTex;
        Shader.SetGlobalTexture("_TerrainDiffuse", TempTex);
    }

    private void DrawToMap(string target)
    {
        CamToDrawWidth.enabled = true;
        CamToDrawWidth.targetTexture = TempTex;
        CamToDrawWidth.depthTextureMode = DepthTextureMode.Depth;
        Shader.SetGlobalFloat("_OrthographicCamSize", CamToDrawWidth.orthographicSize);
        Shader.SetGlobalVector("_OrthographicCamPos", CamToDrawWidth.transform.position);
        CamToDrawWidth.Render();
        Shader.SetGlobalTexture(target, TempTex);
        CamToDrawWidth.enabled = false;
    }

    private void SetUpCam()
    {
        if (CamToDrawWidth == null)
        {
            CamToDrawWidth = GetComponentInChildren<Camera>();
        }

        float size = Bounds.size.magnitude;
        CamToDrawWidth.cullingMask = Layer;
        CamToDrawWidth.orthographicSize = size / AdjustScaling;
        CamToDrawWidth.transform.parent = null;
        CamToDrawWidth.transform.position = new(Bounds.center.x, SaveData.HeightMultipler, Bounds.center.z);
        CamToDrawWidth.transform.parent = gameObject.transform;
    }
}