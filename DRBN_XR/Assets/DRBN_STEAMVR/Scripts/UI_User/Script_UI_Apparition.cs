using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Class for spawning UI User whith the Y button of the controller
/// </summary>
public class Script_UI_Apparition : MonoBehaviour
{
    [Tooltip("Canvas containing all the panels")]
    public GameObject canvasToToggle;

    [Tooltip("Every panels to hide")]
    public List<GameObject> gameObjectsToHide;

    [Tooltip("Every panels to show")]
    public List<GameObject> gameObjectToShow;

    private InputDevice targetDevice;
    private bool previousButtonState = false;

    void Start()
    {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, inputDevices);
        targetDevice = inputDevices[0];
    }

    void Update()
    {
        bool isPressed = false;
        if (targetDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out isPressed)) // Touche Y
        {
            if (isPressed && !previousButtonState)
            {
                ToggleCanvas();
                ToggleGameObjects();
            }
            previousButtonState = isPressed;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCanvas();
            ToggleGameObjects();
        }
    }

    void ToggleCanvas()
    {
        if (canvasToToggle != null)
        {
            canvasToToggle.SetActive(!canvasToToggle.activeSelf);
        }
    }

    void ToggleGameObjects()
    {
        if (canvasToToggle != null && canvasToToggle.activeSelf)
        {
            // if not active, activate
            foreach (var obj in gameObjectToShow)
            {
                if (obj != null && !obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }
        }
        // Hide all the gameobjects who need it
        if(canvasToToggle != null && !canvasToToggle.activeSelf)
        {
            foreach (var obj in gameObjectsToHide)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}
