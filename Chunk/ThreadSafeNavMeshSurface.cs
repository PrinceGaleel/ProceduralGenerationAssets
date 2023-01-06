using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ThreadSafeNavMeshSurface : MonoBehaviour
{
    [SerializeField]
    private int AgentTypeID;

    public Vector3 Size = new(10.0f, 10.0f, 10.0f);
    public Vector3 Center = new(0, 2.0f, 0);
    public LayerMask LayerMask = ~0;

    [Header("Advanced")]
    [SerializeField] private int DefaultArea;
    [SerializeField] private bool OverrideTileSize;
    [SerializeField] private int TileSize = 256;
    [SerializeField] private bool OverrideVoxelSize;
    [SerializeField] private float VoxelSize;
    private Transform _Transform;

    [Header("Currently Not Supported Advanced Options")]
    [SerializeField] private bool BuildHeightMesh;
    public NavMeshData NavMeshData;

    private NavMeshDataInstance NavMeshDataInstance;
    private Vector3 LastPosition = Vector3.zero;
    private Quaternion LastRotation = Quaternion.identity;

    [Header("Surface")]
    private static readonly List<ThreadSafeNavMeshSurface> NavMeshSurfaces = new();
    public NavMeshBuildSettings BuildSettings;

    private void Awake()
    {
        _Transform = transform;

        BuildSettings = NavMesh.GetSettingsByID(AgentTypeID);
        if (BuildSettings.agentTypeID == -1)
        {
            Debug.LogWarning("No build settings for agent type ID " + AgentTypeID, this);
            BuildSettings.agentTypeID = AgentTypeID;
        }

        if (OverrideTileSize)
        {
            BuildSettings.overrideTileSize = true;
            BuildSettings.tileSize = TileSize;
        }
        if (OverrideVoxelSize)
        {
            BuildSettings.overrideVoxelSize = true;
            BuildSettings.voxelSize = VoxelSize;
        }
    }

    private void OnEnable()
    {
        Register(this);
        AddData();
    }

    private void OnDisable()
    {
        RemoveData();
        Unregister(this);
    }

    private void AddData()
    {
        if (!NavMeshDataInstance.valid)
        {
            if (NavMeshData != null)
            {
                NavMeshDataInstance = NavMesh.AddNavMeshData(NavMeshData, _Transform.position, _Transform.rotation);
                NavMeshDataInstance.owner = this;
            }

            LastPosition = _Transform.position;
            LastRotation = _Transform.rotation;
        }
    }

    private void RemoveData()
    {
        NavMeshDataInstance.Remove();
        NavMeshDataInstance = new();
    }

    public NavMeshData BuildNavMesh()
    {
        List<NavMeshBuildSource> sources = CollectSources();
        Bounds sourcesBounds = CalculateWorldBounds(sources);
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(BuildSettings, sources, sourcesBounds, _Transform.position, _Transform.rotation);

        if (data != null)
        {
            data.name = gameObject.name;
            RemoveData();
            NavMeshData = data;
            AddData();
        }

        return data;
    }

    public AsyncOperation UpdateNavMesh(NavMeshData data)
    {
        List<NavMeshBuildSource> sources = CollectSources();
        Bounds sourcesBounds = CalculateWorldBounds(sources);
        return NavMeshBuilder.UpdateNavMeshDataAsync(data, BuildSettings, sources, sourcesBounds);
    }

    private static void Register(ThreadSafeNavMeshSurface surface)
    {
        if (NavMeshSurfaces.Count == 0)
        {
            NavMesh.onPreUpdate += UpdateActive;
        }

        if (!NavMeshSurfaces.Contains(surface))
        {
            NavMeshSurfaces.Add(surface);
        }
    }

    private static void Unregister(ThreadSafeNavMeshSurface surface)
    {
        NavMeshSurfaces.Remove(surface);

        if (NavMeshSurfaces.Count == 0)
        {
            NavMesh.onPreUpdate -= UpdateActive;
        }
    }

    private static void UpdateActive()
    {
        for (int i = 0; i < NavMeshSurfaces.Count; ++i)
        {
            NavMeshSurfaces[i].UpdateDataIfTransformChanged();
        }
    }

    private void AppendModifierVolumes(ref List<NavMeshBuildSource> sources)
    {
        // Modifiers
        List<NavMeshModifierVolume> modifiers = new(GetComponentsInChildren<NavMeshModifierVolume>());

        foreach (NavMeshModifierVolume modifier in modifiers)
        {
            if ((LayerMask & (1 << modifier.gameObject.layer)) == 0)
            {
                continue;
            }
            if (!modifier.AffectsAgentType(AgentTypeID))
            {
                continue;
            }

            Vector3 modifierCenter = modifier.transform.TransformPoint(modifier.center);
            Vector3 scale = modifier.transform.lossyScale;
            Vector3 modifierSize = new(modifier.size.x * Mathf.Abs(scale.x), modifier.size.y * Mathf.Abs(scale.y), modifier.size.z * Mathf.Abs(scale.z));

            NavMeshBuildSource source = new()
            {
                shape = NavMeshBuildSourceShape.ModifierBox,
                transform = Matrix4x4.TRS(modifierCenter, modifier.transform.rotation, Vector3.one),
                size = modifierSize,
                area = modifier.area
            };

            sources.Add(source);
        }
    }

    public List<NavMeshBuildSource> CollectSources()
    {
        List<NavMeshBuildSource> sources = new();
        List<NavMeshBuildMarkup> markups = new();

        List<NavMeshModifier> modifiers = new(GetComponentsInChildren<NavMeshModifier>());

        foreach (NavMeshModifier m in modifiers)
        {
            if ((LayerMask & (1 << m.gameObject.layer)) == 0)
            {
                continue;
            }
            if (!m.AffectsAgentType(AgentTypeID))
            {
                continue;
            }

            NavMeshBuildMarkup markup = new()
            {
                root = m.transform,
                overrideArea = m.overrideArea,
                area = m.area,
                ignoreFromBuild = m.ignoreFromBuild
            };

            markups.Add(markup);
        }

        NavMeshBuilder.CollectSources(_Transform, LayerMask, NavMeshCollectGeometry.RenderMeshes, DefaultArea, markups, sources);   

        AppendModifierVolumes(ref sources);

        return sources;
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
    {
        Vector3 absAxisX = Abs(mat.MultiplyVector(Vector3.right));
        Vector3 absAxisY = Abs(mat.MultiplyVector(Vector3.up));
        Vector3 absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
        Vector3 worldPosition = mat.MultiplyPoint(bounds.center);
        Vector3 worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
        return new Bounds(worldPosition, worldSize);
    }

    public Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
    {
        // Use the unscaled matrix for the NavMeshSurface
        Matrix4x4 worldToLocal = Matrix4x4.TRS(_Transform.position, _Transform.rotation, Vector3.one);
        worldToLocal = worldToLocal.inverse;

        Bounds result = new();
        foreach (NavMeshBuildSource src in sources)
        {
            switch (src.shape)
            {
                case NavMeshBuildSourceShape.Mesh:
                    {
                        Mesh m = src.sourceObject as Mesh;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                        break;
                    }
                case NavMeshBuildSourceShape.Terrain:
                    {
                        // Terrain pivot is lower/left corner - shift bounds accordingly
                        TerrainData t = src.sourceObject as TerrainData;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
                        break;
                    }
                case NavMeshBuildSourceShape.Box:
                case NavMeshBuildSourceShape.Sphere:
                case NavMeshBuildSourceShape.Capsule:
                case NavMeshBuildSourceShape.ModifierBox:
                    result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                    break;
            }
        }

        // Inflate the bounds a bit to avoid clipping co-planar sources
        result.Expand(0.1f);
        return result;
    }

    private bool HasTransformChanged()
    {
        if (LastPosition != _Transform.position || LastRotation != _Transform.rotation)
        {
            return true;
        }

        return false;
    }

    private void UpdateDataIfTransformChanged()
    {
        if (HasTransformChanged())
        {
            RemoveData();
            AddData();
        }
    }
}