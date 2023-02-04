using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintGrass : MonoBehaviour
{
    public BiomeData Biome;
    public Camera Cam;

    private ComputeShader InstantiatedComputeShader;
    private Material InstantiatedMaterial;

    private ComputeBuffer SourceVertBuffer;
    private ComputeBuffer DrawBuffer;
    private ComputeBuffer ArgsBuffer;

    private int IdGrassKernel;
    private int DispatchSize;

    public Bounds _Bounds;

    private void Awake()
    {
        InstantiatedComputeShader = Instantiate(GrassManager.GrassShader);
        InstantiatedMaterial = Instantiate(GrassManager.GrassMaterial);

        IdGrassKernel = InstantiatedComputeShader.FindKernel("Main");

        InstantiatedComputeShader.SetFloat("_WindSpeed", GrassManager.WindSpeed);
        InstantiatedComputeShader.SetFloat("_WindStrength", GrassManager.WindStrength);

        InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        InstantiatedComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", GrassManager.BladesPerVertex);
        InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", GrassManager.SegmentsPerBlade);

        InstantiatedComputeShader.SetFloat("_InteractorRadius", GrassManager.GrassAffectRadius);
        InstantiatedComputeShader.SetFloat("_InteractorStrength", GrassManager.GrassAffectStrength);

        InstantiatedComputeShader.SetFloat("_MinFadeDist", 50);
        InstantiatedComputeShader.SetFloat("_MaxFadeDist", 100);

        InstantiatedComputeShader.SetFloat("_BladeForward", 0.2f);
        InstantiatedComputeShader.SetFloat("_BladeCurve", 3);
        InstantiatedComputeShader.SetFloat("_BottomWidth", 1);

        InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

        System.Random random = new();

        List<GrassManager.SourceVertex> vertices = new();
        for (int i = 0, z = 0; z <= _Bounds.extents.x + _Bounds.center.x; z++)
        {
            for (int x = 0; x <= _Bounds.extents.z + _Bounds.center.z; x++)
            {
                for (int j = 0; j < GrassManager.GrassDensity; j++)
                {
                    float randX = ((float)random.NextDouble() * 2 * GrassManager.BrushSize) - GrassManager.BrushSize + x + transform.position.x;
                    float randZ = ((float)random.NextDouble() * 2 * GrassManager.BrushSize) - GrassManager.BrushSize + z + transform.position.z;

                    if (Physics.Raycast(new(randX, 100, randZ), Vector3.down, out RaycastHit hit, 1000, LayerMask.GetMask("Terrain")))
                    {
                        vertices.Add(new GrassManager.SourceVertex()
                        {
                            Position = hit.point,
                            Normal = Vector3.up,
                            UV = new(GrassManager.GrassWidth, GrassManager.GrassHeight),
                            Color = new(Biome.TerrainColor.r, Biome.TerrainColor.g, Biome.TerrainColor.b)
                        });
                    }
                }

                i++;
            }
        }

        if (vertices.Count > 0)
        {
            // Create compute buffers
            SourceVertBuffer = new ComputeBuffer(vertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            SourceVertBuffer.SetData(vertices);

            DrawBuffer = new ComputeBuffer(vertices.Count * GrassManager.BladesPerVertex * ((GrassManager.SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
            DrawBuffer.SetCounterValue(0);

            ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
            InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
            InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
            InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

            InstantiatedComputeShader.SetInt("_NumSourceVertices", vertices.Count);
            InstantiatedMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

            InstantiatedComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
            DispatchSize = Mathf.CeilToInt((float)vertices.Count / threadGroupSize);
        }
        else
        {
            Debug.Log("Alert: No grass created!");
            enabled = false;
        }
    }

    private void Start()
    {
        RenderTerrainMap.ReloadBlending();
    }

    private void LateUpdate()
    {
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(new int[4] { 0, 1, 0, 0 });

        InstantiatedComputeShader.SetFloat("_Time", Time.time);
        InstantiatedComputeShader.SetVector("_CameraPositionWS", Cam.transform.position);
        InstantiatedComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(InstantiatedMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, Cam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }

    private void OnDestroy()
    {
        Destroy(InstantiatedComputeShader);

        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
    }
}
