using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    public Camera MainCam;
    [SerializeField] private Transform ThirdPersonParent;
    [SerializeField] private Transform FirstPersonParent;

    private CameraState CurrentState;

    private float XCamRotation;
    private float TargetCamDist;
    private float ActualCamDist;
    public float MouseSensitivity;

    [SerializeField] private Transform SpineRotator;
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

    private Outline CurrentOutline;
    private Vector3 StartingSpineRotation;

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
            StartingSpineRotation = SpineRotator.eulerAngles;
            if (!MainCam)
            {
                enabled = false;
            }
        }
    }

    private void Start()
    {
        GoFirstPerson();
        TargetCamDist = 0;
        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        ThirdPersonParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        ActualCamDist = TargetCamDist;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        ResetText();

        if (Physics.Raycast(ThirdPersonParent.position, ThirdPersonParent.forward, out RaycastHit hit, InteractDistance, ~LayerMask.GetMask("Controller")))
        {
            if (hit.transform.TryGetComponent(out DropItem item))
            {
                CurrentOutline = item._Outline;
                CurrentOutline.enabled = true;

                UIController.Instance.InteractInfo.text = "Press F to pick up";

                if (Input.GetKeyDown(KeyCode.F))
                {
                    PlayerStats.Instance._Inventory.AddItem(item._Item, item.Amount);
                    Destroy(hit.transform.gameObject);
                    hit.transform.gameObject.SetActive(false);
                }
            }
        }

        transform.eulerAngles = new Vector3(0, Mathf.Repeat(transform.eulerAngles.y + Input.GetAxis("Mouse X") * MouseSensitivity, 360), 0);
        XCamRotation = Mathf.Clamp(XCamRotation - (Input.GetAxis("Mouse Y") * MouseSensitivity), MinYAngle, MaxYAngle);
        SpineRotator.eulerAngles = StartingSpineRotation + new Vector3(0, transform.eulerAngles.y, XCamRotation);

        if (CurrentState == CameraState.First_Person)
        {
            if (Input.mouseScrollDelta.y < 0) GoThirdPerson();
        }
        else if (CurrentState == CameraState.Third_Person)
        {
            ThirdPersonParent.transform.localEulerAngles = new Vector3(XCamRotation, 0, 0);
            MainCam.transform.localPosition = Vector3.Lerp(MainCam.transform.localPosition, new Vector3(0, 0, -ActualCamDist), Time.deltaTime * 7f);

            if (Physics.SphereCast(ThirdPersonParent.position, CollisionOffset, (MainCam.transform.position - ThirdPersonParent.position).normalized, out RaycastHit sphereHit, Vector3.Distance(MainCam.transform.position, ThirdPersonParent.position), ~LayerMask.GetMask("Weapon", "Harvestable", "Controller", "Hitbox", "Resource")))
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
        MainCam.transform.SetParent(FirstPersonParent);
        CurrentState = CameraState.First_Person;
        ChangeHeadAttachState(false);

        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        ThirdPersonParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        TargetCamDist = 0;
        ActualCamDist = 0;
        MainCam.transform.localPosition = new(0, 0, 0);
    }

    private void GoThirdPerson()
    {
        MainCam.transform.SetParent(ThirdPersonParent);
        CurrentState = CameraState.Third_Person;
        ChangeHeadAttachState(true);

        MainCam.transform.localRotation = Quaternion.Euler(0, 0, 0);
        ThirdPersonParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
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
            if (Vector3.Distance(ThirdPersonParent.transform.InverseTransformPoint(ThirdPersonParent.position), ThirdPersonParent.transform.InverseTransformPoint(hit.point)) > MinCollisionDist)
            {
                if (Vector3.Distance(ThirdPersonParent.transform.InverseTransformPoint(ThirdPersonParent.position), ThirdPersonParent.transform.InverseTransformPoint(hit.point)) < TargetCamDist)
                {
                    ActualCamDist = Vector3.Distance(ThirdPersonParent.transform.InverseTransformPoint(ThirdPersonParent.position), ThirdPersonParent.transform.InverseTransformPoint(hit.point + offset));
                }
            }
        }
    }
}