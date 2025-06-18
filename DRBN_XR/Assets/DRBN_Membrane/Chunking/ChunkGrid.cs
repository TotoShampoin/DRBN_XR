using System.Collections.Generic;
using UnityEngine;
using MarchingCubing.V2;
using WeightGeneration;
using WeightPainting;
using Voxelization;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChunkGrid : MonoBehaviour
{
    public WeightGenerator weightGenerator;
    public WeightPainterNobehaviour weightPainter;
    public MarchingCubesRef marchingCubes;
    public Voxelizer voxelizer;
    public Material material;
    public RenderTexture baseVolume;

    public int marchResolution = 8;
    public bool constantCycle = false;
    float offsetFactor;

    public class ChunkData
    {
        public Mesh mesh;
        public RenderTexture volume;
    };
    readonly Dictionary<Vector3Int, ChunkData> grid = new();

    void Start()
    {
        PrepareModules();
        int N = 2;
        int y = 0;
        for (int x = -N; x <= N; x++)
            // for (int y = -N; y <= N; y++)
            for (int z = -N; z <= N; z++)
            {
                var at = new Vector3Int(x, y, z);
                CreateChunk(at);
                RegenerateChunk(at);
                MarchChunk(at, weightGenerator.Threshold);
            }
    }

    void Update()
    {
        RenderParams rp = new(material);
        foreach (var (pos, chunk) in grid)
        {
            Vector3 posf = pos;
            Graphics.RenderMesh(
                rp, chunk.mesh, 0,
                transform.localToWorldMatrix * Matrix4x4.TRS(posf * offsetFactor, Quaternion.identity, Vector3.one)
            );
        }
        if (constantCycle)
            CycleAll();
    }

    void PrepareModules()
    {
        offsetFactor = 2f - (float)4 / marchResolution;
        marchingCubes.resolution = marchResolution;
        marchingCubes.bounds.size = new Vector3(offsetFactor, offsetFactor, offsetFactor);
    }

    public void CreateChunk(Vector3Int at)
    {
        grid.TryAdd(at, new ChunkData()
        {
            mesh = new Mesh(),
            volume = new RenderTexture(baseVolume),
        });
    }

    public void RegenerateChunk(Vector3Int at)
    {
        Vector3 atf = at;
        weightGenerator.offset = atf * offsetFactor;
        weightGenerator.Generate(grid[at].volume);
    }
    public void VoxelizeChunk(Vector3Int at)
    {
        var vertices = grid[at].mesh.vertices.ToList();
        var triangles = grid[at].mesh.triangles.ToList();
        Vector3Int[] neighbors = {
            new (1, 0, 0), new (-1, 0, 0),
            new (0, 1, 0), new (0, -1, 0),
            new (0, 0, 1), new (0, 0, -1),
            new (1, 1, 0), new (1, -1, 0), new (-1, 1, 0), new (-1, -1, 0),
            new (1, 0, 1), new (1, 0, -1), new (-1, 0, 1), new (-1, 0, -1),
            new (0, 1, 1), new (0, 1, -1), new (0, -1, 1), new (0, -1, -1),
            new (1, 1, 1), new (1, 1, -1), new (1, -1, 1), new (1, -1, -1),
            new (-1, 1, 1), new (-1, 1, -1), new (-1, -1, 1), new (-1, -1, -1)
        };

        int vertexOffset = vertices.Count;
        foreach (var neighbor in neighbors)
        {
            Vector3Int neighborPos = at + neighbor;
            if (grid.TryGetValue(neighborPos, out var neighborChunk) && neighborChunk.mesh != null)
            {
                var neighborVertices = neighborChunk.mesh.vertices;
                var neighborTriangles = neighborChunk.mesh.triangles;
                Vector3 delta = neighborPos - at;
                Vector3 offset = delta * offsetFactor;
                int baseIndex = vertices.Count;
                vertices.AddRange(neighborVertices.Select(v => v + offset));
                triangles.AddRange(neighborTriangles.Select(i => i + baseIndex));
            }
        }
        var mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();

        voxelizer.Voxelize(mesh, grid[at].volume);
    }
    public void MarchChunk(Vector3Int at, float threshold = 0)
    {
        marchingCubes.GenerateMesh(grid[at].volume, threshold, grid[at].mesh);
        MeshMod.DeduplicateVertices(grid[at].mesh);
    }

    public void RegenerateAll()
    {
        PrepareModules();
        foreach (var pos in grid.Keys)
        {
            RegenerateChunk(pos);
            MarchChunk(pos);
        }
    }
    public void VoxelizeAll()
    {
        PrepareModules();
        foreach (var pos in grid.Keys)
        {
            VoxelizeChunk(pos);
        }
    }
    public void MarchAll()
    {
        PrepareModules();
        foreach (var pos in grid.Keys)
        {
            MarchChunk(pos);
        }
    }
    public void CycleAll()
    {
        VoxelizeAll();
        MarchAll();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (grid.Count > 0)
        {
            var min = new Vector3(
                grid.Keys.Min(k => k.x),
                grid.Keys.Min(k => k.y),
                grid.Keys.Min(k => k.z)
            );
            var max = new Vector3(
                grid.Keys.Max(k => k.x),
                grid.Keys.Max(k => k.y),
                grid.Keys.Max(k => k.z)
            );
            Gizmos.color = Color.yellow;
            Vector3 center = 0.5f * offsetFactor * (min + max);
            Vector3 size = (max - min + Vector3.one) * offsetFactor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(ChunkGrid))]
public class ChunkGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChunkGrid grid = (ChunkGrid)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Regenerate All Chunks"))
        {
            grid.RegenerateAll();
        }
        if (GUILayout.Button("Voxelizer Cycle"))
        {
            grid.CycleAll();
        }
    }
}
#endif
