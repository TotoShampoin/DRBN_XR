using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// SliderLogger class to manage sliders and their values.
/// </summary>

public class SliderLogger : MonoBehaviour
{
    [System.Serializable]
    public class SliderInfo
    {
        [Tooltip("Slider to manage.")]
        public Slider slider;

        [Tooltip("Text to display the value of the slider (on the slider itself).")]
        public TextMeshProUGUI valueText;

        [Tooltip("Text to display the value of the slider (on the panel).")]
        public TextMeshProUGUI valueTextPanel;

        [Tooltip("Minimum value for the slider.")]
        public float minCustomValue = 1f;

        [Tooltip("Maximum value for the slider.")]
        public float maxCustomValue = 100f;

        [Tooltip("Default value for the slider.")]
        public float defaultValue = 0.5f;

        [Tooltip("Button to show the slider.")]
        public Button showButton;

        [Tooltip("Line object to show when the slider is active.")]
        public GameObject lineObject;
    }

    [Header("Sliders to manage")]
    public List<SliderInfo> sliders = new List<SliderInfo>();

    [Header("Reset button")]
    public Button resetButton;

    void Start()
    {
        foreach (var sliderInfo in sliders)
        {
            if (sliderInfo.slider != null)
            {
                SliderInfo capturedInfo = sliderInfo;

                // Listener for the slider value change
                capturedInfo.slider.onValueChanged.AddListener((value) => OnSliderValueChanged(capturedInfo, value));
                OnSliderValueChanged(capturedInfo, capturedInfo.slider.value);

                // Listener for the show button
                if (capturedInfo.showButton != null)
                {
                    capturedInfo.showButton.onClick.AddListener(() => ShowOnlySlider(capturedInfo));
                }
                else
                {
                    Debug.LogWarning("No show button assigned for the slider: " + capturedInfo.slider.gameObject.name);
                }
            }
            else
            {
                Debug.LogError("Slider is not assigned in the inspector for: " + sliderInfo.slider.gameObject.name);
            }
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetSliders);
        }
        else
        {
            Debug.LogWarning("No reset button assigned in the inspector.");
        }
    }

    void OnSliderValueChanged(SliderInfo sliderInfo, float value)
    {
        // Map the slider value to the custom range

        float mapped = Mathf.Lerp(
            sliderInfo.minCustomValue,
            sliderInfo.maxCustomValue,
            (value - sliderInfo.slider.minValue) / (sliderInfo.slider.maxValue - sliderInfo.slider.minValue)
        );

        int intValue = Mathf.RoundToInt(mapped);

        if (sliderInfo.valueText != null)
            sliderInfo.valueText.text = intValue.ToString();

        if (sliderInfo.valueTextPanel != null)
            sliderInfo.valueTextPanel.text = intValue.ToString();
    }

    public void ResetSliders()
    {
        foreach (var sliderInfo in sliders)
        {
            if (sliderInfo.slider != null)
            {
                sliderInfo.slider.value = sliderInfo.defaultValue;
            }
        }
    }

    public void ShowOnlySlider(SliderInfo sliderToShow)
    {
        // Hide all sliders and their text except the one we want to show
        foreach (var sliderInfo in sliders)
        {
            bool isTarget = sliderInfo == sliderToShow;

            if (sliderInfo.slider != null)
                sliderInfo.slider.gameObject.SetActive(isTarget);

            if (sliderInfo.valueText != null)
                sliderInfo.valueText.gameObject.SetActive(isTarget);

            if (sliderInfo.lineObject != null)
                sliderInfo.lineObject.SetActive(isTarget);
        }
    }
}
