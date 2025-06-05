using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Class to reset the different settings present in the UI User.
/// Does not reset the simulation settings
/// </summary>
public class Script_Reset_Option : MonoBehaviour
{
    [Tooltip("Slider for changing scale of the UI")]
    public Slider UI_size;

    [Tooltip("Script of InterfaceSize")]
    public Interface_Size interfaceSize;

    [Tooltip("Reset the time control")]
    public TimeControl timeControl;

    public void ResetOption()
    {
        float defaultSliderValue = 0.5f;
        UI_size.value = defaultSliderValue;

        // Save the current slider value
        PlayerPrefs.SetFloat("SliderValue", defaultSliderValue);

        // Reset the time scale to normal
        Time.timeScale = 1f;

        interfaceSize.OnSliderReleased(UI_size.value);

        if (timeControl != null)
        {
            timeControl.UpdateButtonStates();
        }
    }
}
