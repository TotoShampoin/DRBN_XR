using System.Collections.Generic;
using UnityEngine;
using MarchingCubing.V2;
using WeightGeneration;
using WeightPainting;
using Voxelization;
using System.Linq;
using UnityEngine.InputSystem;
using TMPro;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChunkGrid : MonoBehaviour
{
    public WeightGenerator weightGenerator;
    public WeightPainterNobehaviour weightPainter;
    public MarchingCubesRef marchingCubes;
    public Voxelizer voxelizer;
    public DistanceOfVolumes distanceOfVolumes;

    public Material material;
    public Material highlightMaterial;
    public RenderTexture baseVolume;

    public Transform cursor;
    public InputActionReference primary;
    public InputActionReference secondary;

    public TextMeshPro textAtCursor;
    public VolumeRenderer volumeRenderer;

    public int marchResolution = 8;
    public int initNbChunks = 15;
    public float cycleRate = 0f;
    public float paintRadius = 0.2f;
    public float paintWeight = 0.5f;
    public float distanceThreshold = 0.75f;
    [Range(0, 1f)] public float mergeDistance = 0.0001f;

    float offsetFactor;
    bool isPainting = false;
    bool eraseMode = false;

    float CycleInterval => 1f / cycleRate;
    float cycle = 0f;

    Vector3 CursorPos => transform.InverseTransformPoint(cursor.transform.position);

    public class ChunkData
    {
        public Mesh mesh;
        public RenderTexture volume;        // True volume
        public RenderTexture volumeOfMesh;  // Volume that rendered the mesh
        public bool isDirty;
        public bool highlight;
    };
    readonly Dictionary<Vector3Int, ChunkData> grid = new();

    void Start()
    {
        PrepareModules();
        int N = initNbChunks / 2;
        for (int x = -N; x <= N; x++)
            for (int y = -1; y <= 2; y++)
                for (int z = -N; z <= N; z++)
                {
                    var at = new Vector3Int(x, y, z);
                    CreateChunk(at);
                    RegenerateChunk(at);
                    MarchChunkDirty(at, 0, true);
                }

        volumeRenderer.texture = new(baseVolume);

        primary.action.Enable();
        secondary.action.Enable();

        primary.action.performed += _ => isPainting = true;
        secondary.action.performed += _ => eraseMode = true;
        primary.action.canceled += _ => isPainting = false;
        secondary.action.canceled += _ => eraseMode = false;
    }

    void Update()
    {
        cycle += Time.deltaTime;
        if (isPainting) PaintAll(eraseMode);

        PrepareModules();

        Render();
        if (cycle >= CycleInterval)
        {
            VoxelizeAll();
            cycle %= CycleInterval;
        }
        MarchAll(0, false);
    }

    Mesh sphere;
    void Render()
    {
        RenderParams rp = new(material);
        RenderParams rph = new(highlightMaterial);
        foreach (var (pos, chunk) in grid)
        {
            Vector3 posf = pos;
            Graphics.RenderMesh(
                chunk.highlight ? rph : rp, chunk.mesh, 0,
                transform.localToWorldMatrix *
                    Matrix4x4.TRS(posf * offsetFactor, Quaternion.identity, Vector3.one)
            );
        }

        if (sphere == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
        }
        Graphics.RenderMesh(
            rph, sphere, 0,
            transform.localToWorldMatrix *
                Matrix4x4.TRS(CursorPos, Quaternion.identity, Vector3.one * paintRadius)
        );

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
            volumeOfMesh = new RenderTexture(baseVolume),
        });
    }
    public bool ChunkExists(Vector3Int at)
    {
        return grid.ContainsKey(at) && grid[at] != null;
    }

    public void RegenerateAll()
    {
        foreach (var pos in grid.Keys)
        {
            RegenerateChunk(pos);
            MarchChunk(pos);
        }
    }
    public void VoxelizeAll()
    {
        foreach (var (pos, chunk) in grid)
        {
            VoxelizeChunk(pos);
        }
    }
    public void MarchAll(float threshold = 0, bool forceAll = false)
    {
        foreach (var (pos, chunk) in grid)
        {
            MarchChunkDirty(pos, threshold, forceAll);
        }
    }
    public void CycleAll()
    {
        VoxelizeAll();
        MarchAll();
    }
    public void PaintAll(bool eraseMode)
    {
        foreach (var (pos, chunk) in grid)
        {
            PaintChunk(pos, eraseMode);
        }
    }

    public void MarchChunkDirty(Vector3Int at, float threshold = 0, bool force = false)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        chunk.highlight = false;
        if (!force)
        {
            if (!chunk.isDirty) return;
            chunk.highlight = true;
            // if (DistanceOhMeshInChunk(at) < distanceThreshold)
            // {
            //     chunk.isDirty = false;
            //     return;
            // }
        }
        MarchChunk(at, threshold);
        chunk.isDirty = false;
    }

    public void RegenerateChunk(Vector3Int at)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        Vector3 atf = at;
        weightGenerator.offset = atf * offsetFactor;
        weightGenerator.Generate(chunk.volume);
        chunk.isDirty = true;
    }
    public void VoxelizeChunk(Vector3Int at)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        var vertices = chunk.mesh.vertices.ToList();
        var triangles = chunk.mesh.triangles.ToList();
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
            if (!ChunkExists(neighborPos))
                continue;
            var neighborChunk = grid[neighborPos];
            if (neighborChunk != null && neighborChunk.mesh != null)
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

        voxelizer.Voxelize(mesh, chunk.volume);
        chunk.isDirty = true;
    }
    public void MarchChunk(Vector3Int at, float threshold = 0)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        marchingCubes.GenerateMesh(chunk.volume, threshold, chunk.mesh);
        chunk.mesh = MeshMod.DeduplicateVertices(chunk.mesh, mergeDistance);
        Graphics.Blit(chunk.volume, chunk.volumeOfMesh);
    }
    public void PaintChunk(Vector3Int at, bool eraseMode)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        Vector3 pos = at;
        Vector3 chunkCenter = pos * offsetFactor;
        Bounds chunkBoundsMargin = new(chunkCenter, Vector3.one * offsetFactor);
        Bounds paintBounds = new(chunkCenter, Vector3.one * 2);
        chunkBoundsMargin.Expand(paintRadius * 4);

        if (chunkBoundsMargin.Contains(CursorPos))
        {
            weightPainter.Paint(chunk.volume, CursorPos, paintBounds, paintRadius, paintWeight, eraseMode ? WeightPainterNobehaviour.ActionMode.Subtract : WeightPainterNobehaviour.ActionMode.Add);
            chunk.isDirty = true;
        }
    }
    public float DistanceOhMeshInChunk(Vector3Int at, RenderTexture output = null)
    {
        if (!ChunkExists(at))
            return 0f;
        var chunk = grid[at];
        return distanceOfVolumes.Distance(chunk.volume, chunk.volumeOfMesh, output);
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
        if (GUILayout.Button("Voxelize All"))
        {
            grid.VoxelizeAll();
        }
    }
}
#endif
