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

    private ComputeShader InstantiatedComputeShader;
    private Material InstantiatedMaterial;

    public const int GrassRenderDistance = 120;
    public static int RoundedGrassViewDistance = GrassRenderDistance / Chunk.ChunkSize;

    private ComputeBuffer SourceVertBuffer;  
    private ComputeBuffer DrawBuffer; 
    private ComputeBuffer ArgsBuffer; 

    private int IdGrassKernel;
    private int DispatchSize; 

    private Bounds _Bounds;

    // The size of one entry in the various compute buffers
    public const float WindSpeed = 6;
    public const float WindStrength = 0.05f;

    private void Awake()
    {
        InstantiatedComputeShader = Instantiate(World.Instance.GrassShader);
        InstantiatedMaterial = Instantiate(World.Instance.GrassMaterial);

        IdGrassKernel = InstantiatedComputeShader.FindKernel("Main");

        InstantiatedComputeShader.SetFloat("_WindSpeed", WindSpeed);
        InstantiatedComputeShader.SetFloat("_WindStrength", WindStrength);

        InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        InstantiatedComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", GlobalSettings.BladesPerVertex);
        InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", GlobalSettings.SegmentsPerBlade);

        InstantiatedComputeShader.SetFloat("_InteractorRadius", GlobalSettings.GrassAffectRadius);
        InstantiatedComputeShader.SetFloat("_InteractorStrength", GlobalSettings.GrassAffectStrength);

        InstantiatedComputeShader.SetFloat("_MinFadeDist", GlobalSettings.minFadeDistance);
        InstantiatedComputeShader.SetFloat("_MaxFadeDist", GlobalSettings.maxFadeDistance);

        InstantiatedComputeShader.SetFloat("_BladeForward", 0.05f);
        InstantiatedComputeShader.SetFloat("_BladeCurve", 1);
        InstantiatedComputeShader.SetFloat("_BottomWidth", 2);

        InstantiatedComputeShader.SetFloat("_OrthographicCamSize", Shader.GetGlobalFloat("_OrthographicCamSize"));
        InstantiatedComputeShader.SetVector("_OrthographicCamPos", Shader.GetGlobalVector("_OrthographicCamPos"));

        InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

        gameObject.layer = LayerMask.NameToLayer("Grass");
        enabled = false;
    }

    public void Initialize()
    {
        _Bounds = new(new Vector3(ParentChunk.WorldPosition.x, SaveData.HeightMultipler / 2, ParentChunk.WorldPosition.y), new Vector3(Chunk.ChunkSize, SaveData.HeightMultipler * 2, Chunk.ChunkSize));
        System.Random random = new();

        List<SourceVertex> vertices = new();
        for (int i = 0, z = 0; z <= Chunk.ChunkSize; z++)
        {
            for (int x = 0; x <= Chunk.ChunkSize; x++)
            {
                GrassSettings grassSettings = World.Biomes[ParentChunk.BiomeNums[i]]._GrassSettings;

                for (int j = 0; j < grassSettings.GrassDensity; j++)
                {
                    float brushSize = World.Biomes[ParentChunk.BiomeNums[i]]._GrassSettings.BrushSize;
                    float randX = ((float)random.NextDouble() * 2 * brushSize) - brushSize + x + ParentChunk.WorldPosition.x;
                    float randZ = ((float)random.NextDouble() * 2 * brushSize) - brushSize + z + ParentChunk.WorldPosition.y;
                    float height = Chunk.GetPerlinNoise(randX, randZ, World.CurrentSaveData.HeightPerlin) * SaveData.HeightMultipler;

                    vertices.Add(new SourceVertex()
                    {
                        Position = new Vector3(randX - (Chunk.ChunkSize / 2), height, randZ - (Chunk.ChunkSize / 2)),
                        Normal = Vector3.up,
                        UV = new(grassSettings.SizeWidth, grassSettings.SizeLength),
                        Color = new(World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.r, World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.g, World.Biomes[ParentChunk.BiomeNums[i]].TerrainColor.b)
                    });
                }

                i++;
            }
        }

        if (vertices.Count > 0)
        {
            lock (World.AssignGrassMesh)
            {
                World.AssignGrassMesh.Add(this, vertices);
            }
        }
    }

    public void AssignMesh(List<SourceVertex> vertices)
    {
        // Create compute buffers
        SourceVertBuffer = new ComputeBuffer(vertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        SourceVertBuffer.SetData(vertices);

        DrawBuffer = new ComputeBuffer(vertices.Count * (GlobalSettings.BladesPerVertex * ((GlobalSettings.SegmentsPerBlade - 1) * 2 + 1)), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
        DrawBuffer.SetCounterValue(0);

        ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

        InstantiatedComputeShader.SetInt("_NumSourceVertices", vertices.Count);
        InstantiatedMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

        InstantiatedComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
        DispatchSize = Mathf.CeilToInt((float)vertices.Count / threadGroupSize);

        ParentChunk.GrassReady = true;
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

    private void OnDestroy()
    {
        Destroy(InstantiatedComputeShader);
        InstantiatedComputeShader = Instantiate(World.Instance.GrassShader);

        // Release each buffer
        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
    }
}