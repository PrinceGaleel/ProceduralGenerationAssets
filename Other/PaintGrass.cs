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
        InstantiatedComputeShader = Instantiate(World.GrassShader);
        InstantiatedMaterial = Instantiate(World.GrassMaterial);

        IdGrassKernel = InstantiatedComputeShader.FindKernel("Main");

        InstantiatedComputeShader.SetFloat("_WindSpeed", GrassChunk.WindSpeed);
        InstantiatedComputeShader.SetFloat("_WindStrength", GrassChunk.WindStrength);

        InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        InstantiatedComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", GrassChunk.BladesPerVertex);
        InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", GrassChunk.SegmentsPerBlade);

        InstantiatedComputeShader.SetFloat("_InteractorRadius", GrassChunk.GrassAffectRadius);
        InstantiatedComputeShader.SetFloat("_InteractorStrength", GrassChunk.GrassAffectStrength);

        InstantiatedComputeShader.SetFloat("_MinFadeDist", GrassChunk.MinFadeDistance);
        InstantiatedComputeShader.SetFloat("_MaxFadeDist", GrassChunk.MaxFadeDistance);

        InstantiatedComputeShader.SetFloat("_BladeForward", 0.2f);
        InstantiatedComputeShader.SetFloat("_BladeCurve", 3);
        InstantiatedComputeShader.SetFloat("_BottomWidth", 1);

        InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

        System.Random random = new();

        List<GrassChunk.SourceVertex> vertices = new();
        for (int i = 0, z = 0; z <= _Bounds.extents.x + _Bounds.center.x; z++)
        {
            for (int x = 0; x <= _Bounds.extents.z + _Bounds.center.z; x++)
            {
                for (int j = 0; j < GrassChunk.GrassDensity; j++)
                {
                    float randX = ((float)random.NextDouble() * 2 * GrassChunk.BrushSize) - GrassChunk.BrushSize + x + transform.position.x;
                    float randZ = ((float)random.NextDouble() * 2 * GrassChunk.BrushSize) - GrassChunk.BrushSize + z + transform.position.z;

                    if (Physics.Raycast(new(randX, 100, randZ), Vector3.down, out RaycastHit hit, 1000, LayerMask.GetMask("Terrain")))
                    {
                        vertices.Add(new GrassChunk.SourceVertex()
                        {
                            Position = hit.point,
                            Normal = Vector3.up,
                            UV = new(GrassChunk.GrassWidth, GrassChunk.GrassHeight),
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

            DrawBuffer = new ComputeBuffer(vertices.Count * GrassChunk.BladesPerVertex * ((GrassChunk.SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
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
        RenderTerrainMap.Instance.ReloadBlending();
    }

    private void LateUpdate()
    {
        // Clear the draw and indirect args buffers of last frame's data
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(new int[4] { 0, 1, 0, 0 });

        InstantiatedComputeShader.SetFloat("_Time", Time.time);
        InstantiatedComputeShader.SetVector("_CameraPositionWS", Cam.transform.position);

        // Dispatch the grass shader. It will run on the GPU
        InstantiatedComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        // DrawProceduralIndirect queues a draw call up for our generated mesh
        Graphics.DrawProceduralIndirect(InstantiatedMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, Cam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }

    private void OnDestroy()
    {
        Destroy(InstantiatedComputeShader);
        InstantiatedComputeShader = Instantiate(World.GrassShader);

        // Release each buffer
        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
    }
}
