using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class used to put an image in a dropdown, or remove it
/// </summary>
public class ImageClickHandler : MonoBehaviour
{
    [Tooltip("Image's name")]
    public string imageName;
    [Tooltip("Dropdown for the selected prefab")]
    public TMP_Dropdown dropdown;
    [Tooltip("Script ImageLoader")]
    public ImageLoader imageLoader;
    [Tooltip("Button component for the click ")]
    private Button button;
    [Tooltip("To put a picture on an option")]
    public Sprite image;


    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnImageClick);
        }
    }

    void OnImageClick()
    {
        if (dropdown != null && !string.IsNullOrEmpty(imageName))
        {
            // if prefab is in dropdown, delete it
            if (dropdown.options.Any(option => option.text == imageName))
            {
                imageLoader.RemoveOptionFromDropdown(imageName);
            }
            else
            {
                // add the prefab
                dropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData(imageName,image,Color.white) });
            }

            DropdownSelection dropdownSelection = dropdown.GetComponent<DropdownSelection>();
            if (dropdownSelection != null)
            {
                dropdownSelection.ForceUpdateSpawnPrefab();
            }
        }
    }
}
