using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Class to reset everything in the simulation exept the options
/// </summary>
public class SimulationReset : MonoBehaviour
{
    [Tooltip("Slider that needs to conserve its value")]
    public Slider slider;
    
    [Tooltip("Used for closing a panel if reset to executed its start methode")]
    public GameObject settings;
    private Interface_Size interfaceSize;   //conserved his value even if reset executed

    void Awake()
    {
        interfaceSize = slider.GetComponent<Interface_Size>();

        // if save state present, reload it
        if (PlayerPrefs.HasKey("SliderValue"))
        {
            float sliderValue = PlayerPrefs.GetFloat("SliderValue");
            interfaceSize.OnSliderReleased(sliderValue);
        }
        else
        {
            float sliderValue = 0.50f;
            interfaceSize.OnSliderReleased(sliderValue);
        }
    }

    void Start()
    {
        if (settings != null)
        {
            settings.SetActive(false);
        }
    }

    public void ResetSimulation()
    {
        // Save actual value
        float currentSliderValue = slider.value;
        PlayerPrefs.SetFloat("SliderValue", currentSliderValue);

        // Prevent scale update
        interfaceSize.shouldUpdateScale = false;

        // Reload the scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
