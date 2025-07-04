using UnityEngine;
using Voxelization;

public class TestVoxel3D : MonoBehaviour
{
    public RenderTexture volume;
    public Mesh mesh;
    Voxelizer voxelizer;
    VolumeRenderer volumeRenderer;

    void Start()
    {
        voxelizer = GetComponent<Voxelizer>();
        volumeRenderer = GetComponent<VolumeRenderer>();
    }

    void Update()
    {
        voxelizer.Voxelize(mesh, volume);
        volumeRenderer.DrawVolume(volume, new Bounds(Vector3.zero, Vector3.one), transform.localToWorldMatrix);
    }
}
