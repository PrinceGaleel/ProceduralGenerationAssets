using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    [Header("Panels")]
    public GameObject HUD;
    public UIInventory Inventory;
    public GameObject Map;
    public GameObject PauseMenu;
    private bool IsOccupied;

    [Header("HUD")]
    public TextMeshProUGUI InteractInfo;
    public TextMeshProUGUI MessageInfo;
    public TextMeshProUGUI TimeText;
    
    [Header("FPS")]
    public TextMeshProUGUI FrameRateCounter;
    private float FPSTimer = 0;
    private readonly float FPSTime = 1;
    private int NumFrames = 0;


    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;

            if(!InteractInfo)
            {
                Debug.Log("UI Alert: InteractInfo not set");
                InteractInfo = transform.Find("InteractInfo").GetComponent<TextMeshProUGUI>();
            }

            if (!MessageInfo)
            {
                Debug.Log("UI Alert: InteractInfo not set");
                MessageInfo = transform.Find("MessageInfo").GetComponent<TextMeshProUGUI>();
            }

            InteractInfo.text = "";
            MessageInfo.text = "";
            ToggleInventory(false);
            ToggleMap(false);
            TogglePauseMenu(false);

            if(!MessageInfo.GetComponent<DisableTimer>())
            {
                MessageInfo.gameObject.AddComponent<DisableTimer>();
            }
        }
    }

    private void Update()
    {
        NumFrames++;
        FPSTimer += Time.deltaTime;
        if (FPSTimer > FPSTime)
        {
            FrameRateCounter.text = (NumFrames / FPSTime).ToString();
            NumFrames = 0;
            FPSTimer = 0;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (Inventory.gameObject.activeSelf)
            {
                ToggleInventory(false);
            }
            else if (!IsOccupied)
            {
                ToggleInventory(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Inventory.gameObject.activeSelf)
            {
                ToggleInventory(false);
            }
            else if (Map.activeSelf)
            {
                ToggleMap(false);
            }
            else
            {
                TogglePauseMenu(!PauseMenu.activeSelf);
            }
        }
    }

    public void SetMessageInfo(float activatedTime, string message)
    {
        MessageInfo.text = message;
        MessageInfo.GetComponent<DisableTimer>().SetTimer(activatedTime);
    }

    public void ToggleInventory(bool isEnabled)
    {
        IsOccupied = isEnabled;
        TogglePlayer(!isEnabled);
        HUD.SetActive(!isEnabled);
        Inventory.ToggleInventory(isEnabled);
    }

    public void TogglePauseMenu(bool isEnabled)
    {
        IsOccupied = isEnabled;
        TogglePlayer(!isEnabled);
        HUD.SetActive(!isEnabled);
        PauseMenu.SetActive(isEnabled);
    }

    public void ToggleMap(bool isEnabled)
    {
        IsOccupied = isEnabled;
        TogglePlayer(!isEnabled);
        HUD.SetActive(!isEnabled);
        Map.SetActive(isEnabled);
    }

    private void TogglePlayerMovement(bool enabled)
    {
        CameraController.Instance.enabled = enabled;
        PlayerController.Instance.enabled = enabled;
    }

    private void ToggleCursor(bool isEnabled)
    {
        Cursor.visible = isEnabled;

        if(isEnabled)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void TogglePlayer(bool isEnabled)
    {
        TogglePlayerMovement(isEnabled);
        ToggleCursor(!isEnabled);
    }

}