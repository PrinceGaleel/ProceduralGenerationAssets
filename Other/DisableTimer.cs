using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableTimer : MonoBehaviour
{
    public float Timer;
    public float DisableTime;

    private void Awake()
    {
        DisableTime = 0;
        Timer = 0;
    }

    private void Update()
    {
        if(Timer < DisableTime)
        {
            Timer += Time.deltaTime;
        }
        else
        {
            gameObject.SetActive(false);
            Timer = 0;
        }
    }

    public void SetTimer(float disableTime)
    {
        DisableTime = disableTime;
        Timer = 0;
        gameObject.SetActive(true);
    }
}
