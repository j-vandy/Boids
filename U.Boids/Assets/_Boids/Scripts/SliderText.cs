using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    private TMP_Text text;

    void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    public void UpdateValue()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (slider.wholeNumbers)
            text.text = string.Format("{0}", slider.value);
        else
            text.text = string.Format("{0:F1}", slider.value);
    }
}
