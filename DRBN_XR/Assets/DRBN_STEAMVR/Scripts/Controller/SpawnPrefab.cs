using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// SpawnPrefab class to manage the spawning of prefabs in the scene.
/// </summary>

public class SpawnPrefab : MonoBehaviour
{
    [Tooltip("Prefab to spawn when the button is pressed.")]
    private GameObject prefabToSpawn;

    private String prefabName;

    [Tooltip("Spawn point for the prefab.")]
    public Transform spawnPoint;

    private InputDevice targetDevice;

    [Tooltip("DeleteMenu component to manage the delete menu. (add an entry)")]
    public DeleteMenu deleteMenu;
    private bool previousButtonState = false;

    [Tooltip("DeleteScript component")]
    public MonoBehaviour deleteScript;

    void Start()
    {
        List<InputDevice> inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);

        if (inputDevices.Count > 0)
        {
            targetDevice = inputDevices[0];
        }
        else
        {
            Debug.LogWarning("No right controller found.");
        }
    }

    void Update()
    {

        bool isPressed = false;

        if (targetDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out isPressed))
        {
            if (isPressed && !previousButtonState)
            {
                Spawn();
            }
            previousButtonState = isPressed;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Spawn();
        }
    }

    void Spawn()
    {
        if (prefabToSpawn != null && spawnPoint != null)
        {
            // Spawn the prefab at the spawn point position (and rotation)
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

            ModifiedOutline outline = spawnedObject.GetComponent<ModifiedOutline>();
            // Add an entry in the delete menu
            // deleteButton will be invoked when we click the prefab spawn in the scene
            Button deleteButton = deleteMenu.AddEntry(prefabName, spawnedObject, outline);

            // We need to find the XRGrabInteractable component in the prefab and add a listener to it (to delete it)
            XRGrabInteractable[] interactables = spawnedObject.GetComponentsInChildren<XRGrabInteractable>(true);

            foreach (var interactable in interactables)
            {
                // Add a listener to each interactable
                interactable.selectEntered.AddListener((SelectEnterEventArgs args) =>
                {
                    // If the deleteScript is active (we are in the delete menu), and we click on the prefab, we delete it by simulating a click on the delete button of the entry
                    if(deleteScript != null && deleteScript.enabled)
                    {
                        Destroy(interactable.gameObject);
                        // Supprime cet objet depuis le menu
                        deleteButton.onClick.Invoke();
                    }
                });
            }
        }
        else
        {
            Debug.LogWarning("Prefab to spawn or spawn point is not set.");
        }
    }



    public void SetTargetObject(GameObject obj, String name)
    {
        prefabName = name;
        prefabToSpawn = obj;
    }
}