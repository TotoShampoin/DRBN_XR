using UnityEngine;
using Voxelization;

[RequireComponent(typeof(Voxelizer))]
public class TestSlice : MonoBehaviour
{
    public Transform quad;
    public MeshFilter meshFilter;
    public RenderTexture renderTexture;
    public Material material;
    [Range(0, 1)] public float zSlice;
    [Range(0, 1)] public float threshold;
    [Range(0, 1)] public float thresholdThickness;
    [Range(0, 0.1f)] public float dedupeMargin;
    public Vector3 offset;
    [Range(1, 10)] public float size = 1;
    public bool updateVoxelizer = true;

    Voxelizer voxelizer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        voxelizer = GetComponent<Voxelizer>();
    }

    // Update is called once per frame
    void Update()
    {
        material.SetFloat("_Z", zSlice);
        material.SetFloat("_Threshold", threshold);
        material.SetFloat("_ThresholdThickness", thresholdThickness);

        quad.position = new(0, 0, (zSlice - 0.5f) * size);
        quad.localScale = new(size, size, size);

        if (updateVoxelizer)
        {
            UpdateVolume();
            updateVoxelizer = false;
        }
    }

    public void UpdateVolume()
    {
        var mesh = meshFilter.mesh;
        MeshMod.OffsetMesh(mesh, offset, mesh);
        MeshMod.DeduplicateVertices(mesh, dedupeMargin);
        voxelizer.voxelBounds.size = new Vector3(size, size, size) * 2;
        voxelizer.Voxelize(mesh, renderTexture);
    }
}
