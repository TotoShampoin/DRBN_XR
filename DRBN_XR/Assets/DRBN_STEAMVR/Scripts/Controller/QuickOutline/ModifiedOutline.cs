//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//
// Modified version : fix when no mesh is assigned to the MeshFilter (skip), making precomputeOutline public (to precompute when starting when adding prefab)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class ModifiedOutline : MonoBehaviour {
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  public bool precomputeOutline;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  private bool needsUpdate;

  void Awake() {

    // Cache renderers
    renderers = GetComponentsInChildren<Renderer>();

    // Instantiate outline materials
    outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Apply material properties immediately
    needsUpdate = true;
  }

  void OnEnable() {
    foreach (var renderer in renderers) {

      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnValidate() {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (needsUpdate) {
      needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  void OnDisable() {
    foreach (var renderer in renderers) {

      // Remove outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnDestroy() {

    // Destroy material instances
    Destroy(outlineMaskMaterial);
    Destroy(outlineFillMaterial);
  }

  void Bake() {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {
    // Retrieve or generate smooth normals
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
        var sharedMesh = meshFilter.sharedMesh;

        // Skip if mesh is null (added)
        if (sharedMesh == null) {
            Debug.LogWarning("[QuickOutline] Skipping MeshFilter with null sharedMesh.");
            continue;
        }

        // Skip if smooth normals have already been adopted
        if (!registeredMeshes.Add(sharedMesh)) {
            continue;
        }

        // Retrieve or generate smooth normals
        var index = bakeKeys.IndexOf(sharedMesh);
        var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(sharedMesh);

        // Store smooth normals in UV3 (if available (added))
        if (smoothNormals != null && smoothNormals.Count == sharedMesh.vertexCount) {
            sharedMesh.SetUVs(3, smoothNormals);
        } else {
            Debug.LogWarning("[QuickOutline] Skipping UV3 assignment due to mismatched vertex count.");
        }
    }

    // Clear UV4 on skinned mesh renderers
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
        var sharedMesh = skinnedMeshRenderer.sharedMesh;

        if (sharedMesh == null) {
            Debug.LogWarning("[QuickOutline] Skipping SkinnedMeshRenderer with null sharedMesh.");
            continue;
        }

        if (registeredMeshes.Add(sharedMesh)) {
            sharedMesh.uv4 = new Vector2[sharedMesh.vertexCount];
        }
    }
  }

  List<Vector3> SmoothNormals(Mesh mesh) {
    var smoothNormals = new List<Vector3>();

    // Vérification de sécurité
    if (mesh == null || mesh.vertices == null || mesh.normals == null) {
        Debug.LogWarning("[QuickOutline] Mesh is null or missing vertices/normals in SmoothNormals.");
        return smoothNormals;
    }

    // Vérification de cohérence des tailles
    if (mesh.vertices.Length != mesh.normals.Length) {
        Debug.LogWarning("[QuickOutline] Mismatch between vertices and normals count in SmoothNormals.");
        return smoothNormals;
    }

    // Group vertices by location
    var groups = mesh.vertices
        .Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
        .GroupBy(pair => pair.Key);

    // Copy normals to a new list
    smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups) {

        // Skip single vertices
        if (group.Count() == 1) {
            continue;
        }

        // Calculate the average normal
        var smoothNormal = Vector3.zero;

        foreach (var pair in group) {
            smoothNormal += mesh.normals[pair.Value];
        }

        smoothNormal.Normalize();

        // Assign smooth normal to each vertex
        foreach (var pair in group) {
            smoothNormals[pair.Value] = smoothNormal;
        }
    }

    return smoothNormals;
  }

  void UpdateMaterialProperties() {

    // Apply properties according to mode
    outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

    switch (outlineMode) {
      case Mode.OutlineAll:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineVisible:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineHidden:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineAndSilhouette:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.SilhouetteOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", 0);
        break;
    }
  }
}
