using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject HUD;
    public UIInventory Inventory;
    public MenuManager PauseMenu;
    private bool IsOccupied;

    [Header("HUD")]
    public MySlider HealthBar;
    public MySlider ManaBar;
    public MySlider StaminaBar;

    public RectTransform NorthRotator;

    public TextMeshProUGUI InteractInfo;
    public TextMeshProUGUI MessageInfo;
    public TextMeshProUGUI TimeText;

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

            if (!InteractInfo)
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

            if (!MessageInfo.GetComponent<DisableTimer>())
            {
                MessageInfo.gameObject.AddComponent<DisableTimer>();
            }
        }
    }

    private void Start()
    {
        ToggleInventory(false);
        TogglePauseMenu(false);
        //ToggleMap(false);        
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

        NorthRotator.rotation = Quaternion.Euler(0, 0, PlayerStats.PlayerTransform.eulerAngles.y);

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
            /*
            else if (Map.activeSelf)
            {
                ToggleMap(false);
            }
            */
            else
            {
                TogglePauseMenu(!PauseMenu.gameObject.activeSelf);
            }
        }
    }

    public static void UpdateHealthBar(float min, float max)
    {
        Instance.HealthBar.UpdateSlider(min, max);
    }

    public static void UpdateManaBar(float min, float max)
    {
        Instance.ManaBar.UpdateSlider(min, max);
    }

    public static void UpdateStaminaBar(float min, float max)
    {
        Instance.StaminaBar.UpdateSlider(min, max);
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
        PauseMenu.gameObject.SetActive(isEnabled);
    }

    /*
    public void ToggleMap(bool isEnabled)
    {
        IsOccupied = isEnabled;
        TogglePlayer(!isEnabled);
        HUD.SetActive(!isEnabled);
        Map.SetActive(isEnabled);
    }
    */

    private void TogglePlayerMovement(bool enabled)
    {
        CameraController.Instance.enabled = enabled;
        PlayerStats.Instance.CanMove = enabled;
    }

    private void ToggleCursor(bool isEnabled)
    {
        Cursor.visible = isEnabled;

        if (isEnabled)
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