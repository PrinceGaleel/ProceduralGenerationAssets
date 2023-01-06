using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public Camera MainCam;
    public Transform ThirdPersonParent;
    public Transform FirstPersonParent;

    private CameraState CurrentState;

    private float XCamRotation;
    private float CurrentCamDist;
    private float ActualCamDist;
    public float MouseSensitivity;

    public Transform SpineRotator;
    public float InteractDistance = 5f;

    [Header("Camera Settigns")]
    public float MaxCamDist = 4f;
    public float MinCamDist = 2f;
    public float MaxYAngle = 30;
    public float MinYAngle = -55;
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
                MainCam = transform.Find("Main Camera").GetComponent<Camera>();

                if (!MainCam)
                {
                    enabled = false;
                }
            }
        }
    }

    private void Start()
    {
        GoFirstPerson();
        CurrentCamDist = 0;
        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        ThirdPersonParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        ActualCamDist = CurrentCamDist;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        ResetText();

        if (Physics.Raycast(ThirdPersonParent.position, ThirdPersonParent.forward, out RaycastHit hit, InteractDistance, ~LayerMask.GetMask("Controller")))
        {
            if (hit.transform.GetComponent<DropItem>())
            {
                DropItem item = hit.transform.GetComponent<DropItem>();

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
            else 
            {
                Harvestable harvestable = hit.transform.GetComponent<Harvestable>();

                if (harvestable)
                {
                    UIController.Instance.InteractInfo.text = "Press F to pick up";

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        harvestable.Harvest(PlayerStats.Instance._Inventory);
                    }
                }
            }
        }

        transform.eulerAngles = new Vector3(0, Mathf.Repeat(transform.eulerAngles.y + Input.GetAxis("Mouse X") * MouseSensitivity, 360), 0);
        XCamRotation = Mathf.Clamp(XCamRotation - (Input.GetAxis("Mouse Y") * MouseSensitivity), MinYAngle, MaxYAngle);
        SpineRotator.eulerAngles = StartingSpineRotation + new Vector3(0, transform.eulerAngles.y, XCamRotation);

        if (CurrentState == CameraState.First_Person)
        {
            //SpineRotator.localEulerAngles = new Vector3(SpineRotator.localEulerAngles.x, SpineRotator.localEulerAngles.y, SpineRotator.localEulerAngles.z);

            if (Input.mouseScrollDelta.y < 0)
            {
                GoThirdPerson();
            }
        }
        else if (CurrentState == CameraState.Third_Person)
        {
            ThirdPersonParent.transform.localEulerAngles = new Vector3(XCamRotation, 0, 0);
            MainCam.transform.localPosition = Vector3.Lerp(MainCam.transform.localPosition, new Vector3(0, 0, -ActualCamDist), Time.deltaTime * 7f);

            if (Physics.SphereCast(ThirdPersonParent.position, CollisionOffset, (MainCam.transform.position - ThirdPersonParent.position).normalized, out RaycastHit sphereHit, Vector3.Distance(MainCam.transform.position, ThirdPersonParent.position), ~LayerMask.GetMask("Border", "Ragdoll", "Player", "Attacker", "TriggerInteractable")))
            {
                ChangeCurrentCamDist(sphereHit, Vector3.zero);
            }
            else
            {
                ActualCamDist = CurrentCamDist;
            }

            if (Input.mouseScrollDelta.y != 0)
            {
                CurrentCamDist = Mathf.Clamp(CurrentCamDist - Input.mouseScrollDelta.y, MinCamDist, MaxCamDist);

                if (CurrentCamDist - Input.mouseScrollDelta.y < MinCamDist)
                {
                    GoFirstPerson();
                }
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
        CurrentCamDist = 0;
        ActualCamDist = 0;
        MainCam.transform.localPosition = new(0, 0, 0);
    }

    private void GoThirdPerson()
    {
        MainCam.transform.SetParent(ThirdPersonParent);
        CurrentState = CameraState.Third_Person;
        ChangeHeadAttachState(true);

        MainCam.transform.SetLocalPositionAndRotation(new Vector3(0, 0, MinCamDist), Quaternion.Euler(0, 0, 0));
        ThirdPersonParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CurrentCamDist = MinCamDist;
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
                if (Vector3.Distance(ThirdPersonParent.transform.InverseTransformPoint(ThirdPersonParent.position), ThirdPersonParent.transform.InverseTransformPoint(hit.point)) < CurrentCamDist)
                {
                    ActualCamDist = Vector3.Distance(ThirdPersonParent.transform.InverseTransformPoint(ThirdPersonParent.position), ThirdPersonParent.transform.InverseTransformPoint(hit.point + offset));
                }
            }
        }
    }
}