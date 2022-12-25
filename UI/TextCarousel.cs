using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextCarousel : MonoBehaviour
{
    public int CurrentPosition;
    public TextMeshProUGUI Display;
    private string[] Options;

    public void Scroll(bool isLeft)
    {
        if (Options != null)
        {
            if (Options.Length > 0)
            {
                CurrentPosition = Repeat(CurrentPosition + (isLeft ? -1 : 1), Options.Length - 1);
                Display.text = Options[CurrentPosition];
            }
        }
    }

    public void SetOptions(string[] values, int currentPos = 0)
    {
        Options = values;
        CurrentPosition = currentPos;
        Display.text = Options[currentPos];
    }

    private int Repeat(int t, int length)
    {
        if(t < 0)
        {
            return length;
        }
        else if (t > length)
        {
            return 0;
        }
        else
        {
            return t;
        }
    }
}