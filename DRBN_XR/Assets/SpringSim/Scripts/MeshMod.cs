using UnityEngine;
using System.Collections.Generic;

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

    static public void RescaleToBounds(ref Vector3[] positions, Bounds oldBounds, Vector3 newBounds)
    {
        Vector3 minBoundInput = oldBounds.center - oldBounds.size * 0.5f;
        Vector3 maxBoundInput = oldBounds.center + oldBounds.size * 0.5f;
        Vector3 minBoundOutput = -newBounds * 0.5f;
        Vector3 maxBoundOutput = newBounds * 0.5f;
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
}