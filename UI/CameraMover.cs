using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform Cam;
    private Vector3 StartPosition;
    private Vector3 EndPosition;
    private Quaternion StartRotation;
    private Quaternion EndRotation;
    private float TimeCounter;
    public float Speed;

    private void Awake()
    {
        if(!Cam)
        {
            Debug.Log("Error: NO CAMERA ON MOVE CAMERA: " + gameObject.name);
            Destroy(this);
        }

        enabled = false;
    }

    private void Update()
    {
        Cam.transform.SetPositionAndRotation(Vector3.Slerp(StartPosition, EndPosition, (TimeCounter += Time.deltaTime)), Quaternion.Slerp(StartRotation, EndRotation, TimeCounter));

        if(TimeCounter >= 1)
        {
            enabled = false;
        }
    }

    public void SetTarget(Transform target)
    {
        TimeCounter = 0;
        StartRotation = Cam.transform.rotation;
        EndRotation = target.rotation;
        StartPosition = Cam.transform.position;
        EndPosition = target.position;
        enabled = true;
    }
}
