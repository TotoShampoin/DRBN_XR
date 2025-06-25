using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class used to switch the speed of the simulation or pause it
/// </summary>
public class TimeControl : MonoBehaviour
{
    public Button pauseButton;
    public Button normalSpeedButton;
    public Button doubleSpeedButton;

    void Start()
    {
        pauseButton.onClick.AddListener(PauseTime);
        normalSpeedButton.onClick.AddListener(NormalSpeed);
        doubleSpeedButton.onClick.AddListener(DoubleSpeed);

        UpdateButtonStates();
    }

    void PauseTime()
    {
        Time.timeScale = 0f;
        UpdateButtonStates();
    }

    void NormalSpeed()
    {
        Time.timeScale = 1f;
        UpdateButtonStates();
    }

    void DoubleSpeed()
    {
        Time.timeScale = 2f;
        UpdateButtonStates();
    }

    public void UpdateButtonStates()
    {
        pauseButton.interactable = (Time.timeScale != 0f);
        normalSpeedButton.interactable = (Time.timeScale != 1f);
        doubleSpeedButton.interactable = (Time.timeScale != 2f);
    }
}
