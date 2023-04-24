using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject HUD;
    [SerializeField] private UIInventory Inventory;
    [SerializeField] private MenuManager PauseMenu;
    [SerializeField] private bool IsOccupied;

    [Header("HUD")]
    [SerializeField] private MySlider HealthBar;
    [SerializeField] private MySlider ManaBar;
    [SerializeField] private MySlider StaminaBar;

    [SerializeField] private RectTransform MiniMap;
    [SerializeField] private RectTransform NorthRotator;

    public TextMeshProUGUI InteractInfo;
    [SerializeField] private TextMeshProUGUI MessageInfo;
    public TextMeshProUGUI TimeText;

    [SerializeField] private TextMeshProUGUI FrameRateCounter;
    [SerializeField] private float FPSTimer = 0;
    private readonly float FPSTime = 1;
    [SerializeField] private int NumFrames = 0;

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

        Vector3 dir = Vector3.Normalize(Vector3.zero - PlayerStats.PlayerTransform.position);
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        NorthRotator.localRotation = Quaternion.Euler(0f, 0f, -angle);
        MiniMap.rotation = Quaternion.Euler(0, 0, PlayerStats.PlayerTransform.eulerAngles.y);
        //NorthRotator.rotation = Quaternion.LookRotation(new(-PlayerStats.PlayerTransform.position.x, 0, -PlayerStats.PlayerTransform.position.z), new(1, 0, 0));

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

    public static void SetMessageInfo(string message, float activatedTime = 5)
    {
        Instance.MessageInfo.text = message;
        Instance.MessageInfo.GetComponent<DisableTimer>().SetTimer(activatedTime);
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
        PlayerStats.CanMove = enabled;
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