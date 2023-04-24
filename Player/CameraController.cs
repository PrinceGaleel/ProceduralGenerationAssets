using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public Camera MainCam;
    [SerializeField] private Transform CamParent;
    [SerializeField] private CameraState CurrentState;

    [SerializeField] private float XCamRotation;
    [SerializeField] private float TargetCamDist;
    [SerializeField] private float ActualCamDist;
    public float MouseSensitivity;

    [SerializeField] private float InteractDistance = 5f;

    [Header("Camera Settigns")]
    [SerializeField] private float MaxCamDist = 4f;
    [SerializeField] private float MinCamDist = 2f;
    [SerializeField] private float MaxYAngle = 30;
    [SerializeField] private float MinYAngle = -55;
    private const float CollisionOffset = 0.4f;
    private const float MinCollisionDist = 0.2f;

    public List<Transform> FirstPersonDisable;
    public List<Transform> FirstPersonArmsLayer;

    [SerializeField] private Outline CurrentOutline;

    private enum CameraState
    {
        First_Person,
        Third_Person
    }

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple Camera Controller instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            if (!MainCam) enabled = false;
        }
    }

    private void Start()
    {
        GoFirstPerson();
        TargetCamDist = 0;
        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        CamParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        ActualCamDist = TargetCamDist;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        ResetText();

        if (Physics.Raycast(MainCam.transform.position, MainCam.transform.forward, out RaycastHit hit, InteractDistance))
        {
            if (hit.transform.TryGetComponent(out Interactable interactable))
            {
                UIController.Instance.InteractInfo.text = interactable.GetInteractInfo;

                if (Input.GetKeyDown(KeyCode.F))
                {
                    string message = interactable.PlayerInteract();
                    if (message != "") UIController.SetMessageInfo(message);
                }
            }
        }

        transform.eulerAngles = new Vector3(0, Mathf.Repeat(transform.eulerAngles.y + Input.GetAxis("Mouse X") * MouseSensitivity, 360), 0);
        XCamRotation = Mathf.Clamp(XCamRotation - (Input.GetAxis("Mouse Y") * MouseSensitivity), MinYAngle, MaxYAngle);
        CamParent.rotation = Quaternion.Euler(new(XCamRotation, transform.eulerAngles.y, 0));

        if (CurrentState == CameraState.First_Person)
        {
            //if (Input.mouseScrollDelta.y < 0) GoThirdPerson();
        }
        else if (CurrentState == CameraState.Third_Person)
        {
            CamParent.transform.localEulerAngles = new Vector3(XCamRotation, 0, 0);
            MainCam.transform.localPosition = Vector3.Lerp(MainCam.transform.localPosition, new Vector3(0, 0, -ActualCamDist), Time.deltaTime * 7f);

            if (Physics.SphereCast(CamParent.position, CollisionOffset, (MainCam.transform.position - CamParent.position).normalized, out RaycastHit sphereHit, Vector3.Distance(MainCam.transform.position, CamParent.position), ~LayerMask.GetMask("Weapon", "Harvestable", "Controller", "Hitbox", "Resource")))
            {
                ChangeCurrentCamDist(sphereHit, Vector3.zero);
            }
            else ActualCamDist = TargetCamDist; 

            if (Input.mouseScrollDelta.y != 0)
            {
                TargetCamDist = Mathf.Clamp(TargetCamDist - Input.mouseScrollDelta.y, MinCamDist, MaxCamDist);

                if (TargetCamDist - Input.mouseScrollDelta.y < MinCamDist) GoFirstPerson(); 
            }
        }
    }

    private void GoFirstPerson()
    {
        CurrentState = CameraState.First_Person;
        ChangeHeadAttachState(false);

        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        CamParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        TargetCamDist = 0;
        ActualCamDist = 0;
        MainCam.transform.localPosition = new(0, 0, 0);
    }

    private void GoThirdPerson()
    {
        CurrentState = CameraState.Third_Person;
        ChangeHeadAttachState(true);

        MainCam.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CamParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        TargetCamDist = MinCamDist;
        ActualCamDist = MinCamDist;
    }

    public void ChangeHeadAttachState(bool isEnabled)
    {
        string layerName;
        if (isEnabled)
        {
            layerName = "Default";
        }
        else
        {
            layerName = "Arms";
        }

        for (int i = 0; i < FirstPersonArmsLayer.Count; i++)
        {
            FirstPersonArmsLayer[i].gameObject.layer = LayerMask.NameToLayer(layerName);
        }

        for (int i = 0; i < FirstPersonDisable.Count; i++)
        {
            FirstPersonDisable[i].gameObject.SetActive(isEnabled);
        }
    }

    private void ResetText()
    {
        if (UIController.Instance)
        {
            if (UIController.Instance.InteractInfo)
            {
                if (CurrentOutline)
                {
                    CurrentOutline.enabled = false;
                    CurrentOutline = null;
                }

                UIController.Instance.InteractInfo.text = "";
            }
        }
    }

    private void ChangeCurrentCamDist(RaycastHit hit, Vector3 offset)
    {
        if (!hit.transform.GetComponent<CharacterController>())
        {
            if (Vector3.Distance(CamParent.transform.InverseTransformPoint(CamParent.position), CamParent.transform.InverseTransformPoint(hit.point)) > MinCollisionDist)
            {
                if (Vector3.Distance(CamParent.transform.InverseTransformPoint(CamParent.position), CamParent.transform.InverseTransformPoint(hit.point)) < TargetCamDist)
                {
                    ActualCamDist = Vector3.Distance(CamParent.transform.InverseTransformPoint(CamParent.position), CamParent.transform.InverseTransformPoint(hit.point + offset));
                }
            }
        }
    }
}