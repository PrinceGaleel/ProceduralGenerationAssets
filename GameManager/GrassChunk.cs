using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;

using static Chunk;
using static GameManager;
using static PerlinData;
using Unity.Burst;

public class GrassChunk : MonoBehaviour
{
    public Vector2Int MyPos;
    private List<SourceVertex> SourceVertices;
    public static List<GrassShaderInteractor> ShaderInteractors;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    [System.Serializable]
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
    public const float WindSpeed = 6;
    public const float WindStrength = 0.05f;
    public const int BladesPerVertex = 3;
    public const int SegmentsPerBlade = 3;
    public const float BrushSize = 10;
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

        ArgsBuffer?.Dispose();
        DrawBuffer?.Dispose();
        SourceVertBuffer?.Dispose();

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

    public readonly struct GrassUpdateJob : IJob
    {
        private readonly int GrassNum;
        private readonly Vector2Int NewPos;

        public GrassUpdateJob(Vector2Int newPos)
        {
            GrassNum = GrassManager.GrassChunks[newPos];
            NewPos = newPos;
        }

        public void Execute()
        {
            if (GrassManager.GrassChunks.ContainsKey(NewPos))
            {
                List<SourceVertex> newSourceVertices = new();
                Bounds newBounds = new(new(NewPos.x * ChunkSize, HeightMultipler / 2, NewPos.y * ChunkSize),
                    new(ChunkSize, HeightMultipler * 2, ChunkSize));

                Vector2Int max = new(((NewPos.x * ChunkSize) - ChunkSize / 2) + ChunkSize,
                   ((NewPos.y * ChunkSize) - ChunkSize / 2) + ChunkSize);

                lock (GrassManager.Randoms[GrassNum])
                {
                    for (int i = 0, y = (NewPos.y * ChunkSize) - ChunkSize / 2; y <= max.y; y++)
                    {
                        for (int x = (NewPos.x * ChunkSize) - ChunkSize / 2; x <= max.x; x++)
                        {
                            float randX = ((float)GrassManager.Randoms[GrassNum].NextDouble() * 2 * BrushSize) - BrushSize + x;
                            float randZ = ((float)GrassManager.Randoms[GrassNum].NextDouble() * 2 * BrushSize) - BrushSize + y;

                            newSourceVertices.Add(new()
                            {
                                Position = GetPerlinPosition(randX, randZ),
                                Normal = Vector3.up,
                                UV = new(GrassWidth, GrassHeight),
                                Color = TerrainColorGradient.TerrainColorAsVector3(randX, randZ)
                            });

                            i++;
                        }
                    }
                }

                new RefreshBuffers(GrassNum, NewPos, newSourceVertices, newBounds).Schedule();
            }
        }
    }

    private class RefreshBuffers : MainThreadJob
    {
        private readonly int GrassNum;
        private readonly Vector2Int MyPos;
        private readonly List<SourceVertex> NewSourceVertices;
        private readonly Bounds NewBounds;

        public RefreshBuffers(int grassNum, Vector2Int myPos, List<SourceVertex> newSourceVertices, Bounds newBounds)
        {
            GrassNum = grassNum;
            Priority = 0;
            MyPos = myPos;
            NewSourceVertices = newSourceVertices;
            NewBounds = newBounds;
        }

        public override void Execute()
        {
            if (GrassManager.GrassChunks.ContainsKey(MyPos))
            {
                GrassChunk grassChunk = GrassManager.References[GrassNum];
                if (MyPos == grassChunk.MyPos)
                {
                    if (NewSourceVertices.Count > 0)
                    {
                        grassChunk.UpdateGrass(NewSourceVertices, NewBounds);
                        grassChunk.enabled = true;
                    }
                    else
                    {
                        grassChunk.enabled = false;
                    }
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