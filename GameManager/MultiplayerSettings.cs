using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerSettings : MonoBehaviour
{
    public static MultiplayerSettings Instance;

    public static bool ConnectedToMaster;
    public static bool IsMultiplayer;

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
            DontDestroyOnLoad(gameObject);
        }
    }
}