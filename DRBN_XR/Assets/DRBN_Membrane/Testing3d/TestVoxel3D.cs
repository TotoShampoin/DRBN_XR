using MarchingCubing.V2;
using UnityEngine;
using Voxelization;

public class TestVoxel3D : MonoBehaviour
{
    public RenderTexture volume;
    public Mesh mesh;
    public NormalArrow normalArrow;
    public NormalArrow normalArrow2;
    Voxelizer voxelizer;
    // VolumeRenderer volumeRenderer;
    MarchingCubesRef marchingCubes;
    MeshFilter meshFilter;
    Mesh toVoxelize;
    Mesh toRender;
    readonly VoxelizerCPU voxelizerCPU = new();

    public bool remarch = true;
    bool normalArrowDirty = true;
    Vector3 arrowPos;

    void Start()
    {
        voxelizer = GetComponent<Voxelizer>();
        // volumeRenderer = GetComponent<VolumeRenderer>();
        marchingCubes = GetComponent<MarchingCubesRef>();
        meshFilter = GetComponent<MeshFilter>();
        toVoxelize = new();
        toRender = new();
    }

    void Update()
    {
        if (normalArrow.Origin != arrowPos)
        {
            normalArrowDirty = true;
        }

        if (remarch)
        {
            voxelizerCPU.distanceThreshold = voxelizer.distanceThreshold;
            voxelizerCPU.voxelBound = voxelizer.voxelBounds;
            voxelizerCPU.meshBound = new(Vector3.zero, 2 * Vector3.one);

            MeshMod.DeduplicateVertices(mesh, result: toVoxelize);
            // toVoxelize = mesh;
            // voxelizer.Voxelize(toVoxelize, volume);
            // marchingCubes.GenerateMesh(volume, 0, toRender);
            // meshFilter.sharedMesh = toRender;
            meshFilter.sharedMesh = toVoxelize;
            voxelizerCPU.mesh = toVoxelize;

            remarch = false;
        }

        if (normalArrowDirty)
        {
            var data = voxelizerCPU.VoxelizeAtPositionDebug(normalArrow.Origin);
            normalArrow.Direction = Vector3.Normalize(data.projectedPoint - normalArrow.Origin);
            normalArrow.Distance = data.signedDistance;
            arrowPos = normalArrow.Origin;
            normalArrowDirty = false;

            normalArrow2.Origin = data.projectedPoint;
            normalArrow2.Direction = data.projectedNormal;
        }

        // volumeRenderer.DrawVolume(volume, new Bounds(Vector3.zero, Vector3.one), transform.localToWorldMatrix);
    }
}
