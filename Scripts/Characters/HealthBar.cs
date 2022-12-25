using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image Bar;

    public float DisableTime = 5;
    public float DisableTimer = 0;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        DisableTimer += Time.deltaTime;

        if(DisableTimer > DisableTime)
        {
            DisableTimer = 0;
            gameObject.SetActive(false);
        }
    }

    public void ChangeScale(float scale)
    {
        Bar.transform.localScale = new(scale, 1, 1);
        DisableTimer = 0;
        gameObject.SetActive(true);
    }
}
