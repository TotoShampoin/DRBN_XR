using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Profiling;

public class MeshMod
{
    static readonly ProfilerMarker dedupeMarker = new("Membrane.MeshMod.DeduplicateVertices");
    static readonly ProfilerMarker dedupeAllocMarker = new("Membrane.MeshMod.DeduplicateVertices.Allocation");
    static readonly ProfilerMarker dedupeVerticesMarker = new("Membrane.MeshMod.DeduplicateVertices.ProcessVertices");
    static readonly ProfilerMarker dedupeTrianglesMarker = new("Membrane.MeshMod.DeduplicateVertices.FillTriangles");
    static readonly ProfilerMarker distGroupMarker = new("Membrane.MeshMod.DistanceOfGroups");
    static readonly ProfilerMarker triRayMarker = new("Membrane.MeshMod.RayTriangleIntersection");
    static readonly ProfilerMarker meshRayMarker = new("Membrane.MeshMod.RayMeshIntersection");

    /// <summary>
    /// Takes a mesh, and merges vertices that are too close to each other
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="epsilon">Distance beyond which vertices get merged</param>
    /// <param name="result">The resulting new mesh if you don't want to reallocate</param>
    /// <returns>The resulting new mesh</returns>
    static public Mesh DeduplicateVertices(Mesh mesh, float epsilon = 0.0001f, Mesh result = null)
    {
        using (dedupeMarker.Auto())
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            result = result != null ? result : new();

            dedupeAllocMarker.Begin();
            var grid = new Dictionary<(int, int, int), int>();
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var vertexCumul = new List<int>();
            var map = new int[vertices.Length];
            dedupeAllocMarker.End();

            dedupeVerticesMarker.Begin();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var n = normals[i];
                var key = (
                    Mathf.RoundToInt(v.x / epsilon),
                    Mathf.RoundToInt(v.y / epsilon),
                    Mathf.RoundToInt(v.z / epsilon)
                );

                if (grid.TryGetValue(key, out int idx))
                {
                    newVertices[idx] = (newVertices[idx] * vertexCumul[idx] + v) / (vertexCumul[idx] + 1);
                    newNormals[idx] = (newNormals[idx] * vertexCumul[idx] + n) / (vertexCumul[idx] + 1);
                    vertexCumul[idx]++;
                    map[i] = idx;
                }
                else
                {
                    grid[key] = newVertices.Count;
                    newVertices.Add(v);
                    newNormals.Add(n);
                    vertexCumul.Add(1);
                    map[i] = newVertices.Count - 1;
                }
            }
            dedupeVerticesMarker.End();

            dedupeTrianglesMarker.Begin();
            var newTriangles = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
                newTriangles[i] = map[triangles[i]];
            dedupeTrianglesMarker.End();

            result.Clear();
            result.SetVertices(newVertices);
            result.SetNormals(newNormals);
            result.SetTriangles(newTriangles, 0);
            // result.RecalculateNormals();
            result.RecalculateBounds();

            return result;
        }
    }

    static public Mesh OffsetMesh(Mesh mesh, Vector3 offset, Mesh result = null)
    {
        result = result != null ? result : new();
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var triangles = mesh.triangles;
        Parallel.For(0, vertices.Length, i => vertices[i] = vertices[i] + offset);
        result.Clear();
        result.SetVertices(vertices);
        result.SetNormals(normals);
        result.SetTriangles(triangles, 0);
        result.RecalculateBounds();
        return result;
    }

    /// <summary>
    /// Resizes the mesh at the vertex level
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="oldBounds"></param>
    /// <param name="newBounds"></param>
    static public void RescaleToBounds(Vector3[] positions, Bounds oldBounds, Bounds newBounds)
    {
        Vector3 minBoundInput = oldBounds.center - oldBounds.size * 0.5f;
        Vector3 maxBoundInput = oldBounds.center + oldBounds.size * 0.5f;
        Vector3 minBoundOutput = newBounds.center - newBounds.size * 0.5f;
        Vector3 maxBoundOutput = newBounds.center + newBounds.size * 0.5f;
        Vector3 inputSize = maxBoundInput - minBoundInput;
        Vector3 outputSize = maxBoundOutput - minBoundOutput;
        float scaleX = outputSize.x / inputSize.x;
        float scaleY = outputSize.y / inputSize.y;
        float scaleZ = outputSize.z / inputSize.z;
        float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
        Vector3 scaledSize = inputSize * uniformScale;
        Vector3 outputCenter = (minBoundOutput + maxBoundOutput) * 0.5f;
        Vector3 scaledMinBound = outputCenter - scaledSize * 0.5f;
        Vector3 scaledMaxBound = outputCenter + scaledSize * 0.5f;
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 pos = positions[i];
            Vector3 normalizedPos = new(
                Mathf.InverseLerp(minBoundInput.x, maxBoundInput.x, pos.x),
                Mathf.InverseLerp(minBoundInput.y, maxBoundInput.y, pos.y),
                Mathf.InverseLerp(minBoundInput.z, maxBoundInput.z, pos.z)
            );
            positions[i] = new(
                Mathf.Lerp(scaledMinBound.x, scaledMaxBound.x, normalizedPos.x),
                Mathf.Lerp(scaledMinBound.y, scaledMaxBound.y, normalizedPos.y),
                Mathf.Lerp(scaledMinBound.z, scaledMaxBound.z, normalizedPos.z)
            );
        }
    }

    /// <summary>
    /// Returns the distance of each vertex with another set of vertices
    /// </summary>
    /// <param name="of"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    static public float[] DistanceOfVertices(IEnumerable<Vector3> of, IEnumerable<Vector3> with)
    {
        var kd = new KDTree3(with);
        return of.AsParallel().Select(o => kd.NearestDistance(o)).ToArray();
    }

    public struct Group
    {
        public Mesh mesh;
        public int[][] groups;
    }
    /// <summary>
    /// Divide a mesh into groups of adjacent triangles
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    static public Group GroupVertices(Mesh mesh)
    {
        var triangles = mesh.triangles;
        int vertexCount = mesh.vertexCount;
        int[] parent = Enumerable.Range(0, vertexCount).ToArray();

        int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(int x, int y)
        {
            int px = Find(x);
            int py = Find(y);
            if (px != py)
                parent[py] = px;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Union(triangles[i], triangles[i + 1]);
            Union(triangles[i], triangles[i + 2]);
        }

        var groupsDict = new Dictionary<int, List<int>>();
        for (int i = 0; i < vertexCount; i++)
        {
            int root = Find(i);
            if (!groupsDict.ContainsKey(root))
                groupsDict[root] = new List<int>();
            groupsDict[root].Add(i);
        }

        return new()
        {
            mesh = mesh,
            groups = groupsDict.Values.Select(g => g.ToArray()).ToArray(),
        };
    }
    /// <summary>
    /// Takes groups generated by GroupVertices and determines the distances of one group with another
    /// </summary>
    /// <param name="of"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    static public float[] DistanceOfGroups(Group of, Group with)
    {
        using (distGroupMarker.Auto())
        {
            var vertices = of.mesh.vertices;
            var kd = new KDTree3(with.mesh.vertices);
            return of.groups.AsParallel().Select(
                group => group
                    .AsParallel()
                    .Select(idx => kd.NearestDistance(vertices[idx]))
                    .Min()
            ).ToArray();
        }
    }

    /// <summary>
    /// Takes a bounds and enlarges any axis that extends too small
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="epsilon"></param>
    static public void PreventFlatBounds(ref Bounds bounds, float epsilon = 0.005f)
    {
        Vector3 size = bounds.size;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        if (size.x < epsilon)
        {
            min.x -= 0.5f;
            max.x += 0.5f;
        }
        if (size.y < epsilon)
        {
            min.y -= 0.5f;
            max.y += 0.5f;
        }
        if (size.z < epsilon)
        {
            min.z -= 0.5f;
            max.z += 0.5f;
        }
        bounds.SetMinMax(min, max);
    }

    /// <summary>
    /// Ray to triangle intersection by Möller–Trumbore
    /// </summary>
    /// <param name="rayOrigin"></param>
    /// <param name="rayDir"></param>
    /// <param name="v0"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="hit"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    static public bool RayTriangleIntersection(Vector3 rayOrigin, Vector3 rayDir, (Vector3 v0, Vector3 v1, Vector3 v2) triangle, out Vector3 hit, out float t)
    {
        using (triRayMarker.Auto())
        {
            hit = Vector3.zero;
            t = 0f;
            const float EPSILON = 1e-6f;
            Vector3 edge1 = triangle.v1 - triangle.v0;
            Vector3 edge2 = triangle.v2 - triangle.v0;
            Vector3 h = Vector3.Cross(rayDir, edge2);
            float a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON)
                return false;

            float f = 1.0f / a;
            Vector3 s = rayOrigin - triangle.v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDir, q);
            if (v < 0.0f || u + v > 1.0f)
                return false;

            t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON)
            {
                hit = rayOrigin + rayDir * t;
                return true;
            }
            else
                return false;
        }
    }

    /// <summary>
    /// Ray to mesh intersection using RayTriangleIntersection
    /// </summary>
    public static Vector3? RayMeshIntersection(Mesh mesh, Ray ray)
    {
        using (meshRayMarker.Auto())
        {
            var rayOrigin = ray.origin;
            var rayDirection = ray.direction;
            float closestDist = float.MaxValue;
            Vector3 hitPoint = Vector3.zero;
            bool hit = false;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            if (!mesh.bounds.IntersectRay(ray)) return null;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i + 0]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                if (RayTriangleIntersection(
                        rayOrigin, rayDirection, (v0, v1, v2),
                        out Vector3 tempHit, out float dist
                    ) && dist < closestDist
                )
                {
                    closestDist = dist;
                    hitPoint = tempHit;
                    hit = true;
                }
            }

            return hit ? hitPoint : null;
        }
    }
}