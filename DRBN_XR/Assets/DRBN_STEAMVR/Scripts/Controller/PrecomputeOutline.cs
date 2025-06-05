using UnityEngine;
using UnityEditor;
using System.Xml;
using System.IO;
using System.Reflection;
/// <summary>
/// PrefabOutlineFixer class to add outlines to the molecules in the XML
/// It will check if the prefab already has an outline and if not, it will add one and precompute it.
/// It will also check if the outline is already precomputed and if not, it will precompute it.
/// </summary>

public class PrecomputeOutline : MonoBehaviour
{
    public string xmlFilePath = "Resources/XML/PrefabList.xml"; // Path to the XML file
    public void Start()
    {
        int outlineGenerationCount = 0;
        string xmlPath = Path.Combine(Application.dataPath, xmlFilePath);

        if (!File.Exists(xmlPath))
        {
            Debug.LogError("XML not found: " + xmlPath);
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlPath);
        XmlNodeList nodes = xmlDoc.SelectNodes("//Image");


        foreach (XmlNode node in nodes)
        {
            string prefabPath = node.Attributes["prefab"]?.Value;

            if (string.IsNullOrEmpty(prefabPath))
                continue;

            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogWarning("Prefab not found: " + prefabPath);
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            ModifiedOutline outline = instance.GetComponent<ModifiedOutline>();
            bool needsSave = false;

            if (!outline)
            {
                Debug.Log("Adding Outline to prefab: " + prefabPath);

                outlineGenerationCount++;

                outline = instance.AddComponent<ModifiedOutline>();
                outline.OutlineColor = Color.white;
                outline.OutlineWidth = 10f;
                outline.OutlineMode = ModifiedOutline.Mode.OutlineAll;
                outline.enabled = false;
                needsSave = true;
            }

            // Check if the outline is already precomputed, precomputeOutline is a private field
            bool isPrecomputed = outline.precomputeOutline;

            if (!isPrecomputed)
            {
                outline.precomputeOutline = true;
                needsSave = true;
            }

            if (needsSave)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
                Debug.Log("Updated prefab: " + prefabPath);
            }

            DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Outline precomputation completed, " + outlineGenerationCount + " prefabs updated.");
    }
}
