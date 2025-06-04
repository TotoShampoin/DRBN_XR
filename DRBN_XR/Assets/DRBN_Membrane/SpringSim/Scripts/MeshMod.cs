using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MeshMod
{

    static public Mesh DeduplicateVertices(Mesh mesh, float epsilon = 0.0001f)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        List<Vector3> newVertices = new();
        List<int> vertexCumul = new();
        List<int> newTriangles = new();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get vertices for this triangle
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            // Find or add vertices to our deduplicated list
            int index1 = FindOrAddVertex(newVertices, vertexCumul, v1, epsilon);
            int index2 = FindOrAddVertex(newVertices, vertexCumul, v2, epsilon);
            int index3 = FindOrAddVertex(newVertices, vertexCumul, v3, epsilon);

            // Add triangle indices
            newTriangles.Add(index1);
            newTriangles.Add(index2);
            newTriangles.Add(index3);
        }

        // Create new mesh with deduplicated vertices
        Mesh result = new()
        {
            vertices = newVertices.ToArray(),
            triangles = newTriangles.ToArray()
        };
        result.RecalculateNormals();
        result.RecalculateBounds();

        return result;
    }

    static public void RescaleToBounds(ref Vector3[] positions, Bounds oldBounds, Bounds newBounds)
    {
        Vector3 minBoundInput = oldBounds.center - oldBounds.size * 0.5f;
        Vector3 maxBoundInput = oldBounds.center + oldBounds.size * 0.5f;
        Vector3 minBoundOutput = newBounds.center - newBounds.size * 0.5f;
        Vector3 maxBoundOutput = newBounds.center + newBounds.size * 0.5f;
        // Calculate the scale factor to maintain aspect ratio
        Vector3 inputSize = maxBoundInput - minBoundInput;
        Vector3 outputSize = maxBoundOutput - minBoundOutput;
        // Find the dimension with the largest ratio (most constrained)
        float scaleX = outputSize.x / inputSize.x;
        float scaleY = outputSize.y / inputSize.y;
        float scaleZ = outputSize.z / inputSize.z;
        float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
        // Calculate centered scaling with preserved aspect ratio
        Vector3 scaledSize = inputSize * uniformScale;
        Vector3 outputCenter = (minBoundOutput + maxBoundOutput) * 0.5f;
        Vector3 scaledMinBound = outputCenter - scaledSize * 0.5f;
        Vector3 scaledMaxBound = outputCenter + scaledSize * 0.5f;
        for (int i = 0; i < positions.Length; i++)
        {
            // Remap each position from input bounds to output bounds
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
    static public float[] DistanceOfGroups(Group of, Group with)
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

    static private int FindOrAddVertex(List<Vector3> vertices, List<int> vertexCumul, Vector3 vertex, float epsilon = 0.0001f)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if (Vector3.SqrMagnitude(vertices[i] - vertex) < epsilon)
            {
                vertices[i] = (vertices[i] * vertexCumul[i] + vertex) / (vertexCumul[i] + 1);
                vertexCumul[i]++;
                return i;
            }
        }

        // If vertex not found, add it
        vertices.Add(vertex);
        vertexCumul.Add(1);
        return vertices.Count - 1;
    }

    // Möller–Trumbore ray-triangle intersection
    static public bool RayTriangleIntersection(Vector3 rayOrigin, Vector3 rayDir, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hit, out float t)
    {
        hit = Vector3.zero;
        t = 0f;
        const float EPSILON = 1e-6f;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(rayDir, edge2);
        float a = Vector3.Dot(edge1, h);
        if (a > -EPSILON && a < EPSILON)
            return false; // Ray is parallel to triangle

        float f = 1.0f / a;
        Vector3 s = rayOrigin - v0;
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