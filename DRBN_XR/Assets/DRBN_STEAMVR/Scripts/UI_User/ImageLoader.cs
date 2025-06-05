using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class used to add every Molecules in the XML file in the Molecules menu
/// Order the molecules by name, add prefab in the dropdown if selected
/// </summary>
public class ImageLoader : MonoBehaviour
{
    [Tooltip("Path to XLM from Resources/")]
    public string xmlFilePath = "XML/PrefabList";

    [Tooltip("Panel with the pictures")]
    public GameObject imageContainer;

    [Tooltip("Prefab image")]
    public GameObject imageTextPrefab;

    [Tooltip("DropdownTMP")]
    public TMP_Dropdown dropdown;

    [Tooltip("Script DropdownSelection")]
    public DropdownSelection dropdownSelection;

    private Dictionary<string, GameObject> imageToggleObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    void Start()
    {
        LoadImagesFromXML();
    }

    void LoadImagesFromXML()
    {
        TextAsset xmlFile = Resources.Load<TextAsset>(xmlFilePath);
        if (xmlFile == null)
        {
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            xmlDoc.LoadXml(xmlFile.text);
        }
        catch (System.Exception e)
        {
            return;
        }

        XmlNodeList imageNodes = xmlDoc.SelectNodes("Images/Image");
        if (imageNodes == null || imageNodes.Count == 0)
        {
            return;
        }


        List<(Sprite image, string name, GameObject prefab)> loadedImages = new List<(Sprite, string, GameObject)>();

        foreach (XmlNode imageNode in imageNodes)
        {
            string imageName = imageNode.Attributes["name"]?.Value;
            string imagePath = imageNode.Attributes["path"]?.Value;
            string prefabPath = imageNode.Attributes["prefab"]?.Value;

            if (string.IsNullOrEmpty(imageName) || string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            Sprite image = Resources.Load<Sprite>(imagePath);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (image != null && prefab != null)
            {
                loadedImages.Add((image, imageName, prefab));
                prefabs[imageName] = prefab; // save prefab in dictionary
            }
        }

        // Image order by name
        loadedImages = loadedImages.OrderBy(image => image.name).ToList();

        DisplayImages(loadedImages);
    }

    void DisplayImages(List<(Sprite image, string name, GameObject prefab)> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        GridLayoutGroup gridLayout = imageContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = imageContainer.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
        }

        foreach (var (image, name, prefab) in images)
        {
            GameObject instance = Instantiate(imageTextPrefab, imageContainer.transform);
            instance.name = name; // instance name same as in XLM

            Image imageComponent = instance.GetComponent<Image>();
            TextMeshProUGUI textComponent = instance.GetComponentInChildren<TextMeshProUGUI>();
            ImageClickHandler clickHandler = instance.GetComponent<ImageClickHandler>();

            if (imageComponent != null)
            {
                imageComponent.sprite = image;
                imageComponent.preserveAspect = true; // save the ratio of the picture
            }

            if (textComponent != null)
            {
                textComponent.text = name;
            }

            if (clickHandler != null)
            {
                clickHandler.imageName = name; // image name
                clickHandler.dropdown = dropdown; // DropdownTMP
                clickHandler.imageLoader = this; // imageLoader
                clickHandler.image = image;
            }

            // follow state
            imageToggleObjects[name] = instance;
        }

        // Add scroll rect only if necessary
        ScrollRect scrollRect = imageContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = imageContainer.AddComponent<ScrollRect>();
            scrollRect.content = imageContainer.GetComponent<RectTransform>();
            scrollRect.viewport = imageContainer.transform.parent.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
        }
    }

    public GameObject GetPrefabByName(string name)
    {
        if (prefabs.TryGetValue(name, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }

    public void RemoveOptionFromDropdown(string optionText)
    {
        TMP_Dropdown.OptionData option = dropdown.options.Find(o => string.Equals(o.text, optionText));
        if (option != null)
        {
            dropdown.options.Remove(option);
            dropdown.RefreshShownValue();

            // update prefab dropdown
            if (dropdownSelection != null)
            {
                dropdownSelection.ForceUpdateSpawnPrefab();
            }
        }
    }
}
