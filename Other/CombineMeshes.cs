using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CombineMeshes : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private bool GetMeshFilters = false;
    [SerializeField] private bool PrepareMeshes = false;
    [SerializeField] private bool TryCombine = false;

    [Header("Issues")]
    [SerializeField] private List<Transform> EmptyMeshRenderer;
    [SerializeField] private List<Transform> EmptyMeshFilters;

    [Header("Get Mesh Filters")]
    [SerializeField] private Transform MeshFiltersParent;
    [SerializeField] private bool IncludeDisabledFilters = true;
    [SerializeField] private List<CustomTuple<Material, List<MeshFilter>>> Meshes;
    [SerializeField] private List<CustomTuple<Material, int>> TotalNumVerts;

    [Header("Prepare Meshes")]
    [SerializeField] private Transform MeshFilterParent;
    [SerializeField] private List<MeshesToCombine> ToCombine;

    [SerializeField] private int MaxVerts = 65000;

    private void OnValidate()
    {
        if (GetMeshFilters)
        {
            GetMeshFilters = false;
            MeshFilter[] filters = MeshFiltersParent.GetComponentsInChildren<MeshFilter>(IncludeDisabledFilters);

            EmptyMeshFilters = new();
            EmptyMeshRenderer = new();
            Dictionary<Material, List<MeshFilter>> meshesDict = new();
            Meshes = new();

            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh)
                {
                    if (filter.GetComponent<MeshRenderer>().sharedMaterial)
                    {
                        if (!meshesDict.ContainsKey(filter.GetComponent<MeshRenderer>().sharedMaterial)) { meshesDict.Add(filter.GetComponent<MeshRenderer>().sharedMaterial, new() { filter }); }
                        else { meshesDict[filter.GetComponent<MeshRenderer>().sharedMaterial].Add(filter); }
                    }
                    else
                    {
                        EmptyMeshRenderer.Add(filter.transform);
                    }
                }
                else
                {
                    EmptyMeshFilters.Add(filter.transform);
                }
            }

            foreach (Material key in meshesDict.Keys)
            {
                Meshes.Add(new(key, meshesDict[key]));
            }
        }

        if (PrepareMeshes)
        {
            PrepareMeshes = false;
            TotalNumVerts = new();
            ToCombine = new();

            for (int i = 0; i < Meshes.Count; i++)
            {
                if (Meshes[i].Item2.Count > 1)
                {
                    int numVerts = 0;

                    for (int j = 0; j < Meshes[i].Item2.Count; j++)
                    {
                        numVerts += Meshes[i].Item2[j].sharedMesh.vertexCount;
                    }

                    TotalNumVerts.Add(new(Meshes[i].Item1, numVerts));
                    ToCombine.Add(new(Meshes[i].Item1));

                    List<MeshFilter> meshes = new(Meshes[i].Item2);
                    while (meshes.Count > 0)
                    {
                        if (ToCombine[^1].NumVerts + meshes[0].sharedMesh.vertexCount < MaxVerts)
                        {
                            ToCombine[^1].AddMesh(meshes[0]);
                            meshes.RemoveAt(0);
                        }
                        else
                        {
                            ToCombine.Add(new(Meshes[i].Item1));
                        }
                    }
                }
            }
        }

        if (TryCombine)
        {
            TryCombine = false;
            if (MeshFilterParent.childCount == ToCombine.Count)
            {
                MeshFilter[] filters = MeshFilterParent.GetComponentsInChildren<MeshFilter>();
                MeshRenderer[] renderers = new MeshRenderer[filters.Length];

                for (int i = 0; i < filters.Length; i++)
                {
                    if (filters[i].GetComponent<MeshRenderer>())
                    {
                        renderers[i] = filters[i].GetComponent<MeshRenderer>();
                    }
                    else return;
                }

                for (int i = 0; i < ToCombine.Count; i++)
                {
                    filters[i].transform.name = ToCombine[i].Mat.name + " " + i;
                    ToCombine[i].Parent = filters[i];
                    ToCombine[i].Renderer = renderers[i];
                    ToCombine[i].Combine(i);
                }
            }
        }
    }

    [Serializable]
    private class MeshesToCombine
    {
        public MeshFilter Parent;
        public MeshRenderer Renderer;
        public List<MeshFilter> ChildMeshes;
        public Material Mat;
        public int NumVerts { get; private set; }

        public MeshesToCombine(Material mat)
        {
            Mat = mat;
            NumVerts = 0;
            ChildMeshes = new();
        }

        public void AddMesh(MeshFilter filter)
        {
            NumVerts += filter.sharedMesh.vertexCount;
            ChildMeshes.Add(filter);
        }

        public void Combine(int integer)
        {
            Renderer.material = Mat;

            CombineInstance[] instances = new CombineInstance[ChildMeshes.Count];
            for (int i = 0; i < instances.Length; i++)
            {
                instances[i] = new()
                {
                    mesh = ChildMeshes[i].sharedMesh,
                    transform = ChildMeshes[i].transform.localToWorldMatrix
                };

                ChildMeshes[i].gameObject.SetActive(false);
            }

            if (!Parent.sharedMesh) Parent.sharedMesh = new();
            Parent.sharedMesh.CombineMeshes(instances);
            AssetDatabase.CreateAsset(Parent.sharedMesh, FileUtil.GetProjectRelativePath(EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", Mat.name + " " + integer, "asset")));
            if (Parent.GetComponent<MeshCollider>()) Parent.GetComponent<MeshCollider>().sharedMesh = Parent.sharedMesh;
        }
    }
#endif
}