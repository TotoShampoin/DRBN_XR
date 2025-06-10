using UnityEngine;

public class VolumeLoader
{
    // copilot generated code
    public static void SaveMesh(RenderTexture texture, string path)
    {
        if (texture == null)
        {
            Debug.LogError("RenderTexture is null. Cannot save.");
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Path is null or empty. Cannot save mesh.");
            return;
        }

        // Create a new asset file at the specified path
        var assetPath =
            path.EndsWith(".asset") ? path :
            System.IO.Path.Combine(path, $"{texture.name}.asset");
        UnityEditor.AssetDatabase.CreateAsset(texture, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"RenderTexture saved to {assetPath}");
    }

    // copilot generated code
    public static RenderTexture LoadMesh(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Path is null or empty. Cannot load mesh.");
            return null;
        }

        // Load the mesh asset from the specified path
        var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (texture == null)
        {
            Debug.LogError($"No mesh found at {path}");
            return null;
        }

        Debug.Log($"RenderTexture loaded from {path}");
        return texture;
    }
}