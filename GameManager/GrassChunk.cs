using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassChunk : MonoBehaviour
{
    private Vector2Int MyPos;
    private List<SourceVertex> SourceVertices;
    public static List<GrassShaderInteractor> ShaderInteractors;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector3 Color;
    }

    private Bounds MyGrassBounds;
    public static int GrassLayer;

    public Material MyGrassMaterial;
    private ComputeShader MyComputeShader;
    private ComputeBuffer SourceVertBuffer;
    private ComputeBuffer DrawBuffer;
    private ComputeBuffer ArgsBuffer;

    private static int IdGrassKernel;
    private static int DispatchSize;

    [Header("Constants")]
    private const float MinimumDirtPerlin = 0.15f;

    public const float WindSpeed = 6;
    public const float WindStrength = 0.05f;

    public const int BladesPerVertex = 3;
    public const int SegmentsPerBlade = 3;

    public const float BrushSize = 10;
    public const int GrassDensity = 3;

    public const float GrassWidth = 0.1f;
    public const float GrassHeight = 0.3f;

    public const float MinFadeDistance = 40;
    public const float MaxFadeDistance = 60;

    public const float GrassAffectRadius = 0.8f;
    public const float GrassAffectStrength = 1;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Grass");

        MyComputeShader = Instantiate(GrassManager.GrassComputeShader);
        MyGrassMaterial = Instantiate(GrassManager.GrassMaterial);

        IdGrassKernel = MyComputeShader.FindKernel("Main");

        MyComputeShader.SetFloat("_WindSpeed", WindSpeed);
        MyComputeShader.SetFloat("_WindStrength", WindStrength);

        MyComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        MyComputeShader.SetFloat("_Time", Time.time);

        MyComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        MyComputeShader.SetInt("_MaxBladesPerVertex", BladesPerVertex);
        MyComputeShader.SetInt("_MaxSegmentsPerBlade", SegmentsPerBlade);

        MyComputeShader.SetFloat("_InteractorRadius", GrassAffectRadius);
        MyComputeShader.SetFloat("_InteractorStrength", GrassAffectStrength);

        MyComputeShader.SetFloat("_MinFadeDist", MinFadeDistance);
        MyComputeShader.SetFloat("_MaxFadeDist", MaxFadeDistance);

        MyComputeShader.SetFloat("_BladeForward", 0.2f);
        MyComputeShader.SetFloat("_BladeCurve", 3);
        MyComputeShader.SetFloat("_BottomWidth", 1);

        MyGrassMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        MyGrassMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

        SourceVertices = new();
        enabled = false;
    }

    private void LateUpdate()
    {
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(new int[4] { 0, 1, 0, 0 });

        if (ShaderInteractors.Count > 0)
        {
            Vector4[] positions = new Vector4[ShaderInteractors.Count];
            for (int i = 0; i < ShaderInteractors.Count; i++)
            {
                positions[i] = ShaderInteractors[i].transform.position;

            }
            int shaderID = Shader.PropertyToID("_PositionsMoving");
            MyComputeShader.SetVectorArray(shaderID, positions);
            MyComputeShader.SetFloat("_InteractorsLength", ShaderInteractors.Count);
        }

        MyComputeShader.SetFloat("_Time", Time.time);
        MyComputeShader.SetVector("_CameraPositionWS", CameraController.Instance.MainCam.transform.position);
        MyComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(MyGrassMaterial, MyGrassBounds, MeshTopology.Triangles, ArgsBuffer, 0, CameraController.Instance.MainCam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, GrassLayer);
    }

    public void UpdateGrass(List<SourceVertex> vertices, Bounds bounds)
    {
        SourceVertices = vertices;
        MyGrassBounds = bounds;

        SourceVertBuffer = new ComputeBuffer(SourceVertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        SourceVertBuffer.SetData(SourceVertices);

        DrawBuffer = new ComputeBuffer(SourceVertices.Count * BladesPerVertex * ((SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
        DrawBuffer.SetCounterValue(0);

        ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        MyComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
        MyComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
        MyComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

        MyComputeShader.SetInt("_NumSourceVertices", SourceVertices.Count);
        MyGrassMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

        MyComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
        DispatchSize = Mathf.CeilToInt((float)SourceVertices.Count / threadGroupSize);

        RenderTerrainMap.ReloadBlending();
    }

    public class GrassUpdateJob : SecondaryThreadJob
    {
        private readonly Vector2Int NewPos;
        private readonly GrassChunk MyGrassChunk;
        private readonly System.Random Rnd;
        public bool IsCompleted { get; set; }

        public GrassUpdateJob(GrassChunk grassChunk, Vector2Int newPos)
        {
            MyGrassChunk = grassChunk;
            MyGrassChunk.MyPos = newPos;
            NewPos = newPos;
            Rnd = new();
            IsCompleted = false;
        }

        public override void Execute()
        {
            List<SourceVertex> newSourceVertices = new();
            Bounds newBounds = new(new(NewPos.x * Chunk.DefaultChunkSize, Chunk.HeightMultipler / 2, NewPos.y * Chunk.DefaultChunkSize),
                new(Chunk.DefaultChunkSize, Chunk.HeightMultipler * 2, Chunk.DefaultChunkSize));

            Vector2Int max = new(((NewPos.x * Chunk.DefaultChunkSize) - Chunk.DefaultChunkSize / 2) + Chunk.DefaultChunkSize,
               ((NewPos.y * Chunk.DefaultChunkSize) - Chunk.DefaultChunkSize / 2) + Chunk.DefaultChunkSize);

            for (int i = 0, y = (NewPos.y * Chunk.DefaultChunkSize) - Chunk.DefaultChunkSize / 2; y <= max.y; y++)
            {
                for (int x = (NewPos.x * Chunk.DefaultChunkSize) - Chunk.DefaultChunkSize / 2; x <= max.x; x++)
                {
                    for (int j = 0; j < GrassDensity; j++)
                    {
                        float randX = ((float)Rnd.NextDouble() * 2 * BrushSize) - BrushSize + x;
                        float randZ = ((float)Rnd.NextDouble() * 2 * BrushSize) - BrushSize + y;
                        float dirtPerlin = Chunk.GetDirtPerlin(randX, randZ);

                        if (Chunk.GetDirtPerlin(randX, randZ) > MinimumDirtPerlin)
                        {
                            newSourceVertices.Add(new()
                            {
                                Position = Chunk.GetPerlinPosition(randX, randZ),
                                Normal = Vector3.up,
                                UV = new(GrassWidth, GrassHeight),
                                Color = TerrainGradient.TerrainColorAsVector3(randX, randZ, dirtPerlin)
                            });
                        }

                        i++;
                    }
                }
            }

            IsCompleted = true;
            new RefreshBuffers(NewPos, MyGrassChunk, newSourceVertices, newBounds).Schedule();
        }
    }

    private class RefreshBuffers : MainThreadJob
    {
        private readonly Vector2Int MyPos;
        private readonly GrassChunk MyGrassChunk;
        private readonly List<SourceVertex> NewSourceVertices;
        private readonly Bounds NewBounds;

        public RefreshBuffers(Vector2Int myPos, GrassChunk grassChunk, List<SourceVertex> newSourceVertices, Bounds newBounds)
        {
            MyPos = myPos;
            MyGrassChunk = grassChunk;
            NewSourceVertices = newSourceVertices;
            NewBounds = newBounds;
        }

        public override void Execute()
        {
            if (MyPos == MyGrassChunk.MyPos)
            {
                if (NewSourceVertices.Count > 0)
                {
                    MyGrassChunk.UpdateGrass(NewSourceVertices, NewBounds);
                    MyGrassChunk.enabled = true;
                }
                else
                {
                    MyGrassChunk.enabled = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(MyGrassMaterial);
        Destroy(MyComputeShader);

        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
    }
}