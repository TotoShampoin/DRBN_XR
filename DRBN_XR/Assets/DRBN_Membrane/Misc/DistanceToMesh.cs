using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Functions to evaluate the distance between a point and a Mesh.
/// </summary>
public class DistanceToMesh
{
    /// <summary>
    /// Unsigned distance to a mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="point"></param>
    /// <param name="distanceThreshold">Tolerance for when detecting triangles that share the same vertex</param>
    /// <returns></returns>
    public static float UnsignedDistanceFunction(Mesh mesh, Vector3 point, float distanceThreshold = 0.0001f)
        => DistanceFunctionData(mesh, point, distanceThreshold).distance;

    /// <summary>
    /// Signed distance to a mesh. The "inside" is tied to the mesh's normals.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="point"></param>
    /// <param name="distanceThreshold">Tolerance for when detecting triangles that share the same vertex</param>
    /// <returns></returns>
    public static float SignedDistanceFunctionVolume(Mesh mesh, Vector3 point, float distanceThreshold = 0.0001f)
        => SignedDistanceFunctionVolume(DistanceFunctionData(mesh, point, distanceThreshold));
    public static float SignedDistanceFunctionVolume((float distance, float dot, int triangle) data)
    {
        var (distance, dot, _) = data;
        return distance * Mathf.Sign(dot);
    }
    /// <summary>
    /// Signed distance to a mesh. The "inside" is tied to the mesh's surface.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="point"></param>
    /// <param name="distanceThreshold">Tolerance for when detecting triangles that share the same vertex</param>
    /// <returns></returns>
    public static float SignedDistanceFunctionSurface(Mesh mesh, Vector3 point, float thickness = 0.1f, float distanceThreshold = 0.0001f)
        => DistanceFunctionData(mesh, point, distanceThreshold).distance - thickness;

    /// <summary>
    /// Unsigned Distance Function to a mesh, with some additional useful informations.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="point"></param>
    /// <param name="distanceThreshold"></param>
    /// <returns>The distance, the facing coefficient (dot product) and the triangle's index</returns>
    public static (float distance, float dot, int triangle) DistanceFunctionData(
        Mesh mesh, Vector3 point, float distanceThreshold = 0.0001f
    )
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var triangleIndices = mesh.triangles;
        float[] distances = new float[triangleIndices.Length / 3];
        float[] dots = new float[distances.Length];

        Parallel.For(0, distances.Length, i =>
        {
            var i0 = i * 3;
            var i1 = i * 3 + 1;
            var i2 = i * 3 + 2;

            var v0 = vertices[triangleIndices[i0]];
            var v1 = vertices[triangleIndices[i1]];
            var v2 = vertices[triangleIndices[i2]];
            var n0 = normals[triangleIndices[i0]];
            var n1 = normals[triangleIndices[i1]];
            var n2 = normals[triangleIndices[i2]];

            var p = ClosestOnTriangle((v0, v1, v2), point);
            var n = TriangleNormal((v0, v1, v2), (n0, n1, n2), p);
            var dirToPoint = (point - p).normalized;
            distances[i] = Vector3.Distance(point, p);
            dots[i] = Vector3.Dot(n, dirToPoint);
        });
        int minIndex = 0;
        for (int i = 1; i < distances.Length; i++)
        {
            if (distances[i] < distances[minIndex])
                minIndex = i;
        }
        // Then find the best triangle among those with similar distances
        float minDistance = distances[minIndex];
        float mostFacing = dots[minIndex];

        for (int i = 0; i < distances.Length; i++)
        {
            if (Mathf.Abs(distances[i] - minDistance) <= distanceThreshold)
            {
                var thisDot = dots[i];
                if (
                    (mostFacing <= 0 && thisDot < 0 && thisDot < mostFacing) ||
                    (mostFacing > 0 && thisDot >= 0 && thisDot > mostFacing) ||
                    (mostFacing < 0 && thisDot >= 0)
                )
                {
                    minIndex = i;
                    mostFacing = thisDot;
                }
            }
        }

        return (distances[minIndex], dots[minIndex], minIndex);
    }

    /// <summary>
    /// Find the closest point to a vector on a triangle
    /// </summary>
    /// <param name="triangle"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector3 ClosestOnTriangle((Vector3, Vector3, Vector3) triangle, Vector3 position)
    {
        var (a, b, c) = triangle;

        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = position - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);

        if (d1 <= 0f && d2 <= 0f)
            return a;

        Vector3 bp = position - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3)
            return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab;
        }

        Vector3 cp = position - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6)
            return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b);
        }

        float denom = 1f / (va + vb + vc);
        float vFinal = vb * denom;
        float wFinal = vc * denom;
        return a + ab * vFinal + ac * wFinal;
    }

    /// <summary>
    /// Get the triangle's normal interpolated at a given point on said triangle
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="normals"></param>
    /// <param name="mVertex"></param>
    /// <returns></returns>
    public static Vector3 TriangleNormal(
        (Vector3, Vector3, Vector3) positions,
        (Vector3, Vector3, Vector3) normals,
        Vector3 mVertex)
    {
        Vector3 v0 = positions.Item2 - positions.Item1;
        Vector3 v1 = positions.Item3 - positions.Item1;
        Vector3 v2 = mVertex - positions.Item1;

        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        if (denom == 0f)
            return (normals.Item1 + normals.Item2 + normals.Item3).normalized;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        Vector3 normal = (u * normals.Item1 + v * normals.Item2 + w * normals.Item3).normalized;
        return normal;
    }

}