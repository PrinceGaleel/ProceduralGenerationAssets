using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItem : Interactable
{
    [SerializeField] protected GameObject ParentGameObject;
    public override string GetInteractInfo { get { return "Press F to pick up\n" + _Item.ItemName + " (" + Amount + ")"; } }

    [SerializeField] protected Item _Item;
    [SerializeField] protected int Amount;

    public override string PlayerInteract()
    {
        PlayerStats.Instance.MyInventory.AddItem(_Item, Amount);
        Destroy(ParentGameObject);
        gameObject.SetActive(false);
        return "";
    }

    public void Initialize(Item item, int amount, bool hasRigidbody)
    {
        _Item = item;
        Amount = amount;

        /*
        if (!_Outline)
        {
            if (!GetComponent<Outline>())
            {
                _Outline = gameObject.AddComponent<Outline>();
            }
            else
            {
                _Outline = gameObject.GetComponent<Outline>();
            }
        }

        if (_Outline)
        {
            _Outline.enabled = false;
            _Outline.OutlineWidth = 8;
            _Outline.OutlineMode = Outline.Mode.OutlineVisible;
            _Outline.enabled = false;
        }
        */

        if (GetComponent<MeshCollider>())
        {
            Destroy(GetComponent<MeshCollider>());
        }

        GameObject rigidbodyParent = new ();
        rigidbodyParent.transform.SetParent(transform);
        rigidbodyParent.transform.localPosition = Vector3.zero;
        rigidbodyParent.transform.localScale = new(1, 1, 1);
        MeshCollider meshCollider = rigidbodyParent.AddComponent<MeshCollider>();

        if (GetComponent<MeshFilter>())
        {
            meshCollider.sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        }
        
        meshCollider.convex = true;

        if (hasRigidbody)
        {
            if (!rigidbodyParent.GetComponent<Rigidbody>())
            {
                rigidbodyParent.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else
            {
                rigidbodyParent.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
        else if (GetComponent<Rigidbody>())
        {
            Destroy(GetComponent<Rigidbody>());
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!ParentGameObject) ParentGameObject = gameObject;
    }
#endif
}