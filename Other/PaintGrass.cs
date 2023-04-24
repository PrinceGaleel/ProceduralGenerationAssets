using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintGrass : MonoBehaviour
{
    [SerializeField] private BiomeData Biome;
    [SerializeField] private Camera Cam;

    [SerializeField] private ComputeShader MyComputeShader;
    [SerializeField] private Material InstantiatedMaterial;

    [SerializeField] private ComputeBuffer SourceVertBuffer;
    [SerializeField] private ComputeBuffer DrawBuffer;
    [SerializeField] private ComputeBuffer ArgsBuffer;

    [SerializeField] private int IdGrassKernel;
    [SerializeField] private int DispatchSize;

    [SerializeField] private List<GrassChunk.SourceVertex> Vertices = new();
    [SerializeField] private Bounds _Bounds;

    private void Awake()
    {
        if (!Cam)
        {
            Cam = Camera.main;
        }

        GrassChunk.ShaderInteractors = new();
    }

    private void Start()
    {
        if (GrassManager.GrassComputeShader != null && GrassManager.GrassMaterial != null && Vertices.Count > 0)
        {
            MyComputeShader = Instantiate(GrassManager.GrassComputeShader);
            InstantiatedMaterial = Instantiate(GrassManager.GrassMaterial);

            IdGrassKernel = MyComputeShader.FindKernel("Main");
            MyComputeShader.SetFloat("_WindSpeed", GrassChunk.WindSpeed);
            MyComputeShader.SetFloat("_WindStrength", GrassChunk.WindStrength);
            MyComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            MyComputeShader.SetFloat("_Time", Time.time);
            MyComputeShader.SetFloat("_GrassRandomHeight", 0.5f);
            MyComputeShader.SetInt("_MaxBladesPerVertex", GrassChunk.BladesPerVertex);
            MyComputeShader.SetInt("_MaxSegmentsPerBlade", GrassChunk.SegmentsPerBlade);
            MyComputeShader.SetFloat("_InteractorRadius", GrassChunk.GrassAffectRadius);
            MyComputeShader.SetFloat("_InteractorStrength", GrassChunk.GrassAffectStrength);
            MyComputeShader.SetFloat("_MinFadeDist", 50);
            MyComputeShader.SetFloat("_MaxFadeDist", 100);
            MyComputeShader.SetFloat("_BladeForward", 0.2f);
            MyComputeShader.SetFloat("_BladeCurve", 3);
            MyComputeShader.SetFloat("_BottomWidth", 1);

            InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
            InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));
            SourceVertBuffer = new ComputeBuffer(Vertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            SourceVertBuffer.SetData(Vertices);
            DrawBuffer = new ComputeBuffer(Vertices.Count * GrassChunk.BladesPerVertex * ((GrassChunk.SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
            DrawBuffer.SetCounterValue(0);
            ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
            MyComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
            MyComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
            MyComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);
            MyComputeShader.SetInt("_NumSourceVertices", Vertices.Count);
            InstantiatedMaterial.SetBuffer("_DrawTriangles", DrawBuffer);
            MyComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
            DispatchSize = Mathf.CeilToInt((float)Vertices.Count / threadGroupSize);

            RenderTerrainMap.ReloadBlending();
        }
        else
        {
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(new int[4] { 0, 1, 0, 0 });

        MyComputeShader.SetFloat("_Time", Time.time);
        MyComputeShader.SetVector("_CameraPositionWS", Cam.transform.position);
        MyComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(InstantiatedMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, Cam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }

    private void OnDestroy()
    {
        Destroy(MyComputeShader);

        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
    }

#if UNITY_EDITOR
    [SerializeField] protected bool Refresh = true;

    private void OnValidate()
    {
        if (Refresh)
        {
            Refresh = false;
            System.Random random = new();
            Vertices = new();
            for (int i = 0, z = 0; z <= _Bounds.extents.x + _Bounds.center.x; z++)
            {
                for (int x = 0; x <= _Bounds.extents.z + _Bounds.center.z; x++)
                {
                    float randX = ((float)random.NextDouble() * 2 * GrassChunk.BrushSize) - GrassChunk.BrushSize + x + transform.position.x;
                    float randZ = ((float)random.NextDouble() * 2 * GrassChunk.BrushSize) - GrassChunk.BrushSize + z + transform.position.z;

                    if (Physics.Raycast(new(randX, 100, randZ), Vector3.down, out RaycastHit hit, 1000, LayerMask.GetMask("Terrain")))
                    {
                        Vertices.Add(new GrassChunk.SourceVertex()
                        {
                            Position = hit.point,
                            Normal = Vector3.up,
                            UV = new(GrassChunk.GrassWidth, GrassChunk.GrassHeight),
                            Color = new(Biome.TerrainColor.r, Biome.TerrainColor.g, Biome.TerrainColor.b)
                        });
                    }

                    i++;
                }
            }
        }
    }
#endif
}
