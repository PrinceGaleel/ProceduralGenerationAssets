using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    public Transform Target;

    private void Awake()
    {
        if (!Target)
        {
            if (CameraController.Instance)
            {
                Target = CameraController.Instance.MainCam.transform;
            }
        }

        if (!Target)
        {
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(Target.position - transform.position, Vector3.up);
    }
}
