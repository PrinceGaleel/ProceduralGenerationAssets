using System;
using UnityEngine;

public class GrassComputeScript : MonoBehaviour
{
    private ComputeShader InstantiatedComputeShader; // Instantiate the shaders so data belong to their unique compute buffers
    private Material InstantiatedMaterial; 

    private ComputeBuffer SourceVertBuffer; // A compute buffer to hold vertex data of the source mesh     
    private ComputeBuffer DrawBuffer; // A compute buffer to hold vertex data of the generated mesh    
    private ComputeBuffer ArgsBuffer; // A compute buffer to hold indirect draw arguments    

    private int IdGrassKernel;// The id of the kernel in the grass compute shader    ]
    private int DispatchSize; // The x dispatch size for the grass compute shader    

    // The size of one entry in the various compute buffers
    private const int SourceVertStride = sizeof(float) * (3 + 3 + 2 + 3);
    private const int DrawStride = sizeof(float) * (3 + (3 + 2 + 3) * 3);
    private const int IndirectArgsStride = sizeof(int) * 4;
    public const float WindSpeed = 6;
    public const float WindStrength = 0.05f;

    // The data to reset the args buffer with every frame
    // 0: vertex count per draw instance. We will only use one instance
    // 1: instance count. One
    // 2: start vertex location if using a Graphics Buffer
    // 3: and start instance location if using a Graphics Buffer
    private static readonly int[] ArgsBufferReset = new int[4] { 0, 1, 0, 0 };

    private Bounds _Bounds;

    public void Initialize(Mesh mesh)
    {
        InstantiatedComputeShader = Instantiate(World.Instance.GrassShader);
        InstantiatedMaterial = Instantiate(World.Instance.GrassMaterial);

        if (World.Instance.GrassShader == null || World.Instance.GrassMaterial == null || mesh.vertexCount == 0 || GlobalSettings.CurrentSettings == null)
        {
            enabled = false;
            return;
        }

        // Grab data from the source mesh
        Vector3[] positions = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        Color[] colors = mesh.colors;

        // Create the data to upload to the source vert buffer
        GrassChunk.SourceVertex[] vertices = new GrassChunk.SourceVertex[positions.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new GrassChunk.SourceVertex()
            {
                Position = positions[i],
                Normal = normals[i],
                UV = uvs[i],
                Color = new Vector3(colors[i].r, colors[i].g, colors[i].b)
            };
        }

        // Create compute buffers
        // The stride is the size, in bytes, each object in the buffer takes up
        SourceVertBuffer = new ComputeBuffer(vertices.Length, SourceVertStride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        SourceVertBuffer.SetData(vertices);

        DrawBuffer = new ComputeBuffer(vertices.Length * (GlobalSettings.BladesPerVertex * ((GlobalSettings.SegmentsPerBlade - 1) * 2 + 1)),
            DrawStride, ComputeBufferType.Append);
        DrawBuffer.SetCounterValue(0);

        ArgsBuffer = new ComputeBuffer(1, IndirectArgsStride, ComputeBufferType.IndirectArguments);

        // Cache the kernel IDs we will be dispatching
        IdGrassKernel = InstantiatedComputeShader.FindKernel("Main");

        // Set buffer data
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_SourceVertices", SourceVertBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_DrawTriangles", DrawBuffer);
        InstantiatedComputeShader.SetBuffer(IdGrassKernel, "_IndirectArgsBuffer", ArgsBuffer);

        // Set vertex data
        InstantiatedComputeShader.SetInt("_NumSourceVertices", vertices.Length);
        InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", GlobalSettings.BladesPerVertex);
        InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", GlobalSettings.SegmentsPerBlade);

        InstantiatedMaterial.SetBuffer("_DrawTriangles", DrawBuffer);

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then, divide the number of triangles by that size
        InstantiatedComputeShader.GetKernelThreadGroupSizes(IdGrassKernel, out uint threadGroupSize, out _, out _);
        DispatchSize = Mathf.CeilToInt((float)vertices.Length / threadGroupSize);

        // Get the bounds of the source mesh and then expand by the maximum blade width and height
        Bounds localBounds = mesh.bounds;
        localBounds.Expand(Mathf.Max(2, 2));

        // Transform the bounds to world space
        _Bounds = TransformBounds(localBounds);

        SetGrassDataBase();
        enabled = true;
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

    // LateUpdate is called after all Update calls
    private void LateUpdate()
    {
        // Clear the draw and indirect args buffers of last frame's data
        DrawBuffer.SetCounterValue(0);
        ArgsBuffer.SetData(ArgsBufferReset);

        // Update the shader with frame specific data
        SetGrassDataUpdate();

        // Dispatch the grass shader. It will run on the GPU
        InstantiatedComputeShader.Dispatch(IdGrassKernel, DispatchSize, 1, 1);

        // DrawProceduralIndirect queues a draw call up for our generated mesh
        Graphics.DrawProceduralIndirect(InstantiatedMaterial, _Bounds, MeshTopology.Triangles, ArgsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }

    private void SetGrassDataBase()
    {
        InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

        InstantiatedComputeShader.SetFloat("_GrassRandomHeight", 0.5f);

        InstantiatedComputeShader.SetFloat("_WindSpeed", WindSpeed);
        InstantiatedComputeShader.SetFloat("_WindStrength", WindStrength);

        InstantiatedComputeShader.SetFloat("_InteractorRadius", GlobalSettings.GrassAffectRadius);
        InstantiatedComputeShader.SetFloat("_InteractorStrength", GlobalSettings.GrassAffectStrength);

        InstantiatedComputeShader.SetFloat("_BladeForward", 0.05f);
        InstantiatedComputeShader.SetFloat("_BladeCurve", 1);
        InstantiatedComputeShader.SetFloat("_BottomWidth", 2);

        InstantiatedComputeShader.SetFloat("_MinFadeDist", GlobalSettings.minFadeDistance);
        InstantiatedComputeShader.SetFloat("_MaxFadeDist", GlobalSettings.maxFadeDistance);

        InstantiatedComputeShader.SetFloat("_OrthographicCamSize", Shader.GetGlobalFloat("_OrthographicCamSize"));
        InstantiatedComputeShader.SetVector("_OrthographicCamPos", Shader.GetGlobalVector("_OrthographicCamPos"));

        InstantiatedMaterial.SetColor("_TopTint", new Color(1, 1, 1));
        InstantiatedMaterial.SetColor("_BottomTint", new Color(0, 0, 1));
    }

    private void SetGrassDataUpdate()
    {
        InstantiatedComputeShader.SetFloat("_Time", Time.time);

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

        if (Camera.main != null)
        {
            InstantiatedComputeShader.SetVector("_CameraPositionWS", Camera.main.transform.position);
        }
    }

    private Bounds TransformBounds(Bounds boundsOS)
    {
        var center = transform.TransformPoint(boundsOS.center);

        // transform the local extents' axes
        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

        return new Bounds { center = center, extents = extents };
    }
}