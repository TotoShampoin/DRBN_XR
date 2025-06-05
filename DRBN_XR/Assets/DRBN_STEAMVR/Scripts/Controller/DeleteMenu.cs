using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// DeleteMenu class to manage a menu of entries that can be added and removed dynamically.
/// It is used to list / delete / outline the prefab that we spawned in the scene.
/// </summary>
// La classe pour stocker une entrée
[System.Serializable]
//Each entry of the delete menu will be stored in this class
public class MenuEntry
{
    public GameObject entryObject;
    // The entry, here a prefab containing the UI

    public GameObject spawnedObject;
    // The spawn prefab in the scene

    public GameObject dashedLineObject;
    //The line that will be drawn between the entry and the spawned prefab

    public ModifiedOutline outline;
    // The outline of the entry prefab (to highlight it)

    public MenuEntry(GameObject entryObject, GameObject spawnedObject, GameObject dashedLineObject, ModifiedOutline outline)
    {
        this.entryObject = entryObject;
        this.spawnedObject = spawnedObject;
        this.dashedLineObject = dashedLineObject;
        this.outline = outline;
    }
}

public class DeleteMenu : MonoBehaviour
{
    [Tooltip("Prefab for the entry in the menu, must contain a TextMeshProUGUI and two buttons, the first one to delete the entry and the second one to highlight it")]
    public GameObject entryPrefab;

    [Tooltip("Container for the scroll view, needed to adjust the size of the content (for the scrollbar)")]
    public GameObject content;

    [Tooltip("Button to delete all entries, without listeners (wiil be added in the Start method)")]
    public Button deleteAllButton;

    // The list of entries in the menu
    private List<MenuEntry> entries = new List<MenuEntry>();

    private void Start()
    {
        // adding the listener to the delete all button
        if (deleteAllButton != null)
        {
            deleteAllButton.onClick.AddListener(DeleteAllEntries);
        }
        else
        {
            Debug.LogError("Le bouton 'Delete All' n'a pas été assigné !");
        }
    }

    public Button AddEntry(string labelText, GameObject spawnedObject, ModifiedOutline outline)
    {
        if (entryPrefab == null || content == null)
        {
            Debug.LogError("EntryPrefab or Content is not assigned in the inspector.");
            return null;
        }

        // Instantiate under the GameObject to which this script is attached
        GameObject newEntry = Instantiate(entryPrefab, transform);

        // Modify the text of the entry
        TextMeshProUGUI label = newEntry.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = labelText;
        }

        // Gestion des boutons
        Button[] buttons = newEntry.GetComponentsInChildren<Button>();

        Button deleteButton = null;

        if (buttons.Length >= 2)
        {
            deleteButton = buttons[0];
            deleteButton.onClick.AddListener(() => RemoveEntry(newEntry));

            Button highlightButton = buttons[1];
            highlightButton.onClick.AddListener(() => ToggleSelected(newEntry));
        }

        // Search for the DashedLine component in the prefab
        DashedLine dashedLineScript = newEntry.GetComponentInChildren<DashedLine>(true);
        GameObject startDashedLineObject = null;

        if (dashedLineScript != null)
        {
            startDashedLineObject = spawnedObject?.gameObject;

            if (startDashedLineObject != null)
            {
                // Find the last child of the spawned object -> it will be the start point of the dashed line
                Transform lastChild = startDashedLineObject.transform;
                while (lastChild.childCount > 0)
                {
                    lastChild = lastChild.GetChild(0);
                }

                dashedLineScript.startPoint = lastChild;
                dashedLineScript.targetObject = content;

                // We deactivate the dashed line at the beginning (will be activated with the highlight button)
                dashedLineScript.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("DashedLine script not found in the prefab!");
        }

        // Add to the list of entries
        entries.Add(new MenuEntry(newEntry, spawnedObject, dashedLineScript?.gameObject, outline));

        //return the button to delete the entry, will be used in the SpawnPrefab script. 
        //(If the spawned object will be clicked with the controller's trigger, the button will be clicked button.onClick.Invoke())
        return deleteButton;
    }

    // Remove the entry from the list and destroy the GameObject, used with the delete button
    public void RemoveEntry(GameObject entryObject)
    {
        // Search for the entry in the list
        MenuEntry toRemove = entries.Find(e => e.entryObject == entryObject);
        if (toRemove != null)
        {
            entries.Remove(toRemove);

            if (toRemove.spawnedObject != null)
            {
                Destroy(toRemove.spawnedObject);
            }

            if (toRemove.dashedLineObject != null)
            {
                Destroy(toRemove.dashedLineObject);
            }

            Destroy(toRemove.entryObject);
        }
    }

    // Used to highlight the entry when the highlight button is clicked (+ the dashed line)
    private void ToggleSelected(GameObject entryObject)
    {
        // Unselect all other entries, only one can be selected at a time
        foreach (var entry in entries)
        {
            if (entry.entryObject != entryObject)
            {
                // Search the child "Selected" in the entry prefab
                Transform selectedChild = entry.entryObject.transform.Find("Selected");
                if (selectedChild != null)
                {
                    // Deactivate the selected child (the outline)
                    selectedChild.gameObject.SetActive(false);

                    // Deactivate the dashed line
                    if (entry.dashedLineObject != null)
                    {
                        entry.dashedLineObject.SetActive(false);
                    }
                }
                if(entry.outline != null)
                {
                    entry.outline.enabled = false;
                }
            }
        }

        // Select the entry that was clicked
        Transform selected = entryObject.transform.Find("Selected");
        if (selected != null)
        {
            bool activeNow = !selected.gameObject.activeSelf;
            // Activate the selected child (the outline)
            selected.gameObject.SetActive(activeNow);

            // Activate the dashed line
            MenuEntry entry = entries.Find(e => e.entryObject == entryObject);
            if (entry != null && entry.dashedLineObject != null)
            {
                entry.dashedLineObject.SetActive(activeNow);
                entry.outline.enabled = activeNow;
            }
        }
    }

    // Suppress all the entries in the list
    public void DeleteAllEntries()
    {
        foreach (var entry in entries)
        {
            if (entry.spawnedObject != null)
            {
                Destroy(entry.spawnedObject);
            }

            if (entry.dashedLineObject != null)
            {
                Destroy(entry.dashedLineObject);
            }

            Destroy(entry.entryObject);
        }

        // Reinitialize the list
        entries.Clear();
    }

}
