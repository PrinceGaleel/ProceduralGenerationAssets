using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

public class MoveTreeComponents : EditorWindow
{
    [MenuItem("Window/Move Tree Components")]
    public static void OpenWindow()
    {
        EditorWindow window = GetWindow<MoveTreeComponents>();
        window.titleContent = new GUIContent("Tree Component Mover");
    }

    private GameObject Tree;

    private void OnGUI()
    {
        Tree = EditorGUILayout.ObjectField("Tree", Tree, typeof(GameObject), true) as GameObject;

        GUI.enabled = (Tree != null);
        if (GUILayout.Button("Update Trees"))
        {
            if (Tree.transform.childCount > 0)
            {
                GameObject toCopy = Tree.transform.GetChild(0).gameObject;

                CopyMeshFilter(toCopy, Tree);
                CopyMeshRenderer(toCopy, Tree);
                CopyNavMeshObstacle(toCopy, Tree);

                Tree.AddComponent<MeshCollider>();
            }
        }
    }

    private static void CopyMeshFilter(GameObject toCopy, GameObject copyTo)
    {
        if (toCopy.GetComponent<MeshFilter>())
        {
            copyTo.AddComponent<MeshFilter>();
            copyTo.GetComponent<MeshFilter>().sharedMesh = toCopy.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    private static void CopyMeshRenderer(GameObject toCopy, GameObject copyTo)
    {
        if (toCopy.GetComponent<MeshRenderer>())
        {
            copyTo.AddComponent<MeshRenderer>();
            copyTo.GetComponent<MeshRenderer>().sharedMaterials = toCopy.GetComponent<MeshRenderer>().sharedMaterials;
        }
    }

    private static void CopyNavMeshObstacle(GameObject toCopy, GameObject copyTo)
    {
        if (toCopy.GetComponent<NavMeshObstacle>())
        {
            copyTo.AddComponent<NavMeshObstacle>();
            copyTo.GetComponent<NavMeshObstacle>().center = toCopy.GetComponent<NavMeshObstacle>().center;
            copyTo.GetComponent<NavMeshObstacle>().size = toCopy.GetComponent<NavMeshObstacle>().size;
        }
    }
}