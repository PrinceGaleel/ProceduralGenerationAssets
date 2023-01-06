using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassChunk : MonoBehaviour
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector3 Color;
    }

    public Chunk ParentChunk;
    private List<SourceVertex> Vertices;
    
    private ComputeShader InstantiatedComputeShader;
    private Material InstantiatedMaterial;

    private ComputeBuffer SourceVertBuffer;  
    private ComputeBuffer DrawBuffer; 
    private ComputeBuffer ArgsBuffer; 

    private int IdGrassKernel;
    private int DispatchSize; 

    private Bounds _Bounds;

    [Header("Constants")]
    public const float WindSpeed = 6;
    public const float WindStrength = 0.05f;

    public const int BladesPerVertex = 3;
    public const int SegmentsPerBlade = 3;

    public const float BrushSize = 10;
    public const int GrassDensity = 5;

    public const float GrassWidth = 0.1f;
    public const float GrassHeight = 0.3f;

    public const float MinFadeDistance = 40;
    public const float MaxFadeDistance = 60;

    public const float GrassAffectRadius = 0.8f;
    public const float GrassAffectStrength = 2;

    private void Awake()
    {
        InstantiatedComputeShader = Instantiate(World.GrassShader);
        InstantiatedMaterial = Instantiate(World.GrassMaterial);

        IdGrassKernel = InstantiatedComputeShader.FindKernel("Main");

        InstantiatedComputeShader.SetFloat("_WindSpeed", WindSpeed);
        InstantiatedComputeShader.SetFloat("_WindStrength", WindStrength);

        InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        InstantiatedComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", BladesPerVertex);
        InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", SegmentsPerBlade);

        InstantiatedComputeShader.SetFloat("_InteractorRadius", GrassAffectRadius);
        InstantiatedComputeShader.SetFloat("_InteractorStrength", GrassAffectStrength);

        InstantiatedComputeShader.SetFloat("_MinFadeDist", MinFadeDistance);
        InstantiatedComputeShader.SetFloat("_MaxFadeDist", MaxFadeDistance);

        InstantiatedComputeShader.SetFloat("_BladeForward", 0.2f);
        InstantiatedComputeShader.SetFloat("_BladeCurve", 3);
        InstantiatedComputeShader.SetFloat("_BottomWidth", 1);

        InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

        gameObject.layer = LayerMask.NameToLayer("Grass");
        enabled = false;
    }

    public void Initialize()
    {
        _Bounds = new(new Vector3(ParentChunk.WorldPosition.x, SaveData.HeightMultipler / 2, ParentChunk.WorldPosition.y), new Vector3(Chunk.ChunkSize, SaveData.HeightMultipler * 2, Chunk.ChunkSize));
        System.Random random = new();

        Vertices = new();
        for (int i = 0, z = 0; z <= Chunk.ChunkSize; z++)
        {
            for (int x = 0; x <= Chunk.ChunkSize; x++)
            {
                for (int j = 0; j < GrassDensity; j++)
                {
                    float randX = ((float)random.NextDouble() * 2 * BrushSize) - BrushSize + x + ParentChunk.WorldPosition.x;
                    float randZ = ((float)random.NextDouble() * 2 * BrushSize) - BrushSize + z + ParentChunk.WorldPosition.y;
                    float height = Chunk.GetPerlinNoise(randX, randZ, World.CurrentSaveData.HeightPerlin) * SaveData.HeightMultipler;

                    Vertices.Add(new SourceVertex()
                    {
                        Position = new Vector3(randX - (Chunk.ChunkSize / 2), height, randZ - (Chunk.ChunkSize / 2)),
                        Normal = Vector3.up,
                        UV = new(GrassWidth, GrassHeight),
                        Color = new(World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.r, World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.g, World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.b)
                    });
                }

                i++;
            }
        }

        ParentChunk.GrassReady = true;
    }

    public void OnEnable()
    {
        // Create compute buffers
        SourceVertBuffer = new ComputeBuffer(Vertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        SourceVertBuffer.SetData(Vertices);

        DrawBuffer = new ComputeBuffer(Vertices.Count * BladesPerVertex * ((SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
        DrawBuffer.SetCounterValue(0);

        ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);

        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

        InstantiatedComputeShader.SetInt("_NumSourceVertices", Vertices.Count);
        InstantiatedMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

        InstantiatedComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
        DispatchSize = Mathf.CeilToInt((float)Vertices.Count / threadGroupSize);

        ParentChunk.GrassReady = true;
    }

    private void OnDisable()
    {
        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
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

    private void LateUpdate()
    {
        // Clear the draw and indirect args buffers of last frame's data
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(new int[4] { 0, 1, 0, 0 });

        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        // Update the shader with frame specific data
        if (World.ShaderInteractors.Count > 0)
        {
            Vector4[] positions = new Vector4[World.ShaderInteractors.Count];
            for (int i = 0; i < World.ShaderInteractors.Count; i++)
            {
                positions[i] = World.ShaderInteractors[i].transform.position;

            }
            int shaderID = Shader.PropertyToID("_PositionsMoving");
            InstantiatedComputeShader.SetVectorArray(shaderID, positions);
            InstantiatedComputeShader.SetFloat("_InteractorsLength", World.ShaderInteractors.Count);
        }

        InstantiatedComputeShader.SetVector("_CameraPositionWS", CameraController.Instance.MainCam.transform.position);        

        // Dispatch the grass shader. It will run on the GPU
        InstantiatedComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        // DrawProceduralIndirect queues a draw call up for our generated mesh
        Graphics.DrawProceduralIndirect(InstantiatedMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, CameraController.Instance.MainCam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }
}