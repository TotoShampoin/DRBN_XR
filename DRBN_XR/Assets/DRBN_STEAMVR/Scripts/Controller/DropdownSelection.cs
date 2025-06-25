using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DropdownSelection class to manage the selection of a prefab from a dropdown menu.
/// This class is responsible for updating the selected prefab and its associated image in the UI.
/// </summary>

public class DropdownSelection : MonoBehaviour
{
    [Tooltip("The TMP_Dropdown component to manage.")]
    public TMP_Dropdown dropdown;

    [Tooltip("The ImageLoader component to load prefabs.")]
    public ImageLoader imageLoader;

    [Tooltip("The SpawnPrefab component to spawn prefabs.")]
    public SpawnPrefab spawnPrefab;

    [Tooltip("The GameObject to display when at least une prefab is in the favorites. (must be a child of the dropdown)")]
    public GameObject favoriteElement;

    [Tooltip("Additional GameObject to display when at least une prefab is in the favorites. (can be null)")]
    public GameObject favoriteElement2;

    [Tooltip("The GameObject to display when no prefab is in the favorites.")]
    public GameObject messageNoFavorite;

    [Tooltip("The GameObject to display when no prefab is in the favorites.")]
    public GameObject ArrowImageNoFavorite;

    [Tooltip("The Image component to display the selected prefab's image.")]
    public Image targetImageController;

    [Tooltip("The Image component background.")]
    public Image targetImageControllerBackground;

    void Start()
    {
        if (dropdown == null)
        {
            Debug.LogError("DropdownSelection: Dropdown reference is not set.");
            return;
        }

        if (imageLoader == null)
        {
            Debug.LogError("DropdownSelection: ImageLoader reference is not set.");
            return;
        }

        if (spawnPrefab == null)
        {
            Debug.LogError("DropdownSelection: SpawnPrefab reference is not set.");
            return;
        }

        // Add a listener to the dropdown value change event
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // Initialize with first element
        OnDropdownValueChanged(dropdown.value);
    }

    public void OnDropdownValueChanged(int index)
    {
        ForceUpdateSpawnPrefab();
    }

    public void ForceUpdateSpawnPrefab()
    {
        UpdateSpawnPrefab();
    }

    private void UpdateSpawnPrefab()
    {
        if (dropdown.options.Count == 0)
        {
            // The dropdown is empty, we display the message to add favorites
            spawnPrefab.SetTargetObject(null,null);
            favoriteElement.gameObject.SetActive(false);
            favoriteElement2.gameObject.SetActive(false);
            messageNoFavorite.SetActive(true);
            ArrowImageNoFavorite.SetActive(true);
            targetImageController.enabled = false;
            targetImageControllerBackground.enabled = false;

            return;
        }
        else
        {
            // The dropdown is not empty, we hide the message to add favorites
            favoriteElement.gameObject.SetActive(true);
            favoriteElement2.gameObject.SetActive(true);
            messageNoFavorite.SetActive(false);
            ArrowImageNoFavorite.SetActive(false);
            targetImageController.enabled = true;
            targetImageControllerBackground.enabled = true;
        }

        int index = dropdown.value;


        if (index == dropdown.options.Count)
        {
            // Strange but needed. Otherwise there is a bug of synchronization between the dropdown and the spawn prefab under certain cyrcumstances.
            index--;
        }
        if (index >= 0 && index < dropdown.options.Count)
        {
            TMP_Dropdown.OptionData selectedOptionDropdown = dropdown.options[index];

            if (selectedOptionDropdown.image != null)
            {
                // Set the image of the target image controller to the selected option in the dropdown
                targetImageController.sprite = selectedOptionDropdown.image;
            }
            else
            {
                Debug.LogWarning("Image not found for the selected option in the dropdown.");
                targetImageController.enabled = false;
            }

            string selectedOption = dropdown.options[index].text;           
            GameObject prefabToSpawn = imageLoader.GetPrefabByName(selectedOption);

            if (prefabToSpawn != null)
            {
                spawnPrefab.SetTargetObject(prefabToSpawn, selectedOption);
            }
            else
            {
                Debug.LogWarning("Error: Prefab not found for the selected option in the dropdown: " + selectedOption);
            }
        }
        else
        {
            Debug.LogWarning("Index out of range for the dropdown options: " + index);
        }
    }
}
