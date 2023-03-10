using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MySlider : MonoBehaviour
{
    [SerializeField] private Image SliderImage;
    [SerializeField] private TextMeshProUGUI PercentageText;

    private float Max;
    private float TimeElapsed, StartValue, TargetValue;
    [SerializeField] private float LerpSpeed = 2;

    private void Update()
    {
        TimeElapsed += Time.deltaTime * LerpSpeed;
        float actualValue = Mathf.Lerp(StartValue, TargetValue, TimeElapsed);

        SliderImage.fillAmount = actualValue;
        PercentageText.text = Mathf.RoundToInt(actualValue * Max) + "/" + Max;

        if (actualValue == TargetValue) enabled = false;
    }

    public void UpdateSlider(float min, float max) 
    {
        TimeElapsed = 0;
        StartValue = SliderImage.fillAmount;
        Max = max;
        TargetValue = min / max;
        enabled = true;
    }
}
