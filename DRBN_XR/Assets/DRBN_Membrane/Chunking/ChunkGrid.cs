using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

using System;
using System.Collections.Generic;
using System.Linq;

using MarchingCubing.V2;
using SpringSim.V3;
using WeightGeneration;
using WeightPainting;
using Voxelization;

public class ChunkGrid : MonoBehaviour
{
    [Header("Parameters")]
    public int marchResolution = 8;
    public int initNbChunks = 15;
    public float cycleRate = 0f;
    public float paintRadius = 0.2f;
    public float paintWeight = 0.5f;
    public float distanceThreshold = 0.75f;
    [Range(0, 1f)] public float mergeDistance = 0.0001f;
    public bool updateSprings = false;
    public bool forceUpdate = false;

    [Header("Rendering")]
    public Material material;
    public Material highlightMaterial;
    public RenderTexture baseVolume;
    public RenderMode renderMode = RenderMode.MarchedMesh;

    [Header("Interaction")]
    public Transform cursor;
    public InputActionReference primary;
    public InputActionReference secondary;
    public UnityEvent<Vector3> onCursorRayHit;

    [Header("Components")]
    public WeightGenerator weightGenerator;
    public SpringSimulatorNoBehaviour springSimulator;
    public MarchingCubesRef marchingCubes;
    public Voxelizer voxelizer;
    public WeightPainterNobehaviour weightPainter;
    public DistanceOfVolumes distanceOfVolumes;
    public SpringsRenderer springsRenderer;

    float offsetFactor;
    bool isPainting = false;
    bool eraseMode = false;

    float CycleInterval => 1f / cycleRate;
    float cycle = 0f;

    Matrix4x4 objectTransform = Matrix4x4.identity;

    Vector3 GlobalCursorPos => cursor.transform.position;
    Vector3 GlobalCursorDir => cursor.transform.forward;
    Ray GlobalCursorRay => new(GlobalCursorPos, GlobalCursorDir);
    Vector3 LocalCursorPos => WorldToLocalPosition(cursor.transform.position);

    public enum RenderMode
    {
        MarchedMesh,
        Springs,
    }
    public class ChunkData
    {
        public Mesh mesh;
        public RenderTexture volume;        // True volume
        public RenderTexture volumeOfMesh;  // Volume that rendered the mesh
        public SpringSimulatorState springs;
        public Dictionary<ChunkData, List<(Mass self, Mass other)>> jointures;
        public bool isDirtyForMesh;
        public bool isDirtyForVoxels;
        public bool isDirtyForSprings;
        public bool highlight;
    };
    readonly Dictionary<Vector3Int, ChunkData> grid = new();
    public (Vector3Int, ChunkData)? GetChunk(Vector3 at)
    {
        Vector3Int chunkPos = Vector3Int.RoundToInt(WorldToGridPosition(at));
        if (grid.TryGetValue(chunkPos, out ChunkData chunk))
            return (chunkPos, chunk);
        return null;
    }

    public Vector3 WorldToLocalPosition(Vector3 worldPos) =>
        objectTransform.inverse.MultiplyPoint(worldPos);
    public Vector3 LocalToWorldPosition(Vector3 localPos) =>
        objectTransform.MultiplyPoint(localPos);

    public Vector3 WorldToLocalDirection(Vector3 worldPos) =>
        objectTransform.inverse.MultiplyVector(worldPos);
    public Vector3 LocalToWorldDirection(Vector3 localPos) =>
        objectTransform.MultiplyVector(localPos);

    public Vector3 WorldToGridPosition(Vector3 worldPos) =>
        WorldToLocalPosition(worldPos) / offsetFactor;
    public Vector3 GridToWorldPosition(Vector3 gridPos) =>
        LocalToWorldPosition(gridPos * offsetFactor);

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
                    MarchChunk(at, 0);
                }
        ForEachPos(pos => GenerateSpringsForChunk(pos, true));

        primary.action.Enable();
        secondary.action.Enable();

        primary.action.performed += _ => isPainting = true;
        secondary.action.performed += _ => eraseMode = true;
        primary.action.canceled += _ => isPainting = false;
        secondary.action.canceled += _ => eraseMode = false;
    }

    void Update()
    {
        Render();
        ForEachChunk((pos, chunk) => chunk.highlight = false);
    }

    void FixedUpdate()
    {
        PrepareModules();
        Cleanup();

        if (isPainting) ForEachPos(pos => PaintChunk(pos, GlobalCursorPos, eraseMode));
        if (updateSprings)
        {
            ForEachChunk((pos, chunk) =>
            {
                chunk.isDirtyForVoxels |= chunk.isDirtyForSprings;
                UpdateSpringsDirty(pos, Time.fixedDeltaTime);
            });
            ForEachPos(pos => TieJointures(pos));
            ForEachPos(pos => GenerateMeshSpringsInChunk(pos));
            if (cycle >= CycleInterval)
            {
                ForEachChunk((pos, chunk) =>
                {
                    chunk.isDirtyForMesh |= chunk.isDirtyForVoxels;
                    VoxelizeChunkDirty(pos);
                });
            }
            ForEachChunk((pos, chunk) =>
            {
                chunk.isDirtyForSprings |= chunk.isDirtyForMesh;
                MarchChunkDirty(pos, 0);
            });
            ForEachPos(pos => GenerateSpringsDirty(pos, true));
        }
        else
        {
            if (cycle >= CycleInterval)
            {
                ForEachChunk((pos, chunk) =>
                {
                    chunk.isDirtyForMesh |= chunk.isDirtyForVoxels;
                    VoxelizeChunkDirty(pos);
                });
            }
            ForEachPos(pos => MarchChunkDirty(pos));
        }

        if (RayIntersection(GlobalCursorRay) is Vector3 hit)
            onCursorRayHit.Invoke(hit);

        cycle %= CycleInterval;
        cycle += Time.fixedDeltaTime;
    }

    void Render()
    {
        RenderParams rp = new(material);
        RenderParams rph = new(highlightMaterial);
        foreach (var (pos, chunk) in grid)
        {
            var matrix = objectTransform *
                    Matrix4x4.TRS((Vector3)pos * offsetFactor, Quaternion.identity, Vector3.one);

            switch (renderMode)
            {
                case RenderMode.MarchedMesh:
                    Graphics.RenderMesh(chunk.highlight ? rph : rp, chunk.mesh, 0, matrix);
                    break;
                case RenderMode.Springs:
                    springsRenderer.Render(chunk.springs, matrix, chunk.highlight);
                    break;
            }
        }
    }

    void PrepareModules()
    {
        offsetFactor = 2f - (float)4 / marchResolution;
        marchingCubes.resolution = marchResolution;
        marchingCubes.bounds.size = new Vector3(offsetFactor, offsetFactor, offsetFactor);
        objectTransform = transform.localToWorldMatrix;
    }

    public void CreateChunk(Vector3Int at)
    {
        grid.TryAdd(at, new ChunkData()
        {
            volume = new RenderTexture(baseVolume),
            volumeOfMesh = new RenderTexture(baseVolume),
        });
    }
    public bool ChunkExists(Vector3Int at) => grid.ContainsKey(at) && grid[at] != null;
    public void Cleanup() => ForEachChunk((pos, chunk) => { if (chunk == null) grid.Remove(pos); });

    public List<(Vector3Int, ChunkData)> SurroundingChunks(Vector3Int at, int chunkRadius = 1)
    {
        List<(Vector3Int, ChunkData)> neighbors = new(26);
        for (int x = -chunkRadius; x <= chunkRadius; x++)
            for (int y = -chunkRadius; y <= chunkRadius; y++)
                for (int z = -chunkRadius; z <= chunkRadius; z++)
                {
                    if (x == 0 && y == 0 && z == 0) continue;
                    Vector3Int neighborPos = at + new Vector3Int(x, y, z);
                    if (ChunkExists(neighborPos))
                        neighbors.Add((neighborPos, grid[neighborPos]));
                }
        return neighbors;
    }
    public Vector3? RayIntersection(Ray ray)
    {
        var rayOrigin = WorldToLocalPosition(ray.origin);
        var rayDirection = WorldToLocalDirection(ray.direction);
        foreach (var pos in grid.Keys)
        {
            Vector3 offset = (Vector3)pos * offsetFactor;
            var cursorRay = new Ray(rayOrigin - offset, rayDirection);
            if (MeshMod.RayMeshIntersection(grid[pos].mesh, cursorRay) is Vector3 hitMesh)
            {
                return LocalToWorldPosition(hitMesh + offset);
            }
        }
        return null;
    }

    public void ForEachPos(Action<Vector3Int> predicate)
    {
        foreach (var pos in grid.Keys)
            predicate(pos);
    }
    public void ForEachChunk(Action<Vector3Int, ChunkData> predicate)
    {
        foreach (var (pos, chunk) in grid)
            predicate(pos, chunk);
    }

    public void MarchChunkDirty(Vector3Int at, float threshold = 0)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        if (!forceUpdate && !chunk.isDirtyForMesh) return;
        MarchChunk(at, threshold);
        chunk.isDirtyForMesh = false;
    }
    public void GenerateSpringsDirty(Vector3Int at, bool joinNeighbors = false)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        if (!forceUpdate && !chunk.isDirtyForSprings) return;
        GenerateSpringsForChunk(at, joinNeighbors);
        chunk.isDirtyForSprings = false;
    }
    public void UpdateSpringsDirty(Vector3Int at, float deltaTime)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        if (!forceUpdate && !chunk.isDirtyForSprings) return;
        UpdateSpringsInChunk(at, deltaTime);
        chunk.isDirtyForSprings = false;
    }

    public void VoxelizeChunkDirty(Vector3Int at)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        if (!forceUpdate && !chunk.isDirtyForVoxels) return;
        VoxelizeChunk(at);
        chunk.isDirtyForVoxels = false;
    }

    public void RegenerateChunk(Vector3Int at)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        weightGenerator.offset = (Vector3)at * offsetFactor;
        weightGenerator.Generate(chunk.volume);
        chunk.isDirtyForVoxels = false;
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
            if (neighborChunk != null)
            {
                if (neighborChunk.mesh != null)
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
        }
        var mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        MeshMod.DeduplicateVertices(mesh, 0.01f, mesh);

        voxelizer.Voxelize(mesh, chunk.volume);
    }
    public void MarchChunk(Vector3Int at, float threshold = 0)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        chunk.mesh = chunk.mesh != null ? chunk.mesh : new();
        marchingCubes.GenerateMesh(chunk.volume, threshold, chunk.mesh);
        MeshMod.DeduplicateVertices(chunk.mesh, mergeDistance, chunk.mesh);
        Graphics.Blit(chunk.volume, chunk.volumeOfMesh);
    }
    public void PaintChunk(Vector3Int at, Vector3 brushPosition, bool eraseMode)
    {
        if (!ChunkExists(at))
            return;
        var chunk = grid[at];
        Vector3 chunkCenter = (Vector3)at * offsetFactor;
        Bounds chunkBoundsMargin = new(chunkCenter, Vector3.one * offsetFactor);
        Bounds paintBounds = new(chunkCenter, Vector3.one * 2);
        chunkBoundsMargin.Expand(paintRadius * 4);

        var localBrushPosition = WorldToLocalPosition(brushPosition);
        if (chunkBoundsMargin.Contains(localBrushPosition))
        {
            weightPainter.Paint(
                chunk.volume, LocalCursorPos,
                paintBounds, paintRadius, paintWeight,
                eraseMode
                    ? WeightPainterNobehaviour.ActionMode.Subtract
                    : WeightPainterNobehaviour.ActionMode.Add
            );
            chunk.isDirtyForMesh = true;
        }
    }
    public float DistanceOhMeshInChunk(Vector3Int at, RenderTexture output = null)
    {
        if (!ChunkExists(at))
            return 0f;
        var chunk = grid[at];
        return distanceOfVolumes.Distance(chunk.volume, chunk.volumeOfMesh, output);
    }
    public void GenerateSpringsForChunk(Vector3Int at, bool joinNeighbors = false)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        var previousSprings = chunk.springs;
        chunk.springs ??= new()
        {
            origin = (Vector3)at * offsetFactor,
            offsetFactor = offsetFactor,
        };
        chunk.jointures ??= new();
        SpringMeshConversion.MeshToSprings(chunk.mesh, mergeDistance, chunk.springs);

        foreach (var (neighbor, _) in chunk.jointures)
        {
            neighbor?.jointures?.Remove(chunk);
        }
        chunk.jointures.Clear();
        if (joinNeighbors)
            JoinChunkSpringsToNeighbors(at);
    }
    public void GenerateMeshSpringsInChunk(Vector3Int at)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        SpringMeshConversion.SpringsToMesh(chunk.springs, chunk.mesh);
    }
    public void JoinChunkSpringsToNeighbors(Vector3Int at)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        if (chunk.springs == null) return;
        SurroundingChunks(at)
            .ForEach(c =>
            {
                var (npos, neighbor) = c;
                if (neighbor.springs == null) return;
                var jointure = chunk.springs.Join(neighbor.springs, 0.01f);
                chunk.jointures.TryAdd(neighbor, jointure);
                neighbor.jointures[chunk] = jointure.Select(j => (j.other, j.self)).ToList();
            });
    }
    public void TieJointures(Vector3Int at)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        if (chunk.springs == null || chunk.jointures == null) return;
        foreach (var (neighbor, joinList) in chunk.jointures)
        {
            foreach (var (self, other) in joinList)
            {
                var selfPos = chunk.springs.LocalToGlobalPosition(self.position);
                var otherPos = neighbor.springs.LocalToGlobalPosition(other.position);

                var pos = (selfPos + otherPos) / 2f;
                var vel = (self.velocity + other.velocity) / 2f;
                var force = (self.force + other.force) / 2f;
                var normal = Vector3.Normalize(self.normal + other.normal);

                self.position = chunk.springs.GlobalToLocalPosition(pos);
                other.position = neighbor.springs.GlobalToLocalPosition(pos);
                self.velocity = vel;
                other.velocity = vel;
                self.force = force;
                other.force = force;
                self.normal = normal;
                other.normal = normal;
            }
        }
    }
    public void UpdateSpringsInChunk(Vector3Int at, float deltaTime)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        if (chunk.springs == null) return;
        springSimulator.Iterate(chunk.springs, deltaTime);
    }
    public void ApplyForceToSpringsInChunk(
        Vector3Int at,
        Vector3 force, Vector3 origin, float radius,
        bool affectNeighbors = false)
    {
        if (!ChunkExists(at)) return;
        var chunk = grid[at];
        var springs = chunk.springs;

        var forceLocal = WorldToLocalDirection(force);
        var originLocal = springs.GlobalToLocalPosition(WorldToGridPosition(origin));
        var radiusLocal = Vector3.Magnitude(WorldToLocalDirection(Vector3.forward * radius));
        springs.AddExternalForce(forceLocal, originLocal, radiusLocal);
        chunk.isDirtyForSprings = true;

        if (affectNeighbors)
        {
            SurroundingChunks(at)
                .ForEach(c => ApplyForceToSpringsInChunk(
                    c.Item1, force, origin, radius, false));
        }
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
