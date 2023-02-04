using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class GrassManager : MonoBehaviour
{
    private static GrassManager Instance;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector3 Color;
    }
    private static List<SourceVertex> AllVertices;
    private static DictList<Vector2Int, List<SourceVertex>> GrassChunks;

    public static Material GrassMaterial;
    public static ComputeShader GrassShader;
    public static List<ShaderInteractor> ShaderInteractors;

    private static ComputeBuffer SourceVertBuffer;
    private static ComputeBuffer DrawBuffer;
    private static ComputeBuffer ArgsBuffer;

    private static int IdGrassKernel;
    private static int DispatchSize;

    private static Vector2Int LowestChunk;
    private static Vector2Int HighestChunk;
    private static Bounds _Bounds;

    [Header("Constants")]
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
        if (Instance)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;

            AllVertices = new();
            GrassChunks = new();
            ShaderInteractors = new();

            HighestChunk = Chunk.GetChunkPosition(World.CurrentSaveData.LastPosition.x, World.CurrentSaveData.LastPosition.y);
            LowestChunk = HighestChunk;

            IdGrassKernel = GrassShader.FindKernel("Main");

            GrassShader.SetFloat("_WindSpeed", WindSpeed);
            GrassShader.SetFloat("_WindStrength", WindStrength);

            GrassShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            GrassShader.SetFloat("_Time", Time.time);

            GrassShader.SetFloat("_GrassRandomHeight", 0.5f);

            GrassShader.SetInt("_MaxBladesPerVertex", BladesPerVertex);
            GrassShader.SetInt("_MaxSegmentsPerBlade", SegmentsPerBlade);

            GrassShader.SetFloat("_InteractorRadius", GrassAffectRadius);
            GrassShader.SetFloat("_InteractorStrength", GrassAffectStrength);

            GrassShader.SetFloat("_MinFadeDist", MinFadeDistance);
            GrassShader.SetFloat("_MaxFadeDist", MaxFadeDistance);

            GrassShader.SetFloat("_BladeForward", 0.2f);
            GrassShader.SetFloat("_BladeCurve", 3);
            GrassShader.SetFloat("_BottomWidth", 1);

            GrassMaterial.SetColor("_TopTint", new Color(1, 1, 1));
            GrassMaterial.SetColor("_BottomTint", new Color(0, 0, 1));

            gameObject.layer = LayerMask.NameToLayer("Grass");
        }

        gameObject.SetActive(false);
    }

    public static void RefreshBuffers()
    {
        if (AllVertices.Count > 0)
        {
            SourceVertBuffer = new ComputeBuffer(AllVertices.Count, sizeof(float) * (3 + 3 + 2 + 3), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            SourceVertBuffer.SetData(AllVertices);

            DrawBuffer = new ComputeBuffer(AllVertices.Count * BladesPerVertex * ((SegmentsPerBlade - 1) * 2 + 1), sizeof(float) * (3 + (3 + 2 + 3) * 3), ComputeBufferType.Append);
            DrawBuffer.SetCounterValue(0);

            ArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
            GrassShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
            GrassShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
            GrassShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

            GrassShader.SetInt("_NumSourceVertices", AllVertices.Count);
            GrassMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

            GrassShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
            DispatchSize = Mathf.CeilToInt((float)AllVertices.Count / threadGroupSize);

            RenderTerrainMap.ReloadBlending();
            Instance.gameObject.SetActive(true);
        }
        else
        {
            Instance.gameObject.SetActive(false);
        }
    }

    public struct AssignGrassPositionsJob : IJob
    {
        private readonly Vector2Int StartPosition;

        public AssignGrassPositionsJob(Vector2Int playerPosition)
        {
            StartPosition = playerPosition - new Vector2Int(Chunk.DefaultChunkSize / 2, Chunk.DefaultChunkSize / 2);
        }

        public void Execute()
        {
            System.Random random = new();
            List<SourceVertex> vertices = new();

            for (int i = 0, z = 0; z <= Chunk.DefaultChunkSize; z++)
            {
                for (int x = 0; x <= Chunk.DefaultChunkSize; x++)
                {
                    for (int j = 0; j < GrassDensity; j++)
                    {
                        float randX = ((float)random.NextDouble() * 2 * BrushSize) - BrushSize + x + StartPosition.x;
                        float randZ = ((float)random.NextDouble() * 2 * BrushSize) - BrushSize + z + StartPosition.y;

                        vertices.Add(new SourceVertex()
                        {
                            Position = Chunk.GetPerlinPosition(randX, randZ),
                            Normal = Vector3.up,
                            UV = new(GrassWidth, GrassHeight),
                            Color = TerrainGradient.TerrainColorAsVector3(randX, randX)
                        });
                    }

                    i++;
                }
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(GrassShader);
        Destroy(GrassMaterial);

        SourceVertBuffer?.Release();
        DrawBuffer?.Release();
        ArgsBuffer?.Release();
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
            GrassShader.SetVectorArray(shaderID, positions);
            GrassShader.SetFloat("_InteractorsLength", ShaderInteractors.Count);
        }

        GrassShader.SetFloat("_Time", Time.time);
        GrassShader.SetVector("_CameraPositionWS", CameraController.Instance.MainCam.transform.position);
        GrassShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(GrassMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, CameraController.Instance.MainCam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }
}
