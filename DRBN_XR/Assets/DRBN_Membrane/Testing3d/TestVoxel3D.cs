using System.Collections.Generic;
using MarchingCubing.V2;
using UnityEngine;
using Voxelization;
using UnityEditor;

public class TestVoxel3D : MonoBehaviour
{
    public enum RenderMode
    {
        Original,
        BeforeMarch,
        AfterMarch,
    };


    public RenderTexture volume;
    public Mesh mesh;
    public NormalArrow1 NormalArrow1;
    public NormalArrow1 NormalArrow12;
    public RenderMode renderMode = RenderMode.Original;
    public Vector3 offset = Vector3.zero;
    public GameObject debugSphere;
    public float mergeDistance = 0.0001f;
    Voxelizer voxelizer;
    // VolumeRenderer volumeRenderer;
    MarchingCubesRef marchingCubes;
    MeshFilter meshFilter;
    Mesh toVoxelize;
    Mesh toRender;
    readonly VoxelizerCPU voxelizerCPU = new();
    readonly GameObject[] triangle = new GameObject[3];

    public bool remarch = true;
    bool NormalArrow1Dirty = true;
    Vector3 arrowPos;

    void Start()
    {
        voxelizer = GetComponent<Voxelizer>();
        // volumeRenderer = GetComponent<VolumeRenderer>();
        marchingCubes = GetComponent<MarchingCubesRef>();
        meshFilter = GetComponent<MeshFilter>();
        toVoxelize = new();
        toRender = new();

        triangle[0] = Instantiate(debugSphere);
        triangle[1] = Instantiate(debugSphere);
        triangle[2] = Instantiate(debugSphere);
    }

    void Update()
    {
        if (NormalArrow1.Origin != arrowPos)
        {
            NormalArrow1Dirty = true;
        }

        if (remarch)
        {
            voxelizerCPU.distanceThreshold = voxelizer.distanceThreshold;
            voxelizerCPU.voxelBound = voxelizer.voxelBounds;
            voxelizerCPU.meshBound = new(Vector3.zero, 2 * Vector3.one);

            // toVoxelize = mesh;
            MeshMod.DeduplicateVertices(mesh, mergeDistance, result: toVoxelize);
            MeshMod.OffsetMesh(toVoxelize, offset, toVoxelize);
            voxelizerCPU.mesh = toVoxelize;
            voxelizer.Voxelize(toVoxelize, volume);
            marchingCubes.GenerateMesh(volume, 0, toRender);
            // Log all triangles as an array of triplets in one log
            var tris = toVoxelize.triangles;
            var triplets = new List<string>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                triplets.Add($"({tris[i]}, {tris[i + 1]}, {tris[i + 2]})");
            }
            Debug.Log($"Triangles: [{string.Join(", ", triplets)}]");

            remarch = false;
        }

        if (NormalArrow1Dirty)
        {
            var data = voxelizerCPU.VoxelizeAtPositionDebug(NormalArrow1.Origin);
            NormalArrow1.Direction = Vector3.Normalize(data.projectedPoint - NormalArrow1.Origin);
            NormalArrow1.Distance = data.signedDistance;
            arrowPos = NormalArrow1.Origin;
            NormalArrow1Dirty = false;

            NormalArrow12.Origin = data.projectedPoint;
            NormalArrow12.Direction = data.projectedNormal;

            triangle[0].transform.position = data.vertices.Item1;
            triangle[1].transform.position = data.vertices.Item2;
            triangle[2].transform.position = data.vertices.Item3;
        }

        switch (renderMode)
        {
            case RenderMode.Original:
                meshFilter.sharedMesh = mesh;
                break;
            case RenderMode.BeforeMarch:
                meshFilter.sharedMesh = toVoxelize;
                break;
            case RenderMode.AfterMarch:
                meshFilter.sharedMesh = toRender;
                break;
        }

        // volumeRenderer.DrawVolume(volume, new Bounds(Vector3.zero, Vector3.one), transform.localToWorldMatrix);
    }
}

[CustomEditor(typeof(TestVoxel3D))]
public class TestVoxel3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestVoxel3D script = (TestVoxel3D)target;

        GUILayout.Space(10);
        GUILayout.Label("Custom Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Save mesh"))
        {
            MeshLoader.SaveMesh(
                script.GetComponent<MeshFilter>().sharedMesh,
                $"Assets/DRBN_Membrane/Testing3d/mesh.asset");
        }
    }
}