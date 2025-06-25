using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using System.IO;
using UnityEditor;

/// <summary>
/// Class which take picture of the prefab if he doesn't have sprite and stock it in Asset/Resources/Images/Molecules
/// </summary>
public class PrefabImageLoaderAuto : MonoBehaviour
{
    [Tooltip("Place where the prefab sapwn to take a picture")]
    public Vector3 previewPosition = new Vector3(1000, 0, 1000);

    [Tooltip("Size of the picture")]
    public int textureSize = 1024;
    public bool debugDontDestroy = false;
    private string xmlFilePath = "Resources/XML/PrefabList.xml"; // path from Assets repository
    private string imagesFolderPath = "Resources/Images/Molecules"; // Repository to stock images

    void Start()
    {
        // Full path to xml file
        string fullXmlPath = Path.Combine(Application.dataPath, xmlFilePath);

        if (!File.Exists(fullXmlPath))
        {
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(fullXmlPath);

        XmlNodeList imageNodes = xmlDoc.SelectNodes("//Image");
        foreach (XmlNode imageNode in imageNodes)
        {
            string imageName = imageNode.Attributes["name"]?.Value;
            string prefabPath = imageNode.Attributes["prefab"]?.Value;

            if (string.IsNullOrEmpty(imageName) || string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            string fullPath = Path.Combine(Application.dataPath, imagesFolderPath, imageName + ".png");

            if (File.Exists(fullPath))
            {
                continue;
            }

            // Load prefab in real time
            GameObject moleculePrefab = Resources.Load<GameObject>(prefabPath);
            if (moleculePrefab == null)
            {
                continue;
            }

            CaptureAndSaveImage(moleculePrefab, imageName, fullPath);
        }

        AssetDatabase.Refresh();
    }

    // Take the picture and stock it
    void CaptureAndSaveImage(GameObject moleculePrefab, string imageName, string fullPath)  
    {
        RenderTexture renderTexture = new RenderTexture(textureSize, textureSize, 16);
        renderTexture.Create();

        GameObject molecule = Instantiate(moleculePrefab);

        molecule.transform.position = previewPosition;
        Bounds bounds = CalculateBounds(molecule);
        Vector3 center = bounds.center;
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        GameObject camObj = new GameObject("SnapshotCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.targetTexture = renderTexture;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;

        Vector3 viewDirection = new Vector3(1, 0.2f, 1).normalized;
        float distance = maxSize * 1.5f;

        cam.transform.position = center + viewDirection * distance;
        cam.transform.LookAt(center);

        cam.Render();

        RenderTexture.active = renderTexture;
        Texture2D snapshot = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        snapshot.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        snapshot.Apply();
        RenderTexture.active = null;

        if (!debugDontDestroy)
        {
            DestroyImmediate(molecule);
            DestroyImmediate(camObj);
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }

        byte[] pngData = snapshot.EncodeToPNG();

        Directory.CreateDirectory(Path.Combine(Application.dataPath, imagesFolderPath));

        File.WriteAllBytes(fullPath, pngData);

        AssetDatabase.Refresh();

        ConfigureTextureImportSettings(fullPath);

        Sprite sprite = CreateSpriteFromTexture(snapshot, imageName);
    }

    // PNG to Sprite
    void ConfigureTextureImportSettings(string fullPath)    
    {
        string assetPath = Path.GetRelativePath(Application.dataPath, fullPath).Replace("\\", "/");
        assetPath = "Assets/" + assetPath;

        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Sprite;

            var textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            textureImporterSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureImporterSettings.spriteMode = (int)SpriteImportMode.Single;
            textureImporter.SetTextureSettings(textureImporterSettings);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
    }

    Sprite CreateSpriteFromTexture(Texture2D texture, string name)
    {
        Rect rect = new Rect(0, 0, texture.width, texture.height);

        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        sprite.name = name;

        return sprite;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }
}
